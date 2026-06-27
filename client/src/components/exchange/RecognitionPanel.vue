<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useAuthStore } from '@/stores/auth.store'
import type { RecognitionEntryResponse } from '@/types/recognition.types'
import type { MappingSchemeEntryResponse } from '@/types/mappingScheme.types'
import { exportExchangeExcel } from '@/utils/exportExchange'
import StatusBadge from '@/components/common/StatusBadge.vue'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import RecognitionHistoryDrawer from '@/components/exchange/RecognitionHistoryDrawer.vue'
import RecognitionTable from '@/components/exchange/RecognitionTable.vue'
import ActionButton from '@/components/common/ActionButton.vue'
import { documentStatus } from '@/utils/documentStatus'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const authStore = useAuthStore()
const exchangeStore = useExchangeStore()

const loading = ref(true)
const isSaving = ref(false)

interface GradeData {
  enrollmentStatus: string
  originalGrade: string
  ectsGrade: string
  hrGrade: string
  examDate: string
}

const isCoordinator = computed(() => authStore.canActAsCoordinator)

// Top table: the agreed mapping straight from the learning agreement (read-only).
const topEntries = computed<RecognitionEntryResponse[]>(
  () => exchangeStore.serverRecognition?.entries ?? [],
)

// Phase 2 begins once mapping_scheme_entry rows exist for the exchange.
const isPhase2 = computed(
  () => (exchangeStore.serverMappingScheme?.entries.length ?? 0) > 0,
)

// Bottom table: editable grades. Phase 1 -> recognition entries, phase 2 -> mapping scheme entries.
const bottomEntries = computed<(RecognitionEntryResponse | MappingSchemeEntryResponse)[]>(() =>
  isPhase2.value
    ? (exchangeStore.serverMappingScheme?.entries ?? [])
    : (exchangeStore.serverRecognition?.entries ?? []),
)

const editableGrades = reactive<Record<string, GradeData>>({})

function initGrades() {
  for (const key of Object.keys(editableGrades)) delete editableGrades[key]
  const seen = new Set<string>()
  for (const entry of bottomEntries.value) {
    if (seen.has(entry.partnerCourseCode)) continue
    seen.add(entry.partnerCourseCode)
    editableGrades[entry.partnerCourseCode] = {
      enrollmentStatus: entry.enrollmentStatus ?? '',
      originalGrade: entry.originalGrade ?? '',
      ectsGrade: entry.ectsGrade ?? '',
      hrGrade: entry.hrGrade ?? '',
      examDate: entry.examDate ?? '',
    }
  }
}

const hasUnsavedChanges = computed(() => {
  const firstByCode = new Map<string, (typeof bottomEntries.value)[number]>()
  for (const e of bottomEntries.value) {
    if (!firstByCode.has(e.partnerCourseCode)) firstByCode.set(e.partnerCourseCode, e)
  }
  return Array.from(firstByCode.entries()).some(([code, e]) => {
    const g = editableGrades[code]
    if (!g) return false
    return (
      (e.enrollmentStatus ?? '') !== g.enrollmentStatus ||
      (e.originalGrade ?? '') !== g.originalGrade ||
      (e.ectsGrade ?? '') !== g.ectsGrade ||
      (e.hrGrade ?? '') !== g.hrGrade ||
      (e.examDate ?? '') !== g.examDate
    )
  })
})

onMounted(async () => {
  try {
    if (!exchangeStore.serverRecognition) {
      await exchangeStore.fetchRecognition(props.exchangeId)
    }
    initGrades()
  } finally {
    loading.value = false
  }
})

async function saveAll() {
  isSaving.value = true
  try {
    const entriesToSave = (exchangeStore.serverRecognition?.entries ?? []).map((e) => {
      const g = editableGrades[e.partnerCourseCode]
      return {
        learningAgreementEntryId: e.learningAgreementEntryId,
        enrollmentStatus: g?.enrollmentStatus || null,
        originalGrade: g?.originalGrade || null,
        ectsGrade: g?.ectsGrade || null,
        hrGrade: g?.hrGrade || null,
        examDate: g?.examDate || null,
      }
    })
    await exchangeStore.saveRecognition(props.exchangeId, { entries: entriesToSave })
    initGrades()
  } finally {
    isSaving.value = false
  }
}

async function signRecognition() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, { status: documentStatus.Approved })
}
async function backToRecognitionDraft() {
  await exchangeStore.updateRecognitionStatus(props.exchangeId, { status: documentStatus.Draft })
}
function discardChanges() {
  initGrades()
}

function doExport() {
  if (!exchangeStore.serverRecognition) return
  exportExchangeExcel(
    exchangeStore.serverRecognition,
    exchangeStore.serverLearningAgreement!,
    exchangeStore.exchange!,
    locale.value,
  )
}

const showHistory = ref(false)

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(locale.value === 'hr' ? 'hr-HR' : 'en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<template>
  <div>
    <RecognitionHistoryDrawer
      v-if="showHistory"
      :exchange-id="exchangeId"
      @close="showHistory = false"
    />

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <template v-else-if="exchangeStore.serverRecognition">
      <!-- Status + actions bar -->
      <div class="relative mb-3 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3">
          <StatusBadge :status="exchangeStore.serverRecognition!.status" i18n-prefix="recognitionStatus" />
        </div>
        <span
          class="pointer-events-none absolute left-1/2 -translate-x-1/2 text-sm font-semibold text-light/80"
        >
          {{ homeProfileName }}
        </span>
        <div class="flex flex-wrap gap-2">
          <ActionButton size="md" @click="doExport">{{ t('recognition.export') }}</ActionButton>
          <ActionButton size="md" @click="showHistory = true">
            <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
            {{ t('recognition.actions.history') }}
          </ActionButton>
          <template v-if="isCoordinator">
            <button
              v-if="exchangeStore.serverRecognition!.status === documentStatus.Draft"
              type="button"
              class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500"
              @click="signRecognition"
            >
              {{ t('exchange.actions.sign') }}
            </button>
            <button
              v-else-if="exchangeStore.serverRecognition!.status === documentStatus.Approved"
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700/40"
              @click="backToRecognitionDraft"
            >
              {{ t('recognition.actions.backToDraft') }}
            </button>
          </template>
        </div>
      </div>

      <!-- Audit info -->
      <div
        v-if="exchangeStore.serverRecognition?.lastModifiedAt || exchangeStore.serverRecognition?.signedAt"
        class="-mt-2 mb-4 flex flex-col gap-y-1 text-xs text-light/50"
      >
        <span v-if="exchangeStore.serverRecognition?.lastModifiedAt">
          {{ t('exchange.audit.lastModified') }}: {{ formatDate(exchangeStore.serverRecognition.lastModifiedAt) }}
          <template v-if="exchangeStore.serverRecognition?.lastModifiedByName"> — {{ exchangeStore.serverRecognition.lastModifiedByName }}</template>
        </span>
        <span v-if="exchangeStore.serverRecognition?.signedAt">
          {{ t('exchange.audit.signed') }}: {{ formatDate(exchangeStore.serverRecognition.signedAt) }}
          <template v-if="exchangeStore.serverRecognition?.signedByName"> — {{ exchangeStore.serverRecognition.signedByName }}</template>
        </span>
      </div>

      <UnsavedChangesBar
        v-if="hasUnsavedChanges"
        :saving="isSaving"
        @save="saveAll"
        @discard="discardChanges"
      />

      <div
        v-if="topEntries.length === 0"
        class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center"
      >
        <p class="text-light/60">{{ t('recognition.noEntries') }}</p>
      </div>

      <template v-else>
        <!-- Top table: agreed mapping (read-only) -->
        <h3 class="mb-2 text-sm font-semibold text-light/80">
          {{ t('recognition.agreedMappingTitle') }}
        </h3>
        <RecognitionTable :entries="topEntries" :readonly="true" />

        <!-- Bottom table: final recognition + grades (editable) -->
        <h3 class="mb-2 mt-8 text-sm font-semibold text-light/80">
          {{ t('recognition.finalRecognitionTitle') }}
        </h3>
        <RecognitionTable :entries="bottomEntries" :readonly="false" :editable-grades="editableGrades" />
        <p class="mt-2 text-xs text-light/50">{{ t('recognition.finalRecognitionHint') }}</p>
      </template>
    </template>
  </div>
</template>
