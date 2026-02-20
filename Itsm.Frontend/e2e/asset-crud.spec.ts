import { test, expect } from '@playwright/test'

test.describe('Asset CRUD', () => {
  test('create a new manual asset', async ({ page }) => {
    await page.goto('/assets/new')
    await expect(page.getByRole('heading', { name: 'New Asset' })).toBeVisible()

    // Fill required fields
    await page.locator('input[type="text"]').first().fill('E2E Test Phone')
    await page.locator('select').selectOption('Phone')

    // Fill optional fields
    await page.locator('input[type="text"]').nth(1).fill('SN-E2E-CRUD-001')
    await page.locator('input[type="text"]').nth(2).fill('Jane Doe')

    // Submit the form
    await page.getByRole('button', { name: /create asset/i }).click()

    // Should navigate to asset detail page
    await expect(page).toHaveURL(/\/assets\//)
    await expect(page.getByText('E2E Test Phone')).toBeVisible()
  })

  test('create, edit, and verify persistence', async ({ page }) => {
    // Create via API for isolation
    const response = await page.request.post('/assets', {
      data: {
        name: 'E2E Edit Asset',
        type: 'Tablet',
        status: 'InUse',
        source: 'Manual',
      },
    })
    const asset = await response.json()

    // Navigate to detail page
    await page.goto(`/assets/${asset.id}`)
    await expect(page.getByText('E2E Edit Asset')).toBeVisible()

    // Edit status and notes
    await page.locator('select').selectOption('InStorage')
    await page.locator('textarea').fill('Stored in IT closet')
    await page.getByRole('button', { name: /save changes/i }).click()

    // Verify toast confirmation
    await expect(page.getByText('Asset saved')).toBeVisible()

    // Reload and verify persistence
    await page.reload()
    await expect(page.locator('textarea')).toHaveValue('Stored in IT closet')
    await expect(page.locator('select')).toHaveValue('InStorage')
  })

  test('delete an asset', async ({ page }) => {
    // Create via API
    const response = await page.request.post('/assets', {
      data: {
        name: 'E2E Delete Asset',
        type: 'Phone',
        status: 'InUse',
        source: 'Manual',
      },
    })
    const asset = await response.json()

    await page.goto(`/assets/${asset.id}`)
    await expect(page.getByText('E2E Delete Asset')).toBeVisible()

    // Accept the confirmation dialog
    page.on('dialog', (dialog) => dialog.accept())
    await page.getByRole('button', { name: /delete/i }).click()

    // Should redirect to /assets
    await expect(page).toHaveURL('/assets')
  })

  test('cancel button on create form returns to asset list', async ({ page }) => {
    await page.goto('/assets/new')
    await page.getByRole('link', { name: /cancel/i }).click()
    await expect(page).toHaveURL('/assets')
  })

  test('back to assets link on detail page', async ({ page }) => {
    const response = await page.request.post('/assets', {
      data: {
        name: 'E2E Back Link Asset',
        type: 'Other',
        status: 'InUse',
        source: 'Manual',
      },
    })
    const asset = await response.json()

    await page.goto(`/assets/${asset.id}`)
    await page.getByRole('link', { name: /back to assets/i }).click()
    await expect(page).toHaveURL('/assets')
  })
})
