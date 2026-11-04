/**
 * A path inside this app to return to after signing in, or null. Anything that could leave the site
 * ("//host", "/\host", "https://...") is refused; the server checks the same.
 */
export function safeRedirect(value: unknown): string | null {
  if (typeof value !== 'string' || !value.startsWith('/')) return null
  if (value[1] === '/' || value[1] === '\\') return null
  return value
}
