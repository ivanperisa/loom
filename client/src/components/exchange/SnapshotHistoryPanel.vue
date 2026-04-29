<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useExchangeStore } from '@/stores/exchange.store'
import LearningAgreementTable from '@/components/exchange/LearningAgreementTable.vue'
import type {
  CourseSlotResponse,
  SlotStateResponse,
  ExchangeSnapshotResponse,
  LocalSlotState,
} from '@/types/exchange.types'

const props = defineProps<{
  exchangeId: string
  currentSlotStates: SlotStateResponse[]
  slots: CourseSlotResponse[]
}>()

const { t } = useI18n()
const exchangeStore = useExchangeStore()

const selectedAId = ref<string | 'current' | null>(null)
const selectedBId = ref<string | 'current' | null>(null)
const loadingA = ref(false)
const loadingB = ref(false)

function toLocalSlotStates(states: SlotStateResponse[]): LocalSlotState[] {
  return states.map(s => ({
    courseSlotId: s.courseSlotId,
    mode: s.mode,
    mappings: s.mappings.map(m => ({
      localId: m.id,
      foreignCourseId: m.foreignCourseId,
      foreignCourseCode: m.foreignCourseCode,
      foreignCourseNameEn: m.foreignCourseNameEn,
      foreignCourseNameHr: m.foreignCourseNameHr,
      awardedEcts: m.awardedEcts,
    })),
  }))
}

function getSnapshotStates(id: string | 'current' | null): SlotStateResponse[] {
  if (!id) return []
  if (id === 'current') return props.currentSlotStates
  const snap = exchangeStore.snapshots.find(s => s.id === id)
  return snap?.data?.slotStates ?? []
}

const statesA = computed(() => getSnapshotStates(selectedAId.value))
const statesB = computed(() => getSnapshotStates(selectedBId.value))

async function selectSnapshot(id: string, side: 'A' | 'B') {
  if (id === 'current') {
    if (side === 'A') selectedAId.value = 'current'
    else selectedBId.value = 'current'
    return
  }

  const existing = exchangeStore.snapshots.find(s => s.id === id)
  if (existing && existing.data) {
    if (side === 'A') selectedAId.value = id
    else selectedBId.value = id
    return
  }

  if (side === 'A') { loadingA.value = true; selectedAId.value = id }
  else { loadingB.value = true; selectedBId.value = id }

  await exchangeStore.fetchSnapshot(props.exchangeId, id)

  if (side === 'A') loadingA.value = false
  else loadingB.value = false
}

// ── Diff algorithm ────────────────────────────────────────────────────────

interface ChangedMode {
  courseSlotId: string
  slotName: string
  oldMode: string | null
  newMode: string | null
}

interface DiffMapping {
  courseSlotId: string
  slotName: string
  foreignCourseCode: string
  foreignCourseNameEn: string
  awardedEcts: number
}

interface LaDiff {
  changedModes: ChangedMode[]
  addedMappings: DiffMapping[]
  removedMappings: DiffMapping[]
}

function computeDiff(
  statesA: SlotStateResponse[],
  statesB: SlotStateResponse[],
  slots: CourseSlotResponse[]
): LaDiff {
  const slotName = (id: string) => {
    const s = slots.find(sl => sl.id === id)
    return s ? (s.courseCode ?? s.courseName) : id
  }

  const mapA = new Map(statesA.map(s => [s.courseSlotId, s]))
  const mapB = new Map(statesB.map(s => [s.courseSlotId, s]))
  const allIds = new Set([...mapA.keys(), ...mapB.keys()])

  const changedModes: ChangedMode[] = []
  const addedMappings: DiffMapping[] = []
  const removedMappings: DiffMapping[] = []

  for (const id of allIds) {
    const a = mapA.get(id)
    const b = mapB.get(id)
    const modeA = a?.mode ?? null
    const modeB = b?.mode ?? null

    if (modeA !== modeB) {
      changedModes.push({ courseSlotId: id, slotName: slotName(id), oldMode: modeA, newMode: modeB })
    }

    const countMap = (mappings: SlotStateResponse['mappings']) => {
      const map = new Map<string, number>()
      for (const m of mappings) {
        const key = `${m.foreignCourseId}:${m.awardedEcts}`
        map.set(key, (map.get(key) ?? 0) + 1)
      }
      return map
    }

    const countsA = countMap(a?.mappings ?? [])
    const countsB = countMap(b?.mappings ?? [])
    const allKeys = new Set([...countsA.keys(), ...countsB.keys()])

    for (const key of allKeys) {
      const cntA = countsA.get(key) ?? 0
      const cntB = countsB.get(key) ?? 0
      const diff = cntB - cntA

      const allMappings = [...(a?.mappings ?? []), ...(b?.mappings ?? [])]
      const rep = allMappings.find(m => `${m.foreignCourseId}:${m.awardedEcts}` === key)
      if (!rep) continue

      const entry: DiffMapping = {
        courseSlotId: id,
        slotName: slotName(id),
        foreignCourseCode: rep.foreignCourseCode,
        foreignCourseNameEn: rep.foreignCourseNameEn,
        awardedEcts: rep.awardedEcts,
      }

      if (diff > 0) {
        for (let i = 0; i < diff; i++) addedMappings.push(entry)
      } else if (diff < 0) {
        for (let i = 0; i < -diff; i++) removedMappings.push(entry)
      }
    }
  }

  return { changedModes, addedMappings, removedMappings }
}

const diff = computed<LaDiff | null>(() => {
  if (!selectedAId.value || !selectedBId.value) return null
  return computeDiff(statesA.value, statesB.value, props.slots)
})

const hasDiff = computed(() =>
  diff.value !== null &&
  (diff.value.changedModes.length > 0 || diff.value.addedMappings.length > 0 || diff.value.removedMappings.length > 0)
)

function snapshotLabel(snap: ExchangeSnapshotResponse): string {
  const date = new Date(snap.createdAt).toLocaleString('hr-HR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
  return `${date} — ${snap.changedByName}`
}

function optionLabel(id: string | 'current'): string {
  if (id === 'current') return t('snapshot.current')
  const snap = exchangeStore.snapshots.find(s => s.id === id)
  return snap ? snapshotLabel(snap) : id
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-lg font-semibold text-light">{{ t('snapshot.title') }}</h2>

    <!-- Version selectors -->
    <div class="grid gap-4 sm:grid-cols-2">
      <div>
        <label class="mb-1 block text-xs font-semibold uppercase tracking-wide text-light/60">
          {{ t('snapshot.selectA') }}
        </label>
        <select
          class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light focus:border-primary focus:outline-none"
          :value="selectedAId ?? ''"
          @change="e => selectSnapshot((e.target as HTMLSelectElement).value, 'A')"
        >
          <option value="" disabled>— {{ t('snapshot.selectA') }} —</option>
          <option value="current">{{ t('snapshot.current') }}</option>
          <option v-for="snap in exchangeStore.snapshots" :key="snap.id" :value="snap.id">
            {{ snapshotLabel(snap) }}
          </option>
        </select>
      </div>
      <div>
        <label class="mb-1 block text-xs font-semibold uppercase tracking-wide text-light/60">
          {{ t('snapshot.selectB') }}
        </label>
        <select
          class="w-full rounded-lg border border-primary/20 bg-dark px-3 py-2 text-sm text-light focus:border-primary focus:outline-none"
          :value="selectedBId ?? ''"
          @change="e => selectSnapshot((e.target as HTMLSelectElement).value, 'B')"
        >
          <option value="" disabled>— {{ t('snapshot.selectB') }} —</option>
          <option value="current">{{ t('snapshot.current') }}</option>
          <option v-for="snap in exchangeStore.snapshots" :key="snap.id" :value="snap.id">
            {{ snapshotLabel(snap) }}
          </option>
        </select>
      </div>
    </div>

    <!-- Diff result -->
    <template v-if="selectedAId && selectedBId">
      <div v-if="loadingA || loadingB" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center">
        <span class="text-sm text-light/60">{{ t('common.loading') }}</span>
      </div>

      <template v-else-if="diff">
        <div v-if="!hasDiff" class="rounded-xl border border-primary/20 bg-dark-2 p-6 text-center">
          <p class="text-sm text-light/60">{{ t('snapshot.noDiff') }}</p>
        </div>

        <template v-else>
          <!-- Changed modes -->
          <div v-if="diff.changedModes.length > 0" class="rounded-xl border border-primary/20 bg-dark-2 p-5">
            <h3 class="mb-3 text-sm font-semibold text-light">{{ t('snapshot.changedModes') }}</h3>
            <table class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs text-light/60">
                  <th class="pb-2 font-medium">{{ t('snapshot.subject') }}</th>
                  <th class="pb-2 font-medium">{{ t('snapshot.was') }}</th>
                  <th class="pb-2 font-medium">{{ t('snapshot.became') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(cm, i) in diff.changedModes" :key="i" class="border-t border-primary/10">
                  <td class="py-1.5 font-medium text-light">{{ cm.slotName }}</td>
                  <td class="py-1.5 text-light/60">
                    {{ cm.oldMode ? t(`slotMode.${cm.oldMode}`) : '—' }}
                  </td>
                  <td class="py-1.5 text-primary-light">
                    {{ cm.newMode ? t(`slotMode.${cm.newMode}`) : '—' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Added mappings -->
          <div v-if="diff.addedMappings.length > 0" class="rounded-xl border border-green-500/20 bg-green-900/10 p-5">
            <h3 class="mb-3 text-sm font-semibold text-green-300">{{ t('snapshot.addedMappings') }}</h3>
            <ul class="space-y-1.5">
              <li v-for="(m, i) in diff.addedMappings" :key="i" class="flex items-start gap-2 text-sm">
                <span class="mt-0.5 inline-block text-green-400">
                  <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><path d="M7 2v10M2 7h10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>
                </span>
                <span class="text-light/80">
                  <span class="font-medium text-light">{{ m.slotName }}</span>
                  <span class="mx-1 text-light/40">·</span>
                  <span class="font-mono text-xs">{{ m.foreignCourseCode }}</span>
                  <span class="mx-1 text-light/40">·</span>
                  {{ m.foreignCourseNameEn }}
                  <span class="ml-1 text-xs text-light/50">({{ m.awardedEcts }} ECTS)</span>
                </span>
              </li>
            </ul>
          </div>

          <!-- Removed mappings -->
          <div v-if="diff.removedMappings.length > 0" class="rounded-xl border border-red-500/20 bg-red-900/10 p-5">
            <h3 class="mb-3 text-sm font-semibold text-red-300">{{ t('snapshot.removedMappings') }}</h3>
            <ul class="space-y-1.5">
              <li v-for="(m, i) in diff.removedMappings" :key="i" class="flex items-start gap-2 text-sm">
                <span class="mt-0.5 inline-block text-red-400">
                  <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><path d="M2 7h10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>
                </span>
                <span class="text-light/80">
                  <span class="font-medium text-light">{{ m.slotName }}</span>
                  <span class="mx-1 text-light/40">·</span>
                  <span class="font-mono text-xs">{{ m.foreignCourseCode }}</span>
                  <span class="mx-1 text-light/40">·</span>
                  {{ m.foreignCourseNameEn }}
                  <span class="ml-1 text-xs text-light/50">({{ m.awardedEcts }} ECTS)</span>
                </span>
              </li>
            </ul>
          </div>
        </template>

        <!-- Side-by-side tables -->
        <div class="grid gap-6 lg:grid-cols-2">
          <div>
            <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-light/60">
              {{ optionLabel(selectedAId!) }}
            </p>
            <LearningAgreementTable
              :slots="slots"
              :slot-states="toLocalSlotStates(statesA)"
              :exchange-id="exchangeId"
              :readonly="true"
            />
          </div>
          <div>
            <p class="mb-2 text-xs font-semibold uppercase tracking-wide text-light/60">
              {{ optionLabel(selectedBId!) }}
            </p>
            <LearningAgreementTable
              :slots="slots"
              :slot-states="toLocalSlotStates(statesB)"
              :exchange-id="exchangeId"
              :readonly="true"
            />
          </div>
        </div>
      </template>
    </template>

    <!-- Snapshot list -->
    <div class="rounded-xl border border-primary/20 bg-dark-2 p-5">
      <h3 class="mb-3 text-sm font-semibold text-light">{{ t('snapshot.title') }}</h3>
      <div v-if="exchangeStore.snapshots.length === 0" class="text-sm text-light/60">
        {{ t('snapshot.noSnapshots') }}
      </div>
      <ul v-else class="space-y-2">
        <li
          v-for="snap in exchangeStore.snapshots"
          :key="snap.id"
          class="flex items-center justify-between rounded-lg border border-primary/10 bg-dark px-4 py-2.5 text-sm"
        >
          <div>
            <span class="font-medium text-light">
              {{ new Date(snap.createdAt).toLocaleString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
            </span>
            <span class="ml-2 text-xs text-light/50">{{ t('snapshot.approvedBy') }}: {{ snap.changedByName }}</span>
          </div>
          <div class="flex gap-2">
            <button
              type="button"
              class="rounded border border-primary/30 px-2 py-0.5 text-xs text-primary-light hover:bg-primary/10 transition"
              @click="selectSnapshot(snap.id, 'A')"
            >
              A
            </button>
            <button
              type="button"
              class="rounded border border-primary/30 px-2 py-0.5 text-xs text-primary-light hover:bg-primary/10 transition"
              @click="selectSnapshot(snap.id, 'B')"
            >
              B
            </button>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>
