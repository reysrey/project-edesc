import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../../shared/api/client'
import { api } from '../../shared/api/resources'
import { Modal } from '../../shared/ui/Modal'
import { Empty, ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { formatDate } from '../../shared/lib/format'
import { useSession } from '../auth/session'
import { statusConfig, suggestionKey, suggestionsKey } from './model'
import { isSuggestionPath } from './navigation'
import type { SuggestionNavigation } from './navigation'
import { Status } from './Status'
import { SuggestionForm } from './SuggestionForm'
import { SupportButton } from './SupportButton'
import { confirmPendingSubmission, submitWithRecovery } from './workflow'

export function SuggestionRoute() {
  const { id } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const { data: profile } = useSession()
  function close() {
    if ((location.state as SuggestionNavigation | null)?.suggestionFrom) void navigate(-1)
    else void navigate('/my-suggestions', { replace: true })
  }
  if (!profile) return null
  if (!id || !isSuggestionPath(location.pathname))
    return (
      <Modal title="Предложение" onClose={close}>
        <Empty title="Предложение недоступно или не найдено" />
      </Modal>
    )
  return (
    <SuggestionDetail key={`${profile.id}:${id}`} id={id} userId={profile.id} onClose={close} />
  )
}

function SuggestionDetail({
  id,
  userId,
  onClose,
}: {
  id: string
  userId: string
  onClose: () => void
}) {
  const client = useQueryClient()
  const query = useQuery({
    queryKey: suggestionKey(userId, id),
    queryFn: ({ signal }) => api.suggestion(id, signal),
    refetchOnMount: 'always',
  })
  const [editing, setEditing] = useState(false)
  const [recovered, setRecovered] = useState(false)
  const [action, setAction] = useState<'approve' | 'return' | 'reject'>('approve')
  const [value, setValue] = useState('')
  const refresh = () => client.invalidateQueries({ queryKey: suggestionsKey })
  const submission = useMutation({
    mutationFn: () => submitWithRecovery(userId, id),
    onSettled: refresh,
  })
  const moderation = useMutation({
    mutationFn: () => {
      const card = query.data!
      if (card.allowedActions.includes('commission'))
        return api.commissionDecision(id, action, card.version, value.trim())
      return action === 'approve'
        ? api.approveSuggestion(id, card.registrationNumber ?? value.trim())
        : api.returnSuggestion(id, value.trim())
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) return refresh()
    },
    onSuccess: async () => {
      await refresh()
      onClose()
    },
  })
  const busy = submission.isPending || moderation.isPending
  const suggestion = query.data
  const unavailable = query.error instanceof ApiError && [403, 404].includes(query.error.status)
  useEffect(() => {
    if (query.error || !suggestion) return
    const frame = requestAnimationFrame(() => {
      if (confirmPendingSubmission(userId, suggestion)) setRecovered(true)
    })
    return () => cancelAnimationFrame(frame)
  }, [query.error, suggestion, userId])
  if (editing && suggestion && !unavailable)
    return <SuggestionForm idea={suggestion} onClose={() => setEditing(false)} />

  if (query.error || !suggestion) {
    return (
      <Modal title="Предложение" onClose={onClose}>
        {query.isPending ? (
          <Loading />
        ) : unavailable ? (
          <Empty title="Предложение недоступно или не найдено" />
        ) : (
          <>
            <ErrorMessage error={query.error} />
            <button
              className="btn btn-line"
              disabled={query.isFetching}
              onClick={() => void query.refetch()}
            >
              Повторить загрузку
            </button>
          </>
        )}
      </Modal>
    )
  }

  const canEdit = suggestion.allowedActions.includes('edit')
  const canSubmit = suggestion.allowedActions.includes('submit')
  const canModerate = suggestion.allowedActions.includes('moderate')
  const canDecide = suggestion.allowedActions.includes('commission')
  const keepsNumber = canModerate && action === 'approve' && !!suggestion.registrationNumber
  const numberRequired = canModerate && action === 'approve'
  const noteRequired = !canDecide || action !== 'approve'
  return (
    <Modal title={suggestion.title} onClose={onClose} busy={busy}>
      <div className="suggestion-meta">
        <Status status={suggestion.status} />
        <span className="record-id">Создано {formatDate(suggestion.created)}</span>
        {suggestion.isMine && <span className="record-id">Ваше предложение</span>}
      </div>
      <dl className="suggestion-facts">
        <div>
          <dt>Объект улучшения</dt>
          <dd>{suggestion.improvementObject}</dd>
        </div>
        <div>
          <dt>Подразделение</dt>
          <dd>{suggestion.departmentName ?? '—'}</dd>
        </div>
        {suggestion.templateId && (
          <div>
            <dt>Шаблон</dt>
            <dd>
              {suggestion.templateName ?? '—'}
              {suggestion.templateIsActive === false && ' (архивный)'}
            </dd>
          </div>
        )}
        {suggestion.registrationNumber && (
          <div>
            <dt>Регистрационный номер</dt>
            <dd>{suggestion.registrationNumber}</dd>
          </div>
        )}
        {suggestion.authorName && (
          <div>
            <dt>Автор</dt>
            <dd>{suggestion.authorName}</dd>
          </div>
        )}
        {suggestion.isAnonymous && !suggestion.authorName && (
          <div>
            <dt>Автор</dt>
            <dd>Анонимно</dd>
          </div>
        )}
      </dl>
      <section className="suggestion-text">
        <h3>Проблема</h3>
        <p>{suggestion.currentFlaws}</p>
        <h3>Предлагаемое решение</h3>
        <p>{suggestion.proposedSolution}</p>
      </section>
      {!!suggestion.commissionNote?.trim() && (
        <section className="suggestion-note">
          <h3>
            {suggestion.status === 'ReturnedByManager' || suggestion.status === 'RevisionRequired'
              ? 'Причина возврата'
              : suggestion.status === 'Rejected'
                ? 'Причина отклонения'
                : 'Замечание'}
          </h3>
          <p>{suggestion.commissionNote}</p>
        </section>
      )}
      {suggestion.history.length > 0 && (
        <details className="suggestion-history">
          <summary>История изменений</summary>
          <ol>
            {suggestion.history.map((event) => (
              <li key={event.id}>
                <b>
                  {statusConfig[event.fromStatus].label} → {statusConfig[event.toStatus].label}
                </b>
                <small>{formatDate(event.occurredAt)}</small>
                {event.note && <p>{event.note}</p>}
              </li>
            ))}
          </ol>
        </details>
      )}
      <div className="suggestion-meta">
        <SupportButton suggestion={suggestion} />
      </div>
      <ErrorMessage error={recovered ? undefined : submission.error} />
      {(submission.isSuccess || recovered) && (
        <p className="notice-item" role="status">
          Подача подтверждена.
        </p>
      )}
      {(canEdit || canSubmit) && (
        <div className="form-actions">
          {canEdit && (
            <button className="btn btn-line" disabled={busy} onClick={() => setEditing(true)}>
              Редактировать
            </button>
          )}
          {canSubmit && (
            <button
              className="btn btn-primary"
              disabled={busy}
              onClick={() => {
                setRecovered(false)
                submission.mutate()
              }}
            >
              {submission.isPending
                ? 'Отправляем…'
                : suggestion.status === 'ReturnedByManager' ||
                    suggestion.status === 'RevisionRequired'
                  ? 'Подать повторно'
                  : 'Отправить предложение'}
            </button>
          )}
        </div>
      )}
      <ErrorMessage error={moderation.error} />
      {(canModerate || canDecide) && (
        <form
          className="suggestion-moderation"
          onSubmit={(event) => {
            event.preventDefault()
            if (!busy) moderation.mutate()
          }}
        >
          <fieldset disabled={busy}>
            <div className="field">
              <label htmlFor="decision">Решение по предложению</label>
              <select
                id="decision"
                value={action}
                onChange={(event) => {
                  const next = event.target.value
                  setAction(next === 'return' || next === 'reject' ? next : 'approve')
                  setValue('')
                }}
              >
                <option value="approve">{canDecide ? 'Одобрить' : 'Передать в комиссию'}</option>
                <option value="return">Вернуть на доработку</option>
                {canDecide && <option value="reject">Отклонить</option>}
              </select>
            </div>
            <div className="field">
              <label htmlFor="decision-value">
                {numberRequired
                  ? 'Регистрационный номер *'
                  : action === 'reject'
                    ? 'Причина отклонения *'
                    : action === 'return'
                      ? 'Причина возврата *'
                      : 'Комментарий к решению'}
              </label>
              <textarea
                id="decision-value"
                required={noteRequired}
                maxLength={numberRequired ? 200 : 10_000}
                value={keepsNumber ? suggestion.registrationNumber! : value}
                readOnly={keepsNumber}
                onChange={(event) => setValue(event.target.value)}
              />
            </div>
            <button
              className="btn btn-primary"
              disabled={noteRequired && !keepsNumber && !value.trim()}
            >
              {moderation.isPending ? 'Сохраняем…' : 'Подтвердить решение'}
            </button>
          </fieldset>
        </form>
      )}
    </Modal>
  )
}
