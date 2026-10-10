import { fileURLToPath, URL } from 'node:url'
import basicSsl from '@vitejs/plugin-basic-ssl'

import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'
import { visualizer } from 'rollup-plugin-visualizer'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  // Defaults keep the original setup (https dev server, API on its own origin).
  // docker compose sets VITE_DEV_HTTPS=false and VITE_PROXY_TARGET, so the browser talks to one origin.
  const useHttps = env.VITE_DEV_HTTPS !== 'false'
  const proxyTarget = env.VITE_PROXY_TARGET

  return {
    base: env.VITE_BASE_PATH || '/',
    plugins: [
      vue(),
      tailwindcss(),
      vueDevTools(),
      ...(useHttps ? [basicSsl()] : []),
      // `pnpm build:analyze` → dist/stats.html (what each dependency adds to the bundle).
      ...(process.env.ANALYZE ? [visualizer({ filename: 'dist/stats.html', gzipSize: true, template: process.env.ANALYZE === 'json' ? 'raw-data' : 'treemap' })] : []),
    ],
    // Compile-time flags: no Options API, no legacy vue-i18n API, no devtools hooks in production builds.
    define: {
      __VUE_OPTIONS_API__: 'false',
      __VUE_PROD_DEVTOOLS__: 'false',
      __VUE_PROD_HYDRATION_MISMATCH_DETAILS__: 'false',
      __VUE_I18N_LEGACY_API__: 'false',
      __VUE_I18N_FULL_INSTALL__: 'false',
      __INTLIFY_PROD_DEVTOOLS__: 'false',
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      https: useHttps ? {} : undefined,
      hmr: useHttps ? { protocol: 'wss', host: 'localhost' } : undefined,
      // Keep the browser's Host (localhost:5173): the API builds Google's redirect address from it, and
      // Google must come back here (through this proxy) so the cookies land on the app's own origin.
      proxy: proxyTarget
        ? {
            '/api': { target: proxyTarget, changeOrigin: false, xfwd: true },
            '/signin-oidc': { target: proxyTarget, changeOrigin: false, xfwd: true },
          }
        : undefined,
      // Bind mounts on Windows/macOS do not always deliver file events into containers.
      watch: env.VITE_USE_POLLING === 'true' ? { usePolling: true, interval: 300 } : undefined,
    },
  }
})
