<script setup lang="ts">
import { ref, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PartnerInstitutionAdminResponse } from '@/types/institution.types'
import PartnerCourseList from '@/components/admin/PartnerCourseList.vue'

const { t } = useI18n()

defineProps<{ institution: PartnerInstitutionAdminResponse; busy: boolean }>()
const emit = defineEmits<{
  edit: [institution: PartnerInstitutionAdminResponse]
  delete: [id: string]
  restore: [id: string]
  'count-changed': [delta: number]
}>()

const expanded = ref(false)
const everExpanded = ref(false)
const courseList = ref<InstanceType<typeof PartnerCourseList> | null>(null)

function expand() {
  everExpanded.value = true
  expanded.value = true
}

function toggle() {
  if (expanded.value) {
    expanded.value = false
    return
  }
  expand()
}

async function addCourse() {
  expand()
  await nextTick()
  courseList.value?.openCreate()
}
</script>

<template>
  <div class="rounded-xl border border-primary/20 bg-dark-2" :class="institution.isDeleted ? 'opacity-60' : ''">
    <!-- Institution header -->
    <div class="flex items-center gap-3 px-5 py-4">
      <button type="button" class="flex flex-1 items-center gap-3 text-left" @click="toggle">
        <svg class="h-4 w-4 flex-shrink-0 text-light/30 transition-transform" :class="expanded ? 'rotate-90' : ''" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <div class="min-w-0">
          <div class="flex flex-wrap items-baseline gap-x-2">
            <p class="font-semibold text-light">{{ institution.name }}</p>
            <span v-if="institution.erasmusCode" class="rounded border border-primary/30 bg-primary/10 px-1.5 py-0.5 font-mono text-xs text-primary-light">{{ institution.erasmusCode }}</span>
            <span v-if="institution.isDeleted" class="rounded border border-red-400/30 bg-red-500/10 px-1.5 py-0.5 text-xs text-red-300">{{ t('admin.institutions.deleted') }}</span>
          </div>
          <p v-if="institution.nameHr && institution.nameHr !== institution.name" class="text-xs text-light/40">{{ institution.nameHr }}</p>
          <p class="mt-0.5 flex items-center gap-1 text-xs text-light/40">
            <span class="flex items-center gap-1">
              <svg class="h-3 w-3 text-light/30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {{ t(`countries.${institution.country}`) }}<template v-if="institution.city">, {{ institution.city }}</template>
            </span>
          </p>
        </div>
      </button>
      <div class="flex flex-shrink-0 items-center gap-2">
        <template v-if="!institution.isDeleted">
          <button type="button" class="rounded-lg border border-primary/30 px-3 py-1.5 text-xs font-medium text-primary-light transition hover:bg-primary/10 disabled:opacity-40" :disabled="busy" @click="addCourse">
            + {{ t('admin.institutions.addCourse') }}
          </button>
          <button type="button" class="flex h-7 w-7 items-center justify-center rounded-lg border border-primary/20 text-light/60 transition hover:border-primary/50 hover:bg-primary/10 hover:text-primary-light disabled:opacity-40" :disabled="busy" :title="t('admin.institutions.editInstitution')" @click="emit('edit', institution)">
            <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </button>
          <button type="button" class="flex h-7 w-7 items-center justify-center rounded-lg border border-red-400/20 text-red-400/60 transition hover:border-red-400/50 hover:bg-red-500/10 hover:text-red-300 disabled:opacity-40" :disabled="busy" :title="t('admin.institutions.deleteInstitution')" @click="emit('delete', institution.id)">
            <svg class="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </template>
        <button v-else type="button" class="rounded-lg border border-green-400/30 px-3 py-1.5 text-xs font-medium text-green-300 transition hover:bg-green-500/10 disabled:opacity-40" :disabled="busy" @click="emit('restore', institution.id)">
          {{ t('admin.institutions.restore') }}
        </button>
      </div>
    </div>

    <PartnerCourseList
      v-if="everExpanded"
      v-show="expanded"
      ref="courseList"
      :institution-id="institution.id"
      :institution-name="institution.name"
      @count-changed="emit('count-changed', $event)"
    />
  </div>
</template>
