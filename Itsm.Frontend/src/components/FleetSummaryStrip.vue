<script setup lang="ts">
import type { ComputerRecord, AgentRecord } from '../types/inventory'
import { computed } from 'vue'
import { computeComplianceStatus, computeDiskHealth } from '../utils/format'

const props = defineProps<{
  computers: ComputerRecord[]
  agents: Map<string, AgentRecord>
}>()

const total = computed(() => props.computers.length)

const online = computed(() => {
  return props.computers.filter(c =>
    props.agents.get(c.data.identity.hardwareUuid)?.isConnected
  ).length
})

const complianceIssues = computed(() => {
  return props.computers.filter(c => {
    const s = computeComplianceStatus(c.data.firewall, c.data.encryption)
    return !s.passing
  }).length
})

const diskAlerts = computed(() => {
  return props.computers.filter(c => {
    const h = computeDiskHealth(c.data.disks)
    return h.status !== 'ok'
  }).length
})
</script>

<template>
  <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
    <div class="bg-white rounded-lg border border-gray-200 p-4">
      <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Total Devices</p>
      <p class="text-2xl font-bold text-gray-900 mt-1">{{ total }}</p>
    </div>
    <div class="bg-white rounded-lg border border-gray-200 p-4">
      <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Online</p>
      <p class="text-2xl font-bold text-green-600 mt-1">{{ online }}</p>
    </div>
    <div class="bg-white rounded-lg border border-gray-200 p-4">
      <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Compliance Issues</p>
      <p class="text-2xl font-bold mt-1" :class="complianceIssues > 0 ? 'text-red-600' : 'text-gray-900'">{{ complianceIssues }}</p>
    </div>
    <div class="bg-white rounded-lg border border-gray-200 p-4">
      <p class="text-xs font-medium text-gray-500 uppercase tracking-wide">Disk Alerts</p>
      <p class="text-2xl font-bold mt-1" :class="diskAlerts > 0 ? 'text-amber-600' : 'text-gray-900'">{{ diskAlerts }}</p>
    </div>
  </div>
</template>
