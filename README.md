# Loom

Erasmus exchange course mapping: learning agreements, recognition and mapping schemes.

- `server/` – ASP.NET Core API (.NET 10, EF Core, PostgreSQL)
  - `Loom.Api`, `Loom.Application`, `Loom.Domain`, `Loom.Infrastructure` (DbContext, configurations, migrations)
  - `Loom.DevSeed` – applies migrations and loads reference + demo data for local development
- `client/` – Vue 3 + TypeScript + Pinia + Tailwind (Vite)
- `database/` – reference data (FER catalogue) and the one-time production baseline script

## Quick start (Docker)

Requirements: Docker Desktop (or Docker Engine + Compose v2).

```sh
cp .env.example .env        # optional, defaults work
docker compose up
```

Open http://localhost:5173 and pick a persona under **Dev login** (no Google account needed).

| What | Where |
|---|---|
| Web app | http://localhost:5173 |
| API + Swagger | http://localhost:5000/docs |
| Postgres | `localhost:5432`, db/user/password `loom` |
| DB browser (optional) | `docker compose --profile tools up adminer` → http://localhost:8081 |

The first start takes a few minutes (NuGet + pnpm install). After that, both the API (`dotnet watch`) and the web app (Vite) reload on file changes. On Windows/macOS, set `LOOM_FILE_POLLING=true` in `.env` if changes are not picked up.

### Common commands

```sh
docker compose up -d                      # start in the background
docker compose logs -f api                # follow API logs
docker compose run --rm seed --reset      # wipe the database, migrate and reseed
docker compose run --rm seed --migrate-only   # only apply new migrations
docker compose down                       # stop (data is kept)
docker compose down -v                    # stop and delete all data and caches
```

### Running the API or client outside Docker

Useful for debugging in the IDE. Start only the database and seed it:

```sh
docker compose up -d db && docker compose run --rm seed
```

- **API:** `dotnet run --project server/Loom.Api`. The launch profile enables dev login (`DevAuth__Enabled=true`). The connection string comes from user secrets or `ConnectionStrings__DefaultConnection`, for example `Host=localhost;Port=5432;Database=loom;Username=loom;Password=loom`.
- **Client:** `cd client && pnpm install && pnpm dev`. To use the dev login against a local API, create `client/.env.development.local` with:
  ```
  VITE_API_URL=
  VITE_PROXY_TARGET=http://localhost:5000
  VITE_DEV_HTTPS=false
  VITE_DEV_LOGIN=true
  ```

## Dev login and personas

The API exposes `POST /auth/dev/login` only when `ASPNETCORE_ENVIRONMENT=Development` **and** `DevAuth:Enabled=true`. The app refuses to start if DevAuth is enabled in any other environment. Dev login creates the same claims Google would, so user sync, roles and permissions behave as in production. Any email you type that isn't seeded behaves like a first Google login.

All demo data is created through the real application services (`server/Loom.DevSeed/DemoData.cs`). Exchange GUIDs are fixed, so the links below survive a reset.

| Persona | Role | What to try |
|---|---|---|
| `admin@loom.dev` | Admin | Users (paging, search, roles), coordinator requests, whitelist, partner institutions & courses, course merge |
| `ana.coordinator@loom.dev` | Coordinator | Has all scenario students: approve, reopen, messages, recognition approval |
| `ivo.coordinator@loom.dev` | Coordinator | Has no students: what an unassigned coordinator can and cannot do |
| `new.coordinator@loom.dev` | (not a user yet) | Whitelisted: the first login is promoted to Coordinator |
| `req.pending@loom.dev` | Student | Pending coordinator request (approve/reject as admin) |
| `req.rejected@loom.dev` | Student | Rejected coordinator request banner |
| `fresh.student@loom.dev` | Student | Not onboarded: onboarding wizard |
| `claim.student@loom.dev` | Student | Not onboarded: enter JMBAG `0036999002` to take over a placeholder's exchange |
| `s.draft.empty@loom.dev` | Student | Empty learning agreement: first mapping, drag & drop |
| `s.draft.rich@loom.dev` | Student | All slot modes, a course split over two slots, an over-filled slot |
| `s.draft.message@loom.dev` | Student | Draft with a coordinator message |
| `s.approved@loom.dev` | Student | Approved LA (locked: save, import and restore are refused) |
| `s.reopened@loom.dev` | Student | Approved, then reopened by the coordinator and changed |
| `s.history@loom.dev` | Student | Three approved versions, removed courses, amendment numbers, a restore backup |
| `s.recognition.draft@loom.dev` | Student | Recognition in progress: grades, mapping scheme moved/split, a failed course |
| `s.recognition.done@loom.dev` | Student | Recognition approved (results locked), Excel export |
| `s.multi@loom.dev` | Student | Three exchanges (switcher) |
| `s.nocoordinator@loom.dev` | Student | Exchange without a coordinator |
| `student01…40@loom.dev` | Student | Volume for lists; 01–20 have exchanges with Ana |

**Guest access (placeholder student, no login):** http://localhost:5173/access/00000000-0000-4000-8000-000000000013

**Catalogue:** 60 partner institutions (one soft-deleted, one without courses). *Universidad Politécnica de Madrid* has 150+ courses and near-duplicate codes to try the merge tool on (`ML101`/`ML-101`, `DB200`/`DB 200`/`DB-200`, `SEC300`/`SEC-300`). The TUM course `IN9999` is soft-deleted.

Exchange links are `http://localhost:5173/exchange/00000000-0000-4000-8000-0000000000NN`:

| NN | Exchange | NN | Exchange |
|---|---|---|---|
| 01 | s.draft.empty | 08 | s.recognition.done |
| 02 | s.draft.rich | 09–11 | s.multi (TUM, Polimi, UPM) |
| 03 | s.draft.message | 12 | s.nocoordinator |
| 04 | s.approved | 13 | guest placeholder (Gita Guest) |
| 05 | s.reopened | 14 | placeholder claimed by claim.student |
| 06 | s.history | 101–120 | student01–20 |
| 07 | s.recognition.draft | | |

## Database migrations

The schema is managed by EF Core migrations in `server/Loom.Infrastructure/Migrations`. Never edit the database by hand.

```sh
# after changing an entity or configuration
dotnet tool restore
dotnet ef migrations add <Name> --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure
# review the generated file, then apply locally:
docker compose run --rm seed --migrate-only
```

Check that no migration is missing (CI runs this too):

```sh
dotnet ef migrations has-pending-model-changes --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure
```

To produce SQL for a production database:

```sh
dotnet ef migrations script --idempotent --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure -o migrations.sql
```

### Production baseline (one time)

The first migration (`Initial`) describes the schema that production already has (the old `schema.sql`), including constraint and index names. Before the first migration-based deploy:

1. Back up: `pg_dump -Fc ...`.
2. Optionally verify that production matches, by running `database/verify-schema.sql` against production and against a fresh local database, then diffing the two outputs.
3. Run `database/baseline.sql` once. It marks `Initial` as already applied.
4. Apply the remaining migrations: `migrations.sql` from above, or an EF migration bundle.
