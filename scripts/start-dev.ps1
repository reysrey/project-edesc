# Запуск всех серверов разработки одной командой:
#   .\scripts\start-dev.ps1
# Откроются два окна: бэкенд (порт 5000) и Vite (порт 5173).
# Браузер: http://127.0.0.1:5173
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# 1) PostgreSQL в Docker (контейнер сам перезапускается после перезагрузки ПК)
$db = docker ps --filter "name=kaizenboard-db" --format "{{.Names}}"
if ($db -ne 'kaizenboard-db') {
    Write-Host 'PostgreSQL не запущен, поднимаю: docker compose up -d' -ForegroundColor Yellow
    Push-Location $root
    docker compose up -d
    Pop-Location
}

# 2) Бэкенд: http://localhost:5000 (профиль http — без HTTPS-редиректа, который ломает dev)
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "Set-Location '$root'; dotnet run --project backend/src/Web --launch-profile http"

# 3) Фронтенд: Vite раздаёт React и проксирует /api на бэкенд
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "Set-Location '$root\frontend'; npm run dev"

Write-Host 'Серверы запускаются в отдельных окнах. Страница: http://127.0.0.1:5173' -ForegroundColor Green
