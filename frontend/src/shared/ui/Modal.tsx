import { useEffect, useId, useRef } from 'react'
import type { MouseEvent, ReactNode } from 'react'

function isBackdrop(event: MouseEvent<HTMLDialogElement>) {
  if (event.target !== event.currentTarget) return false
  const { left, right, top, bottom } = event.currentTarget.getBoundingClientRect()
  return (
    event.clientX < left || event.clientX > right || event.clientY < top || event.clientY > bottom
  )
}

export function Modal({
  title,
  children,
  onClose,
  busy = false,
}: {
  title: string
  children: ReactNode
  onClose: () => void
  busy?: boolean
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const pressedBackdrop = useRef(false)
  const titleId = useId()
  useEffect(() => {
    const element = dialog.current!
    const previous = document.activeElement as HTMLElement | null
    const scrollPosition = { top: window.scrollY, left: window.scrollX }
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    element.showModal()
    return () => {
      element.close()
      document.body.style.overflow = overflow
      const target =
        previous?.isConnected && previous !== document.body
          ? previous
          : document.getElementById('main')
      target?.focus({ preventScroll: true })
      window.scrollTo({ ...scrollPosition, behavior: 'instant' })
    }
  }, [])
  return (
    <dialog
      ref={dialog}
      className="modal app-dialog"
      aria-labelledby={titleId}
      onCancel={(e) => {
        e.preventDefault()
        if (!busy) onClose()
      }}
      onPointerDown={(e) => {
        pressedBackdrop.current = e.isPrimary && e.button === 0 && isBackdrop(e)
      }}
      onPointerCancel={() => {
        pressedBackdrop.current = false
      }}
      onClick={(e) => {
        const startedOnBackdrop = pressedBackdrop.current
        pressedBackdrop.current = false
        if (!busy && startedOnBackdrop && isBackdrop(e)) onClose()
      }}
    >
      <div className="modal-head">
        <h2 id={titleId}>{title}</h2>
        <button
          className="close"
          type="button"
          aria-label="Закрыть"
          disabled={busy}
          onClick={onClose}
        >
          ×
        </button>
      </div>
      {children}
    </dialog>
  )
}
