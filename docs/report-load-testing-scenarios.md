# Report generation load scenarios (MER-293)

**Story:** MER-92 — Test report generation under load  
**Subtask:** MER-293 — Define report generation load scenarios  
**Tool:** k6 (same stack as MER-288 / dispatcher load tests)

This document defines **what** we simulate for **report APIs** before scripting and running against QA (MER-295, MER-296, MER-297). Traffic uses the **API Gateway** `BASE_URL` (no trailing slash), same as `load-tests/dispatcher-session.js`.

---

## 1. Scope

| In scope | Out of scope (unless added to API later) |
|----------|------------------------------------------|
| Authenticated **GET** report endpoints (JSON) | Refresh-token flows |
| Concurrent users requesting **one or more** reports after login | Browser / PDF rendering |
| Roles **Admin** or **Dispatcher** (per controllers) | gRPC-only internals |

**Source of truth (backend):**

- `DeliveryService` → `GET /api/reports/delivery-success`
- `VehicleService` → `GET /api/reports/vehicle-utilization`
- `DriverService` → `GET /api/reports/driver-performance`

Each accepts optional query params: `startDateUtc`, `endDateUtc` (ISO date-time).

**CSV export (MER-296):** There is **no** dedicated CSV export route in this repository’s controllers at the time of writing. MER-296 should target whatever the team exposes (e.g. a future `GET .../export.csv` or a BFF route). Until then, treat **large JSON report responses** as the stand-in for “heavy read” under load, or align with the frontend team on the exact export URL.

---

## 2. QA gateway paths (via Ocelot)

Upstream paths on the gateway (see `src/ApiGateway/ocelot.QA.json` — `/delivery`, `/vehicle`, `/driver` prefixes):

| Report | Method | Gateway path |
|--------|--------|----------------|
| Delivery success | `GET` | `/delivery/api/reports/delivery-success` |
| Vehicle utilization | `GET` | `/vehicle/api/reports/vehicle-utilization` |
| Driver performance | `GET` | `/driver/api/reports/driver-performance` |

Optional query string (same for all):

- `?startDateUtc=2026-01-01T00:00:00Z&endDateUtc=2026-04-01T23:59:59Z`  
  Omit or use defaults allowed by each service (see `ReportsController` implementations).

**Auth:** `Authorization: Bearer <accessToken>` from `POST /api/auth/login` (same as dispatcher tests).

---

## 3. Primary scenario — “Report burst” (realistic)

One **iteration** = one synthetic **Dispatcher/Admin** user:

1. **Login** — `POST /api/auth/login` (email + password from env).
2. **Generate reports** — call **one or more** of the three GET report endpoints above with the same token.

**Variants to model different load:**

| Variant ID | Description | When to use |
|------------|-------------|-------------|
| R1 — Single report | After login, only `delivery-success` | Smoke / baseline latency |
| R2 — All three sequential | Login → delivery → vehicle → driver (one after another) | Typical “user opens dashboard” |
| R3 — All three concurrent | Login once, then three parallel GETs (same user) | Stress aggregation + gateway fan-out |

For MER-295, tag each request (`name: report_delivery`, `name: report_vehicle`, `name: report_driver`) so k6 can split **response time per endpoint**.

---

## 4. Concurrent load profiles (MER-291 / MER-297 alignment)

Use **QA only**. Tune after a short smoke run.

| Profile | Goal | Suggested shape |
|---------|------|-----------------|
| **Smoke** | Script + env + auth correct | **1–3 VUs**, **30–60 s**, variant R1 or R2 |
| **Typical** | Normal busy period | **10–30 VUs**, ramp **30–90 s**, hold **2–5 min**, variant R2 or R3 |
| **Peak** | Find limits | Higher VUs, team-approved; watch QA DB and Azure costs |

**Executor:** `constant-vus` or `ramping-vus` in k6; separate scenarios if you want “login-only” vs “report-only” isolation (optional).

---

## 5. Test data and environment

| Topic | Decision |
|-------|----------|
| **Credentials** | QA `K6_LOGIN_EMAIL` / `K6_LOGIN_PASSWORD` — user must have **Admin** or **Dispatcher** (same as `load-tests/.env.example` pattern). |
| **Date range** | Use env vars e.g. `MER_REPORT_START_UTC`, `MER_REPORT_END_UTC` so QA data has rows; empty ranges may return fast but unrealistic. |
| **Driver/Vehicle services** | If QA returns **503** or empty data, report payloads may still be valid but **empty** — note in MER-297 results. |

---

## 6. Success criteria (for MER-297 — document later)

- Record **http_req_duration** (p90, p95) **per tagged report endpoint**.
- Record **http_req_failed** and **non-2xx** status codes.
- For MER-296: when CSV exists, add checks for **status code**, **Content-Type**, **body length** (or row count), and **no** obvious truncation (e.g. incomplete last line).

Thresholds (e.g. p95 &lt; X s, error rate &lt; Y%) are **agreed after** the first smoke run.

---

## 7. k6 implementation (concurrent simulation + MER-295 timings)

Script: **`load-tests/report-generation.js`**

| Env | Purpose |
|-----|---------|
| `K6_REPORT_AUTH_MODE` | **`setup`** (default) — single login in `setup()`, all VUs reuse JWT → **measures report endpoints under load** (MER-295). **`per_iteration`** — login every iteration (stresses auth; can hide report latency). |
| `K6_REPORT_VARIANT` | `r1` — delivery only; **`r2`** (default) — three GETs in **sequence**; `r3` — three GETs **in parallel** (`http.batch`, heaviest) |
| `K6_REPORT_VUS` | Target VUs (default `2`) |
| `K6_REPORT_EXECUTOR` | **`ramping`** (default): 0→VUs over `K6_REPORT_RAMP_DURATION` (30s), hold `K6_REPORT_HOLD_DURATION` (90s). **`constant`**: use `K6_REPORT_DURATION` (default 90s). |
| `K6_REPORT_HTTP_TIMEOUT` / `K6_REPORT_LOGIN_TIMEOUT` | Report GET / login timeouts (defaults **120s** / **90s**) — reduces false timeouts on slow QA |
| `MER_REPORT_START_UTC` / `MER_REPORT_END_UTC` | Optional query params on all report GETs |

Request **tags** for per-endpoint metrics: `report_delivery`, `report_vehicle`, `report_driver`, plus `login`.

**MER-295:** Threshold **p(95)** for reports tracks **`K6_REPORT_HTTP_TIMEOUT` + 5s** (ms); login threshold tracks **`K6_REPORT_LOGIN_TIMEOUT` + 5s**. **`handleSummary`** prints **MER-295: report endpoint timings**.

**Next:** MER-296 (CSV checks when an export URL exists); MER-297 (document thresholds vs results).

Run commands: `load-tests/README.md`.

