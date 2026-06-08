import { api } from './api'
import type {
  CreateExchangeRequest,
  ExchangeResponse,
  ExchangeSummaryResponse,
  UpdateCoordinatorMessageRequest,
} from '@/types/exchange.types'

export const exchangeService = {
  create: (request: CreateExchangeRequest) =>
    api.post<ExchangeResponse>('/api/exchanges', request),
  getById: (exchangeId: string) =>
    api.get<ExchangeResponse>(`/api/exchanges/${exchangeId}`),
  getPublic: (exchangeGuid: string) =>
    api.get<ExchangeResponse>(`/api/exchanges/access/${exchangeGuid}`),
  getMine: () =>
    api.get<ExchangeSummaryResponse[]>('/api/exchanges/mine'),
  deleteExchange: (exchangeId: string) =>
    api.delete(`/api/exchanges/${exchangeId}`),
  updateCoordinatorMessage: (exchangeId: string, request: UpdateCoordinatorMessageRequest) =>
    api.put<ExchangeResponse>(`/api/exchanges/${exchangeId}/coordinator-message`, request),
}
