# Team Work And Branching

## Branch Strategy

- Base branch: `main`
- Shared integration branch: `ase/testing-evaluation`
- Suhasna: `ase/suhasna-xunit-parameterized-route`
- Sumuditha: `ase/sumuditha-xunit-fixture-auth`
- Luchitha: `ase/luchitha-k6-dispatcher-local`
- Sasindi: `ase/sasindi-k6-report-local`

Each student should branch from `ase/testing-evaluation` and open pull requests back into `ase/testing-evaluation`. Merge into `main` only after the four ASE branches are reviewed and the demo is rehearsed.

## Ownership Map

- Suhasna
  - Area: `src/RouteService/RouteService.Tests`
  - Demo feature: xUnit parameterized testing
  - Focus: theory-based coverage for fuel calculation and route-related edge cases

- Sumuditha
  - Area: `src/UserService/UserService.Tests`
  - Demo feature: xUnit fixture-based local test host and dependency replacement
  - Focus: keep auth validation tests local, deterministic, and independent from `ConnectionStrings:UserDb`

- Luchitha
  - Area: `load-tests/dispatcher-session.js`
  - Demo feature: local dispatcher smoke load test
  - Focus: demo-safe profile, local base URL, ID pool support, short evidence-friendly output

- Sasindi
  - Area: `load-tests/report-generation.js`
  - Demo feature: local report smoke load test
  - Focus: demo-safe report profile, tagged timing output, summary export, and evidence capture

## Integration Rules

- Do not merge directly into `main`.
- Keep ASE documentation in `ASE/testing-evaluation/`.
- Keep real xUnit edits in existing test projects and real k6 edits in `load-tests/`.
- Save rehearsal outputs into `ASE/testing-evaluation/evidence/`.
