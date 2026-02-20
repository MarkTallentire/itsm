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
    <RouterLink to="/" class="text-sm text-blue-600 hover:underline mb-4 inline-block">&larr; All Computers</RouterLink>

    <p v-if="loading" class="text-gray-500">Loading...</p>
    <p v-else-if="error" class="text-red-600">{{ error }}</p>

    <template v-else-if="record">
      <h1 class="text-2xl font-bold text-gray-900 mb-1">Disk Usage — {{ record.data.computerName }}</h1>
      <p class="text-sm text-gray-500 mb-6">
        Scanned {{ new Date(record.data.scannedAtUtc).toLocaleString() }}
        &middot; Min size: {{ formatBytes(record.data.minimumSizeBytes) }}
      </p>

      <div class="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <table class="w-full text-sm">
          <thead>
            <tr class="text-left text-gray-500 border-b bg-gray-50">
              <th class="px-4 py-2 font-medium">Path</th>
              <th class="px-4 py-2 font-medium w-32">Size</th>
              <th class="px-4 py-2 font-medium w-48">Bar</th>
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
