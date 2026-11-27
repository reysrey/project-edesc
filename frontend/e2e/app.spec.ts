import { test, expect } from '@playwright/test'
import type { Page } from '@playwright/test'
import { readFile } from 'node:fs/promises'
import type { SuggestionDetails } from '../src/shared/api/contracts.ts'

const departmentId = '11111111-1111-4111-8111-111111111111'
const suggestionId = '22222222-2222-4222-8222-222222222222'
const templateId = '33333333-3333-4333-8333-333333333333'
const createdId = '44444444-4444-4444-8444-444444444444'
const otherDraftId = '55555555-5555-4555-8555-555555555555'
const version = (i: number) => i.toString(16).padStart(64, '0')
async function server(
  page: Page,
  options: {
    signedIn?: boolean
    manager?: boolean
    moderator?: boolean
    failSubmit?: boolean
    loseCreateResponse?: boolean
    loseSubmitResponse?: boolean
    empty?: boolean
    allowedActions?: string[]
    notifications?: {
      id: string
      suggestionId: string
      title: string
      status: string
      occurredAt: string
    }[]
  } = {},
) {
  let signedIn = options.signedIn ?? true
  const calls: { method: string; path: string; body: unknown; key?: string }[] = []
  const seed: SuggestionDetails = {
    id: suggestionId,
    title: 'Единый чек-лист смены',
    status: 'Submitted',
    created: '2026-11-18T12:00:00Z',
    createdBy: 'colleague-1',
    authorName: 'Автор идеи',
    isMine: false,
    isAnonymous: false,
    targetDepartmentId: departmentId,
    departmentName: 'Производство',
    improvementObject: 'Рабочее место',
    currentFlaws: 'Смены проверяют оборудование по разным спискам.',
    proposedSolution: 'Ввести единый чек-лист.',
    templateId: null,
    templateName: null,
    templateIsActive: null,
    registrationNumber: null,
    commissionNote: null,
    allowedActions: [],
    version: version(1),
    submissionCount: 1,
    history: [],
    supportCount: 0,
    supportedByMe: false,
  }
  const suggestions = [
    ...(options.empty ? [] : [seed]),
    {
      ...seed,
      id: otherDraftId,
      status: 'Draft',
      title: 'Чужой черновик',
    } satisfies SuggestionDetails,
  ]
  const visible = (item: SuggestionDetails) => item.isMine || item.status !== 'Draft'
  const actions = ({ isMine, status }: SuggestionDetails) => {
    if (isMine && ['Draft', 'ReturnedByManager', 'RevisionRequired'].includes(status))
      return ['edit', 'submit']
    const result: string[] = []
    if (!isMine && status !== 'Draft' && status !== 'Rejected' && settings.supportEnabled)
      result.push('support')
    if (options.manager || options.moderator) {
      if (status === 'Submitted') result.push(...(options.allowedActions ?? ['moderate']))
      if (status === 'InCommission') result.push('commission')
    }
    return result
  }
  const detail = (item: SuggestionDetails) => ({ ...item, allowedActions: actions(item) })
  const createdByKey = new Map<string, string>()
  let nextVersion = 2
  let submitAttempts = 0
  let loseNextDetailResponse = false
  let settings = {
    supportEnabled: true,
    anonymousEnabled: false,
    notificationsEnabled: true,
    boardEnabled: true,
    version: '00000000-0000-0000-0000-000000000000',
  }
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    async (route) => {
      const request = route.request()
      const url = new URL(request.url())
      const path = url.pathname
      const method = request.method()
      const body = request.postData() ? request.postDataJSON() : undefined
      const key = request.headers()['idempotency-key']
      calls.push({ method, path, body, key })
      const send = (data: unknown, status = 200) =>
        route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(data) })
      if (path === '/api/Users/me')
        return signedIn
          ? send({
              id: 'user-1',
              email: 'test@company.ru',
              fullName: 'Анна Иванова',
              position: 'Инженер',
              departmentId,
              roles: options.manager ? ['AdminL1'] : options.moderator ? ['AdminL2'] : [],
            })
          : send({}, 401)
      if (path === '/api/Users/login') {
        if (body.password === 'wrong') return send({}, 401)
        signedIn = true
        return route.fulfill({ status: 200, body: '' })
      }
      if (
        path === '/api/Users/register' ||
        path === '/api/Users/forgotPassword' ||
        path === '/api/Users/resetPassword'
      )
        return route.fulfill({ status: 200, body: '' })
      if (path === '/api/Users/logout') {
        signedIn = false
        return route.fulfill({ status: 200, body: '' })
      }
      if (path === '/api/Users/notifications') return send(options.notifications ?? [])
      if (path === '/api/Users/directory')
        return send({
          items: [
            {
              id: 'directory-employee',
              email: 'employee@localhost',
              fullName: 'Сотрудник IdeaHub',
              position: 'Сотрудник',
              departmentId,
              departmentName: 'Производство',
              roles: ['User'],
            },
          ],
          totalCount: 1,
          pageNumber: 1,
          pageSize: 20,
        })
      if (path === '/api/ServiceSettings/') {
        if (method === 'PUT') settings = { ...body, version: templateId }
        return send(settings)
      }
      if (path === '/api/Departments/' && method === 'GET')
        return send([
          { id: departmentId, name: 'Производство', headUserId: null, employeesCount: 12 },
        ])
      if (path === '/api/Departments/' && method === 'POST') return send(departmentId, 201)
      if (path === '/api/Templates/' && method === 'GET')
        return send([
          {
            id: templateId,
            type: 'Template',
            departmentId,
            improvementObject: 'Рабочее место',
            title: 'Улучшение процесса',
            problemHint: 'Опишите потери времени',
            solutionHint: 'Как сократить потери?',
            isActive: true,
          },
          {
            id: '77777777-7777-4777-8777-777777777777',
            type: 'Template',
            departmentId: null,
            improvementObject: 'Безопасность',
            title: 'Безопасность',
            problemHint: 'Где возникает риск?',
            solutionHint: 'Как его снизить?',
            isActive: true,
          },
        ])
      if (path === '/api/Templates/' && method === 'POST') return send(templateId, 201)
      if (path === `/api/Templates/${templateId}/archive`) return route.fulfill({ status: 204 })
      if (path === '/api/Suggestions/' && method === 'GET')
        return send(suggestions.filter(visible).map(detail))
      if (path === '/api/Suggestions/search' && url.searchParams.get('scope') === 'mine') {
        const pageNumber = Number(url.searchParams.get('pageNumber'))
        const pageSize = Number(url.searchParams.get('pageSize'))
        const status = url.searchParams.get('status')
        const term = url.searchParams.get('search') ?? ''
        const items = suggestions.filter(
          (item) =>
            item.isMine &&
            (!status || item.status === status) &&
            [item.title, item.currentFlaws, item.proposedSolution].join(' ').includes(term),
        )
        return send({
          items: items
            .slice((pageNumber - 1) * pageSize, pageNumber * pageSize)
            .map((item) => ({ ...detail(item), shortDescription: item.currentFlaws })),
          totalCount: items.length,
          pageNumber,
          pageSize,
        })
      }
      if (path === '/api/Suggestions/' && method === 'POST') {
        if (key && createdByKey.has(key)) return send(createdByKey.get(key), 201)
        suggestions.push({
          ...seed,
          ...body,
          id: createdId,
          status: 'Draft',
          created: '2026-11-23T12:00:00Z',
          createdBy: 'user-1',
          authorName: 'Анна Иванова',
          isMine: true,
          templateName: body.templateId ? 'Улучшение процесса' : null,
          templateIsActive: body.templateId ? true : null,
          submissionCount: 0,
          version: version(nextVersion++),
        })
        if (key) createdByKey.set(key, createdId)
        if (options.loseCreateResponse && createdByKey.size === 1) return route.abort('failed')
        return send(createdId, 201)
      }
      const idea = suggestions.find((item) => path.startsWith(`/api/Suggestions/${item.id}`))
      if (idea && !visible(idea)) return send({}, 404)
      if (idea && path.endsWith('/support')) {
        idea.supportedByMe = method === 'PUT'
        idea.supportCount = idea.supportedByMe ? 1 : 0
        return route.fulfill({ status: 204 })
      }
      if (idea && path.includes('/commission/')) {
        idea.status = path.endsWith('/approve')
          ? 'ApprovedPlanning'
          : path.endsWith('/reject')
            ? 'Rejected'
            : 'RevisionRequired'
        idea.commissionNote = body.note
        return route.fulfill({ status: 204 })
      }
      if (idea && path === `/api/Suggestions/${idea.id}` && method === 'GET') {
        if (loseNextDetailResponse) {
          loseNextDetailResponse = false
          return route.abort('failed')
        }
        return send(detail(idea))
      }
      if (idea && path === `/api/Suggestions/${idea.id}` && method === 'PUT') {
        if (idea.version !== body.version) return send({ detail: 'Запись уже изменена.' }, 409)
        const updated = version(nextVersion++)
        Object.assign(idea, body, { version: updated })
        return send({ version: updated })
      }
      if (idea && path.endsWith('/submit')) {
        submitAttempts++
        if (options.failSubmit && submitAttempts === 1)
          return send({ detail: 'Временная ошибка отправки' }, 409)
        idea.status = 'Submitted'
        idea.submissionCount++
        idea.version = version(nextVersion++)
        if (options.loseSubmitResponse && submitAttempts === 1) {
          idea.status = 'ReturnedByManager'
          idea.commissionNote = 'Уточните ожидаемый результат.'
          loseNextDetailResponse = true
          return route.abort('failed')
        }
        return route.fulfill({ status: 204 })
      }
      if (idea && (path.endsWith('/approve') || path.endsWith('/return'))) {
        idea.status = path.endsWith('/approve') ? 'InCommission' : 'ReturnedByManager'
        idea.registrationNumber = body.regNumber ?? null
        idea.commissionNote = body.reason ?? null
        idea.version = version(nextVersion++)
        return route.fulfill({ status: 204 })
      }
      return send({ detail: `Unexpected test API: ${method} ${path}` }, 501)
    },
  )
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
  await expect(page.getByRole('heading', { name: /Добрый день/ })).toBeVisible()
  await page.getByRole('button', { name: /^Выйти/ }).click()
  await expect(page).toHaveURL(/login/)
  expect(calls.filter((c) => c.path === '/api/Users/logout')).toHaveLength(1)
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
  expect(calls.some((c) => c.path.endsWith('/register'))).toBe(false)
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
  await dialog.getByRole('button', { name: 'Закрыть', exact: true }).last().click()
  await page.getByRole('button', { name: 'Предложить идею', exact: false }).click()
  await expect(dialog.getByLabel('Название идеи')).toBeDisabled()
  await dialog.getByRole('button', { name: 'Повторить отправку' }).click()
  await expect(dialog).not.toBeVisible()
  expect(calls.filter((c) => c.path === '/api/Suggestions/' && c.method === 'POST')).toHaveLength(1)
  expect(calls.filter((c) => c.path.endsWith('/submit'))).toHaveLength(2)
})

test('a lost create response reuses the same key and payload after reloading the tab', async ({
  page,
}) => {
  const calls = await server(page, { empty: true, loseCreateResponse: true })
  await page.goto('/my-suggestions')
  await page.getByRole('button', { name: 'Предложить идею' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Название идеи').fill('Черновик после обрыва')
  await dialog.getByLabel('Шаблон', { exact: true }).selectOption(templateId)
  await dialog.getByLabel('Какую проблему').fill('Теряем время')
  await dialog.getByLabel('Как предлагаете').fill('Создать чек-лист')
  await dialog.getByRole('button', { name: 'Сохранить черновик' }).click()
  await expect(dialog.getByRole('alert')).toContainText('Не удалось связаться')
  await page.reload()
  await page.getByRole('button', { name: 'Предложить идею' }).click()
  await expect(dialog.getByLabel('Название идеи')).toBeDisabled()
  await dialog.getByRole('button', { name: 'Сохранить черновик' }).click()
  await expect(dialog).not.toBeVisible()
  await expect(page.getByRole('heading', { name: 'Черновик после обрыва' })).toHaveCount(1)
  const createCalls = calls.filter((c) => c.path === '/api/Suggestions/' && c.method === 'POST')
  expect(createCalls).toHaveLength(2)
  expect(createCalls[0].key).toMatch(/^[\da-f-]{36}$/)
  expect(createCalls[1]).toEqual(createCalls[0])
  expect(createCalls[0].body).toMatchObject({ templateId })
  expect(calls.some((c) => c.path.endsWith('/submit'))).toBe(false)
})

test('the direct card opens after login and a foreign draft stays hidden', async ({ page }) => {
  await server(page, { signedIn: false })
  await page.goto(`/suggestions/${suggestionId}`)
  await expect(page).toHaveURL(/login/)
  await page.getByLabel('Рабочая почта').fill('test@company.ru')
  await page.getByLabel('Пароль', { exact: true }).fill('Correct123!')
  await page.getByRole('button', { name: 'Войти в IdeaHub' }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog.getByRole('heading', { name: 'Единый чек-лист смены' })).toBeVisible()
  await expect(dialog.getByText('Ввести единый чек-лист.')).toBeVisible()
  await expect(page).toHaveURL(new RegExp(suggestionId))
  await page.goto(`/suggestions/${otherDraftId}`)
  await expect(dialog.getByText('Предложение недоступно или не найдено')).toBeVisible()
  await expect(dialog.getByText('Чужой черновик')).toHaveCount(0)
  await page.goto('/my-suggestions')
  await expect(page.getByText('Чужой черновик')).toHaveCount(0)
})

test('a version conflict keeps typed edits until the author explicitly resolves it', async ({
  page,
}, testInfo) => {
  const calls = await server(page, { empty: true })
  await page.goto('/my-suggestions')
  const input = {
    title: 'Исходная версия',
    currentFlaws: 'Проблема смены',
    proposedSolution: 'Составить инструкцию',
    improvementObject: 'Оборудование',
    targetDepartmentId: departmentId,
    templateId: null,
  }
  await page.evaluate(async (body) => {
    await fetch('/api/Suggestions/', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  }, input)
  await page.goto(`/suggestions/${createdId}`)
  const dialog = page.getByRole('dialog')
  await dialog.getByRole('button', { name: 'Редактировать' }).click()
  await dialog.getByLabel('Название идеи').fill('Мой текст')
  expect(
    await page.evaluate(
      async ({ id, body }) => {
        const response = await fetch(`/api/Suggestions/${id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(body),
        })
        return response.status
      },
      { id: createdId, body: { ...input, title: 'Версия из другой вкладки', version: version(2) } },
    ),
  ).toBe(200)
  await dialog.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await dialog.getByRole('button', { name: 'Загрузить актуальную карточку' }).click()
  await expect(dialog.getByText('Версия из другой вкладки')).toBeVisible()
  await expect(dialog.getByLabel('Название идеи')).toHaveValue('Мой текст')
  await page.screenshot({ path: testInfo.outputPath('mvp01-conflict.png') })
  await dialog.getByRole('button', { name: 'Продолжить с моим текстом' }).click()
  await expect(dialog.getByLabel('Название идеи')).toHaveValue('Мой текст')
  await dialog.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await expect(dialog.getByRole('heading', { name: 'Мой текст' })).toBeVisible()
  await expect(dialog.getByText('Черновик', { exact: true })).toBeVisible()
  const edits = calls.filter((c) => c.method === 'PUT')
  expect(edits.map((c) => c.body)).toEqual([
    { ...input, title: 'Версия из другой вкладки', version: version(2) },
    { ...input, title: 'Мой текст', version: version(2) },
    { ...input, title: 'Мой текст', version: version(3) },
  ])
  expect(calls.some((c) => c.path.endsWith('/submit'))).toBe(false)
})

test('a lost submit response is confirmed even after return and the next submission uses the same id', async ({
  page,
}, testInfo) => {
  const calls = await server(page, { empty: true, loseSubmitResponse: true })
  await page.goto('/my-suggestions')
  await page.getByRole('button', { name: 'Предложить идею' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Название идеи').fill('Новая инструкция')
  await dialog.getByLabel('Шаблон', { exact: true }).selectOption(templateId)
  await dialog.getByLabel('Какую проблему').fill('Инструкция устарела')
  await dialog.getByLabel('Как предлагаете').fill('Обновить шаги')
  await dialog.getByRole('button', { name: 'Сохранить черновик' }).click()
  await page.getByRole('button', { name: 'Открыть: Новая инструкция' }).click()
  await dialog.getByRole('button', { name: 'Отправить предложение' }).click()
  await expect(dialog.getByText('Подача подтверждена.')).toBeVisible()
  await expect(dialog.getByRole('heading', { name: 'Причина возврата' })).toBeVisible()
  await expect(dialog.getByText('Уточните ожидаемый результат.')).toBeVisible()
  await page.screenshot({ path: testInfo.outputPath('mvp01-return.png') })
  await dialog.getByRole('button', { name: 'Подать повторно' }).click()
  await expect(dialog.getByText('На проверке у модератора')).toBeVisible()
  expect(calls.filter((c) => c.path.endsWith('/submit')).map((c) => c.path)).toEqual([
    `/api/Suggestions/${createdId}/submit`,
    `/api/Suggestions/${createdId}/submit`,
  ])
  expect(calls.filter((c) => c.path === '/api/Suggestions/' && c.method === 'POST')).toHaveLength(1)
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
  expect(calls.find((c) => c.path.endsWith('/approve'))?.body).toEqual({ regNumber: 'ППУ-2026-42' })
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
  await expect(page.getByLabel('Решение по предложению')).toHaveCount(0)
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).not.toBeVisible()
})

test('moderator has no decision form when the server denies moderation for a suggestion', async ({
  page,
}) => {
  await server(page, { moderator: true, allowedActions: [] })
  await page.goto('/incoming')
  await page.getByRole('button', { name: 'Единый чек-лист смены' }).click()
  await expect(page.getByRole('dialog')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Подтвердить решение' })).toHaveCount(0)
})

test('manager creates a department and a template with real command shapes', async ({ page }) => {
  const calls = await server(page, { manager: true })
  await page.goto('/departments')
  await page.getByRole('button', { name: 'Добавить подразделение' }).click()
  await page.getByLabel('Название').fill('Логистика')
  await page.getByLabel('Количество сотрудников').fill('7')
  await page.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  expect(calls.find((c) => c.path === '/api/Departments/' && c.method === 'POST')?.body).toEqual({
    name: 'Логистика',
    employeesCount: 7,
    headUserId: null,
  })
  await page.goto('/templates')
  await page.getByRole('button', { name: 'Создать шаблон' }).click()
  await page.getByLabel('Название').fill('Безопасность')
  await page.getByLabel('Объект улучшения').fill('Проходы')
  await page.getByRole('button', { name: 'Сохранить', exact: true }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  expect(
    calls.find((c) => c.path === '/api/Templates/' && c.method === 'POST')?.body,
  ).toMatchObject({ title: 'Безопасность', type: 'Template', departmentId: null })
})

test('unauthorized API response clears the session and redirects to login', async ({ page }) => {
  await server(page)
  await page.route('**/api/Suggestions/', (route) => route.fulfill({ status: 401, body: '{}' }))
  await page.goto('/suggestions')
  await expect(page).toHaveURL(/login/)
  await expect(page.getByRole('button', { name: 'Войти в IdeaHub' })).toBeVisible()
})

test('invalid server enum shows a contract error', async ({ page }) => {
  await server(page)
  await page.route('**/api/Suggestions/', (route) =>
    route.fulfill({
      contentType: 'application/json',
      body: JSON.stringify([{ id: suggestionId, title: 'Unknown', status: 'MadeUp', created: '' }]),
    }),
  )
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
  for (const path of [
    '/',
    '/suggestions',
    '/incoming',
    '/review',
    '/rework',
    '/processed',
    '/templates',
    '/departments',
    '/effectiveness',
    '/export',
    '/users',
    '/form',
    '/company',
    '/settings',
  ]) {
    await page.goto(path)
    await expect(page.locator('.loading-state')).toHaveCount(0)
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true)
    await page.screenshot({
      path: testInfo.outputPath(`${path.replaceAll('/', '') || 'dashboard'}.png`),
      fullPage: true,
    })
  }
})

test('moderator has four queues and cannot access leader settings', async ({ page }, testInfo) => {
  const calls = await server(page, { moderator: true })
  await page.goto('/')
  await expect(page).toHaveURL(/incoming/)
  await expect(page.getByLabel('Модератор', { exact: true })).toBeVisible()
  await expect(page.locator('.sidebar .nav-btn')).toHaveCount(5)
  await expect(page.getByRole('link', { name: 'Мои предложения' })).toBeVisible()
  await page.getByRole('link', { name: /На доработке/ }).click()
  await expect(
    page.getByRole('heading', { name: 'В этом разделе пока нет предложений' }),
  ).toBeVisible()
  await page.goto('/settings')
  await expect(page).toHaveURL(/incoming/)
  expect(calls.some((c) => c.path.includes('Settings'))).toBe(false)
  await page.screenshot({ path: testInfo.outputPath('moderator.png'), fullPage: true })
})

test('employee layout follows the template and has one support action per card', async ({
  page,
}, testInfo) => {
  const calls = await server(page)
  await page.goto('/')
  await expect(page.locator('.sidebar')).toHaveCount(0)
  await expect(page.getByRole('heading', { name: 'Мои предложения' })).toBeVisible()
  await expect(page.getByRole('button', { name: /^Поддержать/ })).toHaveCount(1)
  await page.getByRole('button', { name: /^Поддержать/ }).click()
  const supported = page.getByRole('button', { name: /^Отменить поддержку/ })
  await expect(supported).toHaveAttribute('aria-pressed', 'true')
  await supported.click()
  await expect(page.getByRole('button', { name: /^Поддержать/ })).toHaveAttribute(
    'aria-pressed',
    'false',
  )
  await expect(
    page.getByRole('button', {
      name: /Оригинально|Просто и эффективно|Легко внедрить|Нравится результат/,
    }),
  ).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Безопасность' })).toBeVisible()
  await page.getByRole('button', { name: 'Безопасность' }).click()
  await expect(page.getByLabel('Объект улучшения')).toHaveValue('Безопасность')
  await expect(page.getByLabel('Шаблон', { exact: true })).toHaveValue(
    '77777777-7777-4777-8777-777777777777',
  )
  await page.getByRole('button', { name: 'Закрыть', exact: true }).last().click()
  expect(calls.filter((c) => c.path.endsWith('/support')).map((c) => c.method)).toEqual([
    'PUT',
    'DELETE',
  ])
  await page.screenshot({ path: testInfo.outputPath('employee.png'), fullPage: true })
})

test('queue filters survive reload and all matching rows are exported with selected columns', async ({
  page,
}) => {
  await server(page, { manager: true })
  const data = Array.from({ length: 23 }, (_, i) => ({
    id: `22222222-2222-4222-8222-${String(i).padStart(12, '0')}`,
    title: `Идея ${i}`,
    status: i === 22 ? 'Implemented' : 'Submitted',
    created: '2026-11-18T12:00:00Z',
  }))
  await page.route('**/api/Suggestions/', (route) =>
    route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) }),
  )
  await page.goto('/incoming?q=Идея&size=10&page=2')
  await expect(page.locator('.suggestion-row')).toHaveCount(10)
  await expect(page.locator('.pagination-summary')).toContainText('11–20 из 22')
  await page.reload()
  await expect(page.locator('.pagination-summary')).toContainText('11–20 из 22')
  await page.getByRole('link', { name: 'Экспорт данных', exact: true }).click()
  await page.getByLabel('ID', { exact: true }).uncheck()
  await page.getByLabel('Дата', { exact: true }).uncheck()
  const downloadPromise = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Скачать CSV' }).click()
  const download = await downloadPromise
  const csv = await readFile((await download.path())!, 'utf8')
  expect(csv.split('\r\n')).toHaveLength(23)
  expect(csv).toContain('"Название";"Статус"')
  expect(csv).not.toContain('Идея 22')
  expect(csv).not.toContain('"ID"')
})

test('unfinished form text is restored in the same tab and never sent implicitly', async ({
  page,
}) => {
  const calls = await server(page)
  await page.goto('/')
  await page.getByRole('button', { name: 'Предложить идею', exact: false }).click()
  await page.getByLabel('Название идеи').fill('Незавершённая идея')
  await page.getByRole('button', { name: 'Закрыть', exact: true }).last().click()
  await page.reload()
  await page.getByRole('button', { name: 'Предложить идею', exact: false }).click()
  await expect(page.getByLabel('Название идеи')).toHaveValue('Незавершённая идея')
  await expect(page.getByLabel('Ожидаемый эффект', { exact: true })).toBeDisabled()
  expect(calls.some((c) => c.path === '/api/Suggestions/' && c.method === 'POST')).toBe(false)
})

test('leader saves service settings and sees employee directory', async ({ page }) => {
  const calls = await server(page, { manager: true })
  await page.goto('/company')
  await page.getByLabel('Название предприятия').fill('Предприятие для предпросмотра')
  await expect(page.locator('.brand-preview')).toContainText('Предприятие для предпросмотра')
  await expect(page.getByRole('button', { name: 'Сохранить оформление' })).toBeDisabled()
  await page.goto('/settings')
  await page.getByRole('checkbox', { name: 'Анонимная подача' }).check()
  await page.getByRole('button', { name: 'Сохранить настройки' }).click()
  await expect(page.getByRole('status')).toHaveText('Настройки сохранены.')
  await page.reload()
  await expect(page.getByRole('checkbox', { name: 'Анонимная подача' })).toBeChecked()
  await page.goto('/users')
  await page.getByRole('searchbox', { name: 'Поиск пользователей' }).fill('Сотрудник')
  await page.getByRole('combobox', { name: 'Роль пользователя' }).selectOption('User')
  await expect(page.getByText('employee@localhost')).toBeVisible()
  expect(calls.filter((c) => c.method !== 'GET').map((c) => c.path)).toEqual([
    '/api/ServiceSettings/',
  ])
})

test('moderator rejects an idea from the commission queue with a reason and version', async ({
  page,
}) => {
  const calls = await server(page, { moderator: true })
  await page.goto('/incoming')
  await page.getByRole('button', { name: 'Единый чек-лист смены' }).click()
  await page.getByLabel('Регистрационный номер').fill('ППУ-42')
  await page.getByRole('button', { name: 'Подтвердить решение' }).click()
  await page.getByRole('link', { name: /На рассмотрении/ }).click()
  await page.getByRole('button', { name: 'Единый чек-лист смены' }).click()
  await page.getByLabel('Решение по предложению').selectOption('reject')
  await page.getByLabel('Причина отклонения').fill('Решение уже внедрено')
  await page.getByRole('button', { name: 'Подтвердить решение' }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  expect(calls.find((c) => c.path.endsWith('/commission/reject'))?.body).toEqual({
    version: version(2),
    note: 'Решение уже внедрено',
  })
})

test('notifications show the three latest and return to the expanded list after closing a card', async ({
  page,
}) => {
  await server(page, {
    notifications: [5, 4, 3, 2, 1].map((day) => ({
      id: `66666666-6666-4666-8666-00000000000${day}`,
      suggestionId,
      title: `Идея ${day}`,
      status: 'Submitted',
      occurredAt: `2026-11-0${day}T09:00:00Z`,
    })),
  })
  await page.goto('/')
  const shortcut = page.getByRole('link', { name: 'Уведомления', exact: true })
  await expect(shortcut.locator('use')).toHaveAttribute('href', '/ideahub-icons.svg#bell')
  await shortcut.click()
  await expect(page).toHaveURL(/#notifications$/)

  const section = page.getByRole('region', { name: 'Уведомления' })
  const items = section.getByRole('link')
  await expect(items).toHaveCount(3)
  await expect(items).toHaveText([/^Идея 5/, /^Идея 4/, /^Идея 3/])
  const expand = section.getByRole('button', { name: 'Показать ещё уведомления' })
  await expect(expand).toHaveAttribute('aria-expanded', 'false')
  await expand.click()
  await expect(items).toHaveCount(5)
  const cardLink = section.getByRole('link', { name: /^Идея 1/ })
  await cardLink.scrollIntoViewIfNeeded()
  const scrollBefore = await page.evaluate(() => window.scrollY)
  await cardLink.click()
  const card = page.getByRole('dialog')
  await expect(card.getByRole('heading', { name: 'Единый чек-лист смены' })).toBeVisible()
  await card.getByRole('button', { name: 'Закрыть' }).click()

  await expect(page).toHaveURL(/#notifications$/)
  await expect(items).toHaveCount(5)
  await expect
    .poll(async () => Math.abs((await page.evaluate(() => window.scrollY)) - scrollBefore))
    .toBeLessThanOrEqual(1)
  const collapse = section.getByRole('button', { name: 'Свернуть уведомления' })
  await expect(collapse).toHaveAttribute('aria-expanded', 'true')
  await collapse.click()
  await expect(items).toHaveCount(3)
})

test('dragging a selection from the title to the backdrop keeps the idea form open', async ({
  page,
}) => {
  await server(page)
  await page.goto('/')
  await page.getByRole('button', { name: /Предложить идею/ }).click()
  const form = page.getByRole('dialog')
  const title = form.getByLabel('Название идеи')
  await title.fill('Предложение об улучшении передачи смены')
  const inputBounds = (await title.boundingBox())!
  const formBounds = (await form.boundingBox())!
  const y = inputBounds.y + inputBounds.height / 2
  const backdropX = formBounds.x / 2
  await page.mouse.move(inputBounds.x + inputBounds.width - 8, y)
  await page.mouse.down()
  await page.mouse.move(backdropX, y, { steps: 10 })
  await page.mouse.up()

  await expect(form).toBeVisible()
  await expect(title).toHaveValue('Предложение об улучшении передачи смены')
  expect(
    await title.evaluate((input: HTMLInputElement) => input.selectionStart !== input.selectionEnd),
  ).toBe(true)
  await page.mouse.click(backdropX, y)
  await expect(form).not.toBeVisible()
})
