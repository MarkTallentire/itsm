<script setup lang="ts">
import { ref } from 'vue'

const props = withDefaults(defineProps<{
  title: string
  defaultOpen?: boolean
  badge?: string | number | null
}>(), {
  defaultOpen: false,
  badge: null,
})

const open = ref(props.defaultOpen)
</script>

<template>
  <div class="border border-gray-200 rounded-lg bg-white overflow-hidden">
    <button
      @click="open = !open"
      class="w-full flex items-center justify-between px-5 py-3.5 text-left hover:bg-gray-50/50 transition-colors"
    >
      <div class="flex items-center gap-2.5">
        <h3 class="text-sm font-semibold text-gray-900">{{ title }}</h3>
        <span
          v-if="badge != null"
          class="text-xs font-medium bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full"
        >{{ badge }}</span>
      </div>
      <svg
        class="h-4 w-4 text-gray-400 transition-transform duration-200"
        :class="{ 'rotate-180': open }"
        fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"
      >
        <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
      </svg>
    </button>
    <div v-show="open" class="px-5 pb-5 border-t border-gray-100">
      <slot />
    </div>
  </div>
</template>
