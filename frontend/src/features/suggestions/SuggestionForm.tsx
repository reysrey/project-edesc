import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import type { Profile, SuggestionInput, Template } from '../../shared/api/contracts'
import { ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { Modal } from '../../shared/ui/Modal'
import { suggestionsKey } from './model'
import { Unavailable } from '../../shared/ui/Unavailable'

const composeSchema = z.object({
  title: z.string(),
  currentFlaws: z.string(),
  proposedSolution: z.string(),
  improvementObject: z.string(),
  targetDepartmentId: z.string(),
  pendingDraftId: z.string().uuid().optional(),
})

export function SuggestionForm({
  template,
  initialObject,
  onClose,
}: {
  template?: Template
  initialObject?: string
  onClose: () => void
}) {
  const client = useQueryClient()
  const profile = useOutletContext<Profile>()
  const composeKey = `ideahub-compose-${profile.id}`
  const [restored] = useState(() => {
    if (template || initialObject) return null
    try {
      const parsed = composeSchema.safeParse(
        JSON.parse(sessionStorage.getItem(composeKey) ?? 'null'),
      )
      return parsed.success ? parsed.data : null
    } catch {
      return null
    }
  })
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: ({ signal }) => api.departments(signal),
  })
  const [form, setForm] = useState<SuggestionInput>({
    title: restored?.title ?? '',
    currentFlaws: restored?.currentFlaws ?? '',
    proposedSolution: restored?.proposedSolution ?? '',
    improvementObject:
      restored?.improvementObject ?? template?.improvementObject ?? initialObject ?? '',
    targetDepartmentId: restored?.targetDepartmentId ?? template?.departmentId ?? '',
  })
  const [draftId, setDraftId] = useState<string | undefined>(restored?.pendingDraftId)
  useEffect(() => {
    if (draftId) return
    try {
      if (Object.values(form).some((value) => value.trim()))
        sessionStorage.setItem(composeKey, JSON.stringify(form))
      else sessionStorage.removeItem(composeKey)
    } catch {
      /* The form remains usable if session storage is unavailable. */
    }
  }, [composeKey, form, draftId])
  const mutation = useMutation({
    mutationFn: async (submit: boolean) => {
      if (
        !form.title.trim() ||
        !form.currentFlaws.trim() ||
        !form.proposedSolution.trim() ||
        !form.improvementObject.trim()
      )
        throw new Error('Заполните обязательные поля содержательным текстом.')
      const payload = Object.fromEntries(
        Object.entries(form).map(([key, value]) => [key, value.trim()]),
      ) as SuggestionInput
      const id = draftId ?? (await api.createSuggestion(payload))
      setDraftId(id)
      try {
        sessionStorage.setItem(composeKey, JSON.stringify({ ...payload, pendingDraftId: id }))
      } catch {
        /* Retry remains possible while the form stays open. */
      }
      // Invalidate immediately: a failed submit still leaves a real server-side draft.
      void client.invalidateQueries({ queryKey: suggestionsKey })
      if (submit) await api.submitSuggestion(id)
    },
    onSuccess: async () => {
      try {
        sessionStorage.removeItem(composeKey)
      } catch {
        /* Storage may be disabled. */
      }
      await client.invalidateQueries({ queryKey: suggestionsKey })
      onClose()
    },
  })
  function field(key: keyof SuggestionInput) {
    return {
      value: form[key],
      onChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
      ) => setForm({ ...form, [key]: e.target.value }),
    }
  }
  return (
    <Modal
      title={template ? `Идея по шаблону «${template.title}»` : 'Новое предложение'}
      onClose={onClose}
      busy={mutation.isPending}
    >
      {departments.isPending ? (
        <Loading />
      ) : departments.error ? (
        <>
          <ErrorMessage error={departments.error} />
          <button className="btn btn-line" onClick={() => void departments.refetch()}>
            Повторить
          </button>
        </>
      ) : (
        <form
          onSubmit={(e) => {
            e.preventDefault()
            mutation.mutate(true)
          }}
        >
          {restored && (
            <p className="preview-form-note">
              Восстановлена форма из этой вкладки. Предложение ещё не отправлено.
            </p>
          )}
          {draftId && (
            <p className="notice-item" role="status">
              Черновик сохранён. Можно повторить отправку или закрыть окно — он останется в списке
              предложений.
            </p>
          )}
          <fieldset disabled={mutation.isPending || !!draftId}>
            <div className="field">
              <label htmlFor="idea-title">Название идеи *</label>
              <input
                id="idea-title"
                required
                maxLength={200}
                placeholder="Коротко сформулируйте предложение"
                {...field('title')}
              />
              <small className="counter">{form.title.length}/200</small>
            </div>
            <div className="two-col">
              <div className="field">
                <label htmlFor="idea-object">Объект улучшения *</label>
                <input
                  id="idea-object"
                  required
                  maxLength={200}
                  placeholder="Процесс, оборудование или рабочее место"
                  {...field('improvementObject')}
                />
              </div>
              <div className="field">
                <label htmlFor="idea-department">Подразделение *</label>
                <select id="idea-department" required {...field('targetDepartmentId')}>
                  <option value="">Выберите подразделение</option>
                  {departments.data?.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            {departments.data?.length === 0 && (
              <p className="notice-item">
                Для подачи предложения руководитель должен добавить подразделение.
              </p>
            )}
            <div className="field">
              <label htmlFor="idea-problem">Какую проблему вы заметили? *</label>
              <textarea
                id="idea-problem"
                required
                maxLength={10_000}
                placeholder={template?.problemHint || 'Опишите текущую ситуацию'}
                {...field('currentFlaws')}
              />
            </div>
            <div className="field">
              <label htmlFor="idea-solution">Как предлагаете её решить? *</label>
              <textarea
                id="idea-solution"
                required
                maxLength={10_000}
                placeholder={template?.solutionHint || 'Опишите ваше решение'}
                {...field('proposedSolution')}
              />
            </div>
          </fieldset>
          <fieldset className="unsupported-form-fields" disabled>
            <Unavailable>
              Ожидаемый эффект, фотографии и анонимная подача пока не сохраняются. Эти поля станут
              доступны после подключения.
            </Unavailable>
            <div className="field">
              <label htmlFor="idea-effect">Ожидаемый эффект</label>
              <textarea id="idea-effect" placeholder="Например, экономия 20 минут на смену" />
            </div>
            <div className="two-col">
              <div className="field">
                <label htmlFor="idea-before">Фото «До»</label>
                <input id="idea-before" type="file" accept="image/*" />
              </div>
              <div className="field">
                <label htmlFor="idea-after">Фото «После» / визуализация</label>
                <input id="idea-after" type="file" accept="image/*" />
              </div>
            </div>
            <label className="checkbox-label">
              <input type="checkbox" /> Отправить анонимно
            </label>
          </fieldset>
          {!draftId && (
            <small className="file-hint">
              Незавершённый текст сохраняется только в этой вкладке. Кнопка «Сохранить черновик»
              сохраняет его на сервере; редактирование сохранённого черновика пока недоступно.
            </small>
          )}
          <ErrorMessage error={mutation.error} />
          <div className="form-actions">
            <button
              type="button"
              className="btn btn-light"
              disabled={mutation.isPending}
              onClick={onClose}
            >
              Закрыть
            </button>
            <button
              type="button"
              className="btn btn-line"
              disabled={mutation.isPending || !departments.data?.length}
              onClick={(e) => {
                if (e.currentTarget.form?.reportValidity()) mutation.mutate(false)
              }}
            >
              Сохранить черновик
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={mutation.isPending || !departments.data?.length}
            >
              {mutation.isPending
                ? 'Сохраняем…'
                : draftId
                  ? 'Повторить отправку →'
                  : 'Отправить предложение →'}
            </button>
          </div>
        </form>
      )}
    </Modal>
  )
}
