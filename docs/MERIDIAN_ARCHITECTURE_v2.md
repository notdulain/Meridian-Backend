# Meridian - Fleet Management & Route Optimization Platform
## Complete Technical Architecture & Deployment Guide

**Version:** 2.0
**Date:** February 26, 2026
**Project Team:** IT23750760, IT23632332, IT23664708, IT23631724, IT23762336
**Supersedes:** MERIDIAN_ARCHITECTURE v1.0

> [!NOTE]
> This document supersedes v1.0. Major changes from v1: 6 microservices (was 3), WSO2 Identity Server for IAM (was custom JWT), Ocelot API Gateway on port 5050, gRPC for inter-service communication, SQL Server 2022 (was MySQL), standalone TrackingService.

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [System Overview](#2-system-overview)
3. [Architecture Design](#3-architecture-design)
4. [Microservices Specification](#4-microservices-specification)
5. [Database Architecture](#5-database-architecture)
6. [API Design & Inter-Service Communication](#6-api-design--inter-service-communication)
7. [Authentication & Security](#7-authentication--security)
8. [Real-Time Communication](#8-real-time-communication)
9. [External Integrations](#9-external-integrations)
10. [Frontend Architecture](#10-frontend-architecture)
11. [Development Environment Setup](#11-development-environment-setup)
12. [CI/CD Pipeline](#12-cicd-pipeline)
13. [Cloud Deployment Architecture](#13-cloud-deployment-architecture)
14. [Project Structure](#14-project-structure)

---

## 1. Executive Summary

Meridian is a microservices-based fleet management and route optimization platform designed for logistics companies. The system enables efficient delivery operations through intelligent vehicle assignment, real-time tracking, and route optimization using Google Maps APIs.

### Key Features
- Delivery lifecycle management
- Separate Vehicle and Driver management with capacity constraints
- Rule-based vehicle assignment engine (AssignmentService)
- Multi-route optimization with fuel cost estimation
- Real-time GPS tracking with SignalR (dedicated TrackingService)
- WSO2 Identity Server for role-based authentication (Admin, Dispatcher, Driver)
- Operational analytics and reporting

### Technology Stack

| Layer | Technology |
|---|---|
| **Frontend** | Next.js (React 18+) |
| **Backend** | ASP.NET Core (.NET 10) |
| **API Gateway** | Ocelot on ASP.NET Core |
| **IAM** | WSO2 Identity Server 7.2.0 |
| **Database** | SQL Server 2022 |
| **Cache** | Redis |
| **Real-time** | ASP.NET Core SignalR |
| **Inter-service** | gRPC |
| **Containerization** | Docker & Docker Compose |
| **CI/CD** | GitHub Actions |
| **Cloud Platform** | Microsoft Azure |
| **External APIs** | Google Maps (Directions API, Distance Matrix API) |

---

## 2. System Overview

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        USER INTERFACE                           │
│                  Next.js (meridian-frontend)                    │
│                   http://localhost:3000 (dev)                   │
└────────────────────┬────────────────────────────────────────────┘
                     │ HTTPS/REST
                     │
┌────────────────────▼────────────────────────────────────────────┐
│                     API GATEWAY (Ocelot)                        │
│              Port: 5050 (local) | 443 (production)              │
│     Authentication (WSO2 JWT), Rate Limiting, CORS, Routing     │
└─┬──────┬─────────┬──────────┬──────────┬──────────┬────────────┘
  │      │         │          │          │          │
  │REST  │REST     │REST      │REST      │REST      │WS
  ▼      ▼         ▼          ▼          ▼          ▼
┌──────┐ ┌───────┐ ┌────────┐ ┌────────┐ ┌───────┐ ┌─────────┐
│Deliv.│ │Vehicle│ │Driver  │ │Assign. │ │Route  │ │Tracking │
│Svc   │ │Svc    │ │Svc     │ │Svc     │ │Svc    │ │Svc      │
│:6001 │ │:6002  │ │:6003   │ │:6004   │ │:6005  │ │:6006    │
└──────┘ └───────┘ └────────┘ └────────┘ └───────┘ └─────────┘
                                                       SignalR Hub
                  ◄── gRPC (inter-service) ──►

┌───────────────────────────────────────────────────────────────┐
│             Shared Infrastructure (Docker)                    │
│  SQL Server :1433  │  Redis :6379  │  WSO2 IS :9443/:9763    │
└───────────────────────────────────────────────────────────────┘
```

### 2.2 Design Principles

1. **Microservices Architecture:** Each service is independently deployable and scalable
2. **Domain-Driven Design:** Services organized around business capabilities
3. **Database per Service:** Each microservice owns its own SQL Server database
4. **API-First Design:** RESTful APIs exposed externally via Ocelot; gRPC used internally
5. **Stateless Services:** Services don't maintain session state
6. **Centralized Auth:** WSO2 IS issues JWTs; Ocelot validates them at the gateway boundary
7. **Resilience:** Circuit breakers, retries, and fallback mechanisms on gRPC clients

---

## 3. Architecture Design

### 3.1 Microservices Overview

| Service | Responsibility | Port | Database | gRPC Port |
|---------|---------------|------|----------|-----------|
| **DeliveryService** | Delivery lifecycle, status tracking | 6001 | `meridian_delivery` | 7001 |
| **VehicleService** | Vehicle CRUD, capacity, availability | 6002 | `meridian_vehicle` | 7002 |
| **DriverService** | Driver CRUD, availability, working hours | 6003 | `meridian_driver` | 7003 |
| **AssignmentService** | Vehicle-driver-delivery assignment engine | 6004 | `meridian_assignment` | 7004 |
| **RouteService** | Route optimization, Google Maps, fuel calc | 6005 | — (Redis cache) | 7005 |
| **TrackingService** | Real-time GPS tracking, SignalR hub | 6006 | `meridian_tracking` | — |
| **ApiGateway** | Routing, auth, rate limiting, CORS | 5050 | — | — |

> [!IMPORTANT]
> Port 6000 is blocked by Chromium-based browsers (`ERR_UNSAFE_PORT`). The API Gateway runs on **5050** locally.

### 3.2 Service Boundaries

#### DeliveryService
- **Bounded Context:** Delivery operations
- **Core Entities:** Delivery, StatusHistory
- **Responsibilities:** CRUD operations, status lifecycle (Pending → Assigned → InTransit → Completed/Failed), delivery history

#### VehicleService
- **Bounded Context:** Fleet management
- **Core Entities:** Vehicle
- **Responsibilities:** Vehicle CRUD, capacity tracking, availability status, fuel efficiency data

#### DriverService
- **Bounded Context:** Workforce management
- **Core Entities:** Driver
- **Responsibilities:** Driver CRUD, working hours management, availability status

#### AssignmentService
- **Bounded Context:** Assignment operations
- **Core Entities:** Assignment
- **Responsibilities:** Vehicle-driver assignment logic, capacity validation, recommendation engine
- **gRPC Clients:** Calls DeliveryService, VehicleService, DriverService

#### RouteService
- **Bounded Context:** Route optimization
- **Core Entities:** RouteOption (Redis-cached)
- **Responsibilities:** Google Maps API integration, multi-route fetching, fuel cost calculation, route caching
- **gRPC Clients:** Calls VehicleService for fuel efficiency data

#### TrackingService
- **Bounded Context:** Real-time tracking
- **Core Entities:** LocationUpdate
- **Responsibilities:** SignalR hub for live GPS updates, location history storage
- **Ocelot Route:** WebSocket (`ws://`) proxied from `/hubs/tracking`

### 3.3 Inter-Service Communication

**Pattern:** gRPC for all service-to-service calls (previously HTTP/REST in v1)

**Call Map:**

```
AssignmentService ──gRPC──► DeliveryService   (verify delivery exists, get package dims)
AssignmentService ──gRPC──► VehicleService    (get available vehicles, capacity check)
AssignmentService ──gRPC──► DriverService     (get available drivers)
RouteService      ──gRPC──► VehicleService    (get fuel efficiency for cost calculation)
```

**External REST (via Ocelot):**
```
Frontend ──REST──► Ocelot (5050) ──REST──► Downstream services
```

---

## 4. Microservices Specification

### 4.1 DeliveryService (Port 6001 / gRPC 7001)

#### API Endpoints
```
Base: http://localhost:5050/delivery/  (via gateway)

POST   /api/Deliveries              Create delivery
GET    /api/Deliveries/{id}         Get delivery by ID
PUT    /api/Deliveries/{id}         Update delivery
PUT    /api/Deliveries/{id}/status  Update delivery status
DELETE /api/Deliveries/{id}         Cancel delivery
```

#### gRPC Service Definition
```protobuf
// Protos/delivery.proto
syntax = "proto3";
option csharp_namespace = "DeliveryService.API.Grpc";

service DeliveryGrpc {
    rpc GetDelivery (GetDeliveryRequest) returns (DeliveryReply);
    rpc GetDeliveryStatus (GetDeliveryRequest) returns (DeliveryStatusReply);
}

message GetDeliveryRequest { int32 id = 1; }

message DeliveryReply {
    int32 id = 1;
    string pickup_address = 2;
    string delivery_address = 3;
    double package_weight_kg = 4;
    double package_volume_m3 = 5;
    string deadline = 6;
    string status = 7;
}

message DeliveryStatusReply {
    int32 id = 1;
    string status = 2;
}
```

#### Data Models
```csharp
public class Delivery
{
    public int Id { get; set; }
    public string PickupAddress { get; set; }
    public string DeliveryAddress { get; set; }
    public decimal PackageWeightKg { get; set; }
    public decimal PackageVolumeM3 { get; set; }
    public DateTime Deadline { get; set; }
    public DeliveryStatus Status { get; set; }
    public int? AssignedVehicleId { get; set; }
    public int? AssignedDriverId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
}

public enum DeliveryStatus { Pending, Assigned, InTransit, Delivered, Failed, Canceled }
```

---

### 4.2 VehicleService (Port 6002 / gRPC 7002)

#### API Endpoints
```
Base: http://localhost:5050/vehicle/

POST   /api/Vehicles              Create vehicle
GET    /api/Vehicles              List vehicles
GET    /api/Vehicles/{id}         Get vehicle by ID
PUT    /api/Vehicles/{id}         Update vehicle
DELETE /api/Vehicles/{id}         Delete vehicle
GET    /api/Vehicles/available    Get available vehicles
```

#### gRPC Service Definition
```protobuf
// Protos/vehicle.proto
syntax = "proto3";
option csharp_namespace = "VehicleService.API.Grpc";

service VehicleGrpc {
    rpc GetVehicle (GetVehicleRequest) returns (VehicleReply);
    rpc GetAvailableVehicles (Empty) returns (VehicleListReply);
}

message GetVehicleRequest { int32 id = 1; }
message Empty {}

message VehicleReply {
    int32 id = 1;
    string license_plate = 2;
    string type = 3;
    double capacity_weight_kg = 4;
    double capacity_volume_m3 = 5;
    double fuel_efficiency_km_per_l = 6;
    string status = 7;
}

message VehicleListReply { repeated VehicleReply vehicles = 1; }
```

---

### 4.3 DriverService (Port 6003 / gRPC 7003)

#### API Endpoints
```
Base: http://localhost:5050/driver/

POST   /api/Drivers              Create driver
GET    /api/Drivers              List drivers
GET    /api/Drivers/{id}         Get driver by ID
PUT    /api/Drivers/{id}         Update driver
DELETE /api/Drivers/{id}         Delete driver
GET    /api/Drivers/available    Get available drivers
```

#### gRPC Service Definition
```protobuf
// Protos/driver.proto
syntax = "proto3";
option csharp_namespace = "DriverService.API.Grpc";

service DriverGrpc {
    rpc GetDriver (GetDriverRequest) returns (DriverReply);
    rpc GetAvailableDrivers (Empty) returns (DriverListReply);
}

message GetDriverRequest { int32 id = 1; }
message Empty {}

message DriverReply {
    int32 id = 1;
    string full_name = 2;
    string phone_number = 3;
    string license_number = 4;
    int32 max_working_hours_per_day = 5;
    string status = 6;
}

message DriverListReply { repeated DriverReply drivers = 1; }
```

---

### 4.4 AssignmentService (Port 6004 / gRPC 7004)

#### API Endpoints
```
Base: http://localhost:5050/assignment/

GET    /api/Assignments/recommend/{deliveryId}   Get vehicle+driver recommendations
POST   /api/Assignments                          Create assignment
GET    /api/Assignments                          List assignments
GET    /api/Assignments/{id}                     Get assignment by ID
DELETE /api/Assignments/{id}                     Cancel assignment
```

#### gRPC Clients Used
```csharp
// AssignmentService calls three services via gRPC
services.AddGrpcClient<DeliveryGrpc.DeliveryGrpcClient>(o =>
    o.Address = new Uri(config["GrpcUrls:DeliveryService"])); // http://localhost:7001

services.AddGrpcClient<VehicleGrpc.VehicleGrpcClient>(o =>
    o.Address = new Uri(config["GrpcUrls:VehicleService"])); // http://localhost:7002

services.AddGrpcClient<DriverGrpc.DriverGrpcClient>(o =>
    o.Address = new Uri(config["GrpcUrls:DriverService"]));  // http://localhost:7003
```

#### Assignment Engine Logic
```csharp
public async Task<List<AssignmentRecommendation>> GetRecommendationsAsync(int deliveryId)
{
    // 1. Verify delivery exists via gRPC
    var delivery = await _deliveryClient.GetDeliveryAsync(new GetDeliveryRequest { Id = deliveryId });

    // 2. Get available vehicles via gRPC
    var vehicles = await _vehicleClient.GetAvailableVehiclesAsync(new Empty());

    // 3. Get available drivers via gRPC
    var drivers = await _driverClient.GetAvailableDriversAsync(new Empty());

    // 4. Score and rank vehicle-driver pairs
    return Score(vehicles.Vehicles, drivers.Drivers, delivery);
}
```

---

### 4.5 RouteService (Port 6005)

#### API Endpoints
```
Base: http://localhost:5050/route/

POST   /api/Routes/optimize              Get optimized routes
GET    /api/Routes/{deliveryId}          Get saved routes for delivery
POST   /api/Routes/{deliveryId}/select   Select optimal route
POST   /api/Fuel/calculate               Calculate fuel cost
```

#### Redis Caching
- Route results cached for **24 hours** by `origin:destination` key
- Distance matrix results cached for **24 hours**

#### gRPC Client Used
```csharp
// RouteService calls VehicleService for fuel efficiency data
services.AddGrpcClient<VehicleGrpc.VehicleGrpcClient>(o =>
    o.Address = new Uri(config["GrpcUrls:VehicleService"])); // http://localhost:7002
```

---

### 4.6 TrackingService (Port 6006)

#### API Endpoints & SignalR Hub
```
Base: http://localhost:5050/tracking/

WS   /hubs/tracking              SignalR WebSocket hub (proxied by Ocelot)
GET  /api/Tracking/{driverId}    Get location history for driver
```

#### SignalR Hub
```csharp
[Authorize]
public class TrackingHub : Hub
{
    // Client → Server
    public async Task UpdateLocation(int driverId, decimal lat, decimal lng) { ... }
    public async Task UpdateDeliveryStatus(int deliveryId, string status) { ... }

    // Server → Client events
    // "ReceiveLocationUpdate" → broadcasted to all connected clients
    // "ReceiveStatusUpdate"   → broadcasted to all connected clients
}
```

---

### 4.7 ApiGateway (Port 5050)

#### Technology
- **Ocelot** on ASP.NET Core (.NET 10)
- **Authentication:** WSO2 Bearer JWT validation via JWKS endpoint (`https://localhost:9443/oauth2/jwks`)
- **CORS:** Allows `http://localhost:3000` (frontend dev server)

#### Route Table (ocelot.json)

| Upstream (5050) | Downstream Service | Port | Auth |
|---|---|---|---|
| `/delivery/{everything}` | DeliveryService (localhost) | 6001 | WSO2Bearer |
| `/vehicle/{everything}` | VehicleService (delivery-service in Docker) | 6002 | WSO2Bearer |
| `/driver/{everything}` | DriverService | 6003 | WSO2Bearer |
| `/assignment/{everything}` | AssignmentService | 6004 | WSO2Bearer |
| `/route/{everything}` | RouteService | 6005 | WSO2Bearer |
| `/tracking/{everything}` | TrackingService | 6006 | WSO2Bearer |
| `/hubs/tracking` | TrackingService (WebSocket) | 6006 | WSO2Bearer |

> [!NOTE]
> For local `dotnet run` development, downstream hosts are `localhost`. In Docker, they use Docker DNS service names (`delivery-service`, `vehicle-service`, etc.).

---

## 5. Database Architecture

### 5.1 Database Strategy
- **Pattern:** Database per Microservice
- **Engine:** SQL Server 2022 (single instance, separate databases per service)
- **Migration Tool:** DbUp (SQL scripts as embedded resources)

### 5.2 Database Instances

| Service | Database Name | Dev Connection |
|---|---|---|
| DeliveryService | `meridian_delivery` | `Server=localhost,1433;Database=meridian_delivery;` |
| VehicleService | `meridian_vehicle` | `Server=localhost,1433;Database=meridian_vehicle;` |
| DriverService | `meridian_driver` | `Server=localhost,1433;Database=meridian_driver;` |
| AssignmentService | `meridian_assignment` | `Server=localhost,1433;Database=meridian_assignment;` |
| TrackingService | `meridian_tracking` | `Server=localhost,1433;Database=meridian_tracking;` |
| RouteService | — | Redis-only (route cache) |

**SQL Server credentials (docker-compose dev):**
```
SA Password: Passw0rd!
Port: 1433
```

### 5.3 Redis Cache

| Environment | Instance | Purpose |
|---|---|---|
| Development | `localhost:6379` | Google Maps route cache, route optimization results |
| Production | Azure Cache for Redis | Distributed cache |

### 5.4 Migration Scripts Structure

```
src/
├── DeliveryService/DeliveryService.API/Migrations/
│   ├── 001_create_deliveries_table.sql
│   └── 002_create_status_history_table.sql
├── VehicleService/VehicleService.API/Migrations/
│   └── 001_create_vehicles_table.sql
├── DriverService/DriverService.API/Migrations/
│   └── 001_create_drivers_table.sql
├── AssignmentService/AssignmentService.API/Migrations/
│   └── 001_create_assignments_table.sql
└── TrackingService/TrackingService.API/Migrations/
    └── 001_create_location_updates_table.sql
```

---

## 6. API Design & Inter-Service Communication

### 6.1 RESTful API Standards (External — via Gateway)

**HTTP Methods:** `GET`, `POST`, `PUT`, `DELETE`

**Status Codes:**
- `200 OK` — Successful GET, PUT
- `201 Created` — Successful POST
- `204 No Content` — Successful DELETE
- `400 Bad Request` — Validation errors
- `401 Unauthorized` — Missing/invalid WSO2 JWT
- `403 Forbidden` — Valid JWT but insufficient role
- `404 Not Found` — Resource doesn't exist
- `409 Conflict` — Business rule violation (e.g. vehicle already assigned)
- `500 Internal Server Error` — Unhandled exceptions

### 6.2 gRPC Inter-Service Communication

**Why gRPC over HTTP/REST for internal calls:**
- Binary Protobuf serialization — significantly faster than JSON
- Strongly typed contracts via `.proto` files — compile-time safety
- Auto-generated clients — no `HttpClient` boilerplate or JSON parsing
- Contract-first — `.proto` file is the agreed interface between services

**Project Setup per service (caller side):**

```xml
<!-- Add to .csproj of the calling service -->
<ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />
    <PackageReference Include="Grpc.Net.Client" Version="2.67.0" />
    <PackageReference Include="Google.Protobuf" Version="3.29.3" />
    <PackageReference Include="Grpc.Tools" Version="2.67.0" PrivateAssets="All" />
</ItemGroup>

<!-- Reference .proto file (shared or copied) -->
<ItemGroup>
    <Protobuf Include="Protos\delivery.proto" GrpcServices="Client" />
</ItemGroup>
```

**Project Setup per service (server side):**

```xml
<!-- Add to .csproj of the providing service -->
<ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />
</ItemGroup>

<ItemGroup>
    <Protobuf Include="Protos\delivery.proto" GrpcServices="Server" />
</ItemGroup>
```

**Registering the gRPC server (Program.cs of provider):**

```csharp
builder.Services.AddGrpc();

// ...

app.MapGrpcService<DeliveryGrpcService>(); // the service class implementing the proto contract
```

**Registering gRPC clients (Program.cs of consumer):**

```csharp
// AssignmentService Program.cs
builder.Services.AddGrpcClient<DeliveryGrpc.DeliveryGrpcClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcUrls:DeliveryService"]);
});
```

**appsettings.Development.json (AssignmentService example):**
```json
{
  "GrpcUrls": {
    "DeliveryService": "http://localhost:7001",
    "VehicleService": "http://localhost:7002",
    "DriverService": "http://localhost:7003"
  }
}
```

> [!IMPORTANT]
> gRPC requires HTTP/2. For local development without TLS, use `GrpcChannelOptions` with `HttpHandler` configured to allow non-TLS HTTP/2, or use `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)`.

**Example gRPC call (inside AssignmentService):**

```csharp
public class AssignmentManagerService : IAssignmentManagerService
{
    private readonly DeliveryGrpc.DeliveryGrpcClient _deliveryClient;
    private readonly VehicleGrpc.VehicleGrpcClient _vehicleClient;

    public AssignmentManagerService(
        DeliveryGrpc.DeliveryGrpcClient deliveryClient,
        VehicleGrpc.VehicleGrpcClient vehicleClient)
    {
        _deliveryClient = deliveryClient;
        _vehicleClient = vehicleClient;
    }

    public async Task<List<AssignmentRecommendation>> GetRecommendationsAsync(int deliveryId)
    {
        var delivery = await _deliveryClient.GetDeliveryAsync(
            new GetDeliveryRequest { Id = deliveryId });

        if (delivery is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Delivery {deliveryId} not found"));

        var vehicles = await _vehicleClient.GetAvailableVehiclesAsync(new Empty());

        return ScoreAndRank(vehicles.Vehicles, delivery);
    }
}
```

### 6.3 API Documentation

Each microservice exposes Swagger/OpenAPI in Development and QA environments:

```
http://localhost:6001/swagger   → DeliveryService
http://localhost:6002/swagger   → VehicleService
http://localhost:6003/swagger   → DriverService
http://localhost:6004/swagger   → AssignmentService
http://localhost:6005/swagger   → RouteService
```

---

## 7. Authentication & Security

### 7.1 WSO2 Identity Server (IAM)

**Version:** WSO2 IS 7.2.0
**Docker ports:** `9443` (HTTPS management/OIDC), `9763` (HTTP)
**Role model:** Admin, Dispatcher, Driver

**Flow:**
1. User authenticates against WSO2 IS (`POST /oauth2/token`) and receives a JWT access token
2. Frontend sends JWT as `Authorization: Bearer <token>` header with every request
3. **Ocelot gateway** validates the JWT against WSO2 JWKS (`https://localhost:9443/oauth2/jwks`)
4. If valid, Ocelot forwards the request to the downstream service
5. Downstream services **do not** validate tokens independently — trust is established at the gateway

```csharp
// ApiGateway Program.cs — WSO2 JWT validation
builder.Services.AddAuthentication()
    .AddJwtBearer("WSO2Bearer", options =>
    {
        options.Authority = "https://localhost:9443/oauth2/token";
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            // Accept WSO2 self-signed cert in dev
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        options.Configuration = new OpenIdConnectConfiguration
        {
            JwksUri = "https://localhost:9443/oauth2/jwks"
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = "YOUR_WSO2_AUDIENCE",
            ValidateLifetime = true,
        };
    });
```

### 7.2 Role-Based Access (enforced at Gateway via Ocelot + WSO2 scopes)

| Role | Allowed Operations |
|---|---|
| **Admin** | Full access to all services |
| **Dispatcher** | Create/view deliveries, manage assignments, view fleet |
| **Driver** | View own assignments, update location/status via TrackingService |

### 7.3 Security Best Practices

1. **HTTPS enforced** in all non-dev environments
2. **CORS** configured in API Gateway: allows `http://localhost:3000` in dev
3. **SQL Injection prevention:** Parameterized queries with ADO.NET
4. **Secrets:** Azure Key Vault in production; `appsettings.Development.json` for local (not committed)
5. **WSO2 self-signed cert:** Bypassed with `DangerousAcceptAnyServerCertificateValidator` in dev only
6. **gRPC channels** use unencrypted HTTP/2 in dev; TLS required in production

---

## 8. Real-Time Communication

### 8.1 SignalR Architecture

**Service:** TrackingService (Port 6006)
**Hub URL:** `ws://localhost:5050/hubs/tracking` (via Ocelot WebSocket proxy)

**Client → Server Methods:**
- `UpdateLocation(driverId, latitude, longitude)` — Driver sends GPS coordinates
- `UpdateDeliveryStatus(deliveryId, status)` — Driver updates delivery status

**Server → Client Events:**
- `ReceiveLocationUpdate(driverId, latitude, longitude, timestamp)`
- `ReceiveStatusUpdate(deliveryId, status, timestamp)`

### 8.2 Frontend Integration

```typescript
// meridian-frontend — trackingService.ts
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5050/hubs/tracking', {
        accessTokenFactory: () => localStorage.getItem('token') ?? ''
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();

connection.on('ReceiveLocationUpdate', (data) => {
    // Update driver marker on map
});

await connection.start();
```

---

## 9. External Integrations

### 9.1 Google Maps APIs

**Used by:** RouteService (Port 6005)

**APIs:**
1. **Directions API** — Get route options between pickup and delivery
2. **Distance Matrix API** — Calculate distance between vehicle and pickup location

**Caching:** All Google Maps responses cached in Redis for 24 hours to minimize API calls.

**Configuration:**
```json
{
  "GoogleMaps": {
    "ApiKey": "${GOOGLE_MAPS_API_KEY}",
    "CacheDurationHours": 24
  }
}
```

---

## 10. Frontend Architecture

**Framework:** Next.js (App Router)
**Location:** `/meridian-frontend`
**Dev Server:** `http://localhost:3000`

**Key pages (planned):**
- Dashboard — delivery overview
- Deliveries — CRUD, status tracking
- Fleet — vehicle and driver management
- Assignments — recommendation and assignment workflow
- Live Tracking — SignalR-connected map

**API communication:**
- All REST calls go through `http://localhost:5050` (API Gateway) in dev
- SignalR connects to `ws://localhost:5050/hubs/tracking`
- Bearer token from WSO2 IS attached to every request

---

## 11. Development Environment Setup

### 11.1 Prerequisites

- .NET SDK 10.0+
- Node.js 20+
- Docker Desktop
- Git

### 11.2 Start Infrastructure (Docker)

```bash
# From repo root — starts SQL Server, Redis, WSO2 IS
docker compose up -d
```

| Service | URL |
|---|---|
| SQL Server | `localhost:1433` (sa / Passw0rd!) |
| Redis | `localhost:6379` |
| WSO2 IS | `https://localhost:9443` (admin / admin) |

### 11.3 Start Microservices (local dotnet run)

```bash
# Each in a separate terminal
cd src/ApiGateway              && dotnet run   # :5050
cd src/DeliveryService/DeliveryService.API    && dotnet run   # :6001
cd src/VehicleService/VehicleService.API      && dotnet run   # :6002
cd src/DriverService/DriverService.API        && dotnet run   # :6003
cd src/AssignmentService/AssignmentService.API && dotnet run  # :6004
cd src/RouteService/RouteService.API          && dotnet run   # :6005
cd src/TrackingService/TrackingService.API    && dotnet run   # :6006
```

> [!NOTE]
> When running locally with `dotnet run`, set downstream hosts in `ocelot.json` to `localhost`. In Docker, use Docker service DNS names (e.g. `delivery-service`).

### 11.4 Start Frontend

```bash
cd meridian-frontend
npm install
npm run dev   # http://localhost:3000
```

### 11.5 Port Reference (Complete)

| Service | HTTP/REST | gRPC |
|---|---|---|
| ApiGateway | 5050 | — |
| DeliveryService | 6001 | 7001 |
| VehicleService | 6002 | 7002 |
| DriverService | 6003 | 7003 |
| AssignmentService | 6004 | 7004 |
| RouteService | 6005 | 7005 |
| TrackingService | 6006 | — |
| SQL Server | 1433 | — |
| Redis | 6379 | — |
| WSO2 IS (HTTPS) | 9443 | — |
| WSO2 IS (HTTP) | 9763 | — |
| Next.js Frontend | 3000 | — |

---

## 12. CI/CD Pipeline

**Platform:** GitHub Actions (`.github/workflows/`)

### Branch Strategy
```
main        → Production deployments
develop     → Integration branch (all PRs merge here first)
feature/*   → Feature branches
fix/*       → Bug fix branches
```

### Pipeline Stages
1. **Build & Test** — `dotnet build`, `dotnet test`
2. **Docker Build** — Build service images
3. **Push to Registry** — Push to Docker Hub / Azure Container Registry
4. **Deploy** — Deploy to Azure App Service (per-service)

---

## 13. Cloud Deployment Architecture

**Platform:** Microsoft Azure

| Component | Azure Service |
|---|---|
| Microservices | Azure App Service (containerized) |
| Frontend | Azure Static Web Apps |
| Database | Azure SQL Database |
| Cache | Azure Cache for Redis |
| Secrets | Azure Key Vault |
| Monitoring | Azure Application Insights |
| Container Registry | Azure Container Registry |
| IAM | WSO2 IS (hosted on Azure VM) or Azure AD B2C |

---

## 14. Project Structure

```
Meridian/
├── docker-compose.yml              # Infrastructure: SQL Server, Redis, WSO2 IS
├── meridian-frontend/              # Next.js frontend
│   └── app/
│       └── page.tsx                # API Gateway tester (dev tool)
└── Meridian-Backend/
    ├── docs/
    │   ├── MERIDIAN_ARCHITECTURE_v2.md   ← THIS FILE
    │   ├── API_GATEWAY.md
    │   ├── AZURE_DEPLOYMENT_GUIDE.md
    │   └── Contribution-Guidelines.md
    └── src/
        ├── ApiGateway/
        │   ├── ocelot.json             # Route table, auth config
        │   ├── Program.cs              # WSO2 JWT setup, CORS
        │   └── Properties/launchSettings.json  # Port 5050
        ├── DeliveryService/
        │   └── DeliveryService.API/
        │       ├── Controllers/
        │       ├── Services/
        │       ├── Repositories/
        │       ├── Models/
        │       ├── DTOs/
        │       ├── Protos/delivery.proto
        │       └── Migrations/
        ├── VehicleService/
        │   └── VehicleService.API/
        │       ├── Controllers/, Services/, Repositories/, Models/, DTOs/
        │       ├── Protos/vehicle.proto
        │       └── Migrations/
        ├── DriverService/
        │   └── DriverService.API/
        │       ├── Controllers/, Services/, Repositories/, Models/, DTOs/
        │       ├── Protos/driver.proto
        │       └── Migrations/
        ├── AssignmentService/
        │   └── AssignmentService.API/
        │       ├── Controllers/, Services/, Repositories/, Models/, DTOs/
        │       └── Migrations/
        ├── RouteService/
        │   └── RouteService.API/
        │       ├── Controllers/, Services/
        │       └── Protos/
        └── TrackingService/
            └── TrackingService.API/
                ├── Hubs/TrackingHub.cs
                ├── Controllers/, Repositories/, Models/
                └── Migrations/
```
