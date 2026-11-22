import type { ReactNode } from 'react'

export function Unavailable({ children }: { children: ReactNode }) {
  return <p className="preview-form-note availability-note">{children}</p>
}
