<script setup lang="ts">
import { ref } from 'vue'
import type { DirectoryNode } from '../types/inventory'
import { formatBytes } from '../utils/format'

const props = defineProps<{
  node: DirectoryNode
  maxBytes: number
  depth: number
}>()

const expanded = ref(props.depth < 1)

function barWidth(bytes: number, max: number): string {
  return max === 0 ? '0%' : `${Math.max(1, (bytes / max) * 100)}%`
}

function barColor(bytes: number, max: number): string {
  const pct = max === 0 ? 0 : (bytes / max) * 100
  if (pct > 75) return 'bg-primary-600'
  if (pct > 40) return 'bg-primary-400'
  return 'bg-primary-300'
}

function toggle() {
  expanded.value = !expanded.value
}
</script>

<template>
  <tr
    class="border-b border-gray-50 last:border-0 transition-colors cursor-pointer"
    :class="node.children.length ? 'hover:bg-primary-50/50' : 'hover:bg-gray-50/80'"
    @click="toggle"
  >
    <td class="px-4 py-2" :style="{ paddingLeft: `${depth * 20 + 16}px` }">
      <span v-if="node.children.length" class="inline-flex items-center justify-center w-4 h-4 text-gray-400 transition-transform duration-200" :class="expanded ? 'rotate-0' : '-rotate-90'">
        <svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" d="m19.5 8.25-7.5 7.5-7.5-7.5" />
        </svg>
      </span>
      <span v-else class="inline-block w-4" />
      <span class="ml-1.5" :class="node.children.length ? 'font-medium text-gray-800' : 'text-gray-600'">{{ node.path }}</span>
    </td>
    <td class="px-4 py-2 text-right font-mono text-xs text-gray-500 tabular-nums">{{ formatBytes(node.sizeBytes) }}</td>
    <td class="px-4 py-2">
      <div class="w-full bg-gray-100 rounded-full h-1.5">
        <div class="h-1.5 rounded-full transition-all duration-300" :class="barColor(node.sizeBytes, maxBytes)" :style="{ width: barWidth(node.sizeBytes, maxBytes) }" />
      </div>
    </td>
  </tr>
  <template v-if="expanded && node.children.length">
    <DirectoryTree
      v-for="child in node.children"
      :key="child.path"
      :node="child"
      :maxBytes="maxBytes"
      :depth="depth + 1"
    />
  </template>
</template>
