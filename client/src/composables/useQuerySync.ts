import { watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

/**
 * Two-way binds a set of refs to URL query params.
 * Hydrates from the current URL once, then mirrors every change back via replace().
 */
export function useQuerySync(params: Record<string, Ref<string | null>>) {
  const route = useRoute()
  const router = useRouter()

  for (const [key, target] of Object.entries(params)) {
    const initial = route.query[key]
    if (typeof initial === 'string' && initial !== '') target.value = initial
  }

  watch(
    () => Object.fromEntries(Object.entries(params).map(([key, r]) => [key, r.value])),
    (values) => {
      // Preserve query keys this composable doesn't own (e.g. `tab`).
      const query: Record<string, string> = {}
      for (const [key, value] of Object.entries(route.query)) {
        if (!(key in params) && typeof value === 'string') query[key] = value
      }
      for (const [key, value] of Object.entries(values)) {
        if (value !== null && value !== '') query[key] = value
      }
      router.replace({ query })
    },
    { deep: true },
  )
}
