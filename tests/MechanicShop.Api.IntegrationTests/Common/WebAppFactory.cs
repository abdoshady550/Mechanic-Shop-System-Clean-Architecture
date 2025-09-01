using DotNet.Testcontainers.Builders;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Infrastructure.BackgroundJobs;
using MechanicShop.Infrastructure.Data;

using MediatR;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Testcontainers.MsSql;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Common;

public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU13-ubuntu-22.04") // Use specific working version
        .WithPassword("YourStrong@Password123!") // Use stronger password
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .WithStartupCallback(async (container, ct) =>
        {
            // Wait additional time for SQL Server to be fully ready
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        })
        .Build();

    public AppHttpClient CreateAppHttpClient()
    {
        return new AppHttpClient(CreateClient());
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
        await _dbContainer.StartAsync();

        // Additional wait to ensure SQL Server is fully ready
        await Task.Delay(TimeSpan.FromSeconds(5));

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created and ready
        await context.Database.EnsureCreatedAsync();

        context.WorkOrders.RemoveRange(context.WorkOrders);
        await context.SaveChangesAsync();
    }

    public new Task DisposeAsync() => _dbContainer.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<OverdueBookingCleanupService>();

            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
    }
}