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

function toggle() {
  expanded.value = !expanded.value
}
</script>

<template>
  <tr class="border-b last:border-0 hover:bg-gray-50 cursor-pointer" @click="toggle">
    <td class="px-4 py-1.5" :style="{ paddingLeft: `${depth * 20 + 16}px` }">
      <span v-if="node.children.length" class="inline-block w-4 text-gray-400">
        {{ expanded ? '▼' : '▶' }}
      </span>
      <span v-else class="inline-block w-4" />
      <span class="ml-1">{{ node.path }}</span>
    </td>
    <td class="px-4 py-1.5 text-right font-mono text-xs text-gray-600">{{ formatBytes(node.sizeBytes) }}</td>
    <td class="px-4 py-1.5">
      <div class="w-full bg-gray-100 rounded h-2">
        <div class="h-2 rounded bg-blue-500" :style="{ width: barWidth(node.sizeBytes, maxBytes) }" />
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
