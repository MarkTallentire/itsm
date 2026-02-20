<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { Asset, AssetType, AssetStatus } from '../types/inventory'
import { fetchAssets } from '../services/api'
import { assetTypeLabel, timeAgo } from '../utils/format'
import AssetStatusBadge from '../components/AssetStatusBadge.vue'
import AssetTypeBadge from '../components/AssetTypeBadge.vue'

const props = defineProps<{
  assetType?: AssetType
}>()

const router = useRouter()
const assets = ref<Asset[]>([])
const loading = ref(true)
const error = ref('')

const filterStatus = ref('')
const search = ref('')

const assetStatuses: AssetStatus[] = ['InUse', 'InStorage', 'Decommissioned', 'Lost']

// For the "Other" page, include Phone, Tablet, and Other types
const otherTypes = ['Phone', 'Tablet', 'Other']
const isOtherPage = computed(() => props.assetType === 'Other')

const pageTitle = computed(() => {
  if (isOtherPage.value) return 'Other Assets'
  if (props.assetType) return assetTypeLabel(props.assetType) + 's'
  return 'Assets'
})

const pageDescription = computed(() => {
  if (isOtherPage.value) return 'Manually tracked phones, tablets, and other equipment'
  if (props.assetType === 'Monitor') return 'External displays discovered by agents'
  if (props.assetType === 'NetworkPrinter') return 'Network printers discovered via SNMP'
  if (props.assetType === 'UsbPeripheral') return 'USB peripherals with serial numbers'
  return 'All tracked assets'
})

async function loadAssets() {
  loading.value = true
  error.value = ''
  try {
    if (isOtherPage.value) {
      // Fetch all and filter client-side for multi-type
      const all = await fetchAssets({
        status: filterStatus.value || undefined,
        search: search.value || undefined,
      })
      assets.value = all.filter(a => otherTypes.includes(a.type))
    } else {
      assets.value = await fetchAssets({
        type: props.assetType || undefined,
        status: filterStatus.value || undefined,
        search: search.value || undefined,
      })
    }
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

onMounted(loadAssets)

watch(filterStatus, loadAssets)

let searchTimeout: ReturnType<typeof setTimeout> | null = null
watch(search, () => {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(loadAssets, 300)
})

function navigateToAsset(asset: Asset) {
  if (asset.type === 'NetworkPrinter') {
    router.push(`/printers/${encodeURIComponent(asset.id)}`)
  } else {
    router.push(`/assets/${encodeURIComponent(asset.id)}`)
  }
}
</script>

<template>
  <div>
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 tracking-tight">{{ pageTitle }}</h1>
        <p class="text-sm text-gray-500 mt-1">{{ pageDescription }}</p>
      </div>
      <RouterLink
        v-if="isOtherPage"
        to="/assets/new"
        class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
        </svg>
        Add Asset
      </RouterLink>
    </div>

    <!-- Filter bar -->
    <div class="flex items-center gap-3 mb-6">
      <select
        v-model="filterStatus"
        class="border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400 bg-white"
      >
        <option value="">All Statuses</option>
        <option v-for="s in assetStatuses" :key="s" :value="s">
          {{ s === 'InUse' ? 'In Use' : s === 'InStorage' ? 'In Storage' : s }}
        </option>
      </select>
      <div class="relative flex-1 max-w-xs">
        <svg class="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
        </svg>
        <input
          v-model="search"
          type="text"
          placeholder="Search..."
          class="w-full pl-9 pr-3 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
        />
      </div>
    </div>

    <!-- Loading skeleton -->
    <div v-if="loading" class="space-y-4">
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
      <p class="text-sm font-medium text-red-800">Failed to load assets</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <!-- Empty state -->
    <div v-else-if="assets.length === 0" class="rounded-xl border border-gray-200 bg-white p-12 text-center">
      <div class="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-gray-100">
        <svg class="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m20.25 7.5-.625 10.632a2.25 2.25 0 0 1-2.247 2.118H6.622a2.25 2.25 0 0 1-2.247-2.118L3.75 7.5m8.25 3v6.75m0 0-3-3m3 3 3-3M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125Z" />
        </svg>
      </div>
      <p class="text-sm font-medium text-gray-900">No {{ pageTitle.toLowerCase() }} found</p>
      <p class="text-sm text-gray-500 mt-1">
        <template v-if="isOtherPage">Add assets manually using the button above.</template>
        <template v-else>Assets will appear here once agents discover them.</template>
      </p>
    </div>

    <!-- Data table -->
    <template v-else>
      <div class="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="text-left text-xs text-gray-400 uppercase tracking-wider bg-gray-50 border-b border-gray-200">
                <th v-if="isOtherPage" class="px-5 py-3 font-medium">Type</th>
                <th class="px-5 py-3 font-medium">Name</th>
                <th class="px-5 py-3 font-medium">Serial Number</th>
                <th class="px-5 py-3 font-medium">Status</th>
                <th class="px-5 py-3 font-medium">Assigned User</th>
                <th class="px-5 py-3 font-medium">Location</th>
                <th class="px-5 py-3 font-medium">Source</th>
                <th class="px-5 py-3 font-medium">Last Updated</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr
                v-for="asset in assets"
                :key="asset.id"
                @click="navigateToAsset(asset)"
                class="hover:bg-gray-50 cursor-pointer transition-colors"
              >
                <td v-if="isOtherPage" class="px-5 py-3">
                  <AssetTypeBadge :type="asset.type" />
                </td>
                <td class="px-5 py-3 font-medium text-gray-900">{{ asset.name }}</td>
                <td class="px-5 py-3 text-gray-500 font-mono text-xs">{{ asset.serialNumber ?? '-' }}</td>
                <td class="px-5 py-3">
                  <AssetStatusBadge :status="asset.status" />
                </td>
                <td class="px-5 py-3 text-gray-600">{{ asset.assignedUser ?? '-' }}</td>
                <td class="px-5 py-3 text-gray-600">{{ asset.location ?? '-' }}</td>
                <td class="px-5 py-3">
                  <span
                    class="inline-flex items-center text-xs font-medium px-2 py-0.5 rounded-full"
                    :class="asset.source === 'Agent' ? 'bg-purple-50 text-purple-700' : 'bg-gray-100 text-gray-600'"
                  >
                    {{ asset.source }}
                  </span>
                </td>
                <td class="px-5 py-3 text-xs text-gray-500">{{ timeAgo(asset.updatedAtUtc) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>
