# Meridian Backend

This is the backend repository for the **Meridian** fleet management and route optimization platform. It is built as a set of microservices on **.NET 10.0** and **ASP.NET Core Web API**, with an API gateway in front.

## 🏗️ Current Structure

The backend is organized into an API gateway and several microservices, following Domain-Driven Design principles. Paths below are relative to this folder (`Meridian-Backend`):

1. **ApiGateway (`/src/ApiGateway`)**  
   - Ocelot API gateway, HTTP entry point for the frontend (Port `5050`)  
   - Handles routing, CORS, and symmetric JWT validation (`MeridianBearer`)

2. **UserService (`/src/UserService/UserService.API`)**  
   - User registration, login, refresh tokens, and role management (Port `6007`)  
   - Issues JWTs that are validated by the gateway

3. **DeliveryService (`/src/DeliveryService/DeliveryService.API`)**  
   - Delivery lifecycle and status tracking (Port `6001`, gRPC `7001`)

4. **VehicleService (`/src/VehicleService/VehicleService.API`)**  
   - Vehicle CRUD, capacity, and availability (Port `6002`, gRPC `7002`)

5. **DriverService (`/src/DriverService/DriverService.API`)**  
   - Driver CRUD, availability, and working hours (Port `6003`, gRPC `7003`)

6. **AssignmentService (`/src/AssignmentService/AssignmentService.API`)**  
   - Vehicle/driver assignment logic and recommendations (Port `6004`, gRPC `7004`)

7. **RouteService (`/src/RouteService/RouteService.API`)**  
- Route optimization, Google Maps Routes API integration, fuel cost estimation, Redis-backed route cache (Port `6005`, gRPC `7005`)
- HTTP endpoints (via the ApiGateway):
  - `POST /api/routes/optimize` – optimize a route and return the best option plus alternatives
  - `GET /api/routes/calculate` – calculate distance, ETA, and polyline between origin and destination
  - `GET /api/routes/alternatives` – list alternative route options from Google Routes API
  - `GET /api/routes/compare` – compare alternative routes with distance, ETA, and fuel cost metrics
  - `GET /api/routes/rank` – return ranked routes including fuel consumption (L), fuel cost (LKR), and duration (hours)

8. **TrackingService (`/src/TrackingService/TrackingService.API`)**  
   - Real-time GPS tracking via SignalR hub, location history (Port `6006`)

9. **Architecture & Docs (`/docs`)**  
   - `MERIDIAN_ARCHITECTURE_v2.md` (authoritative architecture), API gateway notes, Azure deployment guide, and contribution guidelines

### 🛠️ What's Already Configured

* **Microservice scaffolding:** Each service has pre-created folders and starter classes for `Controllers`, `Services`, `Repositories`, `Models`, and `DTOs`.
* **API Gateway & Auth:** Ocelot-based gateway with symmetric JWT auth (`MeridianBearer`) and CORS configured for the frontend (`http://localhost:3000`).
* **gRPC for internal calls:** `.proto` contracts and gRPC clients/servers set up for inter-service communication (e.g., Assignment → Delivery/Vehicle/Driver, Route → Vehicle).
* **Database migrations:** Each service uses **DbUp** with SQL scripts under its `Migrations` folder, executed automatically on startup.
* **SQL Server & Redis:** Local development uses Dockerized **SQL Server** and **Redis**. Connection strings and secrets live in git-ignored `appsettings.Development.json`.
* **API documentation:** Swagger/OpenAPI is enabled in development for all HTTP services (e.g., `http://localhost:6001/swagger`, `http://localhost:6002/swagger`, `http://localhost:6003/swagger`, etc.).
* **Real-time tracking:** A SignalR hub in `TrackingService` exposes `/hubs/tracking` via the gateway for live location updates.

## 🚀 How to Run Locally

1. **Start the infrastructure (SQL Server, Redis)**  
   Ensure Docker is running. From the root of the overall project (`Meridian`, one level above this folder), run:

   ```bash
   docker compose up -d
   ```

   This starts SQL Server (`localhost:1433`) and Redis (`localhost:6379`).

2. **Run the backend services**  
   From this folder (`Meridian-Backend`), use separate terminals for each process:

   ```bash
   # API Gateway
   cd src/ApiGateway
   dotnet run   # :5050

   # Auth / Users
   cd src/UserService/UserService.API
   dotnet run   # :6007

   # Core domain services
   cd src/DeliveryService/DeliveryService.API
   dotnet run   # :6001

   cd src/VehicleService/VehicleService.API
   dotnet run   # :6002

   cd src/DriverService/DriverService.API
   dotnet run   # :6003

   cd src/AssignmentService/AssignmentService.API
   dotnet run   # :6004

   cd src/RouteService/RouteService.API
   dotnet run   # :6005

   cd src/TrackingService/TrackingService.API
   dotnet run   # :6006
   ```

   On first run, each service connects to SQL Server, creates its database, and applies DbUp migration scripts.

3. **(Optional) Run the frontend**  
   The Next.js frontend lives in the sibling folder `meridian-frontend`:

   ```bash
   cd ../meridian-frontend
   npm install
   npm run dev   # http://localhost:3000
   ```

   The frontend talks to the API Gateway at `http://localhost:5050` and uses `ws://localhost:5050/hubs/tracking` for real-time updates.

## 🗺️ Future Plans & Developer Roadmap

High-level roadmap items (see `docs/MERIDIAN_ARCHITECTURE_v2.md` for full detail):

* **Business logic implementation:** Flesh out controllers, services, and repositories to match the architecture spec for each bounded context.
* **Google Maps integration:** Complete Directions/Distance Matrix usage in `RouteService` with proper caching in Redis.
* **Real-time UX:** Enhance the frontend’s live tracking and status updates using the existing SignalR hub.
* **Testing & CI/CD:** Add unit/integration test projects and GitHub Actions workflows to build, test, containerize, and deploy services to Azure.
