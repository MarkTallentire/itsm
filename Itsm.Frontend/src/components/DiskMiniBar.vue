<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  percent: number
}>()

const barColor = computed(() => {
  if (props.percent > 90) return 'bg-red-500'
  if (props.percent > 70) return 'bg-amber-500'
  return 'bg-primary-500'
})

const trackColor = computed(() => {
  if (props.percent > 90) return 'bg-red-100'
  if (props.percent > 70) return 'bg-amber-100'
  return 'bg-gray-100'
})

const textColor = computed(() => {
  if (props.percent > 90) return 'text-red-700'
  if (props.percent > 70) return 'text-amber-700'
  return 'text-gray-600'
})
</script>

<template>
  <div class="flex items-center gap-2 min-w-[120px]">
    <div class="flex-1 h-1.5 rounded-full" :class="trackColor">
      <div class="h-1.5 rounded-full transition-all" :class="barColor" :style="{ width: percent + '%' }" />
    </div>
    <span class="text-xs font-medium tabular-nums" :class="textColor">{{ percent }}%</span>
  </div>
</template>
