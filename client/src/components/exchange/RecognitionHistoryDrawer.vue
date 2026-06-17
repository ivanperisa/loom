<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import type { RecognitionSnapshotSummary } from '@/types/recognition.types'

const props = defineProps<{ exchangeId: string }>()
const emit = defineEmits<{ close: [] }>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()

const history = ref<RecognitionSnapshotSummary[]>([])
const expandedIds = ref<Set<number>>(new Set())
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    history.value = await exchangeStore.fetchRecognitionHistory(props.exchangeId)
  } finally {
    loading.value = false
  }
})

function entryCount(n: number): string {
  if (locale.value !== 'hr') return `${n} ${n === 1 ? 'entry' : 'entries'}`
  const t2 = n % 100, t1 = n % 10
  if (t2 >= 11 && t2 <= 19) return `${n} stavki`
  if (t1 === 1) return `${n} stavka`
  if (t1 >= 2 && t1 <= 4) return `${n} stavke`
  return `${n} stavki`
}

function toggleExpand(id: number) {
  if (expandedIds.value.has(id)) expandedIds.value.delete(id)
  else expandedIds.value.add(id)
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(locale.value === 'hr' ? 'hr-HR' : 'en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<template>
  <Teleport to="body">
    <div class="drawer-backdrop" @mousedown.self="emit('close')">
      <div class="drawer-backdrop-bg" @click="emit('close')" />
      <div class="drawer">
        <div class="drawer-header">
          <h2 class="drawer-title">{{ t('recognition.history.title') }}</h2>
          <button type="button" class="drawer-close" @click="emit('close')">&times;</button>
        </div>

        <div class="drawer-body">
          <div v-if="loading" class="drawer-empty">{{ t('common.loading') }}</div>
          <p v-else-if="history.length === 0" class="drawer-empty">{{ t('recognition.history.empty') }}</p>

          <div v-for="item in history" :key="item.id" class="snapshot-card">
            <div class="snapshot-card__header">
              <div>
                <div class="snapshot-card__date">{{ formatDate(item.approvedAt) }}</div>
                <div class="snapshot-card__meta">
                  {{ item.approvedByName }} · {{ entryCount(item.entryCount) }}
                </div>
              </div>
              <div v-if="item.diff" class="diff-badges">
                <span v-if="item.diff.added.length" class="diff-badge diff-badge--added">+{{ item.diff.added.length }}</span>
                <span v-if="item.diff.removed.length" class="diff-badge diff-badge--removed">-{{ item.diff.removed.length }}</span>
                <span v-if="item.diff.modified.length" class="diff-badge diff-badge--modified">~{{ item.diff.modified.length }}</span>
              </div>
            </div>

            <button
              v-if="item.diff"
              type="button"
              class="snapshot-card__toggle"
              @click="toggleExpand(item.id)"
            >
              {{ expandedIds.has(item.id) ? `▲ ${t('recognition.history.hide')}` : `▼ ${t('recognition.history.details')}` }}
            </button>

            <div v-if="item.diff && expandedIds.has(item.id)" class="diff-list">
              <div
                v-for="e in item.diff.added"
                :key="`add-${e.homeSlotLabel}-${e.partnerCourseCode}`"
                class="diff-row diff-row--added"
              >
                + {{ e.homeSlotLabel }} → {{ e.partnerCourseCode ?? '—' }}
              </div>
              <div
                v-for="e in item.diff.removed"
                :key="`rem-${e.homeSlotLabel}-${e.partnerCourseCode}`"
                class="diff-row diff-row--removed"
              >
                - {{ e.homeSlotLabel }} → {{ e.partnerCourseCode ?? '—' }}
              </div>
              <div
                v-for="e in item.diff.modified"
                :key="`mod-${e.before.homeSlotLabel}-${e.before.partnerCourseCode}`"
                class="diff-row diff-row--modified"
              >
                ~ {{ e.before.homeSlotLabel }}:
                <template v-if="e.before.originalGrade !== e.after.originalGrade">
                  {{ t('recognition.history.gradeChanged', { before: e.before.originalGrade ?? '–', after: e.after.originalGrade ?? '–' }) }}
                </template>
                <template v-else-if="e.before.isRecognized !== e.after.isRecognized">
                  {{ t('recognition.history.recognizedChanged', {
                    before: e.before.isRecognized ? t('recognition.history.yes') : t('recognition.history.no'),
                    after: e.after.isRecognized ? t('recognition.history.yes') : t('recognition.history.no'),
                  }) }}
                </template>
                <template v-else>{{ t('recognition.history.modified') }}</template>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.drawer-backdrop {
  position: fixed;
  inset: 0;
  z-index: 55;
}

.drawer-backdrop-bg {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.3);
}

.drawer {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: 480px;
  background: var(--color-dark-2);
  border-left: 1px solid color-mix(in srgb, var(--color-primary) 20%, transparent);
  display: flex;
  flex-direction: column;
}

.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid color-mix(in srgb, var(--color-light) 10%, transparent);
  flex-shrink: 0;
}

.drawer-title {
  color: var(--color-light);
  font-size: 16px;
  font-weight: 700;
  margin: 0;
}

.drawer-close {
  color: var(--color-light);
  opacity: 0.5;
  font-size: 22px;
  background: none;
  border: none;
  cursor: pointer;
  line-height: 1;
  padding: 0;
}
.drawer-close:hover { opacity: 0.9; }

.drawer-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 24px;
}

.drawer-empty {
  color: var(--color-light);
  opacity: 0.4;
  font-size: 13px;
  text-align: center;
  padding: 32px 0;
  margin: 0;
}

.snapshot-card {
  border: 1px solid color-mix(in srgb, var(--color-light) 10%, transparent);
  border-radius: 8px;
  padding: 14px;
  margin-bottom: 12px;
}

.snapshot-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.snapshot-card__date {
  color: var(--color-light);
  font-size: 13px;
  font-weight: 600;
}

.snapshot-card__meta {
  color: var(--color-light);
  opacity: 0.7;
  font-size: 12px;
  margin-top: 2px;
}

.diff-badges {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.diff-badge {
  font-size: 11px;
  padding: 2px 7px;
  border-radius: 10px;
  font-weight: 600;
}

.diff-badge--added   { background: color-mix(in srgb, #16a34a 15%, transparent); color: #16a34a; }
.diff-badge--removed { background: color-mix(in srgb, #dc2626 15%, transparent); color: #dc2626; }
.diff-badge--modified{ background: color-mix(in srgb, #d97706 15%, transparent); color: #d97706; }

.snapshot-card__toggle {
  display: inline-block;
  margin-top: 10px;
  font-size: 11px;
  color: var(--color-primary);
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
}

.diff-list {
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.diff-row {
  font-size: 11px;
  padding: 4px 8px;
  border-radius: 4px;
}

.diff-row--added   { background: color-mix(in srgb, #16a34a 8%, transparent); color: #16a34a; }
.diff-row--removed { background: color-mix(in srgb, #dc2626 8%, transparent); color: #dc2626; }
.diff-row--modified{ background: color-mix(in srgb, #d97706 8%, transparent); color: #d97706; }
</style>
