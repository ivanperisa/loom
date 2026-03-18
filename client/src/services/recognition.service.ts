import { api } from './api'
import type {
  RecognitionResponse,
  UpsertRecognitionEntryRequest,
} from '@/types/recognition.types'
import type { UpdateExchangeStatusRequest } from '@/types/exchange.types'

export const recognitionService = {
  getOrCreate: (exchangeId: string) =>
    api.get<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition`),
  upsertEntry: (exchangeId: string, request: UpsertRecognitionEntryRequest) =>
    api.put<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/entries`, request),
  updateStatus: (exchangeId: string, request: UpdateExchangeStatusRequest) =>
    api.patch<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/status`, request),
}
