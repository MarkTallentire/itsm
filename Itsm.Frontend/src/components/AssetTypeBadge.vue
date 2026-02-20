<script setup lang="ts">
import type { AssetType } from '../types/inventory'
import { assetTypeLabel, assetTypeIcon } from '../utils/format'

const props = defineProps<{ type: AssetType }>()
</script>

<template>
  <span class="inline-flex items-center gap-1 text-xs text-gray-500">
    <span>{{ assetTypeIcon(props.type) }}</span>
    {{ assetTypeLabel(props.type) }}
  </span>
</template>
