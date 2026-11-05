# Decisions – polls (ankieta)

Planning Q&A session, 2026-11-05. Source spec: `docs/ankieta-zadanie.md`.
Decisions are numbered; reference them as D1, D2, ….
Environment-specific infrastructure (hosts, addresses, credentials, cluster routing) is kept outside the repo: see `docs/internal/infra.md` and `deploy/k3s/README.md`, which are local only.

## Domain and naming

- **D1 Naming.** "pool" in the spec means *poll* (ankieta). Code, DB, routes and folders use `poll`/`polls`. The k8s namespace stays `ankieta`.
- **D2 Answers are free text only.** No question types or choice options.
- **D3 One answer per user per question.** `answers.id_user` FK → `users.id`, `UNIQUE(id_question, id_user)`. The app checks that the user is assigned to the poll.
- **D4 Ownership lives in `user_poll.is_owner`.** There is no `polls.owned_by`. A filtered unique index allows exactly one owner per poll. The app creates the poll and its owner row in one transaction.
- **D5 The owner is also a participant** and can answer their own poll.
- **D6 Users have a unique name and are never deleted.**
- **D7 Polls have `title` (required) and `description` (optional).**
- **D8 Soft delete** (`deleted_at`) on `polls` and `questions` only. Answers and assignments are hidden through their parent. EF global query filters hide deleted rows. FKs use `NO ACTION` (no cascades). Editing a question's text keeps its answers.
- **D9 Question order uses drag & drop.** `PUT /api/polls/{id}/questions/order` takes the ordered list of question ids, and the server renumbers in one transaction. Unique `(id_poll, sort_order)` filtered on `deleted_at IS NULL`. The renumbering must avoid temporary duplicates, e.g. by moving the values to negative first or doing it in a single set-based statement.

## Identity

- **D10 Cookie identity, no authentication (by design).** The API sets an HttpOnly cookie, protected by Data Protection, holding the current user id:
  - `POST /api/users` creates a user and enters their context
  - `POST /api/session/{userId}` is "become user"
  - `DELETE /api/session` is "leave", back to visitor mode
  - A missing, invalid or unknown cookie means *visitor*.
  - Implemented with ASP.NET's cookie authentication scheme (it selects the user; there is no credential check). Every request checks that the cookie's user exists; if not, the cookie is cleared.
- **D11 Visitor** can only list users, create a user and become a user. All poll endpoints require a current user.

## Database

- **D12 EF Core code-first.** The DbContext is used directly in handlers, without a repository or other abstraction over EF.
- **D52 The EF model uses the Fluent API only**, no data annotations on entities. `OnModelCreating` calls `ApplyConfigurationsFromAssembly`, with one sealed `IEntityTypeConfiguration<T>` per entity in its module's `Infrastructure` folder. Max lengths shared by EF and validators come from constants in the module's `Domain` (e.g. `PollLimits.TitleMaxLength`). Validators check commands; EF config defines the schema.
- **D13 Migrations are applied manually.** `dotnet ef migrations script --idempotent`, committed as `src/polls/db/migrations.sql`, reviewed and run against the target SQL Server. This script is also the schema-proof artifact. Commands: `docs/database.md`. The app's DB login has DML rights only.

### Schema (target)

| table | columns | constraints |
| --- | --- | --- |
| `users` | `id` int identity, `name` nvarchar(100), `created_at` datetime2 | PK; UNIQUE(`name`) |
| `polls` | `id`, `title` nvarchar(200), `description` nvarchar(2000) NULL, `created_at`, `modified_at`, `deleted_at` NULL | PK |
| `user_poll` | `id_poll`, `id_user`, `is_owner` bit | PK(`id_poll`,`id_user`); FKs; UNIQUE(`id_poll`) WHERE `is_owner = 1` |
| `questions` | `id`, `id_poll`, `sort_order` int, `question` nvarchar(1000), `modified_at`, `deleted_at` NULL | PK; FK; UNIQUE(`id_poll`,`sort_order`) WHERE `deleted_at IS NULL` |
| `answers` | `id`, `id_question`, `id_user`, `answer` nvarchar(4000), `modified_at` | PK; FKs; UNIQUE(`id_question`,`id_user`) |
| `threshold_warnings` | `resource` varchar(20) (`users`/`polls`), `sent_at` | PK(`resource`), see D14 |

Changes from the spec: `order` becomes `sort_order` (`ORDER` is a reserved word). Dates use the `_at` suffix everywhere and are stored in UTC.

## Side functions

- **D14 Thresholds are checked on create.** Before inserting a user or poll, the app counts **all** rows, soft-deleted ones included.
  - Warning: 2000 users / 3000 polls. The app emails the admin once through the mail API (D17) and records it in `threshold_warnings`.
  - Lockout: 3000 users / 5000 polls. Create returns a `ProblemDetails` error.
  - Limits and the admin email are set in config. A race between count and insert is acceptable for a demo.
- **D15 Rate limiting.** The built-in ASP.NET RateLimiter uses a fixed window per client IP, read from forwarded headers. A stricter policy applies to create endpoints. Values are set in config.
- **D16 Logging uses Serilog to Graylog (GELF)**, in every environment, development included. GELF over UDP; the sink is off while `Graylog:Address` is empty. Each event carries `Environment` and `AppName` (`Polls-API`); the GELF host is the machine (pod) name. Console output is on in every environment too (`kubectl logs`, fallback). A bootstrap logger covers startup errors. Request log: one line per request, health probes hidden unless they fail, 5xx as Error.
- **D17 Email goes through an external HTTP mail API.** The mail API sits behind one small `IEmailSender` interface in `Infrastructure`, called with a typed `HttpClient`. The API contract is still open, so for now `IEmailSender` is **stubbed**: a stub implementation logs the message (to, subject, body) instead of sending it, in all environments, until the contract is known.

## Frontend

- **D18 Angular** (latest), standalone components with signals, zoneless, Angular Material, CDK drag-drop. Served by nginx in a container.

## Deployment

- **D19 One host, path routing.** `/` goes to the Angular nginx and `/api` to the API. Same origin, so no CORS.
- **D20 Routing into the cluster is defined per environment overlay.** The base manifests contain only Deployments and Services; each overlay adds its own route or ingress to implement D19.
- **D21 Plain YAML + kustomize.** `deploy/<app>.Dockerfile` per image (`polls-api`, `polls-web`), a shared base (e.g. `deploy/kube/`), and the `deploy/docker-desktop/` overlay for local Kubernetes. Entry point `deploy/README.md`. Other environment overlays are not in the repo. Secrets (connection string, mail API credentials, Data Protection keys) are never committed.
- **D22 Data Protection keys must be shared** across API replicas, stored in the DB or a k8s secret, so cookies survive restarts.

## Backend layout

- **D23 One project `polls.Api`** with module folders `Users/` and `Polls/`, using `Domain`/`Core`/`Infrastructure`/`PublicApi` as needed, plus a shared `Infrastructure/` for the DbContext. Tests go in `polls.Api.Tests`, `polls.Architecture.Tests` and `polls.Tests.Common`.

## Access rules

- **D24 Participants see only their own answers.** Only the owner sees all answers.
- **D25 The owner can remove a participant.** The `user_poll` row is deleted. The participant's answers stay in the DB but are hidden. Re-adding the participant makes them visible again.
- **D26 Only the owner edits a poll's title, description and questions.**

## Testing

- **D27 Handler and integration tests run against real SQL Server via Testcontainers.** Migrations are applied to the container, so the schema itself is tested, filtered indexes included. Docker is required to run these tests.

## UX and behaviour

- **D28 The UI is Polish only**, with hardcoded strings and no i18n. API error codes are English and Angular maps them to Polish messages.
- **D29 Answers are saved one question at a time.** `PUT /api/polls/{id}/questions/{qid}/answer` upserts the row and sets `modified_at`. Sending empty text deletes the row (D31).
- **D30 Last write wins.** No rowversion or optimistic concurrency.
- **D31 No row means "not answered".** `answers.answer` is NOT NULL with `CHECK (LEN(answer) > 0)`.
- **D32 The user list uses server-side search and paging.** `GET /api/users?search=&page=&pageSize=`. The participant picker uses autocomplete over it.
- **D33 Results are shown per question.** For each question in `sort_order`: user name, answer, `modified_at`. Owner only (D24).
- **D34 `polls.modified_at` changes on edits to the poll or its questions** (add, edit, reorder, soft delete). Answers and assignments don't change it. It is set centrally, e.g. by a SaveChanges interceptor.
- **D35 One poll list per user.** Owned and participating polls appear together, with a role badge and "answered X of Y", sorted by `modified_at` desc.

## Development

- **D36 Local development uses a shared SQL Server** (there is no local DB container). The connection string, with its credentials, lives in **user secrets**; `appsettings.Development.json` holds only non-secret settings and is committed. Angular `ng serve` proxies `/api` to the local API (same origin, as in prod).

## Operations

- **D37 Images are built and pushed by local scripts** in `deploy/`, then deployed with `kubectl apply -k <overlay>`, e.g. `deploy/docker-desktop`. No CI pipeline yet.
- **D38 One replica each for the API and the front.** Probes: `/api/health/live` and `/api/health/ready` (ready checks the DB). One replica keeps the in-memory rate limiter correct.
- **D39 OpenAPI + Scalar are exposed in every environment** at `/api/docs`.

## API

- **D40 The full API surface is in `docs/api.md`.** Routes are unversioned under `/api`. Errors are `ProblemDetails` with a stable `code`.
- **D41 Users who aren't participants get 404** for a poll and everything under it. A participant calling an owner-only endpoint gets 403.
- **D42 Ids are `int identity` and exposed as-is.**
- **D43 Lockout on create returns 503** with code `limit.users_reached` / `limit.polls_reached`.
- **D44 The session cookie is persistent with a 1-year sliding expiry.** HttpOnly, SameSite=Lax, Secure outside Development.

## UI

- **D45 The UI is described in `docs/ui.md`.** Owner screens for a poll are router tabs under `/polls/:id` (answers, editor, participants, results); a participant sees only "my answers".
- **D46 Unsaved answers are protected** by a `canDeactivate` confirm dialog plus `beforeunload`.
- **D47 Material 3 theme that follows the system light/dark setting**, no toggle.
- **D48 Frontend tests:** Vitest unit tests + Playwright e2e for the main flows.
- **D49 E2E runs use a throwaway SQL Server container.** A script starts SQL Server in Docker, applies the idempotent migration script, runs the API against it, runs Playwright, then removes the container. The shared dev DB is never touched.
- **D50 "My polls" uses Material list rows** (`mat-nav-list`).
- **D51 "Become user" switches immediately**, without confirmation; a snackbar names the new user.

## Open items

- [ ] Rate-limit numbers (D15): defaults per client IP are 300 requests/min overall and 10 creates/min; confirm or change
- [ ] Mail API request contract (D17)
- [ ] docker-desktop overlay: how traffic is routed (D19, D20) and which SQL Server it uses
- [ ] Environment values (hosts, credentials, routing): tracked in the local-only files named at the top
