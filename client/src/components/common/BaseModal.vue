<script setup lang="ts">
import { onMounted, onBeforeUnmount, nextTick, ref } from 'vue'

withDefaults(
  defineProps<{
    labelledBy?: string
    maxWidth?: string
    zClass?: string
    closeOnBackdrop?: boolean
  }>(),
  { maxWidth: 'max-w-md', zClass: 'z-50', closeOnBackdrop: true },
)

const emit = defineEmits<{ close: [] }>()

const panel = ref<HTMLElement | null>(null)
let previouslyFocused: HTMLElement | null = null

const FOCUSABLE =
  'a[href],button:not([disabled]),input:not([disabled]),select:not([disabled]),textarea:not([disabled]),[tabindex]:not([tabindex="-1"])'

function focusables(): HTMLElement[] {
  return panel.value ? Array.from(panel.value.querySelectorAll<HTMLElement>(FOCUSABLE)) : []
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.stopPropagation()
    emit('close')
    return
  }
  if (event.key !== 'Tab') return

  const items = focusables()
  if (items.length === 0) {
    event.preventDefault()
    return
  }
  const first = items[0]!
  const last = items[items.length - 1]!
  const active = document.activeElement as HTMLElement | null

  // Wrap focus at both ends so Tab can never escape the dialog.
  if (event.shiftKey && (active === first || !panel.value?.contains(active))) {
    event.preventDefault()
    last.focus()
  } else if (!event.shiftKey && active === last) {
    event.preventDefault()
    first.focus()
  }
}

onMounted(async () => {
  previouslyFocused = document.activeElement as HTMLElement | null
  // Capture phase: a nested dialog closes before its parent sees the Escape.
  document.addEventListener('keydown', onKeydown, true)
  await nextTick()
  ;(focusables()[0] ?? panel.value)?.focus()
})

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown, true)
  previouslyFocused?.focus()
})
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 flex items-center justify-center bg-black/50 px-4 backdrop-blur-sm"
      :class="zClass"
      @mousedown.self="closeOnBackdrop && emit('close')"
    >
      <div
        ref="panel"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="labelledBy"
        tabindex="-1"
        class="w-full rounded-xl border border-primary/30 bg-dark shadow-xl focus:outline-none"
        :class="maxWidth"
      >
        <slot />
      </div>
    </div>
  </Teleport>
</template>
