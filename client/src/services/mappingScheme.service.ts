import { api } from './api'
import { exchangeBasePath as basePath } from './exchangeBasePath'
import type { MappingSchemeResponse, SaveMappingSchemeRequest } from '@/types/mappingScheme.types'

export const mappingSchemeService = {
  get: (exchangeId: string, guest = false) =>
    api.get<MappingSchemeResponse>(`${basePath(exchangeId, guest)}/mapping-scheme`),
  save: (exchangeId: string, request: SaveMappingSchemeRequest, guest = false) =>
    api.put<MappingSchemeResponse>(`${basePath(exchangeId, guest)}/mapping-scheme/entries`, request),
}
