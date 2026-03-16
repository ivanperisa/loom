import { defineStore } from 'pinia'
import { ref } from 'vue'
import { exchangeService } from '@/services/exchange.service'
import type {
  ExchangeResponse,
  CreateExchangeRequest,
  UpsertExchangeCourseRequest,
  UpdateGradesRequest,
  ProposeMappingRequest
} from '@/types/exchange.types'

export const useExchangeStore = defineStore('exchange', () => {
  const exchange = ref<ExchangeResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchMyExchange() {
    loading.value = true
    error.value = null
    try {
      const res = await exchangeService.getMyExchange()
      exchange.value = res.data
    } catch (e: any) {
      if (e.response?.status === 404) exchange.value = null
      else error.value = e.response?.data?.title ?? 'Greška'
    } finally {
      loading.value = false
    }
  }

  async function createExchange(data: CreateExchangeRequest) {
    const res = await exchangeService.createExchange(data)
    exchange.value = res.data
  }

  async function deleteExchange(exchangeId: string) {
    await exchangeService.deleteExchange(exchangeId)
    exchange.value = null
  }

  async function addCourse(exchangeId: string, data: UpsertExchangeCourseRequest) {
    const res = await exchangeService.addCourse(exchangeId, data)
    exchange.value?.courses.push(res.data)
  }

  async function updateCourse(exchangeId: string, courseId: string, data: UpsertExchangeCourseRequest) {
    const res = await exchangeService.updateCourse(exchangeId, courseId, data)
    if (exchange.value) {
      const idx = exchange.value.courses.findIndex(c => c.id === courseId)
      if (idx !== -1) exchange.value.courses[idx] = res.data
    }
  }

  async function removeCourse(exchangeId: string, courseId: string) {
    await exchangeService.removeCourse(exchangeId, courseId)
    if (exchange.value) {
      exchange.value.courses = exchange.value.courses.filter(c => c.id !== courseId)
    }
  }

  async function updateGrades(exchangeId: string, courseId: string, data: UpdateGradesRequest) {
    const res = await exchangeService.updateGrades(exchangeId, courseId, data)
    if (exchange.value) {
      const idx = exchange.value.courses.findIndex(c => c.id === courseId)
      if (idx !== -1) exchange.value.courses[idx] = res.data
    }
  }

  async function proposeMapping(exchangeId: string, courseId: string, data: ProposeMappingRequest) {
    const res = await exchangeService.proposeMapping(exchangeId, courseId, data)
    if (exchange.value) {
      const idx = exchange.value.courses.findIndex(c => c.id === courseId)
      if (idx !== -1) exchange.value.courses[idx] = res.data
    }
  }

  async function retract(exchangeId: string) {
    const res = await exchangeService.retract(exchangeId)
    exchange.value = res.data
  }

  async function submitForReview(exchangeId: string) {
    const res = await exchangeService.submitForReview(exchangeId)
    exchange.value = res.data
  }

  return {
    exchange,
    loading,
    error,
    fetchMyExchange,
    createExchange,
    deleteExchange,
    addCourse,
    updateCourse,
    removeCourse,
    updateGrades,
    proposeMapping,
    retract,
    submitForReview
  }
})
