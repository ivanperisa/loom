export const statusColorClass: Record<string, string> = {
  Draft: 'bg-fill text-muted border-muted/40',
  Submitted: 'bg-warning-fill text-warning-text border-warning-text/35',
  Approved: 'bg-success-fill text-success-text border-success-text/35',
  Rejected: 'bg-danger-fill text-danger-text border-danger-text/35',
}

export const statusDotClass: Record<string, string> = {
  Draft: 'bg-zinc-400',
  Submitted: 'bg-yellow-400',
  Approved: 'bg-green-400',
  Rejected: 'bg-red-400',
}
