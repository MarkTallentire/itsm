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
  threadCount: number | null
  architecture: string
  speedMHz: number | null
}

export interface MemoryModule {
  slotLabel: string | null
  capacityBytes: number
  speedMHz: number | null
  type: string | null
  manufacturer: string | null
  serialNumber: string | null
}

export interface MemoryInfo {
  totalBytes: number
  modules: MemoryModule[]
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
  kernelName: string | null
  kernelVersion: string | null
  architecture: string | null
  installDate: string | null
  licenseKey: string | null
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
  publisher: string | null
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
  speedMbps: number | null
  interfaceType: string | null
  isDhcp: boolean | null
  gateway: string | null
  subnetMask: string | null
  wifiSsid: string | null
  wifiFrequencyGHz: number | null
  wifiSignalDbm: number | null
}

export interface VpnConnection {
  name: string
  type: string | null
  serverAddress: string | null
  isConnected: boolean
}

export interface DnsConfiguration {
  servers: string[]
  domain: string | null
  searchDomains: string[]
}

export interface NetworkDrive {
  localPath: string
  remotePath: string
  fileSystem: string | null
}

export interface ListeningPort {
  port: number
  protocol: string
  processName: string | null
  pid: number | null
}

export interface NetworkInfo {
  hostname: string
  interfaces: NetworkInterfaceInfo[]
  vpnConnections: VpnConnection[]
  dns: DnsConfiguration
  networkDrives: NetworkDrive[]
  listeningPorts: ListeningPort[]
}

export interface BiosInfo {
  manufacturer: string | null
  version: string | null
  releaseDate: string | null
  serial: string | null
}

export interface MotherboardInfo {
  manufacturer: string | null
  product: string | null
  serial: string | null
  version: string | null
}

export interface AntivirusInfo {
  name: string
  version: string | null
  isEnabled: boolean
  isUpToDate: boolean | null
  expirationDate: string | null
}

export interface SystemController {
  name: string
  manufacturer: string | null
  type: string | null
  pciId: string | null
}

export interface VmInstance {
  name: string
  state: string | null
  type: string | null
  memoryMB: number | null
  cpuCount: number | null
}

export interface DockerContainer {
  id: string
  name: string
  image: string
  state: string
  status: string | null
}

export interface VirtualizationInfo {
  virtualMachines: VmInstance[]
  dockerContainers: DockerContainer[]
}

export interface DatabaseInstanceInfo {
  name: string
  version: string | null
  port: number | null
  isRunning: boolean
}

export interface LocationInfo {
  latitude: number | null
  longitude: number | null
  city: string | null
  region: string | null
  country: string | null
  timezone: string | null
  publicIp: string | null
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
  bios: BiosInfo
  motherboard: MotherboardInfo
  antivirus: AntivirusInfo[]
  controllers: SystemController[]
  virtualization: VirtualizationInfo
  databases: DatabaseInstanceInfo[]
  location: LocationInfo | null
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
  discoveredByComputerName: string | null
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
  discoveredByComputerName: string | null
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
