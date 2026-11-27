import { useLocation, useNavigate } from 'react-router-dom'
import type { Location } from 'react-router-dom'

export type SuggestionNavigation = { suggestionFrom?: Location; suggestionPath?: string }

export function isSuggestionPath(path: unknown): path is string {
  return (
    typeof path === 'string' &&
    /^\/suggestions\/[\da-f]{8}(?:-[\da-f]{4}){3}-[\da-f]{12}$/i.test(path)
  )
}

export function useOpenSuggestion() {
  const navigate = useNavigate()
  const location = useLocation()
  return (id: string) => navigate(`/suggestions/${id}`, { state: { suggestionFrom: location } })
}
