import type { HomeSlotResponse } from '@/types/learningAgreement.types'

export function slotDisplayCode(slot: HomeSlotResponse): string | number | null {
  return slot.courseIsvuCode ?? slot.courseGroupIsvuCode ?? null
}

export function slotDisplayName(slot: HomeSlotResponse, locale: string): string {
  if (locale === 'en') return slot.courseNameEn ?? slot.courseGroupNameEn ?? slot.courseTypeName
  return slot.courseName ?? slot.courseGroupName ?? slot.courseTypeName
}

export function slotCodeLabel(slot: HomeSlotResponse): string {
  const code = slotDisplayCode(slot)
  return code !== null ? String(code) : slot.courseTypeName
}
