<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth.store'
import AppHeader from '@/components/AppHeader.vue'
import CoordinatorDashboard from '@/views/CoordinatorDashboard.vue'
import StudentExchangeView from '@/views/StudentExchangeView.vue'

const authStore = useAuthStore()
const isCoordinator = computed(() => authStore.role === 'Coordinator')
</script>

<template>
  <main class="min-h-screen bg-[#071C2C]">
    <AppHeader />
    <CoordinatorDashboard v-if="isCoordinator" />
    <StudentExchangeView v-else />
  </main>
</template>
