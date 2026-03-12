export type CourseType =
  | 'Mandatory'
  | 'CoreElective'
  | 'ProfileElective'
  | 'FreeElective'
  | 'Transversal'
  | 'Seminar'
  | 'ResearchSeminar'
  | 'Project'
  | 'Thesis'

export type CourseStatus = 'Active' | 'Inactive' | 'Historical'

export interface CourseDto {
  id: string
  code?: string
  name: string
  nameEn?: string
  ects: number
  type: CourseType
  status: CourseStatus
  lecturesHours?: number
  auditoryHours?: number
  labHours?: number
}
