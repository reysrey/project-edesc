import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useLocation, useOutletContext } from 'react-router-dom'
import type { Profile, Template } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { Empty, ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { formatDate } from '../../shared/lib/format'
import { SuggestionForm } from './SuggestionForm'
import { SuggestionsPage } from './SuggestionsPage'
import { Icon } from '../../shared/ui/Icon'
import { templateIcon } from '../catalog/templateIcon'
import { statusConfig, suggestionsKey, useServiceSettings } from './model'
export function Dashboard() {
  const profile = useOutletContext<Profile>()
  const location = useLocation()
  const templates = useQuery({
    queryKey: ['templates'],
    queryFn: ({ signal }) => api.templates(signal),
  })
  const settings = useServiceSettings()
  const notifications = useQuery({
    queryKey: [...suggestionsKey, 'notifications', profile.id],
    queryFn: ({ signal }) => api.notifications(signal),
    refetchOnWindowFocus: 'always',
  })
  const [form, setForm] = useState<{ template?: Template }>()
  const [notificationsExpanded, setNotificationsExpanded] = useState(false)
  const notificationItems = notifications.data ?? []
  const visibleNotifications = notificationsExpanded
    ? notificationItems
    : notificationItems.slice(0, 3)
  return (
    <>
      <section className="welcome">
        <div>
          <div className="eyebrow">ЛИЧНЫЙ КАБИНЕТ · СОТРУДНИК</div>
          <h1>
            {profile.fullName
              ? `Добрый день, ${profile.fullName.split(' ')[0]}!`
              : 'Хороший день для новой идеи'}
          </h1>
          <p>Ваши наблюдения могут сделать работу предприятия лучше.</p>
        </div>
        <div className="welcome-actions">
          <Link className="btn btn-light" to="/my-suggestions">
            Мои предложения
          </Link>
          <button className="btn btn-primary" onClick={() => setForm({})}>
            ＋ Предложить идею
          </button>
        </div>
      </section>
      <section className="section" id="notifications" aria-labelledby="notifications-title">
        <div className="section-head">
          <div>
            <h2 id="notifications-title">Уведомления</h2>
            <p>Обратная связь по вашим предложениям.</p>
          </div>
        </div>
        {notifications.isPending ? (
          <Loading />
        ) : notifications.error ? (
          <ErrorMessage error={notifications.error} />
        ) : notificationItems.length ? (
          <>
            <div className="my-list" id="notification-items">
              {visibleNotifications.map((notification) => (
                <Link
                  className="my-item"
                  to={`/suggestions/${notification.suggestionId}`}
                  state={{ suggestionFrom: location }}
                  key={notification.id}
                >
                  <div>
                    <b>{notification.title}</b>
                    <small>{formatDate(notification.occurredAt)}</small>
                  </div>
                  <span>{statusConfig[notification.status].label}</span>
                </Link>
              ))}
            </div>
            {notificationItems.length > 3 && (
              <button
                type="button"
                className="btn btn-line notifications-toggle"
                aria-controls="notification-items"
                aria-expanded={notificationsExpanded}
                onClick={() => setNotificationsExpanded(!notificationsExpanded)}
              >
                {notificationsExpanded ? 'Свернуть уведомления' : 'Показать ещё уведомления'}
              </button>
            )}
          </>
        ) : (
          <p>
            {settings.data?.notificationsEnabled === false
              ? 'Уведомления отключены.'
              : 'Уведомлений пока нет.'}
          </p>
        )}
      </section>
      <section className="section">
        <div className="section-head">
          <div>
            <h2>Картотека шаблонов и узких мест</h2>
            <p>Выберите шаблон, чтобы заполнить объект улучшения и получить подсказки.</p>
          </div>
          <Link className="text-link" to="/templates">
            Все шаблоны
          </Link>
        </div>
        {templates.isPending ? (
          <Loading />
        ) : templates.error ? (
          <>
            <ErrorMessage error={templates.error} />
            <button className="btn btn-line" onClick={() => void templates.refetch()}>
              Повторить загрузку шаблонов
            </button>
          </>
        ) : templates.data?.length ? (
          <div className="template-grid">
            {templates.data.map((t) => (
              <button className="template" key={t.id} onClick={() => setForm({ template: t })}>
                <span className="emoji" aria-hidden="true">
                  <Icon name={templateIcon(t)} />
                </span>
                <b>{t.title}</b>
                <span>{t.improvementObject}</span>
                <span className="arrow">↗</span>
              </button>
            ))}
          </div>
        ) : (
          <Empty title="Активных шаблонов пока нет" />
        )}
      </section>
      <section className="section" id="board" aria-labelledby="suggestion-board-title">
        <SuggestionsPage embedded />
      </section>
      <section className="section" id="my-suggestions" aria-labelledby="my-suggestions-title">
        <SuggestionsPage embedded mine />
      </section>
      {form && <SuggestionForm template={form.template} onClose={() => setForm(undefined)} />}
    </>
  )
}
