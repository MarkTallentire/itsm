import { createRouter, createWebHistory } from 'vue-router'
import ComputerList from './views/ComputerList.vue'
import ComputerDetail from './views/ComputerDetail.vue'
import DiskUsageView from './views/DiskUsageView.vue'
import AssetList from './views/AssetList.vue'
import AssetDetail from './views/AssetDetail.vue'
import AssetForm from './views/AssetForm.vue'
import PrinterDetail from './views/PrinterDetail.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: ComputerList, meta: { title: 'Computers' } },
    { path: '/computers/:name', component: ComputerDetail, props: true, meta: { title: 'Computers' } },
    { path: '/disk-usage/:name', component: DiskUsageView, props: true, meta: { title: 'Disk Usage' } },
    { path: '/monitors', component: AssetList, props: { assetType: 'Monitor' }, meta: { title: 'Monitors' } },
    { path: '/printers', component: AssetList, props: { assetType: 'NetworkPrinter' }, meta: { title: 'Printers' } },
    { path: '/printers/:id', component: PrinterDetail, props: true, meta: { title: 'Printer Detail' } },
    { path: '/usb-devices', component: AssetList, props: { assetType: 'UsbPeripheral' }, meta: { title: 'USB Devices' } },
    { path: '/other-assets', component: AssetList, props: { assetType: 'Other' }, meta: { title: 'Other Assets' } },
    { path: '/assets/new', component: AssetForm, meta: { title: 'New Asset' } },
    { path: '/assets/:id', component: AssetDetail, props: true, meta: { title: 'Asset Detail' } },
  ],
})

export default router
