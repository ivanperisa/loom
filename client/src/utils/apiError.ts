import type { AxiosError } from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  extensions?: {
    code?: string
    errors?: { code: string; description: string }[]
  }
}

export function extractApiError(error: unknown): { title: string; message?: string } {
  const data = (error as AxiosError)?.response?.data as ProblemDetails | undefined

  if (!data || typeof data !== 'object') {
    return { title: 'An error occurred' }
  }

  const title = data.title ?? 'An error occurred'
  const message = data.detail ?? data.extensions?.errors?.[0]?.description

  return { title, message }
}
