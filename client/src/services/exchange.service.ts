import { api } from './api'
import type {
  CreateExchangeRequest,
  ExchangeResponse,
  ExchangeSnapshotResponse,
  ExchangeSummaryResponse,
  LearningAgreementResponse,
  SetSlotModeRequest,
  AddSlotMappingRequest,
  RemoveSlotMappingRequest,
  UpdateExchangeStatusRequest,
  UpdateCoordinatorMessageRequest,
  SaveLearningAgreementRequest,
} from '@/types/exchange.types'

export const exchangeService = {
  create: (request: CreateExchangeRequest) =>
    api.post<ExchangeResponse>('/api/exchanges', request),
  getById: (exchangeId: string) =>
    api.get<ExchangeResponse>(`/api/exchanges/${exchangeId}`),
  getMine: () =>
    api.get<ExchangeSummaryResponse[]>('/api/exchanges/mine'),
  deleteExchange: (exchangeId: string) =>
    api.delete(`/api/exchanges/${exchangeId}`),
  updateStatus: (exchangeId: string, request: UpdateExchangeStatusRequest) =>
    api.patch<ExchangeResponse>(`/api/exchanges/${exchangeId}/status`, request),
  getLearningAgreement: (exchangeId: string) =>
    api.get<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`),
  setSlotMode: (exchangeId: string, request: SetSlotModeRequest) =>
    api.post<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement/slot-mode`, request),
  addSlotMapping: (exchangeId: string, request: AddSlotMappingRequest) =>
    api.post<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement/mappings`, request),
  removeSlotMapping: (exchangeId: string, request: RemoveSlotMappingRequest) =>
    api.delete<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement/mappings`, { data: request }),
  removeSlotState: (exchangeId: string, courseSlotId: string) =>
    api.delete<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement/slot-state`, { data: { courseSlotId } }),
  updateCoordinatorMessage: (exchangeId: string, request: UpdateCoordinatorMessageRequest) =>
    api.put<ExchangeResponse>(`/api/exchanges/${exchangeId}/coordinator-message`, request),
  getMyStudents: () =>
    api.get<ExchangeSummaryResponse[]>('/api/exchanges/coordinator/students'),
  saveLearningAgreement: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`, request),
  getSnapshots: (exchangeId: string) =>
    api.get<ExchangeSnapshotResponse[]>(`/api/exchanges/${exchangeId}/snapshots`),
  getSnapshot: (exchangeId: string, snapshotId: string) =>
    api.get<ExchangeSnapshotResponse>(`/api/exchanges/${exchangeId}/snapshots/${snapshotId}`),
}
