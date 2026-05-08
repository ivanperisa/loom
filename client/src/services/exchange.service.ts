import { api } from './api'
import type {
  CreateExchangeRequest,
  ExchangeResponse,
  ExchangeSummaryResponse,
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
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
  updateLearningAgreementStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest) =>
    api.patch<ExchangeResponse>(`/api/exchanges/${exchangeId}/learning-agreement/status`, request),
  getLearningAgreement: (exchangeId: string) =>
    api.get<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`),
  saveLearningAgreement: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`, request),
  updateCoordinatorMessage: (exchangeId: string, request: UpdateCoordinatorMessageRequest) =>
    api.put<ExchangeResponse>(`/api/exchanges/${exchangeId}/coordinator-message`, request),
  getMyStudents: () =>
    api.get<ExchangeSummaryResponse[]>('/api/exchanges/coordinator/students'),
}
