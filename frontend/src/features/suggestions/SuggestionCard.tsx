import type { Suggestion } from '../../shared/api/contracts'
import { formatDate } from '../../shared/lib/format'
import { Status } from './Status'
import { Icon } from '../../shared/ui/Icon'

export function SuggestionCard({
  suggestion,
  onOpen,
}: {
  suggestion: Suggestion
  onOpen: () => void
}) {
  return (
    <article className="idea">
      <div className="idea-top">
        <div>
          <div className="idea-num">ППУ · {suggestion.id.slice(-8)}</div>
          <h3>{suggestion.title}</h3>
        </div>
        <Status status={suggestion.status} />
      </div>
      <div className="idea-footer">
        <span className="idea-author">{formatDate(suggestion.created)}</span>
        <div className="vote-row support-only">
          <button
            className="vote"
            disabled
            title="Поддержка идей пока недоступна"
            aria-describedby={`support-${suggestion.id}`}
          >
            <Icon name="heart" /> Поддержать
          </button>
        </div>
      </div>
      <small id={`support-${suggestion.id}`} className="support-hint">
        Поддержка идей пока недоступна.
      </small>
      <button className="link-button" aria-label={`Открыть: ${suggestion.title}`} onClick={onOpen}>
        Открыть предложение →
      </button>
    </article>
  )
}
