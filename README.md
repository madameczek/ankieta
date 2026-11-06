# Polls (ankieta)

A small demo app for simple polls. An owner creates a poll with questions and adds participants; every participant
answers on their own; the owner sees the results. There is no real authentication – the current user is picked
from a list and kept in a cookie. The point of the project is to prove that the database schema for polls is right.

Task: [`docs/ankieta-zadanie.md`](docs/ankieta-zadanie.md) ("pools" there means *polls*).

## Stack
- **API** – .NET 10, ASP.NET Core Minimal APIs, EF Core + SQL Server, Serilog → Graylog, OpenAPI/Scalar at `/api/docs`
- **Frontend** – Angular 22 (standalone, zoneless, signals), Angular Material 3, Polish UI
- **Deployment** – two images (`polls-api`, `polls-web` on nginx), plain Kubernetes YAML + kustomize

## Repository
| Path | Content |
| --- | --- |
| `docs/` | plan, decisions, API, UI, database notes, schema diagrams |
| `src/polls/` | backend solution: API, tests, `db/migrations.sql` |
| `src/polls-angular/` | Angular app, unit tests, `e2e/` Playwright smoke test |
| `deploy/` | Dockerfiles, kustomize base, Docker Desktop overlay, build script |

## Documentation
- [`docs/plan.md`](docs/plan.md) – tasks and their status
- [`docs/decisions.md`](docs/decisions.md) – numbered decisions (D1…), referenced everywhere as Dn
- [`docs/api.md`](docs/api.md) – REST endpoints, access rules, error codes
- [`docs/ui.md`](docs/ui.md) – screens and UI behaviour
- [`docs/database.md`](docs/database.md) – migrations; schema diagram [`docs/db-schema.svg`](docs/db-schema.svg)
  (text version [`docs/db-schema.md`](docs/db-schema.md))
- [`deploy/README.md`](deploy/README.md) – images, overlays, deploy steps

## Run locally
Needs .NET 10 SDK, Node 24, a SQL Server database with the schema applied (`docs/database.md`).

1. API (in `src/polls`): put the connection string in user secrets once –
   `dotnet user-secrets --project polls.Api set "ConnectionStrings:Polls" "<connection string>"` – then
   `dotnet run --project polls.Api`
2. Frontend (in `src/polls-angular`): `npm ci`, then `npm start` and open http://localhost:4200 (`/api` is proxied to the API)

Or run both in Docker Desktop's Kubernetes: `deploy/docker-desktop/deploy.sh` (see `deploy/README.md`).

## Tests
- Backend (in `src/polls`): `dotnet test --solution polls.slnx` – handler, endpoint and schema tests against SQL Server
  in Testcontainers (Docker must run)
- Frontend unit tests (in `src/polls-angular`): `npx ng test --watch=false`
- E2E smoke (in `src/polls-angular`): `npm run e2e` – needs its own test database, see
  [`src/polls-angular/e2e/README.md`](src/polls-angular/e2e/README.md)
