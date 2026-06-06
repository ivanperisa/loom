import { api } from './api'
import type {
  RecognitionResponse,
  SaveRecognitionRequest,
  UpdateRecognitionStatusRequest,
} from '@/types/recognition.types'

export const recognitionService = {
  getOrCreate: (exchangeId: string) =>
    api.get<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition`),
  saveRecognition: (exchangeId: string, request: SaveRecognitionRequest) =>
    api.put<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/entries`, request),
  updateRecognitionStatus: (exchangeId: string, request: UpdateRecognitionStatusRequest) =>
    api.patch<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/status`, request),
  setEntryRecognized: (exchangeId: string, entryId: string, isRecognized: boolean | null) =>
    api.patch<RecognitionResponse>(`/api/exchanges/${exchangeId}/recognition/entries/${entryId}/recognized`, { isRecognized }),
}
