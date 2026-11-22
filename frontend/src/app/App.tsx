import { BrowserRouter, Route, Routes, Link, Navigate, useOutletContext } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { Profile } from '../shared/api/contracts'
import { Layout } from './Layout'
import { AuthPage } from '../features/auth/AuthPage'
import { roleOf, isLeader, canModerate } from '../features/auth/roles'
import { Dashboard } from '../features/suggestions/Dashboard'
import { SuggestionsPage } from '../features/suggestions/SuggestionsPage'
import { TemplatesPage, DepartmentsPage } from '../features/catalog/CatalogPages'
import { AnalyticsPage } from '../features/analytics/AnalyticsPage'
import { OverviewPage, EffectivenessPage } from '../features/management/OverviewPage'
import {
  UsersPage,
  FormSettingsPage,
  CompanyPage,
  ServiceSettingsPage,
} from '../features/management/SettingsPages'
import { Empty } from '../shared/ui/Feedback'

function HomePage() {
  const profile = useOutletContext<Profile>()
  const role = roleOf(profile)
  return role === 'leader' ? (
    <OverviewPage />
  ) : role === 'moderator' ? (
    <Navigate to="/incoming" replace />
  ) : (
    <Dashboard />
  )
}
function LeaderOnly({ children }: { children: ReactNode }) {
  const profile = useOutletContext<Profile>()
  return isLeader(profile) ? (
    children
  ) : (
    <Navigate to={canModerate(profile) ? '/incoming' : '/'} replace />
  )
}
function StaffOnly({ children }: { children: ReactNode }) {
  return canModerate(useOutletContext<Profile>()) ? (
    children
  ) : (
    <Navigate to="/suggestions" replace />
  )
}
export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<AuthPage />} />
        <Route element={<Layout />}>
          <Route index element={<HomePage />} />
          <Route path="suggestions" element={<SuggestionsPage />} />
          {(['incoming', 'review', 'rework', 'processed'] as const).map((queue) => (
            <Route
              key={queue}
              path={queue}
              element={
                <StaffOnly>
                  <SuggestionsPage queue={queue} />
                </StaffOnly>
              }
            />
          ))}
          <Route path="templates" element={<TemplatesPage />} />
          <Route path="departments" element={<DepartmentsPage />} />
          <Route
            path="effectiveness"
            element={
              <LeaderOnly>
                <EffectivenessPage />
              </LeaderOnly>
            }
          />
          <Route
            path="export"
            element={
              <LeaderOnly>
                <AnalyticsPage />
              </LeaderOnly>
            }
          />
          <Route
            path="analytics"
            element={
              <LeaderOnly>
                <AnalyticsPage />
              </LeaderOnly>
            }
          />
          <Route
            path="users"
            element={
              <LeaderOnly>
                <UsersPage />
              </LeaderOnly>
            }
          />
          <Route
            path="form"
            element={
              <LeaderOnly>
                <FormSettingsPage />
              </LeaderOnly>
            }
          />
          <Route
            path="company"
            element={
              <LeaderOnly>
                <CompanyPage />
              </LeaderOnly>
            }
          />
          <Route
            path="settings"
            element={
              <LeaderOnly>
                <ServiceSettingsPage />
              </LeaderOnly>
            }
          />
          <Route
            path="*"
            element={
              <Empty title="Страница не найдена">
                <Link className="btn btn-primary" to="/">
                  На главную
                </Link>
              </Empty>
            }
          />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
