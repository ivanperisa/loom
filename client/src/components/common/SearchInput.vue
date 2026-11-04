<script setup lang="ts">
import { SearchIcon } from '@lucide/vue'
defineProps<{ placeholder?: string; modelValue?: string }>()
defineEmits<{ 'update:modelValue': [value: string] }>()
</script>

<template>
  <div class="relative">
    <SearchIcon class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-light/30" />
    <input
      :value="modelValue ?? ''"
      type="text"
      :placeholder="placeholder"
      class="w-full rounded-xl border border-primary/20 bg-dark-2 py-2.5 pl-10 pr-4 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
  </div>
</template>
