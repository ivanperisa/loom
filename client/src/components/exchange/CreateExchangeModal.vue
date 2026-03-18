<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { institutionService } from '@/services/institution.service'
import { useExchangeStore } from '@/stores/exchange.store'
import type { StudyProgramResponse, StudyProfileResponse, ForeignProgramResponse } from '@/types/institution.types'
import type { AuthMeResponse } from '@/types/auth.types'
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

// Step 1: Study program & profile
const studyPrograms = ref<StudyProgramResponse[]>([])
const loadingPrograms = ref(true)
const selectedProgramId = ref<string | null>(null)
const selectedProfileId = ref<string | null>(null)

const selectedProgram = computed(() =>
  studyPrograms.value.find(p => p.id === selectedProgramId.value) ?? null
)
const availableProfiles = computed<StudyProfileResponse[]>(() =>
  selectedProgram.value?.profiles ?? []
)
const selectedProfile = computed(() =>
  availableProfiles.value.find(p => p.id === selectedProfileId.value) ?? null
)

// Step 2: Foreign program
const foreignPrograms = ref<ForeignProgramResponse[]>([])
const loadingForeignPrograms = ref(true)
const selectedForeignProgramId = ref<string | null>(null)

const selectedForeignProgram = computed(() =>
  foreignPrograms.value.find(p => p.id === selectedForeignProgramId.value) ?? null
)

// Step 3: Details
const coordinators = ref<AuthMeResponse[]>([])
const loadingCoordinators = ref(true)
const selectedCoordinatorId = ref<string | null>(null)
const academicYear = ref('')
const semesterType = ref<ExchangeSemester>('Winter')
const studySemester = ref<number>(1)
const mentor = ref('')

const selectedCoordinator = computed(() =>
  coordinators.value.find(c => c.id === selectedCoordinatorId.value) ?? null
)

// Reset profile when program changes
watch(selectedProgramId, () => {
  selectedProfileId.value = null
})

onMounted(async () => {
  const [programsRes, foreignRes, coordRes] = await Promise.allSettled([
    institutionService.getStudyPrograms(),
    institutionService.getForeignPrograms(),
    institutionService.getCoordinators()
  ])

  if (programsRes.status === 'fulfilled') studyPrograms.value = programsRes.value.data
  loadingPrograms.value = false

  if (foreignRes.status === 'fulfilled') foreignPrograms.value = foreignRes.value.data
  loadingForeignPrograms.value = false

  if (coordRes.status === 'fulfilled') coordinators.value = coordRes.value.data
  loadingCoordinators.value = false
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
    if (!selectedForeignProgramId.value) {
      errorMessage.value = t('createExchange.errors.foreignProgramRequired')
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
      studyProfileId: selectedProfileId.value!,
      foreignProgramId: selectedForeignProgramId.value!,
      coordinatorId: selectedCoordinatorId.value,
      mentor: mentor.value.trim() || null,
      academicYear: academicYear.value.trim(),
      semesterType: semesterType.value,
      studySemester: studySemester.value!
    })
    if (result) {
      emit('created', result.id)
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
  'createExchange.steps.foreign',
  'createExchange.steps.details',
  'createExchange.steps.confirm'
]
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4" @mousedown.self="emit('close')">
    <div class="w-full max-w-lg rounded-xl border border-[#1E4A6E] bg-[#0A2235] shadow-xl">
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-[#1E4A6E] px-6 py-4">
        <h2 class="text-lg font-semibold text-[#CAE4F7]">{{ t('createExchange.title') }}</h2>
        <button
          type="button"
          class="text-xl text-[#5A8AAD] transition hover:text-white"
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
                ? 'bg-[#8AC4ED] text-[#071C2C]'
                : index + 1 === currentStep
                  ? 'bg-[#218CD9] text-white'
                  : 'bg-[#1E4A6E] text-slate-400'
            "
          >
            <template v-if="index + 1 < currentStep">
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>
            </template>
            <template v-else>{{ index + 1 }}</template>
          </div>
          <span class="text-xs" :class="index + 1 <= currentStep ? 'text-[#CAE4F7]' : 'text-slate-500'">
            {{ t(key) }}
          </span>
          <div v-if="index < stepKeys.length - 1" class="h-px flex-1 bg-[#1E4A6E]"></div>
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
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('createExchange.selectProgram') }}</label>
            <template v-if="loadingPrograms">
              <div class="h-10 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
            </template>
            <template v-else>
              <select
                v-model="selectedProgramId"
                class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
              >
                <option :value="null" disabled>{{ t('createExchange.selectProgramPlaceholder') }}</option>
                <option v-for="prog in studyPrograms" :key="prog.id" :value="prog.id">
                  {{ localizedName(prog) }}
                </option>
              </select>
            </template>
          </div>

          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('createExchange.selectProfile') }}</label>
            <select
              v-model="selectedProfileId"
              :disabled="!selectedProgramId"
              class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none disabled:opacity-50"
            >
              <option :value="null" disabled>{{ t('createExchange.selectProfilePlaceholder') }}</option>
              <option v-for="prof in availableProfiles" :key="prof.id" :value="prof.id">
                {{ localizedName(prof) }}
              </option>
            </select>
          </div>
        </div>

        <!-- Step 2: Foreign Program -->
        <div v-if="currentStep === 2" class="space-y-4">
          <label class="block text-sm font-medium text-[#8AC4ED]">{{ t('createExchange.selectForeignProgram') }}</label>
          <template v-if="loadingForeignPrograms">
            <div class="space-y-2">
              <div class="h-10 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
              <div class="h-10 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
            </div>
          </template>
          <template v-else>
            <div class="max-h-64 space-y-1.5 overflow-y-auto pr-1">
              <button
                v-for="fp in foreignPrograms"
                :key="fp.id"
                type="button"
                class="w-full rounded-lg border px-3 py-2.5 text-left text-sm transition"
                :class="
                  selectedForeignProgramId === fp.id
                    ? 'border-[#218CD9] bg-[#123451]'
                    : 'border-[#1E4A6E] bg-[#071C2C] hover:border-[#5A8AAD]'
                "
                @click="selectedForeignProgramId = fp.id"
              >
                <span class="font-medium text-[#CAE4F7]">{{ localizedName(fp) }}</span>
                <span class="block text-xs text-[#5A8AAD]">{{ fp.institutionName }}</span>
              </button>
            </div>
          </template>
        </div>

        <!-- Step 3: Details -->
        <div v-if="currentStep === 3" class="space-y-4">
          <!-- Coordinator -->
          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('createExchange.selectCoordinator') }}</label>
            <template v-if="loadingCoordinators">
              <div class="h-10 animate-pulse rounded-lg bg-[#1E4A6E]"></div>
            </template>
            <template v-else>
              <select
                v-model="selectedCoordinatorId"
                class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
              >
                <option :value="null">{{ t('exchange.noCoordinator') }}</option>
                <option v-for="coord in coordinators" :key="coord.id" :value="coord.id">
                  {{ coord.name }} ({{ coord.email }})
                </option>
              </select>
            </template>
          </div>

          <!-- Academic Year -->
          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.academicYear') }}</label>
            <input
              v-model="academicYear"
              type="text"
              :placeholder="t('createExchange.academicYearPlaceholder')"
              class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            />
          </div>

          <!-- Semester Type -->
          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.semester') }}</label>
            <div class="flex gap-3">
              <button
                type="button"
                class="flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition"
                :class="
                  semesterType === 'Winter'
                    ? 'border-[#218CD9] bg-[#123451] text-white'
                    : 'border-[#1E4A6E] bg-[#071C2C] text-[#5A8AAD] hover:border-[#5A8AAD]'
                "
                @click="semesterType = 'Winter'"
              >
                {{ t('exchangeSemester.Winter') }}
              </button>
              <button
                type="button"
                class="flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition"
                :class="
                  semesterType === 'Summer'
                    ? 'border-[#218CD9] bg-[#123451] text-white'
                    : 'border-[#1E4A6E] bg-[#071C2C] text-[#5A8AAD] hover:border-[#5A8AAD]'
                "
                @click="semesterType = 'Summer'"
              >
                {{ t('exchangeSemester.Summer') }}
              </button>
            </div>
          </div>

          <!-- Study Semester -->
          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.studySemester') }}</label>
            <select
              v-model.number="studySemester"
              class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            >
              <option :value="1">1</option>
              <option :value="2">2</option>
              <option :value="3">3</option>
              <option :value="4">4</option>
            </select>
          </div>

          <!-- Mentor -->
          <div>
            <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.mentor') }}</label>
            <input
              v-model="mentor"
              type="text"
              :placeholder="t('createExchange.mentorPlaceholder')"
              class="w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            />
          </div>
        </div>

        <!-- Step 4: Confirm -->
        <div v-if="currentStep === 4" class="space-y-3">
          <h3 class="text-sm font-semibold text-[#8AC4ED]">{{ t('createExchange.summary') }}</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryProgram') }}</dt>
              <dd class="text-right font-medium text-[#CAE4F7]">{{ selectedProgram ? localizedName(selectedProgram) : '' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryProfile') }}</dt>
              <dd class="text-right font-medium text-[#CAE4F7]">{{ selectedProfile ? localizedName(selectedProfile) : '' }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryForeignProgram') }}</dt>
              <dd class="text-right font-medium text-[#CAE4F7]">
                {{ selectedForeignProgram ? localizedName(selectedForeignProgram) : '' }}
                <span v-if="selectedForeignProgram" class="block text-xs text-[#5A8AAD]">{{ selectedForeignProgram.institutionName }}</span>
              </dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryCoordinator') }}</dt>
              <dd class="text-right font-medium text-[#CAE4F7]">{{ selectedCoordinator?.name ?? t('exchange.noCoordinator') }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryAcademicYear') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ academicYear }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summarySemesterType') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ t(`exchangeSemester.${semesterType}`) }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryStudySemester') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ studySemester }}</dd>
            </div>
            <div v-if="mentor.trim()" class="flex justify-between">
              <dt class="text-[#5A8AAD]">{{ t('createExchange.summaryMentor') }}</dt>
              <dd class="font-medium text-[#CAE4F7]">{{ mentor }}</dd>
            </div>
          </dl>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between border-t border-[#1E4A6E] px-6 py-4">
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
          class="rounded-lg bg-[#218CD9] px-5 py-2 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
          @click="goNext"
        >
          {{ t('onboarding.next') }}
        </button>

        <button
          v-else
          type="button"
          class="rounded-lg bg-[#218CD9] px-5 py-2 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C] disabled:opacity-60"
          :disabled="isSubmitting"
          @click="submitExchange"
        >
          {{ isSubmitting ? t('common.loading') : t('createExchange.submitButton') }}
        </button>
      </div>
    </div>
  </div>
</template>
