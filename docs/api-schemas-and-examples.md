# Meridian API Schemas and Example Payloads

Reference for **MER-340**: request/response schemas and sample JSON payloads for the Meridian API.

Use this with `docs/api-endpoints.md`:
- `api-endpoints.md` = endpoint inventory
- `api-schemas-and-examples.md` = payload shapes

---

## 1) API Gateway

### GET `/api/dashboard/summary` (response)

```json
{
  "success": true,
  "data": {
    "totalDeliveries": 148,
    "pendingDeliveries": 27,
    "activeDeliveries": 19,
    "completedDeliveries": 95,
    "overdueDeliveries": 7,
    "availableVehicles": 14,
    "vehiclesOnTrip": 9,
    "availableDrivers": 12,
    "activeAssignments": 18,
    "generatedAtUtc": "2026-04-13T07:30:00Z"
  }
}
```

---

## 2) UserService

### Auth schemas

`POST /api/auth/register` request:
```json
{
  "fullName": "string",
  "email": "string",
  "password": "string",
  "role": "Admin|Dispatcher|Driver"
}
```

`POST /api/auth/login` request:
```json
{
  "email": "string",
  "password": "string"
}
```

`POST /api/auth/refresh|revoke|logout` request:
```json
{
  "refreshToken": "string"
}
```

Auth response (`register`, `login`, `refresh`):
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 3600
}
```

### Driver account provisioning

`POST /api/users/driver-accounts` request:
```json
{
  "fullName": "Kasun Silva",
  "email": "kasun.driver@meridian.local",
  "password": "Driver@123",
  "licenseNumber": "B1234567",
  "licenseExpiry": "2028-12-31",
  "phoneNumber": "+94771234567",
  "maxWorkingHoursPerDay": 8
}
```

Response:
```json
{
  "success": true,
  "data": {
    "user": {
      "userId": 42,
      "fullName": "Kasun Silva",
      "email": "kasun.driver@meridian.local",
      "role": "Driver",
      "isActive": true,
      "createdAt": "2026-04-13T07:40:00Z",
      "updatedAt": "2026-04-13T07:40:00Z"
    },
    "driver": {
      "driverId": 12,
      "userId": "42",
      "fullName": "Kasun Silva",
      "licenseNumber": "B1234567",
      "licenseExpiry": "2028-12-31",
      "phoneNumber": "+94771234567",
      "maxWorkingHoursPerDay": 8,
      "currentWorkingHoursToday": 0,
      "isActive": true,
      "createdAt": "2026-04-13T07:40:00Z",
      "updatedAt": "2026-04-13T07:40:00Z"
    }
  }
}
```

---

## 3) DeliveryService

### `POST /api/deliveries` request

```json
{
  "pickupAddress": "No 15, Flower Road, Colombo 07",
  "deliveryAddress": "Kandy City Center, Kandy",
  "packageWeightKg": 22.5,
  "packageVolumeM3": 0.12,
  "deadline": "2026-04-15T12:00:00Z",
  "createdBy": "dispatcher@meridian.local"
}
```

### `GET /api/deliveries/{id}` response shape

```json
{
  "id": 101,
  "pickupAddress": "string",
  "deliveryAddress": "string",
  "packageWeightKg": 22.5,
  "packageVolumeM3": 0.12,
  "deadline": "2026-04-15T12:00:00Z",
  "status": "Pending|Assigned|InTransit|Completed|Failed|Cancelled",
  "assignedVehicleId": 5,
  "assignedDriverId": 12,
  "createdAt": "2026-04-13T07:45:00Z",
  "updatedAt": "2026-04-13T08:00:00Z",
  "createdBy": "dispatcher@meridian.local",
  "statusHistory": [
    {
      "statusHistoryId": 1,
      "previousStatus": null,
      "newStatus": "Pending",
      "changedAt": "2026-04-13T07:45:00Z",
      "changedBy": 2,
      "notes": "Created"
    }
  ]
}
```

### `GET /api/deliveries/{id}/recommend-vehicles` item

```json
{
  "vehicleId": 5,
  "plateNumber": "CAB-1234",
  "make": "Isuzu",
  "model": "NPR",
  "capacityKg": 3500,
  "capacityM3": 18,
  "fuelEfficiencyKmPerLitre": 7.5,
  "currentLocation": "Colombo 05",
  "distanceToPickupKm": 4.3,
  "matchScore": 92.5,
  "recommendationReason": "Best capacity-fit and shortest pickup distance"
}
```

---

## 4) VehicleService

### Vehicle create/update schema

```json
{
  "vehicleId": 5,
  "plateNumber": "CAB-1234",
  "make": "Isuzu",
  "model": "NPR",
  "currentLocation": "Colombo 05",
  "year": 2022,
  "capacityKg": 3500,
  "capacityM3": 18,
  "fuelEfficiencyKmPerLitre": 7.5,
  "status": "Available|OnTrip|Maintenance|Retired",
  "createdAt": "2026-04-13T07:00:00Z",
  "updatedAt": "2026-04-13T07:00:00Z"
}
```

`PUT /api/vehicles/{id}/status` request:
```json
{
  "status": "OnTrip"
}
```

Envelope response example:
```json
{
  "success": true,
  "data": { "vehicleId": 5 }
}
```

---

## 5) DriverService

### Driver create/update schema

```json
{
  "driverId": 12,
  "userId": "42",
  "fullName": "Kasun Silva",
  "licenseNumber": "B1234567",
  "licenseExpiry": "2028-12-31",
  "phoneNumber": "+94771234567",
  "maxWorkingHoursPerDay": 8,
  "currentWorkingHoursToday": 3.5,
  "isActive": true,
  "createdAt": "2026-04-13T07:40:00Z",
  "updatedAt": "2026-04-13T08:10:00Z"
}
```

`PUT /api/drivers/{id}/hours` request:
```json
{
  "hoursToAdd": 2.5
}
```

---

## 6) AssignmentService

### `POST /api/assignments` request

```json
{
  "deliveryId": 101,
  "vehicleId": 5,
  "driverId": 12,
  "notes": "Handle before noon"
}
```

### Assignment response item

```json
{
  "assignmentId": 88,
  "deliveryId": 101,
  "vehicleId": 5,
  "driverId": 12,
  "assignedAt": "2026-04-13T08:15:00Z",
  "assignedBy": "2",
  "status": "Active|Completed|Cancelled",
  "notes": "Handle before noon",
  "createdAt": "2026-04-13T08:15:00Z",
  "updatedAt": "2026-04-13T08:15:00Z"
}
```

### Assignment history item (`GET /api/assignments/history`)

```json
{
  "assignmentHistoryId": 501,
  "assignmentId": 88,
  "deliveryId": 101,
  "vehicleId": 5,
  "driverId": 12,
  "previousStatus": "Active",
  "newStatus": "Completed",
  "action": "Completed",
  "changedBy": "2",
  "changedAt": "2026-04-13T10:30:00Z",
  "notes": "Delivered successfully"
}
```

---

## 7) RouteService

### `POST /api/routes/optimize` request

```json
{
  "origin": "Colombo",
  "destination": "Kandy",
  "vehicleId": 5,
  "deliveryId": 101
}
```

### Route option shape

```json
{
  "routeId": "route-1",
  "summary": "via A1",
  "distance": "116 km",
  "distanceValue": 116000,
  "duration": "2h 45m",
  "durationValue": 9900,
  "fuelCost": 5200.5,
  "polylinePoints": "encoded_polyline_here"
}
```

### `POST /api/routes/select` request

```json
{
  "origin": "Colombo",
  "destination": "Kandy",
  "vehicleId": 5,
  "driverId": 12,
  "route": {
    "routeId": "route-1",
    "summary": "via A1",
    "distance": "116 km",
    "distanceValue": 116000,
    "duration": "2h 45m",
    "durationValue": 9900,
    "fuelCost": 5200.5,
    "polylinePoints": "encoded_polyline_here"
  }
}
```

---

## 8) TrackingService

### `POST /api/tracking/location` request

```json
{
  "assignmentId": 88,
  "driverId": 12,
  "latitude": 6.9271,
  "longitude": 79.8612,
  "timestamp": "2026-04-13T09:10:00Z",
  "speedKmh": 38.5
}
```

### Location response item

```json
{
  "locationUpdateId": 9001,
  "assignmentId": 88,
  "driverId": 12,
  "latitude": 6.9271,
  "longitude": 79.8612,
  "timestamp": "2026-04-13T09:10:00Z",
  "speedKmh": 38.5
}
```

---

## 9) Report endpoint payloads (cross-service)

### Delivery report item (`/delivery/api/reports/delivery-success`)
```json
{
  "deliveredCount": 95,
  "failedCount": 6,
  "cancelledCount": 3,
  "terminalCount": 104,
  "successRatePercentage": 91.35
}
```

### Vehicle utilization item (`/vehicle/api/reports/vehicle-utilization`)
```json
{
  "vehicleId": 5,
  "tripsCount": 17,
  "kilometersDriven": 812.4,
  "idleTimeMinutes": 210
}
```

### Driver performance item (`/driver/api/reports/driver-performance`)
```json
{
  "driverId": 12,
  "deliveriesCompleted": 34,
  "averageDeliveryTimeMinutes": 52.1,
  "onTimeRatePercent": 93.2
}
```

### Fuel cost report item (`/route/api/reports/fuel-cost`)
```json
{
  "vehicleId": 5,
  "driverId": 12,
  "periodStartUtc": "2026-04-01T00:00:00Z",
  "tripCount": 12,
  "totalDistanceKm": 430.2,
  "totalFuelConsumptionLitres": 57.36,
  "totalFuelCostLkr": 20710.88
}
```

---

## 10) Source of truth

- DTO/model definitions under each service `API/DTOs` and `API/Models`
- Controller action signatures under `src/*/*/Controllers/`
- Gateway aggregation model: `src/ApiGateway/Models/`
