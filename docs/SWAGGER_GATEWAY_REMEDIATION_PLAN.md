# Swagger and API Gateway Remediation Plan

This document captures the recommended change plan for making Swagger reliably accessible through the API Gateway in Azure QA, while keeping the fix maintainable.

## Current Problem Summary

The current failures are caused by a combination of issues:

- Swagger is only enabled when each service runs in `Development`.
- The API Gateway does not host its own Swagger UI; it only proxies downstream services.
- Ocelot catch-all routes protect most service prefixes with JWT auth, which blocks `/swagger` assets and JSON.
- `UserService` Swagger is not routed through the gateway at all.
- Azure deployment uses `--target-port 8080`, but the repo still contains mixed local and container port assumptions.
- The current `deploy-qa.sh` update path only refreshes container images, not environment variables or other app settings.

## Phase 1: Must Fix First

These changes are required before Swagger through the gateway can be expected to work consistently.

### 1. Fix `deploy-qa.sh` update behavior

File:

- [deploy-qa.sh](/Users/realdulain/Documents/Projects/Meridian/deploy-qa.sh)

Problem:

- Existing Container Apps are only updated with a new image.
- New values in `SHARED_ENV` do not get applied to already-created apps.

Required changes:

- Update the existing-app path in `create_app_if_missing()` so it also updates environment variables.
- Reapply any settings that should remain authoritative from the script:
  - target port
  - ingress mode
  - transport mode
- If script changes are not made immediately, delete and recreate the affected Container Apps once so the new env vars actually take effect.

Why this is first:

- Without this, the rest of the changes may appear to be deployed while Azure is still running stale configuration.

### 2. Make Swagger enablement config-driven

Files:

- [DeliveryService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/DeliveryService/DeliveryService.API/Program.cs)
- [UserService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/UserService/UserService.API/Program.cs)
- [VehicleService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/VehicleService/VehicleService.API/Program.cs)
- [DriverService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/DriverService/DriverService.API/Program.cs)
- [AssignmentService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/AssignmentService/AssignmentService.API/Program.cs)
- [RouteService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/RouteService/RouteService.API/Program.cs)
- [TrackingService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/TrackingService/TrackingService.API/Program.cs)

Problem:

- Swagger is currently gated behind `app.Environment.IsDevelopment()`.

Required changes:

- Introduce a config flag such as `Swagger:Enabled`.
- Change the Swagger conditional to something like:
  - `app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled")`
- Set `Swagger__Enabled=true` in QA.

Short-term fallback:

- Keeping `ASPNETCORE_ENVIRONMENT=Development` in QA can unblock testing, but it should be treated as temporary.

Preferred outcome:

- QA can expose Swagger without pretending to be a development environment.

### 3. Add explicit Swagger proxy routes in Ocelot

File:

- [ApiGateway/ocelot.json](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/ApiGateway/ocelot.json)

Problem:

- Most upstream service routes are authenticated catch-alls.
- Requests such as `/vehicle/swagger/index.html` or `/route/swagger/v1/swagger.json` are matched by authenticated routes and blocked before the UI can load.

Required changes:

- Add dedicated unauthenticated Swagger routes for each service that should expose docs through the gateway:
  - `/delivery/swagger/{everything}`
  - `/vehicle/swagger/{everything}`
  - `/driver/swagger/{everything}`
  - `/assignment/swagger/{everything}`
  - `/route/swagger/{everything}`
  - `/tracking/swagger/{everything}`
- Route each upstream path to downstream `/swagger/{everything}`.
- Place the Swagger routes before the broader service catch-all routes.
- Do not attach `AuthenticationOptions` to the Swagger-only routes unless you explicitly want protected docs.

Expected gateway URLs:

- `https://<gateway-fqdn>/delivery/swagger`
- `https://<gateway-fqdn>/vehicle/swagger`
- `https://<gateway-fqdn>/driver/swagger`
- `https://<gateway-fqdn>/assignment/swagger`
- `https://<gateway-fqdn>/route/swagger`
- `https://<gateway-fqdn>/tracking/swagger`

### 4. Add a UserService Swagger route

File:

- [ApiGateway/ocelot.json](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/ApiGateway/ocelot.json)

Problem:

- `UserService` currently exposes only `/api/auth/*`, `/api/users/*`, and `/api/roles/*` through the gateway.
- There is no upstream route that can reach UserService Swagger.

Required changes:

- Add a dedicated route such as:
  - upstream `/user/swagger/{everything}`
  - downstream `/swagger/{everything}`
  - host `${USER_SERVICE_HOST}`
- Keep this route unauthenticated unless there is a deliberate requirement to protect docs.

## Phase 2: Platform Consistency Cleanup

These changes reduce deployment ambiguity and prevent future regressions.

### 5. Normalize Azure container port usage

Files:

- [ApiGateway/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/ApiGateway/Dockerfile)
- [AssignmentService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/AssignmentService/AssignmentService.API/Dockerfile)
- [DeliveryService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/DeliveryService/DeliveryService.API/Dockerfile)
- [DriverService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/DriverService/DriverService.API/Dockerfile)
- [RouteService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/RouteService/RouteService.API/Dockerfile)
- [TrackingService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/TrackingService/TrackingService.API/Dockerfile)
- [UserService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/UserService/UserService.API/Dockerfile)
- [VehicleService/Dockerfile](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/VehicleService/VehicleService.API/Dockerfile)

Problem:

- Local ports and container ports are mixed together across the repo.
- Azure deploys with `--target-port 8080`, but Dockerfiles expose other ports.

Required changes:

- Standardize Azure container listening on `8080`.
- Set `ASPNETCORE_URLS=http://+:8080` for all Azure-deployed services.
- Change Dockerfiles to `EXPOSE 8080`.
- Keep local development ports only in `launchSettings.json`.

Recommended convention:

- Local:
  - gateway `5050`
  - services `6001` to `6007`
- Azure containers:
  - all services listen internally on `8080`

### 6. Keep gateway environment variables explicit

Files:

- [deploy-qa.sh](/Users/realdulain/Documents/Projects/Meridian/deploy-qa.sh)
- [ApiGateway/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/ApiGateway/Program.cs)

Required checks:

- Keep setting `OCELOT_BASE_URL` to the actual gateway FQDN after creation.
- Verify all `*_SERVICE_HOST` values point to the correct internal Container App FQDNs.
- Confirm the downstream scheme assumptions are correct:
  - `https` for REST services
  - `wss` for SignalR WebSocket traffic

Note:

- The current `Program.cs` fallback values are acceptable as local defaults, but Azure should always provide explicit environment variables.

## Phase 3: Authentication Alignment

These changes are not required just to render Swagger pages, but they matter if Swagger "Try it out" or end-to-end auth is expected to work.

### 7. Align downstream auth configuration

Files to review:

- [AssignmentService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/AssignmentService/AssignmentService.API/Program.cs)
- [RouteService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/RouteService/RouteService.API/Program.cs)
- [TrackingService/Program.cs](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/src/TrackingService/TrackingService.API/Program.cs)

Problem:

- These services still use Keycloak-style JWT configuration, while the gateway uses the shared symmetric `MeridianBearer` token settings.

Required decision:

- Decide whether downstream services should validate the same symmetric JWT as the gateway, or rely on the gateway alone.

Recommended direction:

- Use one consistent JWT model across services unless there is a clear reason to keep mixed auth strategies.

## Phase 4: Documentation Cleanup

These changes prevent the next deployment from inheriting outdated assumptions.

### 8. Update docs to reflect the real port model and Swagger access pattern

Files:

- [README.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/README.md)
- [API_GATEWAY.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/docs/API_GATEWAY.md)
- [AZURE_DEPLOYMENT_GUIDE.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/docs/AZURE_DEPLOYMENT_GUIDE.md)

Required changes:

- Remove stale mentions of gateway port `6000` where local usage is actually `5050`.
- Document that the gateway itself does not host Swagger and only proxies downstream Swagger endpoints.
- Document the intended gateway Swagger URLs.
- Document the Azure internal port convention if `8080` becomes standard.

## Verification Checklist

After implementing the changes, verify in this order:

1. Confirm updated env vars are present on existing Container Apps.
2. Confirm each downstream service serves Swagger directly inside the environment.
3. Confirm each gateway Swagger URL loads HTML:
   - `/delivery/swagger`
   - `/vehicle/swagger`
   - `/driver/swagger`
   - `/assignment/swagger`
   - `/route/swagger`
   - `/tracking/swagger`
   - `/user/swagger` if added
4. Confirm the corresponding Swagger JSON loads through the gateway.
5. Confirm normal API routes still enforce JWT where expected.
6. Confirm SignalR routing still works after any Ocelot route changes.

## Recommended Execution Order

If time is limited, execute the work in this order:

1. Fix `deploy-qa.sh` so env var changes actually apply.
2. Make Swagger config-driven.
3. Add Swagger-specific Ocelot routes, including UserService.
4. Redeploy and verify gateway Swagger URLs.
5. Normalize Azure port handling.
6. Align auth and clean up docs.

## Temporary QA Shortcut

If immediate QA access is needed before the full cleanup:

- Keep `ASPNETCORE_ENVIRONMENT=Development` temporarily.
- Add the explicit Swagger routes in Ocelot.
- Redeploy using a script that truly updates env vars on existing apps.

This is acceptable as a short-term unblock, but it should not be the final state.
