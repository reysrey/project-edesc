import { useState } from 'react'

export function Brand() { return <div className="brand"><div className="brandmark" aria-hidden="true">✳</div><span>idea<span className="brand-light">hub</span></span></div> }
export function ThemeToggle() {
  const [dark, setDark] = useState(document.documentElement.dataset.theme === 'dark')
  return <button type="button" className="theme-toggle" aria-label={dark ? 'Включить светлую тему' : 'Включить тёмную тему'} onClick={() => {
    const next = !dark
    setDark(next); document.documentElement.dataset.theme = next ? 'dark' : 'light'
    try { localStorage.setItem('ideahub-theme', next ? 'dark' : 'light') } catch { /* Theme still works when storage is disabled. */ }
  }}>{dark ? '☀' : '☾'}</button>
}
