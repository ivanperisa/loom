import { api } from './api'
import type { DocumentVersionResponse } from '@/types/documentVersion.types'
import type { RecognitionResponse, SaveGradesRequest, UpdateRecognitionStatusRequest } from '@/types/recognition.types'

const base = (exchangeId: string) => `/api/exchanges/${exchangeId}/recognition`

export const recognitionService = {
  get: (exchangeId: string, signal?: AbortSignal) => api.get<RecognitionResponse>(base(exchangeId), { signal }),
  /** "Start final recognition": freezes the LA and table 1 for good. */
  start: (exchangeId: string) => api.post<RecognitionResponse>(`${base(exchangeId)}/start`),
  saveGrades: (exchangeId: string, request: SaveGradesRequest) => api.put<RecognitionResponse>(`${base(exchangeId)}/grades`, request),
  updateStatus: (exchangeId: string, request: UpdateRecognitionStatusRequest) =>
    api.patch<RecognitionResponse>(`${base(exchangeId)}/status`, request),
  updateMessage: (exchangeId: string, message: string | null) =>
    api.patch<RecognitionResponse>(`${base(exchangeId)}/message`, { message }),
  getVersions: (exchangeId: string, signal?: AbortSignal) => api.get<DocumentVersionResponse[]>(`${base(exchangeId)}/versions`, { signal }),
}
