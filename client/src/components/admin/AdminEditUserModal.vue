<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { adminService, type UserListResponse, type AdminUpdateUserRequest } from '@/services/admin.service'
import { userRole } from '@/utils/userRole'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import type { AuthMeResponse } from '@/types/auth.types'
import type { InstitutionResponse } from '@/types/institution.types'

const props = defineProps<{
  user: UserListResponse
  coordinators: AuthMeResponse[]
  institutions: InstitutionResponse[]
}>()

const emit = defineEmits<{
  close: []
  saved: [user: UserListResponse]
}>()

const { t } = useI18n()

const isStudent = computed(() => props.user.role === userRole.Student)
const isCoordinator = computed(() => props.user.role === userRole.Coordinator)

const form = ref({
  name: props.user.name,
  jmbag: props.user.jmbag ?? '',
  mentor: props.user.mentor ?? '',
  coordinatorId: props.user.coordinatorId as string | null,
  institutionId: props.user.institutionId as string | null,
})

const saving = ref(false)
const error = ref<string | null>(null)

const coordinatorOptions = computed(() => [
  { value: null, label: t('admin.users.noCoordinator') },
  ...props.coordinators.map((c) => ({ value: c.id, label: c.name })),
])

const institutionOptions = computed(() => [
  { value: null, label: t('admin.users.noInstitution') },
  ...props.institutions.map((i) => ({
    value: i.id,
    label: i.name,
    sublabel: i.city ?? undefined,
  })),
])

async function save() {
  saving.value = true
  error.value = null
  try {
    const payload: AdminUpdateUserRequest = {
      name: form.value.name.trim(),
      jmbag: form.value.jmbag.trim() || null,
      mentor: isStudent.value ? (form.value.mentor.trim() || null) : null,
      coordinatorId: isStudent.value ? (form.value.coordinatorId || null) : null,
      institutionId: form.value.institutionId || null,
    }
    const res = await adminService.updateUser(props.user.id, payload)
    emit('saved', res.data)
  } catch {
    error.value = t('admin.users.editUserError')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4"
      @mousedown.self="emit('close')"
    >
      <div class="w-full max-w-lg rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
        <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
          <div>
            <div class="flex items-center gap-2">
              <h3 class="font-semibold text-light">{{ t('admin.users.editUser') }}</h3>
              <span class="rounded-full border border-white/10 px-1.5 py-0.5 text-[10px] text-light/30">{{ t(`admin.users.role.${user.role}`) }}</span>
            </div>
            <p class="mt-0.5 text-xs text-light/40">{{ user.email }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-white" @click="emit('close')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <div class="space-y-4 px-6 py-5">
          <!-- Name -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.users.editUserName') }} *</label>
            <input
              v-model="form.name"
              type="text"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- JMBAG (student only) -->
          <div v-if="isStudent">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.users.editUserJmbag') }}</label>
            <input
              v-model="form.jmbag"
              type="text"
              maxlength="10"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            />
          </div>

          <!-- Institution -->
          <div>
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.users.editUserInstitution') }}</label>
            <SearchableSelect
              v-model="form.institutionId"
              :options="institutionOptions"
              :search-placeholder="t('admin.users.searchInstitution')"
              :no-results-label="t('admin.users.noInstitutionResults')"
            />
          </div>

          <!-- Coordinator -->
          <div v-if="isStudent">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.users.editUserCoordinator') }}</label>
            <SearchableSelect
              v-model="form.coordinatorId"
              :options="coordinatorOptions"
              :search-placeholder="t('admin.users.searchCoordinator')"
              :no-results-label="t('admin.users.noCoordinatorResults')"
            />
          </div>

          <!-- Mentor -->
          <div v-if="isStudent">
            <label class="mb-1.5 block text-sm text-light/70">{{ t('admin.users.editUserMentor') }}</label>
            <input
              v-model="form.mentor"
              type="text"
              class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            />
          </div>

          <p v-if="error" class="text-xs text-red-400">{{ error }}</p>
        </div>

        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button
            type="button"
            class="rounded-lg border border-white/10 px-4 py-2 text-sm text-light/60 transition hover:text-light"
            @click="emit('close')"
          >{{ t('admin.users.editUserCancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="saving || !form.name.trim()"
            @click="save"
          >{{ saving ? t('common.loading') : t('admin.users.editUserSave') }}</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>
