import { api } from './api'
import type { ExchangeResponse } from '@/types/exchange.types'
import type {
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
  SaveLearningAgreementRequest,
  LaSnapshotSummary,
  SnapshotListItem,
  MappingExportDto,
  MappingImportResult,
} from '@/types/learningAgreement.types'

export const learningAgreementService = {
  get: (exchangeId: string) =>
    api.get<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`),
  save: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement`, request, {
      suppressErrorToast: true,
    }),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest) =>
    api.patch<ExchangeResponse>(`/api/exchanges/${exchangeId}/learning-agreement/status`, request),
  updateMessage: (exchangeId: string, message: string | null) =>
    api.patch<LearningAgreementResponse>(`/api/exchanges/${exchangeId}/learning-agreement/message`, { message }),
  getHistory: (exchangeId: string) =>
    api.get<LaSnapshotSummary[]>(`/api/exchanges/${exchangeId}/learning-agreement/history`),
  getSnapshots: (exchangeId: string) =>
    api.get<SnapshotListItem[]>(`/api/exchanges/${exchangeId}/learning-agreement/snapshots`),
  restoreSnapshot: (exchangeId: string, snapshotId: number) =>
    api.post<void>(`/api/exchanges/${exchangeId}/learning-agreement/snapshots/${snapshotId}/restore`, undefined, {
      suppressErrorToast: true,
    }),
  exportMappings: (exchangeId: string) =>
    api.get<Blob>(`/api/exchanges/${exchangeId}/learning-agreement/export`, { responseType: 'blob' }),
  importMappings: (exchangeId: string, dto: MappingExportDto) =>
    api.post<MappingImportResult>(`/api/exchanges/${exchangeId}/learning-agreement/import`, dto, {
      suppressErrorToast: true,
    }),
}
