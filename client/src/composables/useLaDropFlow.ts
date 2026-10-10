import { computed, ref } from 'vue'
import { useLaDraftStore } from '@/stores/laDraft.store'
import { slotMode } from '@/utils/slotMode'
import type { HomeSlotResponse } from '@/types/learningAgreement.types'
import type { PartnerCourseResponse } from '@/types/institution.types'

const round1 = (n: number) => Math.round(n * 10) / 10
const MIN_ECTS = 0.5

/**
 * Placing courses on the LA grid: a course dropped (or clicked while armed) on a slot, or a placed course
 * dragged to another slot, first asks how many ECTS. The dialogs read `pendingDrop` / `pendingMove`.
 */
export function useLaDropFlow() {
  const draft = useLaDraftStore()

  const pendingDrop = ref<{ slot: HomeSlotResponse; course: PartnerCourseResponse } | null>(null)
  const pendingEcts = ref(0)
  const pendingMove = ref<{
    fromSlotId: number
    toSlotId: number
    localId: string
    max: number
    courseCode: string
    courseName: string
  } | null>(null)
  const moveEcts = ref(0)

  /** ECTS of a course already placed anywhere in the draft. */
  function placedEcts(courseId: number): number {
    let sum = 0
    for (const state of draft.slotStates) {
      for (const m of state.mappings) if (m.partnerCourseId === courseId) sum += m.awardedEcts
    }
    return sum
  }

  /** What is left of the pending course to place. */
  const remainingEcts = computed(() => {
    const course = pendingDrop.value?.course
    return course ? round1(course.ects - placedEcts(course.id)) : 0
  })

  function place(slot: HomeSlotResponse, course: PartnerCourseResponse) {
    pendingDrop.value = { slot, course }
    pendingEcts.value = remainingEcts.value
  }

  /** A drag ended on `slot`: a course from the list, or a placed course from another slot. */
  function drop(slot: HomeSlotResponse) {
    const fromSlot = draft.draggingSlotMapping
    const course = draft.draggingCourse
    draft.endDrag()
    if (fromSlot) {
      if (fromSlot.fromSlotId === slot.id) return
      const mapping = draft.slotStates
        .find((s) => s.homeSlotId === fromSlot.fromSlotId)
        ?.mappings.find((m) => m.localId === fromSlot.localId)
      if (!mapping) return
      pendingMove.value = {
        fromSlotId: fromSlot.fromSlotId,
        toSlotId: slot.id,
        localId: fromSlot.localId,
        max: mapping.awardedEcts,
        courseCode: mapping.partnerCourseCode,
        courseName: mapping.partnerCourseName,
      }
      moveEcts.value = mapping.awardedEcts
    } else if (course) {
      place(slot, course)
    }
  }

  function confirmDrop() {
    const pending = pendingDrop.value
    if (!pending || pendingEcts.value > remainingEcts.value) return
    const { slot, course } = pending
    if (draft.slotStates.find((s) => s.homeSlotId === slot.id)?.mode !== slotMode.AtExchange) {
      draft.setSlotMode(slot.id, slotMode.AtExchange)
    }
    draft.addMapping(slot.id, {
      localId: crypto.randomUUID(),
      partnerCourseId: course.id,
      partnerCourseCode: course.code,
      partnerCourseName: course.name,
      partnerCourseNameHr: course.nameHr ?? null,
      partnerCourseUrl: course.url ?? null,
      awardedEcts: Math.max(pendingEcts.value, MIN_ECTS),
    })
    pendingDrop.value = null
  }

  function confirmMove() {
    const pending = pendingMove.value
    if (!pending || moveEcts.value > pending.max) return
    draft.moveMapping(pending.fromSlotId, pending.toSlotId, pending.localId, Math.max(moveEcts.value, MIN_ECTS))
    pendingMove.value = null
  }

  return {
    pendingDrop,
    pendingEcts,
    remainingEcts,
    pendingMove,
    moveEcts,
    place,
    drop,
    confirmDrop,
    cancelDrop: () => (pendingDrop.value = null),
    confirmMove,
    cancelMove: () => (pendingMove.value = null),
  }
}
