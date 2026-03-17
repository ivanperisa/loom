<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { AxiosError } from 'axios'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import InstitutionForm from '@/components/InstitutionForm.vue'
import { useAuthStore } from '@/stores/auth.store'
import type { ProblemDetails } from '@/types/api.types'
import type { LocalInstitutionEntry } from '@/types/onboarding.types'
import { toInstitutionEntryDto } from '@/types/onboarding.types'

const authStore = useAuthStore()
const { t, locale } = useI18n()

const removeErrorMessage = ref('')
const formErrorMessage = ref('')
const addSuccessMessage = ref('')
const updateSuccessMessage = ref('')
const showAddForm = ref(false)
const editingUserInstitutionId = ref<string | null>(null)
const isLoading = ref(true)

const editingJmbag = ref(false)
const jmbagInput = ref(authStore.jmbag ?? '')
const jmbagSuccess = ref('')
const jmbagError = ref('')

async function saveJmbag() {
  jmbagSuccess.value = ''
  jmbagError.value = ''
  try {
    await authStore.updateJmbag(jmbagInput.value.trim() || null)
    jmbagSuccess.value = t('settings.profile.jmbagSaved')
    editingJmbag.value = false
  } catch (error) {
    const problem = (error as AxiosError<ProblemDetails>).response?.data
    jmbagError.value = problem?.detail ?? t('errors.unexpected')
  }
}

function localizedName(name: string, nameEn?: string) {
  return locale.value === 'en' ? nameEn || name : name || nameEn || ''
}

const initials = computed(() => {
  const source = (authStore.name ?? t('common.user')).trim()
  const letters = source
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')

  return letters || 'U'
})

watch(
  () => authStore.institutions,
  () => {
    if (editingUserInstitutionId.value) {
      const exists = authStore.institutions.some((x) => x.userInstitutionId === editingUserInstitutionId.value)
      if (!exists) {
        editingUserInstitutionId.value = null
      }
    }
  },
  { deep: true }
)

function buildPrefill(userInstitutionId: string): LocalInstitutionEntry | undefined {
  const item = authStore.institutions.find((x) => x.userInstitutionId === userInstitutionId)
  if (!item) {
    return undefined
  }

  if (item.studyProfile?.id && item.studyProgram?.id) {
    return {
      institutionName: localizedName(item.institution.name, item.institution.nameEn),
      programName: localizedName(item.studyProgram.name, item.studyProgram.nameEn),
      profileName: localizedName(item.studyProfile.name, item.studyProfile.nameEn),
      existingInstitutionId: item.institution.id,
      existingProgramId: item.studyProgram.id,
      existingStudyProfileId: item.studyProfile.id
    }
  }

  return {
    institutionName: localizedName(item.institution.name, item.institution.nameEn),
    existingInstitutionId: item.institution.id
  }
}

async function addInstitution(entry: LocalInstitutionEntry) {
  formErrorMessage.value = ''
  addSuccessMessage.value = ''

  try {
    await authStore.addInstitution(toInstitutionEntryDto(entry))
    addSuccessMessage.value = t('settings.institutions.addSuccess')
    showAddForm.value = false
  } catch (error) {
    const problem = (error as AxiosError<ProblemDetails>).response?.data
    formErrorMessage.value = problem?.detail ?? t('errors.unexpected')
  }
}

async function updateInstitution(entry: LocalInstitutionEntry) {
  if (!editingUserInstitutionId.value) {
    return
  }

  formErrorMessage.value = ''
  updateSuccessMessage.value = ''

  try {
    await authStore.updateInstitution(editingUserInstitutionId.value, toInstitutionEntryDto(entry))
    updateSuccessMessage.value = t('settings.institutions.updateSuccess')
    editingUserInstitutionId.value = null
  } catch (error) {
    const problem = (error as AxiosError<ProblemDetails>).response?.data
    formErrorMessage.value =
      problem?.extensions?.code === 'HAS_ACTIVE_EXCHANGES'
        ? t('settings.institutions.editDisabledTooltip')
        : (problem?.detail ?? t('errors.unexpected'))
  }
}

async function removeInstitution(userInstitutionId: string) {
  removeErrorMessage.value = ''

  if (!window.confirm(t('settings.institutions.removeConfirm'))) {
    return
  }

  try {
    await authStore.removeInstitution(userInstitutionId)
  } catch (error) {
    const problem = (error as AxiosError<ProblemDetails>).response?.data
    removeErrorMessage.value =
      problem?.extensions?.code === 'HAS_ACTIVE_EXCHANGES'
        ? t('settings.institutions.removeError')
        : (problem?.detail ?? t('errors.unexpected'))
  }
}

onMounted(async () => {
  isLoading.value = true
  await authStore.init(true)
  isLoading.value = false
})
</script>

<template>
  <main class="settings-page relative min-h-screen overflow-hidden text-[#CAE4F7]">
    <AppHeader />

    <section class="relative z-10 mx-auto w-full max-w-[800px] px-4 py-12 sm:px-6">
      <h1 class="mb-6 text-3xl font-bold text-[#CAE4F7]">{{ t('settings.title') }}</h1>

      <article class="profile-card mb-8 rounded-[20px] p-6 sm:p-7">
        <div class="flex flex-col items-start gap-6 sm:flex-row sm:items-center">
          <div class="avatar-shell flex h-[72px] w-[72px] items-center justify-center rounded-full text-2xl font-bold text-white">
            {{ initials }}
          </div>

          <div class="grid w-full gap-3">
            <div class="flex items-center gap-2 text-sm text-[#CAE4F7]">
              <svg viewBox="0 0 24 24" class="h-[18px] w-[18px] text-[#8AC4ED]" fill="none" aria-hidden="true">
                <path d="M4 6h16v12H4z" stroke="currentColor" stroke-width="1.8" />
                <path d="m4 7 8 6 8-6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
              <span class="font-medium text-[#8AC4ED]">{{ t('settings.profile.email') }}:</span>
              <span class="text-white">{{ authStore.email ?? t('common.na') }}</span>
            </div>

            <div class="flex items-center gap-2 text-sm text-[#CAE4F7]">
              <svg viewBox="0 0 24 24" class="h-[18px] w-[18px] text-[#8AC4ED]" fill="none" aria-hidden="true">
                <circle cx="12" cy="8" r="4" stroke="currentColor" stroke-width="1.8" />
                <path d="M5 20a7 7 0 0 1 14 0" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
              </svg>
              <span class="font-medium text-[#8AC4ED]">{{ t('settings.profile.name') }}:</span>
              <span class="text-white">{{ authStore.name ?? t('common.na') }}</span>
            </div>

            <div class="flex items-center gap-2 text-sm text-[#CAE4F7]">
              <svg viewBox="0 0 24 24" class="h-[18px] w-[18px] text-[#8AC4ED]" fill="none" aria-hidden="true">
                <path d="m3 9 9-4 9 4-9 4-9-4Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
                <path d="M7 12.8v2.8c0 1.4 2.2 2.5 5 2.5s5-1.1 5-2.5v-2.8" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
              </svg>
              <span class="font-medium text-[#8AC4ED]">{{ t('settings.profile.role') }}:</span>
              <span
                class="role-badge"
                :class="authStore.role === 'Coordinator' ? 'role-badge-coordinator' : 'role-badge-student'"
              >
                {{ authStore.role === 'Coordinator' ? t('onboarding.role.coordinator') : t('onboarding.role.student') }}
              </span>
            </div>

            <div v-if="authStore.role === 'Student'" class="flex items-center gap-2 text-sm text-[#CAE4F7]">
              <svg viewBox="0 0 24 24" class="h-[18px] w-[18px] text-[#8AC4ED]" fill="none" aria-hidden="true">
                <rect x="3" y="5" width="18" height="14" rx="2" stroke="currentColor" stroke-width="1.8" />
                <path d="M7 10h4M7 14h6" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
              </svg>
              <span class="font-medium text-[#8AC4ED]">{{ t('settings.profile.jmbag') }}:</span>
              <template v-if="!editingJmbag">
                <span class="text-white">{{ authStore.jmbag ?? t('common.na') }}</span>
                <button type="button" class="text-xs text-[#8AC4ED] hover:text-white transition ml-2" @click="editingJmbag = true; jmbagInput = authStore.jmbag ?? ''">
                  {{ t('settings.institutions.edit') }}
                </button>
              </template>
              <template v-else>
                <input
                  v-model="jmbagInput"
                  type="text"
                  maxlength="10"
                  class="rounded-lg border border-[#1E4A6E] bg-[#0A2235] px-3 py-1 text-sm text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
                />
                <button type="button" class="btn-edit" @click="saveJmbag">{{ t('settings.institutions.save') }}</button>
                <button type="button" class="text-xs text-[#8AC4ED] hover:text-white transition" @click="editingJmbag = false">{{ t('settings.institutions.cancel') }}</button>
              </template>
            </div>
            <p v-if="jmbagSuccess" class="mt-1 text-xs text-emerald-300">{{ jmbagSuccess }}</p>
            <p v-if="jmbagError" class="mt-1 text-xs text-red-300">{{ jmbagError }}</p>
          </div>
        </div>
      </article>

      <section class="rounded-[20px] border border-[rgba(202,228,247,0.15)] bg-[rgba(255,255,255,0.02)] p-5 sm:p-6">
        <div class="border-b border-[rgba(202,228,247,0.1)] pb-3">
          <h2 class="text-[1.2rem] font-semibold text-white">{{ t('settings.institutions.title') }}</h2>
        </div>

        <p v-if="removeErrorMessage" class="mt-4 rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200">
          {{ removeErrorMessage }}
        </p>
        <p v-if="formErrorMessage" class="mt-3 rounded-lg border border-red-400/50 bg-red-500/10 px-4 py-2 text-sm text-red-200">
          {{ formErrorMessage }}
        </p>
        <p v-if="addSuccessMessage" class="mt-3 rounded-lg border border-emerald-400/40 bg-emerald-500/10 px-4 py-2 text-sm text-emerald-200">
          {{ addSuccessMessage }}
        </p>
        <p v-if="updateSuccessMessage" class="mt-3 rounded-lg border border-emerald-400/40 bg-emerald-500/10 px-4 py-2 text-sm text-emerald-200">
          {{ updateSuccessMessage }}
        </p>

        <div v-if="isLoading" class="mt-5 grid gap-3">
          <div class="skeleton h-[108px] rounded-[14px] border border-[rgba(202,228,247,0.08)]"></div>
          <div class="skeleton h-[108px] rounded-[14px] border border-[rgba(202,228,247,0.08)]"></div>
        </div>

        <div v-else-if="authStore.institutions.length === 0" class="mt-8 flex flex-col items-center justify-center gap-3 py-8 text-center">
          <svg viewBox="0 0 24 24" class="empty-pulse h-12 w-12 text-[#218CD9]/40" fill="none" aria-hidden="true">
            <path d="M3 21h18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
            <path d="M5 21V9l7-4 7 4v12" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
            <path d="M9 12h2v2H9zm4 0h2v2h-2zm-4 4h2v2H9zm4 0h2v2h-2z" fill="currentColor" />
          </svg>
          <p class="text-sm text-[#8AC4ED]">{{ t('settings.institutions.empty') }}</p>
        </div>

        <TransitionGroup v-else name="institution-list" tag="div" class="mt-5 grid gap-3">
          <article
            v-for="item in authStore.institutions"
            :key="item.userInstitutionId"
            class="institution-card rounded-[14px] p-[18px] sm:p-[20px]"
          >
            <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
              <div>
                <p class="text-base font-semibold text-white">
                  {{ localizedName(item.institution.name, item.institution.nameEn) }}
                </p>

                <p class="mt-1 flex items-center gap-2 text-[0.85rem] text-[rgba(138,196,237,0.72)]">
                  <svg viewBox="0 0 24 24" class="h-4 w-4 text-[#8AC4ED]" fill="none" aria-hidden="true">
                    <path d="M12 21s7-6.5 7-11a7 7 0 1 0-14 0c0 4.5 7 11 7 11Z" stroke="currentColor" stroke-width="1.8" />
                    <circle cx="12" cy="10" r="2.5" stroke="currentColor" stroke-width="1.8" />
                  </svg>
                  <span>{{ item.institution.city ? `${item.institution.city}, ` : '' }}{{ item.institution.country }}</span>
                </p>

                <p v-if="item.studyProgram" class="mt-2 flex items-center gap-2 text-[0.85rem] text-[#CAE4F7]">
                  <svg viewBox="0 0 24 24" class="h-4 w-4 text-[#8AC4ED]" fill="none" aria-hidden="true">
                    <path d="M5 4h12a2 2 0 0 1 2 2v12H7a2 2 0 0 0-2 2V4Z" stroke="currentColor" stroke-width="1.8" />
                    <path d="M7 20V6h10" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
                  </svg>
                  <span>{{ t('settings.institutions.program') }}: {{ localizedName(item.studyProgram.name, item.studyProgram.nameEn) }}</span>
                </p>

                <p v-if="item.studyProfile" class="mt-1 flex items-center gap-2 text-[0.85rem] text-[#CAE4F7]">
                  <svg viewBox="0 0 24 24" class="h-4 w-4 text-[#8AC4ED]" fill="none" aria-hidden="true">
                    <path d="M4 11 12 4l8 7-8 9-8-9Z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round" />
                  </svg>
                  <span>{{ t('settings.institutions.profile') }}: {{ localizedName(item.studyProfile.name, item.studyProfile.nameEn) }}</span>
                </p>
              </div>

              <div class="flex flex-wrap items-center gap-2 sm:flex-col sm:items-end">
                <button
                  type="button"
                  class="btn-edit"
                  :disabled="item.hasActiveExchanges"
                  :title="item.hasActiveExchanges ? t('settings.institutions.editDisabledTooltip') : ''"
                  @click="editingUserInstitutionId = item.userInstitutionId"
                >
                  {{ t('settings.institutions.edit') }}
                </button>
                <button type="button" class="btn-remove" @click="removeInstitution(item.userInstitutionId)">
                  {{ t('settings.institutions.remove') }}
                </button>
              </div>
            </div>

            <Transition name="expand-down">
              <InstitutionForm
                v-if="editingUserInstitutionId === item.userInstitutionId"
                class="mt-4"
                :role="authStore.role === 'Coordinator' ? 'Coordinator' : 'Student'"
                :prefill="buildPrefill(item.userInstitutionId)"
                :submit-label="t('settings.institutions.save')"
                @submit="updateInstitution"
                @cancel="editingUserInstitutionId = null"
              />
            </Transition>
          </article>
        </TransitionGroup>

        <div class="mt-6">
          <button
            v-if="!showAddForm"
            type="button"
            class="add-institution-btn"
            @click="showAddForm = true"
          >
            <svg viewBox="0 0 24 24" class="h-4 w-4" fill="none" aria-hidden="true">
              <path d="M12 5v14M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
            <span>{{ t('settings.institutions.addButton') }}</span>
          </button>

          <Transition name="expand-down">
            <InstitutionForm
              v-if="showAddForm"
              :role="authStore.role === 'Coordinator' ? 'Coordinator' : 'Student'"
              :submit-label="t('settings.institutions.addTitle')"
              @submit="addInstitution"
              @cancel="showAddForm = false"
            />
          </Transition>
        </div>
      </section>
    </section>
  </main>
</template>

<style scoped>
.settings-page {
  background: radial-gradient(circle at 50% 35%, #0d2d44 0%, #071c2c 65%);
}

.settings-page::before {
  content: '';
  pointer-events: none;
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='180' height='180' viewBox='0 0 180 180'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='180' height='180' filter='url(%23n)' opacity='0.12'/%3E%3C/svg%3E");
  opacity: 0.14;
  mix-blend-mode: soft-light;
}

.profile-card {
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(202, 228, 247, 0.15);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
}

.avatar-shell {
  background: linear-gradient(45deg, #218cd9, #8ac4ed);
  box-shadow: 0 0 0 3px rgba(33, 140, 217, 0.3), 0 4px 16px rgba(33, 140, 217, 0.2);
}

.role-badge {
  border-radius: 999px;
  padding: 4px 14px;
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.role-badge-student {
  background: rgba(33, 140, 217, 0.15);
  border: 1px solid #218cd9;
  color: #8ac4ed;
}

.role-badge-coordinator {
  background: rgba(138, 196, 237, 0.15);
  border: 1px solid #8ac4ed;
  color: #cae4f7;
}

.institution-card {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(202, 228, 247, 0.1);
  transition: border-color 0.2s ease, background 0.2s ease;
}

.institution-card:hover {
  border-color: rgba(33, 140, 217, 0.4);
  background: rgba(33, 140, 217, 0.05);
}

.btn-edit,
.btn-remove {
  border-radius: 8px;
  padding: 6px 14px;
  font-size: 0.85rem;
  transition: all 0.2s ease;
}

.btn-edit {
  border: 1px solid rgba(33, 140, 217, 0.4);
  color: #8ac4ed;
  background: transparent;
}

.btn-edit:hover:enabled {
  border-color: #218cd9;
  color: #cae4f7;
  background: rgba(33, 140, 217, 0.1);
}

.btn-edit:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.btn-remove {
  border: 1px solid rgba(239, 68, 68, 0.3);
  color: rgba(239, 68, 68, 0.8);
  background: transparent;
}

.btn-remove:hover {
  border-color: #ef4444;
  color: #ef4444;
  background: rgba(239, 68, 68, 0.08);
}

.add-institution-btn {
  display: inline-flex;
  width: 100%;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border-radius: 14px;
  border: 2px dashed rgba(33, 140, 217, 0.3);
  background: transparent;
  padding: 14px;
  color: #218cd9;
  font-size: 0.9rem;
  transition: all 0.2s ease;
}

.add-institution-btn:hover {
  border-color: #218cd9;
  background: rgba(33, 140, 217, 0.05);
}

.skeleton {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.03) 25%,
    rgba(255, 255, 255, 0.07) 50%,
    rgba(255, 255, 255, 0.03) 75%
  );
  background-size: 800px 100%;
  animation: shimmer 1.5s infinite linear;
}

.institution-list-enter-active {
  animation: slideInUp 0.3s ease-out;
}

.institution-list-leave-active {
  animation: slideOutLeft 0.3s ease-in;
  overflow: hidden;
}

.institution-list-move {
  transition: transform 0.3s ease;
}

.expand-down-enter-active {
  animation: expandDown 0.2s ease-out;
}

.expand-down-leave-active {
  animation: expandDown 0.2s ease-out reverse;
}

.empty-pulse {
  animation: pulseGlow 2s ease-in-out infinite;
}

@keyframes slideInUp {
  from {
    opacity: 0;
    transform: translateY(16px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes slideOutLeft {
  from {
    opacity: 1;
    transform: translateX(0);
    max-height: 120px;
  }

  to {
    opacity: 0;
    transform: translateX(-20px);
    max-height: 0;
    padding-top: 0;
    padding-bottom: 0;
    margin-top: 0;
    margin-bottom: 0;
  }
}

@keyframes expandDown {
  from {
    opacity: 0;
    transform: scaleY(0.95);
    transform-origin: top;
  }

  to {
    opacity: 1;
    transform: scaleY(1);
  }
}

@keyframes shimmer {
  0% {
    background-position: -400px 0;
  }

  100% {
    background-position: 400px 0;
  }
}

@keyframes pulseGlow {
  0% {
    opacity: 0.4;
  }

  50% {
    opacity: 0.7;
  }

  100% {
    opacity: 0.4;
  }
}
</style>
