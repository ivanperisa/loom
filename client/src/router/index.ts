import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'
import { safeRedirect } from '@/utils/redirect'

declare module 'vue-router' {
  interface RouteMeta {
    /** Signed-in users only; others go to the landing page and come back after signing in. */
    requiresAuth: boolean
    /** Only after onboarding (role, institution, JMBAG). */
    requiresOnboarding?: boolean
    requiresCoordinator?: boolean
    requiresAdmin?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/views/LandingView.vue'),
      meta: { requiresAuth: false }
    },
    {
      // Opens an access link; works signed in (claim) or not (guest session).
      path: '/access/:token',
      component: () => import('@/views/AccessLinkView.vue'),
      meta: { requiresAuth: false }
    },
    {
      path: '/guest/exchange/:exchangeId',
      component: () => import('@/views/GuestExchangeView.vue'),
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
      meta: { requiresAuth: true, requiresOnboarding: true, requiresCoordinator: true }
    },
    {
      path: '/admin',
      component: () => import('@/views/AdminView.vue'),
      meta: { requiresAuth: true, requiresOnboarding: true, requiresAdmin: true }
    },
    {
      path: '/:pathMatch(.*)*',
      component: () => import('@/views/NotFoundView.vue'),
      meta: { requiresAuth: false }
    }
  ]
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()
  await authStore.init()

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    return { path: '/', query: { redirect: to.fullPath } }
  }

  if (to.path === '/' && authStore.isLoggedIn) {
    return safeRedirect(to.query.redirect) ?? '/home'
  }

  if (to.meta.requiresOnboarding && !authStore.isOnboarded) {
    return '/onboarding'
  }

  if (authStore.isLoggedIn && authStore.isOnboarded && to.path === '/onboarding') {
    return '/home'
  }

  if (to.meta.requiresAdmin && !authStore.isAdmin) {
    return '/home'
  }

  if (to.meta.requiresCoordinator && !authStore.canActAsCoordinator) {
    return '/home'
  }

  if (to.path === '/home' && authStore.isLoggedIn && authStore.isOnboarded) {
    if (authStore.canActAsCoordinator) {
      return '/coordinator'
    }
  }
})

export default router
