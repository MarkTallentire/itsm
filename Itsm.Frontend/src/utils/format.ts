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
