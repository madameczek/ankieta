# Implementation plan – polls

Budget: **8 h**. Must-have tasks ≈ 6.25 h, the rest is spare time for the "could" list, in order.
One branch per task, merged to `main` when its acceptance criteria pass. References: `docs/decisions.md` (Dn), `docs/api.md`, `docs/ui.md`.

## Must

| # | Task | Branch | Est. | Acceptance criteria |
| --- | --- | --- | --- | --- |
| T1 | Backend scaffold | `feature/backend-scaffold` | 0.75 h | `polls.slnx`, `global.json` (MTP), `Directory.Build.props` (code style in build), `Directory.Packages.props`; `polls.Api` host with `Result<T>` → `ProblemDetails`, handler/validator/endpoint scanning, Serilog (Graylog + console in dev, D16), OpenAPI + Scalar at `/api/docs` (D39), health live/ready (D38); test projects + one NetArchTest rule; `dotnet build` and `dotnet test` green |
| T2 | Schema | `feature/schema` | 1 h | EF entities + configurations matching the schema table (D3–D9, D31); soft-delete query filters; `modified_at` interceptor (D34); initial migration + committed idempotent script (D13); Testcontainers fixture (D27) with schema tests: one owner per poll, one answer per user and question, empty answer rejected, unique `sort_order` among non-deleted questions only |
| T3 | Session and users | `feature/users` | 0.75 h | Protected persistent cookie (D10, D44), Data Protection keys in the DB (D22); `/session` GET/POST/DELETE, `/users` GET (search + paging) / POST; user thresholds with the mail stub (D14, D17, D43); rate limiter (D15); handler tests for every error code |
| T4 | Polls | `feature/polls` | 1.25 h | All poll, question, reorder, participant, answer and results endpoints from `docs/api.md`; access rules D24–D26, D41; poll thresholds; renumbering without temporary duplicates (D9); handler tests for every error code |
| T5 | Frontend | `feature/frontend` | 1.75 h | Angular workspace (standalone, zoneless, Material 3 following system theme, D18, D47); proxy to `/api`; session store + startup call, guards, error interceptor + Polish messages (D28); screens: users, my polls, poll tabs (answers, editor with drag & drop, participants, results) per `docs/ui.md`; Vitest specs for the interceptor, guard and session store (D48) |
| T6 | Deployment | `feature/deploy` | 0.75 h | `deploy/polls-api.Dockerfile`, `deploy/polls-web.Dockerfile` (nginx), shared kustomize base, private environment overlay, build/push scripts, `deploy/README.md` (D19–D21, D37, D38); app reachable through the cluster route, `/api/health/ready` OK |

Total: 6.25 h.

## Could (spare time, in this order)

| # | Task | Est. | Notes |
| --- | --- | --- | --- |
| C1 | UI extras | 0.5 h | keyboard move up/down for questions, `beforeunload` prompt (D46), "answered X of Y" counters in the poll list, participants and results. The API returns the counts already in T3/T4 |
| C2 | E2E smoke | 0.75 h | Playwright main flow against a throwaway SQL Server container (D49) |
| C3 | docker-desktop overlay | 0.5 h | `deploy/docker-desktop/` for local Kubernetes (D21) |

## Order and dependencies

T1 → T2 → T3 → T4 → T5 → T6 → C1 → C2 → C3.
T5 can start after T3 (users screens), but runs after T4 in a single-developer flow.
