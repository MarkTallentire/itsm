import { createRouter, createWebHistory } from 'vue-router'
import ComputerList from './views/ComputerList.vue'
import ComputerDetail from './views/ComputerDetail.vue'
import DiskUsageView from './views/DiskUsageView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: ComputerList },
    { path: '/computers/:name', component: ComputerDetail, props: true },
    { path: '/disk-usage/:name', component: DiskUsageView, props: true },
  ],
})

export default router
