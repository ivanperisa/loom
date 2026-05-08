<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from '@/components/AppHeader.vue'
import ToastContainer from '@/components/common/ToastContainer.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'

const route = useRoute()
const showHeader = computed(() => route.meta.requiresAuth === true && route.path !== '/onboarding')
</script>

<template>
  <AppHeader v-if="showHeader" />
  <router-view />
  <ToastContainer />
  <ConfirmDialog />
</template>
