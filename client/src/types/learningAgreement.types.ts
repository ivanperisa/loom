import type { slotMode } from '@/utils/slotMode'
import type { DocumentStatus } from './exchange.types'

export type SlotMode = (typeof slotMode)[keyof typeof slotMode]

export interface HomeSlotResponse {
  id: string
  semester: number
  slotPosition: number
  ects: number
  courseTypeId: string
  courseTypeName: string
  courseTypeNameEn: string
  color: string
  courseIsvuCode: number | null
  courseName: string | null
  courseNameEn: string | null
  courseGroupIsvuCode: number | null
  courseGroupName: string | null
  courseGroupNameEn: string | null
}

export interface LearningAgreementEntryResponse {
  id: string
  homeSlotId: string
  mode: SlotMode
  partnerCourseId: string | null
  partnerCourseCode: string | null
  partnerCourseNameEn: string | null
  partnerCourseNameHr: string | null
  awardedEcts: number | null
  isDeleted: boolean
}

export interface LearningAgreementResponse {
  exchangeId: string
  status: DocumentStatus
  slots: HomeSlotResponse[]
  entries: LearningAgreementEntryResponse[]
}

export interface UpdateLearningAgreementStatusRequest {
  status: DocumentStatus
  message?: string | null
}

export interface SaveLearningAgreementRequest {
  entries: LearningAgreementEntryUpsertDto[]
}

export interface LearningAgreementEntryUpsertDto {
  homeSlotId: string
  mode: SlotMode
  partnerCourseId?: string | null
  awardedEcts?: number | null
}

export interface LocalSlotState {
  homeSlotId: string
  mode: SlotMode
  mappings: LocalSlotMapping[]
}

export interface LocalSlotMapping {
  localId: string
  partnerCourseId: string
  partnerCourseCode: string
  partnerCourseNameEn: string
  partnerCourseNameHr: string | null
  awardedEcts: number
}
