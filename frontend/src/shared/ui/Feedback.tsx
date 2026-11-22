import type { ReactNode } from 'react'

export function ErrorMessage({ error }: { error: unknown }) {
  return error ? (
    <div className="error-message" role="alert">
      {error instanceof Error ? error.message : 'Не удалось выполнить действие.'}
    </div>
  ) : null
}
export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <div className="empty-icon" aria-hidden="true">
        ✳
      </div>
      <h3>{title}</h3>
      {children}
    </div>
  )
}
export function Loading() {
  return (
    <div role="status" className="loading-state">
      <span className="skeleton" />
      <span className="skeleton" />
      <span className="skeleton" />
      <p>Загружаем данные…</p>
    </div>
  )
}
export function PageHead({
  title,
  subtitle,
  children,
}: {
  title: string
  subtitle: string
  children?: ReactNode
}) {
  return (
    <div className="admin-head">
      <div>
        <div className="eyebrow">ПРОСТРАНСТВО УЛУЧШЕНИЙ</div>
        <h1>{title}</h1>
        <p>{subtitle}</p>
      </div>
      {children}
    </div>
  )
}
