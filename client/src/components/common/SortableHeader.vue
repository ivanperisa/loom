<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  label: string
  sortKey: string
  activeKey: string
  dir: 'asc' | 'desc'
}>()

defineEmits<{ sort: [key: string] }>()

const isActive = computed(() => props.activeKey === props.sortKey)
const ariaSort = computed(() =>
  isActive.value ? (props.dir === 'asc' ? 'ascending' : 'descending') : 'none',
)
</script>

<template>
  <button
    type="button"
    :aria-sort="ariaSort"
    class="flex items-center gap-1 text-left uppercase tracking-wider transition hover:text-light"
    :class="isActive ? 'text-light/70' : ''"
    @click="$emit('sort', sortKey)"
  >
    {{ label }}
    <span v-if="isActive" aria-hidden="true" class="text-[9px] leading-none">
      {{ dir === 'asc' ? '▲' : '▼' }}
    </span>
  </button>
</template>
