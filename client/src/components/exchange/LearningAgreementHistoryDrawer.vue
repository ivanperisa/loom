<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useConfirm } from '@/composables/useConfirm'
import { useNotification } from '@/composables/useNotification'
import ActionButton from '@/components/common/ActionButton.vue'
import type { LaSnapshotSummary, SnapshotListItem } from '@/types/learningAgreement.types'

const props = defineProps<{
  exchangeId: string
  guestMode: boolean
}>()

const emit = defineEmits<{ close: [] }>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { confirm } = useConfirm()
const { notifySuccess, notifyError } = useNotification()

const activeTab = ref<'history' | 'versions'>('history')
const history = ref<LaSnapshotSummary[]>([])
const snapshots = ref<SnapshotListItem[]>([])
const expandedIds = ref<Set<number>>(new Set())
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    history.value = await exchangeStore.fetchLaHistory(props.exchangeId)
    if (!props.guestMode) {
      snapshots.value = await exchangeStore.fetchSnapshots(props.exchangeId)
    }
  } finally {
    loading.value = false
  }
})

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

function mappingCount(n: number): string {
  if (locale.value !== 'hr') return `${n} ${n === 1 ? 'mapping' : 'mappings'}`
  const t2 = n % 100, t1 = n % 10
  if (t2 >= 11 && t2 <= 19) return `${n} mapiranja`
  if (t1 === 1) return `${n} mapiranje`
  return `${n} mapiranja`
}

async function restore(snapshotId: number) {
  const ok = await confirm({
    title: t('la.history.restoreConfirmTitle'),
    message: t('la.history.restoreConfirmMessage'),
  })
  if (!ok) return
  try {
    await exchangeStore.restoreSnapshot(props.exchangeId, snapshotId)
    notifySuccess(t('la.history.restoreSuccess'))
    snapshots.value = await exchangeStore.fetchSnapshots(props.exchangeId)
    emit('close')
  } catch {
    notifyError(t('la.history.restoreError'))
  }
}
</script>

<template>
  <Teleport to="body">
    <div class="drawer-backdrop" @mousedown.self="emit('close')">
      <div class="drawer-backdrop-bg" @click="emit('close')" />
      <div class="drawer">
        <div class="drawer-header">
          <h2 class="drawer-title">{{ t('la.history.title') }}</h2>
          <button type="button" class="drawer-close" @click="emit('close')">&times;</button>
        </div>

        <div class="drawer-tabs">
          <button
            type="button"
            class="drawer-tab"
            :class="{ 'drawer-tab--active': activeTab === 'history' }"
            @click="activeTab = 'history'"
          >
            {{ t('la.history.tabApprovals') }}
          </button>
          <button
            v-if="!guestMode"
            type="button"
            class="drawer-tab"
            :class="{ 'drawer-tab--active': activeTab === 'versions' }"
            @click="activeTab = 'versions'"
          >
            {{ t('la.history.tabVersions') }}
          </button>
        </div>

        <div class="drawer-body">
          <div v-if="loading" class="drawer-empty">{{ t('common.loading') }}</div>

          <template v-else-if="activeTab === 'history'">
            <p v-if="history.length === 0" class="drawer-empty">{{ t('la.history.empty') }}</p>
            <div v-for="item in history" :key="item.id" class="snapshot-card">
              <div class="snapshot-card__header">
                <div>
                  <div class="snapshot-card__date">{{ formatDate(item.approvedAt) }}</div>
                  <div class="snapshot-card__meta">
                    {{ item.approvedByName }} · {{ mappingCount(item.entryCount) }}
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
                {{ expandedIds.has(item.id) ? `▲ ${t('la.history.hide')}` : `▼ ${t('la.history.details')}` }}
              </button>

              <div v-if="item.diff && expandedIds.has(item.id)" class="diff-list">
                <div
                  v-for="e in item.diff.added"
                  :key="`add-${e.homeSlotId}-${e.partnerCourseId}`"
                  class="diff-row diff-row--added"
                >
                  + {{ e.homeSlotLabel }} → {{ e.partnerCourseCode ?? '—' }}<span v-if="e.awardedEcts"> ({{ e.awardedEcts }} ECTS)</span>
                </div>
                <div
                  v-for="e in item.diff.removed"
                  :key="`rem-${e.homeSlotId}-${e.partnerCourseId}`"
                  class="diff-row diff-row--removed"
                >
                  - {{ e.homeSlotLabel }} → {{ e.partnerCourseCode ?? '—' }}
                </div>
                <div
                  v-for="e in item.diff.modified"
                  :key="`mod-${e.before.homeSlotId}-${e.before.partnerCourseId}`"
                  class="diff-row diff-row--modified"
                >
                  ~ {{ e.before.homeSlotLabel }}: {{ e.before.awardedEcts }} → {{ e.after.awardedEcts }} ECTS
                </div>
              </div>
            </div>
          </template>

          <template v-else>
            <p v-if="snapshots.length === 0" class="drawer-empty">{{ t('la.history.versionsEmpty') }}</p>
            <div v-for="snap in snapshots" :key="snap.id" class="snapshot-card snapshot-card--version">
              <div>
                <div class="snapshot-card__version-row">
                  <span
                    class="snapshot-badge"
                    :class="snap.type === 'Auto' ? 'snapshot-badge--auto' : 'snapshot-badge--backup'"
                  >
                    {{ snap.type === 'Auto' ? t('la.history.snapshotTypeAuto') : t('la.history.snapshotTypePreImport') }}
                  </span>
                  <span class="snapshot-card__date">{{ formatDate(snap.createdAt) }}</span>
                </div>
                <div class="snapshot-card__meta">
                  {{ snap.createdByName }} · {{ mappingCount(snap.entryCount) }}
                </div>
              </div>
              <ActionButton variant="solid" @click="restore(snap.id)">
                {{ t('la.history.restore') }}
              </ActionButton>
            </div>
          </template>
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

.drawer-tabs {
  display: flex;
  border-bottom: 1px solid color-mix(in srgb, var(--color-light) 10%, transparent);
  flex-shrink: 0;
}

.drawer-tab {
  flex: 1;
  padding: 12px;
  font-size: 13px;
  font-weight: 600;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  cursor: pointer;
  color: var(--color-light);
  opacity: 0.5;
  transition: opacity 0.15s;
}

.drawer-tab--active {
  color: var(--color-primary);
  opacity: 1;
  border-bottom-color: var(--color-primary);
}

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

.snapshot-card--version {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.snapshot-card__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.snapshot-card__version-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 4px;
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

.snapshot-badge {
  font-size: 10px;
  padding: 2px 8px;
  border-radius: 10px;
  font-weight: 600;
}

.snapshot-badge--auto {
  background: color-mix(in srgb, var(--color-light) 12%, transparent);
  color: var(--color-light);
  opacity: 0.7;
}

.snapshot-badge--backup {
  background: color-mix(in srgb, #d97706 15%, transparent);
  color: #d97706;
}
</style>
