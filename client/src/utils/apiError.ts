import axios from 'axios'
import { i18n } from '@/i18n'

/** The API's ProblemDetails body. Extensions (`code`, `params`, `errors`) sit at the top level. */
interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  code?: string
  traceId?: string
  params?: Record<string, unknown>
  errors?: { code: string; description: string }[] | Record<string, string[]>
}

export interface ApiError {
  /** HTTP status, or null when the request never got an answer (offline, CORS, timeout). */
  status: number | null
  /** The API error code (e.g. `LA_LOCKED`), or null when the server sent none. */
  code: string | null
  params: Record<string, unknown>
  /** The server's own (English) description, used only when the code has no translation. */
  detail: string | null
  /** The request's id in the server logs (the trace part of the W3C trace id). */
  reference: string | null
}

/** "00-<trace>-<span>-00" → "<trace>": the part that is the same in every server log line of the request. */
function referenceOf(traceId: string | undefined): string | null {
  if (!traceId) return null
  return traceId.split('-')[1] ?? traceId
}

export function toApiError(error: unknown): ApiError {
  if (!axios.isAxiosError(error)) return { status: null, code: null, params: {}, detail: null, reference: null }
  const data = error.response?.data as ProblemDetails | undefined
  const body = data && typeof data === 'object' && !(data instanceof Blob) ? data : undefined
  return {
    status: error.response?.status ?? null,
    code: body?.code ?? null,
    params: body?.params ?? {},
    detail: body?.detail ?? null,
    reference: referenceOf(body?.traceId),
  }
}

export interface DescribeOptions {
  /** Turns a partner course id from the error into something readable (code and name). */
  course?: (id: number) => string | undefined
  /** Turns a home slot id from the error into its label. */
  slot?: (id: number) => string | undefined
}

/** The user-facing message for an error code (+ params), in the current language. */
export function describeCode(code: string | null, params: Record<string, unknown> = {}, detail: string | null = null, options: DescribeOptions = {}): string {
  const t = i18n.global.t
  const key = `apiErrors.codes.${code}`
  if (code && i18n.global.te(key)) {
    const courseId = Number(params.courseId)
    const slotId = Number(params.slotId)
    return t(key, {
      ...params,
      course: options.course?.(courseId) ?? t('apiErrors.course', { id: params.courseId }),
      slot: options.slot?.(slotId) ?? t('apiErrors.slot', { id: params.slotId }),
    })
  }
  return detail ?? t('apiErrors.unknown')
}

/**
 * Title + message for a toast or an alert. For server faults (5xx) `reference` is the id to quote when reporting
 * the problem: it finds the request in the server logs.
 */
export function describeApiError(error: unknown, options: DescribeOptions = {}): { title: string; message: string; reference: string | null } {
  const t = i18n.global.t
  const { status, code, params, detail, reference } = toApiError(error)
  if (status === null && axios.isAxiosError(error)) {
    return { title: t('apiErrors.title.network'), message: t('apiErrors.network'), reference: null }
  }
  const title =
    status === 400 ? t('apiErrors.title.validation')
    : status === 403 ? t('apiErrors.title.forbidden')
    : status === 404 ? t('apiErrors.title.notFound')
    : status === 409 ? t('apiErrors.title.conflict')
    : status !== null && status >= 500 ? t('apiErrors.title.server')
    : t('apiErrors.title.unknown')
  return { title, message: describeCode(code, params, detail, options), reference: status !== null && status >= 500 ? reference : null }
}

export function isApiError(error: unknown, code: string): boolean {
  return toApiError(error).code === code
}
