import type { App } from 'vue'
import type { Router } from 'vue-router'

/** An access link's token is a secret: never send it anywhere, not even inside a URL. */
export function scrubAccessToken(url: string): string
export function scrubAccessToken(url: string | undefined): string | undefined
export function scrubAccessToken(url: string | undefined): string | undefined {
  return url?.replace(/\/access\/[^/?#]+/g, '/access/[token]')
}

/**
 * Optional browser error tracking (Sentry, or a self-hosted GlitchTip): only when VITE_SENTRY_DSN is set at build
 * time. The SDK is loaded on demand, so without a DSN it is not even downloaded. No personal data is sent.
 */
export async function startErrorTracking(app: App, router: Router) {
  const dsn = import.meta.env.VITE_SENTRY_DSN
  if (!dsn) return
  const Sentry = await import('@sentry/vue')
  Sentry.init({
    app,
    dsn,
    environment: import.meta.env.MODE,
    sendDefaultPii: false,
    integrations: [Sentry.browserTracingIntegration({ router })],
    tracesSampleRate: 0,
    beforeSend(event) {
      if (event.request?.url) event.request.url = scrubAccessToken(event.request.url)
      return event
    },
    beforeBreadcrumb(breadcrumb) {
      if (breadcrumb.data?.url) breadcrumb.data.url = scrubAccessToken(String(breadcrumb.data.url))
      if (breadcrumb.data?.to) breadcrumb.data.to = scrubAccessToken(String(breadcrumb.data.to))
      if (breadcrumb.data?.from) breadcrumb.data.from = scrubAccessToken(String(breadcrumb.data.from))
      if (breadcrumb.message) breadcrumb.message = scrubAccessToken(breadcrumb.message)
      return breadcrumb
    },
  })
}
