<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { PrinterDetail, AssetStatus } from '../types/inventory'
import { fetchPrinterDetail, updateAsset, deleteAsset } from '../services/api'
import { timeAgo } from '../utils/format'
import AssetStatusBadge from '../components/AssetStatusBadge.vue'

const props = defineProps<{ id: string }>()
const router = useRouter()

const printer = ref<PrinterDetail | null>(null)
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

function populateForm(p: PrinterDetail) {
  form.value.status = p.status
  form.value.assignedUser = p.assignedUser ?? ''
  form.value.location = p.location ?? ''
  form.value.purchaseDate = p.purchaseDate?.split('T')[0] ?? ''
  form.value.warrantyExpiry = p.warrantyExpiry?.split('T')[0] ?? ''
  form.value.cost = p.cost
  form.value.notes = p.notes ?? ''
}

onMounted(async () => {
  try {
    printer.value = await fetchPrinterDetail(props.id)
    populateForm(printer.value)
  } catch (e: any) {
    error.value = e.message
  } finally {
    loading.value = false
  }
})

const printerStatusColor = computed(() => {
  if (!printer.value?.printerStatus) return 'text-gray-500'
  switch (printer.value.printerStatus) {
    case 'Idle': return 'text-green-600'
    case 'Printing': return 'text-blue-600'
    case 'Warming Up': return 'text-yellow-600'
    case 'Error': return 'text-red-600'
    default: return 'text-gray-600'
  }
})

const inkLevels = computed(() => {
  if (!printer.value) return []
  const levels: { name: string; color: string; bgColor: string; percent: number | null }[] = []
  if (printer.value.tonerBlackPercent !== undefined)
    levels.push({ name: 'Black', color: 'bg-gray-800', bgColor: 'bg-gray-200', percent: printer.value.tonerBlackPercent })
  if (printer.value.tonerCyanPercent !== undefined)
    levels.push({ name: 'Cyan', color: 'bg-cyan-500', bgColor: 'bg-cyan-100', percent: printer.value.tonerCyanPercent })
  if (printer.value.tonerMagentaPercent !== undefined)
    levels.push({ name: 'Magenta', color: 'bg-pink-500', bgColor: 'bg-pink-100', percent: printer.value.tonerMagentaPercent })
  if (printer.value.tonerYellowPercent !== undefined)
    levels.push({ name: 'Yellow', color: 'bg-yellow-400', bgColor: 'bg-yellow-100', percent: printer.value.tonerYellowPercent })
  return levels
})

function formatDate(utc: string): string {
  const dateStr = utc.endsWith('Z') ? utc : utc + 'Z'
  return new Date(dateStr).toLocaleDateString()
}

async function save() {
  if (!printer.value) return
  saving.value = true
  try {
    await updateAsset(props.id, {
      name: printer.value.name,
      status: form.value.status,
      assignedUser: form.value.assignedUser || null,
      location: form.value.location || null,
      purchaseDate: form.value.purchaseDate || null,
      warrantyExpiry: form.value.warrantyExpiry || null,
      cost: form.value.cost,
      notes: form.value.notes || null,
    })
    printer.value.status = form.value.status
    toast.value = 'Printer saved'
    setTimeout(() => toast.value = '', 3000)
  } catch {
    toast.value = 'Failed to save'
    setTimeout(() => toast.value = '', 3000)
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  if (!window.confirm('Are you sure you want to delete this printer?')) return
  try {
    await deleteAsset(props.id)
    toast.value = 'Printer deleted'
    setTimeout(() => router.push('/printers'), 500)
  } catch {
    toast.value = 'Failed to delete'
    setTimeout(() => toast.value = '', 3000)
  }
}
</script>

<template>
  <div>
    <!-- Back link -->
    <button
      @click="router.push('/printers')"
      class="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-gray-700 transition-colors mb-4"
    >
      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18" />
      </svg>
      Printers
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
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
      <div class="mx-auto mb-3 flex h-10 w-10 items-center justify-center rounded-full bg-red-100">
        <svg class="h-5 w-5 text-red-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z" />
        </svg>
      </div>
      <p class="text-sm font-medium text-red-800">Failed to load printer</p>
      <p class="text-sm text-red-600 mt-1">{{ error }}</p>
    </div>

    <template v-else-if="printer">
      <!-- Header -->
      <div class="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-4">
            <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-gray-100 text-2xl">
              🖨️
            </div>
            <div>
              <div class="flex items-center gap-2.5">
                <h1 class="text-xl font-bold text-gray-900">{{ printer.name }}</h1>
                <AssetStatusBadge :status="printer.status" />
                <span
                  class="inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full"
                  :class="{
                    'bg-green-50 text-green-700': printer.printerStatus === 'Idle',
                    'bg-blue-50 text-blue-700': printer.printerStatus === 'Printing',
                    'bg-yellow-50 text-yellow-700': printer.printerStatus === 'Warming Up',
                    'bg-red-50 text-red-700': printer.printerStatus === 'Error',
                    'bg-gray-50 text-gray-600': !['Idle', 'Printing', 'Warming Up', 'Error'].includes(printer.printerStatus ?? ''),
                  }"
                >
                  <span class="h-1.5 w-1.5 rounded-full" :class="{
                    'bg-green-500': printer.printerStatus === 'Idle',
                    'bg-blue-500': printer.printerStatus === 'Printing',
                    'bg-yellow-500': printer.printerStatus === 'Warming Up',
                    'bg-red-500': printer.printerStatus === 'Error',
                    'bg-gray-400': !['Idle', 'Printing', 'Warming Up', 'Error'].includes(printer.printerStatus ?? ''),
                  }"></span>
                  {{ printer.printerStatus ?? 'Unknown' }}
                </span>
              </div>
              <p class="text-sm text-gray-500 mt-0.5">
                {{ [printer.manufacturer, printer.model].filter(Boolean).join(' ') || 'Network Printer' }}
              </p>
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

      <!-- Ink levels card -->
      <div v-if="inkLevels.length > 0" class="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 class="text-sm font-semibold text-gray-900 mb-4">Ink / Toner Levels</h2>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div v-for="ink in inkLevels" :key="ink.name" class="space-y-2">
            <div class="flex items-center justify-between text-sm">
              <span class="font-medium text-gray-700">{{ ink.name }}</span>
              <span class="text-gray-500">{{ ink.percent != null ? ink.percent + '%' : 'Unknown' }}</span>
            </div>
            <div class="h-3 rounded-full overflow-hidden" :class="ink.bgColor">
              <div
                v-if="ink.percent != null"
                class="h-full rounded-full transition-all"
                :class="ink.color"
                :style="{ width: ink.percent + '%' }"
              ></div>
              <div
                v-else
                class="h-full rounded-full w-full opacity-30"
                :class="ink.color"
              ></div>
            </div>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Printer info -->
        <div class="bg-white rounded-lg border border-gray-200 p-6">
          <h2 class="text-sm font-semibold text-gray-900 mb-4">Printer Information</h2>
          <dl class="grid grid-cols-[auto_1fr] gap-x-6 gap-y-3 text-sm">
            <dt class="text-gray-400 font-medium">IP Address</dt>
            <dd class="text-gray-700 font-mono text-xs">{{ printer.ipAddress }}</dd>
            <template v-if="printer.macAddress">
              <dt class="text-gray-400 font-medium">MAC Address</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ printer.macAddress }}</dd>
            </template>
            <template v-if="printer.manufacturer">
              <dt class="text-gray-400 font-medium">Manufacturer</dt>
              <dd class="text-gray-700">{{ printer.manufacturer }}</dd>
            </template>
            <template v-if="printer.model">
              <dt class="text-gray-400 font-medium">Model</dt>
              <dd class="text-gray-700">{{ printer.model }}</dd>
            </template>
            <template v-if="printer.serialNumber">
              <dt class="text-gray-400 font-medium">Serial Number</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ printer.serialNumber }}</dd>
            </template>
            <template v-if="printer.firmwareVersion">
              <dt class="text-gray-400 font-medium">Firmware</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ printer.firmwareVersion }}</dd>
            </template>
            <template v-if="printer.pageCount != null">
              <dt class="text-gray-400 font-medium">Page Count</dt>
              <dd class="text-gray-700 font-semibold">{{ printer.pageCount.toLocaleString() }}</dd>
            </template>
            <dt class="text-gray-400 font-medium">Source</dt>
            <dd class="text-gray-700">{{ printer.source }}</dd>
            <template v-if="printer.discoveredByAgent">
              <dt class="text-gray-400 font-medium">Discovered By</dt>
              <dd class="text-gray-700 font-mono text-xs">{{ printer.discoveredByAgent }}</dd>
            </template>
            <dt class="text-gray-400 font-medium">First Seen</dt>
            <dd class="text-gray-700">{{ formatDate(printer.createdAtUtc) }}</dd>
            <dt class="text-gray-400 font-medium">Last Updated</dt>
            <dd class="text-gray-700">{{ timeAgo(printer.updatedAtUtc) }}</dd>
          </dl>
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
              <input v-model="form.assignedUser" type="text" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Location</label>
              <input v-model="form.location" type="text" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400" />
            </div>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Purchase Date</label>
                <input v-model="form.purchaseDate" type="date" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400" />
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-500 mb-1">Warranty Expiry</label>
                <input v-model="form.warrantyExpiry" type="date" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400" />
              </div>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Cost</label>
              <input v-model.number="form.cost" type="number" step="0.01" min="0" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400" />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-500 mb-1">Notes</label>
              <textarea v-model="form.notes" rows="3" class="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-400 resize-none"></textarea>
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
