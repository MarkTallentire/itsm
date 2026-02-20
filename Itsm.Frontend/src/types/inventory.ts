export interface MachineIdentity {
  computerName: string
  modelName: string
  serialNumber: string
  hardwareUuid: string
  loggedInUser: string
  chassisType: string
}

export interface CpuInfo {
  brandString: string
  coreCount: number
  architecture: string
}

export interface MemoryInfo {
  totalBytes: number
}

export interface DiskInfo {
  name: string
  format: string
  totalBytes: number
  freeBytes: number
}

export interface OsInfo {
  description: string
}

export interface NetworkInterfaceInfo {
  name: string
  macAddress: string
  ipAddresses: string[]
}

export interface NetworkInfo {
  hostname: string
  interfaces: NetworkInterfaceInfo[]
}

export interface Computer {
  identity: MachineIdentity
  cpu: CpuInfo
  memory: MemoryInfo
  disks: DiskInfo[]
  os: OsInfo
  network: NetworkInfo
}

export interface ComputerRecord {
  computerName: string
  lastUpdatedUtc: string
  data: Computer
}

export interface DirectoryNode {
  path: string
  sizeBytes: number
  children: DirectoryNode[]
}

export interface DiskUsageSnapshot {
  computerName: string
  scannedAtUtc: string
  minimumSizeBytes: number
  roots: DirectoryNode[]
}

export interface DiskUsageRecord {
  computerName: string
  scannedAtUtc: string
  data: DiskUsageSnapshot
}

export interface AgentRecord {
  hardwareUuid: string
  computerName: string
  displayName: string
  agentVersion: string
  isConnected: boolean
  lastSeenUtc: string
  firstSeenUtc: string
}

export type UpdateType = 'Inventory' | 'DiskUsage'

export interface LogEntry {
  timestampUtc: string
  level: string
  category: string
  message: string
}
