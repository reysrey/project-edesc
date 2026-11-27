import { useQuery } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import type { Suggestion } from '../../shared/api/contracts'

export const suggestionsKey = ['suggestions'] as const
export const serviceSettingsKey = ['service-settings'] as const
export function useServiceSettings(enabled = true) {
  return useQuery({
    queryKey: serviceSettingsKey,
    queryFn: ({ signal }) => api.serviceSettings(signal),
    enabled,
    refetchOnWindowFocus: 'always',
  })
}
export const suggestionKey = (userId: string, id: string) =>
  [...suggestionsKey, 'detail', userId, id] as const
export function useSuggestions(enabled = true) {
  return useQuery({
    queryKey: suggestionsKey,
    queryFn: ({ signal }) => api.suggestions(signal),
    enabled,
  })
}
export const statusConfig: Record<Suggestion['status'], { label: string; className: string }> = {
  Draft: { label: 'Черновик', className: '' },
  Submitted: { label: 'На проверке у модератора', className: 'st-new' },
  ReturnedByManager: { label: 'Возвращено модератором', className: 'st-rework' },
  InCommission: { label: 'На комиссии', className: 'st-review' },
  RevisionRequired: { label: 'Требует доработки', className: 'st-rework' },
  Rejected: { label: 'Отклонено', className: 'st-no' },
  ApprovedPlanning: { label: 'Одобрено к планированию', className: 'st-ok' },
  InImplementation: { label: 'Внедряется', className: 'st-review' },
  Implemented: { label: 'Внедрено', className: 'st-done' },
}
