import { test, expect } from '@playwright/test'

const computerName = 'E2E-Detail-PC'
const hardwareUuid = '00000000-0000-0000-0000-e2edetail001'

test.describe('Computer Detail Page', () => {
  test.beforeAll(async ({ request }) => {
    await request.post('/inventory/computer', {
      data: {
        computerName,
        data: {
          identity: {
            computerName,
            modelName: 'MacBook Pro 16"',
            serialNumber: 'SN-DETAIL-001',
            hardwareUuid,
            loggedInUser: 'jdoe',
            chassisType: 'Laptop',
          },
          os: { description: 'macOS 15.3 Sequoia', version: '15.3', buildNumber: '24D60' },
          cpu: { brandString: 'Apple M4 Pro', coreCount: 14, architecture: 'arm64' },
          memory: { totalBytes: 38654705664 },
          disks: [
            { name: 'Macintosh HD', totalBytes: 1000000000000, freeBytes: 400000000000, format: 'APFS' },
          ],
          network: {
            hostname: 'e2e-detail-pc',
            interfaces: [
              { name: 'en0', macAddress: 'AA:BB:CC:DD:EE:01', ipAddresses: ['192.168.1.100'] },
            ],
          },
          firewall: { isEnabled: true, stealthMode: true },
          encryption: { isEnabled: true, method: 'FileVault' },
          uptime: { uptime: '3.08:15:00', lastBootUtc: '2026-02-17T04:00:00Z' },
          installedApps: [
            { name: 'Visual Studio Code', version: '1.96.0', installDate: '2026-01-15' },
            { name: 'Slack', version: '4.40.0', installDate: '2026-01-10' },
            { name: 'Docker Desktop', version: '4.36.0', installDate: '2026-02-01' },
            { name: 'Firefox', version: '134.0', installDate: '2026-02-05' },
          ],
          gpus: [{ name: 'Apple M4 Pro GPU', vendor: 'Apple', vramBytes: null, driverVersion: null }],
          battery: { isPresent: true, chargePercent: 85, isCharging: false, healthPercent: 94, cycleCount: 127, condition: 'Normal' },
        },
      },
    })
  })

  test('shows computer name and model in header', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)
    await expect(page.getByRole('heading', { name: computerName })).toBeVisible()
    await expect(page.getByText('MacBook Pro 16"')).toBeVisible()
    await expect(page.getByText('jdoe')).toBeVisible()
    await expect(page.getByText('macOS 15.3 Sequoia')).toBeVisible()
  })

  test('shows compliance cards', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Firewall card
    await expect(page.getByText('Firewall')).toBeVisible()
    await expect(page.getByText('Enabled').first()).toBeVisible()

    // Encryption card
    await expect(page.getByText('Encryption')).toBeVisible()
    await expect(page.getByText('FileVault')).toBeVisible()

    // Disk Health card
    await expect(page.getByText('Disk Health')).toBeVisible()

    // Uptime card
    await expect(page.getByText('Uptime')).toBeVisible()
  })

  test('collapsible sections can be toggled', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Hardware section is open by default
    await expect(page.getByText('Apple M4 Pro')).toBeVisible()

    // Click Hardware heading to collapse
    await page.getByRole('button', { name: 'Hardware' }).click()
    await expect(page.getByText('Apple M4 Pro')).not.toBeVisible()

    // Click again to expand
    await page.getByRole('button', { name: 'Hardware' }).click()
    await expect(page.getByText('Apple M4 Pro')).toBeVisible()
  })

  test('Network section expands to show interface table', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Network is closed by default
    await page.getByRole('button', { name: 'Network' }).click()

    await expect(page.getByText('en0')).toBeVisible()
    await expect(page.getByText('AA:BB:CC:DD:EE:01')).toBeVisible()
    await expect(page.getByText('192.168.1.100')).toBeVisible()
  })

  test('Installed Software section with search filter', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Open the Installed Software section (closed by default)
    await page.getByRole('button', { name: /Installed Software/ }).click()

    // All apps should be visible
    await expect(page.getByText('Visual Studio Code')).toBeVisible()
    await expect(page.getByText('Slack')).toBeVisible()
    await expect(page.getByText('Docker Desktop')).toBeVisible()
    await expect(page.getByText('Firefox')).toBeVisible()

    // Search for a specific app
    await page.getByPlaceholder('Search apps...').fill('Docker')
    await expect(page.getByText('Docker Desktop')).toBeVisible()
    await expect(page.getByText('Slack')).not.toBeVisible()
    await expect(page.getByText('Firefox')).not.toBeVisible()

    // Clear search shows all apps again
    await page.getByPlaceholder('Search apps...').clear()
    await expect(page.getByText('Slack')).toBeVisible()
    await expect(page.getByText('Firefox')).toBeVisible()
  })

  test('Storage section shows disk info', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Storage is open by default
    await expect(page.getByText('Macintosh HD')).toBeVisible()
    await expect(page.getByText('APFS')).toBeVisible()
  })

  test('Battery section shows charge info', async ({ page }) => {
    await page.goto(`/computers/${computerName}`)

    // Battery section should be open by default when present
    await expect(page.getByText('Battery')).toBeVisible()
    await expect(page.getByText('85%')).toBeVisible()
    await expect(page.getByText('On Battery')).toBeVisible()
    await expect(page.getByText('127')).toBeVisible() // cycle count
  })
})
