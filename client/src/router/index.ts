import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/views/LandingView.vue')
    },
    {
      path: '/home',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/exchange',
      component: () => import('@/views/ExchangeView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/history',
      name: 'history',
      component: () => import('@/views/HistoryView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/onboarding',
      component: () => import('@/views/OnboardingView.vue'),
      meta: { requiresAuth: true }
    },
    { path: '/callback', component: () => import('@/views/CallbackView.vue') }
  ]
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  await authStore.init()

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    return '/'
  }

  if (authStore.isLoggedIn && !authStore.isOnboarded && to.path !== '/onboarding') {
    return '/onboarding'
  }

  if (to.meta.requiresOnboarding && !authStore.isOnboarded) {
    return '/onboarding'
  }

  if (authStore.isLoggedIn && authStore.isOnboarded && to.path === '/onboarding') {
    return '/home'
  }
})

export default router
