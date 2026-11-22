import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import type { SuggestionInput, Template } from '../../shared/api/contracts'
import { ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { Modal } from '../../shared/ui/Modal'
import { suggestionsKey } from './model'

export function SuggestionForm({ template, onClose }: { template?: Template; onClose: () => void }) {
  const client = useQueryClient()
  const departments = useQuery({ queryKey: ['departments'], queryFn: ({ signal }) => api.departments(signal) })
  const [form, setForm] = useState<SuggestionInput>({ title: '', currentFlaws: '', proposedSolution: '', improvementObject: template?.improvementObject ?? '', targetDepartmentId: template?.departmentId ?? '' })
  const [draftId, setDraftId] = useState<string>()
  const mutation = useMutation({
    mutationFn: async (submit: boolean) => {
      const id = draftId ?? await api.createSuggestion(form)
      setDraftId(id)
      // Invalidate immediately: a failed submit still leaves a real server-side draft.
      void client.invalidateQueries({ queryKey: suggestionsKey })
      if (submit) await api.submitSuggestion(id)
    },
    onSuccess: async () => { await client.invalidateQueries({ queryKey: suggestionsKey }); onClose() },
  })
  function field(key: keyof SuggestionInput) { return { value: form[key], onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => setForm({ ...form, [key]: e.target.value }) } }
  return <Modal title={template ? `Идея по шаблону «${template.title}»` : 'Новое предложение'} onClose={onClose} busy={mutation.isPending}>
    {departments.isPending ? <Loading /> : departments.error ? <><ErrorMessage error={departments.error} /><button className="btn btn-line" onClick={() => void departments.refetch()}>Повторить</button></> : <form onSubmit={e => { e.preventDefault(); mutation.mutate(true) }}>
      {draftId && <p className="notice-item" role="status">Черновик сохранён. Можно повторить отправку или закрыть окно — он останется в списке предложений.</p>}
      <fieldset disabled={mutation.isPending || !!draftId}>
        <div className="field"><label htmlFor="idea-title">Название идеи *</label><input id="idea-title" required maxLength={200} placeholder="Коротко сформулируйте предложение" {...field('title')} /></div>
        <div className="two-col"><div className="field"><label htmlFor="idea-object">Объект улучшения *</label><input id="idea-object" required placeholder="Процесс, оборудование или рабочее место" {...field('improvementObject')} /></div>
        <div className="field"><label htmlFor="idea-department">Подразделение *</label><select id="idea-department" required {...field('targetDepartmentId')}><option value="">Выберите подразделение</option>{departments.data?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></div></div>
        {departments.data?.length === 0 && <p className="notice-item">Для подачи предложения администратор должен добавить подразделение.</p>}
        <div className="field"><label htmlFor="idea-problem">Какую проблему вы заметили? *</label><textarea id="idea-problem" required placeholder={template?.problemHint || 'Опишите текущую ситуацию'} {...field('currentFlaws')} /></div>
        <div className="field"><label htmlFor="idea-solution">Как предлагаете её решить? *</label><textarea id="idea-solution" required placeholder={template?.solutionHint || 'Опишите ваше решение'} {...field('proposedSolution')} /></div>
      </fieldset><ErrorMessage error={mutation.error} /><div className="form-actions"><button type="button" className="btn btn-light" disabled={mutation.isPending} onClick={onClose}>Закрыть</button><button type="button" className="btn btn-line" disabled={mutation.isPending || !departments.data?.length} onClick={e => { if (e.currentTarget.form?.reportValidity()) mutation.mutate(false) }}>Сохранить черновик</button><button type="submit" className="btn btn-primary" disabled={mutation.isPending || !departments.data?.length}>{mutation.isPending ? 'Сохраняем…' : draftId ? 'Повторить отправку →' : 'Отправить предложение →'}</button></div>
    </form>}
  </Modal>
}
