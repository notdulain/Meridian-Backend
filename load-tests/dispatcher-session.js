/**
 * k6: login → list deliveries → create assignment (QA API Gateway paths).
 *
 * Required env:
 *   BASE_URL          e.g. https://ca-api-gateway....azurecontainerapps.io (no trailing slash)
 *   K6_LOGIN_EMAIL
 *   K6_LOGIN_PASSWORD
 *
 * Optional:
 *   K6_DISPATCHER_PROFILE     qa | demo (default: qa)
 *   K6_DISPATCHER_HTTP_TIMEOUT
 *   K6_DISPATCHER_HTTP_DEBUG  full | headers | body
 *   K6_DISPATCHER_P95_MS      default: 10000
 *   K6_DISPATCHER_MAX_FAIL_RATE default: 0.5
 *   K6_LOGIN_VUS / K6_LOGIN_DURATION
 *   K6_DELIVERIES_VUS / K6_DELIVERIES_DURATION
 *   K6_ASSIGNMENT_VUS / K6_ASSIGNMENT_DURATION
 *   DELIVERY_ID  (default 1)  or DELIVERY_IDS=1,2,3
 *   VEHICLE_ID   (default 1)  or VEHICLE_IDS=1,2,3
 *   DRIVER_ID    (default 1)  or DRIVER_IDS=1,2,3
 *   K6_ASSIGNMENT_NOTES
 */
import http from "k6/http";
import { check, sleep } from "k6";

const base = () => (__ENV.BASE_URL || "").replace(/\/$/, "");
const profile = (__ENV.K6_DISPATCHER_PROFILE || "qa").toLowerCase();

function mustEnv(name) {
  const v = __ENV[name];
  if (!v) throw new Error(`Missing env ${name}`);
  return v;
}

function positiveInt(raw, fallback) {
  const parsed = parseInt(raw || "", 10);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function failRateThreshold() {
  const raw = __ENV.K6_DISPATCHER_MAX_FAIL_RATE;
  const parsed = raw != null && raw !== "" ? parseFloat(raw) : 0.5;
  if (Number.isNaN(parsed) || parsed < 0 || parsed > 1) {
    return "rate<0.5";
  }

  return `rate<${parsed}`;
}

function durationThreshold() {
  const parsed = positiveInt(__ENV.K6_DISPATCHER_P95_MS, 10000);
  return `p(95)<${parsed}`;
}

function scenarioValue(name, qaFallback, demoFallback) {
  return __ENV[name] || (profile === "demo" ? demoFallback : qaFallback);
}

function httpParams() {
  return {
    timeout: __ENV.K6_DISPATCHER_HTTP_TIMEOUT || (profile === "demo" ? "15s" : "60s"),
    http2: false,
  };
}

function parseIdPool(poolEnvName, singleEnvName, fallback) {
  const pool = (__ENV[poolEnvName] || "")
    .split(",")
    .map((value) => parseInt(value.trim(), 10))
    .filter((value) => Number.isInteger(value) && value > 0);

  if (pool.length > 0) {
    return pool;
  }

  return [positiveInt(__ENV[singleEnvName], fallback)];
}

function pickId(poolEnvName, singleEnvName, fallback) {
  const pool = parseIdPool(poolEnvName, singleEnvName, fallback);
  return pool[(__VU + __ITER) % pool.length];
}

export const options = {
  ...( __ENV.K6_DISPATCHER_HTTP_DEBUG ? { httpDebug: __ENV.K6_DISPATCHER_HTTP_DEBUG } : {}),
  scenarios: {
    login: {
      executor: "constant-vus",
      exec: "loginScenario",
      vus: positiveInt(scenarioValue("K6_LOGIN_VUS", "3", "1"), 3),
      duration: scenarioValue("K6_LOGIN_DURATION", "45s", "15s"),
    },
    fetch_deliveries: {
      executor: "constant-vus",
      exec: "fetchDeliveriesScenario",
      vus: positiveInt(scenarioValue("K6_DELIVERIES_VUS", "3", "1"), 3),
      duration: scenarioValue("K6_DELIVERIES_DURATION", "45s", "15s"),
    },
    assign_vehicle: {
      executor: "constant-vus",
      exec: "assignVehicleScenario",
      vus: positiveInt(scenarioValue("K6_ASSIGNMENT_VUS", "3", "1"), 3),
      duration: scenarioValue("K6_ASSIGNMENT_DURATION", "45s", "15s"),
    },
  },
  thresholds: {
    http_req_failed: [failRateThreshold()],
    http_req_duration: [durationThreshold()],
  },
};

function login(root) {
  return http.post(
    `${root}/api/auth/login`,
    JSON.stringify({
      email: mustEnv("K6_LOGIN_EMAIL"),
      password: mustEnv("K6_LOGIN_PASSWORD"),
    }),
    {
      ...httpParams(),
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

export function loginScenario() {
  const root = requireBaseUrl();
  const loginRes = login(root);
  check(loginRes, {
    "login status 200": (r) => r.status === 200,
  });
  sleep(1);
}

export function fetchDeliveriesScenario() {
  const root = requireBaseUrl();
  const loginRes = login(root);
  if (loginRes.status !== 200) return;
  const accessToken = getAccessToken(loginRes);
  if (!accessToken) return;
  const auth = { Authorization: `Bearer ${accessToken}` };
  const listRes = http.get(`${root}/delivery/api/deliveries`, {
    ...httpParams(),
    headers: { ...auth, Accept: "application/json" },
    tags: { name: "list_deliveries" },
  });
  check(listRes, {
    "deliveries status 200": (r) => r.status === 200,
  });
  sleep(1);
}

export function assignVehicleScenario() {
  const root = requireBaseUrl();
  const loginRes = login(root);
  if (loginRes.status !== 200) {
    return;
  }
  const accessToken = getAccessToken(loginRes);
  if (!accessToken) return;
  const auth = { Authorization: `Bearer ${accessToken}` };
  const deliveryId = pickId("DELIVERY_IDS", "DELIVERY_ID", 1);
  const vehicleId = pickId("VEHICLE_IDS", "VEHICLE_ID", 1);
  const driverId = pickId("DRIVER_IDS", "DRIVER_ID", 1);

  const assignRes = http.post(
    `${root}/assignment/api/assignments`,
    JSON.stringify({
      deliveryId,
      vehicleId,
      driverId,
      notes: __ENV.K6_ASSIGNMENT_NOTES || "k6 dispatcher-session",
    }),
    {
      ...httpParams(),
      headers: { ...auth, "Content-Type": "application/json" },
      tags: { name: "create_assignment" },
    }
  );

  check(assignRes, {
    "assignment 2xx or 409": (r) =>
      (r.status >= 200 && r.status < 300) || r.status === 409,
  });

  sleep(1);
}

function fmtMs(value) {
  return value != null && typeof value === "number" ? value.toFixed(2) : "n/a";
}

export function handleSummary(data) {
  const durationMetric = data.metrics.http_req_duration;
  const failureMetric = data.metrics.http_req_failed;
  const lines = [
    "\n========== ASE dispatcher smoke summary ==========\n",
    `profile: ${profile}\n`,
    `BASE_URL: ${base() || "(unset)"}\n`,
    `http_req_failed threshold: ${failRateThreshold()}\n`,
    `http_req_duration threshold: ${durationThreshold()}\n`,
  ];

  if (failureMetric?.values?.rate != null) {
    lines.push(`http_req_failed: ${(failureMetric.values.rate * 100).toFixed(2)}%\n`);
  }

  if (durationMetric?.values) {
    lines.push(
      `http_req_duration: avg=${fmtMs(durationMetric.values.avg)}ms ` +
      `p(95)=${fmtMs(durationMetric.values["p(95)"])}ms max=${fmtMs(durationMetric.values.max)}ms\n`
    );
  }

  lines.push("==================================================\n");
  return { stdout: lines.join("") };
}
