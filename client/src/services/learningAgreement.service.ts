import { api } from './api'
import type { ExchangeResponse } from '@/types/exchange.types'
import type { DocumentVersionResponse } from '@/types/documentVersion.types'
import type {
  LearningAgreementResponse,
  UpdateLearningAgreementStatusRequest,
  SaveLearningAgreementRequest,
  MappingExportDto,
  ImportPreviewResponse,
  ImportResult,
  RestoreResult,
} from '@/types/learningAgreement.types'

const base = (exchangeId: string) => `/api/exchanges/${exchangeId}/learning-agreement`

export const learningAgreementService = {
  get: (exchangeId: string) => api.get<LearningAgreementResponse>(base(exchangeId)),
  save: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(base(exchangeId), request, { suppressErrorToast: true }),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest) =>
    api.patch<ExchangeResponse>(`${base(exchangeId)}/status`, request),
  updateMessage: (exchangeId: string, message: string | null) =>
    api.patch<LearningAgreementResponse>(`${base(exchangeId)}/message`, { message }),
  /** Approvals (with changes) and backups, newest first. */
  getVersions: (exchangeId: string) => api.get<DocumentVersionResponse[]>(`${base(exchangeId)}/versions`),
  restoreVersion: (exchangeId: string, versionId: number) =>
    api.post<RestoreResult>(`${base(exchangeId)}/versions/${versionId}/restore`, undefined, { suppressErrorToast: true }),
  exportMappings: (exchangeId: string) => api.get<Blob>(`${base(exchangeId)}/export`, { responseType: 'blob' }),
  previewImport: (exchangeId: string, file: MappingExportDto) =>
    api.post<ImportPreviewResponse>(`${base(exchangeId)}/import/preview`, file, { suppressErrorToast: true }),
  importMappings: (exchangeId: string, file: MappingExportDto) =>
    api.post<ImportResult>(`${base(exchangeId)}/import`, file, { suppressErrorToast: true }),
}
