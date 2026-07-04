export function exchangeBasePath(exchangeId: string, guest: boolean): string {
  return guest ? `/api/exchanges/access/${exchangeId}` : `/api/exchanges/${exchangeId}`
}
