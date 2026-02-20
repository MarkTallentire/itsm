<script setup lang="ts">
import { ref, nextTick, onMounted, onUnmounted } from 'vue'
import type { LogEntry } from '../types/inventory'
import { fetchLogs, streamLogs } from '../services/api'

const props = defineProps<{
  hardwareUuid: string
  isConnected: boolean
}>()

const open = ref(false)
const logs = ref<LogEntry[]>([])
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

function startStream() {
  eventSource = streamLogs(props.hardwareUuid, (entry) => {
    logs.value.push(entry)
    if (logs.value.length > 1000) logs.value.splice(0, logs.value.length - 1000)
    scrollToBottom()
  })
}

async function toggle() {
  open.value = !open.value
  if (open.value) {
    try {
      logs.value = await fetchLogs(props.hardwareUuid)
      scrollToBottom()
    } catch {
      // May not have any logs yet
    }
    startStream()
  } else {
    eventSource?.close()
    eventSource = null
  }
}

onMounted(async () => {
  // Auto-open and start streaming when agent is connected
  if (props.isConnected) {
    open.value = true
    try {
      logs.value = await fetchLogs(props.hardwareUuid)
      scrollToBottom()
    } catch {
      // May not have any logs yet
    }
    startStream()
  }
})

onUnmounted(() => {
  eventSource?.close()
})
</script>

<template>
  <div class="fixed bottom-0 left-16 right-0 z-40">
    <!-- Toggle bar -->
    <button
      @click="toggle"
      class="w-full flex items-center justify-between px-5 py-2.5 bg-gray-900 text-gray-400 hover:bg-gray-800 transition-colors text-xs font-mono border-t border-gray-700"
    >
      <div class="flex items-center gap-2">
        <span class="text-gray-500">$</span>
        <span>agent logs</span>
        <span v-if="logs.length > 0" class="text-gray-600">({{ logs.length }})</span>
        <span v-if="open && isConnected" class="flex items-center gap-1 text-green-400">
          <span class="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse"></span>
          live
        </span>
      </div>
      <svg
        class="h-3.5 w-3.5 text-gray-500 transition-transform duration-200"
        :class="{ 'rotate-180': !open }"
        fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
      </svg>
    </button>

    <!-- Console output -->
    <div v-if="open" class="bg-gray-950 relative">
      <div
        v-if="logs.length === 0"
        class="px-5 py-6 text-center text-xs text-gray-600 font-mono"
      >
        Waiting for log output...
      </div>
      <div
        v-else
        ref="logContainer"
        @scroll="handleLogScroll"
        class="h-64 overflow-y-auto font-mono text-[11px] leading-5 p-3 scrollbar-thin"
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
            scroll to bottom
          </button>
        </div>
      </Transition>
    </div>
  </div>
</template>
