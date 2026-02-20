import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    port: parseInt(process.env.PORT || '5173'),
    proxy: {
      '/inventory': {
        target: process.env.services__itsm_api__https__0
          || process.env.services__itsm_api__http__0
          || 'http://localhost:5119',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
