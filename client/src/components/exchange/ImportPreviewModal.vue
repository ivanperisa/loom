<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useNotification } from '@/composables/useNotification'
import ActionButton from '@/components/common/ActionButton.vue'
import type { MappingExportDto, MappingImportResult } from '@/types/learningAgreement.types'

const props = defineProps<{
  dto: MappingExportDto
  exchangeId: string
}>()

const emit = defineEmits<{
  close: []
  imported: [result: MappingImportResult]
}>()

const { t } = useI18n()
const exchangeStore = useExchangeStore()
const { notifySuccess, notifyError } = useNotification()

interface Mismatch {
  field: string
  fromFile: string
  inExchange: string
}

const mismatches = computed((): Mismatch[] => {
  const ex = exchangeStore.exchange
  if (!ex || !props.dto.home) return []
  const norm = (s: string) => s.trim().toLowerCase()
  const result: Mismatch[] = []

  if (norm(props.dto.institution.name) !== norm(ex.partnerInstitutionName)) {
    const fileVal = props.dto.institution.erasmusCode
      ? `${props.dto.institution.name} (${props.dto.institution.erasmusCode})`
      : props.dto.institution.name
    result.push({ field: t('la.import.mismatchPartner'), fromFile: fileVal, inExchange: ex.partnerInstitutionName })
  }

  if (props.dto.home.institutionName && norm(props.dto.home.institutionName) !== norm(ex.homeInstitutionName)) {
    result.push({ field: t('la.import.mismatchInstitution'), fromFile: props.dto.home.institutionName, inExchange: ex.homeInstitutionName })
  }

  if (props.dto.home.programName && norm(props.dto.home.programName) !== norm(ex.homeProgramName)) {
    result.push({ field: t('la.import.mismatchProgram'), fromFile: props.dto.home.programName, inExchange: ex.homeProgramName })
  }

  if (norm(props.dto.home.profileName) !== norm(ex.homeProfile.name)) {
    result.push({ field: t('la.import.mismatchProfile'), fromFile: props.dto.home.profileName, inExchange: ex.homeProfile.name })
  }

  return result
})

const applicableCount = computed(() => {
  const ids = new Set(
    props.dto.mappings
      .filter((m) => m.partnerCourse !== null)
      .map((m) => m.partnerCourse!.id),
  )
  return ids.size
})


async function apply() {
  try {
    const result = await exchangeStore.importMappings(props.exchangeId, props.dto)
    notifySuccess(t('la.import.successTitle'), t('la.import.successMessage', { count: result.appliedCount }))
    emit('imported', result)
    emit('close')
  } catch {
    notifyError(t('la.import.errorTitle'))
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      class="import-overlay"
      @mousedown.self="emit('close')"
    >
      <div class="import-dialog">
        <div class="import-header">
          <h2 class="import-title">{{ t('la.import.title') }}</h2>
          <button type="button" class="import-close" @click="emit('close')">&times;</button>
        </div>

        <div class="import-context">
          <div class="import-context__row">
            <span class="import-context__label">{{ t('la.import.source') }}</span>
            <span class="import-context__value"><strong>{{ dto.exportedByName }}</strong></span>
          </div>
          <div class="import-context__row">
            <span class="import-context__label">{{ t('la.import.contextPartner') }}</span>
            <span class="import-context__value">{{ dto.institution.name }}<span v-if="dto.institution.erasmusCode" class="import-context__code"> ({{ dto.institution.erasmusCode }})</span></span>
          </div>
          <template v-if="dto.home">
            <div class="import-context__row">
              <span class="import-context__label">{{ t('la.import.contextInstitution') }}</span>
              <span class="import-context__value">{{ dto.home.institutionName }}</span>
            </div>
            <div class="import-context__row">
              <span class="import-context__label">{{ t('la.import.contextProgram') }}</span>
              <span class="import-context__value">{{ dto.home.programName }}</span>
            </div>
            <div class="import-context__row">
              <span class="import-context__label">{{ t('la.import.contextProfile') }}</span>
              <span class="import-context__value">{{ dto.home.profileName }}</span>
            </div>
          </template>
        </div>

        <div v-if="mismatches.length > 0" class="import-mismatch">
          <div class="import-mismatch__title">{{ t('la.import.mismatchTitle') }}</div>
          <div v-for="m in mismatches" :key="m.field" class="import-mismatch__item">
            <div class="import-mismatch__field">{{ m.field }}</div>
            <div class="import-mismatch__row">
              <span class="import-mismatch__side">{{ t('la.import.mismatchFromFile') }}</span>
              <span class="import-mismatch__val import-mismatch__val--bad">{{ m.fromFile }}</span>
            </div>
            <div class="import-mismatch__row">
              <span class="import-mismatch__side">{{ t('la.import.mismatchInExchange') }}</span>
              <span class="import-mismatch__val">{{ m.inExchange }}</span>
            </div>
          </div>
        </div>

        <div class="import-table-wrap">
          <table class="import-table">
            <thead>
              <tr>
                <th>{{ t('la.import.colHomeCourse') }}</th>
                <th>{{ t('la.import.colPartnerCourse') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="entry in dto.mappings" :key="entry.homeSlotId">
                <td>
                  {{ entry.homeSlotLabel }}
                  <span class="import-ects"> · {{ entry.homeSlotEcts }} ECTS</span>
                </td>
                <td>
                  <template v-if="entry.partnerCourse">
                    {{ entry.partnerCourse.code }} — {{ entry.partnerCourse.name }}
                    <span class="import-ects"> ({{ entry.partnerCourse.ects }} ECTS)</span>
                  </template>
                  <span v-else class="import-empty">—</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="import-footer">
          <ActionButton size="md" @click="emit('close')">{{ t('common.cancel') }}</ActionButton>
          <ActionButton
            size="md"
            variant="solid"
            :disabled="applicableCount === 0 || mismatches.length > 0"
            :style="(applicableCount === 0 || mismatches.length > 0) ? 'opacity: 0.4; cursor: not-allowed;' : ''"
            @click="apply"
          >
            {{ t('la.import.apply') }}
          </ActionButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.import-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 60;
}

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

.import-table-wrap {
  overflow-y: auto;
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

</style>
