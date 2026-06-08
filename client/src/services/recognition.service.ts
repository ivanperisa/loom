import { api } from './api'
import type {
  RecognitionResponse,
  SaveRecognitionRequest,
  UpdateRecognitionStatusRequest,
} from '@/types/recognition.types'

function basePath(exchangeId: string, guest: boolean) {
  return guest ? `/api/exchanges/access/${exchangeId}` : `/api/exchanges/${exchangeId}`
}

export const recognitionService = {
  getOrCreate: (exchangeId: string, guest = false) =>
    api.get<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition`),
  saveRecognition: (exchangeId: string, request: SaveRecognitionRequest, guest = false) =>
    api.put<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/entries`, request),
  updateRecognitionStatus: (exchangeId: string, request: UpdateRecognitionStatusRequest, guest = false) =>
    api.patch<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/status`, request),
  setEntryRecognized: (exchangeId: string, entryId: string, isRecognized: boolean | null, guest = false) =>
    api.patch<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/entries/${entryId}/recognized`, { isRecognized }),
}
