import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/views/LandingView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/callback',
      component: () => import('@/views/CallbackView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/onboarding',
      component: () => import('@/views/OnboardingView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: false }
    },
    {
      path: '/home',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/exchange',
      component: () => import('@/views/ExchangeRedirectView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/exchange/:exchangeId',
      component: () => import('@/views/ExchangeView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/settings',
      component: () => import('@/views/SettingsView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    },
    {
      path: '/coordinator',
      component: () => import('@/views/CoordinatorDashboard.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true }
    }
  ]
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  await authStore.init()

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    return '/'
  }

  if (to.meta.requiresOnboarding && !authStore.isOnboarded) {
    return '/onboarding'
  }

  if (authStore.isLoggedIn && authStore.isOnboarded && to.path === '/onboarding') {
    return '/home'
  }
})

export default router
