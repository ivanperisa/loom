import { api } from './api'
import type { MappingSchemeResponse, SaveMappingSchemeRequest } from '@/types/mappingScheme.types'

export const mappingSchemeService = {
  get: (exchangeId: string, signal?: AbortSignal) =>
    api.get<MappingSchemeResponse>(`/api/exchanges/${exchangeId}/mapping-scheme`, { signal }),
  save: (exchangeId: string, request: SaveMappingSchemeRequest) =>
    api.put<MappingSchemeResponse>(`/api/exchanges/${exchangeId}/mapping-scheme/entries`, request),
}
