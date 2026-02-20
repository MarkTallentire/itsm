import { test, expect } from '@playwright/test'

test.describe('Asset List Page', () => {
  test('displays page heading and subtitle', async ({ page }) => {
    await page.goto('/assets')
    await expect(page.getByRole('heading', { name: 'Assets' })).toBeVisible()
    await expect(page.getByText('Track and manage all IT assets')).toBeVisible()
  })

  test('shows filter controls', async ({ page }) => {
    await page.goto('/assets')
    // Type dropdown has "All Types" default option
    await expect(page.getByRole('combobox').filter({ hasText: 'All Types' })).toBeVisible()
    // Status dropdown has "All Statuses" default option
    await expect(page.getByRole('combobox').filter({ hasText: 'All Statuses' })).toBeVisible()
    // Search input
    await expect(page.getByPlaceholder('Search assets...')).toBeVisible()
  })

  test('shows Add Asset button that navigates to /assets/new', async ({ page }) => {
    await page.goto('/assets')
    const addButton = page.getByRole('link', { name: /add asset/i })
    await expect(addButton).toBeVisible()
    await addButton.click()
    await expect(page).toHaveURL('/assets/new')
    await expect(page.getByRole('heading', { name: 'New Asset' })).toBeVisible()
  })

  test('shows empty state or asset table', async ({ page }) => {
    await page.goto('/assets')
    const emptyState = page.getByText('No assets found')
    const table = page.locator('table')
    await expect(emptyState.or(table)).toBeVisible()
  })

  test('shows table headers when assets exist', async ({ page }) => {
    await page.request.post('/assets', {
      data: {
        name: 'E2E List Asset',
        type: 'Monitor',
        status: 'InUse',
        source: 'Manual',
      },
    })

    await page.goto('/assets')
    const table = page.locator('table')
    await expect(table).toBeVisible()

    const headers = ['Type', 'Name', 'Serial Number', 'Status', 'Assigned User', 'Location', 'Source', 'Last Updated']
    for (const header of headers) {
      await expect(table.getByRole('columnheader', { name: header })).toBeVisible()
    }
  })
})
