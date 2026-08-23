export function ectsIndicatorColor(mapped: number, total: number, isLight: boolean): string {
  if (mapped === 0) return isLight ? '#57534e' : '#94a3b8'
  if (mapped < total) return isLight ? '#78350F' : '#f59e0b'
  if (mapped === total) return isLight ? '#14532D' : '#22c55e'
  return isLight ? '#991B1B' : '#ef4444'
}
