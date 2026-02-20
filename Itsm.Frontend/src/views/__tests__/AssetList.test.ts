import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import AssetList from '../AssetList.vue'
import type { Asset } from '../../types/inventory'

vi.mock('../../services/api', () => ({
  fetchAssets: vi.fn(),
}))

import { fetchAssets } from '../../services/api'
const mockFetchAssets = vi.mocked(fetchAssets)

function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: AssetList },
      { path: '/assets/new', component: { template: '<div />' } },
      { path: '/assets/:id', component: { template: '<div />' } },
    ],
  })
}

const sampleAsset: Asset = {
  id: '1',
  name: 'Test Monitor',
  type: 'Monitor',
  status: 'InUse',
  serialNumber: 'SN123',
  assignedUser: 'alice',
  location: 'Office A',
  purchaseDate: null,
  warrantyExpiry: null,
  cost: null,
  notes: null,
  source: 'Manual',
  discoveredByAgent: null,
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: new Date().toISOString(),
}

describe('AssetList', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows loading skeleton initially', () => {
    mockFetchAssets.mockReturnValue(new Promise(() => {})) // never resolves
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    expect(wrapper.find('.skeleton').exists()).toBe(true)
  })

  it('shows error state when fetch fails', async () => {
    mockFetchAssets.mockRejectedValue(new Error('Network error'))
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain('Failed to load assets')
    expect(wrapper.text()).toContain('Network error')
  })

  it('shows empty state when no assets', async () => {
    mockFetchAssets.mockResolvedValue([])
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain('No monitors found')
  })

  it('renders table with asset data', async () => {
    mockFetchAssets.mockResolvedValue([sampleAsset])
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain('Test Monitor')
    expect(wrapper.text()).toContain('SN123')
    expect(wrapper.text()).toContain('alice')
    expect(wrapper.text()).toContain('Office A')
    expect(wrapper.find('table').exists()).toBe(true)
  })

  it('shows page title based on asset type', async () => {
    mockFetchAssets.mockResolvedValue([sampleAsset])
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(wrapper.text()).toContain('Monitors')
  })

  it('calls fetchAssets on mount with type filter', async () => {
    mockFetchAssets.mockResolvedValue([])
    const router = makeRouter()
    mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(mockFetchAssets).toHaveBeenCalledTimes(1)
    expect(mockFetchAssets).toHaveBeenCalledWith(
      expect.objectContaining({ type: 'Monitor' })
    )
  })

  it('status filter re-fetches on change', async () => {
    mockFetchAssets.mockResolvedValue([])
    const router = makeRouter()
    const wrapper = mount(AssetList, { props: { assetType: 'Monitor' }, global: { plugins: [router] } })
    await flushPromises()
    expect(mockFetchAssets).toHaveBeenCalledTimes(1)

    const selects = wrapper.findAll('select')
    await selects[0].setValue('InStorage')
    await flushPromises()
    expect(mockFetchAssets).toHaveBeenCalledTimes(2)
  })
})
