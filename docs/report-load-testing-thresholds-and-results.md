# Report load testing: acceptable thresholds and results (MER-297)

**Story:** MER-92 — Test report generation under load  
**Subtask:** MER-297 — Document acceptable thresholds and results  
**Related:** MER-293 (scenarios), MER-295 (k6 timings), `load-tests/report-generation.js`

This document separates **what the k6 script enforces by default** from **what the team may agree as acceptable for QA** (or production SLOs later). Calibrate numbers after at least one **smoke run** when report endpoints return **200** consistently.

---

## 1. What to record for every serious run

| Field | Why it matters |
|-------|----------------|
| **Date / time (UTC)** | QA changes over time; compare like with like. |
| **`BASE_URL`** (gateway host) | Confirms which environment was tested. |
| **Script + env** | e.g. `report-generation.js`, `K6_REPORT_VARIANT`, `K6_REPORT_VUS`, `K6_REPORT_EXECUTOR`, timeouts, `MER_REPORT_*` date range. |
| **k6 end summary** | `http_req_failed` rate, `http_reqs`, `checks` pass/fail. |
| **Per-endpoint duration** | From **`handleSummary`** block “MER-295: report endpoint timings” — avg, med, p(90), p(95), max for `report_delivery`, `report_vehicle`, `report_driver`, and `login` if relevant. |
| **Threshold pass/fail** | k6 prints which thresholds passed; note any intentional overrides (`K6_REPORT_MAX_FAIL_RATE`). |
| **Qualitative notes** | Empty JSON `data`, **400** with SQL errors, cold start, known outages. |

**Artifact tip:** Save full console output, e.g. `k6 run load-tests/report-generation.js 2>&1 | tee load-tests/results-report-YYYYMMDD.txt`. Optionally add `--summary-export=summary.json` for charts.

---

## 2. What `report-generation.js` enforces (defaults)

These are **technical guardrails** so slow QA does not fail the run purely because the default k6 timeout is 60s. They are **not** the same as a product requirement unless the team adopts them as such.

| Metric | Default rule | Env override |
|--------|----------------|--------------|
| **`http_req_failed`** | `rate < 0.5` (fail &lt; 50% of requests) | `K6_REPORT_MAX_FAIL_RATE` (0–1), e.g. `0.01` for strict runs |
| **`http_req_duration` (reports)** | `p(95) < (K6_REPORT_HTTP_TIMEOUT + 5s)` | `K6_REPORT_HTTP_TIMEOUT` (default **120s**) → threshold **125s** |
| **`http_req_duration{name:login}`** | `p(95) < (K6_REPORT_LOGIN_TIMEOUT + 5s)` | `K6_REPORT_LOGIN_TIMEOUT` (default **90s**) → **95s** |

**Interpretation:** If report GETs return **400** (e.g. Azure SQL cross-database error) or **5xx**, failures count toward `http_req_failed` and checks fail — **that is correct** for measuring “did the API succeed under load,” not “did the script run.”

---

## 3. Suggested QA acceptance (team agreement — edit after smoke)

These are **starting points** for discussion, not fixed requirements. Replace with your own SLOs once baseline data exists.

| Area | Smoke / documentation run | Typical “green” QA run (after backend stable) |
|------|---------------------------|-----------------------------------------------|
| **`http_req_failed`** | May temporarily raise `K6_REPORT_MAX_FAIL_RATE` only to **capture** metrics while bugs exist; document that the run is **not** a pass. | Target **&lt; 1–5%** non-login requests failed, **no** sustained 4xx/5xx on report routes except known data issues. |
| **Report `p(95)` latency** | Use script defaults; focus on **whether** endpoints return 200. | Agree a target (e.g. **&lt; 10–30s** on QA) depending on data size and cold starts — **not** the same as production. |
| **`checks` (status 200)** | Fails if APIs return non-200 — **expected** until DB/SQL issues are fixed. | All report checks **pass** for the date range used. |
| **Empty `data`** | **200** with empty arrays is **acceptable** for “route works”; note **no rows** in the report. | Seed data if you need non-empty load behavior. |

**Known QA blockers (not load):**

- **400** with `Reference to database and/or server name in 'meridian_delivery.dbo.Deliveries'...` — Azure SQL does not support that cross-database pattern as written; fix queries or topology, then re-run.
- **404** — wrong path or gateway routing; see `docs/report-load-testing-scenarios.md` §2.1.

---

## 4. Pass / fail / “documented only”

| Outcome | When to use it |
|---------|----------------|
| **Pass** | Thresholds met, report checks mostly 200, failure rate within agreed cap, notes any acceptable empty data. |
| **Fail** | High error rate, timeouts, or non-200 on report routes when the system should be healthy. |
| **Documented only** | Intentional high `K6_REPORT_MAX_FAIL_RATE`, or run executed **to prove** failure mode / capture timings during an incident — label the Jira comment accordingly so it is not read as a green QA sign-off. |

---

## 5. Dispatcher tests (MER-91) — separate thresholds

`load-tests/dispatcher-session.js` uses different defaults:

| Metric | Rule in script |
|--------|----------------|
| `http_req_failed` | `rate < 0.5` |
| `http_req_duration` | `p(95) < 10000` (10s) |

**MER-292** results and bottlenecks are documented in `docs/load-testing-scenarios.md` 
