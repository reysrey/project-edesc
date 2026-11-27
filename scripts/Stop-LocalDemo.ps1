[CmdletBinding()]
param(
    [string]$PostgresBin,
    [ValidateRange(1024, 65535)][int]$ApiPort = 5501,
    [ValidateRange(1024, 65535)][int]$FrontendPort = 5175
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$frontendDirectory = Join-Path $repository 'frontend'
$webOutput = Join-Path $repository 'artifacts/bin/Web/demo'
$dataDirectory = Join-Path $repository 'artifacts/workflow-demo/pgdata'

function Test-DemoApiProcess($process) {
    $command = ([string]$process.CommandLine).Replace('/', '\')
    return ($process.Name -eq 'KaizenBoard.Web.exe' -and $process.ExecutablePath -eq (Join-Path $webOutput 'KaizenBoard.Web.exe')) -or
        ($process.Name -eq 'dotnet.exe' -and $command.IndexOf((Join-Path $webOutput 'KaizenBoard.Web.dll'), [StringComparison]::OrdinalIgnoreCase) -ge 0)
}

function Stop-DemoListener([int]$Port, [string]$Service, [scriptblock]$Matches) {
    $owner = Get-NetTCPConnection -LocalAddress '127.0.0.1' -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty OwningProcess
    if (-not $owner) {
        Write-Host "$Service is already stopped on port $Port."
        return
    }

    $process = Get-CimInstance Win32_Process -Filter "ProcessId=$owner"
    if (-not $process -or -not (& $Matches $process)) {
        Write-Warning "Port $Port is used by a process outside this demo (PID $owner)."
        return
    }
    Stop-Process -Id $owner
    Wait-Process -Id $owner -Timeout 10 -ErrorAction SilentlyContinue
    Write-Host "$Service stopped on port $Port."
}

Stop-DemoListener $FrontendPort 'Frontend' {
    param($process)
    $command = ([string]$process.CommandLine).Replace('/', '\')
    $process.Name -eq 'node.exe' -and $command -match '\bvite\.js\b' -and
        $command.IndexOf("$frontendDirectory\node_modules\", [StringComparison]::OrdinalIgnoreCase) -ge 0
}
Stop-DemoListener $ApiPort 'API' ${function:Test-DemoApiProcess}

if (-not (Test-Path -LiteralPath (Join-Path $dataDirectory 'postmaster.pid'))) {
    Write-Host 'Demo PostgreSQL is already stopped.'
    return
}

$otherApi = Get-CimInstance Win32_Process -Filter "Name = 'KaizenBoard.Web.exe' OR Name = 'dotnet.exe'" |
    Where-Object { Test-DemoApiProcess $_ } | Select-Object -First 1
if ($otherApi) {
    Write-Warning "Another demo API is still running (PID $($otherApi.ProcessId)). PostgreSQL is still running."
    return
}

if (-not $PostgresBin) {
    $pgCtlCommand = Get-Command pg_ctl.exe -ErrorAction SilentlyContinue
    if ($pgCtlCommand) {
        $PostgresBin = Split-Path -Parent $pgCtlCommand.Source
    } else {
        $PostgresBin = Join-Path (Split-Path -Parent $repository) 'ideahub-local/pgsql/bin'
    }
}
$pgCtl = Join-Path $PostgresBin 'pg_ctl.exe'
if (-not (Test-Path -LiteralPath $pgCtl -PathType Leaf)) {
    throw 'To stop PostgreSQL, pass -PostgresBin with the path to the PostgreSQL bin directory.'
}

& $pgCtl status -D $dataDirectory | Out-Null
if ($LASTEXITCODE -eq 3) {
    Write-Host 'Demo PostgreSQL is already stopped.'
    return
}
if ($LASTEXITCODE -ne 0) { throw 'Could not determine the demo PostgreSQL status.' }

& $pgCtl stop -D $dataDirectory -m fast -w
if ($LASTEXITCODE -ne 0) { throw 'Could not stop the demo PostgreSQL.' }
Write-Host "Demo PostgreSQL stopped. Data remains in $dataDirectory."
