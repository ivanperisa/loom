<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { exchangeService } from '@/services/exchange.service'
import type { InstitutionResponse } from '@/types/institution.types'
import type { CreateExchangeRequest, ExchangeSemester } from '@/types/exchange.types'

const emit = defineEmits<{
  (e: 'submit', data: CreateExchangeRequest): void
  (e: 'close'): void
}>()

const { t } = useI18n()

const institutions = ref<InstitutionResponse[]>([])
const loadingInstitutions = ref(false)

const form = ref<CreateExchangeRequest>({
  foreignInstitutionId: '',
  academicYear: '',
  semester: 'Winter',
  durationMonths: undefined,
  mentor: undefined
})

const submitting = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  loadingInstitutions.value = true
  try {
    const res = await exchangeService.getForeignInstitutions()
    institutions.value = res.data
  } catch {
    // ignore
  } finally {
    loadingInstitutions.value = false
  }
})

async function handleSubmit() {
  if (!form.value.foreignInstitutionId || !form.value.academicYear) {
    error.value = t('errors.required')
    return
  }
  submitting.value = true
  error.value = null
  try {
    emit('submit', { ...form.value })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
    <div class="w-full max-w-lg rounded-xl bg-[#0E2A3D] p-6 shadow-xl">
      <div class="mb-4 flex items-center justify-between">
        <h2 class="text-xl font-semibold text-[#CAE4F7]">{{ t('exchange.create') }}</h2>
        <button @click="emit('close')" class="text-[#8AC4ED] hover:text-white transition">&times;</button>
      </div>

      <div v-if="error" class="mb-3 rounded-lg bg-red-900/40 px-3 py-2 text-sm text-red-300">{{ error }}</div>

      <form @submit.prevent="handleSubmit" class="space-y-4">
        <!-- Foreign Institution -->
        <div>
          <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.foreignInstitution') }}</label>
          <select
            v-model="form.foreignInstitutionId"
            required
            class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-2 focus:ring-[#2E7AB5]"
          >
            <option value="" disabled>{{ loadingInstitutions ? t('common.loading') : t('exchange.selectInstitution') }}</option>
            <option v-for="inst in institutions" :key="inst.id" :value="inst.id">
              {{ inst.nameEn }} ({{ inst.country }})
            </option>
          </select>
        </div>

        <!-- Academic Year -->
        <div>
          <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.academicYear') }}</label>
          <input
            v-model="form.academicYear"
            type="text"
            placeholder="2024/2025"
            required
            class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-2 focus:ring-[#2E7AB5]"
          />
        </div>

        <!-- Semester -->
        <div>
          <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.semester') }}</label>
          <select
            v-model="form.semester"
            class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-2 focus:ring-[#2E7AB5]"
          >
            <option value="Winter">{{ t('exchangeSemesters.Winter') }}</option>
            <option value="Summer">{{ t('exchangeSemesters.Summer') }}</option>
          </select>
        </div>

        <!-- Duration -->
        <div>
          <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.duration') }}</label>
          <input
            v-model.number="form.durationMonths"
            type="number"
            min="1"
            max="24"
            class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-2 focus:ring-[#2E7AB5]"
          />
        </div>

        <!-- Mentor -->
        <div>
          <label class="mb-1 block text-sm font-medium text-[#8AC4ED]">{{ t('exchange.mentor') }}</label>
          <input
            v-model="form.mentor"
            type="text"
            class="w-full rounded-lg bg-[#071C2C] px-3 py-2 text-[#CAE4F7] border border-[#1E4A6E] focus:outline-none focus:ring-2 focus:ring-[#2E7AB5]"
          />
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button type="button" @click="emit('close')" class="rounded-lg px-4 py-2 text-sm text-[#8AC4ED] hover:text-white transition">
            {{ t('settings.institutions.cancel') }}
          </button>
          <button
            type="submit"
            :disabled="submitting"
            class="rounded-lg bg-[#2E7AB5] px-4 py-2 text-sm font-medium text-white hover:bg-[#3A8FD0] transition disabled:opacity-50"
          >
            {{ t('exchange.create') }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
