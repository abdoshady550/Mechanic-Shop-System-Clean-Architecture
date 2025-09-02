using DotNet.Testcontainers.Builders;

using MechanicShop.Api;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Infrastructure.BackgroundJobs;
using MechanicShop.Infrastructure.Data;
using MechanicShop.Infrastructure.Settings;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Testcontainers.MsSql;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Common;

public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer;

    public WebAppFactory()
    {
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Password123!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Express")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/mssql-tools/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "YourStrong@Password123!", "-Q", "SELECT 1"))
            .WithStartupCallback(async (container, ct) =>
            {
                // Give SQL Server extra time to fully initialize
                await Task.Delay(TimeSpan.FromSeconds(10), ct);

                // Verify connection works
                var connectionString = container.GetConnectionString();
                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync(ct);
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(ct);
            })
            .Build();
    }

    public IMediator CreateMediator()
    {
        var serviceScope = Services.CreateScope();
        return serviceScope.ServiceProvider.GetRequiredService<IMediator>();
    }

    public IAppDbContext CreateAppDbContext()
    {
        var serviceScope = Services.CreateScope();
        return serviceScope.ServiceProvider.GetRequiredService<IAppDbContext>();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Start the container and wait for it to be ready
            await _dbContainer.StartAsync();

            // Additional wait to ensure SQL Server is fully ready
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Initialize the database schema
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure database is created and migrated
            await context.Database.EnsureCreatedAsync();

            // Clean up any existing test data
            if (context.WorkOrders.Any())
            {
                context.WorkOrders.RemoveRange(context.WorkOrders);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize test database: {ex.Message}", ex);
        }
    }

    public new async Task DisposeAsync()
    {
        try
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
        }
        catch
        {
            // Ignore disposal errors
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Clear existing configuration sources to avoid conflicts
            configBuilder.Sources.Clear();

            // Add test-specific configuration
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                ["AppSettings:OpeningTime"] = "09:00",
                ["AppSettings:ClosingTime"] = "18:00",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove background services that might interfere with tests
            services.RemoveAll<IHostedService>();
            services.RemoveAll<OverdueBookingCleanupService>();

            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();

            // Add test-specific DbContext with proper configuration
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlServer(_dbContainer.GetConnectionString(), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                });

                // Add interceptors
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                // Enable sensitive data logging for tests
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();

                // Reduce logging noise in tests
                options.LogTo(message => { }, LogLevel.Warning);
            });

            // Configure application settings for tests
            services.Configure<AppSettings>(opts =>
            {
                opts.OpeningTime = new TimeOnly(9, 0);
                opts.ClosingTime = new TimeOnly(18, 0);
            });

            // Configure logging to reduce noise
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });

        // Use test environment
        builder.UseEnvironment("Test");
    }
}