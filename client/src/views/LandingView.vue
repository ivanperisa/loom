<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { authService } from '@/services/auth.service'
import { useTheme } from '@/composables/useTheme'
import type { AppLocale } from '@/i18n/index'

const { t, locale } = useI18n()
const { theme, toggleTheme } = useTheme()

const locales: Array<{ code: AppLocale; flag: string; label: string }> = [
  { code: 'hr', flag: 'fi fi-hr', label: 'HR' },
  { code: 'en', flag: 'fi fi-gb', label: 'EN' },
]

function setLocale(code: AppLocale) {
  locale.value = code
  localStorage.setItem('locale', code)
}

function login() {
  authService.login()
}
</script>

<template>
  <main class="landing-root relative min-h-screen overflow-hidden text-light">
    <div class="absolute right-4 top-4 z-20 flex items-center gap-2">
      <button
        type="button"
        class="flex h-9 w-9 items-center justify-center rounded-lg text-light/60 transition hover:bg-white/10 hover:text-light"
        :title="theme === 'dark' ? t('common.lightTheme') : t('common.darkTheme')"
        @click="toggleTheme"
      >
        <svg v-if="theme === 'dark'" class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707M17.657 17.657l-.707-.707M6.343 6.343l-.707-.707M12 8a4 4 0 100 8 4 4 0 000-8z" />
        </svg>
        <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z" />
        </svg>
      </button>
      <button
        v-for="loc in locales.filter(l => l.code !== locale)"
        :key="loc.code"
        type="button"
        class="flex h-9 items-center gap-1.5 rounded-lg px-2.5 text-xs font-medium text-light/60 transition hover:bg-white/10 hover:text-light"
        @click="setLocale(loc.code)"
      >
        <span :class="loc.flag" aria-hidden="true"></span>
        {{ loc.label }}
      </button>
    </div>

    <div class="absolute inset-0 bg-dark"></div>

    <div class="relative z-10 flex min-h-screen flex-col lg:flex-row">
      <section class="relative flex w-full items-center px-6 py-12 lg:w-1/3 lg:px-12">
        <article
          class="landing-card w-full max-w-xl bg-dark p-8 lg:translate-x-[80px] lg:rounded-[0_30px_30px_0]"
        >
          <h1 class="text-4xl font-black tracking-tight text-light sm:text-5xl">{{ t('common.appName') }}</h1>
          <p class="mt-4 text-lg font-semibold text-primary-light">{{ t('landing.tagline') }}</p>
          <p class="mt-5 max-w-md text-sm leading-7 text-light sm:text-base">
            {{ t('landing.description') }}
          </p>

          <button
            @click="login"
            class="mt-8 flex items-center gap-3 rounded-full bg-white px-6 py-3 font-medium text-[#3c4043] shadow-md transition-shadow duration-200 hover:shadow-lg cursor-pointer"
          >
            <svg width="20" height="20" viewBox="0 0 24 24">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z"/>
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
            </svg>
            {{ t('auth.signInWithGoogle') }}
          </button>
        </article>
      </section>

      <section class="relative flex w-full flex-1 items-center justify-center overflow-hidden p-8">
        <svg viewBox="0 0 1000 700" class="network-svg absolute inset-0 h-full w-full opacity-70" aria-hidden="true">
          <g fill="none" stroke="#FB923C" stroke-width="2">
            <path d="M110 140 L320 200 L520 120 L760 220 L900 150" />
            <path d="M150 360 L350 300 L520 420 L730 340 L880 450" />
            <path d="M110 540 L310 470 L540 560 L760 500 L900 580" />
            <path d="M320 200 L350 300 L310 470" />
            <path d="M520 120 L520 420 L540 560" />
            <path d="M760 220 L730 340 L760 500" />
          </g>
          <g fill="var(--color-light)">
            <circle cx="110" cy="140" r="6" />
            <circle cx="320" cy="200" r="6" />
            <circle cx="520" cy="120" r="6" />
            <circle cx="760" cy="220" r="6" />
            <circle cx="900" cy="150" r="6" />
            <circle cx="150" cy="360" r="6" />
            <circle cx="350" cy="300" r="6" />
            <circle cx="520" cy="420" r="6" />
            <circle cx="730" cy="340" r="6" />
            <circle cx="880" cy="450" r="6" />
            <circle cx="110" cy="540" r="6" />
            <circle cx="310" cy="470" r="6" />
            <circle cx="540" cy="560" r="6" />
            <circle cx="760" cy="500" r="6" />
            <circle cx="900" cy="580" r="6" />
          </g>
        </svg>

        <div class="floating floating-a"></div>
        <div class="floating floating-b"></div>
        <div class="floating floating-c"></div>
        <div class="floating floating-d"></div>

        <h2 class="relative z-10 max-w-3xl text-center text-3xl font-black uppercase tracking-[0.2em] text-light sm:text-5xl">
          {{ t('landing.visualTitle') }}
        </h2>
      </section>
    </div>
  </main>
</template>


<style scoped>
.landing-card {
  border-radius: 1.5rem;
  border: 1px solid rgba(234, 88, 12, 0.2);
  box-shadow: 24px 0 45px rgba(0, 0, 0, 0.5);
  transition: box-shadow 0.25s ease, transform 0.25s ease;
}

[data-theme="light"] .landing-card {
  border-color: rgba(0, 0, 0, 0.15);
  box-shadow: 6px 6px 36px rgba(0, 0, 0, 0.18);
}

.landing-card:hover {
  transform: translateY(-2px);
  box-shadow:
    0 20px 60px rgba(0, 0, 0, 0.7),
    0 0 60px rgba(234, 88, 12, 0.15);
}

[data-theme="light"] .landing-card:hover {
  box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.12),
    0 0 40px rgba(234, 88, 12, 0.08);
}

.floating {
  position: absolute;
  border-radius: 9999px;
  filter: blur(0.3px);
  animation: float 10s ease-in-out infinite;
}

.floating-a {
  width: 180px;
  height: 180px;
  top: 10%;
  left: 12%;
  background: rgba(234, 88, 12, 0.15);
}

.floating-b {
  width: 110px;
  height: 110px;
  top: 60%;
  left: 25%;
  background: rgba(251, 146, 60, 0.12);
  animation-delay: 1.3s;
}

.floating-c {
  width: 150px;
  height: 150px;
  top: 18%;
  right: 16%;
  background: rgba(234, 88, 12, 0.08);
  animation-delay: 2.1s;
}

.floating-d {
  width: 220px;
  height: 220px;
  top: 62%;
  right: 8%;
  background: rgba(251, 146, 60, 0.1);
  animation-delay: 0.8s;
}

.network-svg path {
  stroke-dasharray: 18 12;
  animation: dash 16s linear infinite;
}

@keyframes float {
  0% {
    transform: translateY(0) scale(1);
    opacity: 0.55;
  }

  50% {
    transform: translateY(-18px) scale(1.04);
    opacity: 0.9;
  }

  100% {
    transform: translateY(0) scale(1);
    opacity: 0.55;
  }
}

@keyframes dash {
  from {
    stroke-dashoffset: 420;
  }

  to {
    stroke-dashoffset: 0;
  }
}
</style>
