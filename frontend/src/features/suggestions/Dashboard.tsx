import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useOutletContext } from 'react-router-dom'
import type { Profile, Template } from '../../shared/api/contracts'
import { api } from '../../shared/api/resources'
import { Empty, ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { Unavailable } from '../../shared/ui/Unavailable'
import { SuggestionForm } from './SuggestionForm'
import { SuggestionsPage } from './SuggestionsPage'

const directions = [
  ['Модернизация', '⚙', 'Оборудование, инструменты и новые способы работы'],
  ['Сокращение времени', '◷', 'Устранение лишних действий и ожидания'],
  ['Безопасность', '✳', 'Рабочая среда, риски и профилактика'],
  ['Качество', '◇', 'Стабильность результата и снижение брака'],
  ['Экономия ресурсов', '↘', 'Материалы, энергия и рациональное использование'],
  ['Другое', '＋', 'Любые идеи, которые делают работу лучше'],
] as const
export function Dashboard() {
  const profile = useOutletContext<Profile>()
  const templates = useQuery({
    queryKey: ['templates'],
    queryFn: ({ signal }) => api.templates(signal),
  })
  const [form, setForm] = useState<{ template?: Template; initialObject?: string }>()
  return (
    <>
      <section className="welcome">
        <div>
          <div className="eyebrow">ЛИЧНЫЙ КАБИНЕТ · СОТРУДНИК</div>
          <h1>
            {profile.fullName
              ? `Добрый день, ${profile.fullName.split(' ')[0]}!`
              : 'Хороший день для новой идеи'}
          </h1>
          <p>Ваши наблюдения могут сделать работу предприятия лучше.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setForm({})}>
          ＋ Предложить идею
        </button>
      </section>
      <section className="section" id="notifications">
        <div className="section-head">
          <div>
            <h2>Уведомления</h2>
            <p>Обратная связь по вашим предложениям.</p>
          </div>
        </div>
        <Unavailable>
          Уведомления пока недоступны. Изменения статуса можно посмотреть на доске предложений.
        </Unavailable>
      </section>
      <section className="section">
        <div className="section-head">
          <div>
            <h2>Картотека шаблонов и узких мест</h2>
            <p>Выберите направление — форма предложения уже будет подготовлена.</p>
          </div>
          <span className="aside">БЫСТРЫЙ СТАРТ ↗</span>
        </div>
        <div className="template-grid">
          {directions.map(([title, icon, description]) => (
            <button
              className="template"
              key={title}
              onClick={() => setForm({ initialObject: title })}
            >
              <span className="arrow">↗</span>
              <span className="emoji" aria-hidden="true">
                {icon}
              </span>
              <b>{title}</b>
              <span>{description}</span>
            </button>
          ))}
        </div>
        {templates.isPending ? (
          <Loading />
        ) : templates.error ? (
          <>
            <ErrorMessage error={templates.error} />
            <button className="btn btn-line" onClick={() => void templates.refetch()}>
              Повторить загрузку шаблонов
            </button>
          </>
        ) : (
          !!templates.data?.length && (
            <>
              <div className="section-head template-subheading">
                <div>
                  <h3>Шаблоны предприятия</h3>
                  <p>Подсказки для конкретных процессов и рабочих мест.</p>
                </div>
                <Link className="text-link" to="/templates">
                  Все шаблоны →
                </Link>
              </div>
              <div className="template-grid">
                {templates.data.map((t) => (
                  <button className="template" key={t.id} onClick={() => setForm({ template: t })}>
                    <span className="emoji" aria-hidden="true">
                      {t.type === 'Bottleneck' ? '◷' : '✦'}
                    </span>
                    <b>{t.title}</b>
                    <span>{t.improvementObject}</span>
                    <span className="arrow">↗</span>
                  </button>
                ))}
              </div>
            </>
          )
        )}
      </section>
      <section className="section" id="board">
        <SuggestionsPage embedded />
      </section>
      <section className="section" id="my-suggestions">
        <div className="section-head">
          <div>
            <h2>Мои предложения</h2>
            <p>Ваши идеи и обратная связь по ним.</p>
          </div>
        </div>
        <Empty title="Личный список пока недоступен">
          <p>
            Сейчас нельзя достоверно отделить ваши предложения от остальных. Отправленная идея
            появится на общей доске.
          </p>
        </Empty>
      </section>
      {form && (
        <SuggestionForm
          template={form.template}
          initialObject={form.initialObject}
          onClose={() => setForm(undefined)}
        />
      )}
    </>
  )
}
