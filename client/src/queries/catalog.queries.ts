import { useQuery } from '@tanstack/vue-query'
import { institutionService, fetchAllPages } from '@/services/institution.service'
import { coordinatorService } from '@/services/coordinator.service'
import { queryKeys } from '@/queries/keys'

// Reference data for dropdowns. It rarely changes, so it stays fresh for a while.
const REFERENCE_STALE_MS = 5 * 60_000

export function useHomeInstitutionsQuery() {
  return useQuery({
    queryKey: queryKeys.homeInstitutions,
    queryFn: async ({ signal }) => (await institutionService.getHomeInstitutions(signal)).data,
    staleTime: REFERENCE_STALE_MS,
  })
}

export function useHomeProgramsQuery() {
  return useQuery({
    queryKey: queryKeys.homePrograms,
    queryFn: async ({ signal }) => (await institutionService.getHomePrograms(signal)).data,
    staleTime: REFERENCE_STALE_MS,
  })
}

/** Every partner institution (all pages), for pickers. */
export function usePartnerInstitutionOptionsQuery() {
  return useQuery({
    queryKey: [...queryKeys.partnerInstitutions, 'all'],
    queryFn: ({ signal }) =>
      fetchAllPages((page, pageSize) => institutionService.getPartnerInstitutions(false, { page, pageSize }, signal)),
    staleTime: 60_000,
  })
}

/** Everyone selectable as coordinator. */
export function useCoordinatorsQuery(enabled: () => boolean = () => true) {
  return useQuery({
    queryKey: queryKeys.coordinators,
    queryFn: async ({ signal }) => (await coordinatorService.getCoordinators(signal)).data,
    staleTime: REFERENCE_STALE_MS,
    enabled,
  })
}
