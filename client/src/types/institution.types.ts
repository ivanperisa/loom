export interface InstitutionResponse {
  id: string
  name: string
  nameHr: string | null
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

export interface PartnerInstitutionAdminResponse {
  id: string
  name: string
  nameHr: string | null
  country: string
  city: string | null
  erasmusCode: string | null
  courseCount: number
  isDeleted: boolean
}

export interface PartnerCourseResponse {
  id: string
  code: string
  name: string
  nameHr: string | null
  ects: number
  lecturesH: number | null
  auditoryH: number | null
  labH: number | null
  semester: string
  level: string
  isDeleted: boolean
}
