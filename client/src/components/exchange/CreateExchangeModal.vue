<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import { useExchangeStore } from '@/stores/exchange.store'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type {
  HomeProgramResponse,
  HomeProfileResponse,
  PartnerProgramResponse,
} from '@/types/institution.types'
import type { AuthMeResponse } from '@/types/auth.types'
import type { ExchangeSemester } from '@/types/exchange.types'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import { nWord } from '@/utils/plural'

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'created', exchangeId: string): void
}>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()

function localizedName(item: { name: string; nameEn?: string | null }): string {
  return locale.value === 'en' && item.nameEn ? item.nameEn : item.name
}

const TOTAL_STEPS = 4
const currentStep = ref(1)
const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

// Step 1: Home program & profile
const homePrograms = ref<HomeProgramResponse[]>([])
const loadingPrograms = ref(true)
const selectedProgramId = ref<string | null>(null)
const selectedProfileId = ref<string | null>(null)

const selectedProgram = computed(
  () => homePrograms.value.find((p) => p.id === selectedProgramId.value) ?? null,
)
const availableProfiles = computed<HomeProfileResponse[]>(
  () => selectedProgram.value?.profiles ?? [],
)
const selectedProfile = computed(
  () => availableProfiles.value.find((p) => p.id === selectedProfileId.value) ?? null,
)

// Step 2: Partner program
const partnerPrograms = ref<PartnerProgramResponse[]>([])
const loadingPartnerPrograms = ref(true)
const selectedPartnerProgramId = ref<string | null>(null)
const partnerSearch = ref('')
const selectedCountry = ref<string | null>(null)

const homeProgramOptions = computed(() =>
  homePrograms.value.map((p) => ({ value: p.id, label: localizedName(p) })),
)

const countryOptions = computed(() => [
  { value: null, label: t('createExchange.allCountries') },
  ...availableCountries.value.map((c) => ({ value: c, label: c })),
])

const availableCountries = computed(() => {
  const countries = partnerPrograms.value
    .map((p) => p.institutionCountry)
    .filter((c): c is string => !!c)
  return [...new Set(countries)].sort()
})

const filteredPartnerPrograms = computed(() => {
  let list = partnerPrograms.value
  if (selectedCountry.value)
    list = list.filter((p) => p.institutionCountry === selectedCountry.value)
  if (partnerSearch.value.trim()) {
    const q = partnerSearch.value.trim().toLowerCase()
    list = list.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        (p.nameEn?.toLowerCase().includes(q) ?? false) ||
        p.institutionName.toLowerCase().includes(q) ||
        (p.institutionCity?.toLowerCase().includes(q) ?? false),
    )
  }
  return list
})

const selectedPartnerProgram = computed(
  () => partnerPrograms.value.find((p) => p.id === selectedPartnerProgramId.value) ?? null,
)

// Step 3: Coordinator
const coordinators = ref<AuthMeResponse[]>([])
const selectedCoordinatorId = ref<string | null>(null)

const coordinatorOptions = computed(() => [
  { value: null, label: t('exchange.noCoordinator') },
  ...coordinators.value.map((c) => ({ value: c.id, label: c.name })),
])

const selectedCoordinator = computed(
  () => coordinators.value.find((c) => c.id === selectedCoordinatorId.value) ?? null,
)

// Step 3: Details
function defaultAcademicYear(): string {
  const now = new Date()
  const y = now.getFullYear()
  const m = now.getMonth() + 1
  const start = m >= 9 ? y : y - 1
  return `${start + 1}/${start + 2}`
}

const academicYear = ref(defaultAcademicYear())
const semesterType = ref<ExchangeSemester>(exchangeSemester.Winter)
const studySemesters = ref<number[]>([])

const allowedSemesters = computed<number[]>(() => {
  if (semesterType.value === exchangeSemester.Winter) return [1, 3]
  if (semesterType.value === exchangeSemester.Summer) return [2, 4]
  return []
})

const bothPairs = [[1, 2], [3, 4]]

function selectPair(pair: number[]) {
  const sorted = [...pair].sort((a, b) => a - b)
  const isSame =
    studySemesters.value.length === sorted.length &&
    sorted.every((v, i) => studySemesters.value.slice().sort((a, b) => a - b)[i] === v)
  studySemesters.value = isSame ? [] : [...sorted]
}

function isPairSelected(pair: number[]): boolean {
  const sorted = [...pair].sort((a, b) => a - b)
  return (
    studySemesters.value.length === sorted.length &&
    sorted.every((v, i) => studySemesters.value.slice().sort((a, b) => a - b)[i] === v)
  )
}

function toggleStudySemester(s: number) {
  const idx = studySemesters.value.indexOf(s)
  if (idx === -1) studySemesters.value.push(s)
  else studySemesters.value.splice(idx, 1)
}

watch(semesterType, () => {
  studySemesters.value = []
})

watch(selectedProgramId, () => {
  selectedProfileId.value = null
})

onMounted(async () => {
  const [programsRes, partnerRes, coordRes] = await Promise.allSettled([
    institutionService.getHomePrograms(),
    institutionService.getPartnerPrograms(),
    institutionService.getCoordinators(),
  ])

  if (programsRes.status === 'fulfilled') homePrograms.value = programsRes.value.data
  loadingPrograms.value = false

  if (partnerRes.status === 'fulfilled') partnerPrograms.value = partnerRes.value.data
  loadingPartnerPrograms.value = false

  if (coordRes.status === 'fulfilled') coordinators.value = coordRes.value.data
})

function validateStep(): boolean {
  errorMessage.value = null

  if (currentStep.value === 1) {
    if (!selectedProgramId.value) {
      errorMessage.value = t('createExchange.errors.programRequired')
      return false
    }
    if (!selectedProfileId.value) {
      errorMessage.value = t('createExchange.errors.profileRequired')
      return false
    }
  }

  if (currentStep.value === 2) {
    if (!selectedPartnerProgramId.value) {
      errorMessage.value = t('createExchange.errors.partnerProgramRequired')
      return false
    }
  }

  if (currentStep.value === 3) {
    if (!academicYear.value.trim()) {
      errorMessage.value = t('createExchange.errors.academicYearRequired')
      return false
    }
    if (studySemesters.value.length === 0) {
      errorMessage.value = t('createExchange.errors.studySemesterRequired')
      return false
    }
  }

  return true
}

function goNext() {
  if (!validateStep()) return
  currentStep.value++
}

function goBack() {
  errorMessage.value = null
  if (currentStep.value > 1) currentStep.value--
}

async function submitExchange() {
  errorMessage.value = null
  isSubmitting.value = true
  try {
    const result = await exchangeStore.createExchange({
      homeProfileId: selectedProfileId.value!,
      partnerProgramId: selectedPartnerProgramId.value!,
      academicYear: academicYear.value.trim(),
      semesterType: semesterType.value,
      studySemesters: studySemesters.value,
      coordinatorId: selectedCoordinatorId.value,
    })
    if (result) {
      emit('created', result.guid)
    } else {
      errorMessage.value = exchangeStore.error ?? t('errors.unexpected')
    }
  } catch {
    errorMessage.value = t('errors.unexpected')
  } finally {
    isSubmitting.value = false
  }
}

const stepKeys = [
  'createExchange.steps.program',
  'createExchange.steps.partner',
  'createExchange.steps.details',
  'createExchange.steps.confirm',
]
</script>

<template>
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
    @mousedown.self="emit('close')"
  >
    <div
      class="flex w-full max-w-4xl flex-col rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl"
      style="max-height: 90vh"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-primary/20 px-8 py-5">
        <h2 class="text-xl font-semibold text-light">{{ t('createExchange.title') }}</h2>
        <button
          type="button"
          class="text-light/50 transition hover:text-white"
          @click="emit('close')"
        >
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
      </div>

      <!-- Step indicator -->
      <div class="flex items-center gap-2 px-8 pt-5">
        <template v-for="(key, index) in stepKeys" :key="index">
          <div
            class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full text-xs font-bold"
            :class="
              index + 1 < currentStep
                ? 'bg-primary text-white'
                : index + 1 === currentStep
                  ? 'bg-primary text-white ring-4 ring-primary/20'
                  : 'bg-white/5 text-slate-500'
            "
          >
            <template v-if="index + 1 < currentStep">
              <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                <path
                  fill-rule="evenodd"
                  d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                  clip-rule="evenodd"
                />
              </svg>
            </template>
            <template v-else>{{ index + 1 }}</template>
          </div>
          <span
            class="text-xs font-medium"
            :class="index + 1 <= currentStep ? 'text-light' : 'text-slate-600'"
          >
            {{ t(key) }}
          </span>
          <div v-if="index < stepKeys.length - 1" class="h-px flex-1 bg-primary/15"></div>
        </template>
      </div>

      <div class="min-h-0 flex-1 overflow-y-auto px-8 py-6">
        <p
          v-if="errorMessage"
          class="mb-5 rounded-xl border border-red-400/40 bg-red-500/10 px-4 py-3 text-sm text-red-300"
        >
          {{ errorMessage }}
        </p>

        <!-- Step 1: Home program & profile -->
        <div v-if="currentStep === 1" class="space-y-5">
          <div>
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('createExchange.selectProgram')
            }}</label>
            <div v-if="loadingPrograms" class="h-11 animate-pulse rounded-xl bg-white/5"></div>
            <SearchableSelect
              v-else
              v-model="selectedProgramId"
              :options="homeProgramOptions"
              :placeholder="t('createExchange.selectProgramPlaceholder')"
              :search-placeholder="t('createExchange.searchPartner')"
            />
          </div>

          <div>
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('createExchange.selectProfile')
            }}</label>
            <div
              v-if="!selectedProgramId"
              class="rounded-xl border border-white/5 bg-white/3 px-4 py-3 text-sm text-slate-500"
            >
              {{ t('createExchange.selectProgramFirst') }}
            </div>
            <div v-else class="grid grid-cols-2 gap-2 sm:grid-cols-3">
              <button
                v-for="prof in availableProfiles"
                :key="prof.id"
                type="button"
                class="rounded-xl border px-4 py-3 text-left text-sm transition"
                :class="
                  selectedProfileId === prof.id
                    ? 'border-primary bg-primary/10 text-white'
                    : 'border-white/10 bg-dark text-light/70 hover:border-primary/50 hover:text-white'
                "
                @click="selectedProfileId = prof.id"
              >
                {{ localizedName(prof) }}
              </button>
            </div>
          </div>
        </div>

        <!-- Step 2: Partner program -->
        <div v-if="currentStep === 2" class="flex flex-col gap-4">
          <label class="text-sm font-semibold text-primary-light">{{
            t('createExchange.selectPartnerProgram')
          }}</label>

          <!-- Filters -->
          <div class="flex gap-3">
            <SearchableSelect
              v-model="selectedCountry"
              :options="countryOptions"
              :searchable="false"
              class="w-48 flex-shrink-0"
            />
            <div class="relative flex-1">
              <svg
                class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                />
              </svg>
              <input
                v-model="partnerSearch"
                type="text"
                :placeholder="t('createExchange.searchPartner')"
                class="w-full rounded-xl border border-white/10 bg-dark py-2 pl-9 pr-4 text-sm text-light placeholder-slate-500 transition focus:border-primary focus:outline-none"
              />
            </div>
          </div>

          <!-- Results count -->
          <p class="text-xs text-slate-500">
            {{ nWord(filteredPartnerPrograms.length, locale, { en: ['result', 'results'], hr: ['rezultat', 'rezultata', 'rezultata'] }) }}
          </p>

          <!-- List -->
          <template v-if="loadingPartnerPrograms">
            <div class="space-y-2">
              <div v-for="i in 5" :key="i" class="h-16 animate-pulse rounded-xl bg-white/5"></div>
            </div>
          </template>
          <template v-else>
            <div class="space-y-2 overflow-y-auto pr-1" style="max-height: 340px">
              <p
                v-if="filteredPartnerPrograms.length === 0"
                class="py-8 text-center text-sm text-slate-500"
              >
                {{ t('createExchange.noResults') }}
              </p>
              <button
                v-for="pp in filteredPartnerPrograms"
                :key="pp.id"
                type="button"
                class="group w-full rounded-xl border px-4 py-3.5 text-left transition"
                :class="
                  selectedPartnerProgramId === pp.id
                    ? 'border-primary bg-primary/10'
                    : 'border-white/10 bg-dark hover:border-primary/40 hover:bg-white/3'
                "
                @click="selectedPartnerProgramId = pp.id"
              >
                <div class="flex items-start justify-between gap-4">
                  <div class="min-w-0">
                    <p class="truncate text-sm font-medium text-light">{{ localizedName(pp) }}</p>
                    <p class="mt-0.5 truncate text-xs text-slate-400">{{ pp.institutionName }}</p>
                  </div>
                  <div class="flex flex-shrink-0 flex-col items-end gap-1">
                    <span v-if="pp.institutionCountry" class="text-xs text-slate-500">{{
                      pp.institutionCountry
                    }}</span>
                    <span v-if="pp.institutionCity" class="text-xs text-slate-600">{{
                      pp.institutionCity
                    }}</span>
                  </div>
                </div>
              </button>
            </div>
          </template>
        </div>

        <!-- Step 3: Details -->
        <div v-if="currentStep === 3" class="grid grid-cols-2 gap-6">
          <!-- Academic year -->
          <div class="col-span-2 sm:col-span-1">
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('exchange.academicYear')
            }}</label>
            <input
              v-model="academicYear"
              type="text"
              :placeholder="t('createExchange.academicYearPlaceholder')"
              class="w-full rounded-xl border border-white/10 bg-dark px-4 py-2.5 text-sm text-light transition focus:border-primary focus:outline-none"
            />
          </div>

          <!-- Semester type -->
          <div class="col-span-2 sm:col-span-1">
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('exchange.semester')
            }}</label>
            <div class="grid grid-cols-3 gap-2">
              <button
                v-for="sem in [
                  exchangeSemester.Winter,
                  exchangeSemester.Summer,
                  exchangeSemester.Both,
                ]"
                :key="sem"
                type="button"
                class="rounded-xl border py-2.5 text-xs font-medium transition"
                :class="
                  semesterType === sem
                    ? 'border-primary bg-primary/10 text-white'
                    : 'border-white/10 bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                "
                @click="semesterType = sem"
              >
                {{ t(`exchangeSemester.${sem}`) }}
              </button>
            </div>
          </div>

          <!-- Study semesters -->
          <div class="col-span-2">
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('exchange.studySemester')
            }}</label>

            <!-- Winter / Summer: individual buttons -->
            <div v-if="semesterType !== exchangeSemester.Both" class="flex gap-2">
              <button
                v-for="s in allowedSemesters"
                :key="s"
                type="button"
                class="h-10 w-10 rounded-xl border text-sm font-semibold transition"
                :class="
                  studySemesters.includes(s)
                    ? 'border-primary bg-primary/10 text-white'
                    : 'border-white/10 bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                "
                @click="toggleStudySemester(s)"
              >
                {{ s }}
              </button>
            </div>

            <!-- Both: pair buttons -->
            <div v-else class="flex gap-3">
              <button
                v-for="pair in bothPairs"
                :key="pair.join()"
                type="button"
                class="rounded-xl border px-5 py-2.5 text-sm font-semibold transition"
                :class="
                  isPairSelected(pair)
                    ? 'border-primary bg-primary/10 text-white'
                    : 'border-white/10 bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                "
                @click="selectPair(pair)"
              >
                {{ pair.join(' + ') }}
              </button>
            </div>
          </div>

          <!-- Coordinator -->
          <div class="col-span-2">
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('createExchange.selectCoordinator')
            }}</label>
            <SearchableSelect
              v-model="selectedCoordinatorId"
              :options="coordinatorOptions"
              :placeholder="t('createExchange.selectCoordinatorPlaceholder')"
              :search-placeholder="t('settings.profile.searchCoordinator')"
              :no-results-label="t('settings.profile.noCoordinatorResults')"
            />
          </div>
        </div>

        <!-- Step 4: Confirm -->
        <div v-if="currentStep === 4">
          <h3 class="mb-4 text-sm font-semibold text-primary-light">
            {{ t('createExchange.summary') }}
          </h3>
          <div class="grid grid-cols-2 gap-3">
            <div
              class="col-span-2 rounded-xl border border-white/10 bg-dark px-4 py-3 sm:col-span-1"
            >
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryProgram') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{ selectedProgram ? localizedName(selectedProgram) : '' }}
              </p>
            </div>
            <div
              class="col-span-2 rounded-xl border border-white/10 bg-dark px-4 py-3 sm:col-span-1"
            >
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryProfile') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{ selectedProfile ? localizedName(selectedProfile) : '' }}
              </p>
            </div>
            <div class="col-span-2 rounded-xl border border-white/10 bg-dark px-4 py-3">
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryPartnerProgram') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{ selectedPartnerProgram ? localizedName(selectedPartnerProgram) : '' }}
              </p>
              <p v-if="selectedPartnerProgram" class="mt-0.5 text-xs text-slate-500">
                {{ selectedPartnerProgram.institutionName }}
                <template v-if="selectedPartnerProgram.institutionCity">
                  · {{ selectedPartnerProgram.institutionCity }}</template
                >
                <template v-if="selectedPartnerProgram.institutionCountry">
                  · {{ selectedPartnerProgram.institutionCountry }}</template
                >
              </p>
            </div>
            <div class="rounded-xl border border-white/10 bg-dark px-4 py-3">
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryAcademicYear') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">{{ academicYear }}</p>
            </div>
            <div class="rounded-xl border border-white/10 bg-dark px-4 py-3">
              <p class="text-xs text-slate-500">{{ t('createExchange.summarySemesterType') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{ t(`exchangeSemester.${semesterType}`) }}
              </p>
            </div>
            <div class="col-span-2 rounded-xl border border-white/10 bg-dark px-4 py-3">
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryStudySemester') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{
                  studySemesters
                    .slice()
                    .sort((a, b) => a - b)
                    .join(', ')
                }}
              </p>
            </div>
            <div class="col-span-2 rounded-xl border border-white/10 bg-dark px-4 py-3">
              <p class="text-xs text-slate-500">{{ t('createExchange.summaryCoordinator') }}</p>
              <p class="mt-0.5 text-sm font-medium text-light">
                {{ selectedCoordinator?.name ?? t('exchange.noCoordinator') }}
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between border-t border-primary/20 px-8 py-5">
        <button
          type="button"
          class="rounded-xl border border-white/10 px-5 py-2.5 text-sm font-medium text-slate-300 transition hover:bg-white/5 disabled:opacity-40"
          :disabled="currentStep === 1 || isSubmitting"
          @click="goBack"
        >
          {{ t('onboarding.back') }}
        </button>

        <button
          v-if="currentStep < TOTAL_STEPS"
          type="button"
          class="rounded-xl bg-primary px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
          @click="goNext"
        >
          {{ t('onboarding.next') }}
        </button>
        <button
          v-else
          type="button"
          class="rounded-xl bg-primary px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
          :disabled="isSubmitting"
          @click="submitExchange"
        >
          {{ isSubmitting ? t('common.loading') : t('createExchange.submitButton') }}
        </button>
      </div>
    </div>
  </div>
</template>
