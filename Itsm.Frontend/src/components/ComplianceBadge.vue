<script setup lang="ts">
import { computed } from 'vue'
import type { FirewallInfo, EncryptionInfo } from '../types/inventory'
import { computeComplianceStatus } from '../utils/format'

const props = defineProps<{
  firewall?: FirewallInfo | null
  encryption?: EncryptionInfo | null
}>()

const status = computed(() => computeComplianceStatus(props.firewall, props.encryption))
</script>

<template>
  <span
    class="inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full"
    :class="status.passing
      ? 'bg-green-50 text-green-700'
      : 'bg-red-50 text-red-700'"
  >
    <span class="w-1.5 h-1.5 rounded-full" :class="status.passing ? 'bg-green-500' : 'bg-red-500'"></span>
    {{ status.passing ? 'Passing' : `${status.issues.length} issue${status.issues.length > 1 ? 's' : ''}` }}
  </span>
</template>
