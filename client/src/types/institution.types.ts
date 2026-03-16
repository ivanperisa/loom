export interface InstitutionResponse {
  id: string
  name: string
  nameEn: string
  country: string
  city: string | null
  erasmusCode: string | null
}

export type StudyProgramLevel = 'Undergraduate' | 'Graduate' | 'Postgraduate'

export interface StudyProgramResponse {
  id: string
  name: string
  nameEn?: string
  level: StudyProgramLevel
  durationSemesters: number
  iscedCode?: string
}

export interface StudyProfileResponse {
  id: string
  name: string
  nameEn?: string
  exchangeSemesters?: number
  exchangeSemesterType?: string
  exchangeSpots?: number
}
