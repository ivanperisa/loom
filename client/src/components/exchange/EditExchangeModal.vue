<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { coordinatorService } from '@/services/coordinator.service'
import { useExchangeStore } from '@/stores/exchange.store'
import { exchangeSemester } from '@/utils/exchangeSemester'
import { extractApiError } from '@/utils/apiError'
import { useNotification } from '@/composables/useNotification'
import type { CoordinatorOption } from '@/types/coordinator.types'
import type { ExchangeResponse, ExchangeSemester } from '@/types/exchange.types'
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
const exchangeStore = useExchangeStore()
const { notifyError } = useNotification()

const errorMessage = ref<string | null>(null)
const isSubmitting = ref(false)

// Prefill from the exchange being edited
const academicYear = ref(props.exchange.academicYear)
const semesterType = ref<ExchangeSemester>(props.exchange.semesterType)
const studySemesters = ref<number[]>([...props.exchange.studySemesters])
const selectedCoordinatorId = ref<string | null>(props.exchange.coordinatorId)
const mentorInput = ref(props.exchange.mentor ?? '')
const ewpLinkInput = ref(props.exchange.ewpLink ?? '')

// Academic year options
const academicYearOptions = computed(() => {
  const now = new Date()
  const y = now.getFullYear()
  const start = now.getMonth() + 1 >= 9 ? y : y - 1
  const opts = [`${start}/${start + 1}`, `${start + 1}/${start + 2}`]
  if (!opts.includes(academicYear.value)) opts.unshift(academicYear.value)
  return opts
})
const academicYearSelectOptions = computed(() =>
  academicYearOptions.value.map((year) => ({ value: year, label: year })),
)

const allowedSemesters = computed<number[]>(() => {
  if (semesterType.value === exchangeSemester.Winter) return [1, 3]
  if (semesterType.value === exchangeSemester.Summer) return [2, 4]
  return []
})

const bothPairs = [[1, 2], [3, 4]]

function selectPair(pair: number[]) {
  const sorted = [...pair].sort((a, b) => a - b)
  const isSame =
    studySemesters.value.length === sorted.length &&
    sorted.every((v, i) => studySemesters.value.slice().sort((a, b) => a - b)[i] === v)
  studySemesters.value = isSame ? [] : [...sorted]
}

function isPairSelected(pair: number[]): boolean {
  const sorted = [...pair].sort((a, b) => a - b)
  return (
    studySemesters.value.length === sorted.length &&
    sorted.every((v, i) => studySemesters.value.slice().sort((a, b) => a - b)[i] === v)
  )
}

function toggleStudySemester(s: number) {
  if (semesterType.value === exchangeSemester.Both) {
    const idx = studySemesters.value.indexOf(s)
    if (idx === -1) studySemesters.value.push(s)
    else studySemesters.value.splice(idx, 1)
  } else {
    studySemesters.value = studySemesters.value[0] === s ? [] : [s]
  }
}

function canSelectSemesterType(sem: ExchangeSemester): boolean {
  if (sem === exchangeSemester.Both) return true
  const allowed = sem === exchangeSemester.Winter ? [1, 3] : [2, 4]
  return props.laMappedSemesters.every((s) => allowed.includes(s))
}

const hasBlockedSemesterType = computed(() =>
  [exchangeSemester.Winter, exchangeSemester.Summer, exchangeSemester.Both].some(
    (sem) => !canSelectSemesterType(sem),
  ),
)

function setSemesterType(sem: ExchangeSemester) {
  if (!canSelectSemesterType(sem)) return
  semesterType.value = sem
  if (sem !== exchangeSemester.Both) {
    const allowed = sem === exchangeSemester.Winter ? [1, 3] : [2, 4]
    studySemesters.value = studySemesters.value.filter((s) => allowed.includes(s))
  }
}

const coordinators = ref<CoordinatorOption[]>([])
const coordinatorOptions = computed(() => [
  { value: null, label: t('exchange.noCoordinator') },
  ...coordinators.value.map((c) => ({ value: c.id, label: c.name })),
])

onMounted(async () => {
  if (exchangeStore.guestMode) return
  try {
    coordinators.value = (await coordinatorService.getCoordinators()).data
  } catch {
    // non-fatal: coordinator list stays empty, current value still shows via label fallback
  }
})

async function submit() {
  errorMessage.value = null
  if (!academicYear.value.trim()) {
    errorMessage.value = t('createExchange.errors.academicYearRequired')
    return
  }
  if (studySemesters.value.length === 0) {
    errorMessage.value = t('createExchange.errors.studySemesterRequired')
    return
  }

  isSubmitting.value = true
  try {
    await exchangeStore.updateExchange(props.exchange.guid, {
      academicYear: academicYear.value.trim(),
      semesterType: semesterType.value,
      studySemesters: studySemesters.value,
      coordinatorId: selectedCoordinatorId.value,
      mentor: mentorInput.value.trim() || null,
      ewpLink: ewpLinkInput.value.trim() || null,
    })
    emit('saved')
  } catch (e) {
    const { title, message } = extractApiError(e)
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
        <button type="button" class="text-light/50 transition hover:text-white" @click="emit('close')">
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
              <label class="mb-2 block text-sm font-semibold text-primary-light">{{
                t('exchange.academicYear')
              }}</label>
              <SearchableSelect
                v-model="academicYear"
                :searchable="false"
                :options="academicYearSelectOptions"
              />
            </div>

            <!-- Study semesters -->
            <div>
              <label class="mb-2 block text-sm font-semibold text-primary-light">{{
                t('exchange.studySemester')
              }}</label>

              <div v-if="semesterType !== exchangeSemester.Both" class="flex gap-2">
                <button
                  v-for="s in allowedSemesters"
                  :key="s"
                  type="button"
                  class="h-10 w-10 rounded-xl border text-sm font-semibold transition"
                  :class="
                    studySemesters.includes(s)
                      ? 'border-primary bg-primary/10 text-white'
                      : 'border-hairline bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                  "
                  @click="toggleStudySemester(s)"
                >
                  {{ s }}
                </button>
              </div>

              <div v-else class="flex gap-3">
                <button
                  v-for="pair in bothPairs"
                  :key="pair.join()"
                  type="button"
                  class="rounded-xl border px-5 py-2.5 text-sm font-semibold transition"
                  :class="
                    isPairSelected(pair)
                      ? 'border-primary bg-primary/10 text-white'
                      : 'border-hairline bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                  "
                  @click="selectPair(pair)"
                >
                  {{ pair.join(' + ') }}
                </button>
              </div>
            </div>
          </div>

          <!-- Right column: semester type + lock warning -->
          <div>
            <label class="mb-2 block text-sm font-semibold text-primary-light">{{
              t('exchange.semester')
            }}</label>
            <div class="grid grid-cols-3 gap-2">
              <button
                v-for="sem in [exchangeSemester.Winter, exchangeSemester.Summer, exchangeSemester.Both]"
                :key="sem"
                type="button"
                :disabled="!canSelectSemesterType(sem)"
                class="rounded-xl border py-2.5 text-xs font-medium transition disabled:cursor-not-allowed disabled:opacity-40"
                :class="
                  semesterType === sem
                    ? 'border-primary bg-primary/10 text-white'
                    : 'border-hairline bg-dark text-light/60 hover:border-primary/50 hover:text-white'
                "
                @click="setSemesterType(sem)"
              >
                {{ t(`exchangeSemester.${sem}`) }}
              </button>
            </div>

            <div
              v-if="hasBlockedSemesterType"
              class="mt-3 flex items-start gap-2 rounded-xl border border-amber-400/40 bg-amber-500/15 px-3 py-2.5"
            >
              <svg class="mt-0.5 h-4 w-4 shrink-0 text-amber-400" viewBox="0 0 16 16" fill="none">
                <path d="M8 2L14 13H2L8 2Z" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" />
                <path d="M8 6v4M8 11.5v.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
              </svg>
              <p class="text-xs text-info">
                {{ t('exchange.editLockedByLa') }}<br />
                {{ t('exchange.editLockedByLaHint') }}
              </p>
            </div>
          </div>
        </div>

        <!-- Coordinator -->
        <div v-if="!exchangeStore.guestMode">
          <label class="mb-2 block text-sm font-semibold text-primary-light">{{
            t('createExchange.selectCoordinator')
          }}</label>
          <SearchableSelect
            v-model="selectedCoordinatorId"
            :options="coordinatorOptions"
            :placeholder="t('createExchange.selectCoordinatorPlaceholder')"
            :search-placeholder="t('settings.profile.searchCoordinator')"
            :no-results-label="t('settings.profile.noCoordinatorResults')"
          />
        </div>

        <!-- Mentor -->
        <div>
          <label class="mb-2 block text-sm font-semibold text-primary-light">
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
          <label class="mb-2 block text-sm font-semibold text-primary-light">
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

        <p v-if="errorMessage" class="text-sm text-danger">{{ errorMessage }}</p>
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
          class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary/80 disabled:opacity-50"
          :disabled="isSubmitting"
          @click="submit"
        >
          {{ isSubmitting ? t('common.loading') : t('common.save') }}
        </button>
      </div>
    </div>
  </BaseModal>
</template>
