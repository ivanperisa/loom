import { computed, ref, type Ref } from 'vue'

export function usePagination<T>(rows: Ref<T[]>, perPage = 10) {
  const page = ref(1)

  const totalPages = computed(() => Math.max(1, Math.ceil(rows.value.length / perPage)))

  const paged = computed(() => {
    // Clamp rather than reset, so a shrinking list never shows a blank page.
    const current = Math.min(page.value, totalPages.value)
    return rows.value.slice((current - 1) * perPage, current * perPage)
  })

  return { page, totalPages, paged, perPage }
}
