<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import AppHeader from '@/components/AppHeader.vue'
import { useAuthStore } from '@/stores/auth.store'
import { institutionService } from '@/services/institution.service'
import type { InstitutionResponse } from '@/types/institution.types'
import type { AuthMeResponse } from '@/types/auth.types'

const { t } = useI18n()
const authStore = useAuthStore()

const institutions = ref<InstitutionResponse[]>([])
const coordinators = ref<AuthMeResponse[]>([])
const name = ref('')
const jmbag = ref('')
const institutionId = ref('')
const mentor = ref('')
const coordinatorId = ref<string | null>(null)
const saving = ref(false)
const success = ref(false)
const errorMsg = ref<string | null>(null)

const isStudent = computed(() => authStore.role === 'Student')

onMounted(async () => {
  const [instRes, coordRes] = await Promise.all([
    institutionService.getHomeInstitutions(),
    institutionService.getCoordinators(),
  ])
  institutions.value = instRes.data
  coordinators.value = coordRes.data
  resetForm()
})

function resetForm() {
  name.value = authStore.user?.name ?? ''
  jmbag.value = authStore.user?.jmbag ?? ''
  institutionId.value = authStore.user?.institutionId ?? ''
  mentor.value = authStore.user?.mentor ?? ''
  coordinatorId.value = authStore.user?.coordinatorId ?? null
}

async function save() {
  saving.value = true
  success.value = false
  errorMsg.value = null
  try {
    await authStore.updateProfile({
      name: name.value.trim(),
      jmbag: jmbag.value.trim() || null,
      institutionId: institutionId.value,
      mentor: mentor.value.trim() || null,
      coordinatorId: coordinatorId.value || null,
    })
    success.value = true
    setTimeout(() => (success.value = false), 3000)
  } catch {
    errorMsg.value = t('settings.saveError')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <main class="min-h-screen bg-[#071C2C]">
    <AppHeader />

    <section class="page-container max-w-2xl">
      <h1 class="text-2xl font-bold text-[#CAE4F7]">{{ t('settings.title') }}</h1>

      <!-- Profile card -->
      <div class="mt-6 rounded-xl border border-[#1E4A6E] bg-[#0A2235] p-6">
        <h2 class="mb-4 text-lg font-semibold text-[#CAE4F7]">{{ t('settings.profile.title') }}</h2>

        <div class="space-y-4">
          <!-- Email (read-only) -->
          <div>
            <label class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.email') }}</label>
            <p class="mt-1 text-sm font-medium text-[#CAE4F7]/60">{{ authStore.email }}</p>
          </div>

          <!-- Role (read-only) -->
          <div>
            <label class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.role') }}</label>
            <p class="mt-1 text-sm font-medium text-[#CAE4F7]/60">{{ authStore.role }}</p>
          </div>

          <!-- Name -->
          <div>
            <label for="name" class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.name') }}</label>
            <input
              id="name"
              v-model="name"
              type="text"
              class="mt-1 w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            />
          </div>

          <!-- JMBAG (student only) -->
          <div v-if="isStudent">
            <label for="jmbag" class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.jmbag') }}</label>
            <input
              id="jmbag"
              v-model="jmbag"
              type="text"
              maxlength="10"
              :placeholder="t('onboarding.jmbagPlaceholder')"
              class="mt-1 w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] placeholder-[#5A8AAD] focus:border-[#218CD9] focus:outline-none"
            />
          </div>

          <!-- Institution -->
          <div>
            <label for="institution" class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.institution') }}</label>
            <select
              id="institution"
              v-model="institutionId"
              class="mt-1 w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            >
              <option v-for="inst in institutions" :key="inst.id" :value="inst.id">
                {{ inst.name }}
              </option>
            </select>
          </div>

          <!-- Mentor (student only) -->
          <div v-if="isStudent">
            <label for="mentor" class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.mentor') }}</label>
            <input
              id="mentor"
              v-model="mentor"
              type="text"
              :placeholder="t('createExchange.mentorPlaceholder')"
              class="mt-1 w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] placeholder-[#5A8AAD] focus:border-[#218CD9] focus:outline-none"
            />
          </div>

          <!-- Coordinator (student only) -->
          <div v-if="isStudent">
            <label for="coordinator" class="block text-sm text-[#5A8AAD]">{{ t('settings.profile.coordinator') }}</label>
            <select
              id="coordinator"
              v-model="coordinatorId"
              class="mt-1 w-full rounded-lg border border-[#1E4A6E] bg-[#071C2C] px-3 py-2 text-sm text-[#CAE4F7] focus:border-[#218CD9] focus:outline-none"
            >
              <option :value="null">{{ t('exchange.noCoordinator') }}</option>
              <option v-for="c in coordinators" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </div>
        </div>

        <!-- Actions -->
        <div class="mt-6 flex items-center gap-3">
          <button
            type="button"
            class="rounded-lg bg-[#218CD9] px-5 py-2 text-sm font-semibold text-white transition hover:bg-[#8AC4ED] hover:text-[#071C2C] disabled:opacity-50"
            :disabled="saving"
            @click="save"
          >
            {{ saving ? t('common.loading') : t('settings.save') }}
          </button>
          <button
            type="button"
            class="rounded-lg border border-[#1E4A6E] px-4 py-2 text-sm text-[#5A8AAD] transition hover:text-[#CAE4F7]"
            @click="resetForm"
          >
            {{ t('common.cancel') }}
          </button>
        </div>

        <!-- Success -->
        <p v-if="success" class="mt-3 text-sm text-green-400">{{ t('settings.saveSuccess') }}</p>
        <!-- Error -->
        <p v-if="errorMsg" class="mt-3 text-sm text-red-400">{{ errorMsg }}</p>
      </div>
    </section>
  </main>
</template>
