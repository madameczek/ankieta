# Deployment

Two images, one host (D19): `/` is served by **polls-web** (Angular on nginx), `/api` by **polls-api** (ASP.NET Core).
Same origin, so no CORS and the session cookie just works. Decisions: `docs/decisions.md` (D19–D22, D37, D38).

## Layout

| Path | Content |
| --- | --- |
| `polls-api.Dockerfile` (+ `.dockerignore`) | API image, build context `src/polls` |
| `polls-web.Dockerfile` (+ `.dockerignore`), `nginx.conf` | SPA image, build context `src/polls-angular`, `deploy/` as named context `deploy` |
| `build.sh` | builds both images; with a registry also tags and pushes |
| `kube/` | shared kustomize base: Deployments, Services (no route) for namespace `polls-pkp`; the namespace itself is not created (D55) |
| `docker-desktop/` | overlay for Docker Desktop Kubernetes and its `deploy.sh` (see below, D54) |

Other environment overlays are kept outside the repository. An overlay must provide:

1. **A route** into the cluster: path `/api` → Service `polls-api:8080`, everything else → `polls-web:8080`, one hostname.
   TLS should terminate before the API (the session cookie is `Secure`); the route must pass `X-Forwarded-For`. If another proxy (CDN) sits in front of the gateway and sends the client IP in its own header, set `RateLimiting__ClientIpHeader` to that header name in the API secret – otherwise rate limiting counts the CDN's addresses, not the clients'.
2. **Images**: `images:` entries for `polls-api` and `polls-web` with registry and tag.
3. **Secret `polls-api-secrets`** with these keys:

   | Key | Value |
   | --- | --- |
   | `ConnectionStrings__Polls` | SQL Server connection string with the app's **DML-only** login (D13) |
   | `Graylog__Address`, `Graylog__Port` | GELF/UDP input (D16); empty address = no Graylog |
   | `EmailApi__Url`, `EmailApi__ApiKey`, `EmailApi__From` | mail API send endpoint, its `x-api-key`, sender address (D17); if the URL, sender or recipient is invalid, emails are only logged |
   | `EmailApi__To` | recipient of the threshold warning (D14) |

   Optional overrides use the same `Section__Key` form, e.g. `RateLimiting__Create__PermitLimit`.
4. **Namespace `polls-pkp`** must already exist – the manifests only target it, they never create or delete it (D55),
   so `kubectl delete -k <overlay>` removes the app but keeps the namespace and its pull secret.
5. **Pull secret `ghcr-pull`** (type `kubernetes.io/dockerconfigjson`) must exist in the namespace; the base Deployments
   reference it. It is managed outside these manifests (one per namespace) and rotated in place – no re-apply needed (D53).

## Steps

1. **Schema** (only when `db/migrations.sql` changed), with the DDL login – see `docs/database.md`:
   `sqlcmd -I -S <server> -d <database> -U <ddl-login> -i src/polls/db/migrations.sql`
2. **Images**: `deploy/build.sh <registry> <tag>` (from the repo root; Docker with BuildKit).
3. **Check the namespace** (and its pull secret) before applying – apply fails with `namespaces "polls-pkp" not found` otherwise:
   `kubectl --context <context> get namespace polls-pkp` and `kubectl --context <context> -n polls-pkp get secret ghcr-pull`
4. **Render and check**: `kubectl kustomize <overlay>`
5. **Apply**: `kubectl --context <context> apply -k <overlay>`
6. **Verify**: `kubectl -n polls-pkp get pods` (both `Running`, ready `1/1`); `curl https://<host>/api/health/ready` → `Healthy`.

## Docker Desktop (local Kubernetes)

1. Schema as in step 1 above (the overlay uses the shared SQL Server, D36).
2. `cp deploy/docker-desktop/secrets.env.example deploy/docker-desktop/secrets.env` and fill it in (never committed).
3. `deploy/docker-desktop/deploy.sh` – builds `polls-api:dev` / `polls-web:dev`, applies the overlay with
   `--context docker-desktop` (never the current context) and restarts the pods, since every build reuses the `:dev` tag.
   It creates the `polls-pkp` namespace when missing (local cluster only; nothing else manages it there).
   `--no-build` skips the image build.
4. Open http://localhost:30090 (NodePort). `/api` is proxied by the web pod's nginx; there is no gateway.

Differences from a cluster overlay: plain http, so `Session__SecureCookie=false`; local images, so no `ghcr-pull`;
no client IP header (no edge proxy).

## Runtime notes

- One replica each (D38); the API's rate limiter is in memory, Data Protection keys are stored in the DB (D22).
- Probes: API `/api/health/live` (startup, liveness) and `/api/health/ready` (readiness, includes the DB); web `/healthz`.
- Containers run as non-root with a read-only root filesystem; `/tmp` is an `emptyDir`.
- Logs: console (`kubectl logs`) and Graylog (D16).
