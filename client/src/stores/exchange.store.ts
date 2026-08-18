import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { i18n } from '@/i18n'
import { exchangeService } from '@/services/exchange.service'
import { learningAgreementService } from '@/services/learningAgreement.service'
import { recognitionService } from '@/services/recognition.service'
import { mappingSchemeService } from '@/services/mappingScheme.service'
import { institutionService } from '@/services/institution.service'
import { slotMode } from '@/utils/slotMode'
import { extractApiError } from '@/utils/apiError'
import { useNotification } from '@/composables/useNotification'
import type {
  ExchangeSummaryResponse,
  ExchangeResponse,
  CreateExchangeRequest,
  UpdateCoordinatorMessageRequest,
  UpdateExchangeRequest,
} from '@/types/exchange.types'
import type {
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
  LocalSlotState,
  LocalSlotMapping,
  SlotMode,
  LearningAgreementEntryUpsertDto,
} from '@/types/learningAgreement.types'
import type { PartnerCourseResponse } from '@/types/institution.types'
import type {
  RecognitionResponse,
  SaveRecognitionRequest,
  UpdateRecognitionStatusRequest,
  RecognitionSnapshotSummary,
} from '@/types/recognition.types'
import type {
  MappingSchemeResponse,
  SaveMappingSchemeRequest,
} from '@/types/mappingScheme.types'
import type {
  LaSnapshotSummary,
  SnapshotListItem,
  MappingExportDto,
  MappingImportResult,
} from '@/types/learningAgreement.types'

function buildLocalFromServer(la: LearningAgreementResponse): LocalSlotState[] {
  const map = new Map<string, LocalSlotState>()
  for (const entry of la.entries.filter((e) => !e.isDeleted)) {
    if (!map.has(entry.homeSlotId)) {
      map.set(entry.homeSlotId, {
        homeSlotId: entry.homeSlotId,
        mode: entry.mode,
        mappings: [],
      })
    }
    if (entry.partnerCourseId !== null) {
      map.get(entry.homeSlotId)!.mappings.push({
        localId: entry.id,
        partnerCourseId: entry.partnerCourseId!,
        partnerCourseCode: entry.partnerCourseCode ?? '',
        partnerCourseName: entry.partnerCourseName ?? '',
        partnerCourseNameHr: entry.partnerCourseNameHr ?? null,
        partnerCourseUrl: entry.partnerCourseUrl ?? null,
        awardedEcts: entry.awardedEcts ?? 0,
        amendmentNumber: entry.amendmentNumber,
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
  const serverRecognition = ref<RecognitionResponse | null>(null)
  const serverMappingScheme = ref<MappingSchemeResponse | null>(null)
  const localSlotStates = ref<LocalSlotState[]>([])
  const isDirty = ref(false)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const draggingCourse = ref<PartnerCourseResponse | null>(null)
  const draggingSlotMapping = ref<{ fromSlotId: string; localId: string } | null>(null)
  const stagedPartnerCourseIds = ref<Set<string>>(new Set())
  const guestMode = ref(false)

  const partnerCourses = ref<PartnerCourseResponse[]>([])
  const partnerCoursesLoading = ref(false)
  let partnerCoursesRequestId: string | null = null

  function setGuestMode(value: boolean) {
    guestMode.value = value
  }

  const slots = computed(() => serverLearningAgreement.value?.slots ?? [])

  function startDrag(course: PartnerCourseResponse) {
    draggingCourse.value = course
    draggingSlotMapping.value = null
  }

  function endDrag() {
    draggingCourse.value = null
    draggingSlotMapping.value = null
  }

  function startSlotDrag(fromSlotId: string, localId: string) {
    draggingSlotMapping.value = { fromSlotId, localId }
    draggingCourse.value = null
  }

  function localMoveSlotMapping(fromSlotId: string, toSlotId: string, localId: string, amount?: number) {
    const fromState = localSlotStates.value.find((s) => s.homeSlotId === fromSlotId)
    if (!fromState) return
    const mappingIdx = fromState.mappings.findIndex((m) => m.localId === localId)
    if (mappingIdx === -1) return
    const mapping = fromState.mappings[mappingIdx]!
    const moveAmount = amount === undefined ? mapping.awardedEcts : Math.min(amount, mapping.awardedEcts)
    if (moveAmount <= 0) return

    let toState = localSlotStates.value.find((s) => s.homeSlotId === toSlotId)
    if (!toState) {
      toState = { homeSlotId: toSlotId, mode: slotMode.AtExchange, mappings: [] }
      localSlotStates.value.push(toState)
    } else if (toState.mode !== slotMode.AtExchange) {
      toState.mode = slotMode.AtExchange
    }

    const existing = toState.mappings.find((m) => m.partnerCourseCode === mapping.partnerCourseCode)

    if (moveAmount >= mapping.awardedEcts) {
      fromState.mappings.splice(mappingIdx, 1)
      if (existing) {
        existing.awardedEcts = Math.round((existing.awardedEcts + mapping.awardedEcts) * 10) / 10
      } else {
        toState.mappings.push(mapping)
      }
    } else {
      mapping.awardedEcts = Math.round((mapping.awardedEcts - moveAmount) * 10) / 10
      if (existing) {
        existing.awardedEcts = Math.round((existing.awardedEcts + moveAmount) * 10) / 10
      } else {
        toState.mappings.push({ ...mapping, localId: crypto.randomUUID(), awardedEcts: moveAmount })
      }
    }
    isDirty.value = true
  }

  function stagedStorageKey(exchangeId: string) {
    return `loom.stagedPartnerCourses.${exchangeId}`
  }

  function saveStagedPartnerCourses() {
    if (!exchange.value) return
    localStorage.setItem(
      stagedStorageKey(exchange.value.guid),
      JSON.stringify([...stagedPartnerCourseIds.value]),
    )
  }

  function loadStagedPartnerCourses(exchangeId: string): Set<string> {
    try {
      const raw = localStorage.getItem(stagedStorageKey(exchangeId))
      return raw ? new Set(JSON.parse(raw)) : new Set()
    } catch {
      return new Set()
    }
  }

  function stagePartnerCourse(id: string) {
    stagedPartnerCourseIds.value = new Set([...stagedPartnerCourseIds.value, id])
    saveStagedPartnerCourses()
  }

  function unstagePartnerCourse(id: string) {
    const next = new Set(stagedPartnerCourseIds.value)
    next.delete(id)
    stagedPartnerCourseIds.value = next
    saveStagedPartnerCourses()
  }

  function localSetSlotMode(homeSlotId: string, mode: SlotMode) {
    const existing = localSlotStates.value.find((s) => s.homeSlotId === homeSlotId)
    if (existing) {
      existing.mode = mode
      if (mode !== slotMode.AtExchange) existing.mappings = []
    } else {
      localSlotStates.value.push({ homeSlotId, mode, mappings: [] })
    }
    isDirty.value = true
  }

  function localRemoveSlotState(homeSlotId: string) {
    localSlotStates.value = localSlotStates.value.filter((s) => s.homeSlotId !== homeSlotId)
    isDirty.value = true
  }

  function localAddSlotMapping(homeSlotId: string, mapping: LocalSlotMapping) {
    const state = localSlotStates.value.find((s) => s.homeSlotId === homeSlotId)
    if (state) {
      // Merge into an existing mapping for the same partner course instead of duplicating it.
      const existing = state.mappings.find((m) => m.partnerCourseCode === mapping.partnerCourseCode)
      if (existing) {
        existing.awardedEcts = Math.round((existing.awardedEcts + mapping.awardedEcts) * 10) / 10
      } else {
        state.mappings.push(mapping)
      }
      isDirty.value = true
    }
  }

  function localRemoveSlotMapping(homeSlotId: string, localId: string) {
    const state = localSlotStates.value.find((s) => s.homeSlotId === homeSlotId)
    if (state) {
      state.mappings = state.mappings.filter((m) => m.localId !== localId)
      isDirty.value = true
    }
  }

  function localRemoveAllMappingsForCourse(partnerCourseId: string) {
    for (const state of localSlotStates.value) {
      state.mappings = state.mappings.filter((m) => m.partnerCourseId !== partnerCourseId)
    }
    isDirty.value = true
  }

  function localUpdateMappingEcts(homeSlotId: string, localId: string, newEcts: number) {
    const state = localSlotStates.value.find((s) => s.homeSlotId === homeSlotId)
    if (state) {
      const idx = state.mappings.findIndex((m) => m.localId === localId)
      if (idx !== -1) {
        state.mappings.splice(idx, 1, { ...state.mappings[idx]!, awardedEcts: newEcts })
        isDirty.value = true
      }
    }
  }

  // Fetch actions

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

  async function deleteExchange(exchangeGuid: string) {
    await exchangeService.deleteExchange(exchangeGuid)
    summaries.value = summaries.value.filter((s) => s.guid !== exchangeGuid)
  }

  async function fetchExchange(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = guestMode.value
        ? await exchangeService.getPublic(exchangeId)
        : await exchangeService.getById(exchangeId)
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
      return null
    } finally {
      loading.value = false
    }
  }

  async function fetchLearningAgreement(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await learningAgreementService.get(exchangeId, guestMode.value)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      stagedPartnerCourseIds.value = loadStagedPartnerCourses(exchangeId)
      isDirty.value = false
    } catch {
      error.value = t('common.error')
    } finally {
      loading.value = false
    }
  }

  // Batch save

  async function saveLearningAgreement(exchangeId: string) {
    try {
      const entries: LearningAgreementEntryUpsertDto[] = []
      for (const s of localSlotStates.value) {
        if (s.mode !== slotMode.AtExchange || s.mappings.length === 0) {
          entries.push({
            homeSlotId: s.homeSlotId,
            mode: s.mode,
            partnerCourseId: null,
            awardedEcts: null,
          })
        } else {
          for (const m of s.mappings) {
            entries.push({
              homeSlotId: s.homeSlotId,
              mode: s.mode,
              partnerCourseId: m.partnerCourseId,
              awardedEcts: m.awardedEcts,
            })
          }
        }
      }
      const res = await learningAgreementService.save(exchangeId, { entries }, guestMode.value)
      serverLearningAgreement.value = res.data
      localSlotStates.value = buildLocalFromServer(res.data)
      isDirty.value = false
      await fetchRecognition(exchangeId)
    } catch (e: unknown) {
      const { title, message } = extractApiError(e)
      const match = message?.match(/^(Awarded ECTS for course )(\S+)( exceeds available .+)$/)
      if (match) {
        const mapping = localSlotStates.value
          .flatMap((s) => s.mappings)
          .find((m) => String(m.partnerCourseId) === match[2])
        const label = mapping ? `${mapping.partnerCourseCode} (${mapping.partnerCourseName})` : match[2]
        useNotification().notifyError(title, `${match[1]}${label}${match[3]}`)
      } else {
        useNotification().notifyError(title, message)
      }
      throw e
    }
  }

  // Status & coordinator

  async function updateLearningAgreementStatus(
    exchangeId: string,
    request: UpdateLearningAgreementStatusRequest,
  ) {
    const res = await learningAgreementService.updateStatus(exchangeId, request, guestMode.value)
    exchange.value = res.data
    await fetchLearningAgreement(exchangeId)
  }

  async function updateCoordinatorMessage(
    exchangeId: string,
    request: UpdateCoordinatorMessageRequest,
  ) {
    const res = await exchangeService.updateCoordinatorMessage(exchangeId, request)
    exchange.value = res.data
  }

  async function updateExchange(exchangeId: string, request: UpdateExchangeRequest) {
    const res = await exchangeService.update(exchangeId, request, guestMode.value)
    exchange.value = res.data
    return res.data
  }

  async function updateLaMessage(exchangeId: string, message: string | null) {
    const res = await learningAgreementService.updateMessage(exchangeId, message, guestMode.value)
    serverLearningAgreement.value = res.data
  }

  async function updateRecognitionMessage(exchangeId: string, message: string | null) {
    const res = await recognitionService.updateMessage(exchangeId, message, guestMode.value)
    serverRecognition.value = res.data
  }

  // Recognition

  async function fetchRecognition(exchangeId: string) {
    const [rec, ms] = await Promise.all([
      recognitionService.getOrCreate(exchangeId, guestMode.value),
      mappingSchemeService.get(exchangeId, guestMode.value),
    ])
    serverRecognition.value = rec.data
    serverMappingScheme.value = ms.data
  }

  async function saveRecognition(exchangeId: string, request: SaveRecognitionRequest) {
    const res = await recognitionService.saveRecognition(exchangeId, request, guestMode.value)
    serverRecognition.value = res.data
    const ms = await mappingSchemeService.get(exchangeId, guestMode.value)
    serverMappingScheme.value = ms.data
  }

  async function fetchMappingScheme(exchangeId: string) {
    const res = await mappingSchemeService.get(exchangeId, guestMode.value)
    serverMappingScheme.value = res.data
  }

  async function saveMappingScheme(exchangeId: string, request: SaveMappingSchemeRequest) {
    const res = await mappingSchemeService.save(exchangeId, request, guestMode.value)
    serverMappingScheme.value = res.data
    const rec = await recognitionService.getOrCreate(exchangeId, guestMode.value)
    serverRecognition.value = rec.data
  }

  async function updateRecognitionStatus(
    exchangeId: string,
    request: UpdateRecognitionStatusRequest,
  ) {
    const res = await recognitionService.updateRecognitionStatus(exchangeId, request, guestMode.value)
    serverRecognition.value = res.data
  }

  async function exportMappings(exchangeId: string): Promise<void> {
    const res = await learningAgreementService.exportMappings(exchangeId, guestMode.value)
    const url = URL.createObjectURL(new Blob([res.data], { type: 'application/json' }))
    const a = document.createElement('a')
    a.href = url
    a.download = `la-export-${new Date().toISOString().slice(0, 10)}.json`
    a.click()
    URL.revokeObjectURL(url)
  }

  async function importMappings(exchangeId: string, dto: MappingExportDto): Promise<MappingImportResult> {
    const res = await learningAgreementService.importMappings(exchangeId, dto, guestMode.value)
    await fetchLearningAgreement(exchangeId)
    return res.data
  }

  async function fetchLaHistory(exchangeId: string): Promise<LaSnapshotSummary[]> {
    const res = await learningAgreementService.getHistory(exchangeId, guestMode.value)
    return res.data
  }

  async function fetchSnapshots(exchangeId: string): Promise<SnapshotListItem[]> {
    const res = await learningAgreementService.getSnapshots(exchangeId, guestMode.value)
    return res.data
  }

  async function restoreSnapshot(exchangeId: string, snapshotId: number): Promise<void> {
    await learningAgreementService.restoreSnapshot(exchangeId, snapshotId, guestMode.value)
    await fetchLearningAgreement(exchangeId)
  }

  async function fetchPartnerCourses(institutionId: string, force = false): Promise<void> {
    if (!force && partnerCoursesRequestId === institutionId) return
    partnerCoursesRequestId = institutionId
    partnerCoursesLoading.value = true
    try {
      const items: PartnerCourseResponse[] = []
      let page = 1
      let totalPages = 1
      do {
        const res = await institutionService.getPartnerCoursesByInstitution(institutionId, false, { page, pageSize: 200 })
        items.push(...res.data.items)
        totalPages = Math.ceil(res.data.totalCount / res.data.pageSize) || 1
        page++
      } while (page <= totalPages)
      if (partnerCoursesRequestId === institutionId) partnerCourses.value = items
    } catch {
      if (partnerCoursesRequestId === institutionId) partnerCourses.value = []
    } finally {
      if (partnerCoursesRequestId === institutionId) partnerCoursesLoading.value = false
    }
  }

  async function fetchRecognitionHistory(exchangeId: string): Promise<RecognitionSnapshotSummary[]> {
    const res = await recognitionService.getHistory(exchangeId, guestMode.value)
    return res.data
  }

  return {
    summaries,
    exchange,
    serverLearningAgreement,
    serverRecognition,
    serverMappingScheme,
    localSlotStates,
    isDirty,
    slots,
    loading,
    error,
    draggingCourse,
    draggingSlotMapping,
    stagedPartnerCourseIds,
    guestMode,
    partnerCourses,
    partnerCoursesLoading,
    fetchPartnerCourses,
    setGuestMode,
    startDrag,
    endDrag,
    startSlotDrag,
    localMoveSlotMapping,
    stagePartnerCourse,
    unstagePartnerCourse,
    localSetSlotMode,
    localRemoveSlotState,
    localAddSlotMapping,
    localRemoveSlotMapping,
    localRemoveAllMappingsForCourse,
    localUpdateMappingEcts,
    fetchMySummaries,
    fetchExchange,
    createExchange,
    deleteExchange,
    fetchLearningAgreement,
    saveLearningAgreement,
    updateLearningAgreementStatus,
    updateCoordinatorMessage,
    updateExchange,
    updateLaMessage,
    fetchRecognition,
    saveRecognition,
    fetchMappingScheme,
    saveMappingScheme,
    updateRecognitionStatus,
    updateRecognitionMessage,
    exportMappings,
    importMappings,
    fetchLaHistory,
    fetchSnapshots,
    restoreSnapshot,
    fetchRecognitionHistory,
  }
})
