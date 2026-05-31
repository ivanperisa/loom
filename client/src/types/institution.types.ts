export interface InstitutionResponse {
  id: string
  name: string
  nameEn: string | null
  country: string | null
  city: string | null
  erasmusCode: string | null
}

export interface HomeProfileResponse {
  id: string
  name: string
  nameEn: string | null
}

export interface HomeProgramResponse {
  id: string
  name: string
  nameEn: string | null
  level: string
  durationSemesters: number
  profiles: HomeProfileResponse[]
}

export interface PartnerProgramResponse {
  id: string
  name: string
  nameEn: string | null
  level: string
  institutionName: string
  institutionCountry: string | null
  institutionCity: string | null
}

export interface PartnerCourseResponse {
  id: string
  code: string
  nameEn: string
  nameHr: string | null
  ects: number
  lecturesH: number | null
  auditoryH: number | null
  labH: number | null
}
