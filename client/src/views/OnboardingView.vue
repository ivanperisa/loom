<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { institutionService } from '@/services/institution.service'
import LanguageSwitcher from '@/components/common/LanguageSwitcher.vue'
import type { InstitutionResponse } from '@/types/institution.types'
import { localizedName } from '@/utils/i18n.utils'
import { userRole } from '../utils/userRole'

const router = useRouter()
const authStore = useAuthStore()
const { t } = useI18n()

const isCoordinatorOrAdmin = computed(() => authStore.canActAsCoordinator)

// Step 1 choice: null = not yet chosen, 'Student' | 'Coordinator'
const roleChoice = ref<typeof userRole.Student | typeof userRole.Coordinator | null>(null)

const steps = computed(() => {
  if (isCoordinatorOrAdmin.value) {
    return [{ id: 1, key: 'onboarding.steps.institution' }]
  }
  const base = [
    { id: 1, key: 'onboarding.steps.role' },
    { id: 2, key: 'onboarding.steps.institution' },
  ]
  if (roleChoice.value !== userRole.Coordinator) {
    base.push({ id: 3, key: 'onboarding.steps.jmbag' })
  }
  return base
})

const totalSteps = computed(() => steps.value.length)

const currentStep = ref(1)
const selectedInstitutionId = ref<string | null>(null)
const jmbag = ref('')
const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

const institutions = ref<InstitutionResponse[]>([])
const loadingInstitutions = ref(true)
const institutionSearch = ref('')

const filteredInstitutions = computed(() => {
  const q = institutionSearch.value.trim().toLowerCase()
  if (!q) return institutions.value
  return institutions.value.filter(
    (i) =>
      i.name.toLowerCase().includes(q) ||
      (i.nameHr?.toLowerCase().includes(q) ?? false) ||
      (i.city?.toLowerCase().includes(q) ?? false) ||
      (i.country?.toLowerCase().includes(q) ?? false),
  )
})

const isJmbagValid = computed(() => /^\d{10}$/.test(jmbag.value))

// For already-coordinator/admin, institution is step 1; for students, institution is step 2
const institutionStep = computed(() => (isCoordinatorOrAdmin.value ? 1 : 2))
const jmbagStep = computed(() => (isCoordinatorOrAdmin.value ? -1 : 3))

onMounted(async () => {
  try {
    const res = await institutionService.getHomeInstitutions()
    institutions.value = res.data
  } catch {
    // keep empty
  } finally {
    loadingInstitutions.value = false
  }
})

function goNext() {
  errorMessage.value = null

  if (!isCoordinatorOrAdmin.value && currentStep.value === 1) {
    if (!roleChoice.value) {
      errorMessage.value = t('onboarding.errors.roleRequired')
      return
    }
    currentStep.value = 2
    return
  }

  if (currentStep.value === institutionStep.value) {
    if (!selectedInstitutionId.value) {
      errorMessage.value = t('onboarding.errors.institutionRequired')
      return
    }
    currentStep.value++
  }
}

function goBack() {
  errorMessage.value = null
  if (currentStep.value > 1) {
    currentStep.value -= 1
  }
}

watch(currentStep, () => {
  institutionSearch.value = ''
})

async function finishOnboarding() {
  errorMessage.value = null

  if (!selectedInstitutionId.value) {
    errorMessage.value = t('onboarding.errors.institutionRequired')
    return
  }

  const isRequestingCoordinator = !isCoordinatorOrAdmin.value && roleChoice.value === userRole.Coordinator

  if (!isRequestingCoordinator && !isCoordinatorOrAdmin.value) {
    if (!jmbag.value.trim()) {
      errorMessage.value = t('onboarding.errors.jmbagRequired')
      return
    }
    if (!isJmbagValid.value) {
      errorMessage.value = t('onboarding.errors.jmbagInvalid')
      return
    }
  }

  try {
    isSubmitting.value = true
    await authStore.completeOnboarding({
      institutionId: selectedInstitutionId.value!,
      jmbag: isRequestingCoordinator || isCoordinatorOrAdmin.value ? undefined : jmbag.value.trim(),
      requestCoordinatorRole: isRequestingCoordinator || undefined,
    })
    await router.push('/home')
  } catch {
    errorMessage.value = authStore.error ?? t('errors.unexpected')
  } finally {
    isSubmitting.value = false
  }
}

async function logout() {
  await authStore.logout()
}
</script>

<template>
  <main class="min-h-screen bg-dark text-light">
    <header class="sticky top-0 z-40 w-full border-b border-primary bg-dark">
      <div class="page-container flex h-16 items-center justify-between !py-0">
        <span class="text-lg font-bold text-white">{{ t('common.appName') }}</span>
        <div class="flex items-center gap-3">
          <LanguageSwitcher variant="dark" />
          <button
            type="button"
            class="text-sm font-semibold text-light transition hover:text-red-300"
            @click="logout"
          >
            {{ t('common.signOut') }}
          </button>
        </div>
      </div>
    </header>

    <section class="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-2xl items-center justify-center px-4 py-10">
      <div class="w-full">
        <!-- Step indicator -->
        <div class="mb-8 flex items-center justify-center gap-3">
          <template v-for="(step, index) in steps" :key="step.id">
            <div
              class="flex h-9 w-9 items-center justify-center rounded-full border text-sm font-semibold"
              :class="
                index + 1 < currentStep
                  ? 'border-primary-light bg-primary-light text-dark'
                  : index + 1 === currentStep
                    ? 'border-primary bg-primary text-white'
                    : 'border-slate-500 bg-transparent text-slate-300'
              "
            >
              <template v-if="index + 1 < currentStep">
                <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>
              </template>
              <template v-else>{{ index + 1 }}</template>
            </div>
            <span class="text-sm" :class="index + 1 <= currentStep ? 'text-light' : 'text-slate-400'">
              {{ t(step.key) }}
            </span>
            <div v-if="index < steps.length - 1" class="h-px w-10 bg-slate-600"></div>
          </template>
        </div>

        <!-- Card -->
        <div class="rounded-2xl border border-primary/20 bg-dark-2 p-6 shadow-xl sm:p-8">
          <!-- Logo placeholder -->
          <div class="mb-6 flex justify-center">
            <div class="flex h-14 w-14 items-center justify-center rounded-full bg-primary">
              <svg class="h-7 w-7 text-white" viewBox="0 0 24 24" fill="none">
                <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8"/>
                <path d="M7 14h3l2-4 2 6 3-4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
          </div>

          <h1 class="text-center text-2xl font-bold">{{ t('onboarding.title') }}</h1>

          <p
            v-if="errorMessage"
            class="mt-4 rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200"
          >
            {{ errorMessage }}
          </p>

          <!-- Step 1 (students only): Role choice -->
          <div v-if="!isCoordinatorOrAdmin && currentStep === 1" class="mt-6 space-y-3">
            <p class="text-sm font-medium text-primary-light">{{ t('onboarding.roleQuestion') }}</p>

            <button
              type="button"
              class="w-full rounded-xl border px-4 py-4 text-left transition"
              :class="roleChoice === userRole.Student
                ? 'border-primary bg-primary/10 text-light'
                : 'border-primary/20 bg-dark text-light hover:border-primary/50'"
              @click="roleChoice = userRole.Student"
            >
              <p class="font-semibold">{{ t('onboarding.roleStudent') }}</p>
              <p class="mt-0.5 text-xs text-light/60">{{ t('onboarding.roleStudentDesc') }}</p>
            </button>

            <button
              type="button"
              class="w-full rounded-xl border px-4 py-4 text-left transition"
              :class="roleChoice === userRole.Coordinator
                ? 'border-primary bg-primary/10 text-light'
                : 'border-primary/20 bg-dark text-light hover:border-primary/50'"
              @click="roleChoice = userRole.Coordinator"
            >
              <p class="font-semibold">{{ t('onboarding.roleCoordinator') }}</p>
              <p class="mt-0.5 text-xs text-light/60">{{ t('onboarding.roleCoordinatorDesc') }}</p>
            </button>
          </div>

          <!-- Institution step -->
          <div v-if="currentStep === institutionStep" class="mt-6 space-y-3">
            <label class="block text-sm font-medium text-primary-light">
              {{ t('onboarding.selectInstitution') }}
            </label>

            <!-- Search -->
            <div class="relative">
              <svg class="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                v-model="institutionSearch"
                type="text"
                :placeholder="t('onboarding.searchInstitution')"
                class="w-full rounded-xl border border-primary/20 bg-dark py-2.5 pl-9 pr-4 text-sm text-light placeholder-slate-500 focus:border-primary focus:outline-none"
              />
            </div>

            <!-- List -->
            <template v-if="loadingInstitutions">
              <div class="space-y-2">
                <div v-for="i in 4" :key="i" class="h-14 animate-pulse rounded-xl bg-primary/10"></div>
              </div>
            </template>
            <template v-else>
              <div class="space-y-2 overflow-y-auto" style="max-height: 280px">
                <p v-if="filteredInstitutions.length === 0" class="py-6 text-center text-sm text-slate-500">
                  {{ t('onboarding.noInstitutions') }}
                </p>
                <button
                  v-for="inst in filteredInstitutions"
                  :key="inst.id"
                  type="button"
                  class="w-full rounded-xl border px-4 py-3 text-left transition"
                  :class="
                    selectedInstitutionId === inst.id
                      ? 'border-primary bg-primary/10 text-light'
                      : 'border-primary/20 bg-dark text-light hover:border-primary/50'
                  "
                  @click="selectedInstitutionId = inst.id"
                >
                  <div class="flex items-center justify-between gap-3">
                    <div class="min-w-0">
                      <p class="truncate text-sm font-medium">{{ localizedName(inst) }}</p>
                      <p v-if="inst.city || inst.erasmusCode" class="mt-0.5 truncate text-xs text-slate-400">
                        <template v-if="inst.city">{{ inst.city }}</template>
                        <template v-if="inst.city && inst.erasmusCode"> · </template>
                        <template v-if="inst.erasmusCode">{{ inst.erasmusCode }}</template>
                      </p>
                    </div>
                    <span v-if="inst.country" class="flex-shrink-0 text-xs text-slate-500">{{ inst.country }}</span>
                  </div>
                </button>
              </div>
            </template>
          </div>

          <!-- JMBAG step -->
          <div v-if="currentStep === jmbagStep" class="mt-6 space-y-4">
            <label class="block text-sm font-medium text-primary-light">
              {{ t('onboarding.jmbagLabel') }}
            </label>
            <input
              v-model="jmbag"
              type="text"
              inputmode="numeric"
              maxlength="10"
              :placeholder="t('onboarding.jmbagPlaceholder')"
              class="w-full rounded-xl border border-primary/20 bg-dark px-4 py-2.5 text-light focus:border-primary focus:outline-none"
            />
            <p class="text-xs text-light/60">{{ t('onboarding.jmbagHint') }}</p>
          </div>

          <!-- Navigation buttons -->
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
              v-if="currentStep < totalSteps"
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
              @click="finishOnboarding"
            >
              {{ isSubmitting ? t('common.loading') : t('onboarding.submit') }}
            </button>
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
