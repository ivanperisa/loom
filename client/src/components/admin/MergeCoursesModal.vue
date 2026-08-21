<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import type { PartnerCourseResponse } from '@/types/institution.types'
import BaseModal from '@/components/common/BaseModal.vue'

const props = defineProps<{ courses: PartnerCourseResponse[]; saving: boolean }>()
const emit = defineEmits<{ submit: [primaryId: string]; close: [] }>()

const { t } = useI18n()

const primaryId = ref(props.courses[0]!.id)
</script>

<template>
  <BaseModal max-width="max-w-md" labelled-by="merge-courses-title" @close="emit('close')">
    <div class="rounded-2xl border border-primary/20 bg-dark-2 shadow-2xl">
      <div class="flex items-center justify-between border-b border-primary/20 px-6 py-4">
        <div>
          <h3 id="merge-courses-title" class="font-semibold text-light">{{ t('admin.institutions.mergeCourses') }}</h3>
            <p class="mt-0.5 text-xs text-light/40">{{ t('admin.institutions.mergeDescription') }}</p>
          </div>
          <button type="button" class="text-light/40 transition hover:text-light" @click="emit('close')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
        <div class="space-y-2 px-6 py-5">
          <label
            v-for="course in courses"
            :key="course.id"
            class="flex cursor-pointer items-center gap-3 rounded-lg border border-hairline bg-dark px-3 py-2.5 transition hover:border-primary/30"
            :class="primaryId === course.id ? 'border-primary bg-primary/10' : ''"
          >
            <input type="radio" :value="course.id" v-model="primaryId" class="accent-primary" />
            <div class="min-w-0 flex-1">
              <div class="font-mono text-xs text-light/50">{{ course.code }}</div>
              <div class="text-sm text-light">{{ course.name }}</div>
              <div v-if="course.nameHr" class="text-xs text-light/40">{{ course.nameHr }}</div>
            </div>
            <span
              class="shrink-0 rounded px-2 py-0.5 text-[11px] font-semibold"
              :class="primaryId === course.id ? 'bg-primary/20 text-primary-light' : 'bg-danger/10 text-danger/70'"
            >{{ primaryId === course.id ? t('admin.institutions.mergeKeeps') : t('admin.institutions.mergeDeletes') }}</span>
          </label>
          <p class="pt-1 text-xs text-light/40">{{ t('admin.institutions.mergeHint') }}</p>
        </div>
        <div class="flex justify-end gap-2 border-t border-primary/20 px-6 py-4">
          <button type="button" class="rounded-lg border border-hairline px-4 py-2 text-sm text-light/60 transition hover:text-light" @click="emit('close')">{{ t('admin.institutions.cancel') }}</button>
          <button
            type="button"
            class="rounded-lg bg-primary px-5 py-2 text-sm font-semibold text-white transition hover:bg-primary-light hover:text-dark disabled:opacity-50"
            :disabled="saving"
            @click="emit('submit', primaryId)"
          >
            {{ saving ? t('common.loading') : t('admin.institutions.mergeCourses') }}
          </button>
        </div>
      </div>
  </BaseModal>
</template>
