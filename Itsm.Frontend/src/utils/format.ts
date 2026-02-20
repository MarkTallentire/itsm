export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  const value = bytes / Math.pow(1024, i)
  return `${value.toFixed(i === 0 ? 0 : 1)} ${units[i]}`
}

export function chassisLabel(type: string): string {
  const labels: Record<string, string> = {
    Laptop: 'Laptop',
    Desktop: 'Desktop',
    Tower: 'Tower',
    Mini: 'Mini',
    AllInOne: 'All-in-One',
    Tablet: 'Tablet',
  }
  return labels[type] ?? 'Unknown'
}

export function chassisIcon(type: string): string {
  const icons: Record<string, string> = {
    Laptop: '\uD83D\uDCBB',
    Desktop: '\uD83D\uDDA5',
    Tower: '\uD83D\uDDA5',
    Mini: '\u25AA',
    AllInOne: '\uD83D\uDDA5',
    Tablet: '\uD83D\uDCF1',
  }
  return icons[type] ?? '\u2753'
}

export function timeAgo(utcString: string): string {
  const dateStr = utcString.endsWith('Z') ? utcString : utcString + 'Z'
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  return `${days}d ago`
}

export interface ComplianceStatus {
  passing: boolean
  issues: string[]
}

export function computeComplianceStatus(
  firewall: { isEnabled: boolean } | null | undefined,
  encryption: { isEnabled: boolean } | null | undefined,
): ComplianceStatus {
  const issues: string[] = []
  if (!firewall || !firewall.isEnabled) issues.push('Firewall disabled')
  if (!encryption || !encryption.isEnabled) issues.push('Encryption disabled')
  return { passing: issues.length === 0, issues }
}

export interface DiskHealth {
  worstPercent: number
  status: 'ok' | 'warning' | 'critical'
}

export function computeDiskHealth(
  disks: { totalBytes: number; freeBytes: number }[],
): DiskHealth {
  if (disks.length === 0) return { worstPercent: 0, status: 'ok' }
  let worstPercent = 0
  for (const d of disks) {
    const used = d.totalBytes === 0 ? 0 : Math.round(((d.totalBytes - d.freeBytes) / d.totalBytes) * 100)
    if (used > worstPercent) worstPercent = used
  }
  const status = worstPercent > 90 ? 'critical' : worstPercent > 70 ? 'warning' : 'ok'
  return { worstPercent, status }
}

export function diskPercent(used: number, total: number): number {
  return total === 0 ? 0 : Math.round((used / total) * 100)
}

export function parseUptime(timeSpanStr: string): string {
  if (!timeSpanStr) return 'Unknown'
  const parts = timeSpanStr.split(':')
  let days = 0
  let hours = 0
  if (parts.length >= 2) {
    const firstPart = parts[0]
    if (firstPart.includes('.')) {
      const [d, h] = firstPart.split('.')
      days = parseInt(d, 10) || 0
      hours = parseInt(h, 10) || 0
    } else {
      hours = parseInt(firstPart, 10) || 0
    }
  }
  if (days > 0) return `${days}d ${hours}h`
  return `${hours}h`
}

import type { AssetType, AssetStatus } from '../types/inventory'

export function assetTypeLabel(type: AssetType): string {
  const labels: Record<AssetType, string> = {
    Computer: 'Computer',
    Monitor: 'Monitor',
    UsbPeripheral: 'USB Device',
    NetworkPrinter: 'Printer',
    Phone: 'Phone',
    Tablet: 'Tablet',
    Other: 'Other',
  }
  return labels[type] ?? type
}

export function assetTypeIcon(type: AssetType): string {
  const icons: Record<AssetType, string> = {
    Computer: '\uD83D\uDCBB',
    Monitor: '\uD83D\uDDA5',
    UsbPeripheral: '\uD83D\uDD0C',
    NetworkPrinter: '\uD83D\uDDA8',
    Phone: '\uD83D\uDCF1',
    Tablet: '\uD83D\uDCF1',
    Other: '\uD83D\uDCE6',
  }
  return icons[type] ?? '\u2753'
}

export function assetStatusLabel(status: AssetStatus): string {
  const labels: Record<AssetStatus, string> = {
    InUse: 'In Use',
    InStorage: 'In Storage',
    Decommissioned: 'Decommissioned',
    Lost: 'Lost',
  }
  return labels[status] ?? status
}

export function assetStatusColor(status: AssetStatus): { bg: string; text: string; dot: string } {
  const colors: Record<AssetStatus, { bg: string; text: string; dot: string }> = {
    InUse: { bg: 'bg-green-50', text: 'text-green-700', dot: 'bg-green-500' },
    InStorage: { bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500' },
    Decommissioned: { bg: 'bg-gray-100', text: 'text-gray-600', dot: 'bg-gray-400' },
    Lost: { bg: 'bg-red-50', text: 'text-red-700', dot: 'bg-red-500' },
  }
  return colors[status] ?? { bg: 'bg-gray-100', text: 'text-gray-600', dot: 'bg-gray-400' }
}
