<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue'
import { RouterLink } from 'vue-router'
import type { ComputerRecord, AgentRecord, LogEntry } from '../types/inventory'
import { fetchComputer, fetchAgent, requestUpdate, fetchLogs, streamLogs } from '../services/api'
import {
  formatBytes, chassisLabel, chassisIcon, timeAgo, parseUptime,
  computeComplianceStatus, computeDiskHealth, diskPercent,
} from '../utils/format'
import DetailSection from '../components/DetailSection.vue'

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

const appSearch = ref('')

const filteredApps = computed(() => {
  const apps = record.value?.data.installedApps ?? []
  const sorted = [...apps].sort((a, b) => a.name.localeCompare(b.name))
  if (!appSearch.value) return sorted
  const q = appSearch.value.toLowerCase()
  return sorted.filter(a => a.name.toLowerCase().includes(q))
})

const compliance = computed(() => {
  if (!record.value) return { passing: true, issues: [] }
  return computeComplianceStatus(record.value.data.firewall, record.value.data.encryption)
})

const diskHealth = computed(() => {
  if (!record.value) return { worstPercent: 0, status: 'ok' as const }
  return computeDiskHealth(record.value.data.disks)
})

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

async function refreshInventory() {
  refreshingInventory.value = true
  try {
    await requestUpdate(agent.value!.hardwareUuid, 'Inventory')
    toast.value = 'Inventory refresh requested'
    setTimeout(() => toast.value = '', 3000)
  } catch {
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
  } catch {
    toast.value = 'Failed to request refresh'
    setTimeout(() => toast.value = '', 3000)
  } finally {
    refreshingDiskUsage.value = false
  }
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

function batteryBarColor(percent: number): string {
  if (percent <= 20) return 'bg-red-500'
  if (percent <= 50) return 'bg-amber-500'
  return 'bg-green-500'
}

function batteryTrackColor(percent: number): string {
  if (percent <= 20) return 'bg-red-100'
  if (percent <= 50) return 'bg-amber-100'
  return 'bg-green-100'
}
</script>

<template>
  <div>
    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-6">
      <div class="bg-white rounded-lg border border-gray-200 p-6">
        <div class="flex items-center gap-4">
          <div class="skeleton h-12 w-12 rounded-lg"></div>
          <div class="space-y-2">
            <div class="skeleton h-6 w-48"></div>
            <div class="skeleton h-4 w-32"></div>
          </div>
        </div>
      </div>
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="n in 4" :key="n" class="bg-white rounded-lg border border-gray-200 p-4 space-y-2">
          <div class="skeleton h-3 w-16"></div>
          <div class="skeleton h-5 w-24"></div>
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
      <!-- ============== TIER 0: Device Header Banner ============== -->
      <div class="bg-white rounded-lg border border-gray-200 mb-6 overflow-hidden">
        <div class="p-6">
          <div class="flex flex-col sm:flex-row sm:items-center gap-4">
            <div class="flex items-center gap-4 flex-1 min-w-0">
              <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-gray-100 text-2xl">
                {{ chassisIcon(record.data.identity.chassisType) }}
              </div>
              <div class="min-w-0">
                <div class="flex items-center gap-2.5">
                  <h1 class="text-xl font-bold text-gray-900 truncate">{{ record.data.identity.computerName }}</h1>
                  <span
                    v-if="agent"
                    class="inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full shrink-0"
                    :class="agent.isConnected
                      ? 'bg-green-50 text-green-700'
                      : 'bg-gray-100 text-gray-500'"
                  >
                    <span class="w-1.5 h-1.5 rounded-full" :class="agent.isConnected ? 'bg-green-500' : 'bg-gray-400'"></span>
                    {{ agent.isConnected ? 'Online' : 'Offline' }}
                  </span>
                </div>
                <div class="flex items-center gap-3 mt-1 text-sm text-gray-500 flex-wrap">
                  <span>{{ record.data.identity.modelName }}</span>
                  <span class="text-gray-300">|</span>
                  <span class="font-mono text-xs">{{ record.data.identity.serialNumber }}</span>
                  <span class="text-gray-300">|</span>
                  <span>{{ record.data.identity.loggedInUser }}</span>
                  <span class="text-gray-300">|</span>
                  <span>{{ record.data.os.description }}</span>
                </div>
                <!-- Agent info -->
                <div v-if="agent" class="flex items-center gap-3 mt-1.5 text-xs text-gray-400">
                  <span class="font-medium text-gray-500">{{ agent.displayName || 'Agent' }}</span>
                  <span>v{{ agent.agentVersion }}</span>
                  <span>Last check-in: {{ timeAgo(agent.lastSeenUtc) }}</span>
                  <span>Installed: {{ new Date(agent.firstSeenUtc.endsWith('Z') ? agent.firstSeenUtc : agent.firstSeenUtc + 'Z').toLocaleDateString() }}</span>
                </div>
              </div>
            </div>
            <!-- Action buttons -->
            <div v-if="agent" class="flex gap-2 shrink-0">
              <button
                @click="refreshInventory"
                :disabled="refreshingInventory || !agent.isConnected"
                class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <svg v-if="refreshingInventory" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                Refresh Inventory
              </button>
              <button
                @click="refreshDiskUsage"
                :disabled="refreshingDiskUsage || !agent.isConnected"
                class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
              >
                <svg v-if="refreshingDiskUsage" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                Refresh Disk Usage
              </button>
            </div>
          </div>
        </div>

        <!-- Console toggle + logs inside the header card -->
        <template v-if="agent">
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
            <Transition name="fade">
              <div v-if="!autoScroll && logs.length > 0" class="absolute bottom-2 left-1/2 -translate-x-1/2">
                <button
                  @click="autoScroll = true; scrollToBottom()"
                  class="text-[10px] text-gray-400 hover:text-gray-200 font-mono px-3 py-1 rounded-full bg-gray-800 hover:bg-gray-700 border border-gray-700 transition-colors shadow-lg"
                >
                  scroll to bottom
                </button>
              </div>
            </Transition>
          </div>
        </template>
      </div>

      <!-- ============== TIER 1: Compliance & Health Strip ============== -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <!-- Firewall -->
        <div
          class="bg-white rounded-lg border border-gray-200 p-4 border-l-4"
          :class="record.data.firewall?.isEnabled ? 'border-l-green-500' : 'border-l-red-500'"
        >
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Firewall</p>
          <p class="text-sm font-semibold mt-1" :class="record.data.firewall?.isEnabled ? 'text-green-700' : 'text-red-700'">
            {{ record.data.firewall?.isEnabled ? 'Enabled' : 'Disabled' }}
          </p>
          <p v-if="record.data.firewall?.stealthMode != null" class="text-xs text-gray-400 mt-0.5">
            Stealth: {{ record.data.firewall.stealthMode ? 'On' : 'Off' }}
          </p>
        </div>

        <!-- Encryption -->
        <div
          class="bg-white rounded-lg border border-gray-200 p-4 border-l-4"
          :class="record.data.encryption?.isEnabled ? 'border-l-green-500' : 'border-l-red-500'"
        >
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Encryption</p>
          <p class="text-sm font-semibold mt-1" :class="record.data.encryption?.isEnabled ? 'text-green-700' : 'text-red-700'">
            {{ record.data.encryption?.isEnabled ? 'Enabled' : 'Disabled' }}
          </p>
          <p v-if="record.data.encryption?.method" class="text-xs text-gray-400 mt-0.5">
            {{ record.data.encryption.method }}
          </p>
        </div>

        <!-- Disk Health -->
        <div
          class="bg-white rounded-lg border border-gray-200 p-4 border-l-4"
          :class="{
            'border-l-red-500': diskHealth.status === 'critical',
            'border-l-amber-500': diskHealth.status === 'warning',
            'border-l-green-500': diskHealth.status === 'ok',
          }"
        >
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Disk Health</p>
          <p
            class="text-sm font-semibold mt-1"
            :class="{
              'text-red-700': diskHealth.status === 'critical',
              'text-amber-700': diskHealth.status === 'warning',
              'text-green-700': diskHealth.status === 'ok',
            }"
          >
            {{ diskHealth.worstPercent }}% used
          </p>
          <p class="text-xs text-gray-400 mt-0.5">
            {{ diskHealth.status === 'ok' ? 'Healthy' : diskHealth.status === 'warning' ? 'Getting full' : 'Critical' }}
          </p>
        </div>

        <!-- Uptime -->
        <div
          class="bg-white rounded-lg border border-gray-200 p-4 border-l-4 border-l-primary-500"
        >
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Uptime</p>
          <p class="text-sm font-semibold text-gray-900 mt-1">
            {{ record.data.uptime ? parseUptime(record.data.uptime.uptime) : 'Unknown' }}
          </p>
          <p v-if="record.data.uptime" class="text-xs text-gray-400 mt-0.5">
            Boot: {{ new Date(record.data.uptime.lastBootUtc.endsWith('Z') ? record.data.uptime.lastBootUtc : record.data.uptime.lastBootUtc + 'Z').toLocaleString() }}
          </p>
        </div>
      </div>

      <!-- ============== TIER 2: Collapsible Sections ============== -->
      <div class="space-y-3 mb-6">
        <!-- Hardware -->
        <DetailSection title="Hardware" :default-open="true">
          <div class="pt-4 space-y-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <dt class="text-gray-400 font-medium">CPU</dt>
              <dd class="text-gray-700">{{ record.data.cpu.brandString }}</dd>
              <dt class="text-gray-400 font-medium">Cores</dt>
              <dd class="text-gray-700">{{ record.data.cpu.coreCount }}</dd>
              <dt class="text-gray-400 font-medium">Architecture</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ record.data.cpu.architecture }}</dd>
              <dt class="text-gray-400 font-medium">Total RAM</dt>
              <dd class="text-gray-700 font-semibold">{{ formatBytes(record.data.memory.totalBytes) }}</dd>
            </dl>
            <!-- GPUs -->
            <template v-if="record.data.gpus && record.data.gpus.length > 0">
              <hr class="border-gray-100" />
              <div v-for="(gpu, i) in record.data.gpus" :key="i">
                <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                  <dt class="text-gray-400 font-medium">GPU</dt>
                  <dd class="text-gray-700">{{ gpu.name }}</dd>
                  <dt class="text-gray-400 font-medium">Vendor</dt>
                  <dd class="text-gray-700">{{ gpu.vendor }}</dd>
                  <template v-if="gpu.vramBytes != null">
                    <dt class="text-gray-400 font-medium">VRAM</dt>
                    <dd class="text-gray-700 font-semibold">{{ formatBytes(gpu.vramBytes) }}</dd>
                  </template>
                  <template v-if="gpu.driverVersion">
                    <dt class="text-gray-400 font-medium">Driver</dt>
                    <dd class="text-gray-700 font-mono text-xs">{{ gpu.driverVersion }}</dd>
                  </template>
                </dl>
                <hr v-if="i < record.data.gpus.length - 1" class="my-3 border-gray-100" />
              </div>
            </template>
          </div>
        </DetailSection>

        <!-- Storage -->
        <DetailSection title="Storage" :default-open="true">
          <div class="pt-4 space-y-4">
            <div v-for="disk in record.data.disks" :key="disk.name">
              <div class="flex justify-between text-sm mb-1.5">
                <span class="font-medium text-gray-700">{{ disk.name }}</span>
                <span class="text-gray-500 text-xs font-mono">
                  {{ formatBytes(disk.totalBytes - disk.freeBytes) }} / {{ formatBytes(disk.totalBytes) }}
                  <span
                    class="ml-1 font-semibold"
                    :class="diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 90 ? 'text-red-600' : diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 70 ? 'text-amber-600' : 'text-gray-600'"
                  >
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
        </DetailSection>

        <!-- Network -->
        <DetailSection title="Network" :default-open="false">
          <div class="pt-4">
            <p class="text-sm text-gray-500 mb-3">
              Hostname: <span class="font-mono text-gray-700">{{ record.data.network.hostname }}</span>
            </p>
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
          </div>
        </DetailSection>

        <!-- Battery -->
        <DetailSection
          v-if="record.data.battery && record.data.battery.isPresent"
          title="Battery"
          :default-open="true"
        >
          <div class="pt-4">
            <template v-if="record.data.battery.chargePercent != null">
              <div class="flex justify-between text-sm mb-1.5">
                <span class="font-medium text-gray-700">Charge</span>
                <span class="text-gray-600 font-semibold">{{ Math.round(record.data.battery.chargePercent) }}%</span>
              </div>
              <div class="w-full rounded-full h-2.5" :class="batteryTrackColor(record.data.battery.chargePercent)">
                <div
                  class="h-2.5 rounded-full transition-all duration-500"
                  :class="batteryBarColor(record.data.battery.chargePercent)"
                  :style="{ width: record.data.battery.chargePercent + '%' }"
                />
              </div>
            </template>
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm mt-4">
              <template v-if="record.data.battery.isCharging != null">
                <dt class="text-gray-400 font-medium">Status</dt>
                <dd class="text-gray-700">
                  <span class="inline-flex items-center gap-1.5">
                    <span class="w-2 h-2 rounded-full" :class="record.data.battery.isCharging ? 'bg-green-500' : 'bg-gray-300'"></span>
                    {{ record.data.battery.isCharging ? 'Charging' : 'On Battery' }}
                  </span>
                </dd>
              </template>
              <template v-if="record.data.battery.healthPercent != null">
                <dt class="text-gray-400 font-medium">Health</dt>
                <dd class="text-gray-700 font-semibold">{{ Math.round(record.data.battery.healthPercent) }}%</dd>
              </template>
              <template v-if="record.data.battery.cycleCount != null">
                <dt class="text-gray-400 font-medium">Cycle Count</dt>
                <dd class="text-gray-700">{{ record.data.battery.cycleCount }}</dd>
              </template>
              <template v-if="record.data.battery.condition">
                <dt class="text-gray-400 font-medium">Condition</dt>
                <dd class="text-gray-700">{{ record.data.battery.condition }}</dd>
              </template>
            </dl>
          </div>
        </DetailSection>

        <!-- Installed Software -->
        <DetailSection
          v-if="record.data.installedApps && record.data.installedApps.length > 0"
          title="Installed Software"
          :default-open="false"
          :badge="record.data.installedApps.length"
        >
          <div class="pt-4">
            <div class="mb-3 flex justify-end">
              <div class="relative">
                <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
                </svg>
                <input
                  v-model="appSearch"
                  type="text"
                  placeholder="Search apps..."
                  class="pl-8 pr-3 py-1.5 text-xs border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400 w-52 transition-colors"
                />
              </div>
            </div>
            <div class="overflow-y-auto max-h-96 rounded-lg border border-gray-100">
              <table class="w-full text-sm">
                <thead class="sticky top-0">
                  <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                    <th class="px-4 py-2.5 font-medium">Name</th>
                    <th class="px-4 py-2.5 font-medium">Version</th>
                    <th class="px-4 py-2.5 font-medium">Install Date</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="app in filteredApps" :key="app.name" class="hover:bg-gray-50/50 transition-colors">
                    <td class="px-4 py-2.5 font-medium text-gray-700">{{ app.name }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ app.version }}</td>
                    <td class="px-4 py-2.5 text-gray-500 text-xs">{{ app.installDate ?? '-' }}</td>
                  </tr>
                  <tr v-if="filteredApps.length === 0">
                    <td colspan="3" class="px-4 py-6 text-center text-sm text-gray-400">No apps matching "{{ appSearch }}"</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </DetailSection>

        <!-- System -->
        <DetailSection title="System" :default-open="false">
          <div class="pt-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <template v-if="record.data.os.version">
                <dt class="text-gray-400 font-medium">OS Version</dt>
                <dd class="text-gray-700">{{ record.data.os.version }}</dd>
              </template>
              <template v-if="record.data.os.buildNumber">
                <dt class="text-gray-400 font-medium">Build</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.os.buildNumber }}</dd>
              </template>
              <dt class="text-gray-400 font-medium">UUID</dt>
              <dd class="text-gray-700 font-mono text-xs truncate">{{ record.data.identity.hardwareUuid }}</dd>
            </dl>
          </div>
        </DetailSection>
      </div>

      <!-- Toast -->
      <Transition name="fade">
        <div v-if="toast" class="fixed bottom-6 right-6 px-4 py-2.5 bg-gray-900 text-white text-sm rounded-lg shadow-lg z-50">
          {{ toast }}
        </div>
      </Transition>
    </template>
  </div>
</template>
