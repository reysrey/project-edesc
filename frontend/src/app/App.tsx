import { BrowserRouter, Route, Routes, Link } from 'react-router-dom'
import { Layout } from './Layout'
import { AuthPage } from '../features/auth/AuthPage'
import { Dashboard } from '../features/suggestions/Dashboard'
import { SuggestionsPage } from '../features/suggestions/SuggestionsPage'
import { TemplatesPage, DepartmentsPage } from '../features/catalog/CatalogPages'
import { AnalyticsPage } from '../features/analytics/AnalyticsPage'
import { Empty } from '../shared/ui/Feedback'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<AuthPage />} />
        <Route element={<Layout />}>
          <Route index element={<Dashboard />} />
          <Route path="suggestions" element={<SuggestionsPage />} />
          <Route path="templates" element={<TemplatesPage />} />
          <Route path="departments" element={<DepartmentsPage />} />
          <Route path="analytics" element={<AnalyticsPage />} />
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
