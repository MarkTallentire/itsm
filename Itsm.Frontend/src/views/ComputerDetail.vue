<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import type { ComputerRecord, AgentRecord, LogEntry } from '../types/inventory'
import { fetchComputer, fetchAgent, requestUpdate, fetchLogs, streamLogs } from '../services/api'
import { formatBytes, chassisLabel, chassisIcon } from '../utils/format'

const props = defineProps<{ name: string }>()
const record = ref<ComputerRecord | null>(null)
const agent = ref<AgentRecord | null>(null)
const refreshingInventory = ref(false)
const refreshingDiskUsage = ref(false)
const toast = ref('')
const loading = ref(true)
const error = ref('')
const logs = ref<LogEntry[]>([])
const logsOpen = ref(false)
const logContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)
let eventSource: EventSource | null = null

function scrollToBottom() {
  if (logContainer.value && autoScroll.value) {
    nextTick(() => {
      logContainer.value!.scrollTop = logContainer.value!.scrollHeight
    })
  }
}

function handleLogScroll() {
  if (!logContainer.value) return
  const { scrollTop, scrollHeight, clientHeight } = logContainer.value
  autoScroll.value = scrollHeight - scrollTop - clientHeight < 40
}

function logLevelColor(level: string): string {
  switch (level.toLowerCase()) {
    case 'error': case 'critical': return 'text-red-400'
    case 'warning': return 'text-yellow-400'
    case 'information': return 'text-green-400'
    case 'debug': case 'trace': return 'text-gray-500'
    default: return 'text-gray-400'
  }
}

function logLevelShort(level: string): string {
  switch (level.toLowerCase()) {
    case 'critical': return 'CRT'
    case 'error': return 'ERR'
    case 'warning': return 'WRN'
    case 'information': return 'INF'
    case 'debug': return 'DBG'
    case 'trace': return 'TRC'
    default: return level.substring(0, 3).toUpperCase()
  }
}

function formatLogTime(utcString: string): string {
  const dateStr = utcString.endsWith('Z') ? utcString : utcString + 'Z'
  return new Date(dateStr).toLocaleTimeString()
}

async function toggleLogs() {
  logsOpen.value = !logsOpen.value
  if (logsOpen.value && agent.value) {
    try {
      logs.value = await fetchLogs(agent.value.hardwareUuid)
      scrollToBottom()
    } catch {
      // May not have any logs yet
    }
    eventSource = streamLogs(agent.value.hardwareUuid, (entry) => {
      logs.value.push(entry)
      if (logs.value.length > 1000) logs.value.splice(0, logs.value.length - 1000)
      scrollToBottom()
    })
  } else {
    eventSource?.close()
    eventSource = null
  }
}

onMounted(async () => {
  try {
    record.value = await fetchComputer(props.name)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
  if (record.value) {
    try {
      agent.value = await fetchAgent(record.value.data.identity.hardwareUuid)
    } catch {
      // Agent may not exist yet
    }
  }
})

onUnmounted(() => {
  eventSource?.close()
})

function timeAgo(utcString: string): string {
  const dateStr = utcString.endsWith('Z') ? utcString : utcString + 'Z'
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

async function refreshInventory() {
  refreshingInventory.value = true
  try {
    await requestUpdate(agent.value!.hardwareUuid, 'Inventory')
    toast.value = 'Inventory refresh requested'
    setTimeout(() => toast.value = '', 3000)
  } catch (e: any) {
    toast.value = 'Failed to request refresh'
    setTimeout(() => toast.value = '', 3000)
  } finally {
    refreshingInventory.value = false
  }
}

async function refreshDiskUsage() {
  refreshingDiskUsage.value = true
  try {
    await requestUpdate(agent.value!.hardwareUuid, 'DiskUsage')
    toast.value = 'Disk usage refresh requested'
    setTimeout(() => toast.value = '', 3000)
  } catch (e: any) {
    toast.value = 'Failed to request refresh'
    setTimeout(() => toast.value = '', 3000)
  } finally {
    refreshingDiskUsage.value = false
  }
}

function diskPercent(used: number, total: number): number {
  return total === 0 ? 0 : Math.round((used / total) * 100)
}

function diskColor(percent: number): string {
  if (percent > 90) return 'bg-red-500'
  if (percent > 70) return 'bg-amber-500'
  return 'bg-primary-500'
}

function diskTrackColor(percent: number): string {
  if (percent > 90) return 'bg-red-100'
  if (percent > 70) return 'bg-amber-100'
  return 'bg-primary-100'
}
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
    <div v-if="loading" class="space-y-6">
      <div class="flex items-center gap-3">
        <div class="skeleton h-8 w-52"></div>
        <div class="skeleton h-6 w-20 rounded-full"></div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
        <div v-for="n in 4" :key="n" class="bg-white rounded-xl border border-gray-200/80 p-5 space-y-3">
          <div class="skeleton h-5 w-32"></div>
          <div class="space-y-2">
            <div class="skeleton h-4 w-full"></div>
            <div class="skeleton h-4 w-3/4"></div>
            <div class="skeleton h-4 w-2/3"></div>
          </div>
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
      <p class="text-sm font-medium text-red-800">Failed to load computer details</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <template v-else-if="record">
      <div class="flex items-center gap-3 mb-6 animate-fade-in">
        <h1 class="text-2xl font-bold text-gray-900 tracking-tight">{{ record.data.identity.computerName }}</h1>
        <span class="inline-flex items-center gap-1 text-xs bg-gray-100 text-gray-600 px-2.5 py-1 rounded-full font-medium">
          <span>{{ chassisIcon(record.data.identity.chassisType) }}</span>
          {{ chassisLabel(record.data.identity.chassisType) }}
        </span>
      </div>

      <div v-if="agent" class="mb-6 rounded-xl border border-gray-200/80 shadow-sm animate-fade-in overflow-hidden">
        <!-- Agent info bar -->
        <div class="px-4 py-3 bg-white">
          <div class="flex items-center gap-4">
            <div class="flex items-center gap-2">
              <span class="w-2.5 h-2.5 rounded-full" :class="agent.isConnected ? 'bg-green-500' : 'bg-gray-300'"></span>
              <span class="text-sm font-semibold" :class="agent.isConnected ? 'text-green-700' : 'text-gray-500'">
                {{ agent.displayName || 'Agent' }}
              </span>
              <span class="text-xs px-1.5 py-0.5 rounded-full" :class="agent.isConnected ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'">
                {{ agent.isConnected ? 'Connected' : 'Offline' }}
              </span>
            </div>
            <div class="ml-auto flex gap-2">
              <button
                @click="refreshInventory"
                :disabled="refreshingInventory || !agent.isConnected"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <svg v-if="refreshingInventory" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                Refresh Inventory
              </button>
              <button
                @click="refreshDiskUsage"
                :disabled="refreshingDiskUsage || !agent.isConnected"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <svg v-if="refreshingDiskUsage" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                Refresh Disk Usage
              </button>
            </div>
          </div>
          <div class="flex items-center gap-4 mt-2 text-xs text-gray-400">
            <span>v{{ agent.agentVersion }}</span>
            <span>Last check-in: {{ timeAgo(agent.lastSeenUtc) }}</span>
            <span>Installed: {{ new Date(agent.firstSeenUtc.endsWith('Z') ? agent.firstSeenUtc : agent.firstSeenUtc + 'Z').toLocaleDateString() }}</span>
          </div>
        </div>

        <!-- Console toggle bar -->
        <button
          @click="toggleLogs"
          class="w-full flex items-center justify-between px-4 py-2 bg-gray-900 text-gray-400 hover:bg-gray-800 transition-colors text-xs font-mono border-t border-gray-700"
        >
          <div class="flex items-center gap-2">
            <span class="text-gray-500">$</span>
            <span>agent logs</span>
            <span v-if="logsOpen && agent.isConnected" class="flex items-center gap-1 text-green-400">
              <span class="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse"></span>
              live
            </span>
          </div>
          <svg
            class="h-3.5 w-3.5 text-gray-500 transition-transform duration-200"
            :class="{ 'rotate-180': logsOpen }"
            fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"
          >
            <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
          </svg>
        </button>

        <!-- Console output -->
        <div v-if="logsOpen" class="bg-gray-950 relative">
          <div
            v-if="logs.length === 0"
            class="px-4 py-6 text-center text-xs text-gray-600 font-mono"
          >
            Waiting for log output...
          </div>
          <div
            v-else
            ref="logContainer"
            @scroll="handleLogScroll"
            class="max-h-80 overflow-y-auto font-mono text-[11px] leading-5 p-3 scrollbar-thin"
          >
            <div
              v-for="(entry, i) in logs"
              :key="i"
              class="flex gap-2 hover:bg-white/5 px-1 rounded"
            >
              <span class="text-gray-600 whitespace-nowrap shrink-0 select-none">{{ formatLogTime(entry.timestampUtc) }}</span>
              <span class="shrink-0 w-7 text-center font-bold" :class="logLevelColor(entry.level)">{{ logLevelShort(entry.level) }}</span>
              <span class="text-gray-600 shrink-0 max-w-[120px] truncate" :title="entry.category">{{ entry.category.split('.').pop() }}</span>
              <span class="text-gray-300 break-all">{{ entry.message }}</span>
            </div>
          </div>
          <!-- Scroll-to-bottom pill -->
          <Transition name="fade">
            <div v-if="!autoScroll && logs.length > 0" class="absolute bottom-2 left-1/2 -translate-x-1/2">
              <button
                @click="autoScroll = true; scrollToBottom()"
                class="text-[10px] text-gray-400 hover:text-gray-200 font-mono px-3 py-1 rounded-full bg-gray-800 hover:bg-gray-700 border border-gray-700 transition-colors shadow-lg"
              >
                ↓ scroll to bottom
              </button>
            </div>
          </Transition>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
        <!-- Identity -->
        <section class="bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm animate-fade-in" style="animation-delay: 50ms">
          <h2 class="text-sm font-semibold text-gray-900 uppercase tracking-wide mb-4">Identity</h2>
          <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
            <dt class="text-gray-400 font-medium">Model</dt><dd class="text-gray-700">{{ record.data.identity.modelName }}</dd>
            <dt class="text-gray-400 font-medium">Serial</dt><dd class="text-gray-700 font-mono text-xs">{{ record.data.identity.serialNumber }}</dd>
            <dt class="text-gray-400 font-medium">UUID</dt><dd class="text-gray-700 font-mono text-xs truncate">{{ record.data.identity.hardwareUuid }}</dd>
            <dt class="text-gray-400 font-medium">User</dt><dd class="text-gray-700">{{ record.data.identity.loggedInUser }}</dd>
          </dl>
        </section>

        <!-- CPU & Memory -->
        <section class="bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm animate-fade-in" style="animation-delay: 100ms">
          <h2 class="text-sm font-semibold text-gray-900 uppercase tracking-wide mb-4">CPU &amp; Memory</h2>
          <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
            <dt class="text-gray-400 font-medium">CPU</dt><dd class="text-gray-700">{{ record.data.cpu.brandString }}</dd>
            <dt class="text-gray-400 font-medium">Cores</dt><dd class="text-gray-700">{{ record.data.cpu.coreCount }}</dd>
            <dt class="text-gray-400 font-medium">Architecture</dt><dd class="text-gray-700 font-mono text-xs">{{ record.data.cpu.architecture }}</dd>
            <dt class="text-gray-400 font-medium">Total RAM</dt><dd class="text-gray-700 font-semibold">{{ formatBytes(record.data.memory.totalBytes) }}</dd>
          </dl>
        </section>

        <!-- OS -->
        <section class="bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm animate-fade-in" style="animation-delay: 150ms">
          <h2 class="text-sm font-semibold text-gray-900 uppercase tracking-wide mb-4">Operating System</h2>
          <p class="text-sm text-gray-700">{{ record.data.os.description }}</p>
        </section>

        <!-- Disks -->
        <section class="bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm animate-fade-in" style="animation-delay: 200ms">
          <h2 class="text-sm font-semibold text-gray-900 uppercase tracking-wide mb-4">Disks</h2>
          <div v-for="disk in record.data.disks" :key="disk.name" class="mb-4 last:mb-0">
            <div class="flex justify-between text-sm mb-1.5">
              <span class="font-medium text-gray-700">{{ disk.name }}</span>
              <span class="text-gray-500 text-xs font-mono">
                {{ formatBytes(disk.totalBytes - disk.freeBytes) }} / {{ formatBytes(disk.totalBytes) }}
                <span class="ml-1 font-semibold" :class="diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 90 ? 'text-red-600' : diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 70 ? 'text-amber-600' : 'text-gray-600'">
                  ({{ diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) }}%)
                </span>
              </span>
            </div>
            <div class="w-full rounded-full h-2.5" :class="diskTrackColor(diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes))">
              <div
                class="h-2.5 rounded-full transition-all duration-500"
                :class="diskColor(diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes))"
                :style="{ width: diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) + '%' }"
              />
            </div>
            <p class="text-xs text-gray-400 mt-1.5">{{ disk.format }} · {{ formatBytes(disk.freeBytes) }} free</p>
          </div>
        </section>

        <!-- Network -->
        <section class="bg-white rounded-xl border border-gray-200/80 p-5 shadow-sm lg:col-span-2 animate-fade-in" style="animation-delay: 250ms">
          <h2 class="text-sm font-semibold text-gray-900 uppercase tracking-wide mb-4">Network</h2>
          <p class="text-sm text-gray-500 mb-3">Hostname: <span class="font-mono text-gray-700">{{ record.data.network.hostname }}</span></p>
          <div class="overflow-x-auto rounded-lg border border-gray-100">
            <table class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                  <th class="px-4 py-2.5 font-medium">Interface</th>
                  <th class="px-4 py-2.5 font-medium">MAC</th>
                  <th class="px-4 py-2.5 font-medium">IP Addresses</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="iface in record.data.network.interfaces" :key="iface.name" class="hover:bg-gray-50/50 transition-colors">
                  <td class="px-4 py-2.5 font-medium text-gray-700">{{ iface.name }}</td>
                  <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ iface.macAddress }}</td>
                  <td class="px-4 py-2.5 text-gray-600">{{ iface.ipAddresses.join(', ') }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>

      <div class="mt-6 animate-fade-in" style="animation-delay: 300ms">
        <RouterLink
          :to="`/disk-usage/${encodeURIComponent(record.computerName)}`"
          class="inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors"
        >
          View Disk Usage
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
          </svg>
        </RouterLink>
      </div>

      <Transition name="fade">
        <div v-if="toast" class="fixed bottom-6 right-6 px-4 py-2.5 bg-gray-900 text-white text-sm rounded-lg shadow-lg">
          {{ toast }}
        </div>
      </Transition>
    </template>
  </div>
</template>
