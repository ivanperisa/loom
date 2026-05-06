<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { exchangeService } from '@/services/exchange.service'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import { statusColorClass } from '@/utils/statusColors'

const router = useRouter()
const { t } = useI18n()

const exchanges = ref<ExchangeSummaryResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const expandedStudentId = ref<string | null>(null)


interface StudentGroup {
  studentId: string
  studentName: string
  studentJmbag: string | null
  exchanges: ExchangeSummaryResponse[]
}

const students = computed<StudentGroup[]>(() => {
  const map = new Map<string, StudentGroup>()
  for (const ex of exchanges.value) {
    let group = map.get(ex.studentId)
    if (!group) {
      group = {
        studentId: ex.studentId,
        studentName: ex.studentName,
        studentJmbag: ex.studentJmbag,
        exchanges: [],
      }
      map.set(ex.studentId, group)
    }
    group.exchanges.push(ex)
  }
  return Array.from(map.values())
})

onMounted(async () => {
  try {
    const res = await exchangeService.getMyStudents()
    exchanges.value = res.data
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

function toggleStudent(studentId: string) {
  expandedStudentId.value = expandedStudentId.value === studentId ? null : studentId
}

function viewExchange(exchangeId: string) {
  router.push(`/exchange/${exchangeId}`)
}
</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <h1 class="mb-6 text-2xl font-bold text-light">{{ t('coordinator.title') }}</h1>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-4">
        <div v-for="i in 3" :key="i" class="animate-pulse rounded-xl border border-primary/20 bg-dark-2 p-5">
          <div class="h-5 w-48 rounded bg-primary/20"></div>
          <div class="mt-3 h-4 w-72 rounded bg-primary/20"></div>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-8 text-center">
        <p class="text-red-300">{{ error }}</p>
      </div>

      <!-- Empty -->
      <div v-else-if="students.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <svg class="mx-auto h-12 w-12 text-light/60" viewBox="0 0 24 24" fill="none">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5"/>
        </svg>
        <p class="mt-3 text-light/60">{{ t('coordinator.noStudents') }}</p>
      </div>

      <!-- Student list -->
      <div v-else class="space-y-3">
        <div
          v-for="student in students"
          :key="student.studentId"
          class="rounded-xl border border-primary/20 bg-dark-2 transition"
          :class="{ 'border-primary': expandedStudentId === student.studentId }"
        >
          <!-- Student row (clickable) -->
          <button
            type="button"
            class="flex w-full items-center justify-between rounded-xl p-5 text-left transition hover:bg-dark"
            @click="toggleStudent(student.studentId)"
          >
            <div class="flex items-center gap-3">
              <svg
                class="h-4 w-4 text-light/60 transition-transform"
                :class="{ 'rotate-90': expandedStudentId === student.studentId }"
                viewBox="0 0 20 20" fill="currentColor"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
              </svg>
              <div>
                <h3 class="text-lg font-semibold text-light">{{ student.studentName }}</h3>
                <p v-if="student.studentJmbag" class="mt-0.5 font-mono text-sm text-light/60">{{ student.studentJmbag }}</p>
              </div>
            </div>
            <span class="rounded-full bg-primary/20 px-3 py-1 text-xs font-medium text-primary-light">
              {{ student.exchanges.length }} {{ student.exchanges.length === 1 ? t('coordinator.exchangeCount') : t('coordinator.exchangesCount') }}
            </span>
          </button>

          <!-- Expanded: exchange list -->
          <div v-if="expandedStudentId === student.studentId" class="border-t border-primary/20 px-5 pb-4 pt-3">
            <div class="space-y-2">
              <div
                v-for="ex in student.exchanges"
                :key="ex.id"
                class="flex cursor-pointer items-center justify-between rounded-lg border border-primary/20 bg-dark px-4 py-3 transition hover:border-light/60"
                @click="viewExchange(ex.id)"
              >
                <div class="flex-1">
                  <div class="flex flex-wrap items-center gap-2">
                    <span class="font-medium text-light">{{ ex.foreignInstitutionName }}</span>
                    <span
                      class="rounded-full border px-2 py-0.5 text-xs font-medium"
                      :class="statusColorClass[ex.status] ?? statusColorClass.Draft"
                    >
                      {{ t('exchange.tabs.learningAgreement') }}: {{ t(`exchangeStatus.${ex.status}`) }}
                    </span>
                    <span
                      v-if="ex.status === 'Approved' || ex.recognitionStatus"
                      class="rounded-full border px-2 py-0.5 text-xs font-medium"
                      :class="statusColorClass[ex.recognitionStatus ?? 'Draft'] ?? statusColorClass.Draft"
                    >
                      {{ t('exchange.tabs.recognition') }}: {{ t(`recognitionStatus.${ex.recognitionStatus ?? 'Draft'}`) }}
                    </span>
                  </div>
                  <div class="mt-1 flex flex-wrap items-center gap-2 text-sm text-light/60">
                    <span>{{ ex.foreignProgramName }}</span>
                    <span>&middot;</span>
                    <span>{{ ex.academicYear }}</span>
                    <span>&middot;</span>
                    <span>{{ t(`exchangeSemester.${ex.semesterType}`) }}</span>
                  </div>
                </div>
                <svg class="h-5 w-5 text-light/60" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
                </svg>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
