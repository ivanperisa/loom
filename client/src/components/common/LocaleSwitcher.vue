<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { AppLocale } from '@/i18n/index'

withDefaults(defineProps<{ variant?: 'compact' | 'list' }>(), { variant: 'compact' })

const { locale } = useI18n()

const locales: Array<{ code: AppLocale; flag: string; label: string }> = [
  { code: 'hr', flag: 'fi fi-hr', label: 'Hrvatski' },
  { code: 'en', flag: 'fi fi-gb', label: 'English' },
]

function setLocale(code: AppLocale) {
  locale.value = code
  localStorage.setItem('locale', code)
}
</script>

<template>
  <template v-for="loc in locales.filter((l) => l.code !== locale)" :key="loc.code">
    <button
      v-if="variant === 'compact'"
      type="button"
      class="flex h-9 items-center gap-1.5 rounded-lg px-2.5 text-xs font-medium text-light/60 transition hover:bg-white/10 hover:text-light"
      @click="setLocale(loc.code)"
    >
      <span :class="loc.flag" aria-hidden="true"></span>
      {{ loc.code.toUpperCase() }}
    </button>
    <button
      v-else
      type="button"
      class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm text-light/80 transition hover:bg-white/5 hover:text-white"
      @click="setLocale(loc.code)"
    >
      <span :class="[loc.flag, 'text-base']" aria-hidden="true"></span>
      {{ loc.label }}
    </button>
  </template>
</template>
