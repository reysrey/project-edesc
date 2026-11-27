import { z } from 'zod'

export class ApiError extends Error {
  readonly status: number
  readonly fields: Record<string, string[]>
  constructor(status: number, message: string, fields: Record<string, string[]> = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fields = fields
  }
}
const problemSchema = z.object({
  detail: z.string().optional(),
  title: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
})
const tabHeader = 'X-IdeaHub-Tab'
const tabStorageKey = 'ideahub-tab-session'
let tabId: string | undefined

function currentTabId() {
  if (!tabId) {
    try {
      tabId = sessionStorage.getItem(tabStorageKey) ?? undefined
    } catch {
      // Keep the tab isolated even when session storage is disabled.
    }
    tabId ??= crypto.randomUUID()
  }
  return tabId
}

let onUnauthorized: (() => void) | undefined
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

export async function request<T>(
  path: string,
  schema: z.ZodType<T>,
  options: RequestInit = {},
): Promise<T> {
  const requestTabId = currentTabId()
  const headers = new Headers(options.headers)
  if (options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (!headers.has(tabHeader)) headers.set(tabHeader, requestTabId)

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      ...options,
      credentials: 'include',
      headers,
    })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(
      0,
      'Не удалось связаться с сервером. Проверьте подключение и повторите попытку.',
    )
  }
  if (!response.ok) {
    const problem = problemSchema.safeParse(await response.json().catch(() => null))
    const fields = problem.success ? (problem.data.errors ?? {}) : {}
    const message =
      response.status === 401
        ? 'Сессия завершена. Войдите снова.'
        : response.status === 403
          ? 'Недостаточно прав для этого действия.'
          : Object.values(fields).flat().join(' ') ||
            (problem.success ? problem.data.detail : '') ||
            (response.status >= 500
              ? 'Ошибка сервера. Попробуйте позже.'
              : 'Не удалось выполнить запрос.')
    if (
      response.status === 401 &&
      requestTabId === tabId &&
      !path.startsWith('/Users/login') &&
      path !== '/Users/me'
    )
      onUnauthorized?.()
    throw new ApiError(response.status, message, fields)
  }
  let data: unknown
  try {
    const body = response.status === 204 ? '' : await response.text()
    data = body ? (JSON.parse(body) as unknown) : undefined
  } catch {
    throw new ApiError(
      0,
      'Ответ сервера не соответствует согласованному формату. Сообщите разработчикам.',
    )
  }
  const parsed = schema.safeParse(data)
  if (!parsed.success)
    throw new ApiError(
      0,
      'Ответ сервера не соответствует согласованному формату. Сообщите разработчикам.',
    )
  return parsed.data
}
export const empty = z.unknown()
export const json = (method: string, body?: unknown): RequestInit => ({
  method,
  ...(body === undefined ? {} : { body: JSON.stringify(body) }),
})

export async function login(email: string, password: string) {
  const newTabId = crypto.randomUUID()
  await request('/Users/login?useSessionCookies=true', empty, {
    ...json('POST', { email, password }),
    headers: { [tabHeader]: newTabId },
  })
  tabId = newTabId
  try {
    sessionStorage.setItem(tabStorageKey, tabId)
  } catch {
    // The session remains available in this document until it is reloaded.
  }
}
