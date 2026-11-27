import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../../shared/api/client'
import { api } from '../../shared/api/resources'
import type {
  Profile,
  SuggestionDetails,
  SuggestionInput,
  Template,
} from '../../shared/api/contracts'
import { ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { Modal } from '../../shared/ui/Modal'
import { suggestionKey, suggestionsKey, useServiceSettings } from './model'
import { Unavailable } from '../../shared/ui/Unavailable'
import { sessionKey, useSession } from '../auth/session'
import {
  composeSchema,
  inputFromCard,
  isWriteRejection,
  readSuggestionSession,
  rememberDraftForSubmission,
  submitWithRecovery,
  writeSuggestionSession,
} from './workflow'

export function SuggestionForm({
  template,
  idea,
  onClose,
}: {
  template?: Template
  idea?: SuggestionDetails
  onClose: () => void
}) {
  const client = useQueryClient()
  const { data: profile } = useSession()
  const userId = profile!.id
  const source = template?.id
  const composeKey = `ideahub-compose-${userId}${source ? `-${source}` : ''}`
  const [restored] = useState(() =>
    idea ? undefined : readSuggestionSession(composeKey, composeSchema),
  )
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: ({ signal }) => api.departments(signal),
  })
  const templates = useQuery({
    queryKey: ['templates'],
    queryFn: ({ signal }) => api.templates(signal),
  })
  const settings = useServiceSettings()
  const [base, setBase] = useState(idea)
  const [form, setForm] = useState<SuggestionInput>(() =>
    idea
      ? inputFromCard(idea)
      : {
          title: restored?.title ?? '',
          currentFlaws: restored?.currentFlaws ?? '',
          proposedSolution: restored?.proposedSolution ?? '',
          improvementObject: restored?.improvementObject ?? template?.improvementObject ?? '',
          targetDepartmentId: restored?.targetDepartmentId ?? template?.departmentId ?? '',
          templateId: restored ? (restored.templateId ?? null) : (template?.id ?? null),
          isAnonymous: restored?.isAnonymous,
        },
  )
  const [draftId, setDraftId] = useState(restored?.pendingDraftId)
  const [creation, setCreation] = useState(restored?.creation)
  const activeTemplates = (templates.data ?? []).filter((entry) => entry.isActive)
  const selectedTemplate = activeTemplates.find((entry) => entry.id === form.templateId)
  const savedTemplate = base?.templateId === form.templateId ? base : undefined
  const reload = useMutation({
    mutationFn: () => api.suggestion(idea!.id),
    onSuccess: (card) => {
      if (client.getQueryData<Profile>(sessionKey)?.id === userId)
        client.setQueryData(suggestionKey(userId, card.id), card)
    },
  })
  const mutation = useMutation({
    mutationFn: async (submit: boolean) => {
      let id = draftId
      if (!id) {
        const input: SuggestionInput = {
          title: form.title.trim(),
          currentFlaws: form.currentFlaws.trim(),
          proposedSolution: form.proposedSolution.trim(),
          improvementObject: form.improvementObject.trim(),
          targetDepartmentId: form.targetDepartmentId,
          templateId: form.templateId ?? null,
          isAnonymous: form.isAnonymous,
        }
        if (
          !input.title ||
          !input.currentFlaws ||
          !input.proposedSolution ||
          !input.improvementObject
        )
          throw new Error('Заполните обязательные поля содержательным текстом.')
        if (base) {
          await api.updateSuggestion(base.id, { ...input, version: base.version })
          return
        }
        const attempt = creation ?? { key: crypto.randomUUID(), input }
        setCreation(attempt)
        writeSuggestionSession(composeKey, { ...form, creation: attempt })
        try {
          id = await api.createSuggestion(attempt.input, attempt.key)
        } catch (error) {
          if (isWriteRejection(error) && error.status !== 401) {
            setCreation(undefined)
            writeSuggestionSession(composeKey, form)
          }
          throw error
        }
        setDraftId(id)
        setCreation(undefined)
        writeSuggestionSession(composeKey, { ...form, pendingDraftId: id })
        // The draft already exists even if the next request fails.
        void client.invalidateQueries({ queryKey: suggestionsKey })
      }
      if (submit) {
        rememberDraftForSubmission(userId, id, composeKey)
        await submitWithRecovery(userId, id)
      }
    },
    onSuccess: async () => {
      if (!idea) writeSuggestionSession(composeKey)
      await client.invalidateQueries({ queryKey: suggestionsKey })
      onClose()
    },
  })
  const conflict = !!idea && mutation.error instanceof ApiError && mutation.error.status === 409
  const latest = reload.data
  const busy = mutation.isPending || reload.isPending
  const latestHeading = useRef<HTMLHeadingElement>(null)
  useEffect(() => {
    if (latest) latestHeading.current?.focus()
  }, [latest])
  function change(values: Partial<SuggestionInput>) {
    const next = { ...form, ...values }
    setForm(next)
    if (!idea) writeSuggestionSession(composeKey, next)
  }
  function acceptLatest(card: SuggestionDetails, useServerValues: boolean) {
    setBase(card)
    if (useServerValues) setForm(inputFromCard(card))
    reload.reset()
    mutation.reset()
  }
  function field(key: Exclude<keyof SuggestionInput, 'templateId' | 'isAnonymous'>) {
    return {
      value: form[key],
      onChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>,
      ) => change({ [key]: e.target.value }),
    }
  }
  return (
    <Modal
      title={
        idea
          ? 'Редактирование предложения'
          : template
            ? `Идея по шаблону «${template.title}»`
            : 'Новое предложение'
      }
      onClose={onClose}
      busy={busy}
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
            if (!busy && !conflict) mutation.mutate(false)
          }}
        >
          {restored && !draftId && !creation && (
            <p className="preview-form-note">Восстановлен текст из этой вкладки.</p>
          )}
          {draftId && (
            <p className="notice-item" role="status">
              Черновик сохранён в разделе «Мои предложения». Отправку можно повторить здесь.
            </p>
          )}
          {creation && !busy && (
            <p className="notice-item" role="status">
              Подтверждение создания ещё не получено. Повторите действие, чтобы восстановить ту же
              запись.
            </p>
          )}
          <fieldset disabled={busy || !!draftId || !!creation}>
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
            <div className="field">
              <label htmlFor="idea-template">Шаблон</label>
              <select
                id="idea-template"
                value={form.templateId ?? ''}
                onChange={(event) => {
                  const id = event.target.value || null
                  const entry = activeTemplates.find((item) => item.id === id)
                  change({
                    templateId: id,
                    improvementObject:
                      form.improvementObject || (!idea && entry ? entry.improvementObject : ''),
                    targetDepartmentId:
                      form.targetDepartmentId || (!idea && entry ? (entry.departmentId ?? '') : ''),
                  })
                }}
              >
                <option value="">Без шаблона</option>
                {form.templateId && !selectedTemplate && (
                  <option value={form.templateId}>
                    {savedTemplate?.templateName ??
                      (template?.id === form.templateId ? template.title : 'Текущий шаблон')}
                    {savedTemplate?.templateIsActive === false ? ' (архивный)' : ''}
                  </option>
                )}
                {activeTemplates.map((entry) => (
                  <option key={entry.id} value={entry.id}>
                    {entry.title}
                  </option>
                ))}
              </select>
              <ErrorMessage error={templates.error} />
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
                placeholder={selectedTemplate?.problemHint || 'Опишите текущую ситуацию'}
                {...field('currentFlaws')}
              />
            </div>
            <div className="field">
              <label htmlFor="idea-solution">Как предлагаете её решить? *</label>
              <textarea
                id="idea-solution"
                required
                maxLength={10_000}
                placeholder={selectedTemplate?.solutionHint || 'Опишите ваше решение'}
                {...field('proposedSolution')}
              />
            </div>
          </fieldset>
          {(settings.data?.anonymousEnabled || base?.isAnonymous || form.isAnonymous) && (
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={form.isAnonymous ?? false}
                disabled={
                  busy ||
                  !!draftId ||
                  !!creation ||
                  (!settings.data?.anonymousEnabled && !base?.isAnonymous && !form.isAnonymous)
                }
                onChange={(event) => change({ isAnonymous: event.target.checked })}
              />
              Отправить анонимно
            </label>
          )}
          {!idea && (
            <fieldset className="unsupported-form-fields" disabled>
              <Unavailable>
                Ожидаемый эффект и фотографии появятся в отдельном обновлении формы.
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
            </fieldset>
          )}
          {!idea && !draftId && (
            <small className="file-hint">
              Незавершённый текст хранится в этой вкладке. Сохранённый черновик появится в разделе
              «Мои предложения».
            </small>
          )}
          {conflict ? (
            <section className="suggestion-conflict" aria-label="Конфликт сохранения">
              <p role="alert">
                {latest
                  ? 'Ваш текст остался в форме. Сравните его с данными карточки и выберите, что сохранить.'
                  : 'Предложение изменилось. Ваш текст остался в форме. Загрузите актуальную карточку, чтобы сравнить изменения.'}
              </p>
              <ErrorMessage error={reload.error} />
              {!latest ? (
                <button
                  type="button"
                  className="btn btn-line"
                  disabled={busy}
                  onClick={() => reload.mutate()}
                >
                  {reload.isPending ? 'Загружаем…' : 'Загрузить актуальную карточку'}
                </button>
              ) : (
                <>
                  <h3 tabIndex={-1} ref={latestHeading}>
                    Актуальная карточка
                  </h3>
                  <dl className="suggestion-facts">
                    <div>
                      <dt>Название</dt>
                      <dd>{latest.title}</dd>
                    </div>
                    <div>
                      <dt>Объект улучшения</dt>
                      <dd>{latest.improvementObject}</dd>
                    </div>
                    <div>
                      <dt>Подразделение</dt>
                      <dd>{latest.departmentName ?? '—'}</dd>
                    </div>
                    <div>
                      <dt>Шаблон</dt>
                      <dd>{latest.templateName ?? '—'}</dd>
                    </div>
                    <div>
                      <dt>Проблема</dt>
                      <dd>{latest.currentFlaws}</dd>
                    </div>
                    <div>
                      <dt>Решение</dt>
                      <dd>{latest.proposedSolution}</dd>
                    </div>
                  </dl>
                  {latest.allowedActions.includes('edit') ? (
                    <div className="form-actions">
                      <button
                        type="button"
                        className="btn btn-line"
                        onClick={() => acceptLatest(latest, true)}
                      >
                        Взять данные с сервера
                      </button>
                      <button
                        type="button"
                        className="btn btn-primary"
                        onClick={() => acceptLatest(latest, false)}
                      >
                        Продолжить с моим текстом
                      </button>
                    </div>
                  ) : (
                    <p>
                      Редактирование больше недоступно. Вы можете скопировать свой текст из формы.
                    </p>
                  )}
                </>
              )}
            </section>
          ) : (
            <ErrorMessage error={mutation.error} />
          )}
          <div className="form-actions">
            <button type="button" className="btn btn-light" disabled={busy} onClick={onClose}>
              Закрыть
            </button>
            <button
              type="submit"
              className="btn btn-line"
              disabled={busy || conflict || !departments.data?.length}
            >
              {idea ? 'Сохранить' : 'Сохранить черновик'}
            </button>
            {!idea && (
              <button
                type="button"
                className="btn btn-primary"
                disabled={busy || !departments.data?.length}
                onClick={(event) => {
                  if (event.currentTarget.form?.reportValidity()) mutation.mutate(true)
                }}
              >
                {mutation.isPending
                  ? 'Сохраняем…'
                  : draftId
                    ? 'Повторить отправку →'
                    : 'Отправить предложение →'}
              </button>
            )}
          </div>
        </form>
      )}
    </Modal>
  )
}
