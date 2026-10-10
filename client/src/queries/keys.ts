/**
 * Query keys. Everything about one exchange sits under `['exchange', guid]`, so invalidating that prefix
 * after a change refreshes the exchange, its documents and its partner courses together.
 */
export const queryKeys = {
  exchange: (guid: string) => ['exchange', guid.toLowerCase()] as const,
  exchangeDetails: (guid: string) => [...queryKeys.exchange(guid), 'details'] as const,
  learningAgreement: (guid: string) => [...queryKeys.exchange(guid), 'learning-agreement'] as const,
  recognition: (guid: string) => [...queryKeys.exchange(guid), 'recognition'] as const,
  mappingScheme: (guid: string) => [...queryKeys.exchange(guid), 'mapping-scheme'] as const,
  partnerCourses: (guid: string) => [...queryKeys.exchange(guid), 'partner-courses'] as const,
  versions: (guid: string, document: 'la' | 'recognition') => [...queryKeys.exchange(guid), 'versions', document] as const,

  myExchanges: ['exchanges', 'mine'] as const,
  coordinatorStudents: ['coordinator', 'students'] as const,
  coordinators: ['coordinators'] as const,

  homeInstitutions: ['catalog', 'home'] as const,
  homePrograms: ['catalog', 'home-programs'] as const,
  partnerInstitutions: ['catalog', 'partner'] as const,
  partnerInstitutionCourses: (institutionId: number) => ['catalog', 'partner', institutionId, 'courses'] as const,

  adminUsers: ['admin', 'users'] as const,
  coordinatorRequests: ['admin', 'coordinator-requests'] as const,
  coordinatorWhitelist: ['admin', 'coordinator-whitelist'] as const,
}
