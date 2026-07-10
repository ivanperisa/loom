export function formatDate(iso: string, locale: string): string {
  return new Date(iso).toLocaleString(locale === 'hr' ? 'hr-HR' : 'en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
