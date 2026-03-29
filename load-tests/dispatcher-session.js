/**
 * k6: login → list deliveries → create assignment (QA API Gateway paths).
 *
 * Required env:
 *   BASE_URL          e.g. https://ca-api-gateway....azurecontainerapps.io (no trailing slash)
 *   K6_LOGIN_EMAIL
 *   K6_LOGIN_PASSWORD
 *
 * Optional:
 *   DELIVERY_ID  (default 1)
 *   VEHICLE_ID   (default 1)
 *   DRIVER_ID    (default 1)
 *
 * Run:
 *   cd load-tests
 *   K6_LOGIN_EMAIL=... K6_LOGIN_PASSWORD=... k6 run dispatcher-session.js
 */
import http from "k6/http";
import { check, sleep } from "k6";

const base = () => (__ENV.BASE_URL || "").replace(/\/$/, "");

function mustEnv(name) {
  const v = __ENV[name];
  if (!v) throw new Error(`Missing env ${name}`);
  return v;
}

const httpParams = { timeout: "60s" };

export const options = {
  scenarios: {
    smoke: {
      executor: "constant-vus",
      vus: 3,
      duration: "45s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.5"],
    http_req_duration: ["p(95)<10000"],
  },
};

export default function () {
  const root = base();
  if (!root) throw new Error("Set BASE_URL");

  const loginRes = http.post(
    `${root}/api/auth/login`,
    JSON.stringify({
      email: mustEnv("K6_LOGIN_EMAIL"),
      password: mustEnv("K6_LOGIN_PASSWORD"),
    }),
    {
      ...httpParams,
      headers: { "Content-Type": "application/json" },
      tags: { name: "login" },
    }
  );

  check(loginRes, {
    "login status 200": (r) => r.status === 200,
  });

  if (loginRes.status !== 200) {
    return;
  }

  let accessToken;
  try {
    accessToken = JSON.parse(loginRes.body).accessToken;
  } catch {
    return;
  }
  if (!accessToken) return;

  const auth = { Authorization: `Bearer ${accessToken}` };

  const listRes = http.get(`${root}/delivery/api/deliveries`, {
    ...httpParams,
    headers: { ...auth, Accept: "application/json" },
    tags: { name: "list_deliveries" },
  });

  check(listRes, {
    "deliveries status 200": (r) => r.status === 200,
  });

  const deliveryId = parseInt(__ENV.DELIVERY_ID || "1", 10);
  const vehicleId = parseInt(__ENV.VEHICLE_ID || "1", 10);
  const driverId = parseInt(__ENV.DRIVER_ID || "1", 10);

  const assignRes = http.post(
    `${root}/assignment/api/assignments`,
    JSON.stringify({
      deliveryId,
      vehicleId,
      driverId,
      notes: "k6 dispatcher-session",
    }),
    {
      ...httpParams,
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
