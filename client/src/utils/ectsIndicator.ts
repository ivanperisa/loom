export function ectsIndicatorColor(mapped: number, total: number, isLight: boolean): string {
  if (mapped === 0) return isLight ? '#78716c' : '#94a3b8'
  if (mapped < total) return isLight ? '#b45309' : '#f59e0b'
  if (mapped === total) return isLight ? '#16a34a' : '#22c55e'
  return '#ef4444'
}
