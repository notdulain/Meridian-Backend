# Meridian API Endpoint Inventory

Endpoint catalog for **MER-339**: document all HTTP endpoints across the API Gateway and all 7 microservices.

This document lists implemented HTTP endpoints and gateway route mappings only.

- Request/response schemas with sample payloads: `docs/api-schemas-and-examples.md` (**MER-340**)
- Per-endpoint authentication requirements: `docs/api-auth-requirements.md` (**MER-341**)

---

## 1. Base URLs

- API Gateway (development): `http://localhost:5050`
- Direct service URLs (development):
  - UserService: `http://localhost:6007`
  - DeliveryService: `http://localhost:6001`
  - VehicleService: `http://localhost:6002`
  - DriverService: `http://localhost:6003`
  - AssignmentService: `http://localhost:6004`
  - RouteService: `http://localhost:6005`
  - TrackingService: `http://localhost:6006`

---

## 2. API Gateway Endpoints

### 2.1 Gateway-native endpoints

| Method | Gateway path | Purpose |
|---|---|---|
| GET | `/` | Health probe response (`Meridian API Gateway is running.`) |
| GET | `/diagnostics` | Connectivity diagnostic check to downstream Delivery swagger |
| GET | `/api/dashboard/summary` | Aggregated dashboard summary from Delivery/Vehicle/Driver/Assignment services |

### 2.2 Gateway proxy route groups (Ocelot)

| Upstream route group | Methods | Downstream target |
|---|---|---|
| `/delivery/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6001/{everything}` |
| `/vehicle/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6002/{everything}` |
| `/driver/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6003/{everything}` |
| `/assignment/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6004/{everything}` |
| `/route/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6005/{everything}` |
| `/tracking/{everything}` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `http://localhost:6006/{everything}` |
| `/hubs/tracking` | GET, POST, PUT, PATCH, DELETE, OPTIONS | `ws://localhost:6006/hubs/tracking` |
| `/api/auth/{everything}` | POST | `http://localhost:6007/api/auth/{everything}` |
| `/api/users/{everything}` | GET, POST, PUT, DELETE | `http://localhost:6007/api/users/{everything}` |
| `/api/roles/{everything}` | GET | `http://localhost:6007/api/roles/{everything}` |

### 2.3 Gateway swagger proxy endpoints

| Method | Gateway path |
|---|---|
| GET | `/delivery/swagger` |
| GET | `/delivery/swagger/{everything}` |
| GET | `/vehicle/swagger` |
| GET | `/vehicle/swagger/{everything}` |
| GET | `/driver/swagger` |
| GET | `/driver/swagger/{everything}` |
| GET | `/assignment/swagger` |
| GET | `/assignment/swagger/{everything}` |
| GET | `/route/swagger` |
| GET | `/route/swagger/{everything}` |
| GET | `/tracking/swagger` |
| GET | `/tracking/swagger/{everything}` |
| GET | `/user/swagger` |
| GET | `/user/swagger/{everything}` |

---

## 3. UserService Endpoints

Direct base: `http://localhost:6007`  
Gateway entry: same path for `/api/auth/*`, `/api/users/*`, `/api/roles/*` (no `/user` prefix for API routes)

| Method | Service path |
|---|---|
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| POST | `/api/auth/refresh` |
| POST | `/api/auth/revoke` |
| POST | `/api/auth/logout` |
| POST | `/api/users/driver-accounts` |
| GET | `/api/users` |
| GET | `/api/users/{id}` |
| GET | `/api/users/me` |
| PUT | `/api/users/{id}` |
| DELETE | `/api/users/{id}` |
| GET | `/api/roles` |
| GET | `/api/roles/me` |

---

## 4. DeliveryService Endpoints

Direct base: `http://localhost:6001`  
Gateway prefix: `/delivery`

| Method | Service path | Gateway path |
|---|---|---|
| GET | `/api/deliveries` | `/delivery/api/deliveries` |
| POST | `/api/deliveries` | `/delivery/api/deliveries` |
| GET | `/api/deliveries/{id}` | `/delivery/api/deliveries/{id}` |
| PUT | `/api/deliveries/{id}` | `/delivery/api/deliveries/{id}` |
| DELETE | `/api/deliveries/{id}` | `/delivery/api/deliveries/{id}` |
| GET | `/api/deliveries/{id}/recommend-vehicles` | `/delivery/api/deliveries/{id}/recommend-vehicles` |
| GET | `/api/reports/delivery-success` | `/delivery/api/reports/delivery-success` |
| GET | `/api/reports/delivery-success/csv` | `/delivery/api/reports/delivery-success/csv` |
| GET | `/api/reports/delivery-trends` | `/delivery/api/reports/delivery-trends` |
| GET | `/api/reports/delivery-trends/csv` | `/delivery/api/reports/delivery-trends/csv` |

---

## 5. VehicleService Endpoints

Direct base: `http://localhost:6002`  
Gateway prefix: `/vehicle`

| Method | Service path | Gateway path |
|---|---|---|
| POST | `/api/vehicles` | `/vehicle/api/vehicles` |
| GET | `/api/vehicles` | `/vehicle/api/vehicles` |
| GET | `/api/vehicles/{id}` | `/vehicle/api/vehicles/{id}` |
| PUT | `/api/vehicles/{id}` | `/vehicle/api/vehicles/{id}` |
| PUT | `/api/vehicles/{id}/status` | `/vehicle/api/vehicles/{id}/status` |
| DELETE | `/api/vehicles/{id}` | `/vehicle/api/vehicles/{id}` |
| GET | `/api/vehicles/available` | `/vehicle/api/vehicles/available` |
| GET | `/api/reports/vehicle-utilization` | `/vehicle/api/reports/vehicle-utilization` |
| GET | `/api/reports/vehicle-utilization/csv` | `/vehicle/api/reports/vehicle-utilization/csv` |

---

## 6. DriverService Endpoints

Direct base: `http://localhost:6003`  
Gateway prefix: `/driver`

| Method | Service path | Gateway path |
|---|---|---|
| POST | `/api/drivers` | `/driver/api/drivers` |
| GET | `/api/drivers` | `/driver/api/drivers` |
| GET | `/api/drivers/deleted` | `/driver/api/drivers/deleted` |
| GET | `/api/drivers/{id}` | `/driver/api/drivers/{id}` |
| GET | `/api/drivers/me` | `/driver/api/drivers/me` |
| PUT | `/api/drivers/{id}` | `/driver/api/drivers/{id}` |
| PUT | `/api/drivers/{id}/hours` | `/driver/api/drivers/{id}/hours` |
| DELETE | `/api/drivers/{id}` | `/driver/api/drivers/{id}` |
| GET | `/api/drivers/available` | `/driver/api/drivers/available` |
| GET | `/api/reports/driver-performance` | `/driver/api/reports/driver-performance` |
| GET | `/api/reports/driver-performance/csv` | `/driver/api/reports/driver-performance/csv` |

---

## 7. AssignmentService Endpoints

Direct base: `http://localhost:6004`  
Gateway prefix: `/assignment`

| Method | Service path | Gateway path |
|---|---|---|
| POST | `/api/assignments` | `/assignment/api/assignments` |
| GET | `/api/assignments/history` | `/assignment/api/assignments/history` |
| GET | `/api/assignments` | `/assignment/api/assignments` |
| GET | `/api/assignments/{id}` | `/assignment/api/assignments/{id}` |
| GET | `/api/assignments/delivery/{deliveryId}` | `/assignment/api/assignments/delivery/{deliveryId}` |
| GET | `/api/assignments/driver/{driverId}/active` | `/assignment/api/assignments/driver/{driverId}/active` |
| PUT | `/api/assignments/{id}/complete` | `/assignment/api/assignments/{id}/complete` |
| PUT | `/api/assignments/{id}/cancel` | `/assignment/api/assignments/{id}/cancel` |

---

## 8. RouteService Endpoints

Direct base: `http://localhost:6005`  
Gateway prefix: `/route`

| Method | Service path | Gateway path |
|---|---|---|
| POST | `/api/routes/optimize` | `/route/api/routes/optimize` |
| GET | `/api/routes/calculate` | `/route/api/routes/calculate` |
| GET | `/api/routes/alternatives` | `/route/api/routes/alternatives` |
| POST | `/api/routes/select` | `/route/api/routes/select` |
| GET | `/api/routes/history` | `/route/api/routes/history` |
| GET | `/api/routes/compare` | `/route/api/routes/compare` |
| GET | `/api/routes/rank` | `/route/api/routes/rank` |
| GET | `/api/reports/fuel-cost` | `/route/api/reports/fuel-cost` |
| GET | `/api/reports/fuel-cost/csv` | `/route/api/reports/fuel-cost/csv` |

---

## 9. TrackingService Endpoints

Direct base: `http://localhost:6006`  
Gateway prefix: `/tracking`

| Method | Service path | Gateway path |
|---|---|---|
| POST | `/api/tracking/location` | `/tracking/api/tracking/location` |
| GET | `/api/tracking/assignment/{assignmentId}/history` | `/tracking/api/tracking/assignment/{assignmentId}/history` |
| GET | `/api/tracking/driver/{driverId}/last-known` | `/tracking/api/tracking/driver/{driverId}/last-known` |
| WS | `/hubs/tracking` | `/hubs/tracking` |

---

## 10. Notes and Source References

- Gateway route source: `src/ApiGateway/ocelot.Development.json`
- Gateway-native endpoints source: `src/ApiGateway/Program.cs`
- Service endpoints source: all controller classes under `src/*/*/Controllers/`
- This inventory reflects current controller and Ocelot configuration in the repository.
