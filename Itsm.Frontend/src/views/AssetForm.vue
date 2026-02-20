<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import type { AssetType, AssetStatus } from '../types/inventory'
import { createAsset } from '../services/api'
import { assetTypeLabel } from '../utils/format'

const router = useRouter()
const submitting = ref(false)
const error = ref('')

const assetTypes: AssetType[] = ['Phone', 'Tablet', 'Other']

const form = ref({
  name: '',
  type: '' as AssetType | '',
  serialNumber: '',
  assignedUser: '',
  location: '',
  purchaseDate: '',
  warrantyExpiry: '',
  cost: null as number | null,
  notes: '',
})

async function submit() {
  if (!form.value.name || !form.value.type) return
  submitting.value = true
  error.value = ''
  try {
    const asset = await createAsset({
      name: form.value.name,
      type: form.value.type as AssetType,
      status: 'InUse' as AssetStatus,
      serialNumber: form.value.serialNumber || null,
      assignedUser: form.value.assignedUser || null,
      location: form.value.location || null,
      purchaseDate: form.value.purchaseDate || null,
      warrantyExpiry: form.value.warrantyExpiry || null,
      cost: form.value.cost,
      notes: form.value.notes || null,
      source: 'Manual',
    })
    router.push(`/assets/${encodeURIComponent(asset.id)}`)
  } catch (e: any) {
    error.value = e.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div>
    <!-- Back link -->
    <RouterLink
      to="/other-assets"
      class="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 transition-colors mb-4"
    >
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
      </svg>
      Back to Assets
    </RouterLink>

    <div class="mb-6">
      <h1 class="text-2xl font-bold text-gray-900 tracking-tight">New Asset</h1>
      <p class="text-sm text-gray-500 mt-1">Manually register an IT asset</p>
    </div>

    <div class="bg-white rounded-lg border border-gray-200 p-6 max-w-2xl">
      <div v-if="error" class="mb-4 p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
        {{ error }}
      </div>

      <form @submit.prevent="submit" class="space-y-4">
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Name <span class="text-red-500">*</span></label>
          <input
            v-model="form.name"
            type="text"
            required
            class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
            placeholder="e.g. Dell U2722D"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Type <span class="text-red-500">*</span></label>
          <select
            v-model="form.type"
            required
            class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
          >
            <option value="" disabled>Select a type</option>
            <option v-for="t in assetTypes" :key="t" :value="t">{{ assetTypeLabel(t) }}</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Serial Number</label>
          <input
            v-model="form.serialNumber"
            type="text"
            class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400"
          />
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
        <div class="flex items-center gap-3 pt-2">
          <button
            type="submit"
            :disabled="submitting || !form.name || !form.type"
            class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-primary-500 text-white hover:bg-primary-600 transition-colors disabled:opacity-50"
          >
            <svg v-if="submitting" class="animate-spin h-3.5 w-3.5" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
            Create Asset
          </button>
          <RouterLink
            to="/other-assets"
            class="px-4 py-2 text-sm font-medium rounded-lg border border-gray-200 text-gray-700 hover:bg-gray-50 transition-colors"
          >
            Cancel
          </RouterLink>
        </div>
      </form>
    </div>
  </div>
</template>
