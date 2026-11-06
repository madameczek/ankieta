# E2E smoke test (C2, D49)

One Playwright test of the main flow (`docs/ui.md`): create two users → the owner creates a poll with two
questions and adds the participant → both answer → the owner sees the counters and the results.

## What a run does
1. **Global setup** (`db-setup.ts`): applies `src/polls/db/migrations.sql` to the test database with `sqlcmd -I`
   (idempotent, D13); with `E2E_RESET_DB=true` it then deletes all rows (users, polls, questions, participants,
   answers, threshold warnings). The reset refuses to run unless the database name contains `e2e` or `test`.
2. **Servers** (`playwright.config.ts`): starts the API (`dotnet run`, environment `E2E`, own port, no user
   secrets, no mail, no Graylog, relaxed rate limits) and `ng serve` with `proxy.e2e.mjs` (`/api` → that API).
3. **Test** (`main-flow.e2e.ts`) in headless Chromium; names are unique per run, so it also works without a reset.

## Setup (once)
- A SQL Server database of its own, never the app database. A login that may create tables (DDL) – the same or
  another login is used by the API (DML).
- `cp e2e/.env.example e2e/.env` and fill it in (`.env` is never committed).
- `sqlcmd` on the PATH, .NET SDK, Node, then in `src/polls-angular`: `npm ci` and `npx playwright install chromium`.

## Run (in `src/polls-angular`)
- `npm run e2e` – about 20 s (first run longer: build of the API and the Angular dev server).
- On a failure: screenshot, trace and page snapshot in `e2e/test-results/`;
  `npx playwright show-trace e2e/test-results/<test>/trace.zip`.
