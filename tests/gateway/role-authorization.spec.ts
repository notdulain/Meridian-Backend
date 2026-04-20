import { expect, test } from '@playwright/test';
import {
  authHeader,
  gatewayRoutes,
  loginAsAdmin,
  loginAsDispatcher,
  loginAsUser
} from '../helpers/auth-helper';

test.describe('ApiGateway role authorization', () => {
  test('admin-only user provisioning route rejects dispatcher and regular user tokens', async ({ request }) => {
    const dispatcherSession = await loginAsDispatcher(request);
    const userSession = await loginAsUser(request);

    const dispatcherResponse = await request.post(gatewayRoutes.users.driverAccounts, {
      headers: authHeader(dispatcherSession.accessToken),
      data: {}
    });

    const userResponse = await request.post(gatewayRoutes.users.driverAccounts, {
      headers: authHeader(userSession.accessToken),
      data: {}
    });

    expect(dispatcherResponse.status()).toBe(403);
    expect(userResponse.status()).toBe(403);
  });

  test('dashboard summary allows admin and dispatcher roles but rejects a regular user token', async ({ request }) => {
    const adminSession = await loginAsAdmin(request);
    const dispatcherSession = await loginAsDispatcher(request);
    const userSession = await loginAsUser(request);

    const adminResponse = await request.get(gatewayRoutes.dashboard.summary, {
      headers: authHeader(adminSession.accessToken)
    });

    const dispatcherResponse = await request.get(gatewayRoutes.dashboard.summary, {
      headers: authHeader(dispatcherSession.accessToken)
    });

    const userResponse = await request.get(gatewayRoutes.dashboard.summary, {
      headers: authHeader(userSession.accessToken)
    });

    expect([200, 503]).toContain(adminResponse.status());
    expect([200, 503]).toContain(dispatcherResponse.status());
    expect(userResponse.status()).toBe(403);
  });
});
