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
const controllerSearch = ref('')

const filteredApps = computed(() => {
  const apps = record.value?.data.installedApps ?? []
  const sorted = [...apps].sort((a, b) => a.name.localeCompare(b.name))
  if (!appSearch.value) return sorted
  const q = appSearch.value.toLowerCase()
  return sorted.filter(a => a.name.toLowerCase().includes(q) || (a.publisher && a.publisher.toLowerCase().includes(q)))
})

const filteredControllers = computed(() => {
  const ctrls = record.value?.data.controllers ?? []
  const sorted = [...ctrls].sort((a, b) => a.name.localeCompare(b.name))
  if (!controllerSearch.value) return sorted
  const q = controllerSearch.value.toLowerCase()
  return sorted.filter(c => c.name.toLowerCase().includes(q) || (c.manufacturer && c.manufacturer.toLowerCase().includes(q)))
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
    if (agent.value?.isConnected) {
      logsOpen.value = true
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

function signalStrength(dbm: number): string {
  if (dbm >= -50) return 'Excellent'
  if (dbm >= -60) return 'Good'
  if (dbm >= -70) return 'Fair'
  return 'Weak'
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
      <!-- ============== DEVICE HEADER ============== -->
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
                <div v-if="agent" class="flex items-center gap-3 mt-1.5 text-xs text-gray-400">
                  <span class="font-medium text-gray-500">{{ agent.displayName || 'Agent' }}</span>
                  <span>v{{ agent.agentVersion }}</span>
                  <span>Last check-in: {{ timeAgo(agent.lastSeenUtc) }}</span>
                  <span>Installed: {{ new Date(agent.firstSeenUtc.endsWith('Z') ? agent.firstSeenUtc : agent.firstSeenUtc + 'Z').toLocaleDateString() }}</span>
                </div>
              </div>
            </div>
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

        <!-- Console toggle + logs -->
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

      <!-- ============== COMPLIANCE & HEALTH STRIP ============== -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
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

        <div class="bg-white rounded-lg border border-gray-200 p-4 border-l-4 border-l-primary-500">
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Uptime</p>
          <p class="text-sm font-semibold text-gray-900 mt-1">
            {{ record.data.uptime ? parseUptime(record.data.uptime.uptime) : 'Unknown' }}
          </p>
          <p v-if="record.data.uptime" class="text-xs text-gray-400 mt-0.5">
            Boot: {{ new Date(record.data.uptime.lastBootUtc.endsWith('Z') ? record.data.uptime.lastBootUtc : record.data.uptime.lastBootUtc + 'Z').toLocaleString() }}
          </p>
        </div>
      </div>

      <!-- ============== DETAIL SECTIONS ============== -->
      <div class="space-y-3 mb-6">

        <!-- Hardware -->
        <DetailSection title="Hardware" :default-open="true">
          <div class="pt-4 space-y-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <dt class="text-gray-400 font-medium">CPU</dt>
              <dd class="text-gray-700">{{ record.data.cpu.brandString }}</dd>
              <dt class="text-gray-400 font-medium">Cores / Threads</dt>
              <dd class="text-gray-700">{{ record.data.cpu.coreCount }}C{{ record.data.cpu.threadCount ? ' / ' + record.data.cpu.threadCount + 'T' : '' }}</dd>
              <template v-if="record.data.cpu.speedMHz">
                <dt class="text-gray-400 font-medium">Clock Speed</dt>
                <dd class="text-gray-700">{{ (record.data.cpu.speedMHz / 1000).toFixed(2) }} GHz</dd>
              </template>
              <dt class="text-gray-400 font-medium">Architecture</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ record.data.cpu.architecture }}</dd>
              <dt class="text-gray-400 font-medium">Total RAM</dt>
              <dd class="text-gray-700 font-semibold">{{ formatBytes(record.data.memory.totalBytes) }}</dd>
            </dl>

            <!-- Memory Modules -->
            <template v-if="record.data.memory.modules && record.data.memory.modules.length > 0">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Memory Modules</h4>
              <div class="overflow-x-auto rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Slot</th>
                      <th class="px-4 py-2.5 font-medium">Capacity</th>
                      <th class="px-4 py-2.5 font-medium">Speed</th>
                      <th class="px-4 py-2.5 font-medium">Type</th>
                      <th class="px-4 py-2.5 font-medium">Manufacturer</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="(mod, i) in record.data.memory.modules" :key="i" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-medium text-gray-700">{{ mod.slotLabel ?? 'Slot ' + i }}</td>
                      <td class="px-4 py-2.5 text-gray-700 font-semibold">{{ formatBytes(mod.capacityBytes) }}</td>
                      <td class="px-4 py-2.5 text-gray-600 font-mono text-xs">{{ mod.speedMHz ? mod.speedMHz + ' MHz' : '-' }}</td>
                      <td class="px-4 py-2.5 text-gray-600">{{ mod.type ?? '-' }}</td>
                      <td class="px-4 py-2.5 text-gray-500">{{ mod.manufacturer ?? '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

            <!-- GPUs -->
            <template v-if="record.data.gpus && record.data.gpus.length > 0">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Graphics</h4>
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

        <!-- BIOS & Motherboard -->
        <DetailSection
          v-if="record.data.bios || record.data.motherboard"
          title="BIOS & Motherboard"
          :default-open="false"
        >
          <div class="pt-4 space-y-4">
            <template v-if="record.data.bios">
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">BIOS / Firmware</h4>
              <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                <template v-if="record.data.bios.manufacturer">
                  <dt class="text-gray-400 font-medium">Manufacturer</dt>
                  <dd class="text-gray-700">{{ record.data.bios.manufacturer }}</dd>
                </template>
                <template v-if="record.data.bios.version">
                  <dt class="text-gray-400 font-medium">Version</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.bios.version }}</dd>
                </template>
                <template v-if="record.data.bios.releaseDate">
                  <dt class="text-gray-400 font-medium">Release Date</dt>
                  <dd class="text-gray-700">{{ record.data.bios.releaseDate }}</dd>
                </template>
                <template v-if="record.data.bios.serial">
                  <dt class="text-gray-400 font-medium">Serial</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.bios.serial }}</dd>
                </template>
              </dl>
            </template>

            <template v-if="record.data.motherboard">
              <hr v-if="record.data.bios" class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Motherboard</h4>
              <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                <template v-if="record.data.motherboard.manufacturer">
                  <dt class="text-gray-400 font-medium">Manufacturer</dt>
                  <dd class="text-gray-700">{{ record.data.motherboard.manufacturer }}</dd>
                </template>
                <template v-if="record.data.motherboard.product">
                  <dt class="text-gray-400 font-medium">Product</dt>
                  <dd class="text-gray-700">{{ record.data.motherboard.product }}</dd>
                </template>
                <template v-if="record.data.motherboard.serial">
                  <dt class="text-gray-400 font-medium">Serial</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.motherboard.serial }}</dd>
                </template>
                <template v-if="record.data.motherboard.version">
                  <dt class="text-gray-400 font-medium">Version</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.motherboard.version }}</dd>
                </template>
              </dl>
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
          <div class="pt-4 space-y-5">
            <p class="text-sm text-gray-500">
              Hostname: <span class="font-mono text-gray-700">{{ record.data.network.hostname }}</span>
            </p>

            <!-- Interfaces -->
            <div class="overflow-x-auto rounded-lg border border-gray-100">
              <table class="w-full text-sm">
                <thead>
                  <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                    <th class="px-4 py-2.5 font-medium">Interface</th>
                    <th class="px-4 py-2.5 font-medium">Type</th>
                    <th class="px-4 py-2.5 font-medium">MAC</th>
                    <th class="px-4 py-2.5 font-medium">IP Addresses</th>
                    <th class="px-4 py-2.5 font-medium">Speed</th>
                    <th class="px-4 py-2.5 font-medium">DHCP</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="iface in record.data.network.interfaces" :key="iface.name" class="hover:bg-gray-50/50 transition-colors">
                    <td class="px-4 py-2.5 font-medium text-gray-700">
                      {{ iface.name }}
                      <span v-if="iface.wifiSsid" class="ml-1.5 text-xs text-primary-600 font-normal">({{ iface.wifiSsid }})</span>
                    </td>
                    <td class="px-4 py-2.5 text-gray-600 text-xs">{{ iface.interfaceType ?? '-' }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ iface.macAddress }}</td>
                    <td class="px-4 py-2.5 text-gray-600">{{ iface.ipAddresses.join(', ') }}</td>
                    <td class="px-4 py-2.5 text-gray-600 text-xs font-mono">{{ iface.speedMbps ? iface.speedMbps + ' Mbps' : '-' }}</td>
                    <td class="px-4 py-2.5 text-gray-600 text-xs">{{ iface.isDhcp != null ? (iface.isDhcp ? 'Yes' : 'No') : '-' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- WiFi details for wireless interfaces -->
            <template v-for="iface in record.data.network.interfaces" :key="'wifi-' + iface.name">
              <div v-if="iface.wifiSsid" class="bg-gray-50 rounded-lg p-4">
                <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">WiFi - {{ iface.wifiSsid }}</h4>
                <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-sm">
                  <template v-if="iface.wifiFrequencyGHz">
                    <dt class="text-gray-400 font-medium">Frequency</dt>
                    <dd class="text-gray-700">{{ iface.wifiFrequencyGHz }} GHz</dd>
                  </template>
                  <template v-if="iface.wifiSignalDbm != null">
                    <dt class="text-gray-400 font-medium">Signal</dt>
                    <dd class="text-gray-700">{{ iface.wifiSignalDbm }} dBm ({{ signalStrength(iface.wifiSignalDbm) }})</dd>
                  </template>
                  <template v-if="iface.gateway">
                    <dt class="text-gray-400 font-medium">Gateway</dt>
                    <dd class="text-gray-700 font-mono text-xs">{{ iface.gateway }}</dd>
                  </template>
                  <template v-if="iface.subnetMask">
                    <dt class="text-gray-400 font-medium">Subnet</dt>
                    <dd class="text-gray-700 font-mono text-xs">{{ iface.subnetMask }}</dd>
                  </template>
                </dl>
              </div>
            </template>

            <!-- VPN Connections -->
            <template v-if="record.data.network.vpnConnections && record.data.network.vpnConnections.length > 0">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">VPN Connections</h4>
              <div class="overflow-x-auto rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Name</th>
                      <th class="px-4 py-2.5 font-medium">Type</th>
                      <th class="px-4 py-2.5 font-medium">Server</th>
                      <th class="px-4 py-2.5 font-medium">Status</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="vpn in record.data.network.vpnConnections" :key="vpn.name" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-medium text-gray-700">{{ vpn.name }}</td>
                      <td class="px-4 py-2.5 text-gray-600 text-xs">{{ vpn.type ?? '-' }}</td>
                      <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ vpn.serverAddress ?? '-' }}</td>
                      <td class="px-4 py-2.5">
                        <span
                          class="inline-flex items-center gap-1.5 text-xs font-medium"
                          :class="vpn.isConnected ? 'text-green-700' : 'text-gray-500'"
                        >
                          <span class="w-1.5 h-1.5 rounded-full" :class="vpn.isConnected ? 'bg-green-500' : 'bg-gray-300'"></span>
                          {{ vpn.isConnected ? 'Connected' : 'Disconnected' }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

            <!-- DNS -->
            <template v-if="record.data.network.dns">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">DNS Configuration</h4>
              <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                <template v-if="record.data.network.dns.domain">
                  <dt class="text-gray-400 font-medium">Domain</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.network.dns.domain }}</dd>
                </template>
                <template v-if="record.data.network.dns.servers && record.data.network.dns.servers.length > 0">
                  <dt class="text-gray-400 font-medium">Servers</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.network.dns.servers.join(', ') }}</dd>
                </template>
                <template v-if="record.data.network.dns.searchDomains && record.data.network.dns.searchDomains.length > 0">
                  <dt class="text-gray-400 font-medium">Search Domains</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ record.data.network.dns.searchDomains.join(', ') }}</dd>
                </template>
              </dl>
            </template>

            <!-- Network Drives -->
            <template v-if="record.data.network.networkDrives && record.data.network.networkDrives.length > 0">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Network Drives</h4>
              <div class="overflow-x-auto rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Local Path</th>
                      <th class="px-4 py-2.5 font-medium">Remote Path</th>
                      <th class="px-4 py-2.5 font-medium">File System</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="drive in record.data.network.networkDrives" :key="drive.localPath" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-mono text-xs text-gray-700">{{ drive.localPath }}</td>
                      <td class="px-4 py-2.5 font-mono text-xs text-gray-600">{{ drive.remotePath }}</td>
                      <td class="px-4 py-2.5 text-gray-500 text-xs">{{ drive.fileSystem ?? '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

            <!-- Listening Ports -->
            <template v-if="record.data.network.listeningPorts && record.data.network.listeningPorts.length > 0">
              <hr class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Listening Ports</h4>
              <div class="overflow-y-auto max-h-64 rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead class="sticky top-0">
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Port</th>
                      <th class="px-4 py-2.5 font-medium">Protocol</th>
                      <th class="px-4 py-2.5 font-medium">Process</th>
                      <th class="px-4 py-2.5 font-medium">PID</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="lp in record.data.network.listeningPorts" :key="lp.port + lp.protocol" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-mono text-xs font-semibold text-gray-700">{{ lp.port }}</td>
                      <td class="px-4 py-2.5 text-gray-600 text-xs">{{ lp.protocol }}</td>
                      <td class="px-4 py-2.5 text-gray-700">{{ lp.processName ?? '-' }}</td>
                      <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ lp.pid ?? '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>
          </div>
        </DetailSection>

        <!-- Security -->
        <DetailSection
          v-if="record.data.antivirus && record.data.antivirus.length > 0"
          title="Antivirus"
          :default-open="false"
          :badge="record.data.antivirus.length"
        >
          <div class="pt-4 space-y-3">
            <div v-for="av in record.data.antivirus" :key="av.name" class="bg-gray-50 rounded-lg p-4">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-semibold text-gray-800">{{ av.name }}</span>
                <span
                  class="inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full"
                  :class="av.isEnabled ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="av.isEnabled ? 'bg-green-500' : 'bg-red-500'"></span>
                  {{ av.isEnabled ? 'Active' : 'Inactive' }}
                </span>
              </div>
              <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-1.5 text-sm">
                <template v-if="av.version">
                  <dt class="text-gray-400 font-medium">Version</dt>
                  <dd class="text-gray-700 font-mono text-xs">{{ av.version }}</dd>
                </template>
                <template v-if="av.isUpToDate != null">
                  <dt class="text-gray-400 font-medium">Definitions</dt>
                  <dd :class="av.isUpToDate ? 'text-green-700' : 'text-amber-700'" class="font-medium text-xs">{{ av.isUpToDate ? 'Up to date' : 'Out of date' }}</dd>
                </template>
                <template v-if="av.expirationDate">
                  <dt class="text-gray-400 font-medium">Expires</dt>
                  <dd class="text-gray-700 text-xs">{{ av.expirationDate }}</dd>
                </template>
              </dl>
            </div>
          </div>
        </DetailSection>

        <!-- Controllers (Device Manager) -->
        <DetailSection
          v-if="record.data.controllers && record.data.controllers.length > 0"
          title="Controllers"
          :default-open="false"
          :badge="record.data.controllers.length"
        >
          <div class="pt-4">
            <div class="mb-3 flex justify-end">
              <div class="relative">
                <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
                </svg>
                <input
                  v-model="controllerSearch"
                  type="text"
                  placeholder="Search controllers..."
                  class="pl-8 pr-3 py-1.5 text-xs border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400 w-52 transition-colors"
                />
              </div>
            </div>
            <div class="overflow-y-auto max-h-80 rounded-lg border border-gray-100">
              <table class="w-full text-sm">
                <thead class="sticky top-0">
                  <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                    <th class="px-4 py-2.5 font-medium">Name</th>
                    <th class="px-4 py-2.5 font-medium">Manufacturer</th>
                    <th class="px-4 py-2.5 font-medium">Type</th>
                    <th class="px-4 py-2.5 font-medium">PCI ID</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="ctrl in filteredControllers" :key="ctrl.name + (ctrl.pciId || '')" class="hover:bg-gray-50/50 transition-colors">
                    <td class="px-4 py-2.5 font-medium text-gray-700">{{ ctrl.name }}</td>
                    <td class="px-4 py-2.5 text-gray-600">{{ ctrl.manufacturer ?? '-' }}</td>
                    <td class="px-4 py-2.5 text-gray-500 text-xs">{{ ctrl.type ?? '-' }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-400">{{ ctrl.pciId ?? '-' }}</td>
                  </tr>
                  <tr v-if="filteredControllers.length === 0">
                    <td colspan="4" class="px-4 py-6 text-center text-sm text-gray-400">No controllers matching "{{ controllerSearch }}"</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </DetailSection>

        <!-- Virtualization -->
        <DetailSection
          v-if="record.data.virtualization && (record.data.virtualization.virtualMachines.length > 0 || record.data.virtualization.dockerContainers.length > 0)"
          title="Virtualization"
          :default-open="false"
          :badge="(record.data.virtualization.virtualMachines.length || 0) + (record.data.virtualization.dockerContainers.length || 0)"
        >
          <div class="pt-4 space-y-5">
            <!-- VMs -->
            <template v-if="record.data.virtualization.virtualMachines.length > 0">
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Virtual Machines</h4>
              <div class="overflow-x-auto rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Name</th>
                      <th class="px-4 py-2.5 font-medium">State</th>
                      <th class="px-4 py-2.5 font-medium">Type</th>
                      <th class="px-4 py-2.5 font-medium">Memory</th>
                      <th class="px-4 py-2.5 font-medium">CPUs</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="vm in record.data.virtualization.virtualMachines" :key="vm.name" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-medium text-gray-700">{{ vm.name }}</td>
                      <td class="px-4 py-2.5">
                        <span
                          class="inline-flex items-center gap-1.5 text-xs font-medium"
                          :class="vm.state === 'running' || vm.state === 'Running' ? 'text-green-700' : 'text-gray-500'"
                        >
                          <span class="w-1.5 h-1.5 rounded-full" :class="vm.state === 'running' || vm.state === 'Running' ? 'bg-green-500' : 'bg-gray-300'"></span>
                          {{ vm.state ?? '-' }}
                        </span>
                      </td>
                      <td class="px-4 py-2.5 text-gray-600 text-xs">{{ vm.type ?? '-' }}</td>
                      <td class="px-4 py-2.5 text-gray-600 text-xs font-mono">{{ vm.memoryMB ? vm.memoryMB + ' MB' : '-' }}</td>
                      <td class="px-4 py-2.5 text-gray-600">{{ vm.cpuCount ?? '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

            <!-- Docker -->
            <template v-if="record.data.virtualization.dockerContainers.length > 0">
              <hr v-if="record.data.virtualization.virtualMachines.length > 0" class="border-gray-100" />
              <h4 class="text-xs font-semibold text-gray-500 uppercase tracking-wide">Docker Containers</h4>
              <div class="overflow-x-auto rounded-lg border border-gray-100">
                <table class="w-full text-sm">
                  <thead>
                    <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                      <th class="px-4 py-2.5 font-medium">Name</th>
                      <th class="px-4 py-2.5 font-medium">Image</th>
                      <th class="px-4 py-2.5 font-medium">State</th>
                      <th class="px-4 py-2.5 font-medium">Status</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100">
                    <tr v-for="dc in record.data.virtualization.dockerContainers" :key="dc.id" class="hover:bg-gray-50/50 transition-colors">
                      <td class="px-4 py-2.5 font-medium text-gray-700">{{ dc.name }}</td>
                      <td class="px-4 py-2.5 font-mono text-xs text-gray-600">{{ dc.image }}</td>
                      <td class="px-4 py-2.5">
                        <span
                          class="inline-flex items-center gap-1.5 text-xs font-medium"
                          :class="dc.state === 'running' ? 'text-green-700' : 'text-gray-500'"
                        >
                          <span class="w-1.5 h-1.5 rounded-full" :class="dc.state === 'running' ? 'bg-green-500' : 'bg-gray-300'"></span>
                          {{ dc.state }}
                        </span>
                      </td>
                      <td class="px-4 py-2.5 text-gray-500 text-xs">{{ dc.status ?? '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>
          </div>
        </DetailSection>

        <!-- Databases -->
        <DetailSection
          v-if="record.data.databases && record.data.databases.length > 0"
          title="Databases"
          :default-open="false"
          :badge="record.data.databases.length"
        >
          <div class="pt-4">
            <div class="overflow-x-auto rounded-lg border border-gray-100">
              <table class="w-full text-sm">
                <thead>
                  <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50/80">
                    <th class="px-4 py-2.5 font-medium">Name</th>
                    <th class="px-4 py-2.5 font-medium">Version</th>
                    <th class="px-4 py-2.5 font-medium">Port</th>
                    <th class="px-4 py-2.5 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="db in record.data.databases" :key="db.name" class="hover:bg-gray-50/50 transition-colors">
                    <td class="px-4 py-2.5 font-medium text-gray-700">{{ db.name }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-600">{{ db.version ?? '-' }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-600">{{ db.port ?? '-' }}</td>
                    <td class="px-4 py-2.5">
                      <span
                        class="inline-flex items-center gap-1.5 text-xs font-medium"
                        :class="db.isRunning ? 'text-green-700' : 'text-red-600'"
                      >
                        <span class="w-1.5 h-1.5 rounded-full" :class="db.isRunning ? 'bg-green-500' : 'bg-red-500'"></span>
                        {{ db.isRunning ? 'Running' : 'Stopped' }}
                      </span>
                    </td>
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
                    <th class="px-4 py-2.5 font-medium">Publisher</th>
                    <th class="px-4 py-2.5 font-medium">Version</th>
                    <th class="px-4 py-2.5 font-medium">Install Date</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100">
                  <tr v-for="app in filteredApps" :key="app.name" class="hover:bg-gray-50/50 transition-colors">
                    <td class="px-4 py-2.5 font-medium text-gray-700">{{ app.name }}</td>
                    <td class="px-4 py-2.5 text-gray-500">{{ app.publisher ?? '-' }}</td>
                    <td class="px-4 py-2.5 font-mono text-xs text-gray-500">{{ app.version }}</td>
                    <td class="px-4 py-2.5 text-gray-500 text-xs">{{ app.installDate ?? '-' }}</td>
                  </tr>
                  <tr v-if="filteredApps.length === 0">
                    <td colspan="4" class="px-4 py-6 text-center text-sm text-gray-400">No apps matching "{{ appSearch }}"</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </DetailSection>

        <!-- Operating System -->
        <DetailSection title="Operating System" :default-open="false">
          <div class="pt-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <dt class="text-gray-400 font-medium">Description</dt>
              <dd class="text-gray-700">{{ record.data.os.description }}</dd>
              <template v-if="record.data.os.version">
                <dt class="text-gray-400 font-medium">Version</dt>
                <dd class="text-gray-700">{{ record.data.os.version }}</dd>
              </template>
              <template v-if="record.data.os.buildNumber">
                <dt class="text-gray-400 font-medium">Build</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.os.buildNumber }}</dd>
              </template>
              <template v-if="record.data.os.architecture">
                <dt class="text-gray-400 font-medium">Architecture</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.os.architecture }}</dd>
              </template>
              <template v-if="record.data.os.kernelName">
                <dt class="text-gray-400 font-medium">Kernel</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.os.kernelName }}{{ record.data.os.kernelVersion ? ' ' + record.data.os.kernelVersion : '' }}</dd>
              </template>
              <template v-if="record.data.os.installDate">
                <dt class="text-gray-400 font-medium">Install Date</dt>
                <dd class="text-gray-700">{{ record.data.os.installDate }}</dd>
              </template>
              <template v-if="record.data.os.licenseKey">
                <dt class="text-gray-400 font-medium">License Key</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.os.licenseKey }}</dd>
              </template>
            </dl>
          </div>
        </DetailSection>

        <!-- Location -->
        <DetailSection
          v-if="record.data.location"
          title="Location"
          :default-open="false"
        >
          <div class="pt-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <template v-if="record.data.location.city || record.data.location.region || record.data.location.country">
                <dt class="text-gray-400 font-medium">Location</dt>
                <dd class="text-gray-700">{{ [record.data.location.city, record.data.location.region, record.data.location.country].filter(Boolean).join(', ') }}</dd>
              </template>
              <template v-if="record.data.location.timezone">
                <dt class="text-gray-400 font-medium">Timezone</dt>
                <dd class="text-gray-700">{{ record.data.location.timezone }}</dd>
              </template>
              <template v-if="record.data.location.publicIp">
                <dt class="text-gray-400 font-medium">Public IP</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.location.publicIp }}</dd>
              </template>
              <template v-if="record.data.location.latitude != null && record.data.location.longitude != null">
                <dt class="text-gray-400 font-medium">Coordinates</dt>
                <dd class="text-gray-700 font-mono text-xs">{{ record.data.location.latitude?.toFixed(4) }}, {{ record.data.location.longitude?.toFixed(4) }}</dd>
              </template>
            </dl>
          </div>
        </DetailSection>

        <!-- System -->
        <DetailSection title="System" :default-open="false">
          <div class="pt-4">
            <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
              <dt class="text-gray-400 font-medium">Chassis Type</dt>
              <dd class="text-gray-700">{{ chassisLabel(record.data.identity.chassisType) }}</dd>
              <dt class="text-gray-400 font-medium">UUID</dt>
              <dd class="text-gray-700 font-mono text-xs truncate">{{ record.data.identity.hardwareUuid }}</dd>
              <dt class="text-gray-400 font-medium">Last Updated</dt>
              <dd class="text-gray-700">{{ timeAgo(record.lastUpdatedUtc) }}</dd>
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
