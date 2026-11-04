<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { statusDotClass } from '@/utils/statusColors'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'
import { ExternalLinkIcon, LinkIcon, PlusIcon } from '@lucide/vue'

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
              <ExternalLinkIcon :size="11" />
            </a>
            <button
              v-if="student.isPlaceholder"
              type="button"
              :title="t('exchangeAccess.copyLink')"
              class="flex h-6 w-6 items-center justify-center rounded text-light/40 transition hover:bg-primary/10 hover:text-primary-text"
              @click.stop="emit('copyLink', ex.guid)"
            >
              <LinkIcon class="h-3.5 w-3.5" />
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
        <PlusIcon class="h-3.5 w-3.5" />
        {{ t('coordinator.createExchange') }}
      </button>
    </div>
  </Teleport>
</template>
