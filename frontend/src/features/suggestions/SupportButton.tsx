import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Suggestion } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { ErrorMessage } from '../../shared/ui/Feedback'
import { Icon } from '../../shared/ui/Icon'
import { suggestionsKey } from './model'

export function SupportButton({ suggestion }: { suggestion: Suggestion }) {
  const client = useQueryClient()
  const supported = suggestion.supportedByMe ?? false
  const count = suggestion.supportCount ?? 0
  const mutation = useMutation({
    mutationFn: () => api.supportSuggestion(suggestion.id, !supported),
    onSuccess: () => client.invalidateQueries({ queryKey: suggestionsKey }),
  })
  if (!suggestion.allowedActions?.includes('support')) {
    return count > 0 ? <small>Поддержали {count}</small> : null
  }
  return (
    <div>
      <button
        type="button"
        className={`vote${supported ? ' active' : ''}`}
        aria-pressed={supported}
        disabled={mutation.isPending}
        onClick={() => mutation.mutate()}
      >
        <Icon name="heart" />
        {supported ? 'Отменить поддержку' : 'Поддержать'}
        <span aria-label={`Поддержали ${count}`}>{count}</span>
      </button>
      <ErrorMessage error={mutation.error} />
    </div>
  )
}
