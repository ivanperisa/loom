<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import AdminUsersTab from '@/components/admin/AdminUsersTab.vue'
import AdminInstitutionsTab from '@/components/admin/AdminInstitutionsTab.vue'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()

const tabs = [
  { key: 'users', label: () => t('admin.tabs.users') },
  { key: 'institutions', label: () => t('admin.tabs.institutions') },
] as const

type Tab = typeof tabs[number]['key']
const VALID_TABS = tabs.map((t) => t.key)
const activeTab = ref<Tab>(
  VALID_TABS.includes(route.query.tab as Tab) ? (route.query.tab as Tab) : 'users',
)

watch(activeTab, (tab) => {
  router.replace({ query: { ...route.query, tab } })
})
</script>

<template>
  <main class="min-h-screen bg-dark">
    <section class="page-container space-y-8">
      <h1 class="text-3xl font-bold text-light">{{ t('admin.title') }}</h1>

      <div class="flex gap-1 rounded-xl border border-primary/20 bg-dark-2 p-1">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          type="button"
          class="flex-1 rounded-lg px-4 py-2 text-sm font-medium transition"
          :class="activeTab === tab.key ? 'bg-primary text-white' : 'text-light/60 hover:text-light'"
          @click="activeTab = tab.key"
        >
          {{ tab.label() }}
        </button>
      </div>

      <AdminUsersTab v-if="activeTab === 'users'" />
      <AdminInstitutionsTab v-if="activeTab === 'institutions'" />
    </section>
  </main>
</template>
