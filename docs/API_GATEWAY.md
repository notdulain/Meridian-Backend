# API Gateway

The API Gateway is the single entry point for the Meridian frontend. The React app never calls any microservice directly — every request goes through the gateway first.

**Port:** `http://localhost:6000`

---

## How it works

```
React (3000)  →  API Gateway (6000)  →  Microservice (6001–6006)
```

The gateway does two things:
1. **Validates the JWT token** from WSO2 IS before forwarding any request
2. **Routes the request** to the correct microservice based on the URL path

---

## URL structure

| Frontend calls | Gateway forwards to |
|---------------|---------------------|
| `http://localhost:6000/delivery/...` | Delivery Service — port 6001 |
| `http://localhost:6000/vehicle/...` | Vehicle Service — port 6002 |
| `http://localhost:6000/driver/...` | Driver Service — port 6003 |
| `http://localhost:6000/assignment/...` | Assignment Service — port 6004 |
| `http://localhost:6000/route/...` | Route Service — port 6005 |
| `http://localhost:6000/tracking/...` | Tracking Service — port 6006 |
| `http://localhost:6000/hubs/tracking` | Tracking SignalR Hub (WebSocket) |

The path after the prefix is forwarded as-is. Example:

```
GET /delivery/api/deliveries  →  GET http://delivery-service:6001/api/deliveries
```

---

## Authentication

Every request must include a JWT token in the `Authorization` header:

```
Authorization: Bearer <your_token>
```

Tokens are issued by WSO2 Identity Server running at `https://localhost:9443`.  
If the token is missing or invalid, the gateway returns `401 Unauthorized`.

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

- **All API calls must go to `http://localhost:6000`** — never call a microservice port directly
- Include the JWT token on every request (except login/WSO2 endpoints which are separate)
- For SignalR, connect to `http://localhost:6000/hubs/tracking` with the same JWT token
- The gateway returns `401` if token is expired or missing — handle this in your auth layer and refresh the token
- No Swagger UI exists on the gateway — refer to individual service docs for endpoint details
