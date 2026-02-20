import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import AssetForm from '../AssetForm.vue'

vi.mock('../../services/api', () => ({
  createAsset: vi.fn(),
}))

import { createAsset } from '../../services/api'
const mockCreateAsset = vi.mocked(createAsset)

function makeRouter() {
  return createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div />' } },
      { path: '/assets', component: { template: '<div />' } },
      { path: '/assets/new', component: AssetForm },
      { path: '/assets/:id', component: { template: '<div />' } },
    ],
  })
}

describe('AssetForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders all form fields', async () => {
    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })
    expect(wrapper.find('input[type="text"]').exists()).toBe(true)
    expect(wrapper.find('select').exists()).toBe(true)
    expect(wrapper.find('textarea').exists()).toBe(true)
    expect(wrapper.find('input[type="date"]').exists()).toBe(true)
    expect(wrapper.find('input[type="number"]').exists()).toBe(true)
  })

  it('submit button is disabled when name is empty', async () => {
    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })
    const submitBtn = wrapper.find('button[type="submit"]')
    expect(submitBtn.attributes('disabled')).toBeDefined()
  })

  it('submit button is disabled when type is empty', async () => {
    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })
    const nameInput = wrapper.find('input[type="text"]')
    await nameInput.setValue('Test Asset')
    const submitBtn = wrapper.find('button[type="submit"]')
    // type is still empty, so button should be disabled
    expect(submitBtn.attributes('disabled')).toBeDefined()
  })

  it('does not call createAsset when name is empty', async () => {
    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(mockCreateAsset).not.toHaveBeenCalled()
  })

  it('calls createAsset on valid submit', async () => {
    mockCreateAsset.mockResolvedValue({
      id: 'new-1',
      name: 'Test Phone',
      type: 'Phone',
      status: 'InUse',
      serialNumber: null,
      assignedUser: null,
      location: null,
      purchaseDate: null,
      warrantyExpiry: null,
      cost: null,
      notes: null,
      source: 'Manual',
      discoveredByAgent: null,
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
    })

    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })

    // Fill in required fields
    const inputs = wrapper.findAll('input[type="text"]')
    await inputs[0].setValue('Test Phone') // name
    await wrapper.find('select').setValue('Phone')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mockCreateAsset).toHaveBeenCalledTimes(1)
    expect(mockCreateAsset).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Test Phone',
        type: 'Phone',
        status: 'InUse',
        source: 'Manual',
      }),
    )
  })

  it('shows error when createAsset fails', async () => {
    mockCreateAsset.mockRejectedValue(new Error('Server error'))
    const router = makeRouter()
    await router.push('/assets/new')
    await router.isReady()
    const wrapper = mount(AssetForm, { global: { plugins: [router] } })

    const inputs = wrapper.findAll('input[type="text"]')
    await inputs[0].setValue('Test Phone')
    await wrapper.find('select').setValue('Phone')

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.text()).toContain('Server error')
  })
})
