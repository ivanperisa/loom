<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useTheme } from '@/composables/useTheme'
import { MoonIcon, SunIcon } from '@lucide/vue'

const { t } = useI18n()
const { theme, toggleTheme } = useTheme()
</script>

<template>
  <button
    type="button"
    class="flex h-9 w-9 items-center justify-center rounded-lg text-light/70 transition hover:bg-fill-soft hover:text-light"
    :aria-label="t('common.toggleTheme')"
    :aria-pressed="theme === 'light'"
    @click="toggleTheme"
  >
    <SunIcon v-if="theme === 'dark'" class="h-4 w-4 text-primary-strong" />
    <MoonIcon v-else class="h-4 w-4 text-primary-strong" />
  </button>
</template>
