<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ActionButton from '@/components/common/ActionButton.vue'
import DocumentHistoryDrawer from '@/components/exchange/DocumentHistoryDrawer.vue'
import ImportPreviewModal from '@/components/exchange/ImportPreviewModal.vue'
import { useLearningAgreementMutations, useOfficialDocument } from '@/queries/exchange.queries'
import { useNotification } from '@/composables/useNotification'
import type { MappingExportDto } from '@/types/learningAgreement.types'

/** The LA's document actions: official xlsx, export/import (JSON) and the version history. */
const props = defineProps<{
  exchangeId: string
  /** Import and restore change the draft, so they need an editable LA. */
  editable: boolean
}>()

const { t, locale } = useI18n()
const { notifyError } = useNotification()
const laMutations = useLearningAgreementMutations(() => props.exchangeId)
const officialDocument = useOfficialDocument(() => props.exchangeId)

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
    notifyError(t('la.import.invalidJson'))
  }
}
</script>

<template>
  <div class="flex gap-1.5">
    <ActionButton :disabled="officialDocument.isPending.value" :title="t('documents.officialHint')" @click="officialDocument.mutate(locale)">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
      {{ t('documents.official') }}
    </ActionButton>
    <ActionButton :disabled="laMutations.exportFile.isPending.value" :title="t('la.actions.exportHint')" @click="laMutations.exportFile.mutate()">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
      {{ t('la.actions.export') }}
    </ActionButton>
    <ActionButton :disabled="!editable" :title="editable ? '' : t('la.import.lockedHint')" @click="fileInput?.click()">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>
      {{ t('la.actions.import') }}
    </ActionButton>
    <ActionButton @click="showHistory = true">
      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
      {{ t('la.actions.history') }}
    </ActionButton>

    <input ref="fileInput" type="file" accept=".json" class="hidden" :aria-label="t('la.actions.import')" @change="readImportFile" />

    <DocumentHistoryDrawer
      v-if="showHistory"
      :exchange-id="exchangeId"
      document="la"
      :can-restore="editable"
      @close="showHistory = false"
    />
    <ImportPreviewModal v-if="importDto" :dto="importDto" :exchange-id="exchangeId" @close="importDto = null" />
  </div>
</template>
