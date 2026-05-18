param()

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root = Split-Path -Parent $ScriptDir
$RuntimeLogDir = Join-Path $Root "artifacts/runtime-logs"

$ApiUrl = "http://127.0.0.1:5157"
$PublicUrl = "http://127.0.0.1:3000"
$AdminUrl = "http://127.0.0.1:8000"

$ApiLog = Join-Path $RuntimeLogDir "api-run.out.log"
$ApiErr = Join-Path $RuntimeLogDir "api-run.err.log"
$PublicLog = Join-Path $RuntimeLogDir "public-run.out.log"
$PublicErr = Join-Path $RuntimeLogDir "public-run.err.log"
$AdminLog = Join-Path $RuntimeLogDir "admin-run.out.log"
$AdminErr = Join-Path $RuntimeLogDir "admin-run.err.log"

$env:NEXT_TELEMETRY_DISABLED = "1"
$env:BROWSER = "none"

function Write-Info {
    param([string]$Message)

    Write-Host "[CheapAI] $Message"
}

function Assert-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Missing command: $Name"
    }
}

function Assert-Path {
    param(
        [string]$Path,
        [string]$Hint
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing path: $Path`n$Hint"
    }
}

function Stop-CheapAiProcesses {
    $rootWithBackslashes = $Root.Replace("/", "\")
    $rootWithForwardSlashes = $Root.Replace("\", "/")
    $processes = Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -in @("CheapAI.Api.exe", "CheapAI.BackgroundJobs.exe", "dotnet.exe", "node.exe", "pnpm.exe") -and
            (
                (
                    $_.CommandLine -and (
                        $_.CommandLine.Contains($Root) -or
                        $_.CommandLine.Contains($rootWithBackslashes) -or
                        $_.CommandLine.Contains($rootWithForwardSlashes)
                    )
                ) -or
                (
                    $_.ExecutablePath -and (
                        $_.ExecutablePath.Contains($Root) -or
                        $_.ExecutablePath.Contains($rootWithBackslashes) -or
                        $_.ExecutablePath.Contains($rootWithForwardSlashes)
                    )
                )
            )
        }

    foreach ($process in $processes) {
        if ($process.ProcessId -eq $PID) {
            continue
        }

        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
            Write-Info "Stopped old process $($process.Name) ($($process.ProcessId))."
        }
        catch {
            Write-Warning "Failed to stop process $($process.Name) ($($process.ProcessId)): $($_.Exception.Message)"
        }
    }
}

function Start-LoggedProcess {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$ArgumentList,
        [string]$WorkingDirectory,
        [string]$OutputPath,
        [string]$ErrorPath
    )

    Remove-Item -LiteralPath $OutputPath, $ErrorPath -ErrorAction SilentlyContinue

    $process = Start-Process -FilePath $FilePath `
        -ArgumentList $ArgumentList `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $OutputPath `
        -RedirectStandardError $ErrorPath `
        -WindowStyle Hidden `
        -PassThru

    Write-Info "Started $Name, PID=$($process.Id)."
    return $process
}

function Wait-Http {
    param(
        [string]$Name,
        [string]$Url,
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Info "$Name is ready: $Url ($($response.StatusCode))."
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "$Name startup timeout: $Url"
}

Assert-Command "dotnet"
Assert-Command "pnpm"
Assert-Path (Join-Path $Root "src/frontend/web-public/node_modules") "Run: pnpm install --dir ""$Root/src/frontend/web-public"""
Assert-Path (Join-Path $Root "src/frontend/web-admin/node_modules") "Run: pnpm install --dir ""$Root/src/frontend/web-admin"""

New-Item -ItemType Directory -Force -Path $RuntimeLogDir | Out-Null

Write-Info "Cleaning old project processes."
Stop-CheapAiProcesses

Write-Info "Starting API."
$apiProcess = Start-LoggedProcess `
    -Name "API" `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--project", "$Root/src/backend/CheapAI.Api/CheapAI.Api.csproj", "--urls", $ApiUrl) `
    -WorkingDirectory "$Root/src/backend/CheapAI.Api" `
    -OutputPath $ApiLog `
    -ErrorPath $ApiErr

Wait-Http -Name "API" -Url "$ApiUrl/api/health"

Write-Info "Starting public web."
$publicProcess = Start-LoggedProcess `
    -Name "Public Web" `
    -FilePath "pnpm" `
    -ArgumentList @("--dir", "$Root/src/frontend/web-public", "exec", "next", "dev", "-p", "3000", "-H", "127.0.0.1") `
    -WorkingDirectory "$Root/src/frontend/web-public" `
    -OutputPath $PublicLog `
    -ErrorPath $PublicErr

Wait-Http -Name "Public Web" -Url $PublicUrl

Write-Info "Starting admin web."
$adminProcess = Start-LoggedProcess `
    -Name "Admin Web" `
    -FilePath "pnpm" `
    -ArgumentList @("--dir", "$Root/src/frontend/web-admin", "dev") `
    -WorkingDirectory "$Root/src/frontend/web-admin" `
    -OutputPath $AdminLog `
    -ErrorPath $AdminErr

Wait-Http -Name "Admin Web" -Url $AdminUrl

Write-Host ""
Write-Host "CheapAI is up."
Write-Host "API: $ApiUrl"
Write-Host "Public: $PublicUrl"
Write-Host "Admin: $AdminUrl"
Write-Host "Admin user: admin"
Write-Host "Admin password: ChangeMe123!"
Write-Host "Logs: $RuntimeLogDir"
Write-Host "API PID: $($apiProcess.Id)"
Write-Host "Public PID: $($publicProcess.Id)"
Write-Host "Admin PID: $($adminProcess.Id)"
