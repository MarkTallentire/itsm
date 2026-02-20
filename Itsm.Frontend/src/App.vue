<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="min-h-screen bg-gray-50/80">
    <nav class="bg-white border-b border-gray-200/80 px-6 py-3.5 shadow-xs sticky top-0 z-30">
      <div class="max-w-7xl mx-auto flex items-center gap-6">
        <RouterLink to="/" class="flex items-center gap-2.5 text-lg font-semibold text-gray-900 hover:text-primary-600 transition-colors">
          <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-primary-500 text-white text-xs font-bold shadow-sm">IT</span>
          ITSM Dashboard
        </RouterLink>
      </div>
    </nav>
    <main class="max-w-7xl mx-auto px-6 py-8">
      <RouterView />
    </main>
  </div>
</template>
