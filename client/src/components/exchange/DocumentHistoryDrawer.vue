<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import { useConfirm } from '@/composables/useConfirm'
import { useNotification } from '@/composables/useNotification'
import { extractApiError } from '@/utils/apiError'
import { formatDate } from '@/utils/formatDate'
import ActionButton from '@/components/common/ActionButton.vue'
import BaseModal from '@/components/common/BaseModal.vue'
import type { DocumentChange, DocumentVersionResponse, FieldChange } from '@/types/documentVersion.types'

const props = defineProps<{
  exchangeId: string
  document: 'la' | 'recognition'
  /** Restore loads a version into the LA draft; only while the LA can be edited. */
  canRestore: boolean
}>()

const emit = defineEmits<{ close: [] }>()

const { t, locale } = useI18n()
const exchangeStore = useExchangeStore()
const { confirm } = useConfirm()
const { notifySuccess, notifyWarning, notifyError } = useNotification()

const activeTab = ref<'approvals' | 'backups'>('approvals')
const versions = ref<DocumentVersionResponse[]>([])
const expandedIds = ref<Set<number>>(new Set())
const loading = ref(false)

const approvals = computed(() => versions.value.filter((v) => v.kind === 'Approved'))
const backups = computed(() => versions.value.filter((v) => v.kind === 'Backup'))
const isLa = computed(() => props.document === 'la')

async function load() {
  loading.value = true
  try {
    versions.value = isLa.value
      ? await exchangeStore.fetchLaVersions(props.exchangeId)
      : await exchangeStore.fetchRecognitionVersions(props.exchangeId)
  } finally {
    loading.value = false
  }
}
onMounted(load)

function toggleExpand(id: number) {
  if (expandedIds.value.has(id)) expandedIds.value.delete(id)
  else expandedIds.value.add(id)
}

function versionTitle(v: DocumentVersionResponse): string {
  if (v.versionNo === null) return t('history.backup')
  return v.versionNo > 1
    ? t('history.amendment', { n: v.versionNo, label: t('la.amendmentLabel', { n: v.versionNo - 1 }) })
    : t('history.original', { n: v.versionNo })
}

function count(changes: DocumentChange[] | null, type: DocumentChange['type']): number {
  return changes?.filter((c) => c.type === type).length ?? 0
}

function fieldLabel(field: string): string {
  return t(`history.fields.${field}`)
}

function fieldValue(change: FieldChange, side: 'before' | 'after'): string {
  const value = change[side]
  if (value === null || value === '') return '—'
  if (change.field === 'mode') return t(`slotMode.${value}`)
  if (change.field === 'enrollmentStatus') return t(`recognition.status.${value}`)
  return value
}

async function restore(version: DocumentVersionResponse) {
  const ok = await confirm({
    title: t('history.restoreConfirmTitle', { name: versionTitle(version) }),
    message: t('history.restoreConfirmMessage'),
  })
  if (!ok) return
  try {
    const result = await exchangeStore.restoreLaVersion(props.exchangeId, version.id)
    if (result.missing.length === 0) {
      notifySuccess(t('history.restoreSuccess'))
    } else {
      notifyWarning(
        t('history.restorePartial'),
        result.missing.map((m) => `${m.homeSlotLabel}: ${m.partnerCourseCode ?? '?'} ${m.partnerCourseName ?? ''}`.trim()).join(', '),
      )
    }
    emit('close')
  } catch (error) {
    const { title, message } = extractApiError(error)
    notifyError(t('history.restoreError'), message ?? title)
  }
}
</script>

<template>
  <BaseModal max-width="max-w-2xl" labelled-by="history-drawer-title" @close="emit('close')">
    <div class="drawer">
      <div class="drawer-header">
        <h2 id="history-drawer-title" class="drawer-title">
          {{ isLa ? t('history.titleLa') : t('history.titleRecognition') }}
        </h2>
        <button type="button" class="drawer-close" :aria-label="t('common.close')" @click="emit('close')">&times;</button>
      </div>

      <div v-if="isLa" class="drawer-tabs" role="tablist">
        <button
          type="button"
          role="tab"
          class="drawer-tab"
          :aria-selected="activeTab === 'approvals'"
          :class="{ 'drawer-tab--active': activeTab === 'approvals' }"
          @click="activeTab = 'approvals'"
        >
          {{ t('history.tabApprovals') }}
        </button>
        <button
          type="button"
          role="tab"
          class="drawer-tab"
          :aria-selected="activeTab === 'backups'"
          :class="{ 'drawer-tab--active': activeTab === 'backups' }"
          @click="activeTab = 'backups'"
        >
          {{ t('history.tabBackups') }}
        </button>
      </div>

      <div class="drawer-body">
        <div v-if="loading" class="drawer-empty">{{ t('common.loading') }}</div>

        <template v-else-if="activeTab === 'approvals'">
          <p v-if="approvals.length === 0" class="drawer-empty">{{ t('history.empty') }}</p>
          <div v-for="item in approvals" :key="item.id" class="snapshot-card">
            <div class="snapshot-card__header">
              <div>
                <div class="snapshot-card__version-row">
                  <span class="snapshot-badge snapshot-badge--approved">{{ versionTitle(item) }}</span>
                  <span class="snapshot-card__date">{{ formatDate(item.createdAt, locale) }}</span>
                </div>
                <div class="snapshot-card__meta">
                  {{ item.createdByName ?? '—' }} · {{ t('history.courseCount', { n: item.entryCount }, item.entryCount) }}
                </div>
              </div>
              <div v-if="item.changes" class="diff-badges">
                <span v-if="count(item.changes, 'Added')" class="diff-badge diff-badge--added">+{{ count(item.changes, 'Added') }}</span>
                <span v-if="count(item.changes, 'Removed')" class="diff-badge diff-badge--removed">-{{ count(item.changes, 'Removed') }}</span>
                <span v-if="count(item.changes, 'Modified')" class="diff-badge diff-badge--modified">~{{ count(item.changes, 'Modified') }}</span>
              </div>
            </div>

            <div class="snapshot-card__actions">
              <button
                v-if="item.changes?.length"
                type="button"
                class="snapshot-card__toggle"
                :aria-expanded="expandedIds.has(item.id)"
                @click="toggleExpand(item.id)"
              >
                {{ expandedIds.has(item.id) ? `▲ ${t('history.hide')}` : `▼ ${t('history.details')}` }}
              </button>
              <p v-else-if="item.changes === null" class="snapshot-card__meta">{{ t('history.noDetails') }}</p>
              <ActionButton v-if="isLa && canRestore" @click="restore(item)">{{ t('history.restore') }}</ActionButton>
            </div>

            <ul v-if="item.changes && expandedIds.has(item.id)" class="diff-list">
              <li
                v-for="c in item.changes"
                :key="`${c.type}-${c.homeSlotId}-${c.partnerCourseId}`"
                class="diff-row"
                :class="`diff-row--${c.type.toLowerCase()}`"
              >
                <strong>{{ c.type === 'Added' ? '+' : c.type === 'Removed' ? '−' : '~' }}</strong>
                {{ c.homeSlotLabel }}<template v-if="c.partnerCourseCode"> → {{ c.partnerCourseCode }} {{ c.partnerCourseName }}</template>
                <span v-for="f in c.fields" :key="f.field" class="diff-field">
                  {{ fieldLabel(f.field) }}:
                  <template v-if="c.type === 'Modified'">{{ fieldValue(f, 'before') }} → {{ fieldValue(f, 'after') }}</template>
                  <template v-else>{{ fieldValue(f, c.type === 'Added' ? 'after' : 'before') }}</template>
                </span>
              </li>
            </ul>
          </div>
        </template>

        <template v-else>
          <p class="drawer-hint">{{ t('history.backupsHint') }}</p>
          <p v-if="backups.length === 0" class="drawer-empty">{{ t('history.backupsEmpty') }}</p>
          <div v-for="item in backups" :key="item.id" class="snapshot-card snapshot-card--version">
            <div>
              <div class="snapshot-card__version-row">
                <span class="snapshot-badge snapshot-badge--backup">{{ t('history.backup') }}</span>
                <span class="snapshot-card__date">{{ formatDate(item.createdAt, locale) }}</span>
              </div>
              <div class="snapshot-card__meta">
                {{ item.createdByName ?? '—' }} · {{ t('history.courseCount', { n: item.entryCount }, item.entryCount) }}
              </div>
            </div>
            <ActionButton v-if="canRestore" variant="solid" @click="restore(item)">{{ t('history.restore') }}</ActionButton>
          </div>
        </template>

        <p v-if="isLa && !canRestore && versions.length > 0" class="drawer-hint">{{ t('history.restoreLocked') }}</p>
      </div>
    </div>
  </BaseModal>
</template>

<style scoped>
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
:global([data-theme='light']) .diff-badge--added    { background: var(--color-success-fill); color: var(--color-success-text); }
:global([data-theme='light']) .diff-badge--removed  { background: var(--color-danger-fill); color: var(--color-danger-text); }
:global([data-theme='light']) .diff-badge--modified { background: var(--color-warning-fill); color: var(--color-warning-text); }

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
:global([data-theme='light']) .diff-row--added    { color: var(--color-success-text); }
:global([data-theme='light']) .diff-row--removed  { color: var(--color-danger-text); }
:global([data-theme='light']) .diff-row--modified { color: var(--color-warning-text); }

.snapshot-badge {
  font-size: 10px;
  padding: 2px 8px;
  border-radius: 10px;
  font-weight: 600;
}

.snapshot-badge--auto,
.snapshot-badge--approved {
  background: color-mix(in srgb, var(--color-light) 12%, transparent);
  color: var(--color-light);
  opacity: 0.7;
}

.snapshot-badge--backup {
  background: color-mix(in srgb, #d97706 15%, transparent);
  color: #d97706;
}
:global([data-theme='light']) .snapshot-badge--backup { background: var(--color-warning-fill); color: var(--color-warning-text); }

.snapshot-card__actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-top: 10px;
}
.snapshot-card__actions .snapshot-card__toggle { margin-top: 0; }

.diff-list {
  list-style: none;
  padding: 0;
}

.diff-field {
  display: inline-block;
  margin-left: 8px;
  opacity: 0.85;
}

.drawer-hint {
  color: var(--color-light);
  opacity: 0.6;
  font-size: 12px;
  margin: 0 0 12px;
}
</style>
