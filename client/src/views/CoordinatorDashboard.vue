<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { coordinatorService } from '@/services/coordinator.service'
import { institutionService } from '@/services/institution.service'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import type { ExchangeSummaryResponse } from '@/types/exchange.types'
import type { InstitutionResponse } from '@/types/institution.types'
import { statusColorClass } from '@/utils/statusColors'
import { nWord } from '@/utils/plural'
import CreateExchangeModal from '@/components/exchange/CreateExchangeModal.vue'
import { localizedName } from '@/utils/i18n.utils'

const router = useRouter()
const { t, locale } = useI18n()

const students = ref<CoordinatorStudentResponse[]>([])
const exchanges = ref<ExchangeSummaryResponse[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const expandedStudentId = ref<string | null>(null)

// Add student modal
const showAddModal = ref(false)
const addName = ref('')
const addJmbag = ref('')
const addInstitutionId = ref<string | null>(null)
const addError = ref<string | null>(null)
const addSubmitting = ref(false)
const institutions = ref<InstitutionResponse[]>([])
const institutionSearch = ref('')

const filteredInstitutions = computed(() => {
  const q = institutionSearch.value.trim().toLowerCase()
  if (!q) return institutions.value
  return institutions.value.filter(
    (i) =>
      i.name.toLowerCase().includes(q) ||
      (i.nameEn?.toLowerCase().includes(q) ?? false) ||
      (i.city?.toLowerCase().includes(q) ?? false) ||
      (i.country?.toLowerCase().includes(q) ?? false),
  )
})

const isJmbagValid = computed(() => /^\d{10}$/.test(addJmbag.value))

// Create exchange modal
const showCreateExchangeModal = ref(false)
const createExchangeTargetStudentId = ref<string | null>(null)

const exchangesByStudent = computed(() => {
  const map = new Map<string, ExchangeSummaryResponse[]>()
  for (const ex of exchanges.value) {
    const list = map.get(ex.studentId) ?? []
    list.push(ex)
    map.set(ex.studentId, list)
  }
  return map
})

onMounted(async () => {
  try {
    const [studentsRes, exchangesRes, institutionsRes] = await Promise.allSettled([
      coordinatorService.getStudents(),
      coordinatorService.getStudentsExchanges(),
      institutionService.getHomeInstitutions(),
    ])
    if (studentsRes.status === 'fulfilled') students.value = studentsRes.value.data
    if (exchangesRes.status === 'fulfilled') exchanges.value = exchangesRes.value.data
    if (institutionsRes.status === 'fulfilled') institutions.value = institutionsRes.value.data
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

function openAddModal() {
  addName.value = ''
  addJmbag.value = ''
  addInstitutionId.value = null
  addError.value = null
  institutionSearch.value = ''
  showAddModal.value = true
}

async function submitAddStudent() {
  addError.value = null
  if (!addName.value.trim()) {
    addError.value = t('coordinator.addStudentModal.errors.nameRequired')
    return
  }
  if (!isJmbagValid.value) {
    addError.value = t('coordinator.addStudentModal.errors.jmbagInvalid')
    return
  }
  if (!addInstitutionId.value) {
    addError.value = t('coordinator.addStudentModal.errors.institutionRequired')
    return
  }
  addSubmitting.value = true
  try {
    const res = await coordinatorService.createPlaceholderStudent({
      name: addName.value.trim(),
      jmbag: addJmbag.value,
      institutionId: addInstitutionId.value,
    })
    students.value.push(res.data)
    students.value.sort((a, b) => a.name.localeCompare(b.name))
    showAddModal.value = false
  } catch (e: unknown) {
    const err = e as { response?: { data?: { detail?: string } } }
    addError.value = err?.response?.data?.detail ?? t('errors.unexpected')
  } finally {
    addSubmitting.value = false
  }
}

function openCreateExchange(studentId: string) {
  createExchangeTargetStudentId.value = studentId
  showCreateExchangeModal.value = true
}

function onExchangeCreated(exchangeGuid: string) {
  showCreateExchangeModal.value = false
  router.push(`/exchange/${exchangeGuid}`)
}
</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container">
      <div class="mb-6 flex items-center justify-between">
        <h1 class="text-2xl font-bold text-light">{{ t('coordinator.title') }}</h1>
        <button
          type="button"
          class="flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
          @click="openAddModal"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          {{ t('coordinator.addStudent') }}
        </button>
      </div>

      <div v-if="loading" class="space-y-4">
        <div v-for="i in 3" :key="i" class="animate-pulse rounded-xl border border-primary/20 bg-dark-2 p-5">
          <div class="h-5 w-48 rounded bg-primary/20"></div>
          <div class="mt-3 h-4 w-72 rounded bg-primary/20"></div>
        </div>
      </div>

      <div v-else-if="error" class="rounded-xl border border-red-400/30 bg-red-900/20 p-8 text-center">
        <p class="text-red-300">{{ error }}</p>
      </div>

      <div v-else-if="students.length === 0" class="rounded-xl border border-primary/20 bg-dark-2 p-8 text-center">
        <svg class="mx-auto h-12 w-12 text-light/60" viewBox="0 0 24 24" fill="none">
          <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
          <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5" />
        </svg>
        <p class="mt-3 text-light/60">{{ t('coordinator.noStudents') }}</p>
      </div>

      <!-- Student list -->
      <div v-else class="space-y-3">
        <div
          v-for="student in students"
          :key="student.id"
          class="rounded-xl border border-primary/20 bg-dark-2 transition"
          :class="{ 'border-primary': expandedStudentId === student.id }"
        >
          <!-- Student row -->
          <button
            type="button"
            class="flex w-full items-center justify-between rounded-xl p-5 text-left transition hover:bg-dark"
            @click="toggleStudent(student.id)"
          >
            <div class="flex items-center gap-3">
              <svg
                class="h-4 w-4 text-light/60 transition-transform"
                :class="{ 'rotate-90': expandedStudentId === student.id }"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
              <div>
                <div class="flex items-center gap-2">
                  <h3 class="text-lg font-semibold text-light">{{ student.name }}</h3>
                  <span
                    v-if="student.isPlaceholder"
                    class="rounded-full bg-yellow-500/20 px-2 py-0.5 text-xs font-medium text-yellow-300"
                  >
                    {{ t('coordinator.placeholder') }}
                  </span>
                </div>
                <p v-if="student.jmbag" class="mt-0.5 font-mono text-sm text-light/60">
                  {{ student.jmbag }}
                </p>
              </div>
            </div>
            <span class="rounded-full bg-primary/20 px-3 py-1 text-xs font-medium text-primary-light">
              {{ nWord((exchangesByStudent.get(student.id) ?? []).length, locale, { en: ['exchange', 'exchanges'], hr: ['razmjena', 'razmjene', 'razmjena'] }) }}
            </span>
          </button>

          <!-- Expanded section -->
          <div v-if="expandedStudentId === student.id" class="border-t border-primary/20 px-5 pb-4 pt-3">
            <div v-if="(exchangesByStudent.get(student.id) ?? []).length === 0" class="py-4 text-center text-sm text-light/40">
              {{ t('coordinator.noExchanges') }}
            </div>

            <div v-else class="space-y-2">
              <div
                v-for="ex in exchangesByStudent.get(student.id)"
                :key="ex.id"
                class="flex cursor-pointer items-center justify-between rounded-lg border border-primary/20 bg-dark px-4 py-3 transition hover:border-light/60"
                @click="viewExchange(ex.guid)"
              >
                <div class="flex-1">
                  <div class="flex flex-wrap items-center gap-2">
                    <span class="text-xs font-medium text-light/50">
                      {{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}
                    </span>
                    <span
                      class="rounded-full border px-2 py-0.5 text-xs font-semibold"
                      :class="statusColorClass[ex.learningAgreementStatus]"
                    >
                      {{ t('exchange.tabs.learningAgreement') }}:
                      {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                    </span>
                    <span
                      class="rounded-full border px-2 py-0.5 text-xs font-semibold"
                      :class="statusColorClass[ex.recognitionStatus]"
                    >
                      {{ t('exchange.tabs.recognition') }}:
                      {{ t(`documentStatus.${ex.recognitionStatus}`) }}
                    </span>
                  </div>
                  <p class="mt-2.5 text-sm font-semibold text-light">{{ ex.partnerInstitutionName }}</p>
                  <p class="text-xs text-light/60">{{ ex.partnerProgramName }}</p>
                  <p class="mt-1.5 text-xs text-light/40">
                    {{ ex.homeProgramName }}<span v-if="ex.homeProfileName"> &middot; {{ ex.homeProfileName }}</span>
                  </p>
                </div>
                <svg class="h-5 w-5 text-light/60" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                </svg>
              </div>
            </div>

            <div class="mt-3 flex justify-end">
              <button
                type="button"
                class="flex items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-semibold text-primary-light transition hover:bg-primary/10"
                @click.stop="openCreateExchange(student.id)"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                </svg>
                {{ t('coordinator.createExchange') }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- Add student modal -->
    <div
      v-if="showAddModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      @click.self="showAddModal = false"
    >
      <div class="w-full max-w-md rounded-2xl border border-primary/20 bg-dark-2 p-6 shadow-xl">
        <h2 class="mb-5 text-xl font-bold text-light">{{ t('coordinator.addStudentModal.title') }}</h2>

        <div class="space-y-4">
          <!-- Name -->
          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">
              {{ t('coordinator.addStudentModal.nameLabel') }}
            </label>
            <input
              v-model="addName"
              type="text"
              :placeholder="t('coordinator.addStudentModal.namePlaceholder')"
              class="w-full rounded-xl border border-primary/20 bg-dark px-4 py-2.5 text-light placeholder-slate-500 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- JMBAG -->
          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">
              {{ t('coordinator.addStudentModal.jmbagLabel') }}
            </label>
            <input
              v-model="addJmbag"
              type="text"
              inputmode="numeric"
              maxlength="10"
              :placeholder="t('coordinator.addStudentModal.jmbagPlaceholder')"
              class="w-full rounded-xl border border-primary/20 bg-dark px-4 py-2.5 font-mono text-light placeholder-slate-500 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- Institution -->
          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">
              {{ t('coordinator.addStudentModal.institutionLabel') }}
            </label>
            <input
              v-model="institutionSearch"
              type="text"
              :placeholder="t('coordinator.addStudentModal.searchInstitution')"
              class="mb-2 w-full rounded-xl border border-primary/20 bg-dark px-4 py-2.5 text-sm text-light placeholder-slate-500 focus:border-primary focus:outline-none"
            />
            <div class="max-h-44 overflow-y-auto space-y-1">
              <button
                v-for="inst in filteredInstitutions"
                :key="inst.id"
                type="button"
                class="w-full rounded-lg border px-3 py-2 text-left text-sm transition"
                :class="addInstitutionId === inst.id
                  ? 'border-primary bg-primary/10 text-light'
                  : 'border-primary/20 bg-dark text-light hover:border-primary/50'"
                @click="addInstitutionId = inst.id"
              >
                <p class="truncate font-medium">{{ localizedName(inst) }}</p>
                <p v-if="inst.city || inst.country" class="truncate text-xs text-slate-400">
                  <template v-if="inst.city">{{ inst.city }}</template>
                  <template v-if="inst.city && inst.country"> · </template>
                  <template v-if="inst.country">{{ inst.country }}</template>
                </p>
              </button>
            </div>
          </div>

          <p v-if="addError" class="rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200">
            {{ addError }}
          </p>
        </div>

        <div class="mt-6 flex justify-end gap-3">
          <button
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm text-slate-200 transition hover:bg-slate-700/40"
            @click="showAddModal = false"
          >
            {{ t('coordinator.addStudentModal.cancel') }}
          </button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
            :disabled="addSubmitting"
            @click="submitAddStudent"
          >
            {{ addSubmitting ? t('common.loading') : t('coordinator.addStudentModal.submit') }}
          </button>
        </div>
      </div>
    </div>

    <!-- Create exchange modal -->
    <CreateExchangeModal
      v-if="showCreateExchangeModal"
      :target-student-id="createExchangeTargetStudentId"
      @close="showCreateExchangeModal = false"
      @created="onExchangeCreated"
    />
  </main>
</template>
