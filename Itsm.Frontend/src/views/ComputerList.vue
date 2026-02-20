<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { ComputerRecord } from '../types/inventory'
import { fetchComputers } from '../services/api'
import { formatBytes, chassisLabel } from '../utils/format'

const computers = ref<ComputerRecord[]>([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    computers.value = await fetchComputers()
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <h1 class="text-2xl font-bold text-gray-900 mb-6">Computers</h1>

    <p v-if="loading" class="text-gray-500">Loading...</p>
    <p v-else-if="error" class="text-red-600">{{ error }}</p>
    <p v-else-if="computers.length === 0" class="text-gray-500">No computers reported yet.</p>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="rec in computers"
        :key="rec.computerName"
        class="bg-white rounded-lg border border-gray-200 p-5 hover:shadow-md transition-shadow"
      >
        <div class="flex items-start justify-between mb-3">
          <div>
            <h2 class="font-semibold text-gray-900">{{ rec.data.identity.computerName }}</h2>
            <p class="text-sm text-gray-500">{{ rec.data.identity.modelName }}</p>
          </div>
          <span class="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
            {{ chassisLabel(rec.data.identity.chassisType) }}
          </span>
        </div>

        <div class="space-y-1 text-sm text-gray-600 mb-4">
          <p>{{ rec.data.cpu.brandString }}</p>
          <p>{{ rec.data.cpu.coreCount }} cores &middot; {{ formatBytes(rec.data.memory.totalBytes) }} RAM</p>
          <p>{{ rec.data.os.description }}</p>
          <p class="text-xs text-gray-400">User: {{ rec.data.identity.loggedInUser }}</p>
        </div>

        <div class="flex gap-2">
          <RouterLink
            :to="`/computers/${encodeURIComponent(rec.computerName)}`"
            class="text-sm text-blue-600 hover:underline"
          >
            Details
          </RouterLink>
          <RouterLink
            :to="`/disk-usage/${encodeURIComponent(rec.computerName)}`"
            class="text-sm text-blue-600 hover:underline"
          >
            Disk Usage
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>
