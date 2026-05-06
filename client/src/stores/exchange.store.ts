import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { i18n } from '@/i18n'
import { exchangeService } from '@/services/exchange.service'
import type {
  ExchangeSummaryResponse,
  ExchangeResponse,
  LearningAgreementResponse,
  CreateExchangeRequest,
  UpdateLearningAgreementStatusRequest,
  UpdateCoordinatorMessageRequest,
  LocalSlotState,
  LocalSlotMapping,
  SlotMode,
  ExchangeSnapshotResponse,
  LearningAgreementEntryUpsertDto,
} from '@/types/exchange.types'
import type { ForeignCourseResponse } from '@/types/institution.types'

function buildLocalFromServer(la: LearningAgreementResponse): LocalSlotState[] {
  const map = new Map<string, LocalSlotState>()
  for (const entry of la.entries) {
    if (!map.has(entry.courseSlotId)) {
      map.set(entry.courseSlotId, { courseSlotId: entry.courseSlotId, mode: entry.mode, mappings: [] })
    }
    if (entry.foreignCourseId !== null) {
      map.get(entry.courseSlotId)!.mappings.push({
        localId: entry.id,
        foreignCourseId: entry.foreignCourseId!,
        foreignCourseCode: entry.foreignCourseCode ?? '',
        foreignCourseNameEn: entry.foreignCourseNameEn ?? '',
        foreignCourseNameHr: entry.foreignCourseNameHr ?? null,
        awardedEcts: entry.awardedEcts ?? 0,
      })
    }
  }
  return Array.from(map.values())
}

export const useExchangeStore = defineStore('exchange', () => {
  const t = i18n.global.t

  const summaries = ref<ExchangeSummaryResponse[]>([])
  const exchange = ref<ExchangeResponse | null>(null)
  const serverLearningAgreement = ref<LearningAgreementResponse | null>(null)
  const localSlotStates = ref<LocalSlotState[]>([])
  const isDirty = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const draggingCourse = ref<ForeignCourseResponse | null>(null)
  const snapshots = ref<ExchangeSnapshotResponse[]>([])

  const slots = computed(() => serverLearningAgreement.value?.slots ?? [])

  function startDrag(course: ForeignCourseResponse) {
    draggingCourse.value = course
  }

  function endDrag() {
    draggingCourse.value = null
  }

  // ── Local mutations (sync, no API calls) ──────────────────────────────────

  function localSetSlotMode(courseSlotId: string, mode: SlotMode) {
    const existing = localSlotStates.value.find(s => s.courseSlotId === courseSlotId)
    if (existing) {
      existing.mode = mode
      if (mode !== 'AtExchange') existing.mappings = []
    } else {
      localSlotStates.value.push({ courseSlotId, mode, mappings: [] })
    }
    isDirty.value = true
  }

  function localRemoveSlotState(courseSlotId: string) {
    localSlotStates.value = localSlotStates.value.filter(s => s.courseSlotId !== courseSlotId)
    isDirty.value = true
  }

  function localAddSlotMapping(courseSlotId: string, mapping: LocalSlotMapping) {
    const state = localSlotStates.value.find(s => s.courseSlotId === courseSlotId)
    if (state) {
      state.mappings.push(mapping)
      isDirty.value = true
    }
  }

  function localRemoveSlotMapping(courseSlotId: string, localId: string) {
    const state = localSlotStates.value.find(s => s.courseSlotId === courseSlotId)
    if (state) {
      state.mappings = state.mappings.filter(m => m.localId !== localId)
      isDirty.value = true
    }
  }

  // ── Fetch actions ─────────────────────────────────────────────────────────

  async function fetchMySummaries() {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getMine()
      summaries.value = res.data
    } catch {
      error.value = t('common.error')
    } finally {
      loading.value = false
    }
  }

  async function deleteExchange(exchangeId: string) {
    await exchangeService.deleteExchange(exchangeId)
    summaries.value = summaries.value.filter(s => s.id !== exchangeId)
  }

  async function fetchExchange(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getById(exchangeId)
      exchange.value = res.data
    } catch {
      error.value = t('common.error')
    } finally {
      loading.value = false
    }
  }

  async function createExchange(request: CreateExchangeRequest) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.create(request)
      exchange.value = res.data
      return res.data
    } catch {
      error.value = t('common.error')
      return null
    } finally {
      loading.value = false
    }
  }

  async function fetchLearningAgreement(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getLearningAgreement(exchangeId)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch {
      error.value = t('common.error')
    } finally {
      loading.value = false
    }
  }

  // ── Batch save ────────────────────────────────────────────────────────────

  async function saveLearningAgreement(exchangeId: string) {
    try {
      const entries: LearningAgreementEntryUpsertDto[] = []
      for (const s of localSlotStates.value) {
        if (s.mode !== 'AtExchange' || s.mappings.length === 0) {
          entries.push({ courseSlotId: s.courseSlotId, mode: s.mode, foreignCourseId: null, awardedEcts: null })
        } else {
          for (const m of s.mappings) {
            entries.push({ courseSlotId: s.courseSlotId, mode: s.mode, foreignCourseId: m.foreignCourseId, awardedEcts: m.awardedEcts })
          }
        }
      }
      const res = await exchangeService.saveLearningAgreement(exchangeId, { entries })
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch (e: unknown) {
      error.value = t('common.error')
      throw e
    }
  }

  // ── Status & coordinator ──────────────────────────────────────────────────

  async function updateLearningAgreementStatus(exchangeId: string, request: UpdateLearningAgreementStatusRequest) {
    try {
      const res = await exchangeService.updateLearningAgreementStatus(exchangeId, request)
      exchange.value = res.data
      if (serverLearningAgreement.value) {
        serverLearningAgreement.value = { ...serverLearningAgreement.value, status: request.status }
      }
    } catch {
      error.value = t('common.error')
    }
  }

  async function updateCoordinatorMessage(exchangeId: string, request: UpdateCoordinatorMessageRequest) {
    try {
      const res = await exchangeService.updateCoordinatorMessage(exchangeId, request)
      exchange.value = res.data
    } catch {
      error.value = t('common.error')
    }
  }

  // ── Snapshots ─────────────────────────────────────────────────────────────

  async function fetchSnapshots(exchangeId: string) {
    try {
      const res = await exchangeService.getSnapshots(exchangeId)
      snapshots.value = res.data
    } catch {
      // Snapshots are non-critical — fail silently
    }
  }

  async function fetchSnapshot(exchangeId: string, snapshotId: string): Promise<ExchangeSnapshotResponse | null> {
    try {
      const res = await exchangeService.getSnapshot(exchangeId, snapshotId)
      const idx = snapshots.value.findIndex(s => s.id === snapshotId)
      if (idx !== -1) snapshots.value[idx] = res.data
      return res.data
    } catch {
      return null
    }
  }

  return {
    summaries, exchange, serverLearningAgreement, localSlotStates, isDirty, slots,
    loading, error, draggingCourse, snapshots,
    startDrag, endDrag,
    localSetSlotMode, localRemoveSlotState, localAddSlotMapping, localRemoveSlotMapping,
    fetchMySummaries, fetchExchange, createExchange, deleteExchange,
    fetchLearningAgreement, saveLearningAgreement,
    updateLearningAgreementStatus, updateCoordinatorMessage,
    fetchSnapshots, fetchSnapshot,
  }
})
