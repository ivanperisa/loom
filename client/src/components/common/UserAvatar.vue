<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{ name: string; role: string; size?: 'sm' | 'md' }>(), { size: 'sm' })

const ROLE_COLOR: Record<string, string> = {
  Student: 'bg-primary-fill text-primary-text',
  Coordinator: 'bg-coord/15 text-coord',
  Admin: 'bg-violet-fill text-violet-text',
}

const initials = computed(() => {
  const parts = props.name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((value) => value[0]?.toUpperCase() ?? '')
    .join('')
  return parts || 'U'
})

const colorClass = computed(() => ROLE_COLOR[props.role] ?? 'bg-fill-soft text-light/70')

const sizeClass = computed(() => (props.size === 'md' ? 'h-10 w-10 text-sm' : 'h-9 w-9 text-xs'))
</script>

<template>
  <span
    class="flex shrink-0 items-center justify-center rounded-full font-bold"
    :class="[sizeClass, colorClass]"
    aria-hidden="true"
  >{{ initials }}</span>
</template>
