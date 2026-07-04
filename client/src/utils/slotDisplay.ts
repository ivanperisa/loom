import type { HomeSlotResponse } from '@/types/learningAgreement.types'

export function slotDisplayCode(slot: HomeSlotResponse): string | number | null {
  return slot.courseIsvuCode ?? slot.courseGroupIsvuCode ?? null
}

export function slotDisplayName(slot: HomeSlotResponse): string {
  return slot.courseName ?? slot.courseGroupName ?? slot.courseTypeName
}

export function slotSubLabel(slot: HomeSlotResponse, locale: string): string {
  if (slotDisplayCode(slot) !== null) {
    return locale === 'en'
      ? (slot.courseNameEn ?? slot.courseGroupNameEn ?? slot.courseTypeName)
      : slotDisplayName(slot)
  }
  return slot.courseTypeName
}
