# Loom server

ASP.NET Core (.NET 10) + EF Core + PostgreSQL. Run it, seed it and test it from the repository root (see `../README.md`).

## Projects

| Project | What lives there |
|---|---|
| `Loom.Domain` | Entities, enums, small rules that only need the entity (`User.IsPlaceholder`, `HomeSlot.Label`, `ISoftDeletable`) |
| `Loom.Application` | Use cases, organised by feature. No HTTP. |
| `Loom.Infrastructure` | `AppDbContext`, EF configurations, migrations, cache invalidation |
| `Loom.Api` | Thin controllers, authentication, error handling, the exchange actor filter |
| `Loom.DevSeed` | Applies migrations and loads reference + demo data (local only) |
| `tests/Loom.IntegrationTests` | Tests against a real PostgreSQL (Testcontainers), incl. HTTP tests |

## Features (`Loom.Application/Features`)

Each folder holds its services, contracts (requests/responses), errors and projections.

| Feature | Covers |
|---|---|
| `Catalog` | Home catalogue (cached), partner institutions, partner courses, course merge |
| `Users` | Login sync, own account (me, onboarding, profile, coordinator request), claiming a placeholder, coordinator assignment |
| `Admin` | User list/edit/roles, coordinator requests, coordinator whitelist |
| `Coordination` | Coordinator directory, a coordinator's students and placeholder students |
| `Exchanges` | Exchanges (create/edit/delete/list), access links and guest sessions |
| `Planning` | Learning agreement: document, workflow (Draft ⇄ Approved), versions (history/restore), JSON import/export |
| `Completion` | Start final recognition, recognition (table 1 + table 2) and mapping scheme (after the exchange) |
| `Documents` | Numbered document versions and their diff (shared by the LA and the recognition), the official xlsx |

`Common` has what features share: list querying, the current actor, exchange access, shared errors, cache tags.

## Conventions

**Who is calling.** Services never take a `requesterId`. They inject `ICurrentActor`, which the API fills in from the login cookie (`UserSyncMiddleware`) or, on `[AllowGuest]` actions, from the guest cookie (`ExchangeActorAttribute`). The seeder and tests set it explicitly.

**Access links (guests).** A placeholder student (no account) works through a link the coordinator sends: `/access/{token}`. The token is 32 random bytes in `exchange.access_link`, separate from the exchange GUID; the GUID is just an id. Opening the link (`POST /api/access/session`) swaps the token for a `loom_guest` cookie that holds only the link id. Every guest request re-checks that link (not revoked, student still a placeholder) and is limited to that one exchange, so regenerating or claiming cuts access at once. Guests can call only actions marked `[AllowGuest]`: the exchange, its documents and its partner courses; never delete, link management or anything outside the exchange. A signed-in student who opens the link can claim the placeholder (`POST /api/access/claim`): its exchanges and JMBAG move to their account. Typing the JMBAG alone gives no access (`JMBAG_RESERVED`).

**Routes** are `/api/<plural resource>/<id>/<sub-resource>`, kebab-case (`/api/partner-institutions/{id}/courses`, `/api/exchanges/{guid}/learning-agreement`). `GET` reads, `POST` creates or runs an action (`…/restore`, `…/start`, `…/import`), `PUT` replaces, `PATCH` changes one part (`…/status`, `…/message`), `DELETE` removes. The signed-in user's own things live under `/api/users/me`, `/api/exchanges/mine`, `/api/coordinator/…`.

**Exchange-scoped work** starts with `ExchangeAccess.LoadAsync(guid)`. It is one query: exchange ids + "is the actor its student or assigned coordinator", or `EXCHANGE_NOT_FOUND` / `ACCESS_DENIED`.

**Errors** are `ErrorOr` results, never exceptions. Each feature has a `*Errors` class; the `code` is part of the API contract (the client translates it, `apiErrors.codes.*`). Values the message needs (a course id, available ECTS) go in the error's metadata and reach the client as `params`. Controllers just `Match(result, Ok)`. Unexpected exceptions go to `ApiExceptionHandler`. It maps DB conflicts to 409 and never leaks messages.

**Responses** use enums for statuses, modes and roles (serialized as strings), so the contract has exact types; requests take strings and parse them with a feature error (`INVALID_STATUS`, `INVALID_MODE`).

**Reads** are `IQueryable` → `Where` → `Select(projection)`, with no tracking and no `Include` when a projection does. Projections are `Expression<Func<…>>` fields in the feature (`CatalogProjections`, `ExchangeProjections`, …).

**Writes** load tracked entities, change them, then call `SaveChangesAsync` once. Use a transaction only when a flow needs two saves (`AccountService.TakeOverPlaceholderAsync`).

**Lists** use one shape everywhere: `ListQuery` (`page`, `pageSize`, `search`, `sort=name|-name`; legacy `sortBy`+`sortDir` still accepted) plus typed filters:

```csharp
private static readonly ListSpec<PartnerCourse, PartnerCourseResponse> List = ListSpec.For<PartnerCourse>()
    .SearchIn(c => c.Code, c => c.Name, c => c.NameHr)   // case-insensitive LIKE, escaped, in SQL
    .SortBy("code", c => c.Code, isDefault: true)
    .SortBy("ects", c => c.Ects)
    .Project(CatalogProjections.PartnerCourse);

db.PartnerCourses
    .Where(c => c.InstitutionId == institutionId)
    .IncludeDeleted(query.IncludeDeleted)
    .WhereIf(query.Level is not null, c => c.Level == query.Level)
    .ToPageAsync(List, query, ct);   // count + sort (+ Id tiebreaker) + page + project
```

**Soft delete** is explicit (`.IncludeDeleted(flag)`), not a global filter. A global filter would silently hide soft-deleted courses that learning agreements still point to.

**Caching** is only for reference data (`HybridCache`, tags in `CacheTags`). `CacheInvalidationInterceptor` clears a tag whenever SaveChanges touches one of its entity types, so services never invalidate by hand. Paged lists are not cached.

**Documents (LA, recognition, mapping scheme).** The flow and its rules:

1. *Learning agreement* (before/during the exchange): a draft anyone on the exchange edits. Only the assigned coordinator approves it or sends it back. Each approval that changed something is the next `document_version` (1 = original, 2 = amendment A1, …); approving unchanged content keeps the number. Components carry `AddedInVersion`/`RemovedInVersion`: an approved component taken out is kept, struck through ("removed in An"), never deleted.
2. *Start final recognition* (anyone on the exchange, LA approved, after a warning): freezes the LA and table 1 for good and copies the latest approved version into the results (`mapping_scheme_entry`).
3. *Results* (after the exchange): table 2 (grades, per partner course) and the mapping scheme (placement) are one dataset that may drift from the frozen LA. The coordinator approves them (a recognition version) or reopens them.

Table 1 is computed from the latest approved LA version, never stored. Save, import and restore all go through `LaEntryWriter` (same validation, only in an editable draft, never a status change); import and restore keep a backup when the draft changes. The official xlsx is built on the server (`Documents/Official`) from saved data only.

**Concurrency:** `Exchange`, `LearningAgreement` and `Recognition` carry PostgreSQL's `xmin` as a row version. Two overlapping writes fail with 409 `CONCURRENT_UPDATE` instead of overwriting each other.

## Adding an endpoint

1. Add the use case to the feature's service, or a new service registered in `DependencyInjection.cs`. Request/response records go in the feature's contracts file; error codes go in its `*Errors` class.
2. Exchange-scoped? Start with `ExchangeAccess.LoadAsync`. A list? Give it a `ListSpec`.
3. Add a thin controller action returning `Task<ActionResult<TResponse>>` (`Match(await service.X(...), Ok)`), so the OpenAPI document knows the response type. No body: `IActionResult` with `[ProducesResponseType(204)]`.
4. Add an integration test (`tests/Loom.IntegrationTests`); for auth/routing, an HTTP test in `ApiTests`.
5. Refresh the contract and the client types (`LOOM_UPDATE_OPENAPI=1 dotnet test --filter ApiContract`, then `pnpm api:types`; see `../README.md`).
6. Changed an entity or configuration? Add a migration (see `../README.md`).
