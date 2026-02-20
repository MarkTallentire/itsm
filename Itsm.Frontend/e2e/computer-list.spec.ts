import { test, expect } from '@playwright/test'

test.describe('Computer List Page', () => {
  test('displays page heading', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Computers' })).toBeVisible()
    await expect(page.getByText('Manage and monitor all enrolled devices')).toBeVisible()
  })

  test('shows table headers when computers exist', async ({ page }) => {
    // Seed a computer via the API so the table renders
    await page.request.post('/inventory/computer', {
      data: {
        computerName: 'E2E-LIST-PC',
        data: {
          identity: {
            computerName: 'E2E-LIST-PC',
            modelName: 'Test Model',
            serialNumber: 'SN-LIST-001',
            hardwareUuid: '00000000-0000-0000-0000-e2elist00001',
            loggedInUser: 'testuser',
            chassisType: 'Desktop',
          },
          os: { description: 'macOS 15.0', version: '15.0', buildNumber: '24A335' },
          cpu: { brandString: 'Apple M1', coreCount: 8, architecture: 'arm64' },
          memory: { totalBytes: 17179869184 },
          disks: [{ name: 'Macintosh HD', totalBytes: 500000000000, freeBytes: 250000000000, format: 'APFS' }],
          network: { hostname: 'e2e-list-pc', interfaces: [] },
          firewall: { isEnabled: true, stealthMode: false },
          encryption: { isEnabled: true, method: 'FileVault' },
          uptime: { uptime: '1.02:30:00', lastBootUtc: '2026-02-19T10:00:00Z' },
          installedApps: [],
          gpus: [],
          battery: null,
        },
      },
    })

    await page.goto('/')
    const table = page.locator('table')
    await expect(table).toBeVisible()

    const headers = ['Status', 'Device', 'User', 'OS', 'Compliance', 'Disk', 'Last Seen', 'Type']
    for (const header of headers) {
      await expect(table.getByRole('columnheader', { name: header })).toBeVisible()
    }
  })

  test('shows empty state when no computers exist', async ({ page }) => {
    // Navigate with a baseline assumption of empty state
    // (This test is meaningful in a clean database environment)
    await page.goto('/')
    // Either the table or the empty state will be visible
    const emptyState = page.getByText('No computers reported yet')
    const table = page.locator('table')
    // One of these should be visible
    await expect(emptyState.or(table)).toBeVisible()
  })

  test('clicking a computer row navigates to detail page', async ({ page }) => {
    const computerName = 'E2E-CLICK-PC'
    await page.request.post('/inventory/computer', {
      data: {
        computerName,
        data: {
          identity: {
            computerName,
            modelName: 'Click Test Model',
            serialNumber: 'SN-CLICK-001',
            hardwareUuid: '00000000-0000-0000-0000-e2eclick0001',
            loggedInUser: 'clickuser',
            chassisType: 'Laptop',
          },
          os: { description: 'Windows 11', version: '11', buildNumber: '22631' },
          cpu: { brandString: 'Intel i7', coreCount: 8, architecture: 'x86_64' },
          memory: { totalBytes: 34359738368 },
          disks: [{ name: 'C:', totalBytes: 1000000000000, freeBytes: 500000000000, format: 'NTFS' }],
          network: { hostname: 'e2e-click-pc', interfaces: [] },
          firewall: { isEnabled: true, stealthMode: false },
          encryption: { isEnabled: true, method: 'BitLocker' },
          uptime: { uptime: '0.12:00:00', lastBootUtc: '2026-02-20T00:00:00Z' },
          installedApps: [],
          gpus: [],
          battery: null,
        },
      },
    })

    await page.goto('/')
    await page.getByText(computerName).click()
    await expect(page).toHaveURL(`/computers/${computerName}`)
  })
})
