import { Status } from './Status'
import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useOutletContext } from 'react-router-dom'
import type { Profile, Template } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { Empty, ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { formatDate } from '../../shared/lib/format'
import { useSuggestions } from './model'
import { SuggestionForm } from './SuggestionForm'

export function Dashboard() {
  const profile = useOutletContext<Profile>()
  const suggestions = useSuggestions()
  const templates = useQuery({ queryKey: ['templates'], queryFn: ({ signal }) => api.templates(signal) })
  const [form, setForm] = useState<{ template?: Template }>()
  const data = suggestions.data ?? []
  const recent = [...data].sort((a, b) => new Date(b.created).getTime() - new Date(a.created).getTime()).slice(0, 4)
  return <><section className="welcome"><div><div className="eyebrow">КАЖДАЯ ИДЕЯ ИМЕЕТ ЗНАЧЕНИЕ</div><h1>{profile.fullName ? `Здравствуйте, ${profile.fullName.split(' ')[0]}!` : 'Хороший день для новой идеи'}</h1><p>Замечайте возможности. Предлагайте решения. Меняйте работу к лучшему.</p></div><button className="btn btn-primary" onClick={() => setForm({})}>＋ Предложить идею</button></section>
    {suggestions.isPending ? <Loading /> : suggestions.error ? <><ErrorMessage error={suggestions.error} /><button className="btn btn-line" onClick={() => void suggestions.refetch()}>Повторить</button></> : <div className="stats dashboard-stats">{[['Всего предложений', data.length], ['У руководителя', data.filter(s => s.status === 'Submitted').length], ['На комиссии', data.filter(s => s.status === 'InCommission').length], ['Внедрено', data.filter(s => s.status === 'Implemented').length]].map(([label, count]) => <div className="stat" key={label}><span>{label}</span><strong>{count}</strong></div>)}</div>}
    <section className="section"><div className="section-head"><div><h2>С чего начнём?</h2><p>Выберите шаблон — он поможет сформулировать идею.</p></div><Link to="/templates" className="text-link">Все шаблоны →</Link></div>{templates.isPending ? <Loading /> : templates.error ? <><ErrorMessage error={templates.error} /><button className="btn btn-line" onClick={() => void templates.refetch()}>Повторить</button></> : !templates.data?.length ? <Empty title="Шаблоны пока не добавлены"><p>Вы можете предложить идею с чистого листа.</p></Empty> : <div className="template-grid">{templates.data.slice(0, 3).map(t => <button key={t.id} className="template" onClick={() => setForm({ template: t })}><span className="emoji" aria-hidden="true">{t.type === 'Bottleneck' ? '◷' : '✦'}</span><b>{t.title}</b><span>{t.improvementObject}</span><span className="arrow">↗</span></button>)}</div>}</section>
    <section className="section"><div className="section-head"><div><h2>Последние предложения</h2><p>Новые шаги к большим изменениям.</p></div><Link to="/suggestions" className="text-link">Все предложения →</Link></div>{!suggestions.isPending && !suggestions.error && (recent.length ? <div className="board">{recent.map(s => <article key={s.id} className="idea"><div className="idea-top"><span className="idea-num">#{s.id.slice(0, 8)}</span><Status status={s.status} /></div><h3>{s.title}</h3><div className="idea-footer"><span className="idea-author">{formatDate(s.created)}</span><Link className="text-link" to="/suggestions">К предложениям →</Link></div></article>)}</div> : <Empty title="Первая идея может быть вашей"><p>Поделитесь тем, что поможет вашему предприятию.</p></Empty>)}</section>
    {form && <SuggestionForm template={form.template} onClose={() => setForm(undefined)} />}
  </>
}

