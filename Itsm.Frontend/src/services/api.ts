import type { ComputerRecord, DiskUsageRecord, AgentRecord, UpdateType, LogEntry, Asset, PrinterDetail } from '../types/inventory'

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

export async function fetchAgents(): Promise<AgentRecord[]> {
  const res = await fetch('/agents')
  if (!res.ok) throw new Error(`Failed to fetch agents: ${res.status}`)
  return res.json()
}

export async function fetchAgent(hardwareUuid: string): Promise<AgentRecord> {
  const res = await fetch(`/agents/${encodeURIComponent(hardwareUuid)}`)
  if (!res.ok) throw new Error(`Failed to fetch agent: ${res.status}`)
  return res.json()
}

export async function requestUpdate(hardwareUuid: string, updateType: UpdateType): Promise<void> {
  const res = await fetch(`/agents/${encodeURIComponent(hardwareUuid)}/request-update`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(updateType),
  })
  if (!res.ok) throw new Error(`Failed to request update: ${res.status}`)
}

export async function fetchLogs(hardwareUuid: string): Promise<LogEntry[]> {
  const res = await fetch(`/agents/${encodeURIComponent(hardwareUuid)}/logs`)
  if (!res.ok) throw new Error(`Failed to fetch logs: ${res.status}`)
  return res.json()
}

export function streamLogs(hardwareUuid: string, onLog: (entry: LogEntry) => void): EventSource {
  const url = `/agents/${encodeURIComponent(hardwareUuid)}/logs/stream`
  const es = new EventSource(url)
  es.onmessage = (event) => {
    onLog(JSON.parse(event.data))
  }
  es.onerror = () => {
    // EventSource auto-reconnects on error; no action needed.
    // This handler just prevents console noise.
  }
  return es
}

export async function fetchAssets(params?: { type?: string; status?: string; search?: string }): Promise<Asset[]> {
  const query = new URLSearchParams()
  if (params?.type) query.set('type', params.type)
  if (params?.status) query.set('status', params.status)
  if (params?.search) query.set('search', params.search)
  const qs = query.toString()
  const res = await fetch(`/assets${qs ? '?' + qs : ''}`)
  if (!res.ok) throw new Error(`Failed to fetch assets: ${res.status}`)
  return res.json()
}

export async function fetchAsset(id: string): Promise<Asset> {
  const res = await fetch(`/assets/${encodeURIComponent(id)}`)
  if (!res.ok) throw new Error(`Failed to fetch asset: ${res.status}`)
  return res.json()
}

export async function createAsset(asset: Partial<Asset>): Promise<Asset> {
  const res = await fetch('/assets', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(asset),
  })
  if (!res.ok) throw new Error(`Failed to create asset: ${res.status}`)
  return res.json()
}

export async function updateAsset(id: string, asset: Partial<Asset>): Promise<Asset> {
  const res = await fetch(`/assets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(asset),
  })
  if (!res.ok) throw new Error(`Failed to update asset: ${res.status}`)
  return res.json()
}

export async function deleteAsset(id: string): Promise<void> {
  const res = await fetch(`/assets/${encodeURIComponent(id)}`, { method: 'DELETE' })
  if (!res.ok) throw new Error(`Failed to delete asset: ${res.status}`)
}

export async function fetchPrinterDetail(id: string): Promise<PrinterDetail> {
  const res = await fetch(`/assets/${encodeURIComponent(id)}/printer`)
  if (!res.ok) throw new Error(`Failed to fetch printer: ${res.status}`)
  return res.json()
}
