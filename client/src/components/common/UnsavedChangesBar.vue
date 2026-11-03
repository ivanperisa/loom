<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { TriangleAlertIcon } from '@lucide/vue'

const { t } = useI18n()

defineProps<{ saving: boolean }>()
defineEmits<{ save: []; discard: [] }>()
</script>

<template>
  <div class="mb-4 flex items-center justify-between rounded-xl border border-amber-400/40 bg-amber-500/15 px-4 py-3">
    <div class="flex items-center gap-2">
      <TriangleAlertIcon class="shrink-0 text-amber-400" :size="16" />
      <span class="text-sm font-medium text-info-text">{{ t('learningAgreement.unsavedChanges') }}</span>
    </div>
    <div class="flex gap-2">
      <button
        type="button"
        class="rounded-lg border border-slate-500 px-3 py-1.5 text-xs text-muted transition hover:bg-slate-700/40"
        :disabled="saving"
        @click="$emit('discard')"
      >
        {{ t('learningAgreement.discard') }}
      </button>
      <button
        type="button"
        class="rounded-lg bg-amber-500 px-4 py-1.5 text-xs font-semibold text-dark transition hover:bg-amber-400 disabled:opacity-60"
        :disabled="saving"
        @click="$emit('save')"
      >
        {{ saving ? t('common.loading') : t('learningAgreement.save') }}
      </button>
    </div>
  </div>
</template>
