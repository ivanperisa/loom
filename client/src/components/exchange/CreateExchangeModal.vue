<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import { useExchangeStore } from '@/stores/exchange.store'
import { exchangeSemester } from '@/utils/exchangeSemester'
import type { HomeProgramResponse, HomeProfileResponse, PartnerProgramResponse } from '@/types/institution.types'
import type { ExchangeSemester } from '@/types/exchange.types'

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

const selectedProgram = computed(() =>
  homePrograms.value.find(p => p.id === selectedProgramId.value) ?? null
)
const availableProfiles = computed<HomeProfileResponse[]>(() =>
  selectedProgram.value?.profiles ?? []
)
const selectedProfile = computed(() =>
  availableProfiles.value.find(p => p.id === selectedProfileId.value) ?? null
)

// Step 2: Partner program
const partnerPrograms = ref<PartnerProgramResponse[]>([])
const loadingPartnerPrograms = ref(true)
const selectedPartnerProgramId = ref<string | null>(null)

const selectedPartnerProgram = computed(() =>
  partnerPrograms.value.find(p => p.id === selectedPartnerProgramId.value) ?? null
)

// Step 3: Details
const academicYear = ref('')
const semesterType = ref<ExchangeSemester>(exchangeSemester.Winter)
const studySemester = ref<number>(1)

watch(selectedProgramId, () => {
  selectedProfileId.value = null
})

onMounted(async () => {
  const [programsRes, partnerRes] = await Promise.allSettled([
    institutionService.getHomePrograms(),
    institutionService.getPartnerPrograms(),
  ])

  if (programsRes.status === 'fulfilled') homePrograms.value = programsRes.value.data
  loadingPrograms.value = false

  if (partnerRes.status === 'fulfilled') partnerPrograms.value = partnerRes.value.data
  loadingPartnerPrograms.value = false
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
    if (!studySemester.value || studySemester.value < 1) {
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
      studySemester: studySemester.value!
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
  'createExchange.steps.confirm'
]
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4" @mousedown.self="emit('close')">
    <div class="w-full max-w-2xl rounded-xl border border-primary/20 bg-dark-2 shadow-xl">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
        <h2 class="text-lg font-semibold text-light">{{ t('createExchange.title') }}</h2>
        <button
          type="button"
          class="text-xl text-light/60 transition hover:text-white"
          @click="emit('close')"
        >
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/></svg>
        </button>
      </div>

      <!-- Step indicator -->
      <div class="flex items-center gap-2 px-6 pt-4">
        <template v-for="(key, index) in stepKeys" :key="index">
          <div
            class="flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold"
            :class="
              index + 1 < currentStep
                ? 'bg-primary-light text-dark'
                : index + 1 === currentStep
                  ? 'bg-primary text-white'
                  : 'bg-primary/20 text-slate-400'
            "
          >
            <template v-if="index + 1 < currentStep">
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>
            </template>
            <template v-else>{{ index + 1 }}</template>
          </div>
          <span class="text-xs" :class="index + 1 <= currentStep ? 'text-light' : 'text-slate-500'">
            {{ t(key) }}
          </span>
          <div v-if="index < stepKeys.length - 1" class="h-px flex-1 bg-primary/20"></div>
        </template>
      </div>

      <!-- Body -->
      <div class="px-6 py-5">
        <p
          v-if="errorMessage"
          class="mb-4 rounded-lg border border-red-400/50 bg-red-500/10 px-3 py-2 text-sm text-red-200"
        >
          {{ errorMessage }}
        </p>

        <!-- Step 1: Program & Profile -->
        <div v-if="currentStep === 1" class="space-y-4">
          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">{{ t('createExchange.selectProgram') }}</label>
            <template v-if="loadingPrograms">
              <div class="h-10 animate-pulse rounded-lg bg-primary/20"></div>
            </template>
            <template v-else>
              <select
                v-model="selectedProgramId"
                class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-light focus:border-primary focus:outline-none"
              >
                <option :value="null" disabled>{{ t('createExchange.selectProgramPlaceholder') }}</option>
                <option v-for="prog in homePrograms" :key="prog.id" :value="prog.id">
                  {{ localizedName(prog) }}
                </option>
              </select>
            </template>
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">{{ t('createExchange.selectProfile') }}</label>
            <select
              v-model="selectedProfileId"
              :disabled="!selectedProgramId"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-light focus:border-primary focus:outline-none disabled:opacity-50"
            >
              <option :value="null" disabled>{{ t('createExchange.selectProfilePlaceholder') }}</option>
              <option v-for="prof in availableProfiles" :key="prof.id" :value="prof.id">
                {{ localizedName(prof) }}
              </option>
            </select>
          </div>
        </div>

        <!-- Step 2: Partner Profile -->
        <div v-if="currentStep === 2" class="space-y-4">
          <label class="block text-sm font-medium text-primary-light">{{ t('createExchange.selectPartnerProgram') }}</label>
          <template v-if="loadingPartnerPrograms">
            <div class="space-y-2">
              <div class="h-10 animate-pulse rounded-lg bg-primary/20"></div>
              <div class="h-10 animate-pulse rounded-lg bg-primary/20"></div>
            </div>
          </template>
          <template v-else>
            <div class="max-h-64 space-y-1.5 overflow-y-auto pr-1">
              <button
                v-for="pp in partnerPrograms"
                :key="pp.id"
                type="button"
                class="w-full rounded-lg border px-3 py-2.5 text-left text-sm transition"
                :class="
                  selectedPartnerProgramId === pp.id
                    ? 'border-primary bg-dark-2'
                    : 'border-primary/20 bg-dark hover:border-light/60'
                "
                @click="selectedPartnerProgramId = pp.id"
              >
                <span class="font-medium text-light">{{ localizedName(pp) }}</span>
                <span class="block text-xs text-light/60">{{ pp.institutionName }}</span>
              </button>
            </div>
          </template>
        </div>

        <!-- Step 3: Details -->
        <div v-if="currentStep === 3" class="space-y-4">
          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">{{ t('exchange.academicYear') }}</label>
            <input
              v-model="academicYear"
              type="text"
              :placeholder="t('createExchange.academicYearPlaceholder')"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-light focus:border-primary focus:outline-none"
            />
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">{{ t('exchange.semester') }}</label>
            <div class="flex gap-3">
              <button
                type="button"
                class="flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition"
                :class="
                  semesterType === exchangeSemester.Winter
                    ? 'border-primary bg-dark-2 text-white'
                    : 'border-primary/20 bg-dark text-light/60 hover:border-light/60'
                "
                @click="semesterType = exchangeSemester.Winter"
              >
                {{ t('exchangeSemester.Winter') }}
              </button>
              <button
                type="button"
                class="flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition"
                :class="
                  semesterType === exchangeSemester.Summer
                    ? 'border-primary bg-dark-2 text-white'
                    : 'border-primary/20 bg-dark text-light/60 hover:border-light/60'
                "
                @click="semesterType = exchangeSemester.Summer"
              >
                {{ t('exchangeSemester.Summer') }}
              </button>
            </div>
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-primary-light">{{ t('exchange.studySemester') }}</label>
            <select
              v-model.number="studySemester"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-light focus:border-primary focus:outline-none"
            >
              <option :value="1">1</option>
              <option :value="2">2</option>
              <option :value="3">3</option>
              <option :value="4">4</option>
            </select>
          </div>
        </div>

        <!-- Step 4: Confirm -->
        <div v-if="currentStep === 4" class="space-y-3">
          <h3 class="text-sm font-semibold text-primary-light">{{ t('createExchange.summary') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summaryProgram') }}</dt>
              <dd class="text-right font-medium text-light">{{ selectedProgram ? localizedName(selectedProgram) : '' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summaryProfile') }}</dt>
              <dd class="text-right font-medium text-light">{{ selectedProfile ? localizedName(selectedProfile) : '' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summaryPartnerProgram') }}</dt>
              <dd class="text-right font-medium text-light">
                {{ selectedPartnerProgram ? localizedName(selectedPartnerProgram) : '' }}
                <span v-if="selectedPartnerProgram" class="block text-xs text-light/60">{{ selectedPartnerProgram.institutionName }}</span>
              </dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summaryAcademicYear') }}</dt>
              <dd class="font-medium text-light">{{ academicYear }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summarySemesterType') }}</dt>
              <dd class="font-medium text-light">{{ t(`exchangeSemester.${semesterType}`) }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-light/60">{{ t('createExchange.summaryStudySemester') }}</dt>
              <dd class="font-medium text-light">{{ studySemester }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between border-t border-primary/20 px-6 py-4">
        <button
          type="button"
          class="rounded-lg border border-slate-500 px-4 py-2 text-sm text-slate-200 transition hover:bg-slate-700/40 disabled:opacity-40"
          :disabled="currentStep === 1 || isSubmitting"
          @click="goBack"
        >
          {{ t('onboarding.back') }}
        </button>

        <button
          v-if="currentStep < TOTAL_STEPS"
          type="button"
          class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark"
          @click="goNext"
        >
          {{ t('onboarding.next') }}
        </button>

        <button
          v-else
          type="button"
          class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-60"
          :disabled="isSubmitting"
          @click="submitExchange"
        >
          {{ isSubmitting ? t('common.loading') : t('createExchange.submitButton') }}
        </button>
      </div>
    </div>
  </div>
</template>
