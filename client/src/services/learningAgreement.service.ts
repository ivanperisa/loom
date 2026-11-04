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
  get: (exchangeId: string, signal?: AbortSignal) => api.get<LearningAgreementResponse>(base(exchangeId), { signal }),
  save: (exchangeId: string, request: SaveLearningAgreementRequest) =>
    api.put<LearningAgreementResponse>(base(exchangeId), request, { errorToast: false }),
  updateStatus: (exchangeId: string, request: UpdateLearningAgreementStatusRequest) =>
    api.patch<ExchangeResponse>(`${base(exchangeId)}/status`, request),
  updateMessage: (exchangeId: string, message: string | null) =>
    api.patch<LearningAgreementResponse>(`${base(exchangeId)}/message`, { message }),
  /** Approvals (with changes) and backups, newest first. */
  getVersions: (exchangeId: string, signal?: AbortSignal) => api.get<DocumentVersionResponse[]>(`${base(exchangeId)}/versions`, { signal }),
  restoreVersion: (exchangeId: string, versionId: number) =>
    api.post<RestoreResult>(`${base(exchangeId)}/versions/${versionId}/restore`, undefined, { errorToast: false }),
  exportMappings: (exchangeId: string) => api.get<Blob>(`${base(exchangeId)}/export`, { responseType: 'blob', errorToast: true }),
  previewImport: (exchangeId: string, file: MappingExportDto) =>
    api.post<ImportPreviewResponse>(`${base(exchangeId)}/import/preview`, file, { errorToast: false }),
  importMappings: (exchangeId: string, file: MappingExportDto) =>
    api.post<ImportResult>(`${base(exchangeId)}/import`, file, { errorToast: false }),
}
