import { ref } from 'vue'

/**
 * A menu that opens under the button that toggled it, for one row at a time. Rendered with
 * `position: fixed` (teleported), so it is never clipped by a scrolling table.
 */
export function usePopoverMenu<TId>(width: number) {
  const openId = ref<TId | null>(null)
  const position = ref({ top: 0, left: 0 })

  function toggle(id: TId, event: MouseEvent) {
    if (openId.value === id) {
      openId.value = null
      return
    }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
    position.value = { top: rect.bottom + 6, left: Math.min(rect.left, window.innerWidth - width - 12) }
    openId.value = id
  }

  return { openId, position, toggle, close: () => (openId.value = null) }
}
