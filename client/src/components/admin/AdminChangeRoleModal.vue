<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { adminService, type UserListResponse } from '@/services/admin.service'
import { userRole } from '@/utils/userRole'
import BaseModal from '@/components/common/BaseModal.vue'
import UserAvatar from '@/components/common/UserAvatar.vue'
import { ROLE_CHIP_CLASS } from '@/utils/roleColors'

const props = defineProps<{ user: UserListResponse }>()

const emit = defineEmits<{
  close: []
  saved: [user: UserListResponse]
}>()

const { t } = useI18n()

const ROLES = [userRole.Student, userRole.Coordinator, userRole.Admin]

const selected = ref<string>(props.user.role)
const saving = ref(false)
const error = ref<string | null>(null)

const changed = computed(() => selected.value !== props.user.role)
const demotingToStudent = computed(
  () => selected.value === userRole.Student && props.user.role !== userRole.Student,
)

async function save() {
  saving.value = true
  error.value = null
  try {
    const res = await adminService.setUserRole(props.user.id, selected.value)
    emit('saved', res.data)
  } catch {
    error.value = t('admin.users.changeRoleError')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <BaseModal max-width="max-w-md" labelled-by="admin-change-role-title" @close="emit('close')">
    <div class="rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
      <div class="flex items-start justify-between border-b border-primary/20 px-6 py-4">
        <div class="flex min-w-0 items-center gap-3">
          <UserAvatar :name="user.name" :role="user.role" size="md" />
          <div class="min-w-0">
            <h3 id="admin-change-role-title" class="truncate font-semibold text-light">{{ user.name }}</h3>
            <p class="truncate text-xs text-light/40">{{ user.email || '—' }}</p>
          </div>
        </div>
        <button :aria-label="t('common.close')" type="button" class="ml-3 text-light/40 transition hover:text-light" @click="emit('close')">
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>

      <div class="px-6 py-5">
        <p class="mb-3 text-sm text-light/60">{{ t('admin.users.changeRoleTitle') }}</p>

        <div class="space-y-2" role="radiogroup">
          <button
            v-for="r in ROLES"
            :key="r"
            type="button"
            role="radio"
            :aria-checked="selected === r"
            class="flex w-full items-start gap-3 rounded-xl border px-4 py-3 text-left transition"
            :class="selected === r
              ? 'border-primary bg-primary/10'
              : 'border-hairline bg-dark hover:border-primary/40'"
            @click="selected = r"
          >
            <span
              class="mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded-full border"
              :class="selected === r ? 'border-primary' : 'border-light/30'"
            >
              <span v-if="selected === r" class="h-2 w-2 rounded-full bg-primary"></span>
            </span>
            <span class="min-w-0 flex-1">
              <span class="flex items-center gap-2">
                <span
                  class="rounded-full border px-2.5 py-0.5 text-[11px] font-medium"
                  :class="ROLE_CHIP_CLASS[r]"
                >{{ t(`admin.users.role.${r}`) }}</span>
                <span v-if="user.role === r" class="text-[11px] text-light/35">{{ t('admin.users.currentRole') }}</span>
              </span>
              <span class="mt-1 block text-xs text-light/50">{{ t(`admin.users.roleDescription.${r}`) }}</span>
            </span>
          </button>
        </div>

        <p v-if="demotingToStudent" class="mt-4 rounded-xl border border-warning-text/35 bg-warning-fill px-3 py-2 text-xs text-warning-text">
          {{ t('admin.users.changeRoleWarning') }}
        </p>

        <p v-if="error" class="mt-3 text-xs text-danger-text">{{ error }}</p>
      </div>

      <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
        <button
          type="button"
          class="rounded-lg border border-hairline px-4 py-2 text-sm text-light/60 transition hover:text-light"
          @click="emit('close')"
        >{{ t('admin.users.editUserCancel') }}</button>
        <button
          type="button"
          class="rounded-lg bg-primary-strong px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
          :disabled="saving || !changed"
          @click="save"
        >{{ saving ? t('common.loading') : t('admin.users.changeRoleSave') }}</button>
      </div>
    </div>
  </BaseModal>
</template>
