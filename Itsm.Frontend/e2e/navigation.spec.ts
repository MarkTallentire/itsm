import { test, expect } from '@playwright/test'

test.describe('Navigation', () => {
  test('sidebar shows Computers and Assets links on hover', async ({ page }) => {
    await page.goto('/')
    const sidebar = page.locator('aside')
    await sidebar.hover()
    await expect(sidebar.getByText('Computers')).toBeVisible()
    await expect(sidebar.getByText('Assets')).toBeVisible()
  })

  test('clicking Assets link navigates to /assets', async ({ page }) => {
    await page.goto('/')
    const sidebar = page.locator('aside')
    await sidebar.hover()
    await sidebar.getByRole('link', { name: /assets/i }).click()
    await expect(page).toHaveURL('/assets')
    await expect(page.getByRole('heading', { name: 'Assets' })).toBeVisible()
  })

  test('clicking Computers link navigates to /', async ({ page }) => {
    await page.goto('/assets')
    const sidebar = page.locator('aside')
    await sidebar.hover()
    await sidebar.getByRole('link', { name: /computers/i }).click()
    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Computers' })).toBeVisible()
  })

  test('sidebar logo links to home', async ({ page }) => {
    await page.goto('/assets')
    const sidebar = page.locator('aside')
    // The IT logo link points to /
    await sidebar.locator('a').first().click()
    await expect(page).toHaveURL('/')
  })
})
