const MIN_SEARCH_LENGTH = 3

export function minSearchTerm(term: string): string | undefined {
  const trimmed = term.trim()
  return trimmed.length >= MIN_SEARCH_LENGTH ? trimmed : undefined
}
