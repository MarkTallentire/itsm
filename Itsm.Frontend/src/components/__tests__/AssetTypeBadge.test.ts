import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AssetTypeBadge from '../AssetTypeBadge.vue'
import type { AssetType } from '../../types/inventory'

describe('AssetTypeBadge', () => {
  const types: [AssetType, string][] = [
    ['Computer', 'Computer'],
    ['Monitor', 'Monitor'],
    ['UsbPeripheral', 'USB Device'],
    ['NetworkPrinter', 'Printer'],
    ['Phone', 'Phone'],
    ['Tablet', 'Tablet'],
    ['Other', 'Other'],
  ]

  it.each(types)('renders label for %s', (type, label) => {
    const wrapper = mount(AssetTypeBadge, { props: { type } })
    expect(wrapper.text()).toContain(label)
  })

  it.each(types)('renders icon for %s', (type) => {
    const wrapper = mount(AssetTypeBadge, { props: { type } })
    const spans = wrapper.findAll('span')
    // First inner span is the icon
    const iconSpan = spans.find((s) => s.element.parentElement === wrapper.element)
    expect(iconSpan).toBeDefined()
  })
})
