import { test, expect } from '@playwright/test'
import type { Page } from '@playwright/test'

const departmentId = '11111111-1111-4111-8111-111111111111'
const suggestionId = '22222222-2222-4222-8222-222222222222'
const templateId = '33333333-3333-4333-8333-333333333333'
async function server(page: Page, options: { signedIn?: boolean; manager?: boolean; failSubmit?: boolean; empty?: boolean } = {}) {
  let signedIn = options.signedIn ?? true
  const calls: { method: string; path: string; body: unknown }[] = []
  const suggestions = options.empty ? [] : [{ id: suggestionId, title: 'Единый чек-лист смены', status: 'Submitted', created: '2026-11-18T12:00:00Z' }]
  let submitAttempts = 0
  await page.route(url => url.pathname.startsWith('/api/'), async route => {
    const request = route.request()
    const path = new URL(request.url()).pathname
    const method = request.method()
    const body = request.postData() ? request.postDataJSON() : undefined
    calls.push({ method, path, body })
    const send = (data: unknown, status = 200) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(data) })
    if (path === '/api/Users/me') return signedIn ? send({ id: 'user-1', email: 'test@company.ru', fullName: 'Анна Иванова', position: 'Инженер', departmentId, roles: options.manager ? ['AdminL1'] : [] }) : send({}, 401)
    if (path === '/api/Users/login') { if (body.password === 'wrong') return send({}, 401); signedIn = true; return route.fulfill({ status: 200, body: '' }) }
    if (path === '/api/Users/register' || path === '/api/Users/forgotPassword' || path === '/api/Users/resetPassword') return route.fulfill({ status: 200, body: '' })
    if (path === '/api/Users/logout') { signedIn = false; return route.fulfill({ status: 200, body: '' }) }
    if (path === '/api/Departments/' && method === 'GET') return send([{ id: departmentId, name: 'Производство', headUserId: null, employeesCount: 12 }])
    if (path === '/api/Departments/' && method === 'POST') return send(departmentId, 201)
    if (path === '/api/Templates/' && method === 'GET') return send([{ id: templateId, type: 'Template', departmentId, improvementObject: 'Рабочее место', title: 'Улучшение процесса', problemHint: 'Опишите потери времени', solutionHint: 'Как сократить потери?', isActive: true }])
    if (path === '/api/Templates/' && method === 'POST') return send(templateId, 201)
    if (path === `/api/Templates/${templateId}/archive`) return route.fulfill({ status: 204 })
    if (path === '/api/Suggestions/' && method === 'GET') return send(suggestions)
    if (path === '/api/Suggestions/' && method === 'POST') { suggestions.push({ id: suggestionId, title: body.title, status: 'Draft', created: '2026-11-23T12:00:00Z' }); return send(suggestionId, 201) }
    if (path === `/api/Suggestions/${suggestionId}/submit`) { submitAttempts++; if (options.failSubmit && submitAttempts === 1) return send({ detail: 'Временная ошибка отправки' }, 409); suggestions[suggestions.length - 1].status = 'Submitted'; return route.fulfill({ status: 204 }) }
    if (path === `/api/Suggestions/${suggestionId}/approve` || path === `/api/Suggestions/${suggestionId}/return`) { suggestions[0].status = path.endsWith('approve') ? 'InCommission' : 'ReturnedByManager'; return route.fulfill({ status: 204 }) }
    return send({ detail: `Unexpected test API: ${method} ${path}` }, 501)
  })
  return calls
}

test('cookie login handles invalid credentials and logout', async ({ page }) => {
  const calls = await server(page, { signedIn: false })
  await page.goto('/suggestions')
  await expect(page).toHaveURL(/login/)
  await page.getByLabel('Рабочая почта').fill('test@company.ru')
  await page.getByLabel('Пароль', { exact: true }).fill('wrong')
  await page.getByRole('button', { name: 'Войти в IdeaHub' }).click()
  await expect(page.getByRole('alert')).toContainText('Неверная почта или пароль')
  await page.getByLabel('Пароль', { exact: true }).fill('Correct123!')
  await page.getByRole('button', { name: 'Войти в IdeaHub' }).click()
  await expect(page.getByRole('heading', { name: /Здравствуйте/ })).toBeVisible()
  await page.getByRole('button', { name: /^Выйти/ }).click()
  await expect(page).toHaveURL(/login/)
  expect(calls.filter(c => c.path === '/api/Users/logout')).toHaveLength(1)
  expect(await page.evaluate(() => Object.keys(localStorage))).not.toContain('accessToken')
})

test('registration checks password confirmation before calling API', async ({ page }) => {
  const calls = await server(page, { signedIn: false })
  await page.goto('/login')
  await page.getByRole('button', { name: 'Создать аккаунт', exact: true }).click()
  await page.getByLabel('Рабочая почта').fill('new@company.ru')
  await page.getByLabel('Пароль', { exact: true }).fill('NewPassword123!')
  await page.getByLabel('Повторите пароль').fill('Different123!')
  await page.getByRole('button', { name: 'Создать аккаунт →' }).click()
  await expect(page.getByRole('alert')).toHaveText('Пароли не совпадают.')
  expect(calls.some(c => c.path.endsWith('/register'))).toBe(false)
  await page.getByLabel('Повторите пароль').fill('NewPassword123!')
  await page.getByRole('button', { name: 'Создать аккаунт →' }).click()
  await expect(page.getByRole('status')).toContainText('Аккаунт создан')
})

test('retrying submit uses the saved draft instead of creating a duplicate', async ({ page }) => {
  const calls = await server(page, { failSubmit: true, empty: true })
  await page.goto('/')
  await page.getByRole('button', { name: 'Предложить идею' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Название идеи').fill('Новый процесс')
  await dialog.getByLabel('Объект улучшения').fill('Смена')
  await dialog.getByLabel('Подразделение').selectOption(departmentId)
  await dialog.getByLabel('Какую проблему').fill('Теряем время')
  await dialog.getByLabel('Как предлагаете').fill('Создать чек-лист')
  await dialog.getByRole('button', { name: 'Отправить предложение' }).click()
  await expect(dialog.getByRole('alert')).toContainText('Временная ошибка отправки')
  await dialog.getByRole('button', { name: 'Повторить отправку' }).click()
  await expect(dialog).not.toBeVisible()
  expect(calls.filter(c => c.path === '/api/Suggestions/' && c.method === 'POST')).toHaveLength(1)
  expect(calls.filter(c => c.path.endsWith('/submit'))).toHaveLength(2)
})

test('filtering and empty results preserve the original list', async ({ page }) => {
  await server(page)
  await page.goto('/suggestions')
  await expect(page.getByRole('heading', { name: 'Единый чек-лист смены' })).toBeVisible()
  await page.getByRole('searchbox').fill('не существует')
  await expect(page.getByText('Ничего не найдено')).toBeVisible()
  await page.getByRole('searchbox').fill('')
  await page.getByLabel('Статус предложения').selectOption('Implemented')
  await expect(page.getByText('Ничего не найдено')).toBeVisible()
  await page.getByLabel('Статус предложения').selectOption('Submitted')
  await expect(page.getByRole('heading', { name: 'Единый чек-лист смены' })).toBeVisible()
})

test('manager passes a registration number when approving', async ({ page }) => {
  const calls = await server(page, { manager: true })
  await page.goto('/suggestions')
  await page.getByRole('button', { name: 'Открыть:' }).click()
  await page.getByLabel('Регистрационный номер').fill('ППУ-2026-42')
  await page.getByRole('button', { name: 'Подтвердить решение' }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  await expect(page.locator('.suggestion-row .status')).toHaveText('На комиссии')
  expect(calls.find(c => c.path.endsWith('/approve'))?.body).toEqual({ regNumber: 'ППУ-2026-42' })
})

test('employee has no management actions or analytics access', async ({ page }) => {
  await server(page)
  await page.goto('/departments')
  await expect(page.getByRole('heading', { name: 'Производство' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Добавить подразделение' })).toHaveCount(0)
  await page.goto('/analytics')
  await expect(page).toHaveURL(/\/$/)
  await page.goto('/suggestions')
  await page.getByRole('button', { name: 'Открыть:' }).click()
  await expect(page.getByLabel('Решение руководителя')).toHaveCount(0)
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).not.toBeVisible()
})

test('manager creates a department and a template with real command shapes', async ({ page }) => {
  const calls = await server(page, { manager: true })
  await page.goto('/departments')
  await page.getByRole('button', { name: 'Добавить подразделение' }).click()
  await page.getByLabel('Название').fill('Логистика')
  await page.getByLabel('Количество сотрудников').fill('7')
  await page.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  expect(calls.find(c => c.path === '/api/Departments/' && c.method === 'POST')?.body).toEqual({ name: 'Логистика', employeesCount: 7, headUserId: null })
  await page.goto('/templates')
  await page.getByRole('button', { name: 'Создать шаблон' }).click()
  await page.getByLabel('Название').fill('Безопасность')
  await page.getByLabel('Объект улучшения').fill('Проходы')
  await page.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  expect(calls.find(c => c.path === '/api/Templates/' && c.method === 'POST')?.body).toMatchObject({ title: 'Безопасность', type: 'Template', departmentId: null })
})

test('unauthorized API response clears the session and redirects to login', async ({ page }) => {
  await server(page)
  await page.route('**/api/Suggestions/', route => route.fulfill({ status: 401, body: '{}' }))
  await page.goto('/suggestions')
  await expect(page).toHaveURL(/login/)
  await expect(page.getByRole('button', { name: 'Войти в IdeaHub' })).toBeVisible()
})

test('invalid server enum shows a contract error', async ({ page }) => {
  await server(page)
  await page.route('**/api/Suggestions/', route => route.fulfill({ contentType: 'application/json', body: JSON.stringify([{ id: suggestionId, title: 'Unknown', status: 'MadeUp', created: '' }]) }))
  await page.goto('/suggestions')
  await expect(page.getByRole('alert')).toContainText('не соответствует согласованному формату')
})

test('theme persists and pages fit the viewport', async ({ page }, testInfo) => {
  await server(page, { manager: true })
  await page.goto('/')
  await page.getByRole('button', { name: 'Включить тёмную тему' }).click()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
  await page.reload()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
  await page.getByRole('button', { name: 'Включить светлую тему' }).click()
  for (const path of ['/', '/suggestions', '/templates', '/departments', '/analytics']) {
    await page.goto(path)
    await expect(page.locator('.loading-state')).toHaveCount(0)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    await page.screenshot({ path: testInfo.outputPath(`${path.replaceAll('/', '') || 'dashboard'}.png`), fullPage: true })
  }
})
