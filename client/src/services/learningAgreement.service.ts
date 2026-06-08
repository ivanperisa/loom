import { api } from './api'
import type { ExchangeResponse } from '@/types/exchange.types'
import type {
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
  SaveLearningAgreementRequest,
} from '@/types/learningAgreement.types'

function basePath(exchangeId: string, guest: boolean) {
  return guest ? `/api/exchanges/access/${exchangeId}` : `/api/exchanges/${exchangeId}`
}

export const learningAgreementService = {
  get: (exchangeId: string, guest = false) =>
    api.get<LearningAgreementResponse>(`${basePath(exchangeId, guest)}/learning-agreement`),
  save: (exchangeId: string, request: SaveLearningAgreementRequest, guest = false) =>
    api.put<LearningAgreementResponse>(`${basePath(exchangeId, guest)}/learning-agreement`, request),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest, guest = false) =>
    api.patch<ExchangeResponse>(`${basePath(exchangeId, guest)}/learning-agreement/status`, request),
}
