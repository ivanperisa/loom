<script setup lang="ts">
defineProps<{ placeholder?: string; modelValue?: string }>()
defineEmits<{ 'update:modelValue': [value: string] }>()
</script>

<template>
  <div class="relative">
    <svg
      class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-light/30"
      fill="none" stroke="currentColor" viewBox="0 0 24 24"
    >
      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
        d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
    <input
      :value="modelValue ?? ''"
      type="text"
      :placeholder="placeholder"
      class="w-full rounded-xl border border-primary/20 bg-dark-2 py-2.5 pl-10 pr-4 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
  </div>
</template>
