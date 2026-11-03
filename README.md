# KaizenBoard

Backend-сервис для подачи и обработки предложений по улучшению (ППУ).
Построен на .NET 10 (ASP.NET Core Minimal API) и PostgreSQL с использованием паттернов Clean Architecture и CQRS.

## Что нужно установить

Чтобы запустить проект локально, вам понадобятся:

1. **Docker Desktop** — для запуска базы данных в изолированном контейнере (не требует установки PostgreSQL на компьютер).
   * Скачать: https://www.docker.com/products/docker-desktop/
2. **.NET 10 SDK** — для сборки и запуска C#-кода.
   * Скачать: https://dotnet.microsoft.com/download/dotnet/10.0
3. **Среда разработки (IDE)** — на выбор:
   * Visual Studio 2022 (Рекомендуется): https://visualstudio.microsoft.com/
   * Visual Studio Code: https://code.visualstudio.com/ (потребуется расширение C# Dev Kit)


## Как запустить проект (Getting Started)

### 1. Запуск базы данных
Откройте терминал в корневой папке решения (где лежит файл `docker-compose.yml`) и выполните команду:
```bash
docker compose up -d  
## Либо в vs тоже самое в терминале, это позволит работать с бд по идее у каждого она своя пока что будет

Запуск сервера (API)
Если вы используете Visual Studio: откройте файл KaizenBoard.slnx и нажмите F5 (или зеленую кнопку «Пуск»).

Если вы используете VS Code / консоль: выполните команду:

bash
dotnet run --project src/Web

Структура решения
src/Domain — Бизнес-сущности, правила и Enum'ы (Suggestion, Department, Template).

src/Application — Бизнес-логика, сценарии (CQRS/MediatR Commands и Queries).

src/Infrastructure — Работа с базой данных (EF Core), миграции, настройки Identity.

src/Web — Слой API (эндпоинты, обработка запросов).

tests/ — Модульные и интеграционные тесты.