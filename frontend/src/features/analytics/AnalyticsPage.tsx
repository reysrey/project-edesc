import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { Unavailable } from '../../shared/ui/Unavailable'
import { csvCell, formatDate, formatCount } from '../../shared/lib/format'
import { statusConfig, useSuggestions } from '../suggestions/model'
import { filterSuggestions, queues } from '../suggestions/queues'
import type { Queue } from '../suggestions/queues'
import type { Suggestion } from '../../shared/api/contracts'

const columns = [
  { key: 'id', label: 'ID', value: (s: Suggestion) => s.id },
  { key: 'title', label: 'Название', value: (s: Suggestion) => s.title },
  { key: 'status', label: 'Статус', value: (s: Suggestion) => statusConfig[s.status].label },
  { key: 'created', label: 'Дата', value: (s: Suggestion) => formatDate(s.created) },
] as const
const unavailable = [
  'Автор',
  'Подразделение',
  'Направление',
  'Проблема',
  'Решение',
  'Ожидаемый эффект',
  'Комментарий',
  'Фактический результат',
  'Затраты',
  'Экономия / эффект',
  'История',
  'Поддержка сотрудников',
]
export function AnalyticsPage() {
  const query = useSuggestions()
  const [params] = useSearchParams()
  const [filtered, setFiltered] = useState(params.get('filtered') === '1')
  const [fields, setFields] = useState<string[]>(columns.map((c) => c.key))
  const candidate = params.get('queue') ?? 'all'
  const queue: Queue = Object.hasOwn(queues, candidate) ? (candidate as Queue) : 'all'
  const data = query.data ?? []
  const source = filtered ? filterSuggestions(data, params, queue) : data
  const selected = columns.filter((c) => fields.includes(c.key))
  function download() {
    const rows = [
      selected.map((c) => c.label),
      ...source.map((s) => selected.map((c) => c.value(s))),
    ]
    const url = URL.createObjectURL(
      new Blob(['\ufeff' + rows.map((row) => row.map(csvCell).join(';')).join('\r\n')], {
        type: 'text/csv;charset=utf-8',
      }),
    )
    const a = document.createElement('a')
    a.href = url
    a.download = `ideahub-${new Date().toISOString().slice(0, 10)}.csv`
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  return (
    <>
      <PageHead
        title="Экспорт предложений"
        subtitle="Выгрузите все предложения или выбранную очередь с фильтрами."
      />
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <>
          <ErrorMessage error={query.error} />
          <button className="btn btn-line" onClick={() => void query.refetch()}>
            Повторить
          </button>
        </>
      ) : !data.length ? (
        <Empty title="Пока нет данных для экспорта">
          <p>Созданные предложения появятся здесь автоматически.</p>
        </Empty>
      ) : (
        <div className="panel">
          <label className="setting-row">
            <span>
              <b>Текущий фильтр раздела</b>
              <small>
                {queues[queue].title}
                {params.get('q') ? ` · Поиск: ${params.get('q')}` : ''}
                {params.get('status')
                  ? ` · ${statusConfig[params.get('status') as Suggestion['status']]?.label ?? 'Неизвестный статус'}`
                  : ''}
                . В экспорт входят все страницы выборки.
              </small>
            </span>
            <input
              type="checkbox"
              checked={filtered}
              onChange={(e) => setFiltered(e.target.checked)}
            />
          </label>
          <div className="export-grid">
            {columns.map((column) => (
              <label key={column.key}>
                <input
                  type="checkbox"
                  checked={fields.includes(column.key)}
                  onChange={(e) =>
                    setFields(
                      e.target.checked
                        ? [...fields, column.key]
                        : fields.filter((f) => f !== column.key),
                    )
                  }
                />
                {column.label}
              </label>
            ))}
            {unavailable.map((label) => (
              <label key={label} className="unavailable-field">
                <input type="checkbox" disabled />
                {label}
                <small>Пока недоступно</small>
              </label>
            ))}
          </div>
          <p className="export-summary" role="status">
            Будет выгружено:{' '}
            {formatCount(source.length, ['предложение', 'предложения', 'предложений'])} ·{' '}
            {formatCount(selected.length, ['поле', 'поля', 'полей'])}
          </p>
          {!selected.length && (
            <p className="file-hint">Выберите хотя бы одно поле для выгрузки.</p>
          )}
          {!source.length && (
            <p className="file-hint">
              По текущему фильтру предложений нет. Отключите фильтр для выгрузки всех записей.
            </p>
          )}
          <button
            className="btn btn-primary"
            disabled={!source.length || !selected.length}
            onClick={download}
          >
            ↓ Скачать CSV
          </button>
          <Unavailable>
            В выгрузку включаются только доступные данные. Остальные поля станут доступны после
            подключения.
          </Unavailable>
        </div>
      )}
    </>
  )
}
