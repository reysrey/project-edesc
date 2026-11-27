import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import { ApiError } from '../../shared/api/client'
import type { Profile, ServiceSettings } from '../../shared/api/contracts'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { Unavailable } from '../../shared/ui/Unavailable'
import { roleLabels, roleOf } from '../auth/roles'
import { Brand } from '../../app/Theme'
import { onColor, readableColor } from '../../shared/lib/colors'
import { serviceSettingsKey, suggestionsKey, useServiceSettings } from '../suggestions/model'

export function UsersPage() {
  const profile = useOutletContext<Profile>()
  const [search, setSearch] = useState('')
  const [department, setDepartment] = useState('')
  const [role, setRole] = useState('')
  const [page, setPage] = useState(1)
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: ({ signal }) => api.departments(signal),
  })
  const params = new URLSearchParams({ pageNumber: String(page) })
  if (search.trim()) params.set('search', search.trim())
  if (department) params.set('departmentId', department)
  if (role) params.set('role', role)
  const directory = useQuery({
    queryKey: ['employees', profile.id, params.toString()],
    queryFn: ({ signal }) => api.employees(params, signal),
  })
  const pages = Math.max(
    1,
    Math.ceil((directory.data?.totalCount ?? 0) / (directory.data?.pageSize ?? 20)),
  )
  return (
    <>
      <PageHead title="Пользователи" subtitle="Учётные записи, роли и подразделения." />
      <div className="toolbar">
        <input
          type="search"
          placeholder="Поиск по имени, почте или должности…"
          aria-label="Поиск пользователей"
          value={search}
          maxLength={200}
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(1)
          }}
        />
        <select
          aria-label="Подразделение пользователя"
          value={department}
          onChange={(event) => {
            setDepartment(event.target.value)
            setPage(1)
          }}
        >
          <option value="">Все подразделения</option>
          {departments.data?.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
        <select
          aria-label="Роль пользователя"
          value={role}
          onChange={(event) => {
            setRole(event.target.value)
            setPage(1)
          }}
        >
          <option value="">Все роли</option>
          <option value="User">Сотрудник</option>
          <option value="AdminL2">Модератор</option>
          <option value="AdminL1">Руководитель</option>
        </select>
      </div>
      <ErrorMessage error={departments.error} />
      {directory.isPending ? (
        <Loading />
      ) : directory.error ? (
        <>
          <ErrorMessage error={directory.error} />
          <button className="btn btn-line" onClick={() => void directory.refetch()}>
            Обновить
          </button>
        </>
      ) : directory.data.items.length ? (
        <div className="my-list">
          {directory.data.items.map((user) => (
            <article className="my-item user-row" key={user.id}>
              <div>
                <b>{user.fullName || user.email}</b>
                <small>{user.email}</small>
                {user.position && <small>{user.position}</small>}
              </div>
              <span>{user.departmentName || 'Подразделение не указано'}</span>
              <span className="status st-ok">{roleLabels[roleOf(user)]}</span>
            </article>
          ))}
        </div>
      ) : (
        <Empty title="Пользователи не найдены" />
      )}
      {(page > 1 || pages > 1) && (
        <nav className="pagination" aria-label="Страницы пользователей">
          <span>
            {page} / {pages}
          </span>
          <div>
            <button className="btn btn-line" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              Назад
            </button>
            <button
              className="btn btn-line"
              disabled={page >= pages}
              onClick={() => setPage(page + 1)}
            >
              Далее
            </button>
          </div>
        </nav>
      )}
    </>
  )
}
const formFields = [
  ['Название идеи', true],
  ['Направление / объект улучшения', true],
  ['Подразделение', true],
  ['Проблема', true],
  ['Решение', true],
  ['Ожидаемый эффект', false],
  ['Фото «До»', false],
  ['Фото «После»', false],
] as const
export function FormSettingsPage() {
  return (
    <>
      <PageHead title="Настройка формы" subtitle="Поля и подсказки формы предложения." />
      <Unavailable>
        Форма работает с фиксированными полями. Изменение её состава пока недоступно.
      </Unavailable>
      <div className="two-col">
        <div className="panel">
          <h2>Поля формы</h2>
          <p>Отмечены поля, которые сейчас сохраняются при подаче идеи.</p>
          <fieldset disabled>
            {formFields.map(([label, available]) => (
              <label className="setting-row" key={label}>
                <span>
                  <b>{label}</b>
                  <small>
                    {available ? 'Сохраняется в предложении' : 'Сохранение пока недоступно'}
                  </small>
                </span>
                <input type="checkbox" checked={available} readOnly />
              </label>
            ))}
            <div className="field settings-hint">
              <label htmlFor="form-hint">Подсказка под заголовком</label>
              <textarea id="form-hint" value="Расскажите, что можно сделать лучше." readOnly />
            </div>
            <button className="btn btn-primary" disabled>
              Сохранить настройки
            </button>
          </fieldset>
        </div>
        <div className="panel">
          <h2>Предпросмотр формы</h2>
          <p>Текущий состав полей для сотрудника.</p>
          <div className="preview-form-note">Расскажите, что можно сделать лучше.</div>
          {formFields
            .filter(([, active]) => active)
            .map(([label]) => (
              <div className="preview-field" key={label}>
                <label>{label}</label>
                <div>
                  {label === 'Подразделение' ? 'Выберите подразделение' : 'Поле для заполнения'}
                </div>
              </div>
            ))}
        </div>
      </div>
    </>
  )
}
export function ServiceSettingsPage() {
  const settings = useServiceSettings()
  return (
    <>
      <PageHead
        title="Настройки сервиса"
        subtitle="Возможности IdeaHub для сотрудников предприятия."
      />
      {settings.isPending ? (
        <Loading />
      ) : settings.data ? (
        <ServiceSettingsForm
          initial={settings.data}
          reload={async () => (await settings.refetch({ throwOnError: true })).data!}
        />
      ) : (
        <>
          <ErrorMessage error={settings.error} />
          <button className="btn btn-line" onClick={() => void settings.refetch()}>
            Повторить загрузку
          </button>
        </>
      )}
    </>
  )
}

const settingsFields = [
  [
    'supportEnabled',
    'Поддержка предложений',
    'Сотрудники могут поддерживать идеи коллег и отзывать свою поддержку.',
  ],
  [
    'anonymousEnabled',
    'Анонимная подача',
    'Автор может скрыть своё имя при подготовке предложения.',
  ],
  [
    'notificationsEnabled',
    'Уведомления о статусах',
    'Уведомляет авторов о новых решениях по их предложениям.',
  ],
  [
    'boardEnabled',
    'Публичная доска идей',
    'Сотрудники могут читать предложения коллег. Доступ к своим идеям сохраняется.',
  ],
] as const

function ServiceSettingsForm({
  initial,
  reload,
}: {
  initial: ServiceSettings
  reload: () => Promise<ServiceSettings>
}) {
  const client = useQueryClient()
  const [form, setForm] = useState(initial)
  const save = useMutation({
    mutationFn: () => api.updateServiceSettings(form),
    onSuccess: async (settings) => {
      setForm(settings)
      client.setQueryData(serviceSettingsKey, settings)
      await client.invalidateQueries({ queryKey: suggestionsKey })
    },
  })
  const refresh = useMutation({
    mutationFn: reload,
    onSuccess: (settings) => {
      setForm(settings)
      save.reset()
    },
  })
  const busy = save.isPending || refresh.isPending
  const conflict = save.error instanceof ApiError && save.error.status === 409
  return (
    <div className="panel">
      <form
        onSubmit={(event) => {
          event.preventDefault()
          if (!busy && !conflict) save.mutate()
        }}
      >
        <fieldset disabled={busy}>
          {settingsFields.map(([field, title, description]) => (
            <label className="setting-row" key={title}>
              <span>
                <b>{title}</b>
                <small>{description}</small>
              </span>
              <input
                type="checkbox"
                aria-label={title}
                checked={form[field]}
                onChange={(event) => setForm({ ...form, [field]: event.target.checked })}
              />
            </label>
          ))}
          <ErrorMessage error={save.error ?? refresh.error} />
          {save.isSuccess && <p role="status">Настройки сохранены.</p>}
          {conflict && (
            <button type="button" className="btn btn-line" onClick={() => refresh.mutate()}>
              Загрузить актуальные настройки
            </button>
          )}
          <button className="btn btn-primary settings-hint" disabled={conflict}>
            {save.isPending ? 'Сохраняем…' : 'Сохранить настройки'}
          </button>
        </fieldset>
      </form>
    </div>
  )
}
export function CompanyPage() {
  const [name, setName] = useState('Моё предприятие')
  const [color, setColor] = useState('#39775b')
  const [font, setFont] = useState('Manrope')
  const [logo, setLogo] = useState('')
  const [pattern, setPattern] = useState('')
  const [error, setError] = useState('')
  const foreground = onColor(color)
  useEffect(
    () => () => {
      if (logo) URL.revokeObjectURL(logo)
    },
    [logo],
  )
  useEffect(
    () => () => {
      if (pattern) URL.revokeObjectURL(pattern)
    },
    [pattern],
  )
  function preview(file: File | undefined, set: (value: string) => void) {
    if (!file) return
    if (
      !['image/png', 'image/jpeg', 'image/webp'].includes(file.type) ||
      file.size > 2 * 1024 * 1024
    ) {
      setError('Выберите PNG, JPEG или WebP размером до 2 МБ.')
      return
    }
    setError('')
    set(URL.createObjectURL(file))
  }
  return (
    <>
      <PageHead title="Оформление предприятия" subtitle="Фирменные цвета, название и логотип." />
      <Unavailable>
        Это предпросмотр. Оформление не сохраняется и не меняется у других сотрудников.
      </Unavailable>
      <div className="two-col">
        <div className="panel">
          <h2>Фирменный стиль</h2>
          <div className="field">
            <label htmlFor="company-name">Название предприятия</label>
            <input
              id="company-name"
              value={name}
              maxLength={100}
              onChange={(e) => setName(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="company-color">Фирменный цвет</label>
            <input
              id="company-color"
              type="color"
              value={color}
              onChange={(e) => setColor(e.target.value)}
            />
          </div>
          <div className="field">
            <label htmlFor="company-font">Шрифт интерфейса</label>
            <select id="company-font" value={font} onChange={(e) => setFont(e.target.value)}>
              <option value="Manrope">Manrope</option>
              <option value="system-ui">Системный</option>
            </select>
          </div>
          <div className="field">
            <label htmlFor="company-logo">Логотип предприятия</label>
            <label className="file-label">
              ＋ Загрузить логотип
              <input
                id="company-logo"
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(e) => preview(e.target.files?.[0], setLogo)}
              />
            </label>
            <small className="file-hint">
              PNG, JPEG или WebP, до 2 МБ. Только для предпросмотра.
            </small>
          </div>
          <div className="logo-preview-grid">
            {['light', 'dark'].map((surface) => (
              <div key={surface}>
                <small>{surface === 'light' ? 'На светлом фоне' : 'На тёмном фоне'}</small>
                <div className={`logo-surface ${surface}`}>
                  {logo ? (
                    <img src={logo} alt="Предпросмотр логотипа" />
                  ) : (
                    <span aria-hidden="true">✳</span>
                  )}
                </div>
              </div>
            ))}
          </div>
          {logo && (
            <button className="link-button" onClick={() => setLogo('')}>
              Убрать логотип из предпросмотра
            </button>
          )}
          <div className="field settings-hint">
            <label htmlFor="company-pattern">Паттерн welcome-баннера</label>
            <label className="file-label">
              ＋ Загрузить изображение
              <input
                id="company-pattern"
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(e) => preview(e.target.files?.[0], setPattern)}
              />
            </label>
          </div>
          {pattern && (
            <button className="link-button" onClick={() => setPattern('')}>
              Убрать паттерн из предпросмотра
            </button>
          )}
          {error && (
            <p className="error-message" role="alert">
              {error}
            </p>
          )}
          <button className="btn btn-primary settings-hint" disabled>
            Сохранить оформление
          </button>
        </div>
        <div className="panel">
          <h2>Живое превью</h2>
          <p>Проверьте сочетание цвета, логотипа и типографики.</p>
          <div className="brand-preview" style={{ fontFamily: font }}>
            <div className="brand">
              <div className="brandmark" style={{ background: color, color: foreground }}>
                {logo ? <img src={logo} alt="" /> : '✳'}
              </div>
              <span>{name.trim() || 'Моё предприятие'}</span>
            </div>
            <div
              className="company-banner"
              style={{
                backgroundColor: color,
                color: foreground,
                backgroundImage: pattern ? `url("${pattern}")` : undefined,
              }}
            >
              <div
                className="company-banner-content"
                style={{ background: color, color: foreground }}
              >
                <div className="brand-preview-title">Идеи меняют работу.</div>
                <p>Внутреннее пространство предложений и улучшений.</p>
                <button className="btn btn-primary" disabled>
                  Предложить идею
                </button>
              </div>
            </div>
          </div>
          <div className="dark-preview">
            <Brand />
            <p>
              Тёмная поверхность ·{' '}
              <b style={{ color: readableColor(color, '#17231c') }}>фирменный акцент</b>
            </p>
          </div>
        </div>
      </div>
    </>
  )
}
