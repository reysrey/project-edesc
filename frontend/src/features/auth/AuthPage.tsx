import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import { api } from '../../shared/api/resources'
import { ApiError } from '../../shared/api/client'
import { ErrorMessage, Loading } from '../../shared/ui/Feedback'
import { Brand, ThemeToggle } from '../../app/Theme'
import { sessionKey, useSession } from './session'

type Mode = 'login' | 'register' | 'forgot' | 'reset'
export function AuthPage() {
  const [params] = useSearchParams()
  const [mode, setMode] = useState<Mode>(params.has('resetCode') ? 'reset' : 'login')
  const [email, setEmail] = useState(params.get('email') ?? '')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [code, setCode] = useState(params.get('resetCode') ?? '')
  const [visible, setVisible] = useState(false)
  const [notice, setNotice] = useState('')
  const [validation, setValidation] = useState('')
  const session = useSession()
  const client = useQueryClient()
  const navigate = useNavigate()
  const mutation = useMutation({
    mutationFn: async () => {
      if (mode === 'register') {
        await api.register(email.trim(), password)
        return
      }
      if (mode === 'forgot') {
        await api.forgotPassword(email.trim())
        return
      }
      if (mode === 'reset') {
        await api.resetPassword(email.trim(), code, password)
        return
      }
      await api.login(email.trim(), password)
      return await api.profile()
    },
    onSuccess: (profile) => {
      setPassword('')
      setConfirm('')
      if (profile) {
        client.setQueryData(sessionKey, profile)
        navigate('/', { replace: true })
      } else {
        setNotice(
          mode === 'register'
            ? 'Аккаунт создан. Теперь войдите с вашей почтой и паролем.'
            : mode === 'reset'
              ? 'Пароль изменён. Войдите с новым паролем.'
              : 'Если адрес зарегистрирован, сервер отправит инструкцию по восстановлению.',
        )
        setMode('login')
      }
    },
  })
  if (session.isPending) return <Loading />
  if (session.data) return <Navigate to="/" replace />
  function switchMode(next: Mode) {
    setMode(next)
    setNotice('')
    setValidation('')
    mutation.reset()
  }
  return (
    <div className="login">
      <section className="login-art">
        <Brand />
        <div className="art-copy">
          <div className="script">Большие перемены начинаются с идеи</div>
          <h1>
            Замечай.
            <br />
            Предлагай.
            <br />
            <span className="brand-text">Улучшай.</span>
          </h1>
          <p>
            Единое пространство, где идеи сотрудников превращаются в реальные изменения и измеримый
            результат.
          </p>
        </div>
        <div className="art-note">IDEAHUB · CULTURE OF IMPROVEMENT</div>
      </section>
      <section className="login-panel">
        <div className="login-box">
          <div className="eyebrow">РАБОЧЕЕ ПРОСТРАНСТВО</div>
          <h2>
            {
              {
                login: 'С возвращением',
                register: 'Начните с идеи',
                forgot: 'Восстановление доступа',
                reset: 'Новый пароль',
              }[mode]
            }
          </h2>
          <p>
            {mode === 'login'
              ? 'Войдите, чтобы поделиться идеей или посмотреть, как меняется ваше предприятие.'
              : mode === 'register'
                ? 'Создайте аккаунт с вашей рабочей почтой.'
                : 'Укажите рабочую почту, чтобы восстановить доступ.'}
          </p>
          {notice && (
            <p className="success-message" role="status">
              {notice}
            </p>
          )}
          {session.error &&
            !(session.error instanceof ApiError && session.error.status === 401) && (
              <ErrorMessage error={session.error} />
            )}
          <form
            onSubmit={(e) => {
              e.preventDefault()
              setValidation('')
              if ((mode === 'register' || mode === 'reset') && password !== confirm) {
                setValidation('Пароли не совпадают.')
                return
              }
              mutation.mutate()
            }}
          >
            <fieldset disabled={mutation.isPending}>
              <div className="field">
                <label htmlFor="email">Рабочая почта</label>
                <input
                  id="email"
                  type="email"
                  autoComplete="username"
                  placeholder="name@company.ru"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                />
              </div>
              {mode === 'reset' && (
                <div className="field">
                  <label htmlFor="code">Код восстановления из письма</label>
                  <input
                    id="code"
                    required
                    value={code}
                    onChange={(e) => setCode(e.target.value)}
                  />
                </div>
              )}
              {mode !== 'forgot' && (
                <>
                  <div className="field">
                    <label htmlFor="password">{mode === 'reset' ? 'Новый пароль' : 'Пароль'}</label>
                    <input
                      id="password"
                      type={visible ? 'text' : 'password'}
                      autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      required
                      minLength={mode === 'login' ? undefined : 6}
                    />
                    <button
                      type="button"
                      className="password-toggle"
                      onClick={() => setVisible(!visible)}
                    >
                      {visible ? 'Скрыть пароль' : 'Показать пароль'}
                    </button>
                  </div>
                  {mode !== 'login' && (
                    <>
                      <div className="field">
                        <label htmlFor="confirm">Повторите пароль</label>
                        <input
                          id="confirm"
                          type="password"
                          autoComplete="new-password"
                          required
                          value={confirm}
                          onChange={(e) => setConfirm(e.target.value)}
                        />
                      </div>
                      <p className="file-hint">
                        Не менее 6 символов: заглавная и строчная латинские буквы, цифра и
                        специальный символ.
                      </p>
                    </>
                  )}
                </>
              )}
              <ErrorMessage
                error={
                  validation
                    ? new Error(validation)
                    : mutation.error instanceof ApiError && mutation.error.status === 401
                      ? new Error('Неверная почта или пароль.')
                      : mutation.error
                }
              />
              <button className="btn btn-primary btn-wide" type="submit">
                {mutation.isPending
                  ? 'Подождите…'
                  : {
                      login: 'Войти в IdeaHub →',
                      register: 'Создать аккаунт →',
                      forgot: 'Получить инструкцию',
                      reset: 'Сохранить пароль',
                    }[mode]}
              </button>
            </fieldset>
          </form>
          <div className="login-foot">
            {mode === 'login' ? (
              <>
                Впервые здесь?{' '}
                <button className="login-switch" onClick={() => switchMode('register')}>
                  Создать аккаунт
                </button>
                <button className="forgot-link" onClick={() => switchMode('forgot')}>
                  Забыли пароль?
                </button>
                <button className="login-switch" onClick={() => switchMode('reset')}>
                  Уже есть код восстановления
                </button>
              </>
            ) : (
              <button className="login-switch" onClick={() => switchMode('login')}>
                ← Вернуться ко входу
              </button>
            )}
          </div>
        </div>
      </section>
      <div className="theme-corner">
        <ThemeToggle />
      </div>
    </div>
  )
}
