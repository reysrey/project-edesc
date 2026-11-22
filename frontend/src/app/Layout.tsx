import { useMutation, useQueryClient } from '@tanstack/react-query'
import { NavLink, Navigate, Outlet, useNavigate } from 'react-router-dom'
import { api } from '../shared/api/resources'
import { ApiError } from '../shared/api/client'
import { ErrorMessage, Loading } from '../shared/ui/Feedback'
import { isManager, sessionKey, useSession } from '../features/auth/session'
import { Brand, ThemeToggle } from './Theme'

export function Layout() {
  const session = useSession()
  const client = useQueryClient()
  const navigate = useNavigate()
  const logout = useMutation({ mutationFn: api.logout, onSuccess: async () => { await client.cancelQueries(); client.setQueryData(sessionKey, null); client.removeQueries({ predicate: query => query.queryKey[0] !== sessionKey[0] }); navigate('/login', { replace: true }) } })
  if (session.isPending) return <Loading />
  if (session.error instanceof ApiError && session.error.status === 401) return <Navigate to="/login" replace />
  if (!session.data && !session.error) return <Navigate to="/login" replace />
  if (!session.data) return <main className="employee-main"><ErrorMessage error={session.error} /><button className="btn btn-primary" onClick={() => void session.refetch()}>Повторить</button></main>
  const profile = session.data
  const manager = isManager(profile)
  const name = profile.fullName || profile.email
  return <div className="shell"><a className="skip-link" href="#main">Перейти к содержимому</a><header className="topbar"><Brand /><span className="company-label">Пространство улучшений</span><div className="user-chip"><div className="user-meta"><b>{name}</b><span>{manager ? 'Администратор' : 'Сотрудник'}{profile.position ? ` · ${profile.position}` : ''}</span></div><div className="avatar" aria-hidden="true">{name.slice(0, 2).toUpperCase()}</div><ThemeToggle /><button className="logout" disabled={logout.isPending} onClick={() => logout.mutate()}>{logout.isPending ? 'Выходим…' : 'Выйти'}</button></div></header>
    <div className="admin-layout"><aside className="sidebar"><div className="side-label">РАБОЧЕЕ ПРОСТРАНСТВО</div><NavLink end className="nav-btn" to="/">⌂ Обзор</NavLink><NavLink className="nav-btn" to="/suggestions">▤ Предложения</NavLink><NavLink className="nav-btn" to="/templates">▧ Шаблоны</NavLink><NavLink className="nav-btn" to="/departments">▦ Подразделения</NavLink>{manager && <><div className="side-label">УПРАВЛЕНИЕ</div><NavLink className="nav-btn" to="/analytics">◷ Аналитика и экспорт</NavLink></>}<div className="side-foot">Каждая идея — шаг<br />к большим переменам.<br /><span>IdeaHub</span></div></aside><main id="main" className="admin-main"><ErrorMessage error={logout.error} /><Outlet context={profile} /></main></div>
  </div>
}
