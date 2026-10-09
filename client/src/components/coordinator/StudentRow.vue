<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { statusColorClass } from '@/utils/statusColors'
import type { CoordinatorStudentResponse } from '@/types/coordinator.types'

/** One student in the coordinator's list, showing their newest exchange (the others are behind "+n"). */
const props = defineProps<{ student: CoordinatorStudentResponse; actionsOpen: boolean }>()
const emit = defineEmits<{
  openExchanges: [event: MouseEvent]
  openActions: [event: MouseEvent]
  createExchange: []
  copyLink: [exchangeGuid: string]
  navigate: []
}>()

const { t } = useI18n()
const primary = computed(() => props.student.exchanges[0])
const extraCount = computed(() => Math.max(props.student.exchanges.length - 1, 0))
</script>

<template>
  <div class="group relative" data-menu-anchor>
    <!-- Stretched link: click anywhere on the row to open its primary exchange -->
    <RouterLink
      v-if="primary"
      :to="`/exchange/${primary!.guid}`"
      class="absolute inset-0 z-0 rounded-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
      :aria-label="`${student.name} — ${primary!.partnerInstitutionName}`"
      @click="emit('navigate')"
    />
    <button
      v-else
      type="button"
      class="absolute inset-0 z-0 text-left focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary focus-visible:-outline-offset-2"
      :aria-label="`${t('coordinator.createExchange')} — ${student.name}`"
      @click="emit('createExchange')"
    />

    <div class="coord-row-grid relative pointer-events-none gap-4 px-4 py-3 transition group-hover:bg-dark">
      <div class="min-w-0">
        <p class="truncate text-sm font-semibold text-light">{{ student.name }}</p>
        <!-- Metadata line: JMBAG and the not-yet-claimed state share one row so every
             student cell is the same height whether or not the badge is present. -->
        <div class="mt-0.5 flex min-w-0 items-center gap-1.5 text-xs">
          <span v-if="student.jmbag" class="truncate font-mono text-light/40">{{ student.jmbag }}</span>
          <span
            v-if="student.isPlaceholder"
            class="shrink-0 rounded-full border border-warning-text/35 bg-warning-fill px-2 py-0.5 text-[11px] font-medium text-warning-text"
          >
            {{ t('coordinator.placeholder') }}
          </span>
        </div>
      </div>

      <div class="min-w-0">
        <template v-if="primary">
          <div class="flex flex-wrap items-center gap-2">
            <span class="truncate text-sm text-light">{{ primary!.partnerInstitutionName }}</span>
            <button
              v-if="extraCount > 0"
              type="button"
              class="pointer-events-auto shrink-0 rounded-full bg-fill px-2 py-0.5 text-[11px] font-medium text-light/60 transition hover:bg-primary hover:text-white"
              @click.stop="emit('openExchanges', $event)"
            >
              {{ t('coordinator.table.moreExchanges', { n: extraCount }) }}
            </button>
          </div>
          <p class="mt-0.5 truncate text-xs text-light/40">
            {{ primary!.homeProgramName
            }}<span v-if="primary!.homeProfileName"> &middot; {{ primary!.homeProfileName }}</span>
          </p>
        </template>
        <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
      </div>

      <div class="min-w-0 text-center">
        <template v-if="primary">
          <p class="truncate text-sm text-light/70">{{ primary!.academicYear }}</p>
          <p class="truncate text-xs text-light/40">{{ t(`exchangeSemester.${primary!.semesterType}`) }}</p>
        </template>
        <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
      </div>

      <div class="flex justify-center">
        <span
          v-if="primary"
          class="rounded-full border px-2.5 py-0.5 text-xs font-semibold"
          :class="statusColorClass[primary!.learningAgreementStatus]"
        >
          {{ t(`documentStatus.${primary!.learningAgreementStatus}`) }}
        </span>
        <span v-else class="text-sm text-light/30">{{ t('coordinator.table.none') }}</span>
      </div>

      <div class="flex items-center justify-self-center gap-0.5">
        <button
          v-if="student.isPlaceholder && primary"
          type="button"
          :title="t('exchangeAccess.copyLink')"
          class="pointer-events-auto flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-light/40 transition hover:bg-primary/10 hover:text-primary-text"
          @click.stop="emit('copyLink', primary!.guid)"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
            <path d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5z" />
            <path d="M7.414 15.414a2 2 0 01-2.828-2.828l3-3a2 2 0 012.828 0 1 1 0 001.414-1.414 4 4 0 00-5.656 0l-3 3a4 4 0 105.656 5.656l1.5-1.5a1 1 0 10-1.414-1.414l-1.5 1.5z" />
          </svg>
        </button>
        <span v-else class="h-7 w-7 shrink-0"></span>

        <a
          v-if="primary?.ewpLink"
          :href="primary!.ewpLink!"
          target="_blank"
          rel="noopener noreferrer"
          :title="t('exchange.ewpLink')"
          class="pointer-events-auto flex h-7 w-7 shrink-0 items-center justify-center rounded-lg text-light/40 transition hover:bg-primary/10 hover:text-primary-text"
          @click.stop
        >
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
            <path d="M5 2H2a1 1 0 0 0-1 1v7a1 1 0 0 0 1 1h7a1 1 0 0 0 1-1V7" />
            <path d="M8 1h3v3" /><line x1="11" y1="1" x2="5" y2="7" />
          </svg>
        </a>
        <span v-else class="h-7 w-7 shrink-0"></span>
      </div>

      <button
        type="button"
        class="pointer-events-auto flex h-7 w-7 items-center justify-center justify-self-center rounded-lg text-lg leading-none text-light/40 transition hover:bg-fill hover:text-light"
        :aria-expanded="actionsOpen"
        aria-haspopup="true"
        :aria-label="`${t('coordinator.table.actions')} — ${student.name}`"
        @click.stop="emit('openActions', $event)"
      >⋯</button>
    </div>
  </div>
</template>
