<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ActionButton from '@/components/common/ActionButton.vue'
import DocumentHistoryDrawer from '@/components/exchange/DocumentHistoryDrawer.vue'
import ImportPreviewModal from '@/components/exchange/ImportPreviewModal.vue'
import { useLearningAgreementMutations } from '@/queries/exchange.queries'
import { useNotification } from '@/composables/useNotification'
import type { MappingExportDto } from '@/types/learningAgreement.types'

/** The learning agreement's document actions: export/import (JSON) and the version history. */
const props = defineProps<{
  exchangeId: string
  /** Import and restore change the draft, so they need an editable learning agreement. */
  editable: boolean
}>()

const { t } = useI18n()
const { notifyError } = useNotification()
const learningAgreementMutations = useLearningAgreementMutations(() => props.exchangeId)

const showHistory = ref(false)
const importDto = ref<MappingExportDto | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

async function readImportFile(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  try {
    importDto.value = JSON.parse(await file.text()) as MappingExportDto
  } catch {
    notifyError(t('learningAgreement.import.invalidJson'))
  }
}
</script>

<template>
  <div class="flex gap-1.5">
    <ActionButton :disabled="learningAgreementMutations.exportFile.isPending.value" :title="t('learningAgreement.actions.exportHint')" @click="learningAgreementMutations.exportFile.mutate()">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
      {{ t('learningAgreement.actions.export') }}
    </ActionButton>
    <ActionButton :disabled="!editable" :title="editable ? '' : t('learningAgreement.import.lockedHint')" @click="fileInput?.click()">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
      {{ t('learningAgreement.actions.import') }}
    </ActionButton>
    <ActionButton @click="showHistory = true">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
      {{ t('learningAgreement.actions.history') }}
    </ActionButton>

    <input ref="fileInput" type="file" accept=".json" class="hidden" :aria-label="t('learningAgreement.actions.import')" @change="readImportFile" />

    <DocumentHistoryDrawer
      v-if="showHistory"
      :exchange-id="exchangeId"
      document="learningAgreement"
      :can-restore="editable"
      @close="showHistory = false"
    />
    <ImportPreviewModal v-if="importDto" :dto="importDto" :exchange-id="exchangeId" @close="importDto = null" />
  </div>
</template>
