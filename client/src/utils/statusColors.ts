export const statusColorClass: Record<string, string> = {
  Draft: 'bg-muted/15 text-muted border-muted/40',
  Submitted: 'bg-yellow-500/15 text-warning border-warning/40',
  Approved: 'bg-success/15 text-success border-success/40',
  Rejected: 'bg-red-500/15 text-danger border-danger/40',
}

export const statusDotClass: Record<string, string> = {
  Draft: 'bg-zinc-400',
  Submitted: 'bg-yellow-400',
  Approved: 'bg-green-400',
  Rejected: 'bg-red-400',
}
