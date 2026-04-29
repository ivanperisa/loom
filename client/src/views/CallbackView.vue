<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-dark px-6 text-center text-light">
    <p class="text-lg font-semibold">{{ t('callback.signingIn') }}</p>
  </main>
</template>
