<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppSidebar from './components/AppSidebar.vue'
import AppBreadcrumbs from './components/AppBreadcrumbs.vue'
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <AppSidebar />

    <!-- Main content area offset by sidebar -->
    <div class="pl-16">
      <!-- Header -->
      <header class="sticky top-0 z-20 bg-white border-b border-gray-200 px-8 py-3 flex items-center">
        <AppBreadcrumbs />
      </header>

      <!-- Content -->
      <main class="px-8 py-6 pb-20">
        <RouterView />
      </main>
    </div>
  </div>
</template>
