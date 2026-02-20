import type { ComputerRecord, DiskUsageRecord } from '../types/inventory'

export async function fetchComputers(): Promise<ComputerRecord[]> {
  const res = await fetch('/inventory/computers')
  if (!res.ok) throw new Error(`Failed to fetch computers: ${res.status}`)
  return res.json()
}

export async function fetchComputer(name: string): Promise<ComputerRecord> {
  const res = await fetch(`/inventory/computers/${encodeURIComponent(name)}`)
  if (!res.ok) throw new Error(`Failed to fetch computer: ${res.status}`)
  return res.json()
}

export async function fetchDiskUsage(name: string): Promise<DiskUsageRecord> {
  const res = await fetch(`/inventory/disk-usage/${encodeURIComponent(name)}`)
  if (!res.ok) throw new Error(`Failed to fetch disk usage: ${res.status}`)
  return res.json()
}
