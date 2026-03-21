<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/stores/auth.store'
import { institutionService } from '@/services/institution.service'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'
import type { InstitutionResponse } from '@/types/institution.types'

const router = useRouter()
const authStore = useAuthStore()
const { t, locale } = useI18n()

const isCoordinatorOrAdmin = computed(() => {
  const role = authStore.user?.role
  return role === 'Coordinator' || role === 'Admin'
})

const steps = computed(() => {
  const base = [{ id: 1, key: 'onboarding.steps.institution' }]
  if (!isCoordinatorOrAdmin.value) {
    base.push({ id: 2, key: 'onboarding.steps.jmbag' })
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

function localizedName(item: { name: string; nameEn?: string | null }): string {
  return locale.value === 'en' && item.nameEn ? item.nameEn : item.name
}

const selectedInstitutionName = computed(() => {
  const inst = institutions.value.find(i => i.id === selectedInstitutionId.value)
  return inst ? localizedName(inst) : null
})

const isJmbagValid = computed(() => /^\d{10}$/.test(jmbag.value))

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
  if (currentStep.value === 1) {
    if (!selectedInstitutionId.value) {
      errorMessage.value = t('onboarding.errors.institutionRequired')
      return
    }
    currentStep.value = 2
  }
}

function goBack() {
  errorMessage.value = null
  if (currentStep.value > 1) {
    currentStep.value -= 1
  }
}

async function finishOnboarding() {
  errorMessage.value = null

  if (!selectedInstitutionId.value) {
    errorMessage.value = t('onboarding.errors.institutionRequired')
    return
  }

  if (!isCoordinatorOrAdmin.value) {
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
      jmbag: isCoordinatorOrAdmin.value ? undefined : jmbag.value.trim()
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
  <main class="min-h-screen bg-[#071C2C] text-[#CAE4F7]">
    <header class="sticky top-0 z-40 w-full border-b border-[#218CD9] bg-[#071C2C]">
      <div class="page-container flex h-16 items-center justify-between !py-0">
        <span class="text-lg font-bold text-white">{{ t('common.appName') }}</span>
        <div class="flex items-center gap-3">
          <LanguageSwitcher variant="dark" />
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

    <section class="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md items-center justify-center px-4 py-10">
      <div class="w-full">
        <!-- Step indicator -->
        <div class="mb-8 flex items-center justify-center gap-3">
          <template v-for="(step, index) in steps" :key="step.id">
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
              <template v-if="index + 1 < currentStep">
                <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>
              </template>
              <template v-else>{{ index + 1 }}</template>
            </div>
            <span class="text-sm" :class="index + 1 <= currentStep ? 'text-[#CAE4F7]' : 'text-slate-400'">
              {{ t(step.key) }}
            </span>
            <div v-if="index < steps.length - 1" class="h-px w-10 bg-slate-600"></div>
          </template>
        </div>

        <!-- Card -->
        <div class="rounded-2xl border border-[#1E4A6E] bg-[#0A2235] p-6 shadow-xl sm:p-8">
          <!-- Logo placeholder -->
          <div class="mb-6 flex justify-center">
            <div class="flex h-14 w-14 items-center justify-center rounded-full bg-[#218CD9]">
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

          <!-- Step 1: Institution -->
          <div v-if="currentStep === 1" class="mt-6 space-y-4">
            <label class="block text-sm font-medium text-[#8AC4ED]">
              {{ t('onboarding.selectInstitution') }}
            </label>

            <template v-if="loadingInstitutions">
              <div class="h-11 animate-pulse rounded-xl bg-[#1E4A6E]"></div>
            </template>
            <template v-else>
              <select
                v-model="selectedInstitutionId"
                class="w-full rounded-xl border border-[#1E4A6E] bg-[#071C2C] px-4 py-2.5 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
              >
                <option :value="null" disabled>{{ t('onboarding.selectInstitutionPlaceholder') }}</option>
                <option
                  v-for="inst in institutions"
                  :key="inst.id"
                  :value="inst.id"
                >
                  {{ localizedName(inst) }}
                  <template v-if="inst.city"> — {{ inst.city }}</template>
                </option>
              </select>
            </template>
          </div>

          <!-- Step 2: JMBAG -->
          <div v-if="currentStep === 2" class="mt-6 space-y-4">
            <label class="block text-sm font-medium text-[#8AC4ED]">
              {{ t('onboarding.jmbagLabel') }}
            </label>
            <input
              v-model="jmbag"
              type="text"
              inputmode="numeric"
              maxlength="10"
              :placeholder="t('onboarding.jmbagPlaceholder')"
              class="w-full rounded-xl border border-[#1E4A6E] bg-[#071C2C] px-4 py-2.5 text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            />
            <p class="text-xs text-[#5A8AAD]">{{ t('onboarding.jmbagHint') }}</p>
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
              {{ isSubmitting ? t('common.loading') : t('onboarding.submit') }}
            </button>
          </div>
        </div>
      </div>
    </section>
  </main>
</template>
