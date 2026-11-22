import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useOutletContext } from 'react-router-dom'
import { api } from '../../shared/api/resources'
import type {
  Department,
  DepartmentInput,
  Profile,
  Template,
  TemplateInput,
} from '../../shared/api/contracts'
import { isLeader } from '../auth/roles'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { Modal } from '../../shared/ui/Modal'
import { SuggestionForm } from '../suggestions/SuggestionForm'

export function DepartmentsPage() {
  const manager = isLeader(useOutletContext<Profile>())
  const query = useQuery({
    queryKey: ['departments'],
    queryFn: ({ signal }) => api.departments(signal),
  })
  const [edit, setEdit] = useState<{ department?: Department }>()
  const [remove, setRemove] = useState<Department>()
  const [search, setSearch] = useState('')
  const visible = query.data?.filter((d) =>
    d.name.toLocaleLowerCase('ru').includes(search.trim().toLocaleLowerCase('ru')),
  )
  return (
    <>
      <PageHead title="Подразделения" subtitle="Команды, которые делают предприятие лучше.">
        {manager && (
          <button className="btn btn-primary" onClick={() => setEdit({})}>
            ＋ Добавить подразделение
          </button>
        )}
      </PageHead>
      <div className="toolbar">
        <input
          type="search"
          aria-label="Поиск подразделений"
          placeholder="Найти подразделение…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button
          className="btn btn-line refresh-button"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          Обновить
        </button>
      </div>
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <ErrorMessage error={query.error} />
      ) : !visible?.length ? (
        <Empty title={search ? 'Подразделения не найдены' : 'Подразделений пока нет'}>
          <p>
            {manager
              ? 'Добавьте подразделение для подачи идей.'
              : 'Руководитель скоро добавит подразделения.'}
          </p>
        </Empty>
      ) : (
        <div className="catalog-grid">
          {visible.map((d) => (
            <article className="panel" key={d.id}>
              <div className="catalog-icon" aria-hidden="true">
                ▦
              </div>
              <h2>{d.name}</h2>
              <p>Сотрудников: {d.employeesCount}</p>
              {manager && (
                <div className="card-actions">
                  <button className="btn btn-line" onClick={() => setEdit({ department: d })}>
                    Изменить
                  </button>
                  <button className="btn btn-danger" onClick={() => setRemove(d)}>
                    Удалить
                  </button>
                </div>
              )}
            </article>
          ))}
        </div>
      )}
      {edit && <DepartmentForm department={edit.department} onClose={() => setEdit(undefined)} />}
      {remove && (
        <ConfirmAction
          title="Удалить подразделение?"
          description={`«${remove.name}» будет удалено. Если есть связанные данные, сервер может запретить удаление.`}
          action={() => api.deleteDepartment(remove.id)}
          queryKey="departments"
          onClose={() => setRemove(undefined)}
        />
      )}
    </>
  )
}

function DepartmentForm({ department, onClose }: { department?: Department; onClose: () => void }) {
  const [form, setForm] = useState<DepartmentInput>({
    name: department?.name ?? '',
    headUserId: department?.headUserId ?? null,
    employeesCount: department?.employeesCount ?? 0,
  })
  const client = useQueryClient()
  const mutation = useMutation({
    mutationFn: () =>
      department ? api.updateDepartment(department.id, form) : api.createDepartment(form),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ['departments'] })
      onClose()
    },
  })
  return (
    <Modal
      title={department ? 'Изменить подразделение' : 'Новое подразделение'}
      onClose={onClose}
      busy={mutation.isPending}
    >
      <form
        onSubmit={(e) => {
          e.preventDefault()
          mutation.mutate()
        }}
      >
        <fieldset disabled={mutation.isPending}>
          <div className="field">
            <label htmlFor="department-name">Название *</label>
            <input
              id="department-name"
              required
              maxLength={200}
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </div>
          <div className="field">
            <label htmlFor="employees">Количество сотрудников *</label>
            <input
              id="employees"
              type="number"
              min="0"
              step="1"
              required
              value={form.employeesCount}
              onChange={(e) => setForm({ ...form, employeesCount: Number(e.target.value) })}
            />
          </div>
          <div className="field">
            <label htmlFor="department-head">Модератор подразделения</label>
            <input
              id="department-head"
              value={form.headUserId ?? 'Назначение пока недоступно'}
              readOnly
              disabled
            />
            <small className="file-hint">
              Выбор сотрудника станет доступен после подключения списка пользователей.
            </small>
          </div>
          <ErrorMessage error={mutation.error} />
          <div className="form-actions">
            <button type="button" className="btn btn-light" onClick={onClose}>
              Отмена
            </button>
            <button className="btn btn-primary">
              {mutation.isPending ? 'Сохраняем…' : 'Сохранить'}
            </button>
          </div>
        </fieldset>
      </form>
    </Modal>
  )
}

export function TemplatesPage() {
  const manager = isLeader(useOutletContext<Profile>())
  const query = useQuery({
    queryKey: ['templates'],
    queryFn: ({ signal }) => api.templates(signal),
  })
  const [edit, setEdit] = useState<{ template?: Template }>()
  const [archive, setArchive] = useState<Template>()
  const [idea, setIdea] = useState<Template>()
  const [search, setSearch] = useState('')
  const [type, setType] = useState('')
  const visible = query.data?.filter(
    (t) =>
      (!type || t.type === type) &&
      `${t.title} ${t.improvementObject}`
        .toLocaleLowerCase('ru')
        .includes(search.trim().toLocaleLowerCase('ru')),
  )
  return (
    <>
      <PageHead
        title="Шаблоны предложений"
        subtitle="Простая отправная точка для полезных изменений."
      >
        {manager && (
          <button className="btn btn-primary" onClick={() => setEdit({})}>
            ＋ Создать шаблон
          </button>
        )}
      </PageHead>
      <div className="toolbar">
        <input
          type="search"
          aria-label="Поиск шаблонов"
          placeholder="Найти шаблон или узкое место…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select aria-label="Тип шаблона" value={type} onChange={(e) => setType(e.target.value)}>
          <option value="">Все типы</option>
          <option value="Template">Шаблоны</option>
          <option value="Bottleneck">Узкие места</option>
        </select>
        <button
          className="btn btn-line refresh-button"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          Обновить
        </button>
      </div>
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <ErrorMessage error={query.error} />
      ) : !visible?.length ? (
        <Empty title={search || type ? 'Шаблоны не найдены' : 'Шаблоны пока не добавлены'}>
          <p>Предложение можно создать без шаблона на странице обзора.</p>
        </Empty>
      ) : (
        <div className="catalog-grid">
          {visible.map((t) => (
            <article className="panel" key={t.id}>
              <div className="catalog-icon" aria-hidden="true">
                {t.type === 'Bottleneck' ? '◷' : '✦'}
              </div>
              <span className="idea-num">{t.type === 'Bottleneck' ? 'УЗКОЕ МЕСТО' : 'ШАБЛОН'}</span>
              <h2>{t.title}</h2>
              <p>{t.improvementObject}</p>
              {t.problemHint && <p>{t.problemHint}</p>}
              <div className="card-actions">
                <button className="btn btn-primary" onClick={() => setIdea(t)}>
                  Предложить идею →
                </button>
                {manager && (
                  <>
                    <button className="btn btn-line" onClick={() => setEdit({ template: t })}>
                      Изменить
                    </button>
                    <button className="btn btn-light" onClick={() => setArchive(t)}>
                      В архив
                    </button>
                  </>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
      {edit && <TemplateForm template={edit.template} onClose={() => setEdit(undefined)} />}
      {idea && <SuggestionForm template={idea} onClose={() => setIdea(undefined)} />}
      {archive && (
        <ConfirmAction
          title="Архивировать шаблон?"
          description={`«${archive.title}» больше не будет доступен для новых предложений.`}
          action={() => api.archiveTemplate(archive.id)}
          queryKey="templates"
          onClose={() => setArchive(undefined)}
        />
      )}
    </>
  )
}

function TemplateForm({ template, onClose }: { template?: Template; onClose: () => void }) {
  const client = useQueryClient()
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: ({ signal }) => api.departments(signal),
  })
  const [form, setForm] = useState<TemplateInput>({
    title: template?.title ?? '',
    improvementObject: template?.improvementObject ?? '',
    problemHint: template?.problemHint ?? '',
    solutionHint: template?.solutionHint ?? '',
    type: template?.type ?? 'Template',
    departmentId: template?.departmentId ?? null,
    isActive: template?.isActive ?? true,
  })
  const mutation = useMutation({
    mutationFn: () => (template ? api.updateTemplate(template.id, form) : api.createTemplate(form)),
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ['templates'] })
      onClose()
    },
  })
  return (
    <Modal
      title={template ? 'Изменить шаблон' : 'Новый шаблон'}
      onClose={onClose}
      busy={mutation.isPending}
    >
      <form
        onSubmit={(e) => {
          e.preventDefault()
          mutation.mutate()
        }}
      >
        <fieldset disabled={mutation.isPending}>
          <div className="field">
            <label htmlFor="template-title">Название *</label>
            <input
              id="template-title"
              required
              maxLength={200}
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </div>
          <div className="two-col">
            <div className="field">
              <label htmlFor="template-type">Тип</label>
              <select
                id="template-type"
                value={form.type}
                onChange={(e) => setForm({ ...form, type: e.target.value as Template['type'] })}
              >
                <option value="Template">Шаблон</option>
                <option value="Bottleneck">Узкое место</option>
              </select>
            </div>
            <div className="field">
              <label htmlFor="template-department">Подразделение</label>
              <select
                id="template-department"
                value={form.departmentId ?? ''}
                disabled={departments.isPending || !!departments.error}
                onChange={(e) => setForm({ ...form, departmentId: e.target.value || null })}
              >
                <option value="">Все подразделения</option>
                {departments.data?.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <ErrorMessage error={departments.error} />
          <div className="field">
            <label htmlFor="template-object">Объект улучшения *</label>
            <input
              id="template-object"
              required
              value={form.improvementObject}
              onChange={(e) => setForm({ ...form, improvementObject: e.target.value })}
            />
          </div>
          <div className="field">
            <label htmlFor="problem-hint">Подсказка для описания проблемы</label>
            <textarea
              id="problem-hint"
              value={form.problemHint}
              onChange={(e) => setForm({ ...form, problemHint: e.target.value })}
            />
          </div>
          <div className="field">
            <label htmlFor="solution-hint">Подсказка для описания решения</label>
            <textarea
              id="solution-hint"
              value={form.solutionHint}
              onChange={(e) => setForm({ ...form, solutionHint: e.target.value })}
            />
          </div>
          <ErrorMessage error={mutation.error} />
          <div className="form-actions">
            <button type="button" className="btn btn-light" onClick={onClose}>
              Отмена
            </button>
            <button className="btn btn-primary">
              {mutation.isPending ? 'Сохраняем…' : 'Сохранить'}
            </button>
          </div>
        </fieldset>
      </form>
    </Modal>
  )
}

function ConfirmAction({
  title,
  description,
  action,
  queryKey,
  onClose,
}: {
  title: string
  description: string
  action: () => Promise<unknown>
  queryKey: string
  onClose: () => void
}) {
  const client = useQueryClient()
  const mutation = useMutation({
    mutationFn: action,
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: [queryKey] })
      onClose()
    },
  })
  return (
    <Modal title={title} onClose={onClose} busy={mutation.isPending}>
      <p className="confirm-description">{description}</p>
      <ErrorMessage error={mutation.error} />
      <div className="form-actions">
        <button className="btn btn-light" disabled={mutation.isPending} onClick={onClose}>
          Отмена
        </button>
        <button
          className="btn btn-danger"
          disabled={mutation.isPending}
          onClick={() => mutation.mutate()}
        >
          {mutation.isPending ? 'Выполняем…' : 'Подтвердить'}
        </button>
      </div>
    </Modal>
  )
}
