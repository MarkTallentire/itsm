import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AssetStatusBadge from '../AssetStatusBadge.vue'
import type { AssetStatus } from '../../types/inventory'

describe('AssetStatusBadge', () => {
  const statuses: [AssetStatus, string][] = [
    ['InUse', 'In Use'],
    ['InStorage', 'In Storage'],
    ['Decommissioned', 'Decommissioned'],
    ['Lost', 'Lost'],
  ]

  it.each(statuses)('renders label for %s', (status, label) => {
    const wrapper = mount(AssetStatusBadge, { props: { status } })
    expect(wrapper.text()).toContain(label)
  })

  it('InUse applies green classes', () => {
    const wrapper = mount(AssetStatusBadge, { props: { status: 'InUse' as AssetStatus } })
    expect(wrapper.html()).toContain('bg-green-50')
    expect(wrapper.html()).toContain('text-green-700')
  })

  it('Lost applies red classes', () => {
    const wrapper = mount(AssetStatusBadge, { props: { status: 'Lost' as AssetStatus } })
    expect(wrapper.html()).toContain('bg-red-50')
    expect(wrapper.html()).toContain('text-red-700')
  })

  it('InStorage applies blue classes', () => {
    const wrapper = mount(AssetStatusBadge, { props: { status: 'InStorage' as AssetStatus } })
    expect(wrapper.html()).toContain('bg-blue-50')
    expect(wrapper.html()).toContain('text-blue-700')
  })

  it('Decommissioned applies gray classes', () => {
    const wrapper = mount(AssetStatusBadge, { props: { status: 'Decommissioned' as AssetStatus } })
    expect(wrapper.html()).toContain('bg-gray-100')
    expect(wrapper.html()).toContain('text-gray-600')
  })

  it('has the dot indicator element', () => {
    const wrapper = mount(AssetStatusBadge, { props: { status: 'InUse' as AssetStatus } })
    const dot = wrapper.find('.w-1\\.5')
    expect(dot.exists()).toBe(true)
  })
})
