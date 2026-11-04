export function buildAccessLink(token: string): string {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '')
  return `${window.location.origin}${base}/access/${token}`
}

/** Pulls the token out of whatever was pasted: a full link, a path or the bare token. */
export function parseAccessToken(input: string): string | null {
  const value = input.trim()
  const fromLink = value.match(/\/access\/([A-Za-z0-9_-]+)/)
  const token = fromLink ? fromLink[1] : value
  return token && /^[A-Za-z0-9_-]{8,64}$/.test(token) ? token : null
}

/**
 * Copies a link that is still being fetched. Safari only allows clipboard writes that start inside the click
 * handler, so the pending text goes into a ClipboardItem right away instead of awaiting the request first.
 */
export async function copyPendingText(text: Promise<string>): Promise<void> {
  if (typeof ClipboardItem !== 'undefined' && navigator.clipboard?.write) {
    const blob = text.then((value) => new Blob([value], { type: 'text/plain' }))
    await navigator.clipboard.write([new ClipboardItem({ 'text/plain': blob })])
  } else {
    await navigator.clipboard.writeText(await text)
  }
}
