import { describe, it, expect, vi, afterEach } from 'vitest'
import {
  formatBytes,
  chassisLabel,
  chassisIcon,
  timeAgo,
  computeComplianceStatus,
  computeDiskHealth,
  diskPercent,
  parseUptime,
  assetTypeLabel,
  assetTypeIcon,
  assetStatusLabel,
  assetStatusColor,
} from '../format'
import type { AssetType, AssetStatus } from '../../types/inventory'

describe('formatBytes', () => {
  it('returns "0 B" for 0', () => {
    expect(formatBytes(0)).toBe('0 B')
  })
  it('formats bytes', () => {
    expect(formatBytes(500)).toBe('500 B')
  })
  it('formats kilobytes', () => {
    expect(formatBytes(1024)).toBe('1.0 KB')
  })
  it('formats megabytes', () => {
    expect(formatBytes(1048576)).toBe('1.0 MB')
  })
  it('formats gigabytes', () => {
    expect(formatBytes(1073741824)).toBe('1.0 GB')
  })
  it('formats terabytes', () => {
    expect(formatBytes(1099511627776)).toBe('1.0 TB')
  })
})

describe('chassisLabel', () => {
  it.each([
    ['Laptop', 'Laptop'],
    ['Desktop', 'Desktop'],
    ['Tower', 'Tower'],
    ['Mini', 'Mini'],
    ['AllInOne', 'All-in-One'],
    ['Tablet', 'Tablet'],
  ])('returns "%s" -> "%s"', (input, expected) => {
    expect(chassisLabel(input)).toBe(expected)
  })

  it('returns "Unknown" for unrecognized type', () => {
    expect(chassisLabel('Fridge')).toBe('Unknown')
  })
})

describe('chassisIcon', () => {
  it('returns an icon string for known types', () => {
    expect(chassisIcon('Laptop')).toBeTruthy()
    expect(chassisIcon('Desktop')).toBeTruthy()
  })
  it('returns fallback for unknown type', () => {
    expect(chassisIcon('Unknown')).toBe('\u2753')
  })
})

describe('timeAgo', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns "just now" for recent timestamps', () => {
    const now = new Date().toISOString()
    expect(timeAgo(now)).toBe('just now')
  })

  it('returns minutes ago', () => {
    const fiveMinAgo = new Date(Date.now() - 5 * 60 * 1000).toISOString()
    expect(timeAgo(fiveMinAgo)).toBe('5m ago')
  })

  it('returns hours ago', () => {
    const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString()
    expect(timeAgo(twoHoursAgo)).toBe('2h ago')
  })

  it('returns days ago', () => {
    const threeDaysAgo = new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString()
    expect(timeAgo(threeDaysAgo)).toBe('3d ago')
  })

  it('handles timestamps without Z suffix', () => {
    const now = new Date().toISOString().replace('Z', '')
    expect(timeAgo(now)).toBe('just now')
  })
})

describe('computeComplianceStatus', () => {
  it('returns passing when both enabled', () => {
    const result = computeComplianceStatus({ isEnabled: true }, { isEnabled: true })
    expect(result.passing).toBe(true)
    expect(result.issues).toHaveLength(0)
  })

  it('fails when firewall disabled', () => {
    const result = computeComplianceStatus({ isEnabled: false }, { isEnabled: true })
    expect(result.passing).toBe(false)
    expect(result.issues).toContain('Firewall disabled')
  })

  it('fails when encryption disabled', () => {
    const result = computeComplianceStatus({ isEnabled: true }, { isEnabled: false })
    expect(result.passing).toBe(false)
    expect(result.issues).toContain('Encryption disabled')
  })

  it('fails when both disabled', () => {
    const result = computeComplianceStatus({ isEnabled: false }, { isEnabled: false })
    expect(result.passing).toBe(false)
    expect(result.issues).toHaveLength(2)
  })

  it('fails when firewall is null', () => {
    const result = computeComplianceStatus(null, { isEnabled: true })
    expect(result.passing).toBe(false)
    expect(result.issues).toContain('Firewall disabled')
  })

  it('fails when encryption is undefined', () => {
    const result = computeComplianceStatus({ isEnabled: true }, undefined)
    expect(result.passing).toBe(false)
    expect(result.issues).toContain('Encryption disabled')
  })
})

describe('computeDiskHealth', () => {
  it('returns ok for empty disks', () => {
    const result = computeDiskHealth([])
    expect(result.worstPercent).toBe(0)
    expect(result.status).toBe('ok')
  })

  it('returns ok for 50% usage', () => {
    const result = computeDiskHealth([{ totalBytes: 1000, freeBytes: 500 }])
    expect(result.worstPercent).toBe(50)
    expect(result.status).toBe('ok')
  })

  it('returns warning for 75% usage', () => {
    const result = computeDiskHealth([{ totalBytes: 1000, freeBytes: 250 }])
    expect(result.worstPercent).toBe(75)
    expect(result.status).toBe('warning')
  })

  it('returns critical for 95% usage', () => {
    const result = computeDiskHealth([{ totalBytes: 1000, freeBytes: 50 }])
    expect(result.worstPercent).toBe(95)
    expect(result.status).toBe('critical')
  })

  it('picks worst disk', () => {
    const result = computeDiskHealth([
      { totalBytes: 1000, freeBytes: 500 },
      { totalBytes: 1000, freeBytes: 50 },
    ])
    expect(result.worstPercent).toBe(95)
    expect(result.status).toBe('critical')
  })

  it('handles zero total bytes', () => {
    const result = computeDiskHealth([{ totalBytes: 0, freeBytes: 0 }])
    expect(result.worstPercent).toBe(0)
    expect(result.status).toBe('ok')
  })
})

describe('diskPercent', () => {
  it('calculates percentage', () => {
    expect(diskPercent(500, 1000)).toBe(50)
  })
  it('returns 0 when total is 0', () => {
    expect(diskPercent(0, 0)).toBe(0)
  })
  it('rounds to nearest integer', () => {
    expect(diskPercent(333, 1000)).toBe(33)
  })
})

describe('parseUptime', () => {
  it('returns "Unknown" for empty string', () => {
    expect(parseUptime('')).toBe('Unknown')
  })

  it('parses hours only', () => {
    expect(parseUptime('5:30:00')).toBe('5h')
  })

  it('parses days and hours', () => {
    expect(parseUptime('3.12:30:00')).toBe('3d 12h')
  })

  it('parses zero days with hours', () => {
    expect(parseUptime('0.8:15:00')).toBe('8h')
  })
})

describe('assetTypeLabel', () => {
  const cases: [AssetType, string][] = [
    ['Computer', 'Computer'],
    ['Monitor', 'Monitor'],
    ['UsbPeripheral', 'USB Device'],
    ['NetworkPrinter', 'Printer'],
    ['Phone', 'Phone'],
    ['Tablet', 'Tablet'],
    ['Other', 'Other'],
  ]

  it.each(cases)('returns "%s" -> "%s"', (type, label) => {
    expect(assetTypeLabel(type)).toBe(label)
  })
})

describe('assetTypeIcon', () => {
  const types: AssetType[] = ['Computer', 'Monitor', 'UsbPeripheral', 'NetworkPrinter', 'Phone', 'Tablet', 'Other']

  it.each(types)('returns an icon for %s', (type) => {
    expect(assetTypeIcon(type)).toBeTruthy()
    expect(typeof assetTypeIcon(type)).toBe('string')
  })
})

describe('assetStatusLabel', () => {
  const cases: [AssetStatus, string][] = [
    ['InUse', 'In Use'],
    ['InStorage', 'In Storage'],
    ['Decommissioned', 'Decommissioned'],
    ['Lost', 'Lost'],
  ]

  it.each(cases)('returns "%s" -> "%s"', (status, label) => {
    expect(assetStatusLabel(status)).toBe(label)
  })
})

describe('assetStatusColor', () => {
  const statuses: AssetStatus[] = ['InUse', 'InStorage', 'Decommissioned', 'Lost']

  it.each(statuses)('returns color object for %s', (status) => {
    const color = assetStatusColor(status)
    expect(color).toHaveProperty('bg')
    expect(color).toHaveProperty('text')
    expect(color).toHaveProperty('dot')
  })

  it('InUse has green colors', () => {
    const color = assetStatusColor('InUse')
    expect(color.bg).toContain('green')
    expect(color.text).toContain('green')
    expect(color.dot).toContain('green')
  })

  it('Lost has red colors', () => {
    const color = assetStatusColor('Lost')
    expect(color.bg).toContain('red')
    expect(color.text).toContain('red')
    expect(color.dot).toContain('red')
  })

  it('InStorage has blue colors', () => {
    const color = assetStatusColor('InStorage')
    expect(color.bg).toContain('blue')
    expect(color.text).toContain('blue')
    expect(color.dot).toContain('blue')
  })

  it('Decommissioned has gray colors', () => {
    const color = assetStatusColor('Decommissioned')
    expect(color.bg).toContain('gray')
    expect(color.text).toContain('gray')
    expect(color.dot).toContain('gray')
  })
})
