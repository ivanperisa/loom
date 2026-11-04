<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter, useRoute } from 'vue-router'
import AdminUsersTab from '@/components/admin/AdminUsersTab.vue'
import AdminInstitutionsTab from '@/components/admin/AdminInstitutionsTab.vue'
import TabBar, { tabId, tabPanelId } from '@/components/common/TabBar.vue'

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

      <TabBar v-model="activeTab" id-prefix="admin" variant="pill" :tabs="tabs.map((tab) => ({ key: tab.key, label: tab.label() }))" />

      <div :id="tabPanelId('admin', activeTab)" role="tabpanel" :aria-labelledby="tabId('admin', activeTab)">
        <AdminUsersTab v-if="activeTab === 'users'" />
        <AdminInstitutionsTab v-if="activeTab === 'institutions'" />
      </div>
    </section>
  </main>
</template>
