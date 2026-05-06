<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

defineProps<{ saving: boolean }>()
defineEmits<{ save: []; discard: [] }>()
</script>

<template>
  <div class="mb-4 flex items-center justify-between rounded-xl border border-amber-400/40 bg-amber-500/15 px-4 py-3">
    <div class="flex items-center gap-2">
      <svg class="shrink-0 text-amber-400" width="16" height="16" viewBox="0 0 16 16" fill="none">
        <path d="M8 2L14 13H2L8 2Z" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round"/>
        <path d="M8 6v4M8 11.5v.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
      </svg>
      <span class="text-sm font-medium text-amber-300">{{ t('la.unsavedChanges') }}</span>
    </div>
    <div class="flex gap-2">
      <button
        type="button"
        class="rounded-lg border border-slate-500 px-3 py-1.5 text-xs text-slate-200 transition hover:bg-slate-700/40"
        :disabled="saving"
        @click="$emit('discard')"
      >
        {{ t('la.discard') }}
      </button>
      <button
        type="button"
        class="rounded-lg bg-amber-500 px-4 py-1.5 text-xs font-semibold text-dark transition hover:bg-amber-400 disabled:opacity-60"
        :disabled="saving"
        @click="$emit('save')"
      >
        {{ saving ? t('common.loading') : t('la.save') }}
      </button>
    </div>
  </div>
</template>
