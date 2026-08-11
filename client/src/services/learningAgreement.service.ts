import { api } from './api'
import { exchangeBasePath as basePath } from './exchangeBasePath'
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
  get: (exchangeId: string, guest = false) =>
    api.get<LearningAgreementResponse>(`${basePath(exchangeId, guest)}/learning-agreement`),
  save: (exchangeId: string, request: SaveLearningAgreementRequest, guest = false) =>
    api.put<LearningAgreementResponse>(`${basePath(exchangeId, guest)}/learning-agreement`, request, {
      suppressErrorToast: true,
    }),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest, guest = false) =>
    api.patch<ExchangeResponse>(`${basePath(exchangeId, guest)}/learning-agreement/status`, request),
  updateMessage: (exchangeId: string, message: string | null, guest = false) =>
    api.patch<LearningAgreementResponse>(`${basePath(exchangeId, guest)}/learning-agreement/message`, { message }),
  getHistory: (exchangeId: string, guest = false) =>
    api.get<LaSnapshotSummary[]>(`${basePath(exchangeId, guest)}/learning-agreement/history`),
  getSnapshots: (exchangeId: string, guest = false) =>
    api.get<SnapshotListItem[]>(`${basePath(exchangeId, guest)}/learning-agreement/snapshots`),
  restoreSnapshot: (exchangeId: string, snapshotId: number, guest = false) =>
    api.post<void>(`${basePath(exchangeId, guest)}/learning-agreement/snapshots/${snapshotId}/restore`, undefined, {
      suppressErrorToast: true,
    }),
  exportMappings: (exchangeId: string, guest = false) =>
    api.get<Blob>(`${basePath(exchangeId, guest)}/learning-agreement/export`, { responseType: 'blob' }),
  importMappings: (exchangeId: string, dto: MappingExportDto, guest = false) =>
    api.post<MappingImportResult>(`${basePath(exchangeId, guest)}/learning-agreement/import`, dto, {
      suppressErrorToast: true,
    }),
}
