<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeContext } from '@/composables/useExchangeContext'
import { useLaDraftStore } from '@/stores/laDraft.store'
import { useLearningAgreementMutations } from '@/queries/exchange.queries'
import { useNotification } from '@/composables/useNotification'
import { describeApiError, describeCode } from '@/utils/apiError'
import ActionButton from '@/components/common/ActionButton.vue'
import BaseModal from '@/components/common/BaseModal.vue'
import type { ImportPreviewResponse, ImportRow, MappingExportDto } from '@/types/learningAgreement.types'

const props = defineProps<{
  dto: MappingExportDto
  exchangeId: string
}>()

const emit = defineEmits<{ close: [] }>()

const { t } = useI18n()
const { learningAgreement } = useExchangeContext()
const draft = useLaDraftStore()
const laMutations = useLearningAgreementMutations(() => props.exchangeId)
const { notifySuccess, notifyError } = useNotification()

const preview = ref<ImportPreviewResponse | null>(null)
const loadError = ref<string | null>(null)
const applying = ref(false)

// The server matches the file to this exchange (slots by id, courses by id or code) and runs the save validation.
onMounted(async () => {
  try {
    preview.value = await laMutations.previewImport.mutateAsync(props.dto)
  } catch (error) {
    const { title, message } = describeApiError(error)
    loadError.value = message ?? title
  }
})

const changeCount = computed(() =>
  preview.value ? preview.value.added.length + preview.value.removed.length + preview.value.changed.length : 0,
)
const canApply = computed(() => !!preview.value?.canApply && changeCount.value > 0 && !applying.value)

function rowLabel(row: ImportRow): string {
  if (row.partnerCourseCode) return `${row.partnerCourseCode} ${row.partnerCourseName ?? ''}`.trim()
  return t(`slotMode.${row.mode}`)
}

async function apply() {
  applying.value = true
  try {
    const result = await laMutations.importFile.mutateAsync(props.dto)
    // The imported draft replaces whatever was being edited.
    if (learningAgreement.value) draft.load(props.exchangeId, learningAgreement.value)
    notifySuccess(t('la.import.successTitle'), t('la.import.successMessage', { added: result.added, removed: result.removed, changed: result.changed }))
    emit('close')
  } catch (error) {
    const { title, message } = describeApiError(error)
    notifyError(t('la.import.errorTitle'), message ?? title)
  } finally {
    applying.value = false
  }
}
</script>

<template>
  <BaseModal max-width="max-w-2xl" z-class="z-[60]" labelled-by="import-preview-title" @close="emit('close')">
    <div class="import-dialog">
      <div class="import-header">
        <h2 id="import-preview-title" class="import-title">{{ t('la.import.title') }}</h2>
        <button type="button" class="import-close" :aria-label="t('common.close')" @click="emit('close')">&times;</button>
      </div>

      <div class="import-context">
        <div class="import-context__row">
          <span class="import-context__label">{{ t('la.import.source') }}</span>
          <span class="import-context__value"><strong>{{ dto.exportedByName }}</strong></span>
        </div>
        <div class="import-context__row">
          <span class="import-context__label">{{ t('la.import.contextPartner') }}</span>
          <span class="import-context__value">{{ dto.institution?.name }}<span v-if="dto.institution?.erasmusCode" class="import-context__code"> ({{ dto.institution.erasmusCode }})</span></span>
        </div>
        <div v-if="dto.home" class="import-context__row">
          <span class="import-context__label">{{ t('la.import.contextProfile') }}</span>
          <span class="import-context__value">{{ dto.home.profileName }}</span>
        </div>
      </div>

      <p class="import-note">{{ t('la.import.replaceNote') }}</p>

      <p v-if="loadError" class="import-blocked">{{ loadError }}</p>
      <p v-else-if="!preview" class="import-note">{{ t('common.loading') }}</p>

      <template v-else>
        <div v-if="preview.contextWarnings.length > 0" class="import-mismatch">
          <div class="import-mismatch__title">{{ t('la.import.mismatchTitle') }}</div>
          <div v-for="w in preview.contextWarnings" :key="w.field" class="import-mismatch__item">
            <div class="import-mismatch__field">{{ t(`la.import.mismatch.${w.field}`) }}</div>
            <div class="import-mismatch__row">
              <span class="import-mismatch__side">{{ t('la.import.mismatchFromFile') }}</span>
              <span class="import-mismatch__val import-mismatch__val--bad">{{ w.fromFile }}</span>
            </div>
            <div class="import-mismatch__row">
              <span class="import-mismatch__side">{{ t('la.import.mismatchInExchange') }}</span>
              <span class="import-mismatch__val">{{ w.inExchange }}</span>
            </div>
          </div>
          <div class="import-mismatch__hint">{{ t('la.import.mismatchHint') }}</div>
        </div>

        <p v-if="!preview.canApply" class="import-blocked">{{ describeCode(preview.blockingCode, preview.blockingParams ?? {}, preview.blockingMessage) }}</p>

        <div class="import-table-wrap">
          <table class="import-table">
            <thead>
              <tr>
                <th>{{ t('la.import.colChange') }}</th>
                <th>{{ t('la.import.colHomeCourse') }}</th>
                <th>{{ t('la.import.colPartnerCourse') }}</th>
                <th>ECTS</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="row in preview.added" :key="`a-${row.homeSlotId}-${row.partnerCourseId}`" class="import-row--added">
                <td>{{ t('la.import.added') }}</td>
                <td>{{ row.homeSlotLabel }}</td>
                <td>{{ rowLabel(row) }}</td>
                <td>{{ row.awardedEcts ?? '—' }}</td>
              </tr>
              <tr v-for="row in preview.changed" :key="`c-${row.homeSlotId}-${row.partnerCourseId}`" class="import-row--changed">
                <td>{{ t('la.import.changed') }}</td>
                <td>{{ row.homeSlotLabel }}</td>
                <td>{{ rowLabel(row) }}</td>
                <td>{{ row.previousEcts ?? '—' }} → {{ row.awardedEcts ?? '—' }}</td>
              </tr>
              <tr v-for="row in preview.removed" :key="`r-${row.homeSlotId}-${row.partnerCourseId}`" class="import-row--removed">
                <td>{{ t('la.import.removed') }}</td>
                <td>{{ row.homeSlotLabel }}</td>
                <td>{{ rowLabel(row) }}</td>
                <td>{{ row.awardedEcts ?? '—' }}</td>
              </tr>
              <tr v-for="(skip, i) in preview.skipped" :key="`s-${i}`" class="import-row--skipped">
                <td>{{ t('la.import.skipped') }}</td>
                <td>{{ skip.homeSlotLabel }}</td>
                <td>{{ skip.partnerCourseCode ?? '—' }}</td>
                <td>{{ t(`la.import.skipReason.${skip.reason}`) }}</td>
              </tr>
            </tbody>
          </table>
          <p v-if="changeCount === 0 && preview.canApply" class="import-note">{{ t('la.import.nothingToChange') }}</p>
          <p v-if="preview.unchanged > 0" class="import-note">{{ t('la.import.unchanged', { n: preview.unchanged }) }}</p>
        </div>
      </template>

      <div class="import-footer">
        <ActionButton size="md" @click="emit('close')">{{ t('common.cancel') }}</ActionButton>
        <ActionButton size="md" variant="solid" :disabled="!canApply" @click="apply">
          {{ t('la.import.apply', { n: changeCount }) }}
        </ActionButton>
      </div>
    </div>
  </BaseModal>
</template>

<style scoped>
.import-dialog {
  background: var(--color-dark-2);
  border: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent);
  border-radius: 12px;
  padding: 28px;
  min-width: 520px;
  max-width: 680px;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.import-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.import-title {
  color: var(--color-light);
  font-size: 16px;
  font-weight: 700;
  margin: 0;
}

.import-close {
  color: var(--color-light);
  opacity: 0.5;
  font-size: 22px;
  background: none;
  border: none;
  cursor: pointer;
  line-height: 1;
  padding: 0;
}

.import-close:hover {
  opacity: 0.9;
}

.import-context {
  border: 1px solid color-mix(in srgb, var(--color-light) 10%, transparent);
  border-radius: 8px;
  padding: 10px 14px;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.import-context__row {
  display: flex;
  gap: 8px;
  font-size: 12px;
  align-items: baseline;
}

.import-context__label {
  color: var(--color-light);
  opacity: 0.5;
  min-width: 130px;
  flex-shrink: 0;
}

.import-context__value {
  color: var(--color-light);
  font-weight: 500;
}

.import-context__code {
  opacity: 0.6;
  font-weight: 400;
}


.import-mismatch {
  border: 1px solid color-mix(in srgb, #dc2626 45%, transparent);
  background: color-mix(in srgb, #dc2626 8%, var(--color-dark-2));
  border-radius: 8px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.import-mismatch__title {
  font-size: 12px;
  font-weight: 700;
  color: #dc2626;
}
:global([data-theme='light']) .import-mismatch__title { color: var(--color-danger-text); }

.import-mismatch__item {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.import-mismatch__field {
  font-size: 11px;
  font-weight: 700;
  color: var(--color-light);
  opacity: 0.85;
}

.import-mismatch__row {
  display: flex;
  gap: 6px;
  font-size: 11px;
  align-items: baseline;
}

.import-mismatch__side {
  color: var(--color-light);
  opacity: 0.45;
  min-width: 80px;
  flex-shrink: 0;
}

.import-mismatch__val {
  color: var(--color-light);
  opacity: 0.85;
}

.import-mismatch__val--bad {
  color: #dc2626;
  opacity: 1;
  text-decoration: line-through;
}
:global([data-theme='light']) .import-mismatch__val--bad { color: var(--color-danger-text); }

.import-table-wrap {
  overflow: auto;
  flex: 1;
}

.import-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.import-table thead tr {
  border-bottom: 1px solid color-mix(in srgb, var(--color-light) 15%, transparent);
}

.import-table th {
  text-align: left;
  padding: 6px 8px;
  color: var(--color-light);
  opacity: 0.7;
  font-weight: 600;
}

.import-table tbody tr {
  border-bottom: 1px solid color-mix(in srgb, var(--color-light) 7%, transparent);
}

.import-table td {
  padding: 7px 8px;
  color: var(--color-light);
}

.import-table td:nth-child(2) {
  color: var(--color-light);
}

.import-ects {
  opacity: 0.5;
  font-size: 11px;
}

.import-empty {
  opacity: 0.35;
}


.import-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 10px;
  padding-top: 4px;
  border-top: 1px solid color-mix(in srgb, var(--color-light) 10%, transparent);
}


.import-note {
  color: var(--color-light);
  opacity: 0.6;
  font-size: 12px;
  margin: 0;
}

.import-blocked {
  color: #dc2626;
  font-size: 12px;
  font-weight: 600;
  margin: 0;
}
:global([data-theme='light']) .import-blocked { color: var(--color-danger-text); }

.import-mismatch__hint {
  font-size: 11px;
  color: var(--color-light);
  opacity: 0.7;
}

.import-row--added td:first-child { color: #16a34a; font-weight: 600; }
.import-row--changed td:first-child { color: #d97706; font-weight: 600; }
.import-row--removed td:first-child { color: #dc2626; font-weight: 600; }
.import-row--skipped td { opacity: 0.55; }
:global([data-theme='light']) .import-row--added td:first-child { color: var(--color-success-text); }
:global([data-theme='light']) .import-row--changed td:first-child { color: var(--color-warning-text); }
:global([data-theme='light']) .import-row--removed td:first-child { color: var(--color-danger-text); }

@media (max-width: 640px) {
  .import-dialog { min-width: 0; padding: 20px 16px; }
}
</style>
