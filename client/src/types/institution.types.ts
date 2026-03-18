export interface InstitutionResponse {
  id: string
  name: string
  nameEn: string | null
  country: string | null
  city: string | null
  erasmusCode: string | null
  isHome: boolean
}

export interface StudyProfileResponse {
  id: string
  name: string
  nameEn: string | null
}

export interface StudyProgramResponse {
  id: string
  name: string
  nameEn: string | null
  level: string
  durationSemesters: number
  profiles: StudyProfileResponse[]
}

export interface ForeignProgramResponse {
  id: string
  name: string
  nameEn: string | null
  institutionId: string
  institutionName: string
}

export interface ForeignCourseResponse {
  id: string
  code: string
  nameEn: string
  nameHr: string | null
  ects: number
  lecturesH: number | null
  auditoryH: number | null
  labH: number | null
}
