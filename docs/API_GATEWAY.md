# API Gateway

The API Gateway is the single entry point for the Meridian frontend. The React app never calls any microservice directly — every request goes through the gateway first.

**Port:** `http://localhost:5050`

---

## How it works

```
React (3000)  →  API Gateway (5050)  →  Microservice (6001–6007 locally, 8080 in Azure containers)
```

The gateway does two things:
1. **Validates the JWT token** from WSO2 IS before forwarding any request
2. **Routes the request** to the correct microservice based on the URL path

---

## URL structure

| Frontend calls | Gateway forwards to |
|---------------|---------------------|
| `http://localhost:5050/delivery/...` | Delivery Service — port 6001 |
| `http://localhost:5050/vehicle/...` | Vehicle Service — port 6002 |
| `http://localhost:5050/driver/...` | Driver Service — port 6003 |
| `http://localhost:5050/assignment/...` | Assignment Service — port 6004 |
| `http://localhost:5050/route/...` | Route Service — port 6005 |
| `http://localhost:5050/tracking/...` | Tracking Service — port 6006 |
| `http://localhost:5050/user/swagger` | User Service Swagger — port 6007 |
| `http://localhost:5050/hubs/tracking` | Tracking SignalR Hub (WebSocket) |

The path after the prefix is forwarded as-is. Example:

```
GET /delivery/api/deliveries  →  GET http://delivery-service:6001/api/deliveries
```

---

## Authentication

Protected requests must include a JWT token in the `Authorization` header:

```
Authorization: Bearer <your_token>
```

Tokens are issued by `UserService` and validated by the gateway using the shared symmetric JWT settings.  
If the token is missing or invalid on a protected route, the gateway returns `401 Unauthorized`.

---

## Running locally

Start infrastructure first (SQL, Redis, WSO2 IS):
```bash
docker-compose up -d   # run from Meridian/ root
```

Then run the gateway:
```bash
cd Meridian-Backend/src/ApiGateway
dotnet run
```

Then run whichever microservices you need with `dotnet run` in separate terminals.

---

## For frontend developers

- **All frontend API calls must go to `http://localhost:5050`** — never call a microservice port directly
- Include the JWT token on every protected request
- For SignalR, connect to `http://localhost:5050/hubs/tracking` with the same JWT token
- The gateway returns `401` if token is expired or missing — handle this in your auth layer and refresh the token
- The gateway does not host its own combined Swagger UI, but it proxies downstream Swagger endpoints:
  - `/delivery/swagger`
  - `/vehicle/swagger`
  - `/driver/swagger`
  - `/assignment/swagger`
  - `/route/swagger`
  - `/tracking/swagger`
  - `/user/swagger`
