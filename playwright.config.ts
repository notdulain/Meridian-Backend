import { defineConfig } from '@playwright/test';
import { config as loadEnv } from 'dotenv';

loadEnv();

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.API_GATEWAY_URL ?? 'http://localhost:5050',
    extraHTTPHeaders: {
      Accept: 'application/json'
    }
  }
});
