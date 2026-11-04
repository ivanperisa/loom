/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string
  readonly VITE_BASE_PATH: string
  /** Shows the "Log in as" persona picker (local development only). */
  readonly VITE_DEV_LOGIN?: string
  readonly VITE_DEV_HTTPS?: string
  readonly VITE_PROXY_TARGET?: string
  readonly VITE_USE_POLLING?: string
  /** Optional Sentry/GlitchTip DSN for browser errors; empty = off. */
  readonly VITE_SENTRY_DSN?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
