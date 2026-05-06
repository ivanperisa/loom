<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { statusColorClass } from '@/utils/statusColors'

const { t } = useI18n()

defineProps<{
  status: string
  i18nPrefix?: string
}>()
</script>

<template>
  <span
    class="rounded-full border px-3 py-0.5 text-xs font-semibold"
    :class="statusColorClass[status] ?? statusColorClass['Draft']"
  >
    {{ t(`${i18nPrefix ?? 'documentStatus'}.${status}`) }}
  </span>
</template>
