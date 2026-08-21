import { onMounted, onUnmounted } from 'vue'

export function useDragAutoScroll() {
  const EDGE = 90
  const SPEED = 20

  function onDragOver(e: DragEvent) {
    if (e.clientY < EDGE) window.scrollBy(0, -SPEED)
    else if (e.clientY > window.innerHeight - EDGE) window.scrollBy(0, SPEED)

    const wrap = (e.target as HTMLElement)?.closest?.('.doc-table-wrap') as HTMLElement | null
    if (!wrap) return
    const r = wrap.getBoundingClientRect()
    if (e.clientX < r.left + EDGE) wrap.scrollLeft -= SPEED
    else if (e.clientX > r.right - EDGE) wrap.scrollLeft += SPEED
  }

  onMounted(() => window.addEventListener('dragover', onDragOver))
  onUnmounted(() => window.removeEventListener('dragover', onDragOver))
}
