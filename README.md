# IdeaHub / KaizenBoard

Внутреннее приложение для предложений по улучшению предприятия. Backend: .NET 10, ASP.NET Core Minimal API, PostgreSQL, Clean Architecture/CQRS. Frontend: React, TypeScript, Vite.

## Локальный запуск

Нужны .NET SDK **10.0.402** (см. global.json), Docker Desktop и Node.js 24 LTS.

Из корня репозитория запустите базу:

```powershell
docker compose up -d
```

В отдельном терминале запустите API:

```powershell
dotnet run --project src/Web --launch-profile http
```

API: http://localhost:5000, документация: http://localhost:5000/scalar. При старте сервер применяет миграции и seed. Seed-администратор предназначен только для локальной разработки; параметры смотрите в ApplicationDbContextInitialiser.cs.

В другом терминале запустите фронтенд:

```powershell
cd frontend
npm ci
npm run dev
```

Приложение: http://127.0.0.1:5173. Запросы `/api` проксируются на `http://localhost:5000`. Браузер должен открывать фронтенд, а не API. Изменить адрес API можно через `frontend/.env` по примеру `.env.example`.

## Структура

- `frontend/src/app` — приложение, маршруты, layout, тема, стили макета.
- `frontend/src/features` — сценарии авторизации, предложений, справочников, аналитики.
- `frontend/src/shared` — типизированный API, runtime-проверка контрактов, общие компоненты и форматирование.
- `src/Domain` — бизнес-сущности и статусы.
- `src/Application` — команды и запросы.
- `src/Infrastructure` — EF Core, PostgreSQL и Identity.
- `src/Web` — HTTP API.
- `tests` — тесты бэкенда; `frontend/e2e` — браузерные тесты фронта.
- `docs/design/ideahub.html` — исходный HTML-макет.

Подробности архитектуры, развёртывания и проверок — в [frontend/README.md](frontend/README.md).

## Границы текущей интеграции

Фронт работает с имеющимися endpoint-ами и добавленным GET `/api/Users/me`. Он не содержит демо-входа или фальшивых записей. Полный сценарий макета требует развития API: подробной карточки, прав владельца, редактирования, комиссии, фото, голосов и уведомлений. Конкретный список для команды — [docs/BACKEND_HANDOFF.md](docs/BACKEND_HANDOFF.md).

Для проверки фронта без запущенного API доступны браузерные тесты с явно заданными ответами. Они не заменяют проверку с реальной БД и сервером.
