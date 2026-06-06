import { api } from './api'
import type {
  CreateExchangeRequest,
  ExchangeResponse,
  ExchangeSummaryResponse,
  UpdateCoordinatorMessageRequest,
} from '@/types/exchange.types'

export const exchangeService = {
  create: (request: CreateExchangeRequest) =>
    api.post<ExchangeResponse>('/exchanges', request),
  getById: (exchangeId: string) =>
    api.get<ExchangeResponse>(`/exchanges/${exchangeId}`),
  getMine: () =>
    api.get<ExchangeSummaryResponse[]>('/exchanges/mine'),
  deleteExchange: (exchangeId: string) =>
    api.delete(`/exchanges/${exchangeId}`),
  updateCoordinatorMessage: (exchangeId: string, request: UpdateCoordinatorMessageRequest) =>
    api.put<ExchangeResponse>(`/exchanges/${exchangeId}/coordinator-message`, request),
  getMyStudents: () =>
    api.get<ExchangeSummaryResponse[]>('/coordinator/students/exchanges'),
}
