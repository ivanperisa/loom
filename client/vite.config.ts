import { fileURLToPath, URL } from 'node:url'
import basicSsl from '@vitejs/plugin-basic-ssl'

import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  // Defaults keep the original setup (https dev server, API on its own origin).
  // docker compose sets VITE_DEV_HTTPS=false and VITE_PROXY_TARGET, so the browser talks to one origin.
  const useHttps = env.VITE_DEV_HTTPS !== 'false'
  const proxyTarget = env.VITE_PROXY_TARGET

  return {
    base: env.VITE_BASE_PATH || '/',
    plugins: [vue(), tailwindcss(), vueDevTools(), ...(useHttps ? [basicSsl()] : [])],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      https: useHttps ? {} : undefined,
      hmr: useHttps ? { protocol: 'wss', host: 'localhost' } : undefined,
      proxy: proxyTarget
        ? {
            '/api': proxyTarget,
            '/signin-oidc': proxyTarget,
          }
        : undefined,
      // Bind mounts on Windows/macOS do not always deliver file events into containers.
      watch: env.VITE_USE_POLLING === 'true' ? { usePolling: true, interval: 300 } : undefined,
    },
  }
})
