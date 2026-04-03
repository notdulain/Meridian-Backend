/**
 * k6: report GETs via QA gateway (MER-92). MER-295: per-endpoint duration under load.
 *
 * Required env:
 *   BASE_URL, K6_LOGIN_EMAIL, K6_LOGIN_PASSWORD
 *
 * Auth (MER-295 default isolates report latency):
 *   K6_REPORT_AUTH_MODE   setup | per_iteration (default: setup)
 *     setup — one login in setup(); all VUs reuse token (measures reports under load)
 *     per_iteration — login every iteration (realistic; login can dominate / timeout)
 *
 * Optional:
 *   K6_REPORT_VARIANT   r1 | r2 | r3 (default: r2 — sequential; less backend contention than r3)
 *   MER_REPORT_START_UTC, MER_REPORT_END_UTC
 *   K6_REPORT_VUS       default 2 (raise after QA is stable)
 *   K6_REPORT_DURATION  default 90s (constant-vus only)
 *   K6_REPORT_EXECUTOR  constant | ramping (default: ramping — gradual VU increase)
 *   K6_REPORT_RAMP_DURATION / K6_REPORT_HOLD_DURATION — ramping stages (defaults 30s / 90s)
 *   K6_REPORT_LOGIN_TIMEOUT   default 90s (setup / per-iteration login)
 *   K6_REPORT_HTTP_TIMEOUT    default 120s (report GETs only; QA often >60s under load)
 */
import http from "k6/http";
import { check, sleep } from "k6";

const base = () => (__ENV.BASE_URL || "").replace(/\/$/, "");

function mustEnv(name) {
  const v = __ENV[name];
  if (!v) throw new Error(`Missing env ${name}`);
  return v;
}

const http2 = false;

function loginHttpParams() {
  return {
    timeout: __ENV.K6_REPORT_LOGIN_TIMEOUT || "90s",
    http2,
  };
}

function reportHttpParams() {
  return {
    timeout: __ENV.K6_REPORT_HTTP_TIMEOUT || "120s",
    http2,
  };
}

function reportQuery() {
  const s = __ENV.MER_REPORT_START_UTC;
  const e = __ENV.MER_REPORT_END_UTC;
  if (!s && !e) return "";
  const parts = [];
  if (s) parts.push(`startDateUtc=${encodeURIComponent(s)}`);
  if (e) parts.push(`endDateUtc=${encodeURIComponent(e)}`);
  return `?${parts.join("&")}`;
}

function login(root) {
  return http.post(
    `${root}/api/auth/login`,
    JSON.stringify({
      email: mustEnv("K6_LOGIN_EMAIL"),
      password: mustEnv("K6_LOGIN_PASSWORD"),
    }),
    {
      ...loginHttpParams(),
      headers: { "Content-Type": "application/json" },
      tags: { name: "login" },
    }
  );
}

function getAccessToken(loginRes) {
  try {
    return JSON.parse(loginRes.body).accessToken;
  } catch {
    return "";
  }
}

function requireBaseUrl() {
  const root = base();
  if (!root) throw new Error("Set BASE_URL");
  return root;
}

/** @param {string} root @param {string} accessToken @param {string} variant */
function runReports(root, accessToken, variant) {
  const rp = reportHttpParams();
  const authHeaders = {
    Authorization: `Bearer ${accessToken}`,
    Accept: "application/json",
  };
  const q = reportQuery();

  const deliveryUrl = `${root}/delivery/api/reports/delivery-success${q}`;
  const vehicleUrl = `${root}/vehicle/api/reports/vehicle-utilization${q}`;
  const driverUrl = `${root}/driver/api/reports/driver-performance${q}`;

  if (variant === "r1") {
    const r = http.get(deliveryUrl, {
      ...rp,
      headers: authHeaders,
      tags: { name: "report_delivery" },
    });
    check(r, { "report_delivery 200": (x) => x.status === 200 });
  } else if (variant === "r2") {
    const d = http.get(deliveryUrl, {
      ...rp,
      headers: authHeaders,
      tags: { name: "report_delivery" },
    });
    check(d, { "report_delivery 200": (x) => x.status === 200 });

    const v = http.get(vehicleUrl, {
      ...rp,
      headers: authHeaders,
      tags: { name: "report_vehicle" },
    });
    check(v, { "report_vehicle 200": (x) => x.status === 200 });

    const dr = http.get(driverUrl, {
      ...rp,
      headers: authHeaders,
      tags: { name: "report_driver" },
    });
    check(dr, { "report_driver 200": (x) => x.status === 200 });
  } else {
    const batchRes = http.batch([
      [
        "GET",
        deliveryUrl,
        null,
        { ...rp, headers: authHeaders, tags: { name: "report_delivery" } },
      ],
      [
        "GET",
        vehicleUrl,
        null,
        { ...rp, headers: authHeaders, tags: { name: "report_vehicle" } },
      ],
      [
        "GET",
        driverUrl,
        null,
        { ...rp, headers: authHeaders, tags: { name: "report_driver" } },
      ],
    ]);

    check(batchRes[0], { "report_delivery 200": (x) => x.status === 200 });
    check(batchRes[1], { "report_vehicle 200": (x) => x.status === 200 });
    check(batchRes[2], { "report_driver 200": (x) => x.status === 200 });
  }

  sleep(1);
}

function reportScenarioThresholdMs() {
  const raw = __ENV.K6_REPORT_HTTP_TIMEOUT || "120s";
  const m = /^(\d+)(s|ms)?$/i.exec(String(raw).trim());
  if (!m) return 125000;
  const n = parseInt(m[1], 10);
  const unit = (m[2] || "s").toLowerCase();
  const ms = unit === "ms" ? n : n * 1000;
  return ms + 5000;
}

function loginThresholdMs() {
  const raw = __ENV.K6_REPORT_LOGIN_TIMEOUT || "90s";
  const m = /^(\d+)(s|ms)?$/i.exec(String(raw).trim());
  if (!m) return 95000;
  const n = parseInt(m[1], 10);
  const unit = (m[2] || "s").toLowerCase();
  const ms = unit === "ms" ? n : n * 1000;
  return ms + 5000;
}

function buildScenarios() {
  const vus = parseInt(__ENV.K6_REPORT_VUS || "2", 10);
  const exec = (__ENV.K6_REPORT_EXECUTOR || "ramping").toLowerCase();

  if (exec === "constant") {
    return {
      concurrent_report_users: {
        executor: "constant-vus",
        vus,
        duration: __ENV.K6_REPORT_DURATION || "90s",
      },
    };
  }

  return {
    concurrent_report_users: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        {
          duration: __ENV.K6_REPORT_RAMP_DURATION || "30s",
          target: vus,
        },
        {
          duration: __ENV.K6_REPORT_HOLD_DURATION || "90s",
          target: vus,
        },
      ],
      gracefulRampDown: "30s",
    },
  };
}

export function setup() {
  const mode = (__ENV.K6_REPORT_AUTH_MODE || "setup").toLowerCase();
  if (mode === "per_iteration") {
    return { authMode: "per_iteration" };
  }

  const root = requireBaseUrl();
  const loginRes = login(root);
  if (loginRes.status !== 200) {
    throw new Error(
      `setup login failed: status=${loginRes.status} body=${String(loginRes.body).slice(0, 200)}`
    );
  }
  const token = getAccessToken(loginRes);
  if (!token) {
    throw new Error("setup: no accessToken in login response");
  }
  return { authMode: "setup", token, root };
}

const reportP95 = `p(95)<${reportScenarioThresholdMs()}`;
const loginP95 = `p(95)<${loginThresholdMs()}`;

export const options = {
  scenarios: buildScenarios(),
  thresholds: {
    http_req_failed: ["rate<0.5"],
    http_req_duration: [reportP95],
    "http_req_duration{name:report_delivery}": [reportP95],
    "http_req_duration{name:report_vehicle}": [reportP95],
    "http_req_duration{name:report_driver}": [reportP95],
    "http_req_duration{name:login}": [loginP95],
  },
};

export default function (data) {
  const root = data.root || requireBaseUrl();
  const variant = (__ENV.K6_REPORT_VARIANT || "r2").toLowerCase();

  if (data.authMode === "per_iteration") {
    const loginRes = login(root);
    check(loginRes, { "login status 200": (r) => r.status === 200 });
    if (loginRes.status !== 200) return;
    const accessToken = getAccessToken(loginRes);
    if (!accessToken) return;
    runReports(root, accessToken, variant);
    return;
  }

  runReports(root, data.token, variant);
}

function fmtMs(x) {
  return x != null && typeof x === "number" ? x.toFixed(2) : "n/a";
}

function formatTrend(label, metric) {
  if (!metric || !metric.values) {
    return `${label}: (no samples)\n`;
  }
  const v = metric.values;
  return (
    `${label}: avg=${fmtMs(v.avg)}ms med=${fmtMs(v.med)}ms ` +
    `p(90)=${fmtMs(v["p(90)"])}ms p(95)=${fmtMs(v["p(95)"])}ms max=${fmtMs(v.max)}ms\n`
  );
}

export function handleSummary(data) {
  const m = data.metrics;
  const lines = ["\n========== MER-295: report endpoint timings (http_req_duration) ==========\n"];

  const tagged = [
    "http_req_duration{name:report_delivery}",
    "http_req_duration{name:report_vehicle}",
    "http_req_duration{name:report_driver}",
    "http_req_duration{name:login}",
  ];
  for (const k of tagged) {
    if (m[k]) {
      lines.push(formatTrend(k, m[k]));
    }
  }

  for (const key of Object.keys(m)) {
    if (
      key.startsWith("http_req_duration{") &&
      key.includes("report_") &&
      !tagged.includes(key)
    ) {
      lines.push(formatTrend(key, m[key]));
    }
  }

  lines.push("========================================================================\n");
  return { stdout: lines.join("") };
}
