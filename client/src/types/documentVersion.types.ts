export type VersionKind = 'Approved' | 'Backup'

export interface FieldChange {
  field: string
  before: string | null
  after: string | null
}

export interface DocumentChange {
  type: 'Added' | 'Removed' | 'Modified'
  homeSlotId: number
  homeSlotLabel: string
  partnerCourseId: number | null
  partnerCourseCode: string | null
  partnerCourseName: string | null
  fields: FieldChange[]
}

/** An approval (numbered, with what changed since the previous one) or a backup taken before an import/restore. */
export interface DocumentVersionResponse {
  id: number
  kind: VersionKind
  versionNo: number | null
  /** "A1", "A2", … for amendments; null for the original approval and for backups. */
  amendmentLabel: string | null
  createdAt: string
  createdByName: string | null
  entryCount: number
  changes: DocumentChange[] | null
}
