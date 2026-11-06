// ng serve proxy for the E2E run: /api → the API started by Playwright (port from e2e/.env, set by playwright.config.ts)
export default {
  '/api': {
    target: `http://localhost:${process.env.E2E_API_PORT ?? 5180}`,
    secure: false,
    changeOrigin: false,
  },
};
