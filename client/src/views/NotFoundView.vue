<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-dark px-4">
    <div class="text-center">
      <p class="text-5xl font-bold text-primary-strong">404</p>
      <h1 class="mt-3 text-xl font-semibold text-light">{{ t('notFound.title') }}</h1>
      <p class="mt-2 text-sm text-light/60">{{ t('notFound.message') }}</p>
      <RouterLink
        to="/"
        class="mt-6 inline-block rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
      >
        {{ t('notFound.home') }}
      </RouterLink>
    </div>
  </main>
</template>
