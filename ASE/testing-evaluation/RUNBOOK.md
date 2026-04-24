# ASE Rehearsal And Live Demo Runbook

## Prerequisites

- .NET SDK installed
- Docker Desktop running
- Node.js installed
- `k6` installed locally
  - macOS: `brew install k6`

## 1. Start the Meridian local stack

From the workspace root (`Meridian/`), start shared infrastructure:

```bash
docker compose up -d
```

Then start the microservices:

```bash
./run-all-microservices.sh
```

Wait until the gateway is reachable on `http://localhost:5050`.

## 2. Validate the unit-test demos

From `Meridian-Backend/`, run:

```bash
dotnet test src/RouteService/RouteService.Tests/RouteService.Tests.csproj \
  --filter "FullyQualifiedName~FuelCostCalculatorTests"
```

```bash
dotnet test src/UserService/UserService.Tests/UserService.Tests.csproj \
  --filter "FullyQualifiedName~AuthValidationTests"
```

Optional wider baseline checks:

```bash
dotnet test src/RouteService/RouteService.Tests/RouteService.Tests.csproj
dotnet test src/TrackingService/TrackingService.Tests/TrackingService.Tests.csproj
```

## 3. Prepare local k6 env

Create a real env file from the template:

```bash
cp ASE/testing-evaluation/env/local-k6.env.example ASE/testing-evaluation/env/local-k6.env
```

Update the login credentials and local IDs in `local-k6.env` before rehearsal.

## 4. Run the local k6 demos

Dispatcher smoke run:

```bash
./ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh
```

Report smoke run:

```bash
./ASE/testing-evaluation/scripts/run-report-local-smoke.sh
```

These scripts save text output and summary JSON into `ASE/testing-evaluation/evidence/`.

## 5. Evidence to keep before the viva

- One passing screenshot of Suhasna's filtered RouteService test run
- One passing screenshot of Sumuditha's filtered UserService auth test run
- `dispatcher-local-smoke.txt`
- `dispatcher-local-smoke-summary.json`
- `report-local-smoke.txt`
- `report-local-smoke-summary.json`

## 6. Live-demo defaults

- Use the short local smoke profiles only.
- Keep a terminal already positioned in `Meridian-Backend/`.
- Keep the `evidence/` folder open so outputs can be shown immediately if the machine is slow.
- Do not attempt QA-scale load during the viva.
