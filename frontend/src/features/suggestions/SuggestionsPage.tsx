import { Status } from './Status'
import { useMemo, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import type { Profile, Suggestion } from '../../shared/api/contracts'
import { statuses } from '../../shared/api/contracts'
import { isManager } from '../auth/session'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { formatDate } from '../../shared/lib/format'
import { statusConfig, useSuggestions } from './model'
import { SuggestionForm } from './SuggestionForm'
import { SuggestionDetail } from './SuggestionDetail'

export function SuggestionsPage() {
  const profile = useOutletContext<Profile>()
  const query = useSuggestions()
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(1)
  const [create, setCreate] = useState(false)
  const [selectedId, setSelectedId] = useState<string>()
  const filtered = useMemo(
    () =>
      (query.data ?? [])
        .filter(
          (s) =>
            (!status || s.status === status) &&
            s.title.toLocaleLowerCase('ru').includes(search.toLocaleLowerCase('ru')),
        )
        .sort((a, b) =>
          sort === 'title'
            ? a.title.localeCompare(b.title, 'ru')
            : (sort === 'newest' ? -1 : 1) *
              (new Date(a.created).getTime() - new Date(b.created).getTime()),
        ),
    [query.data, search, sort, status],
  )
  const pages = Math.max(1, Math.ceil(filtered.length / 10))
  const current = Math.min(page, pages)
  const selected: Suggestion | undefined = query.data?.find((s) => s.id === selectedId)
  return (
    <>
      <PageHead title="Предложения" subtitle="Идеи и их путь к изменениям.">
        <button className="btn btn-primary" onClick={() => setCreate(true)}>
          ＋ Предложить идею
        </button>
      </PageHead>
      <div className="toolbar">
        <input
          aria-label="Поиск предложений"
          type="search"
          placeholder="Найти идею…"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
        />
        <select
          aria-label="Статус предложения"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value)
            setPage(1)
          }}
        >
          <option value="">Все статусы</option>
          {statuses.map((s) => (
            <option key={s} value={s}>
              {statusConfig[s].label}
            </option>
          ))}
        </select>
        <select
          aria-label="Порядок предложений"
          value={sort}
          onChange={(e) => setSort(e.target.value)}
        >
          <option value="newest">Сначала новые</option>
          <option value="oldest">Сначала старые</option>
          <option value="title">По названию</option>
        </select>
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
        <Empty title={search || status ? 'Ничего не найдено' : 'Здесь появятся первые идеи'}>
          <p>
            {search || status
              ? 'Попробуйте изменить поиск или фильтр.'
              : 'Заметьте возможность для улучшения и предложите решение.'}
          </p>
        </Empty>
      ) : (
        <>
          <div className="proposal-table">
            <div className="list-table-head">
              <span>Предложение</span>
              <span>Создано</span>
              <span>Статус</span>
              <span />
            </div>
            {filtered.slice((current - 1) * 10, current * 10).map((s) => (
              <div className="suggestion-row" key={s.id}>
                <div>
                  <div className="idea-num">#{s.id.slice(0, 8)}</div>
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
          <nav className="pagination" aria-label="Страницы предложений">
            <span className="pagination-summary">Найдено: {filtered.length}</span>
            <div className="pagination-controls">
              <button
                className="btn btn-line"
                disabled={current === 1}
                onClick={() => setPage(current - 1)}
              >
                ← Назад
              </button>
              <span aria-live="polite">
                {current} / {pages}
              </span>
              <button
                className="btn btn-line"
                disabled={current === pages}
                onClick={() => setPage(current + 1)}
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
          manager={isManager(profile)}
          onClose={() => setSelectedId(undefined)}
        />
      )}
    </>
  )
}
