import { test, expect } from '@playwright/test'

test.describe('Asset Filtering', () => {
  const testPrefix = `E2E-Filter-${Date.now()}`

  test.beforeAll(async ({ request }) => {
    await request.post('/assets', {
      data: { name: `${testPrefix} Computer`, type: 'Computer', status: 'InUse', source: 'Manual' },
    })
    await request.post('/assets', {
      data: { name: `${testPrefix} Phone`, type: 'Phone', status: 'InStorage', source: 'Manual' },
    })
    await request.post('/assets', {
      data: { name: `${testPrefix} Monitor`, type: 'Monitor', status: 'Decommissioned', source: 'Manual' },
    })
  })

  test('filter by type shows only matching assets', async ({ page }) => {
    await page.goto('/assets')

    // Select Phone type filter
    await page.locator('select').first().selectOption('Phone')

    // Should show the phone, not the computer or monitor
    await expect(page.getByText(`${testPrefix} Phone`)).toBeVisible()
    await expect(page.getByText(`${testPrefix} Computer`)).not.toBeVisible()
    await expect(page.getByText(`${testPrefix} Monitor`)).not.toBeVisible()
  })

  test('filter by status shows only matching assets', async ({ page }) => {
    await page.goto('/assets')

    // Select InStorage status filter
    await page.locator('select').nth(1).selectOption('InStorage')

    // Should show the phone (InStorage), not the computer (InUse)
    await expect(page.getByText(`${testPrefix} Phone`)).toBeVisible()
    await expect(page.getByText(`${testPrefix} Computer`)).not.toBeVisible()
  })

  test('search by name filters results', async ({ page }) => {
    await page.goto('/assets')

    await page.getByPlaceholder('Search assets...').fill(`${testPrefix} Monitor`)

    // Wait for debounced search to complete
    await expect(page.getByText(`${testPrefix} Monitor`)).toBeVisible()
    await expect(page.getByText(`${testPrefix} Phone`)).not.toBeVisible()
    await expect(page.getByText(`${testPrefix} Computer`)).not.toBeVisible()
  })

  test('clearing filters shows all assets', async ({ page }) => {
    await page.goto('/assets')

    // Apply a type filter
    await page.locator('select').first().selectOption('Phone')
    await expect(page.getByText(`${testPrefix} Computer`)).not.toBeVisible()

    // Clear the filter by selecting "All Types"
    await page.locator('select').first().selectOption('')
    await expect(page.getByText(`${testPrefix} Computer`)).toBeVisible()
    await expect(page.getByText(`${testPrefix} Phone`)).toBeVisible()
  })

  test('combining type and status filters', async ({ page }) => {
    await page.goto('/assets')

    // Filter by Phone type AND InStorage status
    await page.locator('select').first().selectOption('Phone')
    await page.locator('select').nth(1).selectOption('InStorage')

    await expect(page.getByText(`${testPrefix} Phone`)).toBeVisible()
    await expect(page.getByText(`${testPrefix} Computer`)).not.toBeVisible()
    await expect(page.getByText(`${testPrefix} Monitor`)).not.toBeVisible()
  })
})
