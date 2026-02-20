<script setup lang="ts">
import type { AssetStatus } from '../types/inventory'
import { assetStatusLabel, assetStatusColor } from '../utils/format'

const props = defineProps<{ status: AssetStatus }>()
</script>

<template>
  <span
    class="inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full"
    :class="[assetStatusColor(props.status).bg, assetStatusColor(props.status).text]"
  >
    <span class="w-1.5 h-1.5 rounded-full" :class="assetStatusColor(props.status).dot"></span>
    {{ assetStatusLabel(props.status) }}
  </span>
</template>
