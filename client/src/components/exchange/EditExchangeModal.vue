<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useCoordinatorsQuery } from '@/queries/catalog.queries'
import { useExchangeContext } from '@/composables/useExchangeContext'
import { useExchangeMutations } from '@/queries/exchange.queries'
import { useExchangePeriod } from '@/composables/useExchangePeriod'
import StudySemesterPicker from '@/components/exchange/period/StudySemesterPicker.vue'
import SemesterTypePicker from '@/components/exchange/period/SemesterTypePicker.vue'
import { describeApiError } from '@/utils/apiError'
import { useNotification } from '@/composables/useNotification'
import type { CoordinatorOption } from '@/types/coordinator.types'
import type { ExchangeResponse } from '@/types/exchange.types'
import SearchableSelect from '@/components/common/SearchableSelect.vue'
import BaseModal from '@/components/common/BaseModal.vue'

const props = defineProps<{
  exchange: ExchangeResponse
  laMappedSemesters: number[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'saved'): void
}>()

const { t } = useI18n()
const { guest, isApproved } = useExchangeContext()
const exchangeMutations = useExchangeMutations(() => props.exchange.guid)
const { notifyError } = useNotification()

const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

// Prefill from the exchange being edited
const period = useExchangePeriod({
  academicYear: props.exchange.academicYear,
  semesterType: props.exchange.semesterType,
  studySemesters: props.exchange.studySemesters,
  lockedSemesters: () => props.laMappedSemesters,
})
const { academicYear, semesterType, studySemesters, academicYearSelectOptions, hasBlockedSemesterType } = period
const selectedCoordinatorId = ref<number | null>(props.exchange.coordinatorId)
const mentorInput = ref(props.exchange.mentor ?? '')
const ewpLinkInput = ref(props.exchange.ewpLink ?? '')

const coordinatorsQuery = useCoordinatorsQuery(() => !guest.value)
const coordinators = computed<CoordinatorOption[]>(() => coordinatorsQuery.data.value ?? [])
const coordinatorOptions = computed(() => [
  { value: null, label: t('exchange.noCoordinator') },
  ...coordinators.value.map((c) => ({ value: c.id, label: c.name })),
])


async function submit() {
  const missing = period.validate()
  errorMessage.value = missing ? t(missing) : null
  if (missing) return

  isSubmitting.value = true
  try {
    await exchangeMutations.update.mutateAsync({
      academicYear: academicYear.value.trim(),
      semesterType: semesterType.value,
      studySemesters: studySemesters.value,
      coordinatorId: selectedCoordinatorId.value,
      mentor: mentorInput.value.trim() || null,
      ewpLink: ewpLinkInput.value.trim() || null,
    })
    emit('saved')
  } catch (e) {
    const { title, message } = describeApiError(e)
    errorMessage.value = message ?? title
    notifyError(title, message)
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <BaseModal max-width="max-w-2xl" labelled-by="edit-exchange-title" @close="emit('close')">
    <div
      class="flex w-full flex-col rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl"
      style="max-height: 90vh"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-primary/20 px-8 py-5">
        <h2 id="edit-exchange-title" class="text-xl font-semibold text-light">{{ t('exchange.editExchange') }}</h2>
        <button :aria-label="t('common.close')" type="button" class="text-light/50 transition hover:text-light" @click="emit('close')">
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path
              fill-rule="evenodd"
              d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
              clip-rule="evenodd"
            />
          </svg>
        </button>
      </div>

      <!-- Body -->
      <div class="flex flex-col gap-6 overflow-y-auto px-8 py-6">
        <div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <!-- Left column: academic year + study semesters -->
          <div class="flex flex-col gap-6">
            <div>
              <label class="mb-2 block text-sm font-semibold text-primary-text">{{
                t('exchange.academicYear')
              }}</label>
              <SearchableSelect
                v-model="academicYear"
                :searchable="false"
                :options="academicYearSelectOptions"
              />
            </div>

            <StudySemesterPicker v-model="studySemesters" :semester-type="semesterType" />
          </div>

          <!-- Right column: semester type + lock warning -->
          <div>
            <SemesterTypePicker :model-value="semesterType" :can-select="period.canSelectSemesterType" @update:model-value="period.setSemesterType" />

            <div
              v-if="hasBlockedSemesterType"
              class="mt-3 flex items-start gap-2 rounded-xl border border-amber-400/40 bg-amber-500/15 px-3 py-2.5"
            >
              <svg class="mt-0.5 h-4 w-4 shrink-0 text-amber-400" viewBox="0 0 16 16" fill="none">
                <path d="M8 2L14 13H2L8 2Z" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" />
                <path d="M8 6v4M8 11.5v.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
              </svg>
              <p class="text-xs text-info-text">
                {{ t('exchange.editLockedByLa') }}<br />
                {{ t('exchange.editLockedByLaHint') }}
              </p>
            </div>
          </div>
        </div>

        <!-- Coordinator -->
        <div v-if="!guest">
          <label class="mb-2 block text-sm font-semibold text-primary-text">{{
            t('createExchange.selectCoordinator')
          }}</label>
          <SearchableSelect
            v-model="selectedCoordinatorId"
            :options="coordinatorOptions"
            :disabled="isApproved"
            :placeholder="t('createExchange.selectCoordinatorPlaceholder')"
            :search-placeholder="t('settings.profile.searchCoordinator')"
            :no-results-label="t('settings.profile.noCoordinatorResults')"
          />
          <p v-if="isApproved" class="mt-1.5 text-xs text-light/40">
            {{ t('exchange.coordinatorLockedApproved') }}
          </p>
        </div>

        <!-- Mentor -->
        <div>
          <label class="mb-2 block text-sm font-semibold text-primary-text">
            {{ t('exchange.mentor') }}
            <span class="ml-1 text-xs font-normal text-light/40">({{ t('common.optional') }})</span>
          </label>
          <input
            v-model="mentorInput"
            type="text"
            class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            :placeholder="t('exchange.mentorPlaceholder')"
          />
        </div>

        <!-- EWP link -->
        <div>
          <label class="mb-2 block text-sm font-semibold text-primary-text">
            {{ t('exchange.ewpLink') }}
            <span class="ml-1 text-xs font-normal text-light/40">({{ t('common.optional') }})</span>
          </label>
          <input
            v-model="ewpLinkInput"
            type="url"
            class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light placeholder:text-light/40 focus:border-primary focus:outline-none"
            :placeholder="t('exchange.ewpLinkPlaceholder')"
          />
        </div>

        <p v-if="errorMessage" class="text-sm text-danger-text">{{ errorMessage }}</p>
      </div>

      <!-- Footer -->
      <div class="flex justify-end gap-3 border-t border-primary/20 px-8 py-4">
        <button
          type="button"
          class="rounded-lg px-4 py-2 text-sm font-medium text-light/60 transition hover:text-light"
          @click="emit('close')"
        >
          {{ t('common.cancel') }}
        </button>
        <button
          type="button"
          class="rounded-lg bg-primary-strong px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary/80 disabled:opacity-50"
          :disabled="isSubmitting"
          @click="submit"
        >
          {{ isSubmitting ? t('common.loading') : t('common.save') }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
