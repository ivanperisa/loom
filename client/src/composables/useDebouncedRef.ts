import { ref, watch, onBeforeUnmount, type Ref } from 'vue'

/** A lagging mirror of `source`. Bind inputs to `source`; filter on the return value. */
export function useDebouncedRef<T>(source: Ref<T>, delayMs = 200): Ref<T> {
  const debounced = ref(source.value) as Ref<T>
  let timer: ReturnType<typeof setTimeout> | undefined

  watch(source, (value) => {
    clearTimeout(timer)
    timer = setTimeout(() => {
      debounced.value = value
    }, delayMs)
  })

  onBeforeUnmount(() => clearTimeout(timer))

  return debounced
}
