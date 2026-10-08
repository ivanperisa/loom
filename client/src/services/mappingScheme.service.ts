import { api } from './api'
import type { MappingSchemeResponse, SaveMappingSchemeRequest } from '@/types/mappingScheme.types'

export const mappingSchemeService = {
  get: (exchangeId: string) =>
    api.get<MappingSchemeResponse>(`/api/exchanges/${exchangeId}/mapping-scheme`),
  save: (exchangeId: string, request: SaveMappingSchemeRequest) =>
    api.put<MappingSchemeResponse>(`/api/exchanges/${exchangeId}/mapping-scheme/entries`, request),
}
