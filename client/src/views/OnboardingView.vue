<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { AxiosError } from 'axios'
import { userService } from '@/services/user.service'
import { useAuthStore } from '@/stores/auth.store'
import InstitutionForm from '@/components/InstitutionForm.vue'
import type { ProblemDetails } from '@/types/api.types'
import type { OnboardingRequestDto } from '@/types/user.types'
import type { LocalInstitutionEntry } from '@/types/onboarding.types'
import { toInstitutionEntryDto } from '@/types/onboarding.types'
import type { UserRole } from '@/types/auth.types'

const router = useRouter()
const authStore = useAuthStore()
const { locale, t } = useI18n()

const steps = [
  { id: 1, key: 'onboarding.steps.role' },
  { id: 2, key: 'onboarding.steps.institutions' },
  { id: 3, key: 'onboarding.steps.confirm' }
]

const currentStep = ref(1)
const selectedRole = ref<UserRole | null>(null)
const selectedInstitutions = ref<LocalInstitutionEntry[]>([])
const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

const selectedRoleLabel = computed(() =>
  selectedRole.value === 'Coordinator' ? t('onboarding.role.coordinator') : t('onboarding.role.student')
)

function toggleLocale() {
  locale.value = locale.value === 'hr' ? 'en' : 'hr'
  localStorage.setItem('locale', locale.value)
}

async function logout() {
  await authStore.logout()
}

function isDuplicate(entry: LocalInstitutionEntry): boolean {
  return selectedInstitutions.value.some((x) => {
    const leftInstitution = x.existingInstitutionId ?? x.newInstitution?.name ?? ''
    const leftProfile = x.existingStudyProfileId ?? `${x.newStudyProfile?.studyProgramId}:${x.newStudyProfile?.profileName ?? ''}`

    const rightInstitution = entry.existingInstitutionId ?? entry.newInstitution?.name ?? ''
    const rightProfile =
      entry.existingStudyProfileId ?? `${entry.newStudyProfile?.studyProgramId}:${entry.newStudyProfile?.profileName ?? ''}`

    return leftInstitution === rightInstitution && leftProfile === rightProfile
  })
}

function addEntry(entry: LocalInstitutionEntry) {
  errorMessage.value = null
  if (isDuplicate(entry)) {
    errorMessage.value = t('onboarding.institutions.duplicate')
    return
  }

  selectedInstitutions.value.push({ ...entry, id: crypto.randomUUID() })
}

function removeEntry(index: number) {
  selectedInstitutions.value.splice(index, 1)
}

function goNext() {
  errorMessage.value = null

  if (currentStep.value === 1) {
    if (!selectedRole.value) {
      errorMessage.value = t('onboarding.errors.selectRole')
      return
    }

    currentStep.value = 2
    return
  }

  if (currentStep.value === 2) {
    if (selectedInstitutions.value.length === 0) {
      errorMessage.value = t('onboarding.institutions.minOne')
      return
    }

    currentStep.value = 3
  }
}

function goBack() {
  errorMessage.value = null
  if (currentStep.value > 1) {
    currentStep.value -= 1
  }
}

async function finishOnboarding() {
  if (!selectedRole.value) {
    errorMessage.value = t('onboarding.errors.selectRole')
    return
  }

  if (selectedInstitutions.value.length === 0) {
    errorMessage.value = t('onboarding.institutions.minOne')
    return
  }

  const payload: OnboardingRequestDto = {
    role: selectedRole.value,
    institutions: selectedInstitutions.value.map((x) => toInstitutionEntryDto(x))
  }

  try {
    isSubmitting.value = true
    errorMessage.value = null
    await userService.completeOnboarding(payload)
    await authStore.init(true)
    await router.push('/home')
  } catch (error) {
    const problem = (error as AxiosError<ProblemDetails>).response?.data
    errorMessage.value = problem?.detail ?? t('errors.unexpected')
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <main class="min-h-screen bg-[#071C2C] text-[#CAE4F7]">
    <header class="sticky top-0 z-40 w-full border-b border-[#218CD9] bg-[#071C2C]">
      <div class="mx-auto flex h-16 w-full max-w-5xl items-center justify-between px-4 sm:px-6">
        <span class="text-lg font-bold text-white">{{ t('common.appName') }}</span>
        <div class="flex items-center gap-3">
          <button
            type="button"
            class="inline-flex items-center rounded-full bg-[#218CD9] px-3 py-1.5 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C]"
            @click="toggleLocale"
          >
            {{ locale.toUpperCase() }}
          </button>
          <button
            type="button"
            class="text-sm font-semibold text-[#CAE4F7] transition hover:text-red-300"
            @click="logout"
          >
            {{ t('common.signOut') }}
          </button>
        </div>
      </div>
    </header>

    <section class="mx-auto flex w-full max-w-5xl flex-col px-4 py-10 sm:px-6">
      <div class="mb-10 flex flex-wrap items-center gap-3">
        <div v-for="(step, index) in steps" :key="step.id" class="flex items-center gap-3">
          <div
            class="flex h-9 w-9 items-center justify-center rounded-full border text-sm font-semibold"
            :class="
              index + 1 < currentStep
                ? 'border-[#8AC4ED] bg-[#8AC4ED] text-[#071C2C]'
                : index + 1 === currentStep
                  ? 'border-[#218CD9] bg-[#218CD9] text-white'
                  : 'border-slate-500 bg-transparent text-slate-300'
            "
          >
            {{ index + 1 }}
          </div>
          <span class="text-sm" :class="index + 1 <= currentStep ? 'text-[#CAE4F7]' : 'text-slate-400'">
            {{ t(step.key) }}
          </span>
          <div v-if="index < steps.length - 1" class="h-px w-8 bg-slate-600 sm:w-12"></div>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-700 bg-[#0B263B] p-6 shadow-xl sm:p-8">
        <h1 class="text-2xl font-bold">{{ t('onboarding.title') }}</h1>

        <p v-if="errorMessage" class="mt-4 rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200">
          {{ errorMessage }}
        </p>

        <div v-if="currentStep === 1" class="mt-6 grid gap-4 md:grid-cols-2">
          <p class="md:col-span-2 text-sm text-[#8AC4ED]">{{ t('onboarding.role.title') }}</p>
          <button
            type="button"
            class="rounded-xl border p-5 text-left transition"
            :class="
              selectedRole === 'Student'
                ? 'border-[#218CD9] bg-[#123451]'
                : 'border-slate-600 bg-[#0A2235] hover:border-[#8AC4ED]'
            "
            @click="selectedRole = 'Student'"
          >
            <h2 class="text-lg font-semibold">{{ t('onboarding.role.student') }}</h2>
          </button>

          <button
            type="button"
            class="rounded-xl border p-5 text-left transition"
            :class="
              selectedRole === 'Coordinator'
                ? 'border-[#218CD9] bg-[#123451]'
                : 'border-slate-600 bg-[#0A2235] hover:border-[#8AC4ED]'
            "
            @click="selectedRole = 'Coordinator'"
          >
            <h2 class="text-lg font-semibold">{{ t('onboarding.role.coordinator') }}</h2>
            <p class="mt-2 text-sm text-[#8AC4ED]">{{ t('onboarding.role.coordinatorNote') }}</p>
          </button>
        </div>

        <div v-if="currentStep === 2" class="mt-6 space-y-4">
          <h2 class="text-lg font-semibold">{{ t('onboarding.institutions.title') }}</h2>
          <p class="text-sm text-[#8AC4ED]">{{ t('onboarding.institutions.subtitle') }}</p>

          <div class="rounded-xl border border-slate-600 bg-[#071C2C] p-4">
            <p class="mb-3 text-sm font-semibold">{{ t('onboarding.institutions.added') }}</p>
            <p v-if="selectedInstitutions.length === 0" class="text-sm text-slate-300">{{ t('onboarding.institutions.empty') }}</p>
            <ul v-else class="space-y-2 text-sm">
              <li v-for="(entry, index) in selectedInstitutions" :key="entry.id" class="flex items-center justify-between gap-3">
                <span>
                  {{ entry.institutionName }}
                  <template v-if="entry.programName"> - {{ entry.programName }}</template>
                  <template v-if="entry.profileName"> - {{ entry.profileName }}</template>
                </span>
                <button
                  type="button"
                  class="rounded border border-red-400/40 px-2 py-1 text-xs text-red-200 hover:bg-red-500/20"
                  @click="removeEntry(index)"
                >
                  {{ t('settings.institutions.remove') }}
                </button>
              </li>
            </ul>
          </div>

          <InstitutionForm
            v-if="selectedRole"
            :role="selectedRole"
            :submit-label="t('onboarding.institutions.addToList')"
            @submit="addEntry"
          />
        </div>

        <div v-if="currentStep === 3" class="mt-6 rounded-xl border border-slate-600 bg-[#071C2C] p-5">
          <h2 class="text-lg font-semibold">{{ t('onboarding.confirm.title') }}</h2>
          <div class="mt-4 space-y-2 text-sm text-[#CAE4F7]">
            <p><span class="text-[#8AC4ED]">{{ t('onboarding.confirm.role') }}:</span> {{ selectedRoleLabel }}</p>
            <p class="text-[#8AC4ED]">{{ t('onboarding.institutions.added') }}:</p>
            <ul class="space-y-1">
              <li v-for="entry in selectedInstitutions" :key="entry.id">
                {{ entry.institutionName }}
                <template v-if="entry.programName"> - {{ entry.programName }}</template>
                <template v-if="entry.profileName"> - {{ entry.profileName }}</template>
              </li>
            </ul>
          </div>
        </div>

        <div class="mt-8 flex items-center justify-between">
          <button
            type="button"
            class="rounded-lg border border-slate-500 px-4 py-2 text-sm text-slate-200 transition hover:bg-slate-700/40 disabled:opacity-40"
            :disabled="currentStep === 1 || isSubmitting"
            @click="goBack"
          >
            {{ t('onboarding.back') }}
          </button>

          <button
            v-if="currentStep < 3"
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
            @click="finishOnboarding"
          >
            {{ isSubmitting ? t('common.loading') : t('onboarding.confirm.submit') }}
          </button>
        </div>
      </div>
    </section>
  </main>
</template>
