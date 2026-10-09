import { computed, toValue, type MaybeRefOrGetter } from 'vue'
import { useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/vue-query'
import { exchangeService } from '@/services/exchange.service'
import { learningAgreementService } from '@/services/learningAgreement.service'
import { recognitionService } from '@/services/recognition.service'
import { mappingSchemeService } from '@/services/mappingScheme.service'
import { documentService } from '@/services/document.service'
import { fetchAllPages } from '@/services/institution.service'
import { queryKeys } from '@/queries/keys'
import { saveBlob, saveResponse } from '@/utils/download'
import type { CreateExchangeRequest, ExchangeResponse, UpdateExchangeRequest } from '@/types/exchange.types'
import type {
  LearningAgreementResponse,
  MappingExportDto,
  SaveLearningAgreementRequest,
  UpdateLearningAgreementStatusRequest,
} from '@/types/learningAgreement.types'
import type { RecognitionResponse, SaveGradesRequest, UpdateRecognitionStatusRequest } from '@/types/recognition.types'
import type { MappingSchemeResponse, SaveMappingSchemeRequest } from '@/types/mappingScheme.types'
import type { PartnerCourseRequest } from '@/types/institution.types'

type Guid = MaybeRefOrGetter<string>

// Reads

export function useMyExchangesQuery() {
  return useQuery({
    queryKey: queryKeys.myExchanges,
    queryFn: async ({ signal }) => (await exchangeService.getMine(signal)).data,
  })
}

export function useExchangeQuery(guid: Guid) {
  return useQuery({
    queryKey: computed(() => queryKeys.exchangeDetails(toValue(guid))),
    queryFn: async ({ signal }) => (await exchangeService.getById(toValue(guid), signal)).data,
  })
}

export function useLearningAgreementQuery(guid: Guid) {
  return useQuery({
    queryKey: computed(() => queryKeys.learningAgreement(toValue(guid))),
    queryFn: async ({ signal }) => (await learningAgreementService.get(toValue(guid), signal)).data,
  })
}

export function useRecognitionQuery(guid: Guid) {
  return useQuery({
    queryKey: computed(() => queryKeys.recognition(toValue(guid))),
    queryFn: async ({ signal }) => (await recognitionService.get(toValue(guid), signal)).data,
  })
}

export function useMappingSchemeQuery(guid: Guid) {
  return useQuery({
    queryKey: computed(() => queryKeys.mappingScheme(toValue(guid))),
    queryFn: async ({ signal }) => (await mappingSchemeService.get(toValue(guid), signal)).data,
  })
}

/** Every course of the exchange's partner institution (all pages; the drag-and-drop lists filter locally). */
export function usePartnerCoursesQuery(guid: Guid) {
  return useQuery({
    queryKey: computed(() => queryKeys.partnerCourses(toValue(guid))),
    queryFn: ({ signal }) =>
      fetchAllPages((page, pageSize) => exchangeService.getPartnerCourses(toValue(guid), { page, pageSize }, signal)),
    staleTime: 60_000,
  })
}

export function useDocumentVersionsQuery(guid: Guid, document: 'la' | 'recognition') {
  return useQuery({
    queryKey: computed(() => queryKeys.versions(toValue(guid), document)),
    queryFn: async ({ signal }) =>
      (document === 'la'
        ? await learningAgreementService.getVersions(toValue(guid), signal)
        : await recognitionService.getVersions(toValue(guid), signal)
      ).data,
    staleTime: 0,
  })
}

// Writes. Each one stores what the server answered and marks the rest of the exchange stale.

/** Everything under the exchange except its partner course list, which only course changes affect. */
function invalidateExchange(client: QueryClient, guid: string) {
  return client.invalidateQueries({
    queryKey: queryKeys.exchange(guid),
    predicate: (q) => q.queryKey[2] !== 'partner-courses',
  })
}

function setExchange(client: QueryClient, guid: string, data: ExchangeResponse) {
  client.setQueryData(queryKeys.exchangeDetails(guid), data)
}

export function useExchangeMutations(guid: Guid) {
  const client = useQueryClient()
  const id = () => toValue(guid)

  const update = useMutation({
    mutationFn: async (request: UpdateExchangeRequest) => (await exchangeService.update(id(), request)).data,
    onSuccess: (data) => {
      setExchange(client, id(), data)
      client.invalidateQueries({ queryKey: queryKeys.myExchanges })
      client.invalidateQueries({ queryKey: queryKeys.studentsExchanges })
      return invalidateExchange(client, id())
    },
  })

  const remove = useMutation({
    mutationFn: () => exchangeService.deleteExchange(id()),
    onSuccess: () => {
      client.removeQueries({ queryKey: queryKeys.exchange(id()) })
      client.invalidateQueries({ queryKey: queryKeys.myExchanges })
      client.invalidateQueries({ queryKey: queryKeys.studentsExchanges })
    },
  })

  return { update, remove }
}

export function useCreateExchange() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (request: CreateExchangeRequest) => (await exchangeService.create(request)).data,
    onSuccess: (data) => {
      setExchange(client, data.guid, data)
      client.invalidateQueries({ queryKey: queryKeys.myExchanges })
      client.invalidateQueries({ queryKey: queryKeys.studentsExchanges })
    },
  })
}

export function useLearningAgreementMutations(guid: Guid) {
  const client = useQueryClient()
  const id = () => toValue(guid)
  const setLa = (data: LearningAgreementResponse) => client.setQueryData(queryKeys.learningAgreement(id()), data)

  const save = useMutation({
    mutationFn: async (request: SaveLearningAgreementRequest) => (await learningAgreementService.save(id(), request)).data,
    onSuccess: (data) => {
      setLa(data)
      return invalidateExchange(client, id())
    },
  })

  const setStatus = useMutation({
    mutationFn: async (request: UpdateLearningAgreementStatusRequest) => (await learningAgreementService.updateStatus(id(), request)).data,
    onSuccess: (data) => {
      setExchange(client, id(), data)
      client.invalidateQueries({ queryKey: queryKeys.myExchanges })
      client.invalidateQueries({ queryKey: queryKeys.studentsExchanges })
      return invalidateExchange(client, id())
    },
  })

  const setMessage = useMutation({
    mutationFn: async (message: string | null) => (await learningAgreementService.updateMessage(id(), message)).data,
    onSuccess: setLa,
  })

  const importFile = useMutation({
    mutationFn: async (file: MappingExportDto) => (await learningAgreementService.importMappings(id(), file)).data,
    onSuccess: () => invalidateExchange(client, id()),
  })

  const restore = useMutation({
    mutationFn: async (versionId: number) => (await learningAgreementService.restoreVersion(id(), versionId)).data,
    onSuccess: () => invalidateExchange(client, id()),
  })

  /** Import preview changes nothing; it is a mutation only because it posts a file. */
  const previewImport = useMutation({
    mutationFn: async (file: MappingExportDto) => (await learningAgreementService.previewImport(id(), file)).data,
  })

  /** "Export for import" (JSON). */
  const exportFile = useMutation({
    mutationFn: async () => {
      const res = await learningAgreementService.exportMappings(id())
      saveBlob(new Blob([res.data], { type: 'application/json' }), `la-export-${new Date().toISOString().slice(0, 10)}.json`)
    },
  })

  return { save, setStatus, setMessage, importFile, restore, previewImport, exportFile }
}

export function useRecognitionMutations(guid: Guid) {
  const client = useQueryClient()
  const id = () => toValue(guid)
  const setRecognition = (data: RecognitionResponse) => client.setQueryData(queryKeys.recognition(id()), data)

  /** "Start final recognition": the LA and table 1 freeze; results and the mapping scheme appear. */
  const start = useMutation({
    mutationFn: async () => (await recognitionService.start(id())).data,
    onSuccess: (data) => {
      setRecognition(data)
      return invalidateExchange(client, id())
    },
  })

  const saveGrades = useMutation({
    mutationFn: async (request: SaveGradesRequest) => (await recognitionService.saveGrades(id(), request)).data,
    onSuccess: (data) => {
      setRecognition(data)
      return client.invalidateQueries({ queryKey: queryKeys.mappingScheme(id()) })
    },
  })

  const setStatus = useMutation({
    mutationFn: async (request: UpdateRecognitionStatusRequest) => (await recognitionService.updateStatus(id(), request)).data,
    onSuccess: (data) => {
      setRecognition(data)
      return invalidateExchange(client, id())
    },
  })

  const setMessage = useMutation({
    mutationFn: async (message: string | null) => (await recognitionService.updateMessage(id(), message)).data,
    onSuccess: setRecognition,
  })

  const saveMappingScheme = useMutation({
    mutationFn: async (request: SaveMappingSchemeRequest) => (await mappingSchemeService.save(id(), request)).data,
    onSuccess: (data: MappingSchemeResponse) => {
      client.setQueryData(queryKeys.mappingScheme(id()), data)
      return client.invalidateQueries({ queryKey: queryKeys.recognition(id()) })
    },
  })

  return { start, saveGrades, setStatus, setMessage, saveMappingScheme }
}

export function useAddPartnerCourse(guid: Guid) {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (data: PartnerCourseRequest) => (await exchangeService.createPartnerCourse(toValue(guid), data)).data,
    onSuccess: () => client.invalidateQueries({ queryKey: queryKeys.partnerCourses(toValue(guid)) }),
  })
}

/** The official xlsx, built by the server from what is saved. */
export function useOfficialDocument(guid: Guid) {
  return useMutation({
    mutationFn: async (lang: string) => saveResponse(await documentService.downloadOfficial(toValue(guid), lang), 'exchange.xlsx'),
  })
}
