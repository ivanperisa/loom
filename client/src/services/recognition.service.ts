import { api } from './api'
import type {
  RecognitionResponse,
  SaveRecognitionRequest,
  UpdateRecognitionStatusRequest,
  RecognitionSnapshotSummary,
} from '@/types/recognition.types'

export const recognitionService = {
  getOrCreate: (exchangeId: string) =>
    api.get<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition`),
  saveRecognition: (exchangeId: string, request: SaveRecognitionRequest) =>
    api.put<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/entries`, request),
  updateRecognitionStatus: (exchangeId: string, request: UpdateRecognitionStatusRequest) =>
    api.patch<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/status`, request),
  updateMessage: (exchangeId: string, message: string | null) =>
    api.patch<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/message`, { message }),
  getHistory: (exchangeId: string) =>
    api.get<RecognitionSnapshotSummary[]>(`/api/exchanges/${exchangeId}/recognition/history`),
}
