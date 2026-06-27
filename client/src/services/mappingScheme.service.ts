import { api } from './api'
import type { MappingSchemeResponse, SaveMappingSchemeRequest } from '@/types/mappingScheme.types'

function basePath(exchangeId: string, guest: boolean) {
  return guest ? `/api/exchanges/access/${exchangeId}` : `/api/exchanges/${exchangeId}`
}

export const mappingSchemeService = {
  get: (exchangeId: string, guest = false) =>
    api.get<MappingSchemeResponse>(`${basePath(exchangeId, guest)}/mapping-scheme`),
  save: (exchangeId: string, request: SaveMappingSchemeRequest, guest = false) =>
    api.put<MappingSchemeResponse>(`${basePath(exchangeId, guest)}/mapping-scheme/entries`, request),
}
