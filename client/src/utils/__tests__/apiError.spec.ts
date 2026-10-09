import { describe, expect, it } from 'vitest'
import { AxiosError, AxiosHeaders, type AxiosResponse } from 'axios'
import { i18n } from '@/i18n'
import { describeApiError, describeCode, isApiError, toApiError } from '@/utils/apiError'

function apiError(status: number, data: unknown): AxiosError {
  const response = { status, data, headers: {}, config: { headers: new AxiosHeaders() }, statusText: '' } as AxiosResponse
  return new AxiosError('failed', 'ERR_BAD_RESPONSE', undefined, undefined, response)
}

describe('API errors', () => {
  i18n.global.locale.value = 'en'

  it('reads the code and params ProblemDetails carry at the top level', () => {
    const error = apiError(400, { title: 'Validation Error', code: 'ECTS_EXCEEDED', params: { courseId: 7, available: 5 } })
    expect(toApiError(error)).toEqual({ status: 400, code: 'ECTS_EXCEEDED', params: { courseId: 7, available: 5 }, detail: null })
    expect(isApiError(error, 'ECTS_EXCEEDED')).toBe(true)
  })

  it('words the message in the current language, naming the course', () => {
    const error = apiError(400, { code: 'ECTS_EXCEEDED', params: { courseId: 7, available: 5 } })
    const { title, message } = describeApiError(error, { course: (id) => (id === 7 ? 'IN2003 (Algorithms)' : undefined) })
    expect(title).toBe('Check your input')
    expect(message).toBe('The ECTS awarded for IN2003 (Algorithms) exceed the 5 available.')

    i18n.global.locale.value = 'hr'
    expect(describeApiError(error).message).toBe('Dodijeljeni ECTS za predmet 7 premašuju dostupnih 5.')
    i18n.global.locale.value = 'en'
  })

  it('falls back to the server text for a code without a translation', () => {
    expect(describeCode('SOMETHING_NEW', {}, 'Server says no.')).toBe('Server says no.')
  })

  it('explains a request that got no answer', () => {
    const offline = new AxiosError('Network Error', 'ERR_NETWORK')
    expect(describeApiError(offline).title).toBe('No connection')
  })
})
