# Meridian ASE Testing Evaluation

This folder contains the ASE-specific deliverables for the Meridian testing evaluation using:

- `xUnit` for unit testing
- `k6` for load testing

The production test code stays in the normal backend locations:

- `src/RouteService/RouteService.Tests`
- `src/UserService/UserService.Tests`
- `load-tests/dispatcher-session.js`
- `load-tests/report-generation.js`

## Current Meridian Baseline

- The backend already contains 7 xUnit test projects and roughly 220 discovered `[Fact]` / `[Theory]` tests.
- `RouteService.Tests` is the main unit-testing demo area for Suhasna's parameterized testing feature.
- `UserService.Tests/AuthValidationTests.cs` is the fixture/mocking demo area for Sumuditha's local test-host hardening.
- The load-testing area already existed in `load-tests/`; ASE work adds local smoke-run wrappers, env templates, and demo documentation around those scripts.

## Demo Commands

Run Suhasna's unit-test demo:

```bash
dotnet test src/RouteService/RouteService.Tests/RouteService.Tests.csproj \
  --filter "FullyQualifiedName~FuelCostCalculatorTests"
```

Run Sumuditha's unit-test demo:

```bash
dotnet test src/UserService/UserService.Tests/UserService.Tests.csproj \
  --filter "FullyQualifiedName~AuthValidationTests"
```

Run Luchitha's local k6 smoke demo:

```bash
./ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh
```

Run Sasindi's local k6 smoke demo:

```bash
./ASE/testing-evaluation/scripts/run-report-local-smoke.sh
```

## Folder Guide

- [testing-tool-evaluation.html](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/testing-tool-evaluation.html) is the ASE presentation-ready summary.
- [TEAM_WORK.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/TEAM_WORK.md) maps branches and ownership.
- [DEMO_PLAN.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/DEMO_PLAN.md) breaks down the 12-minute flow.
- [RUNBOOK.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/RUNBOOK.md) contains rehearsal and live-demo steps.
- [env/local-k6.env.example](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/env/local-k6.env.example) is the local k6 template.
- [evidence/README.md](/Users/realdulain/Documents/Projects/Meridian/Meridian-Backend/ASE/testing-evaluation/evidence/README.md) explains what to save after rehearsal runs.
