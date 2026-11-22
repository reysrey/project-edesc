import { Status } from './Status'
import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { Suggestion } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { Modal } from '../../shared/ui/Modal'
import { ErrorMessage } from '../../shared/ui/Feedback'
import { formatDate } from '../../shared/lib/format'
import { suggestionsKey } from './model'

export function SuggestionDetail({ suggestion, manager, onClose }: { suggestion: Suggestion; manager: boolean; onClose: () => void }) {
  const client = useQueryClient()
  const [action, setAction] = useState<'approve' | 'return'>('approve')
  const [value, setValue] = useState('')
  const mutation = useMutation({ mutationFn: () => action === 'approve' ? api.approveSuggestion(suggestion.id, value.trim()) : api.returnSuggestion(suggestion.id, value.trim()), onSuccess: async () => { await client.invalidateQueries({ queryKey: suggestionsKey }); onClose() } })
  return <Modal title={suggestion.title} onClose={onClose} busy={mutation.isPending}>
    <div className="detail"><Status status={suggestion.status} /><p className="idea-author">Создано {formatDate(suggestion.created)}</p><p className="record-id">ID: {suggestion.id}</p></div>
    <p className="preview-form-note">Подробное описание пока недоступно: сервер возвращает только заголовок, статус и дату предложения.</p>
    {manager && suggestion.status === 'Submitted' && <form onSubmit={e => { e.preventDefault(); mutation.mutate() }}><fieldset disabled={mutation.isPending}>
      <div className="field"><label htmlFor="decision">Решение руководителя</label><select id="decision" value={action} onChange={e => { setAction(e.target.value as 'approve' | 'return'); setValue('') }}><option value="approve">Передать в комиссию</option><option value="return">Вернуть на доработку</option></select></div>
      <div className="field"><label htmlFor="decision-value">{action === 'approve' ? 'Регистрационный номер *' : 'Причина возврата *'}</label><textarea id="decision-value" required value={value} onChange={e => setValue(e.target.value)} /></div><ErrorMessage error={mutation.error} /><button className="btn btn-primary" disabled={!value.trim()}>{mutation.isPending ? 'Сохраняем…' : 'Подтвердить решение'}</button>
    </fieldset></form>}
  </Modal>
}

