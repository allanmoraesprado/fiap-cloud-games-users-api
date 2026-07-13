# FIAP Cloud Games — Users API

Identity microservice for the FIAP Cloud Games Phase 2 platform. Owns user
registration, authentication, **JWT issuance**, roles, and user administration.

> **Milestone status: M1.** Independent .NET 8 API against the `fcg_users`
> database. **No Kafka yet** — `UserCreatedEvent` publishing is added in M2.
> No Kubernetes manifests yet.

Part of the five-repository solution (`users-api`, `catalog-api`,
`payments-api`, `notifications-api`, `orchestration`).

---

## Responsibilities

- Register users (`POST /api/auth/register`).
- Authenticate and issue JWTs (`POST /api/auth/login`).
- Role-based authorization (`User`, `Admin`).
- User administration (`GET/DELETE /api/users`) — Admin only.

UsersAPI is the **sole JWT issuer**. CatalogAPI (from M3) validates those tokens
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
| GET | `/swagger` | public | Swagger UI |

Seeded users: `admin@fcg.com / Admin@123` (Admin), `user@fcg.com / User@123` (User).

---

## Environment variables

| Variable | Meaning | Local default |
|---|---|---|
| `ConnectionStrings__Postgres` | `fcg_users` connection string | `Host=localhost;Port=5432;Database=fcg_users;Username=fcg;Password=fcg` |
| `JWT__SECRETKEY` | Shared symmetric signing key (≥32 chars) | dev placeholder |
| `JWT__ISSUER` | Token issuer | `FiapCloudGames` |
| `JWT__AUDIENCE` | Token audience | `FiapCloudGames` |
| `JWT__EXPIRATIONMINUTES` | Token lifetime | `120` |

Only local/development placeholders are committed. Provide a real secret via
environment variable in any shared environment.

---

## Run locally (uses the M0 Postgres)

1. Start the M0 infrastructure (from the orchestration repo): `docker compose up -d`
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
