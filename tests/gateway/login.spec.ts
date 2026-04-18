import { expect, test } from '@playwright/test';
import {
  assertAuthTokens,
  getAdminCredentials,
  gatewayRoutes,
  parseJson
} from '../helpers/auth-helper';

test.describe('ApiGateway login', () => {
  test('valid login returns accessToken, refreshToken, and expiresIn', async ({ request }) => {
    const credentials = getAdminCredentials();

    const response = await request.post(gatewayRoutes.auth.login, {
      data: {
        email: credentials.email,
        password: credentials.password
      }
    });

    expect(response.status()).toBe(200);

    const payload = await parseJson<{
      accessToken: string;
      refreshToken: string;
      expiresIn: number;
    }>(response);

    assertAuthTokens(payload);
  });

  test('invalid login is rejected cleanly', async ({ request }) => {
    const credentials = getAdminCredentials();

    const response = await request.post(gatewayRoutes.auth.login, {
      data: {
        email: credentials.email,
        password: `${credentials.password}-invalid`
      }
    });

    expect(response.status()).toBe(401);
    expect(await response.text()).not.toEqual('');
  });
});
