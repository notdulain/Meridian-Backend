# Meridian API Authentication Requirements

Reference for **MER-341**: authentication and role requirements per endpoint.

Use this with:
- `docs/api-endpoints.md` for route inventory
- `docs/api-schemas-and-examples.md` for payload examples

---

## 1) Gateway-level policy

### 1.1 Gateway-native endpoints

| Method | Gateway endpoint | Auth | Roles | Enforced at |
|---|---|---|---|---|
| GET | `/` | Public | - | Gateway |
| GET | `/diagnostics` | Public | - | Gateway |
| GET | `/api/dashboard/summary` | JWT required | `Admin`, `Dispatcher` | Gateway middleware |

### 1.2 Ocelot route groups

| Upstream route group | Gateway JWT (`MeridianBearer`) |
|---|---|
| `/delivery/{everything}` | No (service still enforces `[Authorize]`) |
| `/vehicle/{everything}` | Yes |
| `/driver/{everything}` | Yes |
| `/assignment/{everything}` | Yes |
| `/route/{everything}` | Yes |
| `/tracking/{everything}` | Yes |
| `/hubs/tracking` | Yes |
| `/api/auth/{everything}` | No |
| `/api/users/{everything}` | Yes |
| `/api/roles/{everything}` | Yes |

> Note: gateway authentication and service authentication can both apply. A request must pass both layers when both are configured.

---

## 2) UserService (`/api/auth`, `/api/users`, `/api/roles`)

| Method | Endpoint | Auth | Roles | Notes |
|---|---|---|---|---|
| POST | `/api/auth/register` | Public | - | Account registration |
| POST | `/api/auth/login` | Public | - | Token issuance |
| POST | `/api/auth/refresh` | Public | - | Token refresh by refresh token |
| POST | `/api/auth/revoke` | JWT required | Any authenticated user | Revokes provided refresh token |
| POST | `/api/auth/logout` | JWT required | Any authenticated user | Alias of revoke |
| POST | `/api/users/driver-accounts` | JWT required | `Admin` | Creates both user + driver profile |
| GET | `/api/users` | JWT required | `Admin` | List users |
| GET | `/api/users/{id}` | JWT required | Self or `Admin` | Non-admin can only read own id |
| GET | `/api/users/me` | JWT required | Any authenticated user | Current user profile |
| PUT | `/api/users/{id}` | JWT required | `Admin` | Update user |
| DELETE | `/api/users/{id}` | JWT required | `Admin` | Soft delete |
| GET | `/api/roles` | JWT required | `Admin` | Role list |
| GET | `/api/roles/me` | JWT required | Any authenticated user | Current role claim |

---

## 3) DeliveryService (`/delivery/api/...` via gateway)

| Method | Endpoint | Auth | Roles | Notes |
|---|---|---|---|---|
| GET | `/delivery/api/deliveries` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| POST | `/delivery/api/deliveries` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| GET | `/delivery/api/deliveries/{id}` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| PUT | `/delivery/api/deliveries/{id}` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| DELETE | `/delivery/api/deliveries/{id}` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| GET | `/delivery/api/deliveries/{id}/recommend-vehicles` | JWT required | `Admin`, `Dispatcher` | Controller-level role guard |
| GET | `/delivery/api/reports/delivery-success` | JWT required | `Admin`, `Dispatcher`, `Manager` | Report controller guard |
| GET | `/delivery/api/reports/delivery-success/csv` | JWT required | `Admin`, `Dispatcher`, `Manager` | Report controller guard |
| GET | `/delivery/api/reports/delivery-trends` | JWT required | `Admin`, `Dispatcher`, `Manager` | Report controller guard |
| GET | `/delivery/api/reports/delivery-trends/csv` | JWT required | `Admin`, `Dispatcher`, `Manager` | Report controller guard |

---

## 4) VehicleService (`/vehicle/api/...` via gateway)

| Method | Endpoint | Auth | Roles |
|---|---|---|---|
| POST | `/vehicle/api/vehicles` | JWT required | `Admin` |
| GET | `/vehicle/api/vehicles` | JWT required | `Admin`, `Dispatcher` |
| GET | `/vehicle/api/vehicles/{id}` | JWT required | `Admin`, `Dispatcher` |
| PUT | `/vehicle/api/vehicles/{id}` | JWT required | `Admin` |
| PUT | `/vehicle/api/vehicles/{id}/status` | JWT required | `Admin`, `Dispatcher` |
| DELETE | `/vehicle/api/vehicles/{id}` | JWT required | `Admin` |
| GET | `/vehicle/api/vehicles/available` | JWT required | `Admin`, `Dispatcher`, `Driver` |
| GET | `/vehicle/api/reports/vehicle-utilization` | JWT required | `Admin`, `Dispatcher` |
| GET | `/vehicle/api/reports/vehicle-utilization/csv` | JWT required | `Admin`, `Dispatcher`, `Manager` |

---

## 5) DriverService (`/driver/api/...` via gateway)

| Method | Endpoint | Auth | Roles |
|---|---|---|---|
| POST | `/driver/api/drivers` | JWT required | `Admin` |
| GET | `/driver/api/drivers` | JWT required | `Admin`, `Dispatcher` |
| GET | `/driver/api/drivers/deleted` | JWT required | `Admin` |
| GET | `/driver/api/drivers/{id}` | JWT required | `Admin`, `Dispatcher` |
| GET | `/driver/api/drivers/me` | JWT required | `Driver` |
| PUT | `/driver/api/drivers/{id}` | JWT required | `Admin` |
| PUT | `/driver/api/drivers/{id}/hours` | JWT required | `Admin`, `Dispatcher` |
| DELETE | `/driver/api/drivers/{id}` | JWT required | `Admin` |
| GET | `/driver/api/drivers/available` | JWT required | `Admin`, `Dispatcher` |
| GET | `/driver/api/reports/driver-performance` | JWT required | `Admin`, `Dispatcher` |
| GET | `/driver/api/reports/driver-performance/csv` | JWT required | `Admin`, `Dispatcher`, `Manager` |

---

## 6) AssignmentService (`/assignment/api/...` via gateway)

| Method | Endpoint | Auth | Roles |
|---|---|---|---|
| POST | `/assignment/api/assignments` | JWT required | `Admin`, `Dispatcher` |
| GET | `/assignment/api/assignments/history` | JWT required | `Admin`, `Dispatcher` |
| GET | `/assignment/api/assignments` | JWT required | `Admin`, `Dispatcher` |
| GET | `/assignment/api/assignments/{id}` | JWT required | `Admin`, `Dispatcher` |
| GET | `/assignment/api/assignments/delivery/{deliveryId}` | JWT required | `Admin`, `Dispatcher` |
| GET | `/assignment/api/assignments/driver/{driverId}/active` | JWT required | `Admin`, `Dispatcher`, `Driver` |
| PUT | `/assignment/api/assignments/{id}/complete` | JWT required | `Admin`, `Dispatcher` |
| PUT | `/assignment/api/assignments/{id}/cancel` | JWT required | `Admin`, `Dispatcher` |

---

## 7) RouteService (`/route/api/...` via gateway)

| Method | Endpoint | Auth via gateway | Service role requirement | Notes |
|---|---|---|---|---|
| POST | `/route/api/routes/optimize` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/routes/calculate` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/routes/alternatives` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| POST | `/route/api/routes/select` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/routes/history` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/routes/compare` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/routes/rank` | JWT required | None (`[Authorize]` not applied) | Gateway enforces token |
| GET | `/route/api/reports/fuel-cost` | JWT required | `Admin`, `Dispatcher`, `Manager` | Service controller enforces roles |
| GET | `/route/api/reports/fuel-cost/csv` | JWT required | `Admin`, `Dispatcher`, `Manager` | Service controller enforces roles |

---

## 8) TrackingService + SignalR (`/tracking/api/...`, `/hubs/tracking`)

### 8.1 REST endpoints

| Method | Endpoint | Auth | Roles |
|---|---|---|---|
| POST | `/tracking/api/tracking/location` | JWT required | `Driver` |
| GET | `/tracking/api/tracking/assignment/{assignmentId}/history` | JWT required | `Admin`, `Dispatcher` |
| GET | `/tracking/api/tracking/driver/{driverId}/last-known` | JWT required | `Admin`, `Dispatcher` |

### 8.2 Hub endpoint

| Channel | Endpoint | Auth | Roles |
|---|---|---|---|
| WebSocket | `/hubs/tracking` | JWT required | Any authenticated user to connect |

SignalR hub method authorization:
- `JoinAssignmentGroup` -> `Admin`, `Dispatcher`
- `LeaveAssignmentGroup` -> `Admin`, `Dispatcher`
- `SendLocationUpdate` -> `Driver`

---

## 9) Role reference

- `Admin`: full admin operations
- `Dispatcher`: operations and dispatch/report access
- `Driver`: driver-scoped operations and location publishing
- `Manager`: report-only role on selected CSV/report endpoints

Source of truth:
- `[Authorize]` attributes in controller and hub classes
- `src/ApiGateway/ocelot.Development.json` `AuthenticationOptions`
- Gateway middleware checks in `src/ApiGateway/Program.cs`
