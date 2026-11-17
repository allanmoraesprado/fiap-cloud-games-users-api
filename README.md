# FIAP Cloud Games — Users API

Identity microservice for the FIAP Cloud Games Phase 2 platform. Owns user
registration, authentication, **JWT issuance**, roles, and user administration.

Independent .NET 8 API. Owns the `fcg_users` database, issues JWTs, and publishes
**`UserCreatedEvent`** to Kafka (`fcg.users.created`) after a successful registration
(consumed by the Notifications Function since Phase 3). Runs standalone, via Docker Compose,
and on local Kubernetes (see `k8s/`); in Phase 3 it is reached through the **Kong API
Gateway** (`/api/auth/*` public, `/api/users/*` JWT-protected at the edge and here). For the
full system runbook (Compose + Kubernetes) and architecture docs, see the
**`fiap-cloud-games-orchestration`** repository.

Part of the six-repository solution (`users-api`, `catalog-api`, `payments-api`,
`notifications-function`, `notifications-api` [Phase 2 history], `orchestration`).

---

## Responsibilities

- Register users (`POST /api/auth/register`).
- Authenticate and issue JWTs (`POST /api/auth/login`).
- Role-based authorization (`User`, `Admin`).
- User administration (`GET/DELETE /api/users`) — Admin only.

UsersAPI is the **sole JWT issuer**. CatalogAPI validates those tokens
using the **same shared symmetric secret** — keep `Jwt` settings identical across
both services.

---

## Tech

.NET 8 · ASP.NET Core (Controllers) · EF Core 8 + Npgsql (PostgreSQL) ·
JWT Bearer · PBKDF2 password hashing · Swagger · Serilog · xUnit/Moq/FluentAssertions.

Single-project layout with internal folders: `Domain`, `Application`,
`Infrastructure`, `Controllers`, `Configuration`, `Middleware`.

---

## Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | public | Create a user → 201 `UserResponse` |
| POST | `/api/auth/login` | public | Authenticate → 200 `LoginResponse` (token) |
| GET | `/api/users` | Admin | List users |
| GET | `/api/users/{id}` | Admin | Get a user |
| DELETE | `/api/users/{id}` | Admin | Delete a user → 204 |
| GET | `/health` | public | Liveness probe → 200 |
| GET | `/metrics` | public (direct port only, not routed by Kong) | Prometheus metrics (Phase 3) |
| GET | `/swagger` | public | Swagger UI |

Seeded users: `admin@fcg.com / Admin@123` (Admin), `user@fcg.com / User@123` (User).

### Metrics (Phase 3)

`prometheus-net.AspNetCore` exposes the default HTTP metrics (`http_requests_received_total`,
`http_request_duration_seconds`, `http_requests_in_progress`) plus custom counters with
low-cardinality labels only (no ids, no e-mails):

| Metric | Labels | Meaning |
|---|---|---|
| `fcg_users_registrations_total` | — | Users registered successfully |
| `fcg_users_login_attempts_total` | `result` = `success` \| `failure` | Login attempts |
| `fcg_events_published_total` | `topic`, `result` = `success` \| `failure` | `UserCreatedEvent` publications to Kafka |

Scraped by Prometheus and shown in the Grafana "FCG Overview" dashboard (orchestration repo,
`docs/observability.md`).

### Events published

| Event | Topic | Key | When |
|---|---|---|---|
| `UserCreatedEvent` | `fcg.users.created` | `UserId` | After a successful `POST /api/auth/register` |

Publishing happens **after** the DB commit and failures are swallowed/logged — a
temporarily-unavailable broker does not fail the registration.

---

## Environment variables

| Variable | Meaning | Local default |
|---|---|---|
| `ConnectionStrings__Postgres` | `fcg_users` connection string | `Host=localhost;Port=5432;Database=fcg_users;Username=fcg;Password=fcg` |
| `JWT__SECRETKEY` | Shared symmetric signing key (≥32 chars) | dev placeholder |
| `JWT__ISSUER` | Token issuer | `FiapCloudGames` |
| `JWT__AUDIENCE` | Token audience | `FiapCloudGames` |
| `JWT__EXPIRATIONMINUTES` | Token lifetime | `120` |
| `Kafka__BootstrapServers` | Kafka bootstrap (host dev / `kafka:9092` in containers) | `localhost:29092` |
| `Kafka__UserCreatedTopic` | Topic for `UserCreatedEvent` | `fcg.users.created` |

Only local/development placeholders are committed. Provide a real secret via
environment variable in any shared environment.

---

## Run locally (uses the Compose Postgres)

1. Start the infrastructure (from the orchestration repo): `docker compose up -d`
   — the `fcg_users` database already exists.
2. Run the API:
   ```bash
   dotnet run --project src/UsersApi --urls http://localhost:8080
   ```
   On startup it applies the EF migration and seeds the baseline users.
3. Open `http://localhost:8080/swagger`.

## Test

```bash
dotnet test
```

## Docker

```bash
docker build -t fcg-users-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Postgres="Host=host.docker.internal;Port=5432;Database=fcg_users;Username=fcg;Password=fcg" \
  -e JWT__SECRETKEY="dev-only-change-me-please-min-32-characters-placeholder" \
  fcg-users-api
```

---

## Suggested Swagger flow

1. `POST /api/auth/login` with the **Admin** credentials → copy `token`.
2. Click **Authorize**, paste `Bearer <token>`.
3. `GET /api/users` → list. `GET /api/users/{id}` → one user.
4. Re-authorize with the **User** credentials → `GET /api/users` returns **403**.

---

## Cloud deployment (Phase 4: AKS + CI/CD)

- **Manifests**: `k8s/azure/` (Deployment, Service, ConfigMap) rendered by the pipeline with the ACR
  login server and the **commit SHA** as image tag. `k8s/` keeps the local (Docker Desktop) manifests.
- **Pipeline**: `.github/workflows/ci-cd.yml` calls the shared workflow of the orchestration repository
  (`dotnet-service.yml`): CI on every push/PR (build, tests, Docker build, Trivy scan); CD on `main` only
  (OIDC login to Azure, push to the private ACR, rolling update on AKS with automatic rollback, smoke test
  through the public Kong gateway).
- **Secrets**: none in the manifests. On AKS the Azure Key Vault CSI driver mounts the service's secrets as
  files (Workload Identity) and the API loads them as configuration when `SecretsPath` is set
  (`Configuration/RuntimeConfiguration.cs`; file `Jwt__SecretKey` -> key `Jwt:SecretKey`). Without
  `SecretsPath` (local runs) nothing changes.
- **Version**: the image carries `APP_VERSION` (commit SHA); every response has `X-App-Version`.
- Cloud-only configuration: `Seed__AdminPassword` comes from Key Vault and `Seed__UserPassword` is empty (no demo user). Locally the `Seed` section of `appsettings.json` keeps the development accounts.

Details: orchestration repository, `docs/phase4/` (CICD.md, KEYVAULT.md, PROVISIONING.md).
