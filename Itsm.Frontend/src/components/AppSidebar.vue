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

      <RouterLink
        to="/monitors"
        class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-800 hover:text-white transition-colors"
        active-class="!bg-gray-800 !text-white"
      >
        <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M6 20.25h12m-7.5-3v3m3-3v3m-10.125-3h17.25c.621 0 1.125-.504 1.125-1.125V4.125c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v12c0 .621.504 1.125 1.125 1.125Z" />
        </svg>
        <span class="whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
          Monitors
        </span>
      </RouterLink>

      <RouterLink
        to="/printers"
        class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-800 hover:text-white transition-colors"
        active-class="!bg-gray-800 !text-white"
      >
        <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M6.72 13.829c-.24.03-.48.062-.72.096m.72-.096a42.415 42.415 0 0 1 10.56 0m-10.56 0L6.34 18m10.94-4.171c.24.03.48.062.72.096m-.72-.096L17.66 18m0 0 .229 2.523a1.125 1.125 0 0 1-1.12 1.227H7.231c-.662 0-1.18-.568-1.12-1.227L6.34 18m11.318 0h1.091A2.25 2.25 0 0 0 21 15.75V9.456c0-1.081-.768-2.015-1.837-2.175a48.055 48.055 0 0 0-1.913-.247M6.34 18H5.25A2.25 2.25 0 0 1 3 15.75V9.456c0-1.081.768-2.015 1.837-2.175a48.041 48.041 0 0 1 1.913-.247m10.5 0a48.536 48.536 0 0 0-10.5 0m10.5 0V3.375c0-.621-.504-1.125-1.125-1.125h-8.25c-.621 0-1.125.504-1.125 1.125v3.659M18 10.5h.008v.008H18V10.5Zm-3 0h.008v.008H15V10.5Z" />
        </svg>
        <span class="whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
          Printers
        </span>
      </RouterLink>

      <RouterLink
        to="/usb-devices"
        class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-800 hover:text-white transition-colors"
        active-class="!bg-gray-800 !text-white"
      >
        <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M13.19 8.688a4.5 4.5 0 0 1 1.242 7.244l-4.5 4.5a4.5 4.5 0 0 1-6.364-6.364l1.757-1.757m13.35-.622 1.757-1.757a4.5 4.5 0 0 0-6.364-6.364l-4.5 4.5a4.5 4.5 0 0 0 1.242 7.244" />
        </svg>
        <span class="whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
          USB Devices
        </span>
      </RouterLink>

      <RouterLink
        to="/other-assets"
        class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium hover:bg-gray-800 hover:text-white transition-colors"
        active-class="!bg-gray-800 !text-white"
      >
        <svg class="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m20.25 7.5-.625 10.632a2.25 2.25 0 0 1-2.247 2.118H6.622a2.25 2.25 0 0 1-2.247-2.118L3.75 7.5m8.25 3v6.75m0 0-3-3m3 3 3-3M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125Z" />
        </svg>
        <span class="whitespace-nowrap opacity-0 group-hover/sidebar:opacity-100 transition-opacity duration-200">
          Other Assets
        </span>
      </RouterLink>
    </nav>
  </aside>
</template>
