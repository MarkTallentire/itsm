<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { ComputerRecord, AgentRecord } from '../types/inventory'
import { fetchComputers, fetchAgents } from '../services/api'
import { chassisLabel, chassisIcon, timeAgo, computeComplianceStatus, computeDiskHealth } from '../utils/format'
import FleetSummaryStrip from '../components/FleetSummaryStrip.vue'
import ComplianceBadge from '../components/ComplianceBadge.vue'
import DiskMiniBar from '../components/DiskMiniBar.vue'

const router = useRouter()
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

function navigateToComputer(name: string) {
  router.push(`/computers/${encodeURIComponent(name)}`)
}

function getAgent(c: ComputerRecord): AgentRecord | undefined {
  return agents.value.get(c.data.identity.hardwareUuid)
}
</script>

<template>
  <div>
    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 tracking-tight">Computers</h1>
      <p class="text-sm text-gray-500 mt-1">Manage and monitor all enrolled devices</p>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-4">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="n in 4" :key="n" class="bg-white rounded-lg border border-gray-200 p-4 space-y-2">
          <div class="skeleton h-3 w-20"></div>
          <div class="skeleton h-7 w-12"></div>
        </div>
      </div>
      <div class="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div v-for="n in 6" :key="n" class="flex items-center gap-4 px-5 py-3.5 border-b border-gray-100">
          <div class="skeleton h-4 w-4 rounded-full"></div>
          <div class="skeleton h-4 w-40"></div>
          <div class="skeleton h-4 w-24 ml-auto"></div>
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

    <!-- Fleet summary + data table -->
    <template v-else>
      <FleetSummaryStrip :computers="computers" :agents="agents" />

      <div class="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50 border-b border-gray-200">
                <th class="px-5 py-3 font-medium w-10">Status</th>
                <th class="px-5 py-3 font-medium">Device</th>
                <th class="px-5 py-3 font-medium">User</th>
                <th class="px-5 py-3 font-medium">OS</th>
                <th class="px-5 py-3 font-medium">Compliance</th>
                <th class="px-5 py-3 font-medium">Disk</th>
                <th class="px-5 py-3 font-medium">Last Seen</th>
                <th class="px-5 py-3 font-medium">Type</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr
                v-for="rec in computers"
                :key="rec.computerName"
                @click="navigateToComputer(rec.computerName)"
                class="hover:bg-gray-50 cursor-pointer transition-colors"
              >
                <!-- Status -->
                <td class="px-5 py-3">
                  <span
                    class="w-2.5 h-2.5 rounded-full inline-block"
                    :class="getAgent(rec)?.isConnected ? 'bg-green-500' : 'bg-gray-300'"
                    :title="getAgent(rec)?.isConnected ? 'Online' : 'Offline'"
                  ></span>
                </td>
                <!-- Device -->
                <td class="px-5 py-3">
                  <div class="font-medium text-gray-900">{{ rec.data.identity.computerName }}</div>
                  <div class="text-xs text-gray-500">{{ rec.data.identity.modelName }}</div>
                </td>
                <!-- User -->
                <td class="px-5 py-3 text-gray-600">{{ rec.data.identity.loggedInUser }}</td>
                <!-- OS -->
                <td class="px-5 py-3 text-gray-600 text-xs">{{ rec.data.os.description }}</td>
                <!-- Compliance -->
                <td class="px-5 py-3">
                  <ComplianceBadge :firewall="rec.data.firewall" :encryption="rec.data.encryption" />
                </td>
                <!-- Disk -->
                <td class="px-5 py-3">
                  <DiskMiniBar
                    v-if="rec.data.disks.length > 0"
                    :percent="computeDiskHealth(rec.data.disks).worstPercent"
                  />
                  <span v-else class="text-xs text-gray-400">-</span>
                </td>
                <!-- Last Seen -->
                <td class="px-5 py-3 text-xs text-gray-500">
                  <template v-if="getAgent(rec)">{{ timeAgo(getAgent(rec)!.lastSeenUtc) }}</template>
                  <template v-else>-</template>
                </td>
                <!-- Type -->
                <td class="px-5 py-3">
                  <span class="inline-flex items-center gap-1 text-xs text-gray-500">
                    <span>{{ chassisIcon(rec.data.identity.chassisType) }}</span>
                    {{ chassisLabel(rec.data.identity.chassisType) }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>
