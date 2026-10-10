# Loom client

Vue 3 + TypeScript + Vite + Tailwind. Run it with `docker compose up` from the repository root (see `../README.md`), or `pnpm dev` against a running API.

## Commands

| Command | What it does |
|---|---|
| `pnpm dev` | Dev server (Vite) |
| `pnpm build` | Type-check (`vue-tsc`) and build |
| `pnpm build:analyze` | Build and open `dist/stats.html`: what each dependency adds to the bundle |
| `pnpm lint:check` | oxlint + ESLint, no fixes (`pnpm lint` fixes) |
| `pnpm test:unit` | Vitest: composables, the LA draft, error texts, i18n keys |
| `pnpm e2e` | Playwright against `docker compose up` (seeded personas) |
| `pnpm api:types` | `openapi.json` → `src/api/schema.d.ts` |

## Where things live

| Folder | What lives there |
|---|---|
| `api/` | Generated API types (`schema.d.ts`, never edited by hand) and the `Schemas` alias |
| `types/` | Familiar names for the generated types (`ExchangeResponse = Schemas['ExchangeResponse']`) and client-only types (the LA draft) |
| `services/` | Thin axios calls, one per endpoint. No state, no caching. |
| `queries/` | vue-query: query keys, read hooks (`useExchangeQuery`, …) and mutations that update the cache |
| `stores/` | Pinia, only for state the client owns: the signed-in user (`auth`) and the LA being edited (`laDraft`) |
| `composables/` | Reusable logic: `useListQuery`, `useExchangeContext`, `useExchangePeriod`, `useLaDropFlow`, … |
| `components/` | `common/` primitives (`TabBar`, `ErrorAlert`, `SearchableSelect`, `BaseModal`, …) and feature folders |

## Conventions

**Server state is vue-query, not Pinia.** Read with a query hook; change with a mutation from `queries/`. A mutation stores what the server answered and marks the rest stale (everything about one exchange sits under `['exchange', guid]`). Never copy query data into a store.

**One exchange page, one context.** `ExchangeDetailPanel` calls `provideExchangeContext(guid, guest)`; every panel below calls `useExchangeContext()` for the exchange, its documents and the permissions (`isCoordinator`, `isEditable`, `isConcluded`). Guest mode (an access link) is just `guest: true`.

**The LA draft** (`stores/laDraft.store.ts`) holds unsaved edits and drag and drop. It reloads from the saved LA whenever it has no unsaved changes; import and restore replace it on purpose.

**Lists** use `useListQuery({ key, fetch, filters, defaultSort, syncToUrl })`: page, debounced search, sort and typed filters, the previous page kept on screen while the next loads, stale requests aborted, state mirrored in the address bar. The server side is `ListQuery`/`ListSpec` (see `../server/README.md`).

**Errors.** The API answers with an error `code` (+ `params`); `utils/apiError.ts` turns it into a message in the current language (`apiErrors.codes.*`). Failed reads show `<ErrorAlert>` where the data would be; failed actions get a toast from the axios interceptor (`errorToast: false` when a screen shows the error itself). A test fails when a server error code has no translation.

**Types come from the API.** Change the server, refresh `openapi.json` (`LOOM_UPDATE_OPENAPI=1 dotnet test --filter ApiContract`), run `pnpm api:types`. Ids are numbers, exchange guids strings, statuses and modes string unions.

**i18n.** `en.ts` is the reference; `hr.ts` must have exactly the same keys (`satisfies MessageSchema`, so `vue-tsc` fails otherwise). A unit test checks every `t('…')` with a fixed key.

**Accessibility.** Icon-only buttons get an `aria-label`; tabs use `TabBar` (arrow keys); selects use `SearchableSelect` (listbox, arrow keys, Escape); clickable grid cells are buttons with keyboard support.

## Adding a screen that reads data

1. Endpoint first (server), then `pnpm api:types`.
2. A service call in `services/`, a query hook in `queries/` with a key from `queries/keys.ts` (`useListQuery` for a list).
3. In the component: loading skeleton, `<ErrorAlert>` with retry, then the data.
4. Strings in both `en.ts` and `hr.ts`; an E2E step in `e2e/personas.spec.ts` for a new flow.
