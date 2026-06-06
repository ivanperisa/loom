import { api } from './api'
import type {
  ExchangeResponse,
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
  SaveLearningAgreementRequest,
} from '@/types/exchange.types'

export const learningAgreementService = {
  get: (exchangeId: string) =>
    api.get<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`),
  save: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`, request),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest) =>
    api.patch<ExchangeResponse>(`/api/exchanges/${exchangeId}/learning-agreement/status`, request),
}
