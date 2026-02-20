import { createRouter, createWebHistory } from 'vue-router'
import ComputerList from './views/ComputerList.vue'
import ComputerDetail from './views/ComputerDetail.vue'
import DiskUsageView from './views/DiskUsageView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: ComputerList, meta: { title: 'Computers' } },
    { path: '/computers/:name', component: ComputerDetail, props: true, meta: { title: 'Computers' } },
    { path: '/disk-usage/:name', component: DiskUsageView, props: true, meta: { title: 'Disk Usage' } },
  ],
})

export default router
