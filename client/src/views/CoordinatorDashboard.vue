<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import { exchangeService } from '@/services/exchange.service'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'

const router = useRouter()
const { t } = useI18n()

const exchanges = ref<ExchangeSummaryResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const expandedStudentId = ref<string | null>(null)

const statusColorClass: Record<string, string> = {
  Draft: 'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-[#218CD9]/20 text-[#8AC4ED] border-[#218CD9]',
  Approved: 'bg-green-500/20 text-green-300 border-green-400',
  Rejected: 'bg-red-500/20 text-red-300 border-red-400',
}

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
  <main class="min-h-screen bg-[#071C2C]">
    <AppHeader />

    <section class="page-container">
      <h1 class="mb-6 text-2xl font-bold text-[#CAE4F7]">{{ t('coordinator.title') }}</h1>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-4">
        <div v-for="i in 3" :key="i" class="animate-pulse rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-5">
          <div class="h-5 w-48 rounded bg-[#1E4A6E]"></div>
          <div class="mt-3 h-4 w-72 rounded bg-[#1E4A6E]"></div>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-8 text-center">
        <p class="text-red-300">{{ error }}</p>
      </div>

      <!-- Empty -->
      <div v-else-if="students.length === 0" class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-8 text-center">
        <svg class="mx-auto h-12 w-12 text-[#5A8AAD]" viewBox="0 0 24 24" fill="none">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5"/>
        </svg>
        <p class="mt-3 text-[#5A8AAD]">{{ t('coordinator.noStudents') }}</p>
      </div>

      <!-- Student list -->
      <div v-else class="space-y-3">
        <div
          v-for="student in students"
          :key="student.studentId"
          class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] transition"
          :class="{ 'border-[#218CD9]': expandedStudentId === student.studentId }"
        >
          <!-- Student row (clickable) -->
          <button
            type="button"
            class="flex w-full items-center justify-between rounded-xl p-5 text-left transition hover:bg-[#0D2A40]"
            @click="toggleStudent(student.studentId)"
          >
            <div class="flex items-center gap-3">
              <svg
                class="h-4 w-4 text-[#5A8AAD] transition-transform"
                :class="{ 'rotate-90': expandedStudentId === student.studentId }"
                viewBox="0 0 20 20" fill="currentColor"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd"/>
              </svg>
              <div>
                <h3 class="text-lg font-semibold text-[#CAE4F7]">{{ student.studentName }}</h3>
                <p v-if="student.studentJmbag" class="mt-0.5 font-mono text-sm text-[#5A8AAD]">{{ student.studentJmbag }}</p>
              </div>
            </div>
            <span class="rounded-full bg-[#1E4A6E] px-3 py-1 text-xs font-medium text-[#8AC4ED]">
              {{ student.exchanges.length }} {{ student.exchanges.length === 1 ? t('coordinator.exchangeCount') : t('coordinator.exchangesCount') }}
            </span>
          </button>

          <!-- Expanded: exchange list -->
          <div v-if="expandedStudentId === student.studentId" class="border-t border-[#1E4A6E] px-5 pb-4 pt-3">
            <div class="space-y-2">
              <div
                v-for="ex in student.exchanges"
                :key="ex.id"
                class="flex cursor-pointer items-center justify-between rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-4 py-3 transition hover:border-[#5A8AAD]"
                @click="viewExchange(ex.id)"
              >
                <div class="flex-1">
                  <div class="flex items-center gap-2">
                    <span class="font-medium text-[#CAE4F7]">{{ ex.foreignInstitutionName }}</span>
                    <span
                      class="rounded-full border px-2 py-0.5 text-xs font-medium"
                      :class="statusColorClass[ex.status] ?? statusColorClass.Draft"
                    >
                      {{ t(`exchangeStatus.${ex.status}`) }}
                    </span>
                  </div>
                  <div class="mt-1 flex flex-wrap items-center gap-2 text-sm text-[#5A8AAD]">
                    <span>{{ ex.foreignProgramName }}</span>
                    <span>&middot;</span>
                    <span>{{ ex.academicYear }}</span>
                    <span>&middot;</span>
                    <span>{{ t(`exchangeSemester.${ex.semesterType}`) }}</span>
                  </div>
                </div>
                <svg class="h-5 w-5 text-[#5A8AAD]" viewBox="0 0 20 20" fill="currentColor">
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
