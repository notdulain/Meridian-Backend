import { test } from '@playwright/test';
import {
  authHeader,
  createExpiredJwt,
  expectUnauthorized,
  gatewayRoutes,
  getTestEnvironment
} from '../helpers/auth-helper';

test.describe('ApiGateway JWT validation', () => {
  test('missing JWT is rejected on protected gateway routes', async ({ request }) => {
    const response = await request.get(gatewayRoutes.dashboard.summary);

    await expectUnauthorized(response);
  });

  test('malformed JWT is rejected on protected gateway routes', async ({ request }) => {
    const response = await request.get(gatewayRoutes.dashboard.summary, {
      headers: authHeader('not-a-valid-jwt')
    });

    await expectUnauthorized(response);
  });

  test('expired JWT is rejected on protected gateway routes', async ({ request }) => {
    test.skip(
      !getTestEnvironment().jwtSecret,
      'Set JWT_SECRET to generate a valid expired token for this gateway validation test.'
    );

    const response = await request.get(gatewayRoutes.dashboard.summary, {
      headers: authHeader(createExpiredJwt())
    });

    await expectUnauthorized(response);
  });
});
