import { computed, ref, watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { keepPreviousData, useQuery } from '@tanstack/vue-query'
import type { AxiosResponse } from 'axios'
import { useDebouncedRef } from '@/composables/useDebouncedRef'
import { minSearchTerm } from '@/utils/searchTerm'
import type { ListParams, PagedResponse } from '@/types/paged.types'

export type SortDir = 'asc' | 'desc'
type FilterValue = string | boolean | null

export interface ListQueryOptions<TItem, TFilters extends Record<string, FilterValue>> {
  /** Base query key; params are appended, and invalidating this key refreshes every page. */
  key: readonly unknown[]
  fetch: (params: ListParams & Partial<TFilters>, signal: AbortSignal) => Promise<AxiosResponse<PagedResponse<TItem>>>
  filters?: { [K in keyof TFilters]: Ref<TFilters[K]> }
  pageSize?: number
  /** Sort key used when none is chosen; `-` prefix for descending. */
  defaultSort?: string
  /**
   * Mirror page, search, sort and filters in the address bar (shareable, survives reload).
   * A string prefixes the names, for pages with more than one list.
   */
  syncToUrl?: boolean | string
  /** False: the query waits (e.g. until a tab is open). */
  enabled?: Ref<boolean>
}

/**
 * One server-paged list: page, debounced search, sort and typed filters, with the previous page kept
 * on screen while the next one loads and stale requests aborted. Changing search, sort or a filter
 * goes back to page 1.
 */
export function useListQuery<TItem, TFilters extends Record<string, FilterValue> = Record<string, never>>(
  options: ListQueryOptions<TItem, TFilters>,
) {
  const pageSize = options.pageSize ?? 25
  const filters = (options.filters ?? {}) as Record<string, Ref<FilterValue>>
  const page = ref(1)
  const search = ref('')
  const sort = ref(options.defaultSort ?? '')
  const debouncedSearch = useDebouncedRef(search, 300)

  if (options.syncToUrl) syncWithUrl(options.syncToUrl === true ? '' : `${options.syncToUrl}.`)

  const params = computed(() => {
    const values: Record<string, unknown> = {
      page: page.value,
      pageSize,
      search: minSearchTerm(debouncedSearch.value),
      sort: sort.value || undefined,
    }
    for (const [name, filter] of Object.entries(filters)) {
      if (filter.value !== null && filter.value !== '') values[name] = filter.value
    }
    return values as ListParams & Partial<TFilters>
  })

  watch(
    () => [debouncedSearch.value, sort.value, ...Object.values(filters).map((f) => f.value)],
    () => (page.value = 1),
  )

  const query = useQuery({
    queryKey: computed(() => [...options.key, params.value]),
    queryFn: async ({ signal }) => (await options.fetch(params.value, signal)).data,
    placeholderData: keepPreviousData,
    enabled: options.enabled ?? true,
  })

  const items = computed(() => query.data.value?.items ?? [])
  const totalCount = computed(() => query.data.value?.totalCount ?? 0)
  const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)))

  // A shrinking list (deletes, a narrower filter) never leaves the user on an empty page.
  watch(totalPages, (pages) => {
    if (page.value > pages) page.value = pages
  })

  const sortKey = computed(() => sort.value.replace(/^-/, ''))
  const sortDir = computed<SortDir>(() => (sort.value.startsWith('-') ? 'desc' : 'asc'))

  /** Same key flips the direction; a new key starts ascending. */
  function toggleSort(key: string) {
    sort.value = sortKey.value === key && sortDir.value === 'asc' ? `-${key}` : key
  }

  function syncWithUrl(prefix: string) {
    const route = useRoute()
    const router = useRouter()
    const read = (name: string) => {
      const value = route.query[prefix + name]
      return typeof value === 'string' && value !== '' ? value : null
    }

    page.value = Number(read('page')) || 1
    search.value = read('q') ?? ''
    sort.value = read('sort') ?? sort.value
    for (const [name, filter] of Object.entries(filters)) {
      const value = read(name)
      if (value !== null) filter.value = typeof filter.value === 'boolean' ? value === 'true' : value
    }

    watch(
      () => ({
        page: page.value > 1 ? String(page.value) : null,
        q: debouncedSearch.value.trim() || null,
        sort: sort.value && sort.value !== options.defaultSort ? sort.value : null,
        ...Object.fromEntries(Object.entries(filters).map(([name, f]) => [name, f.value === null ? null : String(f.value)])),
      }),
      (values) => {
        // Keep params this list doesn't own (another list's, `tab`, ...).
        const next: Record<string, string> = {}
        for (const [name, value] of Object.entries(route.query)) {
          if (typeof value === 'string' && !(name.startsWith(prefix) && name.slice(prefix.length) in values)) next[name] = value
        }
        for (const [name, value] of Object.entries(values)) {
          if (value !== null && value !== '') next[prefix + name] = value
        }
        router.replace({ query: next })
      },
    )
  }

  return {
    page,
    pageSize,
    /** Bind the search input here; requests use a debounced copy (3+ characters). */
    search,
    sort,
    sortKey,
    sortDir,
    toggleSort,
    items,
    totalCount,
    totalPages,
    hasDeleted: computed(() => query.data.value?.hasDeleted ?? false),
    /** First load only; later loads keep the previous page visible (see `isFetching`). */
    isPending: query.isPending,
    isFetching: query.isFetching,
    error: query.error,
    refetch: query.refetch,
  }
}
