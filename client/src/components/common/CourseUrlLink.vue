<script setup lang="ts">
import { ExternalLinkIcon } from '@lucide/vue'
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
    <ExternalLinkIcon :size="size" />
  </a>
</template>
