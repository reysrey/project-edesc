import { Navigate, useOutletContext } from 'react-router-dom'
import type { Profile } from '../../shared/api/contracts'
import { statuses } from '../../shared/api/contracts'
import { isManager } from '../auth/session'
import { Empty, ErrorMessage, Loading, PageHead } from '../../shared/ui/Feedback'
import { csvCell, formatDate } from '../../shared/lib/format'
import { statusConfig, useSuggestions } from '../suggestions/model'

export function AnalyticsPage() {
  const manager = isManager(useOutletContext<Profile>())
  if (!manager) return <Navigate to="/" replace />
  return <AnalyticsContent />
}
function AnalyticsContent() {
  const query = useSuggestions()
  function exportCsv() {
    const rows = [
      ['ID', 'Название', 'Статус', 'Создано'],
      ...(query.data ?? []).map((s) => [
        s.id,
        s.title,
        statusConfig[s.status].label,
        formatDate(s.created),
      ]),
    ]
    const url = URL.createObjectURL(
      new Blob(['\ufeff' + rows.map((row) => row.map(csvCell).join(';')).join('\r\n')], {
        type: 'text/csv;charset=utf-8',
      }),
    )
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `ideahub-${new Date().toISOString().slice(0, 10)}.csv`
    anchor.click()
    setTimeout(() => URL.revokeObjectURL(url), 1000)
  }
  return (
    <>
      <PageHead
        title="Аналитика и экспорт"
        subtitle="Распределение доступных предложений по статусам."
      >
        <button
          className="btn btn-primary"
          disabled={!query.data?.length || !!query.error}
          onClick={exportCsv}
        >
          ↓ Скачать CSV
        </button>
      </PageHead>
      <button
        className="btn btn-line refresh-button"
        disabled={query.isFetching}
        onClick={() => void query.refetch()}
      >
        Обновить
      </button>
      {query.isPending ? (
        <Loading />
      ) : query.error ? (
        <ErrorMessage error={query.error} />
      ) : !query.data?.length ? (
        <Empty title="Пока недостаточно данных">
          <p>Аналитика появится вместе с первыми предложениями.</p>
        </Empty>
      ) : (
        <div className="panel analytics-panel">
          <h2>Путь идеи</h2>
          <p>Всего предложений: {query.data.length}</p>
          {statuses.map((status) => {
            const count = query.data.filter((s) => s.status === status).length
            return (
              <div className="bar-row" key={status}>
                <span>{statusConfig[status].label}</span>
                <div className="bar-track" aria-hidden="true">
                  <i style={{ width: `${(count / query.data.length) * 100}%` }} />
                </div>
                <b>{count}</b>
              </div>
            )
          })}
        </div>
      )}
    </>
  )
}
