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
  version: string | null
  buildNumber: string | null
}

export interface GpuInfo {
  name: string
  vendor: string
  vramBytes: number | null
  driverVersion: string | null
}

export interface BatteryInfo {
  isPresent: boolean
  chargePercent: number | null
  cycleCount: number | null
  healthPercent: number | null
  isCharging: boolean | null
  condition: string | null
}

export interface InstalledApp {
  name: string
  version: string
  installDate: string | null
}

export interface UptimeInfo {
  lastBootUtc: string
  uptime: string
}

export interface FirewallInfo {
  isEnabled: boolean
  stealthMode: boolean | null
}

export interface EncryptionInfo {
  isEnabled: boolean
  method: string | null
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
  gpus: GpuInfo[]
  battery: BatteryInfo
  installedApps: InstalledApp[]
  uptime: UptimeInfo
  firewall: FirewallInfo
  encryption: EncryptionInfo
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

export type UpdateType = 'Inventory' | 'DiskUsage' | 'Peripherals'

export type AssetType = 'Computer' | 'Monitor' | 'UsbPeripheral' | 'NetworkPrinter' | 'Phone' | 'Tablet' | 'Other'
export type AssetStatus = 'InUse' | 'InStorage' | 'Decommissioned' | 'Lost'

export interface Asset {
  id: string
  name: string
  type: AssetType
  status: AssetStatus
  serialNumber: string | null
  assignedUser: string | null
  location: string | null
  purchaseDate: string | null
  warrantyExpiry: string | null
  cost: number | null
  notes: string | null
  source: 'Agent' | 'Manual'
  discoveredByAgent: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface PrinterDetail {
  id: string
  name: string
  status: AssetStatus
  serialNumber: string | null
  assignedUser: string | null
  location: string | null
  purchaseDate: string | null
  warrantyExpiry: string | null
  cost: number | null
  notes: string | null
  source: 'Agent' | 'Manual'
  discoveredByAgent: string | null
  createdAtUtc: string
  updatedAtUtc: string
  ipAddress: string
  macAddress: string | null
  firmwareVersion: string | null
  pageCount: number | null
  tonerBlackPercent: number | null
  tonerCyanPercent: number | null
  tonerMagentaPercent: number | null
  tonerYellowPercent: number | null
  printerStatus: string | null
  manufacturer: string | null
  model: string | null
}

export interface LogEntry {
  timestampUtc: string
  level: string
  category: string
  message: string
}
