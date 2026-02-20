<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import type { Asset, AssetStatus } from '../types/inventory'
import { fetchAsset, updateAsset, deleteAsset } from '../services/api'
import { assetTypeIcon, assetTypeLabel, timeAgo } from '../utils/format'
import AssetStatusBadge from '../components/AssetStatusBadge.vue'

const props = defineProps<{ id: string }>()
const router = useRouter()

const asset = ref<Asset | null>(null)
const loading = ref(true)
const error = ref('')
const saving = ref(false)
const toast = ref('')

const form = ref({
  status: '' as AssetStatus,
  assignedUser: '',
  location: '',
  purchaseDate: '',
  warrantyExpiry: '',
  cost: null as number | null,
  notes: '',
})

const assetStatuses: AssetStatus[] = ['InUse', 'InStorage', 'Decommissioned', 'Lost']

function populateForm(a: Asset) {
  form.value.status = a.status
  form.value.assignedUser = a.assignedUser ?? ''
  form.value.location = a.location ?? ''
  form.value.purchaseDate = a.purchaseDate?.split('T')[0] ?? ''
  form.value.warrantyExpiry = a.warrantyExpiry?.split('T')[0] ?? ''
  form.value.cost = a.cost
  form.value.notes = a.notes ?? ''
}

onMounted(async () => {
  try {
    asset.value = await fetchAsset(props.id)
    populateForm(asset.value)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

async function save() {
  if (!asset.value) return
  saving.value = true
  try {
    const updated = await updateAsset(props.id, {
      name: asset.value.name,
      status: form.value.status,
      assignedUser: form.value.assignedUser || null,
      location: form.value.location || null,
      purchaseDate: form.value.purchaseDate || null,
      warrantyExpiry: form.value.warrantyExpiry || null,
      cost: form.value.cost,
      notes: form.value.notes || null,
    })
    asset.value = updated
    populateForm(updated)
    toast.value = 'Asset saved'
    setTimeout(() => toast.value = '', 3000)
  } catch {
    toast.value = 'Failed to save'
    setTimeout(() => toast.value = '', 3000)
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!window.confirm('Are you sure you want to delete this asset?')) return
  try {
    await deleteAsset(props.id)
    toast.value = 'Asset deleted'
    setTimeout(() => router.back(), 500)
  } catch {
    toast.value = 'Failed to delete asset'
    setTimeout(() => toast.value = '', 3000)
  }
}

function formatDate(utc: string): string {
  const dateStr = utc.endsWith('Z') ? utc : utc + 'Z'
  return new Date(dateStr).toLocaleDateString()
}
</script>

<template>
  <div>
    <!-- Back link -->
    <button
      @click="router.back()"
      class="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 transition-colors mb-4"
    >
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
      </svg>
      Back
    </button>

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
      <div class="grid grid-cols-2 gap-4">
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
      <p class="text-sm font-medium text-red-800">Failed to load asset</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <template v-else-if="asset">
      <!-- Header -->
      <div class="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-4">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-gray-100 text-2xl">
              {{ assetTypeIcon(asset.type) }}
            </div>
            <div>
              <div class="flex items-center gap-2.5">
                <h1 class="text-xl font-bold text-gray-900">{{ asset.name }}</h1>
                <AssetStatusBadge :status="asset.status" />
              </div>
              <p class="text-sm text-gray-500 mt-0.5">{{ assetTypeLabel(asset.type) }}</p>
            </div>
          </div>
          <button
            @click="confirmDelete"
            class="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium rounded-lg border border-red-200 text-red-600 hover:bg-red-50 transition-colors"
          >
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
            </svg>
            Delete
          </button>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Read-only info -->
        <div class="bg-white rounded-lg border border-gray-200 p-6">
          <h2 class="text-sm font-semibold text-gray-900 mb-4">Discovery Information</h2>
          <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
            <dt class="text-gray-400 font-medium">Type</dt>
            <dd class="text-gray-700">{{ assetTypeLabel(asset.type) }}</dd>
            <dt class="text-gray-400 font-medium">Serial Number</dt>
            <dd class="text-gray-700 font-mono text-xs">{{ asset.serialNumber ?? '-' }}</dd>
            <dt class="text-gray-400 font-medium">Source</dt>
            <dd class="text-gray-700">{{ asset.source }}</dd>
            <template v-if="asset.discoveredByAgent">
              <dt class="text-gray-400 font-medium">Discovered By</dt>
              <dd class="text-gray-700">
                <RouterLink
                  v-if="asset.discoveredByComputerName"
                  :to="`/computers/${encodeURIComponent(asset.discoveredByComputerName)}`"
                  class="text-primary-600 hover:text-primary-700 font-medium text-sm transition-colors"
                >
                  {{ asset.discoveredByComputerName }}
                </RouterLink>
                <span class="font-mono text-xs text-gray-400 ml-1.5">({{ asset.discoveredByAgent }})</span>
              </dd>
            </template>
            <dt class="text-gray-400 font-medium">Created</dt>
            <dd class="text-gray-700">{{ formatDate(asset.createdAtUtc) }}</dd>
            <dt class="text-gray-400 font-medium">Last Updated</dt>
            <dd class="text-gray-700">{{ timeAgo(asset.updatedAtUtc) }}</dd>
          </dl>

          <template v-if="asset.type === 'Computer'">
            <hr class="my-4 border-gray-100" />
            <RouterLink
              :to="`/computers/${encodeURIComponent(asset.name)}`"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors"
            >
              View Computer Details
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
              </svg>
            </RouterLink>
          </template>
        </div>

        <!-- Editable form -->
        <div class="bg-white rounded-lg border border-gray-200 p-6">
          <h2 class="text-sm font-semibold text-gray-900 mb-4">Asset Details</h2>
          <form @submit.prevent="save" class="space-y-4">
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Status</label>
              <select
                v-model="form.status"
                class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
              >
                <option v-for="s in assetStatuses" :key="s" :value="s">
                  {{ s === 'InUse' ? 'In Use' : s === 'InStorage' ? 'In Storage' : s }}
                </option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Assigned User</label>
              <input
                v-model="form.assignedUser"
                type="text"
                class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Location</label>
              <input
                v-model="form.location"
                type="text"
                class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
              />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Purchase Date</label>
                <input
                  v-model="form.purchaseDate"
                  type="date"
                  class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
                />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Warranty Expiry</label>
                <input
                  v-model="form.warrantyExpiry"
                  type="date"
                  class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
                />
              </div>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Cost</label>
              <input
                v-model.number="form.cost"
                type="number"
                step="0.01"
                min="0"
                class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Notes</label>
              <textarea
                v-model="form.notes"
                rows="3"
                class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400 resize-none"
              ></textarea>
            </div>
            <div class="flex justify-end">
              <button
                type="submit"
                :disabled="saving"
                class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors disabled:opacity-50"
              >
                <svg v-if="saving" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                Save Changes
              </button>
            </div>
          </form>
        </div>
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
