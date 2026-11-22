import { useState } from 'react'
import { Link, useOutletContext, useSearchParams } from 'react-router-dom'
import type { Profile, Suggestion } from '../../shared/api/contracts'
import { statuses } from '../../shared/api/contracts'
import { roleOf, canModerate, isLeader } from '../auth/roles'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { formatDate, formatCount } from '../../shared/lib/format'
import { Status } from './Status'
import { statusConfig, useSuggestions } from './model'
import { filterSuggestions, inQueue, queues } from './queues'
import type { Queue } from './queues'
import { SuggestionForm } from './SuggestionForm'
import { SuggestionDetail } from './SuggestionDetail'
import { SuggestionCard } from './SuggestionCard'

export function SuggestionsPage({
  queue = 'all',
  embedded = false,
}: {
  queue?: Queue
  embedded?: boolean
}) {
  const profile = useOutletContext<Profile>()
  const employee = roleOf(profile) === 'employee'
  const query = useSuggestions()
  const [params, setParams] = useSearchParams()
  const [create, setCreate] = useState(false)
  const [selectedId, setSelectedId] = useState<string>()
  const search = params.get('q') ?? ''
  const status = params.get('status') ?? ''
  const sort = params.get('sort') ?? 'newest'
  const sizeValue = Number(params.get('size') ?? 20)
  const pageSize = [10, 20, 50, 100].includes(sizeValue) ? sizeValue : 20
  const data = (query.data ?? []).filter((s) => !employee || s.status !== 'Draft')
  const filtered = filterSuggestions(data, params, queue)
  const pages = Math.max(1, Math.ceil(filtered.length / pageSize))
  const requestedPage = Number(params.get('page') ?? 1)
  const current = Number.isFinite(requestedPage)
    ? Math.min(pages, Math.max(1, Math.floor(requestedPage)))
    : 1
  const selected: Suggestion | undefined = query.data?.find((s) => s.id === selectedId)
  function update(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next, { replace: true })
  }
  function reset() {
    setParams({}, { replace: true })
  }
  const title = employee ? 'Доска предложений' : queues[queue].title
  const list = filtered.slice((current - 1) * pageSize, current * pageSize)
  const allowedStatuses = queues[queue].statuses as readonly string[] | null
  return (
    <>
      {embedded ? (
        <div className="section-head">
          <div>
            <h2>{title}</h2>
            <p>Знакомьтесь с идеями коллег и поддерживайте полезные инициативы.</p>
          </div>
          <span className="aside">
            {query.data
              ? formatCount(data.length, [
                  'предложение',
                  'предложения',
                  'предложений',
                ]).toUpperCase()
              : ''}
          </span>
        </div>
      ) : (
        <PageHead
          title={title}
          subtitle={
            employee
              ? 'Знакомьтесь с идеями коллег и поддерживайте полезные инициативы.'
              : 'Обрабатывайте идеи, оставляйте обратную связь и отслеживайте результат.'
          }
        >
          {employee ? (
            <button className="btn btn-primary" onClick={() => setCreate(true)}>
              ＋ Предложить идею
            </button>
          ) : (
            isLeader(profile) && (
              <Link
                className="btn btn-line"
                to={`/export?${new URLSearchParams({ ...Object.fromEntries(params), queue, filtered: '1' })}`}
              >
                Экспорт данных
              </Link>
            )
          )}
        </PageHead>
      )}
      {!employee && query.data && (
        <div className="stats queue-stats">
          {[
            ['В этом разделе', data.filter((s) => inQueue(s, queue)).length],
            ['Всего в системе', data.length],
            ['На доработке', data.filter((s) => inQueue(s, 'rework')).length],
            ['Внедрено', data.filter((s) => s.status === 'Implemented').length],
          ].map(([label, value]) => (
            <div className="stat" key={label}>
              <span>{label}</span>
              <strong>{value}</strong>
            </div>
          ))}
        </div>
      )}
      <div className="toolbar">
        <input
          aria-label="Поиск предложений"
          type="search"
          placeholder="Поиск по номеру или названию…"
          value={search}
          onChange={(e) => update('q', e.target.value)}
        />
        <select
          aria-label="Статус предложения"
          value={status}
          onChange={(e) => update('status', e.target.value)}
        >
          <option value="">Все статусы</option>
          {statuses
            .filter(
              (s) =>
                (!allowedStatuses || allowedStatuses.includes(s)) && (!employee || s !== 'Draft'),
            )
            .map((s) => (
              <option key={s} value={s}>
                {statusConfig[s].label}
              </option>
            ))}
        </select>
        <select
          aria-label="Порядок предложений"
          value={sort}
          onChange={(e) => update('sort', e.target.value)}
        >
          <option value="newest">Сначала новые</option>
          <option value="oldest">Сначала старые</option>
          <option value="title">По названию</option>
          <option value="status">По статусу</option>
        </select>
        <select
          aria-label="Предложений на странице"
          value={pageSize}
          onChange={(e) => update('size', e.target.value)}
        >
          {[10, 20, 50, 100].map((n) => (
            <option value={n} key={n}>
              {n} на странице
            </option>
          ))}
        </select>
        <button className="btn btn-light" onClick={reset}>
          Сбросить
        </button>
        <button
          className="btn btn-line"
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
      ) : !filtered.length ? (
        <Empty
          title={search || status ? 'Ничего не найдено' : 'В этом разделе пока нет предложений'}
        >
          <p>
            {search || status
              ? 'Попробуйте изменить поиск или фильтр.'
              : employee
                ? 'Предложите первую идею с помощью шаблона выше.'
                : 'Предложения появятся здесь, когда перейдут в соответствующий статус.'}
          </p>
          {(search || status) && (
            <button className="btn btn-light" onClick={reset}>
              Сбросить фильтры
            </button>
          )}
        </Empty>
      ) : (
        <>
          {employee ? (
            <div className="board">
              {list.map((s) => (
                <SuggestionCard suggestion={s} key={s.id} onOpen={() => setSelectedId(s.id)} />
              ))}
            </div>
          ) : (
            <div className="proposal-table">
              <div className="list-table-head">
                <button onClick={() => update('sort', sort === 'oldest' ? 'newest' : 'oldest')}>
                  Предложение · дата ↕
                </button>
                <span>Создано</span>
                <button onClick={() => update('sort', 'status')}>Статус ↕</button>
                <span />
              </div>
              {list.map((s) => (
                <div className="suggestion-row" key={s.id}>
                  <div>
                    <div className="idea-num">ППУ · {s.id.slice(-8)}</div>
                    <h3>{s.title}</h3>
                  </div>
                  <span className="idea-author">{formatDate(s.created)}</span>
                  <Status status={s.status} />
                  <button
                    className="btn btn-line"
                    aria-label={`Открыть: ${s.title}`}
                    onClick={() => setSelectedId(s.id)}
                  >
                    Открыть →
                  </button>
                </div>
              ))}
            </div>
          )}
          <nav className="pagination" aria-label="Страницы предложений">
            <span className="pagination-summary">
              Показано {(current - 1) * pageSize + 1}–
              {Math.min(current * pageSize, filtered.length)} из {filtered.length}
            </span>
            <div className="pagination-controls">
              <button
                className="btn btn-line"
                disabled={current === 1}
                onClick={() => update('page', String(current - 1))}
              >
                ← Назад
              </button>
              <span aria-live="polite">
                {current} / {pages}
              </span>
              <button
                className="btn btn-line"
                disabled={current === pages}
                onClick={() => update('page', String(current + 1))}
              >
                Далее →
              </button>
            </div>
          </nav>
        </>
      )}
      {create && <SuggestionForm onClose={() => setCreate(false)} />}
      {selected && (
        <SuggestionDetail
          suggestion={selected}
          manager={canModerate(profile)}
          onClose={() => setSelectedId(undefined)}
        />
      )}
    </>
  )
}
