<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()

const { t } = useI18n()
</script>

<template>
  <button
    type="button"
    role="switch"
    :aria-checked="modelValue"
    class="flex shrink-0 items-center gap-2 text-xs text-light/60 transition hover:text-light"
    @click="emit('update:modelValue', !modelValue)"
  >
    <span
      class="relative inline-flex h-5 w-9 shrink-0 items-center rounded-full border transition-colors"
      :class="modelValue ? 'border-primary bg-primary' : 'border-hairline bg-fill-soft'"
    >
      <span
        class="inline-block h-3.5 w-3.5 transform rounded-full bg-white shadow transition-transform"
        :class="modelValue ? 'translate-x-[18px]' : 'translate-x-0.5'"
      ></span>
    </span>
    {{ t('admin.institutions.showDeleted') }}
  </button>
</template>
