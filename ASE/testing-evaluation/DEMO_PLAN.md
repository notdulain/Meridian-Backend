# 12-Minute Demo Plan

## 0. Group Intro - 2 minutes

- Explain Meridian as a microservice-based logistics platform with an API gateway and multiple backend services.
- State the two ASE tool choices:
  - `xUnit` for backend unit testing
  - `k6` for API-gateway load testing
- Explain why these fit the current codebase:
  - xUnit already exists across backend services, so the team can demonstrate real extension work instead of a throwaway setup.
  - k6 already exists for Meridian backend traffic, so the ASE work focuses on making it local-demo friendly and evidence-ready.

## 1. Suhasna - 2 minutes

- Open `src/RouteService/RouteService.Tests/FuelCostCalculatorTests.cs`.
- Show the `[Theory]` plus `TheoryData` / `InlineData` cases.
- Run:

```bash
dotnet test src/RouteService/RouteService.Tests/RouteService.Tests.csproj \
  --filter "FullyQualifiedName~FuelCostCalculatorTests"
```

- Explain why parameterized testing improves coverage over a single happy-path test.

## 2. Sumuditha - 2 minutes

- Open `src/UserService/UserService.Tests/AuthValidationTests.cs`.
- Show the `IClassFixture` host plus the fake dependency replacements.
- Run:

```bash
dotnet test src/UserService/UserService.Tests/UserService.Tests.csproj \
  --filter "FullyQualifiedName~AuthValidationTests"
```

- Explain that the auth tests now stay local and do not require `ConnectionStrings:UserDb`.

## 3. Luchitha - 2 minutes

- Open `load-tests/dispatcher-session.js`.
- Point out the local `demo` profile, ID pool support, and summary output.
- Run:

```bash
./ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh
```

- Explain the gateway URL, the VU profile, and how seeded IDs can be controlled with `DELIVERY_IDS`, `VEHICLE_IDS`, and `DRIVER_IDS`.

## 4. Sasindi - 2 minutes

- Open `load-tests/report-generation.js`.
- Point out the local `demo` profile and tagged per-endpoint timings in the summary.
- Run:

```bash
./ASE/testing-evaluation/scripts/run-report-local-smoke.sh
```

- Explain why the live demo uses a short local smoke run while longer runs are saved as evidence artifacts.

## 5. Q&A - 2 minutes

- Each student answers only for their own feature area.
- Keep one fallback terminal ready with previously saved evidence files in case the lab machine is slow.
