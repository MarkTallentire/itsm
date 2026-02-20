<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { DiskUsageRecord } from '../types/inventory'
import { fetchDiskUsage } from '../services/api'
import { formatBytes } from '../utils/format'
import DirectoryTree from '../components/DirectoryTree.vue'

const props = defineProps<{ name: string }>()
const record = ref<DiskUsageRecord | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    record.value = await fetchDiskUsage(props.name)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <RouterLink to="/" class="inline-flex items-center gap-1 text-sm font-medium text-gray-500 hover:text-primary-600 transition-colors mb-6">
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
      </svg>
      All Computers
    </RouterLink>

    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-4">
      <div class="space-y-1">
        <div class="skeleton h-7 w-72"></div>
        <div class="skeleton h-4 w-48"></div>
      </div>
      <div class="bg-white rounded-xl border border-gray-200/80 overflow-hidden">
        <div class="bg-gray-50/80 px-4 py-3 border-b border-gray-100 flex gap-8">
          <div class="skeleton h-4 w-16"></div>
          <div class="skeleton h-4 w-12 ml-auto"></div>
          <div class="skeleton h-4 w-24"></div>
        </div>
        <div v-for="n in 8" :key="n" class="px-4 py-2.5 border-b border-gray-50 flex items-center gap-4">
          <div class="skeleton h-4 w-4" :style="{ marginLeft: `${(n % 3) * 20}px` }"></div>
          <div class="skeleton h-4 flex-1 max-w-xs"></div>
          <div class="skeleton h-4 w-16 ml-auto"></div>
          <div class="skeleton h-2 w-32 rounded-full"></div>
        </div>
      </div>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
      <div class="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-red-100">
        <svg class="h-5 w-5 text-red-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z" />
        </svg>
      </div>
      <p class="text-sm font-medium text-red-800">Failed to load disk usage</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <template v-else-if="record">
      <div class="mb-6 animate-fade-in">
        <h1 class="text-2xl font-bold text-gray-900 tracking-tight mb-1">Disk Usage</h1>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-gray-500">
          <span class="font-medium text-gray-700">{{ record.data.computerName }}</span>
          <span class="text-gray-300">|</span>
          <span>Scanned {{ new Date(record.data.scannedAtUtc).toLocaleString() }}</span>
          <span class="text-gray-300">|</span>
          <span>Min size: {{ formatBytes(record.data.minimumSizeBytes) }}</span>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-gray-200/80 overflow-hidden shadow-sm animate-fade-in" style="animation-delay: 100ms">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80 border-b border-gray-100">
              <th class="px-4 py-2.5 font-medium">Path</th>
              <th class="px-4 py-2.5 font-medium w-28 text-right">Size</th>
              <th class="px-4 py-2.5 font-medium w-48">Usage</th>
            </tr>
          </thead>
          <tbody>
            <DirectoryTree
              v-for="root in record.data.roots"
              :key="root.path"
              :node="root"
              :maxBytes="record.data.roots.reduce((max, r) => Math.max(max, r.sizeBytes), 0)"
              :depth="0"
            />
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
