import type { DocumentStatus } from './exchange.types'
import type { RecognitionEntryFields } from './recognitionEntryFields.types'

export interface RecognitionEntryResponse extends RecognitionEntryFields {
  learningAgreementEntryId: string
}

export interface RecognitionResponse {
  id: string
  exchangeId: string
  status: DocumentStatus
  message: string | null
  entries: RecognitionEntryResponse[]
  createdAt: string
  updatedAt: string
  lastModifiedAt: string | null
  lastModifiedByName: string | null
  signedAt: string | null
  signedByName: string | null
}

export interface UpsertRecognitionEntryRequest {
  learningAgreementEntryId: string
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
}

export interface SaveRecognitionRequest {
  entries: UpsertRecognitionEntryRequest[]
}

export interface UpdateRecognitionStatusRequest {
  status: DocumentStatus
}

export interface RecognitionSnapshotEntry {
  homeSlotLabel: string
  partnerCourseCode: string | null
  partnerCourseName: string | null
  enrollmentStatus: string | null
  originalGrade: string | null
  ectsGrade: string | null
  hrGrade: string | null
  examDate: string | null
  isRecognized: boolean | null
  recognizedAsCourseName: string | null
}

export interface RecognitionSnapshotEntryChange {
  before: RecognitionSnapshotEntry
  after: RecognitionSnapshotEntry
}

export interface RecognitionSnapshotDiff {
  added: RecognitionSnapshotEntry[]
  removed: RecognitionSnapshotEntry[]
  modified: RecognitionSnapshotEntryChange[]
}

export interface RecognitionSnapshotSummary {
  id: number
  approvedAt: string
  approvedByName: string
  entryCount: number
  diff: RecognitionSnapshotDiff | null
}
