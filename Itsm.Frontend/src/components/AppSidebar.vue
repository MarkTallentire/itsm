<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>

<template>
  <aside
    class="fixed inset-y-0 left-0 z-30 flex flex-col bg-gray-900 text-gray-400 w-16 hover:w-60 transition-all duration-200 overflow-hidden group/sidebar"
  >
    <!-- Logo -->
    <RouterLink to="/" class="flex items-center gap-3 px-4 h-14 shrink-0 border-b border-gray-800">
      <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-primary-500 text-white text-xs font-bold">
        IT
      </span>
      <span class="text-sm font-semibold text-white whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
        ITSM
      </span>
    </RouterLink>

    <!-- Nav -->
    <nav class="flex-1 px-2 py-4 space-y-1">
      <RouterLink
        to="/"
        class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-800 hover:text-white transition-colors"
        active-class="!bg-gray-800 !text-white"
      >
        <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 17.25v1.007a3 3 0 0 1-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0 1 15 18.257V17.25m6-12V15a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 15V5.25A2.25 2.25 0 0 1 5.25 3h13.5A2.25 2.25 0 0 1 21 5.25Z" />
        </svg>
        <span class="whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
          Computers
        </span>
      </RouterLink>
    </nav>
  </aside>
</template>
