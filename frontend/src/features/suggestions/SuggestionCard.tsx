import type { Suggestion } from '../../shared/api/contracts'
import { formatDate } from '../../shared/lib/format'
import { Status } from './Status'
import { SupportButton } from './SupportButton'

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
        <SupportButton suggestion={suggestion} />
      </div>
      <button className="link-button" aria-label={`Открыть: ${suggestion.title}`} onClick={onOpen}>
        Открыть предложение →
      </button>
    </article>
  )
}
