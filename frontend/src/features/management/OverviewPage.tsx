import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { Suggestion } from '../../shared/api/contracts'
import { statuses } from '../../shared/api/contracts'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { Unavailable } from '../../shared/ui/Unavailable'
import { formatDate } from '../../shared/lib/format'
import { useSuggestions, statusConfig } from '../suggestions/model'
import { Status } from '../suggestions/Status'
import { SuggestionDetail } from '../suggestions/SuggestionDetail'

export function OverviewPage() {
  const query = useSuggestions()
  const [selected, setSelected] = useState<Suggestion>()
  if (query.isPending) return <Loading />
  if (query.error)
    return (
      <>
        <ErrorMessage error={query.error} />
        <button className="btn btn-line" onClick={() => void query.refetch()}>
          Повторить
        </button>
      </>
    )
  const data = query.data
  const recent = [...data].sort((a, b) => Date.parse(b.created) - Date.parse(a.created)).slice(0, 5)
  const done = data.filter((s) => s.status === 'Implemented').length
  return (
    <>
      <PageHead
        title="Обзор IdeaHub"
        subtitle="Сводка по инициативам, статусам и результатам предприятия."
      >
        <Link className="btn btn-primary" to="/suggestions">
          Все предложения →
        </Link>
      </PageHead>
      <div className="stats">
        {[
          ['Всего идей', data.length, 'За всё время'],
          [
            'Внедрено',
            done,
            `${data.length ? Math.round((done / data.length) * 100) : 0}% от всех предложений`,
          ],
          [
            'Одобрено',
            data.filter((s) => s.status === 'ApprovedPlanning').length,
            'Ожидают реализации',
          ],
          [
            'На рассмотрении',
            data.filter((s) => s.status === 'InCommission').length,
            'Требуют решения',
          ],
        ].map(([label, value, hint]) => (
          <div className="stat" key={label}>
            <span>{label}</span>
            <strong>{value}</strong>
            <small>{hint}</small>
          </div>
        ))}
      </div>
      <div className="two-col overview-panels">
        <div className="panel analytics-panel">
          <h2>Распределение по статусам</h2>
          <p>Текущее состояние потока предложений.</p>
          {statuses.map((status) => {
            const count = data.filter((s) => s.status === status).length
            return (
              <div className="bar-row" key={status}>
                <span>{statusConfig[status].label}</span>
                <div className="bar-track" aria-hidden="true">
                  <i style={{ width: `${data.length ? (count / data.length) * 100 : 0}%` }} />
                </div>
                <b>{count}</b>
              </div>
            )
          })}
        </div>
        <div className="panel">
          <h2>Последние инициативы</h2>
          <p>Недавно добавленные предложения.</p>
          <div className="my-list">
            {recent.length ? (
              recent.map((s) => (
                <div className="my-item" key={s.id}>
                  <div>
                    <b>{s.title}</b>
                    <small>{formatDate(s.created)}</small>
                  </div>
                  <Status status={s.status} />
                  <button className="btn btn-light" onClick={() => setSelected(s)}>
                    Открыть
                  </button>
                </div>
              ))
            ) : (
              <Empty title="Пока нет предложений">
                <p>Здесь появятся первые инициативы сотрудников.</p>
              </Empty>
            )}
          </div>
        </div>
      </div>
      <div className="panel">
        <h2>Активность подразделений</h2>
        <Unavailable>Разбивка по подразделениям пока недоступна.</Unavailable>
      </div>
      {selected && (
        <SuggestionDetail
          suggestion={query.data.find((s) => s.id === selected.id) ?? selected}
          manager
          onClose={() => setSelected(undefined)}
        />
      )}
    </>
  )
}
export function EffectivenessPage() {
  const query = useSuggestions()
  const [selected, setSelected] = useState<Suggestion>()
  const done = (query.data ?? []).filter((s) => s.status === 'Implemented')
  return (
    <>
      <PageHead title="Эффективность" subtitle="Фактические результаты внедрённых инициатив." />
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <>
          <ErrorMessage error={query.error} />
          <button className="btn btn-line" onClick={() => void query.refetch()}>
            Повторить
          </button>
        </>
      ) : (
        <>
          <div className="stats">
            <div className="stat">
              <span>Внедрено</span>
              <strong>{done.length}</strong>
              <small>Решений запущено</small>
            </div>
            <div className="stat">
              <span>Доля внедрённых</span>
              <strong>
                {query.data?.length ? Math.round((done.length / query.data.length) * 100) : 0}%
              </strong>
              <small>От всех предложений</small>
            </div>
            <div className="stat">
              <span>Экономический эффект</span>
              <strong aria-label="Данные недоступны">—</strong>
              <small>Данные пока недоступны</small>
            </div>
            <div className="stat">
              <span>Поддержка сотрудников</span>
              <strong aria-label="Данные недоступны">—</strong>
              <small>Данные пока недоступны</small>
            </div>
          </div>
          <div className="panel">
            <h2>Внедрённые предложения</h2>
            <p>Решения, которые уже изменили работу предприятия.</p>
            {done.length ? (
              <div className="my-list">
                {done.map((s) => (
                  <div className="my-item" key={s.id}>
                    <div>
                      <b>{s.title}</b>
                      <small>{formatDate(s.created)}</small>
                    </div>
                    <Status status={s.status} />
                    <button className="btn btn-light" onClick={() => setSelected(s)}>
                      Результат →
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <Empty title="Пока нет внедрённых предложений">
                <p>Они появятся после завершения внедрения.</p>
              </Empty>
            )}
          </div>
          <Unavailable>
            Описание фактического результата, затраты и экономия пока недоступны.
          </Unavailable>
        </>
      )}
      {selected && (
        <SuggestionDetail suggestion={selected} manager onClose={() => setSelected(undefined)} />
      )}
    </>
  )
}
