<script setup lang="ts">
withDefaults(
  defineProps<{
    url: string
    title?: string
    block?: boolean
    size?: number
  }>(),
  { block: false, size: 9 },
)
</script>

<template>
  <a
    :href="url"
    target="_blank"
    rel="noopener noreferrer"
    :title="title"
    style="display: inline-flex; vertical-align: middle; color: #0066cc;"
    :style="block ? '' : 'margin-left: 3px;'"
    @click.stop
  >
    <svg :width="size" :height="size" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
      <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
      <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
    </svg>
  </a>
</template>
