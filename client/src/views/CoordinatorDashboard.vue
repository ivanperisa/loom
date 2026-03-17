<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { exchangeService } from '@/services/exchange.service'
import type { CoordinatorStudentSummaryResponse, ExchangeResponse } from '@/types/exchange.types'

const { t } = useI18n()
const router = useRouter()

const students = ref<CoordinatorStudentSummaryResponse[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const selectedExchangeId = ref<string | null>(null)
const exchangeDetails = ref<ExchangeResponse | null>(null)
const detailsLoading = ref(false)
const detailsError = ref<string | null>(null)
const actionLoading = ref(false)

const statusColors: Record<string, string> = {
  Draft: 'bg-gray-500/20 text-gray-300',
  Submitted: 'bg-[#218CD9]/20 text-[#8AC4ED]',
  Approved: 'bg-green-500/20 text-green-300',
  Rejected: 'bg-red-500/20 text-red-300',
  Completed: 'bg-purple-500/20 text-purple-300'
}

onMounted(async () => {
  loading.value = true
  try {
    const res = await exchangeService.getCoordinatorStudents()
    students.value = res.data
  } catch (e: any) {
    error.value = e.response?.data?.title ?? t('common.error')
  } finally {
    loading.value = false
  }
})

async function selectExchange(exchangeId: string) {
  selectedExchangeId.value = exchangeId
  detailsLoading.value = true
  detailsError.value = null
  try {
    const res = await exchangeService.getExchangeDetails(exchangeId)
    exchangeDetails.value = res.data
  } catch (e: any) {
    detailsError.value = e.response?.data?.title ?? t('common.error')
  } finally {
    detailsLoading.value = false
  }
}

function goBackToList() {
  selectedExchangeId.value = null
  exchangeDetails.value = null
}

async function performAction(action: () => Promise<any>) {
  if (!selectedExchangeId.value) return
  actionLoading.value = true
  detailsError.value = null
  try {
    const res = await action()
    exchangeDetails.value = res.data
    updateStudentFromDetails(selectedExchangeId.value, res.data)
    await refreshStudentList()
  } catch (e: any) {
    detailsError.value = e.response?.data?.title ?? t('common.error')
  } finally {
    actionLoading.value = false
  }
}

async function approve() {
  if (!confirm(t('coordinator.confirmApprove'))) return
  await performAction(() => exchangeService.approveExchange(selectedExchangeId.value!))
}

async function reject() {
  if (!confirm(t('coordinator.confirmReject'))) return
  await performAction(() => exchangeService.rejectExchange(selectedExchangeId.value!))
}

async function returnToDraft() {
  if (!confirm(t('coordinator.confirmReturn'))) return
  await performAction(() => exchangeService.returnExchange(selectedExchangeId.value!))
}

async function refreshStudentList() {
  try {
    const res = await exchangeService.getCoordinatorStudents()
    students.value = res.data
  } catch {
    // keep existing list on error
  }
}

function updateStudentFromDetails(exchangeId: string, exchange: ExchangeResponse) {
  const s = students.value.find(s => s.exchangeId === exchangeId)
  if (!s) return
  s.status = exchange.status as any
  s.totalCourses = exchange.courses.length
  s.pendingMappings = exchange.courses
    .reduce((sum, c) => sum + c.mappings.filter(m => m.status === 'Pending').length, 0)
  s.approvedMappings = exchange.courses
    .reduce((sum, c) => sum + c.mappings.filter(m => m.status === 'Approved').length, 0)
}

function openMappingBoard() {
  router.push({ name: 'exchange-mapping', query: { exchangeId: selectedExchangeId.value! } })
}

const selectedStudent = computed(() =>
  students.value.find(s => s.exchangeId === selectedExchangeId.value)
)

const totalMappings = computed(() => {
  if (!exchangeDetails.value) return 0
  return exchangeDetails.value.courses.reduce((sum, c) => sum + c.mappings.length, 0)
})

const approvedMappings = computed(() => {
  if (!exchangeDetails.value) return 0
  return exchangeDetails.value.courses.reduce(
    (sum, c) => sum + c.mappings.filter(m => m.status === 'Approved').length, 0
  )
})

const pendingMappings = computed(() => {
  if (!exchangeDetails.value) return 0
  return exchangeDetails.value.courses.reduce(
    (sum, c) => sum + c.mappings.filter(m => m.status === 'Pending').length, 0
  )
})

const rejectedMappings = computed(() => {
  if (!exchangeDetails.value) return 0
  return exchangeDetails.value.courses.reduce(
    (sum, c) => sum + c.mappings.filter(m => m.status === 'Rejected').length, 0
  )
})
</script>

<template>
  <section class="mx-auto max-w-4xl px-6 py-8">
    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16 text-[#8AC4ED]">
      {{ t('common.loading') }}
    </div>

    <!-- Error -->
    <div v-else-if="error" class="rounded-xl bg-red-900/30 px-6 py-4 text-red-300">
      {{ error }}
    </div>

    <!-- State 2: Exchange details -->
    <div v-else-if="selectedExchangeId">
      <button
        @click="goBackToList"
        class="mb-6 text-sm font-medium text-[#8AC4ED] hover:text-[#CAE4F7] transition"
      >
        &larr; {{ t('coordinator.backToList') }}
      </button>

      <div v-if="detailsLoading" class="flex justify-center py-16 text-[#8AC4ED]">
        {{ t('common.loading') }}
      </div>

      <div v-else-if="detailsError" class="rounded-xl bg-red-900/30 px-6 py-4 text-red-300">
        {{ detailsError }}
      </div>

      <div v-else-if="exchangeDetails">
        <!-- Header -->
        <div class="mb-6">
          <div class="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-[#CAE4F7]">
                {{ selectedStudent?.studentName }} — {{ exchangeDetails.foreignInstitution.nameEn }}
              </h1>
              <div class="mt-1 flex flex-wrap items-center gap-3 text-sm text-[#8AC4ED]">
                <span>{{ exchangeDetails.academicYear }}</span>
                <span>·</span>
                <span>{{ t(`exchangeSemesters.${exchangeDetails.semester}`) }}</span>
              </div>
            </div>
            <span
              :class="[statusColors[exchangeDetails.status] ?? 'bg-gray-500/20 text-gray-300', 'rounded-full px-3 py-1 text-sm font-medium']"
            >
              {{ t(`exchangeStatus.${exchangeDetails.status}`) }}
            </span>
          </div>
        </div>

        <!-- Action buttons -->
        <div v-if="exchangeDetails.status === 'Submitted'" class="mb-6 flex flex-wrap gap-3">
          <button
            @click="approve"
            :disabled="actionLoading"
            class="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 transition disabled:opacity-50"
          >
            {{ t('coordinator.approveExchange') }}
          </button>
          <button
            @click="reject"
            :disabled="actionLoading"
            class="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 transition disabled:opacity-50"
          >
            {{ t('coordinator.rejectExchange') }}
          </button>
          <button
            @click="returnToDraft"
            :disabled="actionLoading"
            class="rounded-lg border border-[#1E4A6E] px-4 py-2 text-sm font-medium text-[#8AC4ED] hover:text-[#CAE4F7] transition disabled:opacity-50"
          >
            {{ t('coordinator.returnToDraft') }}
          </button>
        </div>

        <!-- Mapping board link -->
        <div class="mb-6">
          <button
            @click="openMappingBoard"
            class="rounded-lg border border-[#218CD9] px-4 py-2 text-sm font-medium text-[#8AC4ED] hover:bg-[#218CD9]/10 hover:text-[#CAE4F7] transition"
          >
            {{ t('coordinator.openMappingBoard') }} &rarr;
          </button>
        </div>

        <!-- Course summary -->
        <div class="rounded-xl bg-[#0A2235] border border-[#1E4A6E] p-5">
          <h2 class="mb-3 text-lg font-semibold text-[#CAE4F7]">{{ t('coordinator.courseSummary') }}</h2>
          <ul class="space-y-1 text-sm text-[#8AC4ED]">
            <li>{{ exchangeDetails.courses.length }} {{ t('coordinator.foreignCourses') }}</li>
            <li>
              {{ totalMappings }} {{ t('coordinator.mappingsCount') }}
              ({{ approvedMappings }} {{ t('coordinator.approved') }},
              {{ pendingMappings }} {{ t('coordinator.pending') }},
              {{ rejectedMappings }} {{ t('coordinator.rejected') }})
            </li>
          </ul>
        </div>
      </div>
    </div>

    <!-- State 1: Student list -->
    <div v-else>
      <h1 class="mb-6 text-2xl font-bold text-[#CAE4F7]">{{ t('coordinator.title') }}</h1>

      <div v-if="students.length === 0" class="py-16 text-center text-[#5A8AAD]">
        {{ t('coordinator.noStudents') }}
      </div>

      <div class="space-y-4">
        <div
          v-for="student in students"
          :key="student.exchangeId"
          :class="[
            'rounded-xl bg-[#0A2235] p-5 transition',
            student.pendingMappings > 0 ? 'border-2 border-[#218CD9]' : 'border border-[#1E4A6E]'
          ]"
        >
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="flex-1">
              <div class="flex items-center gap-3">
                <h3 class="text-lg font-semibold text-[#CAE4F7]">{{ student.studentName }}</h3>
                <span
                  :class="[statusColors[student.status] ?? 'bg-gray-500/20 text-gray-300', 'rounded-full px-2.5 py-0.5 text-xs font-medium']"
                >
                  {{ t(`exchangeStatus.${student.status}`) }}
                </span>
              </div>
              <div class="mt-1 flex flex-wrap items-center gap-2 text-sm text-[#5A8AAD]">
                <span v-if="student.studentJmbag">{{ student.studentJmbag }} ·</span>
                <span>{{ student.studentEmail }}</span>
              </div>
              <div class="mt-1 text-sm text-[#8AC4ED]">
                {{ student.foreignInstitutionName }}
                <span v-if="student.foreignInstitutionCountry">({{ student.foreignInstitutionCountry }})</span>
                · {{ student.academicYear }}
                · {{ t(`exchangeSemesters.${student.semester}`) }}
              </div>
              <div class="mt-1.5 flex items-center gap-4 text-xs text-[#5A8AAD]">
                <span>{{ student.totalCourses }} {{ t('coordinator.foreignCourses') }}</span>
                <span v-if="student.pendingMappings > 0" class="text-yellow-400">
                  ● {{ student.pendingMappings }} {{ t('coordinator.pendingReview') }}
                </span>
              </div>
            </div>
            <button
              @click="selectExchange(student.exchangeId)"
              class="rounded-lg bg-[#1E4A6E] px-4 py-2 text-sm font-medium text-[#CAE4F7] hover:bg-[#2E5A7E] transition"
            >
              {{ t('coordinator.viewExchange') }} &rarr;
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
