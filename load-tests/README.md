# k6 load tests

This folder contains load tests for dispatcher workflows against the QA API gateway.

**Report generation (MER-92):** scenarios are in [`docs/report-load-testing-scenarios.md`](../docs/report-load-testing-scenarios.md). Simulate concurrent report traffic with:

```bash
export BASE_URL="https://<your-gateway>.azurecontainerapps.io"
export K6_LOGIN_EMAIL="<qa user>"
export K6_LOGIN_PASSWORD="<secret>"

# Defaults: setup auth, variant r2 (sequential reports), 2 VUs, ramping 30s→2 VUs then 5m hold (r2/r3 need time per iteration on slow QA),
# report HTTP timeout 120s, login timeout 90s (QA-friendly).
export K6_REPORT_VARIANT=r2   # r1 | r2 | r3 (parallel batch — heavier on backends)
# export K6_REPORT_EXECUTOR=constant K6_REPORT_DURATION=90s   # instead of ramping
# export K6_REPORT_VUS=5
# export K6_REPORT_HTTP_TIMEOUT=180s   # if QA still times out
# MER-295: default auth = one login in setup(), all VUs hit reports.
# export K6_REPORT_AUTH_MODE=per_iteration   # login every iteration
# Optional date range:
# export MER_REPORT_START_UTC="2026-01-01T00:00:00Z"
# export MER_REPORT_END_UTC="2026-04-01T23:59:59Z"

k6 run load-tests/report-generation.js
# End of run prints diagnostics + "MER-295: report endpoint timings" (p95 per tagged endpoint).
```

Save output for MER-297:

```bash
k6 run load-tests/report-generation.js 2>&1 | tee load-tests/report-results-qa.txt
```

**Thresholds and what to put in Jira:** [`docs/report-load-testing-thresholds-and-results.md`](../docs/report-load-testing-thresholds-and-results.md) (MER-297).

### Report test: many failures / 404 with a valid token

The script calls **`/delivery/api/reports/delivery-success`**, **`/vehicle/api/reports/vehicle-utilization`**, **`/driver/api/reports/driver-performance`** (same as the backend controllers). Paths like **`.../utilization`** or **`.../performance`** alone are **wrong** and will 404. If QA still returns **404** on the exact paths above, the **gateway deployment** may not match `ocelot.QA.json` or services may be missing — that is **not** a load-test issue. See **`docs/report-load-testing-scenarios.md` §2.1**. Optional overrides: `MER_REPORT_SEGMENT_VEHICLE`, etc.

### Report test: if `setup()` times out after 60s

k6’s default **`setupTimeout` is 60s**. This script sets **`setupTimeout` to 120s** by default (login can use up to **90s**). Override if needed: `export K6_REPORT_SETUP_TIMEOUT=180s`.

### Report test: if `http_req_failed` threshold fails

1. Read the **full** k6 **TOTAL RESULTS** (failure rate and check pass/fail). The script also prints a short **run diagnostics** block before MER-295 timings.
2. **False failures at end of test:** the scenario uses **`gracefulStop` (default 120s)** so VUs can finish the current iteration. If it still spikes, increase: `export K6_REPORT_GRACEFUL_STOP=180s`.
3. **QA still flaky** (document numbers, not a green gate): relax only for reporting: `export K6_REPORT_MAX_FAIL_RATE=0.9` (team should agree).
4. **Isolate one endpoint:** `export K6_REPORT_VARIANT=r1` and retest.
5. **Manual check** (get `TOKEN` from login JSON):

```bash
curl -i -m 120 -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/delivery/api/reports/delivery-success"
curl -i -m 120 -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/vehicle/api/reports/vehicle-utilization"
curl -i -m 120 -H "Authorization: Bearer $TOKEN" \
  "$BASE_URL/driver/api/reports/driver-performance"
```

## What this test covers

`dispatcher-session.js` runs 3 concurrent scenarios:

- `login`: calls `POST /api/auth/login`
- `fetch_deliveries`: login, then calls `GET /delivery/api/deliveries`
- `assign_vehicle`: login, then calls `POST /assignment/api/assignments`

## Prerequisites

- `k6` installed locally
  - macOS: `brew install k6`
- Valid QA credentials
- Reachable QA gateway URL
- Existing IDs in QA database for assignment flow (`DELIVERY_ID`, `VEHICLE_ID`, `DRIVER_ID`)

## Required environment variables

- `BASE_URL`: gateway base URL with no trailing slash
  - Example: `https://ca-api-gateway.happysand-beec0abe.eastasia.azurecontainerapps.io`
- `K6_LOGIN_EMAIL`: QA account email
- `K6_LOGIN_PASSWORD`: QA account password

Optional (used by assignment scenario):

- `DELIVERY_ID` (default: `1`)
- `VEHICLE_ID` (default: `1`)
- `DRIVER_ID` (default: `1`)

## How to run

From the repository root:

```bash
export BASE_URL="https://<your-gateway>.azurecontainerapps.io"
export K6_LOGIN_EMAIL="<qa user email>"
export K6_LOGIN_PASSWORD="<qa user password>"

# Optional but recommended to set valid IDs from QA DB
export DELIVERY_ID=1
export VEHICLE_ID=1
export DRIVER_ID=1

k6 run load-tests/dispatcher-session.js
```

Save run output to a file:

```bash
k6 run load-tests/dispatcher-session.js | tee load-tests/results-qa.txt
```

## How to read results

Focus on these metrics in the summary:

- `http_req_failed`: request failure rate
- `http_req_duration` (especially `p(95)`): latency under load
- checks:
  - `login status 200`
  - `deliveries status 200`
  - `assignment 2xx or 409`

## Common issues

- **Timeouts at ~60s**: backend not responding fast enough under concurrency
- **HTTP 500**: server-side error in gateway/auth/delivery/assignment path
- **All requests fail immediately**: missing/invalid env vars or wrong `BASE_URL`

## Documenting results (MER-292)

After a QA run, save the console output and summarize bottlenecks for the team:

```bash
k6 run load-tests/dispatcher-session.js 2>&1 | tee load-tests/results-qa.txt
```

See **`docs/load-testing-scenarios.md` §9** for the template: metrics table, flagged bottlenecks, and a Jira-ready blurb.

Quick connectivity check before running k6:

```bash
curl -i -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$K6_LOGIN_EMAIL\",\"password\":\"$K6_LOGIN_PASSWORD\"}"
```