# Load testing: concurrent user scenarios (MER-289)

**Story:** MER-91 - QA load tests for concurrent users  
**Subtask:** MER-289 - Define concurrent user scenarios  
**Tool:** k6 (see MER-288)

This document defines **what** we simulate **before** running tests against QA (MER-290). All traffic goes through the **API Gateway** base URL unless noted otherwise.

---

## 1. Scope

| In scope | Out of scope (later / other stories) |
|----------|----------------------------------------|
| `POST /api/auth/login` | Refresh token flows |
| `GET /api/deliveries` (authenticated) | gRPC-only paths |
| `POST /api/assignments` (authenticated) | Frontend / browser load |
| Concurrent virtual users (VUs) via k6 | Production load tests |

**Assumption:** Test users have a role that may call protected routes (e.g. **Dispatcher** or **Admin** for assignment creation). Confirm with the team before MER-290.

---

## 2. Primary scenario — “Dispatcher session” (end-to-end)

One **iteration** = one synthetic user doing a realistic mini workflow.

| Step | Action | Method | Route | Body / headers |
|------|--------|--------|-------|----------------|
| 1 | Login | `POST` | `/api/auth/login` | JSON: `{ "email": "<qa_user>", "password": "<secret>" }` — **no** `Authorization` header |
| 2 | List deliveries | `GET` | **`/delivery/api/deliveries`** (QA gateway) | `Authorization: Bearer <AccessToken>` from step 1 |
| 3 | Assign vehicle | `POST` | **`/assignment/api/assignments`** (QA gateway) | Same header; JSON: `{ "deliveryId": <int>, "vehicleId": <int>, "driverId": <int>, "notes": "k6 MER-289" }` |

**Note:** At the **deployed QA** gateway, Ocelot uses the **`/delivery/...`** and **`/assignment/...`** prefixes. Plain `/api/deliveries` is not routed the same way. See `src/ApiGateway/ocelot.QA.json`.

**Token handling:** Login response JSON includes `accessToken` (camelCase in API responses), `refreshToken`, `expiresIn`. k6 should parse `accessToken` and pass it to steps 2–3.

**Think time (optional):** 1–3 s sleep between steps to mimic human pacing; omit for pure stress.

---

## 3. Concurrent load profiles (choose per run)

Use **QA environment only**. Adjust numbers after a short smoke run.

| Profile | Goal | Suggested k6 shape | When to use |
|---------|------|--------------------|-------------|
| **Smoke** | Script + env correct | **1–5 VUs**, **30–60 s** total, little or no ramp | Before any serious run |
| **Typical** | Normal busy period | **10–30 VUs**, ramp **30–90 s**, hold **3–5 min** | Main MER-290/291 run |
| **Peak** | Stress / find limits | Ramp to **50–100+ VUs**, hold **2–5 min** | Only with team agreement; watch QA DB and costs |

**Executor:** `ramping-vus` or `constant-vus` is enough for MER-289; exact k6 options are set when scripting (MER-290).

---

## 4. Secondary scenarios (optional separate runs)

Use these to **isolate** bottlenecks if the full journey is noisy.

| ID | Name | Steps | Isolates |
|----|------|-------|----------|
| S1 | **Auth only** | Login only, repeated | UserService / JWT issuance |
| S2 | **Read-heavy** | Login → GET `/api/deliveries` (loop or repeat GET) | Gateway + DeliveryService reads |
| S3 | **Write-heavy** | Login → POST `/api/assignments` (minimal GET) | AssignmentService + DB writes |

---

## 5. Test data rules

| Topic | Decision |
|-------|----------|
| **Credentials** | Dedicated QA dispatcher (or admin) account; never prod passwords in scripts — use env vars (`K6_LOGIN_EMAIL`, `K6_LOGIN_PASSWORD`) or CI secrets |
| **IDs for assignment** | **Option A — Single tuple:** one `deliveryId` / `vehicleId` / `driverId` for all VUs → high **contention** (409s possible). **Option B — Pool:** N pre-seeded deliveries; VU `i` uses pool `i % N` → more realistic, fewer artificial conflicts |
| **Data reset** | Agree who resets QA data between runs (dev / DBA / script) |

QA gateway (example — confirm in your Azure deployment):

- Base URL: `https://ca-api-gateway.happysand-beec0abe.eastasia.azurecontainerapps.io`

Credentials and IDs must **not** be committed. Use env vars or `load-tests/.env` (gitignored). See `load-tests/.env.example` and script `load-tests/dispatcher-session.js`.

- Login: `K6_LOGIN_EMAIL`, `K6_LOGIN_PASSWORD`
- Assignment tuple: `DELIVERY_ID` / `VEHICLE_ID` / `DRIVER_ID` (defaults `1` in script; adjust to real QA rows)

---

## 6. Success criteria (for MER-291 — capture metrics)

Not part of MER-289 definition, but align early:

- Record **http_req_duration** (e.g. p95) per step or per request.
- Record **http_req_failed** rate (or % non-2xx where appropriate).
- Note **429/502/504** spikes separately from **401/403/409** (business vs infra).

Thresholds (e.g. “p95 &lt; 2s”, “errors &lt; 1%”) can be agreed with the team after the first smoke run.

---

## 7. Copy-paste summary for Jira (MER-289)

```
MER-289 — Concurrent user scenarios defined

Primary scenario (Dispatcher session):
1) POST /api/auth/login (body: email + password)
2) GET /api/deliveries (Authorization: Bearer accessToken)
3) POST /api/assignments (same header; deliveryId, vehicleId, driverId, notes)

Load profiles documented: Smoke (1–5 VUs), Typical (10–30 VUs), Peak (50+ VUs, team-approved).
Secondary scenarios: Auth-only, Read-heavy, Write-heavy for bottleneck isolation.

Test data: QA gateway URL + dedicated QA user + assignment IDs documented separately.
Full detail: docs/load-testing-scenarios.md
```

---

## 8. References

- Gateway routes: `docs/MERIDIAN_ARCHITECTURE_v2.md`, `docs/API_GATEWAY.md`
- Postman collection: `postman/Meridian-Backend.postman_collection.json`

---

## 9. QA results and bottlenecks (MER-292)

**Purpose:** Record what we measured on QA and what to fix next.  
**Artifact:** Raw k6 console output is saved as `load-tests/results-qa.txt` (regenerate with `k6 run load-tests/dispatcher-session.js | tee load-tests/results-qa.txt`).

### 9.1 Run configuration (this document)

| Item | Value |
|------|--------|
| **Environment** | QA API Gateway (`ca-api-gateway.happysand-beec0abe.eastasia.azurecontainerapps.io`) |
| **Script** | `load-tests/dispatcher-session.js` |
| **Scenarios** | `login`, `fetch_deliveries`, `assign_vehicle` (each **3 VUs**, **45 s**; max **9 VUs** total) |
| **Credentials** | Via `K6_LOGIN_EMAIL` / `K6_LOGIN_PASSWORD` (not committed) |
| **Assignment tuple** | `DELIVERY_ID` / `VEHICLE_ID` / `DRIVER_ID` (defaults `1` when unset) |

### 9.2 Metrics captured (`results-qa.txt` summary)

These numbers come from the **end-of-run** k6 summary in `load-tests/results-qa.txt`.

| Metric | Observed |
|--------|-----------|
| **HTTP requests** | 9 |
| **http_req_failed** | **0.00%** (0 / 9) |
| **http_req_duration** | avg **~44.99 s**, med **~44.47 s**, max **~46.61 s**, **p(95) ~46.61 s** |
| **Checks (`login status 200`)** | **100%** (3 / 3) |
| **Script thresholds** | `http_req_failed` **passed**; `http_req_duration p(95) < 10s` **failed** (p95 ~46.6 s) |
| **Execution note** | Run ended with **3 complete** and **6 interrupted** iterations (some VUs did not finish cleanly within graceful stop — consistent with very slow responses) |

### 9.3 Bottlenecks flagged

1. **Primary: `/api/auth/login` latency under concurrent load**  
   All measured HTTP time is dominated by **~45–47 s** response times while still returning **200**. That points to the **auth path** (gateway → identity/auth service → DB or downstream) **not keeping up** when multiple scenarios hit login at once — not a client-side script issue.

2. **Secondary: dependency readiness (QA data / services)**  
   Manual checks showed **`GET .../driver/api/drivers` returned 503** and **vehicles list empty** at times — assignment scenarios need **valid driver and vehicle rows** and healthy services. Without that, “assign vehicle” load results are harder to interpret even when HTTP errors are zero.

3. **Stability / flakiness**  
   Separate runs at the same profile sometimes show **login timeouts (~60 s)** and **100% http_req_failed** when QA is cold, overloaded, or unstable. Treat **p95 latency** and **failure rate** together; a “green” error rate with **40+ s** p95 is still a **performance failure** for users.

### 9.4 Recommended follow-ups (for dev / DevOps)

- Inspect **Azure Container Apps** metrics and **application logs** for the gateway and auth service during the test window (CPU, restarts, DB connection pool, slow queries).
- Consider **min replicas > 0** on QA to reduce cold-start noise when comparing runs.
- **Seed QA** with at least one **driver** and **vehicle** (and fix driver service **503**) so assignment load is meaningful.
- For MER-291/MER-292 reporting, store **`--summary-export`** JSON alongside `tee` text for easier charts.

### 9.5 Copy-paste for Jira (MER-292)

```
MER-292 — QA load test results documented

Environment: QA gateway (see docs/load-testing-scenarios.md §9).
Script: load-tests/dispatcher-session.js (3 scenarios × 3 VUs × 45s).

Results (from load-tests/results-qa.txt):
- http_req_failed: 0% (0/9)
- http_req_duration p(95): ~46.6s (threshold p95<10s failed)
- Login check: 100% pass

Bottlenecks:
- Auth/login path extremely slow under concurrent load (~45s+), primary suspect for UX and test duration.
- QA driver service intermittently 503; vehicle list sometimes empty — blocks reliable assignment testing.

Artifact: load-tests/results-qa.txt
```
