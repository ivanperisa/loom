export function buildAccessLink(guid: string): string {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '')
  return `${window.location.origin}${base}/access/${guid}`
}
