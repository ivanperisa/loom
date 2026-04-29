import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { exchangeService } from '@/services/exchange.service'
import type {
  ExchangeSummaryResponse,
  ExchangeResponse,
  LearningAgreementResponse,
  CreateExchangeRequest,
  SetSlotModeRequest,
  AddSlotMappingRequest,
  RemoveSlotMappingRequest,
  UpdateExchangeStatusRequest,
  UpdateCoordinatorMessageRequest,
  LocalSlotState,
  LocalSlotMapping,
  SlotMode,
  ExchangeSnapshotResponse,
} from '@/types/exchange.types'
import type { ForeignCourseResponse } from '@/types/institution.types'

function buildLocalFromServer(la: LearningAgreementResponse): LocalSlotState[] {
  return la.slotStates.map(s => ({
    courseSlotId: s.courseSlotId,
    mode: s.mode,
    mappings: s.mappings.map(m => ({
      localId: m.id,
      foreignCourseId: m.foreignCourseId,
      foreignCourseCode: m.foreignCourseCode,
      foreignCourseNameEn: m.foreignCourseNameEn,
      foreignCourseNameHr: m.foreignCourseNameHr,
      awardedEcts: m.awardedEcts,
    })),
  }))
}

export const useExchangeStore = defineStore('exchange', () => {
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
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu razmjena.'
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
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu razmjene.'
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
    } catch (e: unknown) {
      error.value = 'Greška pri kreiranju razmjene.'
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
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu learning agreementa.'
    } finally {
      loading.value = false
    }
  }

  // ── Batch save ────────────────────────────────────────────────────────────

  async function saveLearningAgreement(exchangeId: string) {
    try {
      const res = await exchangeService.saveLearningAgreement(exchangeId, {
        slotStates: localSlotStates.value.map(s => ({
          courseSlotId: s.courseSlotId,
          mode: s.mode,
          mappings: s.mappings.map(m => ({
            foreignCourseId: m.foreignCourseId,
            awardedEcts: m.awardedEcts,
          })),
        })),
      })
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch (e: unknown) {
      error.value = 'Greška pri spremanju learning agreementa.'
      throw e
    }
  }

  // ── Legacy individual mutations (kept, unused by UI) ─────────────────────

  async function setSlotMode(exchangeId: string, request: SetSlotModeRequest) {
    try {
      const res = await exchangeService.setSlotMode(exchangeId, request)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch (e: unknown) {
      error.value = 'Greška pri postavljanju stanja ćelije.'
    }
  }

  async function addSlotMapping(exchangeId: string, request: AddSlotMappingRequest) {
    try {
      const res = await exchangeService.addSlotMapping(exchangeId, request)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch (e: unknown) {
      error.value = 'Greška pri dodavanju mapiranja.'
    }
  }

  async function removeSlotMapping(exchangeId: string, request: RemoveSlotMappingRequest) {
    try {
      const res = await exchangeService.removeSlotMapping(exchangeId, request)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch (e: unknown) {
      error.value = 'Greška pri uklanjanju mapiranja.'
    }
  }

  async function removeSlotState(exchangeId: string, courseSlotId: string) {
    try {
      const res = await exchangeService.removeSlotState(exchangeId, courseSlotId)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
    } catch {
      error.value = 'Greška pri uklanjanju stanja.'
    }
  }

  // ── Status & coordinator ──────────────────────────────────────────────────

  async function updateStatus(exchangeId: string, request: UpdateExchangeStatusRequest) {
    try {
      const res = await exchangeService.updateStatus(exchangeId, request)
      exchange.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri promjeni statusa.'
    }
  }

  async function updateCoordinatorMessage(exchangeId: string, request: UpdateCoordinatorMessageRequest) {
    try {
      const res = await exchangeService.updateCoordinatorMessage(exchangeId, request)
      exchange.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri ažuriranju poruke.'
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
    setSlotMode, addSlotMapping, removeSlotMapping, removeSlotState,
    updateStatus, updateCoordinatorMessage,
    fetchSnapshots, fetchSnapshot,
  }
})
