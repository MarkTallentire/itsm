<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { ComputerRecord, AgentRecord } from '../types/inventory'
import { fetchComputers, fetchAgents } from '../services/api'
import { formatBytes, chassisLabel, chassisIcon } from '../utils/format'

const computers = ref<ComputerRecord[]>([])
const agents = ref<Map<string, AgentRecord>>(new Map())
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const [computerList, agentList] = await Promise.all([
      fetchComputers(),
      fetchAgents().catch(() => [] as AgentRecord[]),
    ])
    computers.value = computerList
    agents.value = new Map(agentList.map(a => [a.hardwareUuid, a]))
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900 tracking-tight">Computers</h1>
      <p class="text-sm text-gray-500 mt-1">Manage and monitor all enrolled devices</p>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
      <div v-for="n in 6" :key="n" class="bg-white rounded-xl border border-gray-200/80 p-5 space-y-3">
        <div class="flex items-start justify-between">
          <div class="space-y-2">
            <div class="skeleton h-5 w-36"></div>
            <div class="skeleton h-4 w-24"></div>
          </div>
          <div class="skeleton h-6 w-16 rounded-full"></div>
        </div>
        <div class="space-y-2 pt-1">
          <div class="skeleton h-3.5 w-full"></div>
          <div class="skeleton h-3.5 w-48"></div>
          <div class="skeleton h-3.5 w-40"></div>
        </div>
        <div class="flex gap-3 pt-2">
          <div class="skeleton h-4 w-14"></div>
          <div class="skeleton h-4 w-20"></div>
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
      <p class="text-sm font-medium text-red-800">Failed to load computers</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="computers.length === 0" class="rounded-xl border border-gray-200 bg-white p-12 text-center">
      <div class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-gray-100">
        <svg class="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 17.25v1.007a3 3 0 0 1-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0 1 15 18.257V17.25m6-12V15a2.25 2.25 0 0 1-2.25 2.25H5.25A2.25 2.25 0 0 1 3 15V5.25A2.25 2.25 0 0 1 5.25 3h13.5A2.25 2.25 0 0 1 21 5.25Z" />
        </svg>
      </div>
      <p class="text-sm font-medium text-gray-900">No computers reported yet</p>
      <p class="text-sm text-gray-500 mt-1">Devices will appear here once agents start reporting in.</p>
    </div>

    <!-- Computer cards -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
      <RouterLink
        v-for="(rec, i) in computers"
        :key="rec.computerName"
        :to="`/computers/${encodeURIComponent(rec.computerName)}`"
        class="group bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm hover:shadow-md hover:border-gray-300 transition-all duration-200 animate-fade-in block"
        :style="{ animationDelay: `${i * 50}ms` }"
      >
        <div class="flex items-start justify-between mb-3">
          <div>
            <h2 class="font-semibold text-gray-900 group-hover:text-primary-600 transition-colors flex items-center gap-2">
              <span class="w-2 h-2 rounded-full" :class="agents.get(rec.data.identity.hardwareUuid)?.isConnected ? 'bg-green-500' : 'bg-gray-300'"></span>
              {{ rec.data.identity.computerName }}
            </h2>
            <p class="text-sm text-gray-500">{{ rec.data.identity.modelName }}</p>
          </div>
          <span class="inline-flex items-center gap-1 text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded-full font-medium">
            <span>{{ chassisIcon(rec.data.identity.chassisType) }}</span>
            {{ chassisLabel(rec.data.identity.chassisType) }}
          </span>
        </div>

        <div class="space-y-1.5 text-sm text-gray-600 mb-4">
          <p class="truncate">{{ rec.data.cpu.brandString }}</p>
          <p>{{ rec.data.cpu.coreCount }} cores · {{ formatBytes(rec.data.memory.totalBytes) }} RAM</p>
          <p class="text-gray-500">{{ rec.data.os.description }}</p>
          <p class="text-xs text-gray-400">User: {{ rec.data.identity.loggedInUser }}</p>
          <p v-if="agents.get(rec.data.identity.hardwareUuid)?.displayName" class="text-xs text-gray-400">Agent: {{ agents.get(rec.data.identity.hardwareUuid)!.displayName }}</p>
        </div>

        <div class="flex gap-3 pt-3 border-t border-gray-100">
          <span class="text-xs font-medium text-primary-600 group-hover:text-primary-700 transition-colors">
            View Details
          </span>
          <RouterLink
            :to="`/disk-usage/${encodeURIComponent(rec.computerName)}`"
            class="text-xs font-medium text-gray-400 hover:text-primary-600 transition-colors"
            @click.stop
          >
            Disk Usage
          </RouterLink>
        </div>
      </RouterLink>
    </div>
  </div>
</template>
