[CmdletBinding()]
param(
    [string]$PostgresBin,
    [switch]$WithFrontend,
    [ValidateRange(1024, 65535)][int]$ApiPort = 5501,
    [ValidateRange(1024, 65535)][int]$FrontendPort = 5175
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$demoRoot = Join-Path $repository 'artifacts/workflow-demo'
$apiUrl = "http://127.0.0.1:$ApiPort"
$frontendUrl = "http://127.0.0.1:$FrontendPort"
$dbPort = 55441
if ($ApiPort -eq $FrontendPort -or $ApiPort -eq $dbPort -or $FrontendPort -eq $dbPort) {
    throw 'The API, frontend and PostgreSQL must use different ports.'
}

function Get-PortOwner([int]$Port) {
    Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty OwningProcess
}

function Test-DemoApi {
    try {
        $spec = Invoke-RestMethod -UseBasicParsing -Uri "$apiUrl/openapi/v1.json" -TimeoutSec 2
        return $spec.info.title -eq 'KaizenBoard.Web | v1' -and
            $null -ne $spec.paths.PSObject.Properties['/api/ServiceSettings']
    }
    catch { return $false }
}

function Test-DemoFrontend {
    try {
        $page = Invoke-WebRequest -UseBasicParsing -Uri $frontendUrl -TimeoutSec 2
        return $page.Content.Contains('<title>IdeaHub') -and $page.Content.Contains('/@vite/client')
    }
    catch { return $false }
}

function Test-FrontendProxy {
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$frontendUrl/api/Users/me" -TimeoutSec 2 | Out-Null
        return $false
    }
    catch { return [int]$_.Exception.Response.StatusCode -eq 401 }
}

function Wait-ForDemo([scriptblock]$IsReady, [System.Diagnostics.Process]$Process, [string]$Log) {
    $deadline = (Get-Date).AddMinutes(2)
    do {
        if (& $IsReady) { return }
        if ($Process.HasExited) { break }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "Startup failed or timed out. See $Log and $Log.stdout."
}

$apiOwner = Get-PortOwner $ApiPort
if ($apiOwner -and -not (Test-DemoApi)) {
    throw "API port $ApiPort is used by process $apiOwner. Use -ApiPort with a free port."
}

if ($WithFrontend) {
    $frontendOwner = Get-PortOwner $FrontendPort
    if ($frontendOwner -and -not (Test-DemoFrontend)) {
        throw "Frontend port $FrontendPort is used by process $frontendOwner. Use -FrontendPort with a free port."
    }
    if ($apiOwner -and $frontendOwner -and (Test-FrontendProxy)) {
        Write-Host "IdeaHub is already running: $frontendUrl"
        return
    }

    if (-not (Test-Path -LiteralPath $demoRoot)) {
        New-Item -ItemType Directory -Path $demoRoot | Out-Null
    }
    if (-not $apiOwner) {
        $apiLog = Join-Path $demoRoot "api-$ApiPort.log"
        $apiArguments = @('-NoProfile', '-File', ('"{0}"' -f $PSCommandPath),
            '-ApiPort', $ApiPort, '-FrontendPort', $FrontendPort)
        if ($PostgresBin) { $apiArguments += @('-PostgresBin', ('"{0}"' -f $PostgresBin)) }
        Write-Host "Starting PostgreSQL and API: $apiUrl"
        $apiProcess = Start-Process -FilePath 'powershell.exe' -ArgumentList $apiArguments -WorkingDirectory $repository -WindowStyle Hidden -RedirectStandardError $apiLog -RedirectStandardOutput "$apiLog.stdout" -PassThru
        Wait-ForDemo ${function:Test-DemoApi} $apiProcess $apiLog
    }

    if (-not $frontendOwner) {
        $frontendDirectory = Join-Path $repository 'frontend'
        $vite = Join-Path $frontendDirectory 'node_modules/vite/bin/vite.js'
        if (-not (Test-Path -LiteralPath $vite)) {
            Push-Location $frontendDirectory
            try {
                & npm.cmd ci
                if ($LASTEXITCODE -ne 0) { throw 'Frontend dependency installation failed.' }
            }
            finally { Pop-Location }
        }
        $frontendLog = Join-Path $demoRoot "frontend-$FrontendPort.log"
        $oldProxyTarget = $env:API_PROXY_TARGET
        try {
            $env:API_PROXY_TARGET = $apiUrl
            Write-Host "Starting frontend: $frontendUrl"
            $frontendProcess = Start-Process -FilePath 'node.exe' -ArgumentList @(('"{0}"' -f $vite), '--port', $FrontendPort) -WorkingDirectory $frontendDirectory -WindowStyle Hidden -RedirectStandardError $frontendLog -RedirectStandardOutput "$frontendLog.stdout" -PassThru
        }
        finally { $env:API_PROXY_TARGET = $oldProxyTarget }
        Wait-ForDemo ${function:Test-DemoFrontend} $frontendProcess $frontendLog
    }
    if (-not (Test-FrontendProxy)) {
        throw "The frontend on port $FrontendPort does not reach the API. Use a free -FrontendPort together with the intended -ApiPort."
    }
    Write-Host "IdeaHub is ready: $frontendUrl"
    return
}

if ($apiOwner) {
    Write-Host "IdeaHub API is already running: $apiUrl"
    return
}

if (-not $PostgresBin) {
    $pgCtlCommand = Get-Command pg_ctl.exe -ErrorAction SilentlyContinue
    if (-not $pgCtlCommand) {
        throw 'Pass -PostgresBin with the path to the PostgreSQL bin directory.'
    }
    $PostgresBin = Split-Path -Parent $pgCtlCommand.Source
}

$pgCtl = Join-Path $PostgresBin 'pg_ctl.exe'
$initDb = Join-Path $PostgresBin 'initdb.exe'
$createDb = Join-Path $PostgresBin 'createdb.exe'
$psql = Join-Path $PostgresBin 'psql.exe'
foreach ($command in @($pgCtl, $initDb, $createDb, $psql)) {
    if (-not (Test-Path -LiteralPath $command -PathType Leaf)) {
        throw "PostgreSQL executable not found: $command"
    }
}

$dataDirectory = Join-Path $demoRoot 'pgdata'
$passwordFile = Join-Path $demoRoot 'db-password.txt'
$initialized = Test-Path -LiteralPath (Join-Path $dataDirectory 'PG_VERSION')
$dbUser = 'kaizen_demo'
$dbName = 'ideahub_demo'
if (-not (Test-Path -LiteralPath $demoRoot)) {
    New-Item -ItemType Directory -Path $demoRoot | Out-Null
}
if (-not (Test-Path -LiteralPath $passwordFile)) {
    if ($initialized) {
        throw "The existing demo database needs its original password file: $passwordFile"
    }
    Set-Content -LiteralPath $passwordFile -Value ([Guid]::NewGuid().ToString('N')) -Encoding ASCII
}
$password = (Get-Content -LiteralPath $passwordFile -Raw).Trim()
if (-not $initialized) {
    & $initDb -D $dataDirectory -U $dbUser --auth=scram-sha-256 "--pwfile=$passwordFile" -E UTF8 --no-locale
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL initialization failed.' }
}

& $pgCtl status -D $dataDirectory | Out-Null
if ($LASTEXITCODE -ne 0) {
    $pgLog = Join-Path $demoRoot 'postgres.log'
    & $pgCtl start -D $dataDirectory -l $pgLog -o "-h 127.0.0.1 -p $dbPort" -w
    if ($LASTEXITCODE -ne 0) { throw "Could not start PostgreSQL. See $pgLog." }
}

$oldPgPassword = $env:PGPASSWORD
try {
    $env:PGPASSWORD = $password
    $databaseExists = & $psql -w -h 127.0.0.1 -p $dbPort -U $dbUser -d postgres -Atc "SELECT 1 FROM pg_database WHERE datname = '$dbName';"
    if ($LASTEXITCODE -ne 0) { throw 'Could not connect to the local demo PostgreSQL.' }
    if ($databaseExists -ne '1') {
        & $createDb -w -h 127.0.0.1 -p $dbPort -U $dbUser $dbName
        if ($LASTEXITCODE -ne 0) { throw 'Could not create the demo database.' }
    }
}
finally {
    $env:PGPASSWORD = $oldPgPassword
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__KaizenBoardDb = "Host=127.0.0.1;Port=$dbPort;Database=$dbName;Username=$dbUser;Password=$password"
$env:Cors__AllowedOrigins__0 = $frontendUrl
$webProject = Join-Path $repository 'backend/src/Web/Web.csproj'
& dotnet run --project $webProject -c Demo --no-launch-profile -- --urls $apiUrl
if ($LASTEXITCODE -ne 0) { throw "The demo API exited with code $LASTEXITCODE. See the error above." }
