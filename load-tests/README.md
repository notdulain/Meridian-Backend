# k6 load tests

This folder contains load tests for dispatcher workflows against the QA API gateway.

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

Quick connectivity check before running k6:

```bash
curl -i -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$K6_LOGIN_EMAIL\",\"password\":\"$K6_LOGIN_PASSWORD\"}"
```