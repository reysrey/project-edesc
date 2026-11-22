import { useQuery } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import type { Suggestion } from '../../shared/api/contracts'

export const suggestionsKey = ['suggestions'] as const
export function useSuggestions() {
  return useQuery({ queryKey: suggestionsKey, queryFn: ({ signal }) => api.suggestions(signal) })
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
