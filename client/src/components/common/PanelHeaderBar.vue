<script setup lang="ts">
defineProps<{
  homeProfileName: string
}>()
</script>

<template>
  <div class="relative mb-4 flex min-h-[38px] flex-wrap items-center justify-between gap-3">
    <div class="flex items-center gap-3">
      <slot name="left" />
    </div>
    <span class="pointer-events-none absolute left-1/2 -translate-x-1/2 text-sm font-semibold text-light/80">
      {{ homeProfileName }}
    </span>
    <div class="flex flex-wrap gap-2">
      <slot name="right" />
    </div>
  </div>
</template>
