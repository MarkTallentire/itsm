<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, RouterLink } from 'vue-router'

const route = useRoute()

const crumbs = computed(() => {
  const items: { label: string; to?: string }[] = []
  const matched = route.matched

  for (let i = 0; i < matched.length; i++) {
    const r = matched[i]
    const title = (r.meta?.title as string) || 'Page'
    const isLast = i === matched.length - 1

    if (isLast) {
      // For detail pages, use the route param as label
      if (route.params.name) {
        items.push({ label: title, to: r.path === '/' ? '/' : undefined })
        items.push({ label: route.params.name as string })
      } else {
        items.push({ label: title })
      }
    } else {
      items.push({ label: title, to: r.path })
    }
  }

  return items
})
</script>

<template>
  <nav class="flex items-center gap-1.5 text-sm">
    <template v-for="(crumb, i) in crumbs" :key="i">
      <svg v-if="i > 0" class="h-3.5 w-3.5 text-gray-300" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
      </svg>
      <RouterLink
        v-if="crumb.to"
        :to="crumb.to"
        class="text-gray-500 hover:text-gray-700 transition-colors font-medium"
      >{{ crumb.label }}</RouterLink>
      <span v-else class="text-gray-900 font-semibold">{{ crumb.label }}</span>
    </template>
  </nav>
</template>
