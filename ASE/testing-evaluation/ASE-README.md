# ASE Testing Evaluation Report - Group 09

## Module

- Module: `SE3112 - Advanced Software Engineering`
- Project used: `Meridian`
- Testing tools used:
  - `xUnit` for unit testing
  - `k6` for load testing

## Project Overview

Meridian is a microservice-based logistics platform. It has an API gateway and several backend services such as User, Route, Delivery, Vehicle, Driver, Assignment, and Tracking services.

For this ASE testing task, we added automated testing in two areas:

- unit testing for backend logic and authentication validation
- load testing for API gateway flows using k6

This work was organized inside:

```text
Meridian-Backend/ASE/testing-evaluation/
```

Repository link:

- `https://github.com/realdulain/Meridian`

## Team Members

| Name | Student ID | Role in ASE Work |
|---|---:|---|
| D. N. Gunawardhana | IT23750760 | Oversaw the whole ASE work, created the ASE testing folder, created branches for members, delegated tasks, reviewed outputs, and compiled final results |
| U. T. S. Ranatunga | IT23632332 | Worked on xUnit parameterized testing for RouteService |
| L. T. Jayawardena | IT23631724 | Worked on local dispatcher k6 load testing |
| A. M. S. V. Aththanayake | IT23762336 | Worked on xUnit authentication validation and local test host support in UserService |
| T. U. S. S. Bandara | IT23664708 | Worked on local report k6 load testing |

## Branch Structure Used

- Main integration branch: `ase/testing-evaluation`
- Individual branches created:
  - `ase/suhasna-xunit-parameterized-route`
  - `ase/sumuditha-xunit-fixture-auth`
  - `ase/luchitha-k6-dispatcher-local`
  - `ase/sasindi-k6-report-local`

## What Was Implemented

### 1. Unit Testing

We used the existing backend xUnit setup and extended it for ASE work.

Implemented areas:

- parameterized unit testing for fuel calculation logic in RouteService
- authentication and token validation tests in UserService
- local test host setup so auth tests can run without depending on the real database connection string

Main files:

- `src/RouteService/RouteService.Tests/FuelCostCalculatorTests.cs`
- `src/UserService/UserService.Tests/AuthValidationTests.cs`

### 2. Load Testing

We used the existing Meridian k6 scripts and improved them for a local ASE demo.

Implemented areas:

- local smoke-run support for dispatcher workflow
- local smoke-run support for report generation workflow
- env file support for local demo runs
- saved evidence files for terminal outputs and summary JSON files

Main files:

- `load-tests/dispatcher-session.js`
- `load-tests/report-generation.js`
- `ASE/testing-evaluation/env/local-k6.env.example`
- `ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh`
- `ASE/testing-evaluation/scripts/run-report-local-smoke.sh`

## Individual Contributions

### D. N. Gunawardhana - IT23750760

#### Contribution

- Oversaw the full ASE testing work
- Created the `ASE/testing-evaluation/` folder
- Created the branch structure for all members
- Delegated tasks to team members
- Checked unit-test and k6 results
- Compiled the final ASE report and evidence

#### Work Summary

- Organized the final ASE deliverable structure
- Helped combine all testing outputs into one final evaluation package
- Reviewed the saved evidence files and made sure the results were understandable

#### Screenshot Placeholders

- `[Insert screenshot: ASE folder structure]`
- `[Insert screenshot: branch list / git branch output]`
- `[Insert screenshot: compiled evidence folder]`

---

### U. T. S. Ranatunga - IT23632332

#### Role

- Unit testing member
- Owned the parameterized testing work for RouteService

#### Assigned Task

- Worked on parameterized unit testing for RouteService

#### Main File

- `src/RouteService/RouteService.Tests/FuelCostCalculatorTests.cs`

#### What Was Done

- Added theory-based unit tests
- Added multiple test input cases
- Added edge cases for invalid values
- Improved coverage beyond simple happy-path testing

#### Command Run

```bash
dotnet test src/RouteService/RouteService.Tests/RouteService.Tests.csproj --filter "FullyQualifiedName~FuelCostCalculatorTests"
```

#### Expected Result

- Tests should pass
- Final output should show the filtered test class succeeded

#### Screenshot Placeholders

- `[Insert screenshot: code view of FuelCostCalculatorTests.cs]`
- `[Insert screenshot: terminal output for filtered RouteService tests]`

---

### A. M. S. V. Aththanayake - IT23762336

#### Role

- Unit testing member
- Owned the authentication validation testing work for UserService

#### Assigned Task

- Worked on authentication validation tests in UserService

#### Main File

- `src/UserService/UserService.Tests/AuthValidationTests.cs`

#### What Was Done

- Added and maintained token validation test cases
- Verified malformed, expired, wrong issuer, and wrong audience token handling
- Verified valid-token access
- Verified forbidden and not-found access scenarios
- Kept the tests local and independent from the real DB configuration

#### Command Run

```bash
dotnet test src/UserService/UserService.Tests/UserService.Tests.csproj --filter "FullyQualifiedName~AuthValidationTests"
```

#### Actual Result

- Total tests: `12`
- Failed: `0`
- Succeeded: `12`

#### Important Note

The terminal output contains many security log messages such as invalid token, expired token, wrong issuer, and wrong audience. These are expected because the tests intentionally send bad tokens to verify that the system rejects them correctly.

#### Screenshot Placeholders

- `[Insert screenshot: code view of AuthValidationTests.cs]`
- `[Insert screenshot: final terminal summary showing 12/12 tests passed]`

---

### L. T. Jayawardena - IT23631724

#### Role

- Load testing member
- Owned the dispatcher workflow smoke test with k6

#### Assigned Task

- Worked on the local dispatcher k6 smoke test

#### Main Files

- `load-tests/dispatcher-session.js`
- `ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh`

#### What Was Done

- Added local demo support for the dispatcher flow
- Added a short demo-safe assignment duration
- Added ID pool support
- Added a wrapper script for easy execution
- Saved text and JSON output files as evidence

#### Command Run

```bash
./ASE/testing-evaluation/scripts/run-dispatcher-local-smoke.sh
```

#### Evidence Files

- `ASE/testing-evaluation/evidence/dispatcher-local-smoke.txt`
- `ASE/testing-evaluation/evidence/dispatcher-local-smoke-summary.json`

#### Result Summary

- Script completed successfully
- Dispatcher flow was tested through the local API gateway
- Response times were low and suitable for a local demo

#### Important Note

This flow includes assignment creation. Because this is a write operation, repeated runs can produce business conflicts such as `409 Conflict`. These are not login or routing failures. They are expected in repeated assignment scenarios.

#### Screenshot Placeholders

- `[Insert screenshot: code view of dispatcher-session.js]`
- `[Insert screenshot: terminal output for dispatcher k6 run]`
- `[Insert screenshot: dispatcher-local-smoke.txt or summary JSON file]`

---

### T. U. S. S. Bandara - IT23664708

#### Role

- Load testing member
- Owned the report workflow smoke test with k6

#### Assigned Task

- Worked on the local report k6 smoke test

#### Main Files

- `load-tests/report-generation.js`
- `ASE/testing-evaluation/scripts/run-report-local-smoke.sh`

#### What Was Done

- Added local demo support for report testing
- Added a simple report smoke-run profile
- Saved terminal and JSON evidence files
- Used tagged timings for report endpoint output

#### Command Run

```bash
./ASE/testing-evaluation/scripts/run-report-local-smoke.sh
```

#### Evidence Files

- `ASE/testing-evaluation/evidence/report-local-smoke.txt`
- `ASE/testing-evaluation/evidence/report-local-smoke-summary.json`

#### Result Summary

- Script completed successfully
- `http_req_failed` was `0.00%`
- Delivery report endpoint timings were shown clearly
- This was the cleanest and safest k6 result for presentation

#### Screenshot Placeholders

- `[Insert screenshot: code view of report-generation.js]`
- `[Insert screenshot: terminal output for report k6 run]`
- `[Insert screenshot: report-local-smoke.txt or summary JSON file]`

## Saved Evidence Files

The following evidence files are available inside `ASE/testing-evaluation/evidence/`:

- `dispatcher-local-smoke.txt`
- `dispatcher-local-smoke-summary.json`
- `report-local-smoke.txt`
- `report-local-smoke-summary.json`

## Final Evaluation Summary

This ASE work successfully added two types of automated testing to the Meridian backend:

- backend unit testing
- backend API load testing

The work also produced:

- a separate ASE testing folder
- separate branches for members
- local demo-ready scripts
- saved evidence files for presentation and reporting

The overall work was reviewed and compiled by D. N. Gunawardhana.

## Required Screenshots Checklist

### Group / Overall

- `[ ] ASE folder structure screenshot`
- `[ ] Branch list screenshot`
- `[ ] Evidence folder screenshot`

### Unit Testing Member - RouteService

- `[ ] Code screenshot of FuelCostCalculatorTests.cs`
- `[ ] Terminal screenshot of filtered RouteService test run`

### Unit Testing Member - UserService

- `[ ] Code screenshot of AuthValidationTests.cs`
- `[ ] Terminal screenshot showing 12 tests passed`

### k6 Member - Dispatcher

- `[ ] Code screenshot of dispatcher-session.js`
- `[ ] Terminal screenshot of dispatcher k6 run summary`
- `[ ] Screenshot of dispatcher-local-smoke.txt or summary JSON`

### k6 Member - Report

- `[ ] Code screenshot of report-generation.js`
- `[ ] Terminal screenshot of report k6 run summary`
- `[ ] Screenshot of report-local-smoke.txt or summary JSON`
