import type { DocumentStatus } from './exchange.types'
import type { RecognitionEntryFields } from './recognitionEntryFields.types'

/** Table 1: one agreed course placement from the latest approved LA version (read-only, no grades). */
export interface AgreedEntryResponse extends Omit<RecognitionEntryFields, 'enrollmentStatus' | 'originalGrade' | 'ectsGrade' | 'hrGrade' | 'examDate'> {
  homeSlotId: string
  partnerCourseId: string
}

export interface RecognitionResponse {
  exchangeId: string
  status: DocumentStatus
  message: string | null
  /** "Start final recognition" was pressed: the results (table 2 + mapping scheme) exist. */
  isStarted: boolean
  startedAt: string | null
  startedByName: string | null
  /** Start is possible now: the LA is approved and final recognition has not started. */
  canStart: boolean
  /** The LA version table 1 shows (null before the first approval). */
  agreedVersionNo: number | null
  agreed: AgreedEntryResponse[]
  lastModifiedAt: string | null
  lastModifiedByName: string | null
  signedAt: string | null
  signedByName: string | null
  approvedVersionCount: number
}

/** Table 2: grades belong to a partner course (all slots it is placed in). */
export interface CourseGradesRequest {
  partnerCourseId: number
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}

export interface SaveGradesRequest {
  entries: CourseGradesRequest[]
}

export interface UpdateRecognitionStatusRequest {
  status: DocumentStatus
}
