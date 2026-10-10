# Loom

Erasmus exchange course mapping: learning agreements, recognition and mapping schemes.

- `server/` – ASP.NET Core API (.NET 10, EF Core, PostgreSQL); structure and conventions in `server/README.md`
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

Real Google login works too: set `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` in `.env`, add `http://localhost:5173/signin-oidc` as an authorized redirect URI of that OAuth client in Google Cloud Console, then `docker compose up -d api` to reload them.

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
docker compose restart web api            # after pulling new client/server packages (installs them)
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

The API exposes `POST /api/auth/dev/login` only when `ASPNETCORE_ENVIRONMENT=Development` **and** `DevAuth:Enabled=true`. The app refuses to start if DevAuth is enabled in any other environment. Dev login creates the same claims Google would, so user sync, roles and permissions behave as in production. Any email you type that isn't seeded behaves like a first Google login.

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
| `claim.student@loom.dev` | Student | Not onboarded: sign in, then open http://localhost:5173/access/dev-access-link-14 and claim the exchange |
| `s.draft.empty@loom.dev` | Student | Empty learning agreement: first mapping, drag & drop |
| `s.draft.rich@loom.dev` | Student | All slot modes, a course split over two slots, an over-filled slot |
| `s.draft.message@loom.dev` | Student | Draft with a coordinator message |
| `s.approved@loom.dev` | Student | Approved LA (locked: save, import and restore are refused); ready for **Start final recognition** in the Recognition tab |
| `s.reopened@loom.dev` | Student | Approved, then reopened by the coordinator and changed |
| `s.history@loom.dev` | Student | Three approved versions (original, A1, A2), removed courses struck through, a backup from a restore |
| `s.recognition.draft@loom.dev` | Student | Final recognition started: grades, mapping scheme moved/split, a failed course; the coordinator can approve |
| `s.recognition.done@loom.dev` | Student | Recognition approved (results locked); the official document has all five sheets |
| `s.multi@loom.dev` | Student | Three exchanges (switcher) |
| `s.nocoordinator@loom.dev` | Student | Exchange without a coordinator |
| `student01…40@loom.dev` | Student | Volume for lists; 01–20 have exchanges with Ana |

**Guest access (placeholder student, no login):** http://localhost:5173/access/dev-access-link-13 (seeded links have readable tokens; real ones are 43 random characters)

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

## Tests

```sh
dotnet test Loom.slnx          # integration tests: real PostgreSQL via Testcontainers (needs Docker)
cd client && pnpm lint:check && pnpm type-check && pnpm test:unit   # unit tests: Vitest (composables, LA draft, error texts, i18n keys)
cd client && pnpm exec playwright install chromium && pnpm e2e   # browser smoke tests against `docker compose up`
```

The integration tests also check that every model change has a migration, run the full demo seed once, and check that `client/openapi.json` matches the API.

## API contract (server → client types)

The client's API types are generated, not written by hand:

```sh
LOOM_UPDATE_OPENAPI=1 dotnet test Loom.slnx --filter ApiContract   # API changed → refresh client/openapi.json
cd client && pnpm api:types                                         # openapi.json → src/api/schema.d.ts
```

Commit both files. CI fails when either is stale. `client/src/types/*.ts` only gives the generated types their familiar names (`ExchangeResponse = Schemas['ExchangeResponse']`); client-only types (the LA draft, list params) live there too.

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

### Production

Merging into `main` migrates the production database. Before it unpacks the new API, `deploy-server.yml` runs `database/deploy/migrate.sh` on the server. If nothing is pending, it does nothing. Otherwise it:

1. marks `Initial` as applied on a database still on the old `schema.sql` (`database/baseline.sql`),
2. takes a backup (`database/backup/backup.sh`, into `~/loom-backups` or `BACKUP_DIR`),
3. applies the idempotent script in a single transaction.

If the migration fails, nothing is applied, the step fails before unpacking and the old API keeps running. The server needs `psql`, `pg_dump` and `pg_restore`. To approve each deploy by hand, add required reviewers to the `production` environment.

To produce the same SQL by hand:

```sh
dotnet ef migrations script --idempotent --no-transactions --project server/Loom.Infrastructure --startup-project server/Loom.Infrastructure -o migrations.sql
psql -v ON_ERROR_STOP=1 -1 -f migrations.sql
```

The first deploy from `main` (with a rehearsal on a copy of production and a rollback plan) is described in [`DEPLOYMENT.md`](DEPLOYMENT.md) (Croatian).

## Running in production

### Database requirements

- PostgreSQL 13 or newer. The `AccentInsensitiveSearch` migration creates the `unaccent` and `pg_trgm` extensions; both are "trusted" extensions, so the database owner can create them without superuser rights. If the owner lacks the `CREATE` privilege on the database, an admin runs `CREATE EXTENSION unaccent; CREATE EXTENSION pg_trgm;` once beforehand.
- The API turns off PostgreSQL's JIT for its own connections (`-c jit=off` in the connection options). For these short queries JIT costs more than it saves (measured: 15 ms per call, hundreds of ms on first use). It is a client-side option, so the server setting stays as it is. One exception: PgBouncer in transaction mode rejects startup options. Behind PgBouncer, set `Database__DisableJit=false` and turn JIT off for the app's role instead (`ALTER ROLE loom SET jit = off`).

### Backups

`database/backup/backup.sh` writes a compressed `pg_dump`, checks that `pg_restore` can read it, and rotates old ones: the last 14 daily and 8 weekly (Sunday) dumps, configurable with `KEEP_DAILY` and `KEEP_WEEKLY`.

```sh
PGHOST=db.example PGUSER=loom PGDATABASE=loom ./database/backup/backup.sh /var/backups/loom   # password in ~/.pgpass
./database/backup/restore.sh /var/backups/loom/daily/loom-<time>.dump loom_check               # into a NEW database, never over the live one
```

- **Nightly:** `loom-backup.service` and `loom-backup.timer` (same folder) are a systemd example for 02:30 every night.
- **Before every deploy that applies migrations:** run `backup.sh` first.
- **Off-site:** copy the backup folder to another machine. A backup on the database server is lost together with it.
- **Test restores:** restore into a scratch database now and then. A backup that has never been restored is a hope, not a backup.

### Logs

The API writes to the console (journald, `docker logs`) and to `logs/loom-<date>.log` next to the app, with errors also in `logs/loom-errors-<date>.log`. Files are kept 30 days, the error files 90. Every line carries the request's trace id. When a server error reaches a user, they see "Reference: <id>", which finds the request:

```sh
grep <id> logs/loom-*.log
```

### Error tracking (optional)

Errors can be sent to Sentry or to a self-hosted [GlitchTip](https://glitchtip.com) (same protocol). It stays off until a DSN is set:

- API: `Sentry__Dsn=https://<key>@<host>/<project>` (environment variable or `Sentry:Dsn` in appsettings)
- Client: the `VITE_SENTRY_DSN` variable in the GitHub environment, used at build time. Without it, the SDK isn't even downloaded.

No personal data is sent: no cookies, headers, request bodies or emails. Access-link tokens are removed from URLs before anything leaves the browser.

### Performance

Checked on generated data (20k students, 24k partner courses, 20k exchanges, 120k LA entries): every page's requests answer in 10–50 ms once warm, and the official xlsx takes about 200 ms. `QueryBudgetTests` fail if a page starts running a query per row. To look at the client bundle, run `pnpm build:analyze` in `client/`.
