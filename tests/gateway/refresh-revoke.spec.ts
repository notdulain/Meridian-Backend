import { expect, test } from '@playwright/test';
import {
  assertAuthTokens,
  loginAsAdmin,
  parseJson,
  refreshAccessToken,
  revokeRefreshToken
} from '../helpers/auth-helper';

test.describe('ApiGateway refresh and revoke', () => {
  test('refresh token flow returns a new token pair and rejects refresh token replay', async ({ request }) => {
    const originalSession = await loginAsAdmin(request);

    const refreshResponse = await refreshAccessToken(request, originalSession.refreshToken);
    expect(refreshResponse.status()).toBe(200);

    const refreshedSession = await parseJson<{
      accessToken: string;
      refreshToken: string;
      expiresIn: number;
    }>(refreshResponse);

    assertAuthTokens(refreshedSession);
    expect(refreshedSession.accessToken).not.toBe(originalSession.accessToken);
    expect(refreshedSession.refreshToken).not.toBe(originalSession.refreshToken);

    const replayResponse = await refreshAccessToken(request, originalSession.refreshToken);
    expect(replayResponse.status()).toBe(401);
  });

  test('revoked refresh token cannot be used again', async ({ request }) => {
    const session = await loginAsAdmin(request);

    const revokeResponse = await revokeRefreshToken(
      request,
      session.accessToken,
      session.refreshToken
    );

    expect(revokeResponse.status()).toBe(204);

    const refreshResponse = await refreshAccessToken(request, session.refreshToken);
    expect(refreshResponse.status()).toBe(401);
  });
});
