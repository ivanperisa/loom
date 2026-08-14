import { computed, ref, type Ref } from 'vue'

export type SortDir = 'asc' | 'desc'
export type SortAccessor<T> = (row: T) => string | number | null | undefined

export function useSortable<T>(
  rows: Ref<T[]>,
  accessors: Record<string, SortAccessor<T>>,
  initialKey: string,
  initialDir: SortDir = 'asc',
  locale?: Ref<string>,
) {
  const sortKey = ref(initialKey)
  const sortDir = ref<SortDir>(initialDir)

  function toggleSort(key: string) {
    if (sortKey.value === key) {
      sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortKey.value = key
      sortDir.value = 'asc'
    }
  }

  const sorted = computed(() => {
    const get = accessors[sortKey.value]
    if (!get) return rows.value
    const dir = sortDir.value === 'asc' ? 1 : -1

    return rows.value.slice().sort((a, b) => {
      const av = get(a)
      const bv = get(b)
      // Blanks always sink, regardless of direction.
      if (av === null || av === undefined || av === '') return 1
      if (bv === null || bv === undefined || bv === '') return -1
      if (typeof av === 'number' && typeof bv === 'number') return (av - bv) * dir
      return String(av).localeCompare(String(bv), locale?.value) * dir
    })
  })

  return { sortKey, sortDir, toggleSort, sorted }
}
