<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { ComputerRecord } from '../types/inventory'
import { fetchComputer } from '../services/api'
import { formatBytes, chassisLabel } from '../utils/format'

const props = defineProps<{ name: string }>()
const record = ref<ComputerRecord | null>(null)
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    record.value = await fetchComputer(props.name)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

function diskPercent(used: number, total: number): number {
  return total === 0 ? 0 : Math.round((used / total) * 100)
}
</script>

<template>
  <div>
    <RouterLink to="/" class="text-sm text-blue-600 hover:underline mb-4 inline-block">&larr; All Computers</RouterLink>

    <p v-if="loading" class="text-gray-500">Loading...</p>
    <p v-else-if="error" class="text-red-600">{{ error }}</p>

    <template v-else-if="record">
      <div class="flex items-center gap-3 mb-6">
        <h1 class="text-2xl font-bold text-gray-900">{{ record.data.identity.computerName }}</h1>
        <span class="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded">
          {{ chassisLabel(record.data.identity.chassisType) }}
        </span>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Identity -->
        <section class="bg-white rounded-lg border border-gray-200 p-5">
          <h2 class="font-semibold text-gray-900 mb-3">Identity</h2>
          <dl class="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
            <dt class="text-gray-500">Model</dt><dd>{{ record.data.identity.modelName }}</dd>
            <dt class="text-gray-500">Serial</dt><dd>{{ record.data.identity.serialNumber }}</dd>
            <dt class="text-gray-500">UUID</dt><dd class="truncate">{{ record.data.identity.hardwareUuid }}</dd>
            <dt class="text-gray-500">User</dt><dd>{{ record.data.identity.loggedInUser }}</dd>
          </dl>
        </section>

        <!-- CPU & Memory -->
        <section class="bg-white rounded-lg border border-gray-200 p-5">
          <h2 class="font-semibold text-gray-900 mb-3">CPU &amp; Memory</h2>
          <dl class="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
            <dt class="text-gray-500">CPU</dt><dd>{{ record.data.cpu.brandString }}</dd>
            <dt class="text-gray-500">Cores</dt><dd>{{ record.data.cpu.coreCount }}</dd>
            <dt class="text-gray-500">Architecture</dt><dd>{{ record.data.cpu.architecture }}</dd>
            <dt class="text-gray-500">Total RAM</dt><dd>{{ formatBytes(record.data.memory.totalBytes) }}</dd>
          </dl>
        </section>

        <!-- OS -->
        <section class="bg-white rounded-lg border border-gray-200 p-5">
          <h2 class="font-semibold text-gray-900 mb-3">Operating System</h2>
          <p class="text-sm">{{ record.data.os.description }}</p>
        </section>

        <!-- Disks -->
        <section class="bg-white rounded-lg border border-gray-200 p-5">
          <h2 class="font-semibold text-gray-900 mb-3">Disks</h2>
          <div v-for="disk in record.data.disks" :key="disk.name" class="mb-3 last:mb-0">
            <div class="flex justify-between text-sm mb-1">
              <span class="font-medium">{{ disk.name }}</span>
              <span class="text-gray-500">{{ formatBytes(disk.totalBytes - disk.freeBytes) }} / {{ formatBytes(disk.totalBytes) }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div
                class="h-2 rounded-full"
                :class="diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 90 ? 'bg-red-500' : diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) > 70 ? 'bg-yellow-500' : 'bg-blue-500'"
                :style="{ width: diskPercent(disk.totalBytes - disk.freeBytes, disk.totalBytes) + '%' }"
              />
            </div>
            <p class="text-xs text-gray-400 mt-1">{{ disk.format }} &middot; {{ formatBytes(disk.freeBytes) }} free</p>
          </div>
        </section>

        <!-- Network -->
        <section class="bg-white rounded-lg border border-gray-200 p-5 lg:col-span-2">
          <h2 class="font-semibold text-gray-900 mb-3">Network</h2>
          <p class="text-sm text-gray-500 mb-2">Hostname: {{ record.data.network.hostname }}</p>
          <table class="w-full text-sm">
            <thead>
              <tr class="text-left text-gray-500 border-b">
                <th class="pb-2 font-medium">Interface</th>
                <th class="pb-2 font-medium">MAC</th>
                <th class="pb-2 font-medium">IP Addresses</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="iface in record.data.network.interfaces" :key="iface.name" class="border-b last:border-0">
                <td class="py-2">{{ iface.name }}</td>
                <td class="py-2 font-mono text-xs">{{ iface.macAddress }}</td>
                <td class="py-2">{{ iface.ipAddresses.join(', ') }}</td>
              </tr>
            </tbody>
          </table>
        </section>
      </div>

      <div class="mt-4">
        <RouterLink
          :to="`/disk-usage/${encodeURIComponent(record.computerName)}`"
          class="text-sm text-blue-600 hover:underline"
        >
          View Disk Usage &rarr;
        </RouterLink>
      </div>
    </template>
  </div>
</template>
