import { useState } from 'react'
import { Link, useOutletContext, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import type { Profile, SuggestionSearch } from '../../shared/api/contracts'
import { statuses } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { roleOf, isLeader } from '../auth/roles'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { formatDate, formatCount } from '../../shared/lib/format'
import { Status } from './Status'
import { statusConfig, suggestionsKey, useServiceSettings, useSuggestions } from './model'
import { filterSuggestions, inQueue, queues } from './queues'
import type { Queue } from './queues'
import { SuggestionForm } from './SuggestionForm'
import { SuggestionCard } from './SuggestionCard'
import { useOpenSuggestion } from './navigation'

export function SuggestionsPage({
  queue = 'all',
  embedded = false,
  mine = false,
}: {
  queue?: Queue
  embedded?: boolean
  mine?: boolean
}) {
  const profile = useOutletContext<Profile>()
  const employee = roleOf(profile) === 'employee'
  const settings = useServiceSettings(employee && !mine)
  const boardDisabled = employee && !mine && settings.data?.boardEnabled === false
  const board = useSuggestions(!mine && !boardDisabled)
  const openSuggestion = useOpenSuggestion()
  const [params, setParams] = useSearchParams()
  const [create, setCreate] = useState(false)
  const prefix = embedded && mine ? 'mine-' : ''
  const search = params.get(prefix + 'q') ?? ''
  const status = params.get(prefix + 'status') ?? ''
  const sort = params.get(prefix + 'sort') ?? 'newest'
  const sizeValue = Number(params.get(prefix + 'size') ?? 20)
  const pageSize = [10, 20, 50, 100].includes(sizeValue) ? sizeValue : 20
  const pageValue = Number(params.get(prefix + 'page') ?? 1)
  const requestedPage = Number.isFinite(pageValue)
    ? Math.min(2_147_483_647, Math.max(1, Math.floor(pageValue)))
    : 1
  const mineFilters: SuggestionSearch = {
    scope: 'mine',
    search: search.trim().slice(0, 200),
    status: statuses.find((value) => value === status),
    sort:
      sort === 'oldest'
        ? 'createdAsc'
        : sort === 'title' || sort === 'status'
          ? sort
          : 'createdDesc',
    pageNumber: requestedPage,
    pageSize,
  }
  const personal = useQuery({
    queryKey: [...suggestionsKey, 'mine', profile.id, mineFilters],
    queryFn: ({ signal }) => api.searchSuggestions(mineFilters, signal),
    enabled: mine,
  })
  const query = mine ? personal : board
  const data = (board.data ?? []).filter((s) => !employee || s.status !== 'Draft')
  const filtered = filterSuggestions(data, params, queue)
  const total = mine ? (personal.data?.totalCount ?? 0) : filtered.length
  const pages = Math.max(1, Math.ceil(total / pageSize))
  const current = mine
    ? (personal.data?.pageNumber ?? requestedPage)
    : Math.min(pages, requestedPage)
  function update(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(prefix + key, value)
    else next.delete(prefix + key)
    if (key !== 'page') next.delete(prefix + 'page')
    setParams(next, { replace: true })
  }
  function reset() {
    const next = new URLSearchParams(params)
    for (const key of ['q', 'status', 'sort', 'size', 'page']) next.delete(prefix + key)
    setParams(next, { replace: true })
  }
  const title = mine ? 'Мои предложения' : employee ? 'Доска предложений' : queues[queue].title
  const list = mine
    ? (personal.data?.items ?? [])
    : filtered.slice((current - 1) * pageSize, current * pageSize)
  const description = mine
    ? 'Ваши черновики, отправленные идеи и предложения на доработке.'
    : 'Знакомьтесь с идеями коллег и поддерживайте полезные инициативы.'
  const allowedStatuses = queues[queue].statuses as readonly string[] | null
  return (
    <>
      {embedded ? (
        <div className="section-head">
          <div>
            <h2 id={mine ? 'my-suggestions-title' : 'suggestion-board-title'}>{title}</h2>
            <p>{description}</p>
          </div>
          <span className="aside">
            {query.data
              ? formatCount(mine ? total : data.length, [
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
            employee || mine
              ? description
              : 'Обрабатывайте идеи, оставляйте обратную связь и отслеживайте результат.'
          }
        >
          {employee || mine ? (
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
      {!employee && !mine && query.data && (
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
      {!boardDisabled && (
        <div className="toolbar">
          <input
            aria-label="Поиск предложений"
            type="search"
            placeholder={mine ? 'Название, проблема или решение…' : 'Поиск по номеру или названию…'}
            maxLength={mine ? 200 : undefined}
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
                  (!allowedStatuses || allowedStatuses.includes(s)) &&
                  (mine || !employee || s !== 'Draft'),
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
      )}
      {boardDisabled ? (
        <Empty title="Общая доска отключена">
          <Link className="btn btn-line" to="/my-suggestions">
            Мои предложения
          </Link>
        </Empty>
      ) : query.isPending ? (
        <Loading />
      ) : query.error ? (
        <ErrorMessage error={query.error} />
      ) : mine && current > pages ? (
        <Empty title="На этой странице нет предложений">
          <button className="btn btn-light" onClick={() => update('page', '1')}>
            На первую страницу
          </button>
        </Empty>
      ) : !list.length ? (
        <Empty
          title={search || status ? 'Ничего не найдено' : 'В этом разделе пока нет предложений'}
        >
          <p>
            {search || status
              ? 'Попробуйте изменить поиск или фильтр.'
              : mine
                ? 'Здесь появятся ваши сохранённые черновики и отправленные предложения.'
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
          {employee && !mine ? (
            <div className="board">
              {list.map((s) => (
                <SuggestionCard suggestion={s} key={s.id} onOpen={() => openSuggestion(s.id)} />
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
                    onClick={() => openSuggestion(s.id)}
                  >
                    Открыть →
                  </button>
                </div>
              ))}
            </div>
          )}
          <nav className="pagination" aria-label="Страницы предложений">
            <span className="pagination-summary">
              Показано {(current - 1) * pageSize + 1}–{Math.min(current * pageSize, total)} из{' '}
              {total}
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
    </>
  )
}
