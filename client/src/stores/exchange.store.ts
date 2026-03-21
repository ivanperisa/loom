import { defineStore } from 'pinia'
import { ref } from 'vue'
import { exchangeService } from '@/services/exchange.service'
import type {
  ExchangeSummaryResponse,
  ExchangeResponse,
  LearningAgreementResponse,
  CreateExchangeRequest,
  SetSlotModeRequest,
  AddSlotMappingRequest,
  RemoveSlotMappingRequest,
  UpdateExchangeStatusRequest,
  UpdateCoordinatorMessageRequest,
} from '@/types/exchange.types'
import type { ForeignCourseResponse } from '@/types/institution.types'

export const useExchangeStore = defineStore('exchange', () => {
  const summaries = ref<ExchangeSummaryResponse[]>([])
  const exchange = ref<ExchangeResponse | null>(null)
  const learningAgreement = ref<LearningAgreementResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const draggingCourse = ref<ForeignCourseResponse | null>(null)

  function startDrag(course: ForeignCourseResponse) {
    draggingCourse.value = course
  }

  function endDrag() {
    draggingCourse.value = null
  }

  async function fetchMySummaries() {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getMine()
      summaries.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu razmjena.'
    } finally {
      loading.value = false
    }
  }

  async function deleteExchange(exchangeId: string) {
    await exchangeService.deleteExchange(exchangeId)
    summaries.value = summaries.value.filter(s => s.id !== exchangeId)
  }

  async function fetchExchange(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getById(exchangeId)
      exchange.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu razmjene.'
    } finally {
      loading.value = false
    }
  }

  async function createExchange(request: CreateExchangeRequest) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.create(request)
      exchange.value = res.data
      return res.data
    } catch (e: unknown) {
      error.value = 'Greška pri kreiranju razmjene.'
      return null
    } finally {
      loading.value = false
    }
  }

  async function fetchLearningAgreement(exchangeId: string) {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getLearningAgreement(exchangeId)
      learningAgreement.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri dohvatu learning agreementa.'
    } finally {
      loading.value = false
    }
  }

  async function setSlotMode(exchangeId: string, request: SetSlotModeRequest) {
    try {
      const res = await exchangeService.setSlotMode(exchangeId, request)
      learningAgreement.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri postavljanju stanja ćelije.'
    }
  }

  async function addSlotMapping(exchangeId: string, request: AddSlotMappingRequest) {
    try {
      const res = await exchangeService.addSlotMapping(exchangeId, request)
      learningAgreement.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri dodavanju mapiranja.'
    }
  }

  async function removeSlotMapping(exchangeId: string, request: RemoveSlotMappingRequest) {
    try {
      const res = await exchangeService.removeSlotMapping(exchangeId, request)
      learningAgreement.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri uklanjanju mapiranja.'
    }
  }

  async function removeSlotState(exchangeId: string, courseSlotId: string) {
    try {
      const res = await exchangeService.removeSlotState(exchangeId, courseSlotId)
      learningAgreement.value = res.data
    } catch {
      error.value = 'Greška pri uklanjanju stanja.'
    }
  }

  async function updateStatus(exchangeId: string, request: UpdateExchangeStatusRequest) {
    try {
      const res = await exchangeService.updateStatus(exchangeId, request)
      exchange.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri promjeni statusa.'
    }
  }

  async function updateCoordinatorMessage(exchangeId: string, request: UpdateCoordinatorMessageRequest) {
    try {
      const res = await exchangeService.updateCoordinatorMessage(exchangeId, request)
      exchange.value = res.data
    } catch (e: unknown) {
      error.value = 'Greška pri ažuriranju poruke.'
    }
  }

  return {
    summaries, exchange, learningAgreement, loading, error,
    draggingCourse, startDrag, endDrag,
    fetchMySummaries, fetchExchange, createExchange, deleteExchange,
    fetchLearningAgreement, setSlotMode, addSlotMapping,
    removeSlotMapping, removeSlotState, updateStatus, updateCoordinatorMessage,
  }
})
