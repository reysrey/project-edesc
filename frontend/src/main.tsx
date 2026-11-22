import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import App from './app/App'
import { ApiError, setUnauthorizedHandler } from './shared/api/client'
import { sessionKey } from './features/auth/session'
import './app/design.css'
import './app/styles.css'

const client = new QueryClient({ defaultOptions: { queries: { staleTime: 30_000, retry: (count, error) => !(error instanceof ApiError && error.status < 500) && count < 1, refetchOnWindowFocus: false }, mutations: { retry: false } } })
setUnauthorizedHandler(() => {
  client.setQueryData(sessionKey, null)
  const privateQueries = { predicate: (query: { queryKey: readonly unknown[] }) => query.queryKey[0] !== sessionKey[0] }
  void client.cancelQueries(privateQueries)
  client.removeQueries(privateQueries)
})
createRoot(document.getElementById('root')!).render(<StrictMode><QueryClientProvider client={client}><App /></QueryClientProvider></StrictMode>)
