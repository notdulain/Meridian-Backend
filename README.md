# Meridian Backend

This is the backend repository for the **Meridian** fleet management and route optimization platform. It is built using a microservices architecture on **.NET 10.0** and **ASP.NET Core Web API**.

## 🏗️ Current Structure

The backend is divided into three distinct microservices, following Domain-Driven Design principles:

1. **Delivery Service (`/src/DeliveryService/DeliveryService.API`)**: Manages the delivery lifecycle and status tracking (Port `6001`).
2. **Fleet Service (`/src/FleetService/FleetService.API`)**: Manages vehicles, drivers, and capacity constraints (Port `6002`).
3. **Route Service (`/src/RouteService/RouteService.API`)**: Handles route optimization, fuel calculation, and tracking (Port `6003`).

### 🛠️ What's Already Configured
*   **Database Migrations:** Configured using **DbUp**. SQL scripts are stored in the `Migrations` folder of each service as embedded resources and run automatically on startup.
*   **Database & Caching Provider:** Local environments use a Dockerized **SQL Server** for relational data and **Redis** for caching. connection strings are securely managed in `appsettings.Development.json` (git-ignored).
*   **Security:** **JWT Bearer Authentication** is wired up in the ASP.NET pipeline, along with appropriate **CORS** policies for frontend communication.
*   **API Documentation:** Configured using .NET 10's built-in `Microsoft.AspNetCore.OpenApi`. Endpoints can be tested using the `/openapi/v1.json` auto-generated endpoints, or by plugging them into Swagger UI.
*   **Code Scaffolding:** Each microservice has pre-created folders and placeholder C# classes for `Models`, `DTOs`, `Controllers`, `Services`, and `Repositories`.

## 🚀 How to Run Locally

1. **Start the Infrastructure (Database Cache):**
   Ensure Docker is running on your machine. From the root directory of the entire Meridian project (one level up from this folder), run:
   ```bash
   docker-compose up -d
   ```
   This will spin up the local SQL Server port `1433`) and Redis (port `6379`) instances.

2. **Run the Microservices:**
   Open separate terminal instances for each service and execute:
   ```bash
   # Terminal 1: Delivery Service
   cd src/DeliveryService/DeliveryService.API
   dotnet run

   # Terminal 2: Fleet Service
   cd src/FleetService/FleetService.API
   dotnet run

   # Terminal 3: Route Service
   cd src/RouteService/RouteService.API
   dotnet run
   ```
   *Note: On their first run, the services will automatically connect to the SQL Server container, provision their respective databases (`delivery_dev`, `fleet_dev`, `route_dev`), and run any pending DbUp migration scripts.*

## 🗺️ Future Plans & Developer Roadmap

As developers pick up this scaffolded project, the following areas require building out:

*   **Business Logic Implementation:** Populate the empty `Controllers`, `Services`, and `Repositories` with the core business rules defined in the system architecture.
*   **Google Maps Integration:** Integrate the Google Maps Directions and Distance Matrix APIs into the `RouteService` to power the optimization engine.
*   **Real-Time Tracking:** Implement **SignalR** hubs (likely within the Route or Delivery service) to push live GPS location updates and delivery status changes to the client.
*   **Asynchronous Messaging:** Introduce a message broker (like Azure Service Bus or RabbitMQ) to handle eventual consistency and cross-service domain events (e.g., `DeliveryCreated`, `VehicleAssigned`).
*   **Testing Setup:** Introduce xUnit or NUnit projects for unit and integration testing.
*   **Azure Deployment Strategy:** Containerize these APIs using Docker and set up CI/CD pipelines (GitHub Actions) to deploy to Azure App Services or Azure Container Apps.
