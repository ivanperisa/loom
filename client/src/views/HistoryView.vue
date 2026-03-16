<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { exchangeService } from '@/services/exchange.service'
import AppHeader from '@/components/AppHeader.vue'
import type { MappingHistoryResponse, StudentExchangeSummaryResponse } from '@/types/exchange.types'

const { t, locale } = useI18n()
const authStore = useAuthStore()
const isCoordinator = computed(() => authStore.role === 'Coordinator')

const students = ref<StudentExchangeSummaryResponse[]>([])
const selectedExchangeId = ref<string | null>(null)

const history = ref<MappingHistoryResponse[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-500/20 text-yellow-300 border-yellow-500/30',
  Approved: 'bg-green-500/20 text-green-300 border-green-500/30',
  Rejected: 'bg-red-500/20 text-red-300 border-red-500/30'
}

onMounted(async () => {
  if (isCoordinator.value) {
    try {
      const res = await exchangeService.getStudentsWithExchange()
      students.value = res.data
      if (students.value.length > 0) {
        selectedExchangeId.value = students.value[0]!.exchangeId
      }
    } catch {
      // students list stays empty
    }
  } else {
    await fetchHistory()
  }
})

watch(selectedExchangeId, async (id) => {
  if (id) await fetchHistory(id)
})

async function fetchHistory(exchangeId?: string) {
  loading.value = true
  error.value = null
  try {
    const res = exchangeId
      ? await exchangeService.getExchangeHistory(exchangeId)
      : await exchangeService.getMyHistory()
    history.value = res.data
  } catch (e: any) {
    error.value = e.response?.data?.title ?? t('common.error')
  } finally {
    loading.value = false
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(locale.value === 'hr' ? 'hr-HR' : 'en-GB', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}
</script>

<template>
  <main class="min-h-screen bg-[#071C2C]">
    <AppHeader />
    <section class="mx-auto max-w-3xl px-6 py-10">

      <h1 class="mb-6 text-2xl font-bold text-[#CAE4F7]">
        {{ t('historyPage.title') }}
      </h1>

      <!-- Coordinator: student selector -->
      <div v-if="isCoordinator" class="mb-6">
        <label class="mb-2 block text-xs font-medium uppercase tracking-wide text-[#8AC4ED]">
          {{ t('historyPage.selectStudent') }}
        </label>
        <select
          v-model="selectedExchangeId"
          class="w-full rounded-lg border border-[#1E4A6E] bg-[#0A2235] px-4 py-2 text-[#CAE4F7] focus:border-[#2E7AB5] focus:outline-none"
        >
          <option v-for="s in students" :key="s.exchangeId" :value="s.exchangeId">
            {{ s.studentName }} — {{ s.academicYear }}, {{ t(`exchangeSemesters.${s.semester}`) }}, {{ s.foreignInstitutionName }}
          </option>
        </select>
        <p v-if="students.length === 0" class="mt-2 text-sm text-[#8AC4ED]">
          {{ t('historyPage.noStudents') }}
        </p>
      </div>

      <!-- Error -->
      <div v-if="error" class="mb-4 rounded-xl bg-red-900/30 px-4 py-3 text-sm text-red-300">
        {{ error }}
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-3">
        <div v-for="i in 4" :key="i" class="h-28 animate-pulse rounded-xl bg-[#0A2235]" />
      </div>

      <!-- Empty state -->
      <div
        v-else-if="!loading && history.length === 0 && (!isCoordinator || selectedExchangeId)"
        class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-10 text-center text-sm text-[#8AC4ED]"
      >
        {{ t('mappingHistory.noHistory') }}
      </div>

      <!-- Timeline -->
      <div v-else-if="history.length > 0" class="relative space-y-4">
        <!-- Vertical line -->
        <div class="absolute bottom-0 left-3 top-0 w-px bg-[#1E4A6E]" />

        <div v-for="entry in history" :key="entry.id" class="relative pl-10">
          <!-- Dot -->
          <div class="absolute left-1.5 top-4 h-3 w-3 rounded-full border-2 border-[#218CD9] bg-[#071C2C]" />

          <div class="rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-4">
            <!-- Header -->
            <div class="mb-2 flex flex-wrap items-center justify-between gap-2">
              <span class="text-sm font-medium text-[#CAE4F7]">{{ entry.changedByName }}</span>
              <span class="text-xs text-[#5A8AAD]">{{ formatDate(entry.createdAt) }}</span>
            </div>

            <!-- Exchange course context -->
            <p class="mb-3 text-xs text-[#8AC4ED]">
              <template v-if="entry.exchangeCourseCode">{{ entry.exchangeCourseCode }} — </template>{{ entry.exchangeCourseName }}
            </p>

            <!-- Snapshot -->
            <div class="space-y-1.5">
              <div class="flex flex-wrap items-center gap-2">
                <span
                  :class="[statusColors[entry.snapshot.status] ?? 'border-gray-500/30 bg-gray-500/20 text-gray-300', 'rounded-full border px-2.5 py-0.5 text-xs font-medium']"
                >
                  {{ t(`mappingStatus.${entry.snapshot.status}`) }}
                </span>
                <span class="text-sm text-[#CAE4F7]">{{ entry.snapshot.courseName }}</span>
                <span v-if="entry.snapshot.courseCode" class="text-xs text-[#5A8AAD]">({{ entry.snapshot.courseCode }})</span>
              </div>
              <div v-if="entry.snapshot.awardedEcts != null" class="text-xs text-[#8AC4ED]">
                {{ t('mapping.awardedEcts') }}: <span class="text-[#CAE4F7]">{{ entry.snapshot.awardedEcts }}</span>
              </div>
              <div v-if="entry.snapshot.convertedGrade" class="text-xs text-[#8AC4ED]">
                {{ t('mapping.convertedGrade') }}: <span class="text-[#CAE4F7]">{{ entry.snapshot.convertedGrade }}</span>
              </div>
              <div v-if="entry.snapshot.coordinatorNote" class="text-xs text-[#8AC4ED]">
                {{ t('mapping.coordinatorNote') }}: <span class="text-[#CAE4F7]">{{ entry.snapshot.coordinatorNote }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

    </section>
  </main>
</template>
