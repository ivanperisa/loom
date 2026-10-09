import { api } from './api'
import type {
  AccessLinkResponse,
  CreateExchangeRequest,
  ExchangeResponse,
  ExchangeSummaryResponse,
  UpdateCoordinatorMessageRequest,
  UpdateExchangeRequest,
} from '@/types/exchange.types'
import type { PartnerCourseRequest, PartnerCourseResponse } from '@/types/institution.types'
import type { PagedParams, PagedResponse } from '@/types/paged.types'

export const exchangeService = {
  create: (request: CreateExchangeRequest) =>
    api.post<ExchangeResponse>('/api/exchanges', request),
  update: (exchangeId: string, request: UpdateExchangeRequest) =>
    api.put<ExchangeResponse>(`/api/exchanges/${exchangeId}`, request, { errorToast: false }),
  getById: (exchangeId: string, signal?: AbortSignal) =>
    api.get<ExchangeResponse>(`/api/exchanges/${exchangeId}`, { signal }),
  getMine: (signal?: AbortSignal) =>
    api.get<ExchangeSummaryResponse[]>('/api/exchanges/mine', { signal }),
  deleteExchange: (exchangeId: string) =>
    api.delete(`/api/exchanges/${exchangeId}`),
  updateCoordinatorMessage: (exchangeId: string, request: UpdateCoordinatorMessageRequest) =>
    api.put<ExchangeResponse>(`/api/exchanges/${exchangeId}/coordinator-message`, request),
  /** The live access link of a placeholder student's exchange (created on first use). */
  getAccessLink: (exchangeGuid: string) =>
    api.post<AccessLinkResponse>(`/api/exchanges/${exchangeGuid}/access-link`),
  regenerateAccessLink: (exchangeGuid: string) =>
    api.post<AccessLinkResponse>(`/api/exchanges/${exchangeGuid}/access-link/regenerate`),
  getPartnerCourses: (exchangeGuid: string, params: PagedParams = {}, signal?: AbortSignal) =>
    api.get<PagedResponse<PartnerCourseResponse>>(`/api/exchanges/${exchangeGuid}/partner-courses`, { params, signal }),
  createPartnerCourse: (exchangeGuid: string, data: PartnerCourseRequest) =>
    api.post<PartnerCourseResponse>(`/api/exchanges/${exchangeGuid}/partner-courses`, data),
}
