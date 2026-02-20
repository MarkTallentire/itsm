import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

const apiTarget = process.env.services__itsm_api__https__0
  || process.env.services__itsm_api__http__0
  || 'http://localhost:5119'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    port: parseInt(process.env.PORT || '5173'),
    proxy: {
      '/inventory': { target: apiTarget, changeOrigin: true, secure: false },
      '/agents': { target: apiTarget, changeOrigin: true, secure: false },
      '/hubs': { target: apiTarget, changeOrigin: true, secure: false, ws: true },
    },
  },
})
