import type { Suggestion } from '../../shared/api/contracts'

export const queues = {
  all: { title: 'Все предложения', path: '/suggestions', statuses: null },
  incoming: { title: 'Входящие', path: '/incoming', statuses: ['Submitted'] },
  review: { title: 'На рассмотрении', path: '/review', statuses: ['InCommission'] },
  rework: {
    title: 'На доработке',
    path: '/rework',
    statuses: ['ReturnedByManager', 'RevisionRequired'],
  },
  processed: {
    title: 'Обработанные',
    path: '/processed',
    statuses: ['Rejected', 'ApprovedPlanning', 'InImplementation', 'Implemented'],
  },
} as const
export type Queue = keyof typeof queues
export function inQueue(suggestion: Suggestion, queue: Queue) {
  const allowed: readonly string[] | null = queues[queue].statuses
  return allowed === null || allowed.includes(suggestion.status)
}
export function filterSuggestions(data: Suggestion[], params: URLSearchParams, queue: Queue) {
  const search = (params.get('q') ?? '').trim().toLocaleLowerCase('ru')
  const status = params.get('status') ?? ''
  const sort = params.get('sort') ?? 'newest'
  return data
    .filter(
      (s) =>
        inQueue(s, queue) &&
        (!status || s.status === status) &&
        (!search || `${s.title} ${s.id}`.toLocaleLowerCase('ru').includes(search)),
    )
    .sort((a, b) => {
      if (sort === 'title') return a.title.localeCompare(b.title, 'ru')
      if (sort === 'status') return a.status.localeCompare(b.status)
      return (sort === 'oldest' ? 1 : -1) * (Date.parse(a.created) - Date.parse(b.created))
    })
}
