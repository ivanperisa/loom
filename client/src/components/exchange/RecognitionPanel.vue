<script setup lang="ts">
import { ref, computed, reactive, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLaDraftStore } from '@/stores/laDraft.store'
import { useExchangeContext } from '@/composables/useExchangeContext'
import { useMappingSchemeQuery, useOfficialDocument, useRecognitionMutations } from '@/queries/exchange.queries'
import { useConfirm } from '@/composables/useConfirm'
import { useNotification } from '@/composables/useNotification'
import { formatDate } from '@/utils/formatDate'
import { documentStatus } from '@/utils/documentStatus'
import type { CourseGradesRequest } from '@/types/recognition.types'
import UnsavedChangesBar from '@/components/common/UnsavedChangesBar.vue'
import RecognitionTable from '@/components/exchange/RecognitionTable.vue'
import DocumentHistoryDrawer from '@/components/exchange/DocumentHistoryDrawer.vue'
import ActionButton from '@/components/common/ActionButton.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import PanelHeaderBar from '@/components/common/PanelHeaderBar.vue'
import AuditInfo from '@/components/common/AuditInfo.vue'
import ErrorAlert from '@/components/common/ErrorAlert.vue'

const props = defineProps<{
  exchangeId: string
  homeProfileName: string
}>()

const { t, locale } = useI18n()
const draft = useLaDraftStore()
const { recognition, recognitionQuery, isCoordinator } = useExchangeContext()
const mappingSchemeQuery = useMappingSchemeQuery(() => props.exchangeId)
const mutations = useRecognitionMutations(() => props.exchangeId)
const officialDocument = useOfficialDocument(() => props.exchangeId)
/** Unsaved LA changes would be left out of the frozen version, so starting waits for them. */
const laHasUnsavedChanges = computed(() => draft.exchangeId === props.exchangeId && draft.isDirty)
const { confirm } = useConfirm()
const { notifySuccess } = useNotification()

const loading = computed(() => recognitionQuery.isPending.value || mappingSchemeQuery.isPending.value)
const loadError = computed(() => recognitionQuery.error.value ?? mappingSchemeQuery.error.value)
const isSaving = computed(() => mutations.saveGrades.isPending.value)
const starting = computed(() => mutations.start.isPending.value)
const showHistory = ref(false)

interface GradeData {
  enrollmentStatus: string
  originalGrade: string
  ectsGrade: string
  hrGrade: string
  examDate: string
}

const isStarted = computed(() => recognition.value?.isStarted ?? false)
const isApproved = computed(() => recognition.value?.status === documentStatus.Approved)

function byCourseName<T extends { partnerCourseName: string | null }>(entries: T[]): T[] {
  return entries.slice().sort((a, b) => (a.partnerCourseName ?? '').localeCompare(b.partnerCourseName ?? ''))
}

// Table 1: the latest approved LA version (computed by the server, never the draft).
const agreedEntries = computed(() => byCourseName(recognition.value?.agreed ?? []).map((e) => ({ ...e, enrollmentStatus: null })))

// Table 2: the results (same data as the mapping scheme). Grades belong to a partner course.
const resultEntries = computed(() => byCourseName(mappingSchemeQuery.data.value?.entries ?? []))

const editableGrades = reactive<Record<string, GradeData>>({})

function initGrades() {
  for (const key of Object.keys(editableGrades)) delete editableGrades[key]
  for (const entry of resultEntries.value) {
    const key = entry.partnerCourseId
    if (key === null || editableGrades[key]) continue
    editableGrades[key] = {
      enrollmentStatus: entry.enrollmentStatus ?? '',
      originalGrade: entry.originalGrade ?? '',
      ectsGrade: entry.ectsGrade ?? '',
      hrGrade: entry.hrGrade ?? '',
      examDate: entry.examDate ?? '',
    }
  }
}

const changedGrades = computed<CourseGradesRequest[]>(() => {
  const seen = new Set<string>()
  const changed: CourseGradesRequest[] = []
  for (const e of resultEntries.value) {
    if (e.partnerCourseId === null || seen.has(e.partnerCourseId)) continue
    seen.add(e.partnerCourseId)
    const g = editableGrades[e.partnerCourseId]
    if (!g) continue
    const differs =
      (e.enrollmentStatus ?? '') !== g.enrollmentStatus ||
      (e.originalGrade ?? '') !== g.originalGrade ||
      (e.ectsGrade ?? '') !== g.ectsGrade ||
      (e.hrGrade ?? '') !== g.hrGrade ||
      (e.examDate ?? '') !== g.examDate
    if (differs) {
      changed.push({
        partnerCourseId: Number(e.partnerCourseId),
        enrollmentStatus: g.enrollmentStatus || null,
        originalGrade: g.originalGrade || null,
        ectsGrade: g.ectsGrade || null,
        hrGrade: g.hrGrade || null,
        examDate: g.examDate || null,
      })
    }
  }
  return changed
})

// Saved grades replace the edits (after load, save, start, or a change to the mapping scheme).
watch(() => mappingSchemeQuery.data.value, initGrades, { immediate: true })

function saveAll() {
  mutations.saveGrades.mutate({ entries: changedGrades.value })
}

async function startFinalRecognition() {
  const ok = await confirm({
    title: t('recognition.start.confirmTitle'),
    message: t('recognition.start.confirmMessage'),
    confirmLabel: t('recognition.start.confirmButton'),
    variant: 'danger',
  })
  if (!ok) return
  try {
    await mutations.start.mutateAsync()
    notifySuccess(t('recognition.start.done'))
  } catch {
    // The toast already says why.
  }
}

function setStatus(status: typeof documentStatus.Draft | typeof documentStatus.Approved) {
  mutations.setStatus.mutate({ status })
}

function downloadOfficial() {
  officialDocument.mutate(locale.value)
}
</script>

<template>
  <div>
    <DocumentHistoryDrawer
      v-if="showHistory"
      :exchange-id="exchangeId"
      document="recognition"
      :can-restore="false"
      @close="showHistory = false"
    />

    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-14 animate-pulse rounded bg-primary/20"></div>
    </div>

    <ErrorAlert v-else-if="loadError" :error="loadError" @retry="recognitionQuery.refetch(); mappingSchemeQuery.refetch()" />

    <template v-else-if="recognition">
      <PanelHeaderBar :home-profile-name="homeProfileName">
        <template #left>
          <StatusBadge v-if="isStarted" :status="recognition.status" />
          <div style="display: flex; gap: 6px;">
            <ActionButton :disabled="officialDocument.isPending.value" :title="t('documents.officialHint')" @click="downloadOfficial">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
              {{ t('documents.official') }}
            </ActionButton>
            <ActionButton v-if="isStarted" @click="showHistory = true">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
              {{ t('recognition.actions.history') }}
            </ActionButton>
          </div>
        </template>
        <template #right>
          <template v-if="isCoordinator && isStarted">
            <button
              v-if="!isApproved"
              type="button"
              class="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-500 disabled:opacity-50"
              :disabled="changedGrades.length > 0"
              :title="changedGrades.length > 0 ? t('recognition.actions.saveFirst') : ''"
              @click="setStatus(documentStatus.Approved)"
            >
              {{ t('recognition.actions.approve') }}
            </button>
            <button
              v-else
              type="button"
              class="rounded-lg border border-slate-500 px-4 py-2 text-sm font-medium text-muted transition hover:bg-slate-700/40"
              @click="setStatus(documentStatus.Draft)"
            >
              {{ t('recognition.actions.backToDraft') }}
            </button>
          </template>
        </template>
      </PanelHeaderBar>

      <AuditInfo
        v-if="isStarted"
        :last-modified-at="recognition.lastModifiedAt"
        :last-modified-by-name="recognition.lastModifiedByName"
        :signed-at="recognition.signedAt"
        :signed-by-name="recognition.signedByName"
      />

      <UnsavedChangesBar
        v-if="isStarted && !isApproved && changedGrades.length > 0"
        :saving="isSaving"
        @save="saveAll"
        @discard="initGrades"
      />

      <!-- Table 1: agreed recognition (frozen once final recognition starts) -->
      <h3 class="mb-2 text-sm font-semibold text-light/80">
        {{ t('recognition.agreedMappingTitle') }}
        <span v-if="recognition.agreedVersionNo" class="font-normal text-light/50">
          · {{ t('recognition.agreedFromVersion', { n: recognition.agreedVersionNo }) }}
        </span>
      </h3>
      <div v-if="agreedEntries.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <p class="text-light/60">{{ t('recognition.noAgreed') }}</p>
      </div>
      <RecognitionTable v-else :entries="agreedEntries" :readonly="true" />

      <!-- Before final recognition: the step that starts it -->
      <section v-if="!isStarted" class="start-card" aria-labelledby="start-final-title">
        <h3 id="start-final-title" class="text-sm font-semibold text-light">{{ t('recognition.start.title') }}</h3>
        <p class="mt-1 text-xs text-light/70">{{ t('recognition.start.explanation') }}</p>
        <div class="mt-3 flex flex-wrap items-center gap-3">
          <button
            type="button"
            class="rounded-lg bg-primary-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="!recognition.canStart || starting || laHasUnsavedChanges"
            @click="startFinalRecognition"
          >
            {{ t('recognition.start.button') }}
          </button>
          <span v-if="!recognition.canStart" class="text-xs text-light/60">{{ t('recognition.start.needsApproval') }}</span>
          <span v-else-if="laHasUnsavedChanges" class="text-xs text-light/60">{{ t('recognition.start.unsavedLa') }}</span>
        </div>
      </section>

      <!-- After: table 2, the results with grades -->
      <template v-else>
        <h3 class="mb-2 mt-8 text-sm font-semibold text-light/80">
          {{ t('recognition.finalRecognitionTitle') }}
          <span class="font-normal text-light/50">
            · {{ t('recognition.startedBy', { date: formatDate(recognition.startedAt ?? '', locale), name: recognition.startedByName ?? '—' }) }}
          </span>
        </h3>
        <RecognitionTable :entries="resultEntries" :readonly="false" :locked="isApproved" :editable-grades="editableGrades" />
        <p class="mt-2 text-xs text-light/50">
          {{ isApproved ? t('recognition.approvedHint') : t('recognition.finalRecognitionHint') }}
        </p>
      </template>
    </template>
  </div>
</template>

<style scoped>
.start-card {
  margin-top: 24px;
  padding: 16px;
  border-radius: 12px;
  border: 1px dashed color-mix(in srgb, var(--color-primary) 45%, transparent);
  background: color-mix(in srgb, var(--color-primary) 6%, transparent);
}
</style>
