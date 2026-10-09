import { defineStore } from 'pinia'
import { ref } from 'vue'
import { slotMode } from '@/utils/slotMode'
import type {
  LearningAgreementEntryUpsertDto,
  LearningAgreementResponse,
  LocalSlotMapping,
  LocalSlotState,
  SlotMode,
} from '@/types/learningAgreement.types'
import type { PartnerCourseResponse } from '@/types/institution.types'

const round1 = (n: number) => Math.round(n * 10) / 10

function slotStatesFrom(la: LearningAgreementResponse): LocalSlotState[] {
  const map = new Map<number, LocalSlotState>()
  for (const entry of la.entries) {
    if (entry.isDeleted) continue
    let state = map.get(entry.homeSlotId)
    if (!state) {
      state = { homeSlotId: entry.homeSlotId, mode: entry.mode, mappings: [] }
      map.set(entry.homeSlotId, state)
    }
    if (entry.partnerCourseId !== null) {
      state.mappings.push({
        localId: String(entry.id),
        partnerCourseId: entry.partnerCourseId,
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

function stagedKey(exchangeId: string) {
  return `loom.stagedPartnerCourses.${exchangeId}`
}

function readStaged(exchangeId: string): Set<number> {
  try {
    const raw = localStorage.getItem(stagedKey(exchangeId))
    return raw ? new Set((JSON.parse(raw) as unknown[]).map(Number).filter(Number.isFinite)) : new Set()
  } catch {
    return new Set()
  }
}

/**
 * The learning agreement being edited: client-owned state only. The saved LA comes from vue-query
 * (`useLearningAgreementQuery`); this holds the unsaved copy, drag and drop, and the courses staged
 * in the "mapped" list.
 */
export const useLaDraftStore = defineStore('laDraft', () => {
  const exchangeId = ref<string | null>(null)
  const slotStates = ref<LocalSlotState[]>([])
  const isDirty = ref(false)
  const draggingCourse = ref<PartnerCourseResponse | null>(null)
  const draggingSlotMapping = ref<{ fromSlotId: number; localId: string } | null>(null)
  const armedCourse = ref<PartnerCourseResponse | null>(null)
  const stagedPartnerCourseIds = ref<Set<number>>(new Set())

  /** Starts over from the saved LA (first load, after a save, or on discard). */
  function load(id: string, la: LearningAgreementResponse) {
    if (exchangeId.value !== id) {
      stagedPartnerCourseIds.value = readStaged(id)
      endDrag()
      armedCourse.value = null
    }
    exchangeId.value = id
    slotStates.value = slotStatesFrom(la)
    isDirty.value = false
  }

  /** Whether `load` would throw away nothing: a different exchange, or no unsaved changes. */
  function canReload(id: string) {
    return exchangeId.value !== id || !isDirty.value
  }

  function toRequest(): { entries: LearningAgreementEntryUpsertDto[] } {
    const entries: LearningAgreementEntryUpsertDto[] = []
    for (const s of slotStates.value) {
      if (s.mode !== slotMode.AtExchange || s.mappings.length === 0) {
        entries.push({ homeSlotId: s.homeSlotId, mode: s.mode, partnerCourseId: null, awardedEcts: null })
      } else {
        for (const m of s.mappings) {
          entries.push({ homeSlotId: s.homeSlotId, mode: s.mode, partnerCourseId: m.partnerCourseId, awardedEcts: m.awardedEcts })
        }
      }
    }
    return { entries }
  }

  const stateFor = (homeSlotId: number) => slotStates.value.find((s) => s.homeSlotId === homeSlotId)

  // Drag and drop

  function startDrag(course: PartnerCourseResponse) {
    draggingCourse.value = course
    draggingSlotMapping.value = null
    armedCourse.value = null
  }

  function startSlotDrag(fromSlotId: number, localId: string) {
    draggingSlotMapping.value = { fromSlotId, localId }
    draggingCourse.value = null
    armedCourse.value = null
  }

  function endDrag() {
    draggingCourse.value = null
    draggingSlotMapping.value = null
  }

  /** Click-to-place: arm a course, then click a slot. Clicking the armed course again disarms it. */
  function armCourse(course: PartnerCourseResponse) {
    armedCourse.value = armedCourse.value?.id === course.id ? null : course
    endDrag()
  }

  function disarm() {
    armedCourse.value = null
  }

  // Staged courses ("mapped" list without ECTS yet), remembered per exchange in this browser

  function saveStaged() {
    if (exchangeId.value) localStorage.setItem(stagedKey(exchangeId.value), JSON.stringify([...stagedPartnerCourseIds.value]))
  }

  function stagePartnerCourse(id: number) {
    stagedPartnerCourseIds.value = new Set([...stagedPartnerCourseIds.value, id])
    saveStaged()
  }

  function unstagePartnerCourse(id: number) {
    const next = new Set(stagedPartnerCourseIds.value)
    next.delete(id)
    stagedPartnerCourseIds.value = next
    saveStaged()
  }

  // Edits

  function setSlotMode(homeSlotId: number, mode: SlotMode) {
    const existing = stateFor(homeSlotId)
    if (existing) {
      existing.mode = mode
      if (mode !== slotMode.AtExchange) existing.mappings = []
    } else {
      slotStates.value.push({ homeSlotId, mode, mappings: [] })
    }
    isDirty.value = true
  }

  function removeSlotState(homeSlotId: number) {
    slotStates.value = slotStates.value.filter((s) => s.homeSlotId !== homeSlotId)
    isDirty.value = true
  }

  function addMapping(homeSlotId: number, mapping: LocalSlotMapping) {
    const state = stateFor(homeSlotId)
    if (!state) return
    // Merge into an existing mapping for the same partner course instead of duplicating it.
    const existing = state.mappings.find((m) => m.partnerCourseId === mapping.partnerCourseId)
    if (existing) existing.awardedEcts = round1(existing.awardedEcts + mapping.awardedEcts)
    else state.mappings.push(mapping)
    isDirty.value = true
  }

  function removeMapping(homeSlotId: number, localId: string) {
    const state = stateFor(homeSlotId)
    if (!state) return
    state.mappings = state.mappings.filter((m) => m.localId !== localId)
    isDirty.value = true
  }

  function removeAllMappingsForCourse(partnerCourseId: number) {
    for (const state of slotStates.value) {
      state.mappings = state.mappings.filter((m) => m.partnerCourseId !== partnerCourseId)
    }
    isDirty.value = true
  }

  function updateMappingEcts(homeSlotId: number, localId: string, ects: number) {
    const mapping = stateFor(homeSlotId)?.mappings.find((m) => m.localId === localId)
    if (!mapping) return
    mapping.awardedEcts = ects
    isDirty.value = true
  }

  /** Moves all or part (`amount` ECTS) of a mapping to another slot. */
  function moveMapping(fromSlotId: number, toSlotId: number, localId: string, amount?: number) {
    const from = stateFor(fromSlotId)
    const index = from?.mappings.findIndex((m) => m.localId === localId) ?? -1
    if (!from || index === -1) return
    const mapping = from.mappings[index]!
    const moved = amount === undefined ? mapping.awardedEcts : Math.min(amount, mapping.awardedEcts)
    if (moved <= 0) return

    let to = stateFor(toSlotId)
    if (!to) {
      to = { homeSlotId: toSlotId, mode: slotMode.AtExchange, mappings: [] }
      slotStates.value.push(to)
    } else {
      to.mode = slotMode.AtExchange
    }

    const existing = to.mappings.find((m) => m.partnerCourseId === mapping.partnerCourseId)
    if (moved >= mapping.awardedEcts) {
      from.mappings.splice(index, 1)
      if (existing) existing.awardedEcts = round1(existing.awardedEcts + mapping.awardedEcts)
      else to.mappings.push(mapping)
    } else {
      mapping.awardedEcts = round1(mapping.awardedEcts - moved)
      if (existing) existing.awardedEcts = round1(existing.awardedEcts + moved)
      else to.mappings.push({ ...mapping, localId: crypto.randomUUID(), awardedEcts: moved })
    }
    isDirty.value = true
  }

  return {
    exchangeId,
    slotStates,
    isDirty,
    draggingCourse,
    draggingSlotMapping,
    armedCourse,
    stagedPartnerCourseIds,
    load,
    canReload,
    toRequest,
    startDrag,
    startSlotDrag,
    endDrag,
    armCourse,
    disarm,
    stagePartnerCourse,
    unstagePartnerCourse,
    setSlotMode,
    removeSlotState,
    addMapping,
    removeMapping,
    removeAllMappingsForCourse,
    updateMappingEcts,
    moveMapping,
  }
})
