<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{
  exchangeId: string
}>()

const { t } = useI18n()
</script>

<template>
  <div class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center text-light/60">
    {{ t('exchange.placeholder.comingSoon') }}
  </div>
</template>
