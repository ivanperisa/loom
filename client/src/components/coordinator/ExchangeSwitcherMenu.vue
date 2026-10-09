<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { statusDotClass } from '@/utils/statusColors'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'

/** A student's exchanges (newest first, ✓ = the one their row opens), and "new exchange". */
defineProps<{ student: CoordinatorStudentResponse; position: { top: number; left: number } }>()
const emit = defineEmits<{ view: [exchangeGuid: string]; copyLink: [exchangeGuid: string]; createExchange: []; close: [] }>()

const { t } = useI18n()
</script>

<template>
  <Teleport to="body">
    <div
        data-menu-anchor
      class="fixed z-50 w-[380px] rounded-xl border border-primary/20 bg-dark-2 p-1.5 shadow-2xl shadow-black/50"
      :style="{ top: position.top + 'px', left: position.left + 'px' }"
    >
      <p class="px-2.5 pb-1 pt-1 text-[10px] font-semibold uppercase tracking-wider text-light/40">
        {{ t('coordinator.table.exchange') }}
      </p>

      <div v-if="student.exchanges.length === 0" class="px-2.5 py-3 text-center text-xs text-light/40">
        {{ t('coordinator.noExchanges') }}
      </div>
      <div v-else class="space-y-0.5">
        <div
          v-for="ex in student.exchanges"
          :key="ex.id"
          class="flex cursor-pointer items-start justify-between gap-2 rounded-lg px-2 py-1.5 transition hover:bg-fill-soft"
          @click="emit('view', ex.guid)"
        >
          <div class="flex min-w-0 items-start gap-2">
            <span class="mt-0.5 w-3 shrink-0 text-center text-xs font-bold text-primary-text">
              {{ ex.id === student.exchanges[0]?.id ? '✓' : '' }}
            </span>
            <div class="min-w-0">
              <p class="text-sm font-medium text-light">{{ ex.partnerInstitutionName }}</p>
              <p class="mt-0.5 text-xs text-light/40">
                {{ ex.homeProgramName }}<span v-if="ex.homeProfileName"> &middot; {{ ex.homeProfileName }}</span>
              </p>
              <p class="mt-0.5 flex items-center gap-1.5 text-xs text-light/50">
                <span class="h-1.5 w-1.5 shrink-0 rounded-full" :class="statusDotClass[ex.learningAgreementStatus]"></span>
                <span class="truncate">
                  {{ ex.academicYear }} &middot; {{ t(`exchangeSemester.${ex.semesterType}`) }}
                  &middot; {{ t(`documentStatus.${ex.learningAgreementStatus}`) }}
                </span>
              </p>
            </div>
          </div>
          <div class="flex shrink-0 items-center gap-1">
            <a
              v-if="ex.ewpLink"
              :href="ex.ewpLink"
              target="_blank"
              rel="noopener noreferrer"
              :title="t('exchange.ewpLink')"
              class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-text"
              @click.stop="emit('close')"
            >
              <svg width="11" height="11" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
                <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
              </svg>
            </a>
            <button
              v-if="student.isPlaceholder"
              type="button"
              :title="t('exchangeAccess.copyLink')"
              class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-text"
              @click.stop="emit('copyLink', ex.guid)"
            >
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
                <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <div class="my-1.5 border-t border-primary/20"></div>
      <button
        type="button"
        class="flex w-full items-center gap-2 rounded-lg px-2.5 py-2 text-left text-sm font-semibold text-primary-text transition hover:bg-primary/10"
        @click="emit('createExchange')"
      >
        <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        {{ t('coordinator.createExchange') }}
      </button>
    </div>
  </Teleport>
</template>
