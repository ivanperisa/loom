import { api } from './api'
import { exchangeBasePath as basePath } from './exchangeBasePath'
import type {
  RecognitionResponse,
  SaveRecognitionRequest,
  UpdateRecognitionStatusRequest,
  RecognitionSnapshotSummary,
} from '@/types/recognition.types'

export const recognitionService = {
  getOrCreate: (exchangeId: string, guest = false) =>
    api.get<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition`),
  saveRecognition: (exchangeId: string, request: SaveRecognitionRequest, guest = false) =>
    api.put<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/entries`, request),
  updateRecognitionStatus: (exchangeId: string, request: UpdateRecognitionStatusRequest, guest = false) =>
    api.patch<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/status`, request),
  updateMessage: (exchangeId: string, message: string | null, guest = false) =>
    api.patch<RecognitionResponse>(`${basePath(exchangeId, guest)}/recognition/message`, { message }),
  getHistory: (exchangeId: string, guest = false) =>
    api.get<RecognitionSnapshotSummary[]>(`${basePath(exchangeId, guest)}/recognition/history`),
}
