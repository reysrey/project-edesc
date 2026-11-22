import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import type { Profile } from '../../shared/api/contracts'
import { PageHead } from '../../shared/ui/Feedback'
import { Unavailable } from '../../shared/ui/Unavailable'
import { roleLabels, roleOf } from '../auth/roles'
import { Brand } from '../../app/Theme'
import { onColor, readableColor } from '../../shared/lib/colors'

export function UsersPage() {
  const profile = useOutletContext<Profile>()
  return (
    <>
      <PageHead title="Пользователи" subtitle="Управление доступом и ролями участников." />
      <Unavailable>
        Список пользователей, назначение ролей и блокировка пока недоступны.
      </Unavailable>
      <div className="toolbar">
        <input
          type="search"
          placeholder="Поиск по имени, почте или должности…"
          aria-label="Поиск пользователей"
          disabled
        />
      </div>
      <div className="panel">
        <h2>Ваша учётная запись</h2>
        <div className="my-item user-row">
          <div>
            <b>
              {profile.fullName === 'Главный Администратор'
                ? 'Руководитель'
                : profile.fullName || profile.email}
            </b>
            <small>{profile.email}</small>
          </div>
          <span className="status st-ok">{roleLabels[roleOf(profile)]}</span>
          <button className="btn btn-line" disabled>
            Изменить роль
          </button>
          <button className="btn btn-danger" disabled>
            Заблокировать
          </button>
        </div>
      </div>
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
  ['Анонимная подача', false],
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
  const settings = [
    ['Поддержка предложений', 'Одна кнопка «Поддержать» для полезных инициатив.'],
    ['Анонимная подача', 'Подача идеи без отображения имени автора.'],
    ['Уведомления о статусах', 'Обратная связь при изменении статуса предложения.'],
    ['Публичная доска идей', 'Доступность предложений для других сотрудников.'],
  ]
  return (
    <>
      <PageHead
        title="Настройки сервиса"
        subtitle="Возможности IdeaHub для сотрудников предприятия."
      />
      <Unavailable>
        Общие настройки пока недоступны. Переключатели не показывают действующие значения.
      </Unavailable>
      <div className="panel">
        <fieldset disabled>
          {settings.map(([title, description]) => (
            <label className="setting-row" key={title}>
              <span>
                <b>{title}</b>
                <small>{description}</small>
              </span>
              <input type="checkbox" aria-label={title} />
            </label>
          ))}
          <button className="btn btn-primary settings-hint" disabled>
            Сохранить настройки
          </button>
        </fieldset>
      </div>
    </>
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
