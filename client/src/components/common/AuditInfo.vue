<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { formatDate } from '@/utils/formatDate'

defineProps<{
  lastModifiedAt: string | null | undefined
  lastModifiedByName: string | null | undefined
  signedAt: string | null | undefined
  signedByName: string | null | undefined
}>()

const { t, locale } = useI18n()
</script>

<template>
  <div
    v-if="lastModifiedAt || signedAt"
    class="-mt-2 mb-4 flex flex-col gap-y-1 text-xs text-light/50"
  >
    <span v-if="lastModifiedAt">
      {{ t('exchange.audit.lastModified') }}: {{ formatDate(lastModifiedAt, locale) }}
      <template v-if="lastModifiedByName"> — {{ lastModifiedByName }}</template>
    </span>
    <span v-if="signedAt">
      {{ t('exchange.audit.signed') }}: {{ formatDate(signedAt, locale) }}
      <template v-if="signedByName"> — {{ signedByName }}</template>
    </span>
  </div>
</template>
