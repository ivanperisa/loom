<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { formatDate } from '@/utils/formatDate'

const props = defineProps<{
  lastModifiedAt: string | null | undefined
  lastModifiedByName: string | null | undefined
  signedAt: string | null | undefined
  signedByName: string | null | undefined
}>()

const { t, locale } = useI18n()

const isDuplicate = computed(() =>
  !!props.lastModifiedAt &&
  !!props.signedAt &&
  formatDate(props.lastModifiedAt, locale.value) === formatDate(props.signedAt, locale.value) &&
  props.lastModifiedByName === props.signedByName,
)
</script>

<template>
  <div
    v-if="lastModifiedAt || signedAt"
    class="-mt-3 mb-2 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-light/50"
  >
    <span v-if="lastModifiedAt && !isDuplicate">
      {{ t('exchange.audit.lastModified') }}: {{ formatDate(lastModifiedAt, locale) }}
      <template v-if="lastModifiedByName"> — {{ lastModifiedByName }}</template>
    </span>
    <span v-if="lastModifiedAt && !isDuplicate && signedAt" class="text-light/30">·</span>
    <span v-if="signedAt">
      {{ t('exchange.audit.signed') }}: {{ formatDate(signedAt, locale) }}
      <template v-if="signedByName"> — {{ signedByName }}</template>
    </span>
  </div>
</template>
