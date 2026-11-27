import { test, expect } from '@playwright/test'
import type { Locator, Page } from '@playwright/test'
import { pathToFileURL } from 'node:url'
import { resolve } from 'node:path'
import { contrastRatio } from '../src/shared/lib/colors.ts'

async function checkContrast(locator: Locator) {
  const colors = await locator.evaluateAll((elements) =>
    elements
      .filter((element) => element.getBoundingClientRect().width > 0)
      .map((element) => {
        const style = getComputedStyle(element)
        let parent: Element | null = element
        let background = 'rgb(255, 255, 255)'
        while (parent) {
          const color = getComputedStyle(parent).backgroundColor
          if (color !== 'rgba(0, 0, 0, 0)' && color !== 'transparent') {
            background = color
            break
          }
          parent = parent.parentElement
        }
        return {
          text: element.textContent?.trim().slice(0, 70),
          foreground: style.color,
          background,
        }
      }),
  )
  const hex = (color: string) =>
    '#' +
    color
      .match(/[\d.]+/g)!
      .slice(0, 3)
      .map((channel) => Math.round(Number(channel)).toString(16).padStart(2, '0'))
      .join('')
  for (const color of colors)
    expect(
      contrastRatio(hex(color.foreground), hex(color.background)),
      JSON.stringify(color),
    ).toBeGreaterThanOrEqual(4.5)
}
async function app(page: Page, leader = false) {
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => {
      const path = new URL(route.request().url()).pathname
      if (path === '/api/Suggestions/search')
        return route.fulfill({
          json: { items: [], totalCount: 0, pageNumber: 1, pageSize: 20 },
        })
      const data =
        path === '/api/Users/me'
          ? {
              id: 'design-user',
              email: 'design@example.test',
              fullName: 'Анна Иванова',
              position: '',
              departmentId: null,
              roles: leader ? ['AdminL1'] : [],
            }
          : path === '/api/Templates/'
            ? [
                'Модернизация',
                'Сокращение времени',
                'Безопасность',
                'Качество',
                'Экономия ресурсов',
                'Другое',
                'Особый шаблон',
              ].map((title, index) => ({
                id: `11111111-1111-4111-8111-${String(index).padStart(12, '0')}`,
                type: 'Template',
                departmentId: null,
                improvementObject: 'Процесс',
                title,
                problemHint: '',
                solutionHint: '',
                isActive: true,
              }))
            : path === '/api/Suggestions/'
              ? [
                  'Draft',
                  'Submitted',
                  'ReturnedByManager',
                  'InCommission',
                  'RevisionRequired',
                  'Rejected',
                  'ApprovedPlanning',
                  'InImplementation',
                  'Implemented',
                ].map((status, i) => ({
                  id: `22222222-2222-4222-8222-${String(i).padStart(12, '0')}`,
                  title: `Предложение ${i}`,
                  status,
                  created: '2026-11-23T10:00:00Z',
                }))
              : []
      return route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) })
    },
  )
}
for (const theme of ['light', 'dark'] as const) {
  test(`standalone reference keeps icons and text visible in ${theme}`, async ({
    page,
  }, testInfo) => {
    await page.goto(pathToFileURL(resolve('../docs/design/ideahub.html')).href, {
      waitUntil: 'domcontentloaded',
    })
    await page.evaluate(`demoLogin('employee');applyTheme('${theme}')`)
    const icons = page.locator('.template .emoji .ui-icon')
    await expect(icons).toHaveCount(6)
    const shapes = await icons
      .locator('use')
      .evaluateAll((elements) => elements.map((e) => e.getAttribute('href')))
    expect(new Set(shapes).size).toBe(6)
    expect(
      await icons.evaluateAll((elements) =>
        elements.every((e) => (e as SVGGraphicsElement).getBBox().width > 0),
      ),
    ).toBe(true)
    for (const status of await page.locator('.idea .status').all()) {
      await expect(status.locator('.ui-icon')).toHaveCount(1)
      expect(await status.evaluate((e) => getComputedStyle(e, '::before').content)).toBe('none')
    }
    await checkContrast(
      page.locator(
        '.template .emoji,.template b,.template span:not(.arrow),.idea p,.idea-author,.idea .status,.empty',
      ),
    )
    await page
      .locator('.template-grid')
      .screenshot({ path: testInfo.outputPath(`reference-templates-${theme}.png`) })
    await page
      .locator('#employeeBoard')
      .screenshot({ path: testInfo.outputPath(`reference-ideas-${theme}.png`) })
    await page.evaluate(`demoLogin('chief');goAdmin('company');applyTheme('${theme}')`)
    for (const color of ['#f3f6ec', '#ffff00', '#000000', '#0000ff', '#39775b']) {
      await page.evaluate(`brandColor='${color}';applyBrand();updateBrandPreview()`)
      await checkContrast(
        page.locator(
          '.btn-primary,.nav-btn.active,.avatar,.brandmark,.dark-preview b,.brand-preview p',
        ),
      )
    }
  })
  test(`application shares readable template and status icons in ${theme}`, async ({
    page,
  }, testInfo) => {
    await app(page)
    await page.addInitScript((theme) => localStorage.setItem('ideahub-theme', theme), theme)
    await page.goto('/')
    await expect(page.locator('.template .ui-icon')).toHaveCount(7)
    await expect
      .poll(() =>
        page
          .locator('.template .ui-icon')
          .evaluateAll((elements) =>
            elements.every((e) => (e as SVGGraphicsElement).getBBox().width > 0),
          ),
      )
      .toBe(true)
    expect(await page.locator('.template .ui-icon use').last().getAttribute('href')).toContain(
      '#file',
    )
    for (const status of await page.locator('.idea .status').all()) {
      await expect(status.locator('.ui-icon')).toHaveCount(1)
      expect(await status.evaluate((e) => getComputedStyle(e, '::before').content)).toBe('none')
    }
    await checkContrast(
      page.locator(
        '.template .emoji,.template b,.template span:not(.arrow),.idea-author,.idea .status,.availability-note,.empty p',
      ),
    )
    await page
      .locator('.template-grid')
      .first()
      .screenshot({ path: testInfo.outputPath(`app-templates-${theme}.png`) })
  })
}

test('branding preview stays readable for pale and dark colors and a white image', async ({
  page,
}) => {
  await app(page, true)
  await page.goto('/company')
  await page.locator('#company-pattern').setInputFiles({
    name: 'white.png',
    mimeType: 'image/png',
    buffer: Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6iVAAAAAASUVORK5CYII=',
      'base64',
    ),
  })
  for (const color of ['#f3f6ec', '#ffff00', '#000000', '#0000ff', '#39775b']) {
    await page.locator('#company-color').fill(color)
    await checkContrast(
      page.locator(
        '.company-banner-content,.company-banner p,.brand-preview .brandmark,.dark-preview b',
      ),
    )
  }
})
