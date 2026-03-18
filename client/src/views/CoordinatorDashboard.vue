<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import { exchangeService } from '@/services/exchange.service'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'

const router = useRouter()
const { t } = useI18n()

const students = ref<ExchangeSummaryResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const statusColorClass: Record<string, string> = {
  Draft: 'bg-slate-500/20 text-slate-300 border-slate-400',
  Submitted: 'bg-[#218CD9]/20 text-[#8AC4ED] border-[#218CD9]',
  Approved: 'bg-green-500/20 text-green-300 border-green-400',
  Rejected: 'bg-red-500/20 text-red-300 border-red-400',
}

onMounted(async () => {
  try {
    const res = await exchangeService.getMyStudents()
    students.value = res.data
  } catch {
    error.value = t('common.error')
  } finally {
    loading.value = false
  }
})

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
          <div class="mt-2 h-4 w-40 rounded bg-[#1E4A6E]"></div>
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
          :key="student.id"
          class="cursor-pointer rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-5 transition hover:border-[#218CD9]"
          @click="viewExchange(student.id)"
        >
          <div class="flex flex-wrap items-start justify-between gap-3">
            <div class="flex-1">
              <div class="flex items-center gap-3">
                <h3 class="text-lg font-semibold text-[#CAE4F7]">{{ student.studentName }}</h3>
                <span
                  class="rounded-full border px-2.5 py-0.5 text-xs font-medium"
                  :class="statusColorClass[student.status] ?? statusColorClass.Draft"
                >
                  {{ t(`exchangeStatus.${student.status}`) }}
                </span>
              </div>
              <p v-if="student.studentJmbag" class="mt-0.5 text-sm font-mono text-[#5A8AAD]">{{ student.studentJmbag }}</p>
              <div class="mt-2 flex flex-wrap items-center gap-2 text-sm text-[#8AC4ED]">
                <span>{{ student.foreignInstitutionName }}</span>
                <span class="text-[#5A8AAD]">&middot;</span>
                <span>{{ student.foreignProgramName }}</span>
              </div>
              <div class="mt-1 flex flex-wrap items-center gap-2 text-xs text-[#5A8AAD]">
                <span>{{ student.studyProfileName }}</span>
                <span>&middot;</span>
                <span>{{ student.academicYear }}</span>
                <span>&middot;</span>
                <span>{{ t(`exchangeSemester.${student.semesterType}`) }}</span>
              </div>
            </div>
            <button
              type="button"
              class="rounded-lg bg-[#1E4A6E] px-4 py-2 text-sm font-medium text-[#CAE4F7] transition hover:bg-[#2E5A7E]"
              @click.stop="viewExchange(student.id)"
            >
              {{ t('coordinator.viewExchange') }}
            </button>
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
