<script setup lang="ts">
defineProps<{
  homeProfileName: string
}>()
</script>

<template>
  <!-- Three columns on wide screens, so the centred title never runs into the buttons; stacked on narrow ones. -->
  <div class="mb-4 flex min-h-[38px] flex-col gap-3 xl:grid xl:grid-cols-[1fr_auto_1fr] xl:items-center">
    <div class="flex flex-wrap items-center gap-3">
      <slot name="left" />
    </div>
    <span class="order-first text-sm font-semibold text-light/80 xl:order-none xl:text-center">
      {{ homeProfileName }}
    </span>
    <div class="flex flex-wrap gap-2 xl:justify-end">
      <slot name="right" />
    </div>
  </div>
</template>
