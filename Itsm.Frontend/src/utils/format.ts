export function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  const value = bytes / Math.pow(1024, i)
  return `${value.toFixed(i === 0 ? 0 : 1)} ${units[i]}`
}

export function chassisLabel(type: number): string {
  const labels: Record<number, string> = {
    0: 'Unknown',
    1: 'Laptop',
    2: 'Desktop',
    3: 'Tower',
    4: 'Mini',
    5: 'All-in-One',
    6: 'Tablet',
  }
  return labels[type] ?? 'Unknown'
}
