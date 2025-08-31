# 🛠️ MechanicShop System

![MechanicShop Banner](https://t3.ftcdn.net/jpg/04/36/49/44/360_F_436494409_m2NAPydjqYtrh3lzqde8HKbw7jkUEwSq.jpg)
<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/ASP.NET-Core-5C2D91?style=for-the-badge&logo=dotnet&logoColor=white"/>
  <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white"/>
  <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white"/>
  <img src="https://img.shields.io/badge/Grafana-F46800?style=for-the-badge&logo=grafana&logoColor=white"/>
</p>
Welcome to the MechanicShop System! This project is a modern web application designed to streamline the operations of a mechanic workshop: manage customers and vehicles, schedule and relocate work orders by spot and time, track states, define repair tasks and labor, and handle invoicing (issue, fetch, PDF download, mark paid) with role-based access. 🚀

## ✨ Features

- **Comprehensive Workshop Management**: Manage work orders, customer data, and repair tasks efficiently.
- **Modern Web Interface**: A responsive and interactive frontend for seamless user experience.
- **Robust Backend API**: A powerful ASP.NET Core API handling all business logic and data interactions.
- **Containerized Environment**: Easy setup and deployment using Docker and Docker Compose.
- **Real-time Updates**: Stay informed with live updates on work order statuses.
- **Advanced Monitoring & Logging**: Integrated tools for performance monitoring and centralized logging.
- **Secure Authentication**: JWT-based authentication for secure access.

## 🏗️ Project Structure

The project is organized into several logical layers within the `src` directory, promoting a clean architecture and separation of concerns:

- **`src/MechanicShop.Api`**: The heart of the backend, an ASP.NET Core Web API that exposes endpoints for frontend interaction and handles core business processes.
- **`src/MechanicShop.Client`**: The interactive frontend application, likely built with Blazor WebAssembly, providing a rich user interface.
- **`src/MechanicShop.Application`**: Contains the application's business logic and use cases, orchestrating operations between the domain and infrastructure layers.
- **`src/MechanicShop.Domain`**: Defines the core business entities, value objects, and domain rules, representing the essence of the MechanicShop business.
- **`src/MechanicShop.Contracts`**: Holds shared data transfer objects (DTOs) and interfaces used for communication between different layers and services.
- **`src/MechanicShop.Infrastructure`**: Manages external concerns such as database interactions, identity management, external services, and real-time communication. (More details below! 👇)

## 🚀 Technologies Used

This project harnesses the power of a diverse set of modern technologies:

- **.NET 9.0**: The latest version of Microsoft's versatile development platform.
- **ASP.NET Core**: For building high-performance, cross-platform web APIs.
- **Blazor WebAssembly**: (Likely) For creating interactive client-side web UIs with C#.
- **Docker**: Containerization for consistent environments across development and production.
- **Docker Compose**: Orchestration for multi-container Docker applications.
- **SQL Server**: Robust relational database for persistent data storage.
- **Seq**: Centralized logging server for collecting and visualizing application logs. 📊
- **Prometheus**: Open-source monitoring system for collecting and storing time-series metrics. 📈
- **Grafana**: Data visualization and dashboarding tool for analyzing Prometheus metrics. 📉
- **Serilog**: Flexible logging library for .NET applications.
- **Entity Framework Core**: Object-relational mapper (ORM) for database interactions.
- **JWT Bearer Authentication**: Secure token-based authentication for APIs.
- **SignalR**: (Likely) For real-time web functionality, enabling instant updates.

## ⚙️ Infrastructure Layer (`src/MechanicShop.Infrastructure`)

The `src/MechanicShop.Infrastructure` layer is crucial for abstracting away external technical concerns from the core business logic. It provides implementations for interfaces defined in the `Application` and `Domain` layers, connecting the application to the outside world. Here's what it typically handles:

- **Data Access**: Manages interactions with the database using Entity Framework Core, including `AppDbContext` for database sessions, and interceptors (`AuditableEntityInterceptor`) for automated behaviors like auditing.
- **Identity & Authentication**: Implements user management, roles, and authentication using ASP.NET Core Identity and JWT Bearer tokens. It defines authorization policies (e.g., `ManagerOnly`, `SelfScopedWorkOrderAccess`) to control access to resources.
- **Real-time Communication**: Integrates real-time features, likely via SignalR, to push instant updates to clients (e.g., `SignalRWorkOrderNotifier`).
- **External Services**: Provides concrete implementations for various external services, such as `InvoicePdfGenerator` for PDF creation and `NotificationService` for sending notifications.
- **Background Jobs**: Contains services that run independently in the background, like `OverdueBookingCleanupService` for periodic tasks.
- **Caching**: Utilizes `HybridCache` for performance optimization by temporarily storing frequently accessed data.

This separation ensures that the core business logic remains clean, testable, and flexible, allowing for easier changes to underlying technologies without impacting the application's core functionality. 🌟

## 🚀 Getting Started

To get this project up and running, you'll need Docker and Docker Compose installed. Once you have them, navigate to the project root and run:

```bash
docker-compose up --build
```

This command will build the `mechanicshop-api` image, and then start all the defined services (API, SQL Server, Seq, Prometheus, Grafana). 

## 🌐 Access Points

Once the services are running, you can access them at the following URLs:

- **MechanicShop API**: `http://localhost:5001`
- **SQL Server**: `localhost:1433`
- **Seq (Logging)**: `http://localhost:8081`
- **Prometheus (Monitoring)**: `http://localhost:9090`
- **Grafana (Dashboards)**: `http://localhost:3000`

Enjoy exploring the MechanicShop System! If you have any questions or suggestions, feel free to open an issue. 😊
