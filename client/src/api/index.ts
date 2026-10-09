import type { components } from './schema'

/**
 * Types of the API contract, generated from `client/openapi.json` (`pnpm api:types`).
 * Never edit `schema.d.ts` by hand; change the server, refresh openapi.json (see ApiContractTests), regenerate.
 */
export type Schemas = components['schemas']
