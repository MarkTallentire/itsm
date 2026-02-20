import type { ComputerRecord, DiskUsageRecord, AgentRecord, UpdateType, LogEntry } from '../types/inventory'

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
  const es = new EventSource(`/agents/${encodeURIComponent(hardwareUuid)}/logs/stream`)
  es.onmessage = (event) => {
    onLog(JSON.parse(event.data))
  }
  return es
}
