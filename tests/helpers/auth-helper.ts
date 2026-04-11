import { expect, type APIRequestContext, type APIResponse } from '@playwright/test';
import { createHmac, randomUUID } from 'node:crypto';

type Credentials = {
  email: string;
  password: string;
};

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
};

type TestEnvironment = {
  apiGatewayUrl: string;
  jwtIssuer: string;
  jwtAudience: string;
  jwtSecret?: string;
};

export const gatewayRoutes = {
  auth: {
    login: '/api/auth/login',
    refresh: '/api/auth/refresh',
    revoke: '/api/auth/revoke'
  },
  users: {
    me: '/api/users/me',
    driverAccounts: '/api/users/driver-accounts'
  },
  roles: {
    me: '/api/roles/me'
  },
  dashboard: {
    summary: '/api/dashboard/summary'
  }
} as const;

let cachedEnvironment: TestEnvironment | undefined;

function requireEnv(name: string): string {
  const value = process.env[name];

  if (!value) {
    throw new Error(`${name} is required for gateway regression tests.`);
  }

  return value;
}

export function getTestEnvironment(): TestEnvironment {
  if (cachedEnvironment) {
    return cachedEnvironment;
  }

  cachedEnvironment = {
    apiGatewayUrl: process.env.API_GATEWAY_URL ?? 'http://localhost:5050',
    jwtIssuer: process.env.JWT_ISSUER ?? 'meridian-gateway',
    jwtAudience: process.env.JWT_AUDIENCE ?? 'meridian-api',
    jwtSecret: process.env.JWT_SECRET
  };

  return cachedEnvironment;
}

export function getAdminCredentials(): Credentials {
  return {
    email: requireEnv('ADMIN_EMAIL'),
    password: requireEnv('ADMIN_PASSWORD')
  };
}

export function getDispatcherCredentials(): Credentials {
  return {
    email: requireEnv('DISPATCHER_EMAIL'),
    password: requireEnv('DISPATCHER_PASSWORD')
  };
}

export function getUserCredentials(): Credentials {
  return {
    email: requireEnv('USER_EMAIL'),
    password: requireEnv('USER_PASSWORD')
  };
}

export function authHeader(accessToken: string): Record<string, string> {
  return {
    Authorization: `Bearer ${accessToken}`
  };
}

export async function parseJson<T>(response: APIResponse): Promise<T> {
  return (await response.json()) as T;
}

export function assertAuthTokens(payload: AuthTokens): void {
  expect(payload.accessToken).toBeTruthy();
  expect(payload.refreshToken).toBeTruthy();
  expect(payload.expiresIn).toBeGreaterThan(0);
  expect(payload.accessToken.split('.')).toHaveLength(3);
  expect(payload.refreshToken.length).toBeGreaterThan(20);
}

export async function login(
  request: APIRequestContext,
  credentials: Credentials
): Promise<AuthTokens> {
  const response = await request.post(gatewayRoutes.auth.login, {
    data: {
      email: credentials.email,
      password: credentials.password
    }
  });

  expect(response.status()).toBe(200);

  const payload = await parseJson<AuthTokens>(response);
  assertAuthTokens(payload);
  return payload;
}

export async function loginAsAdmin(request: APIRequestContext): Promise<AuthTokens> {
  return login(request, getAdminCredentials());
}

export async function loginAsDispatcher(request: APIRequestContext): Promise<AuthTokens> {
  return login(request, getDispatcherCredentials());
}

export async function loginAsUser(request: APIRequestContext): Promise<AuthTokens> {
  return login(request, getUserCredentials());
}

export async function refreshAccessToken(
  request: APIRequestContext,
  refreshToken: string
): Promise<APIResponse> {
  return request.post(gatewayRoutes.auth.refresh, {
    data: { refreshToken }
  });
}

export async function revokeRefreshToken(
  request: APIRequestContext,
  accessToken: string,
  refreshToken: string
): Promise<APIResponse> {
  return request.post(gatewayRoutes.auth.revoke, {
    headers: authHeader(accessToken),
    data: { refreshToken }
  });
}

export async function expectUnauthorized(response: APIResponse): Promise<void> {
  expect(response.status()).toBe(401);

  const authenticateHeader = response.headers()['www-authenticate'];
  expect(authenticateHeader?.toLowerCase()).toContain('bearer');
}

function toBase64Url(value: string | Buffer): string {
  return Buffer.from(value)
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}

export function createExpiredJwt(): string {
  const environment = getTestEnvironment();

  if (!environment.jwtSecret) {
    throw new Error('JWT_SECRET is required to generate an expired JWT.');
  }

  const now = Math.floor(Date.now() / 1000);
  const header = toBase64Url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = toBase64Url(
    JSON.stringify({
      sub: '0',
      email: 'expired-token@meridian.local',
      iss: environment.jwtIssuer,
      aud: environment.jwtAudience,
      jti: randomUUID(),
      iat: now - 120,
      exp: now - 60
    })
  );

  const unsignedToken = `${header}.${payload}`;
  const signature = createHmac('sha256', environment.jwtSecret)
    .update(unsignedToken)
    .digest();

  return `${unsignedToken}.${toBase64Url(signature)}`;
}
