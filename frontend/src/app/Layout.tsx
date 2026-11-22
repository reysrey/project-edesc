import { useMutation, useQueryClient } from '@tanstack/react-query'
import { NavLink, Navigate, Outlet, useNavigate } from 'react-router-dom'
import { api } from '../shared/api/resources'
import { ApiError } from '../shared/api/client'
import { ErrorMessage, Loading } from '../shared/ui/Feedback'
import { sessionKey, useSession } from '../features/auth/session'
import { roleLabels, roleOf } from '../features/auth/roles'
import { queues } from '../features/suggestions/queues'
import { useSuggestions } from '../features/suggestions/model'
import { Brand, ThemeToggle } from './Theme'

function Sidebar({ leader }: { leader: boolean }) {
  const query = useSuggestions()
  return (
    <aside
      className="sidebar"
      aria-label={leader ? 'Панель руководителя' : 'Рабочая очередь модератора'}
    >
      <div className="side-label">{leader ? 'ПАНЕЛЬ УПРАВЛЕНИЯ' : 'РАБОЧАЯ ОЧЕРЕДЬ'}</div>
      {leader && (
        <>
          <NavLink end className="nav-btn" to="/">
            ◫ Обзор
          </NavLink>
          <NavLink className="nav-btn" to="/suggestions">
            ▤ Все предложения
          </NavLink>
        </>
      )}
      {(['incoming', 'review', 'rework', 'processed'] as const).map((key) => (
        <NavLink key={key} className="nav-btn" to={queues[key].path}>
          <span aria-hidden="true">
            {{ incoming: '↘', review: '◷', rework: '↻', processed: '✓' }[key]}
          </span>
          {queues[key].title}
          {query.data && key !== 'processed' && (
            <small className="nav-count">
              {
                query.data.filter((s) =>
                  (queues[key].statuses as readonly string[]).includes(s.status),
                ).length
              }
            </small>
          )}
        </NavLink>
      ))}
      {leader && (
        <>
          <NavLink className="nav-btn" to="/effectiveness">
            ⌁ Эффективность
          </NavLink>
          <NavLink className="nav-btn" to="/users">
            ♙ Пользователи
          </NavLink>
          <NavLink className="nav-btn" to="/form">
            ▧ Настройка формы
          </NavLink>
          <NavLink className="nav-btn" to="/company">
            ✳ Оформление предприятия
          </NavLink>
          <NavLink className="nav-btn" to="/settings">
            ⚙ Настройки сервиса
          </NavLink>
          <NavLink className="nav-btn" to="/export">
            ⇩ Экспорт данных
          </NavLink>
          <div className="side-label">СПРАВОЧНИКИ</div>
          <NavLink className="nav-btn" to="/templates">
            ▧ Шаблоны
          </NavLink>
          <NavLink className="nav-btn" to="/departments">
            ▦ Подразделения
          </NavLink>
        </>
      )}
      <div className="side-foot">
        {leader ? 'Панель руководителя' : 'Панель модератора'}
        <br />
        Обработка предложений
      </div>
    </aside>
  )
}
export function Layout() {
  const session = useSession()
  const client = useQueryClient()
  const navigate = useNavigate()
  const logout = useMutation({
    mutationFn: api.logout,
    onSuccess: async () => {
      await client.cancelQueries()
      client.setQueryData(sessionKey, null)
      client.removeQueries({ predicate: (q) => q.queryKey[0] !== sessionKey[0] })
      navigate('/login', { replace: true })
    },
  })
  if (session.isPending) return <Loading />
  if (
    (session.error instanceof ApiError && session.error.status === 401) ||
    (!session.data && !session.error)
  )
    return <Navigate to="/login" replace />
  if (!session.data)
    return (
      <main className="employee-main">
        <ErrorMessage error={session.error} />
        <button className="btn btn-primary" onClick={() => void session.refetch()}>
          Повторить
        </button>
      </main>
    )
  const profile = session.data
  const role = roleOf(profile)
  const name =
    profile.fullName === 'Главный Администратор'
      ? 'Руководитель'
      : profile.fullName || profile.email
  return (
    <div className="shell">
      <a className="skip-link" href="#main">
        Перейти к содержимому
      </a>
      <header className="topbar">
        <NavLink to="/" className="brand-home" aria-label="IdeaHub — главная">
          <Brand />
        </NavLink>
        <span className="company-label">· Пространство улучшений</span>
        <div className="user-chip">
          <div className="user-meta">
            <b>{name}</b>
            <span>{roleLabels[role]}</span>
          </div>
          <div className="avatar" aria-label={roleLabels[role]}>
            {name
              .split(/\s+/)
              .map((n) => n[0])
              .slice(0, 2)
              .join('')
              .toUpperCase()}
          </div>
          <div className="topbar-actions">
            <ThemeToggle />
            <button
              className="theme-toggle"
              type="button"
              disabled
              aria-label="Уведомления пока недоступны"
              title="Уведомления пока недоступны"
            >
              ♧
            </button>
          </div>
          <button className="logout" disabled={logout.isPending} onClick={() => logout.mutate()}>
            {logout.isPending ? 'Выходим…' : 'Выйти'}
          </button>
        </div>
      </header>
      {role === 'employee' ? (
        <main id="main" className="employee-main" tabIndex={-1}>
          <ErrorMessage error={logout.error} />
          <Outlet context={profile} />
        </main>
      ) : (
        <div className="admin-layout">
          <Sidebar leader={role === 'leader'} />
          <main id="main" className="admin-main" tabIndex={-1}>
            <ErrorMessage error={logout.error} />
            <Outlet context={profile} />
          </main>
        </div>
      )}
    </div>
  )
}
