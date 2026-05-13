$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Stage = "smoke"
$SkipScreenshots = $false
$AllowedStages = @("smoke", "p0", "p1", "p2", "all")

for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq "-Stage" -and $i + 1 -lt $args.Count) {
        $Stage = $args[$i + 1]
    }
    elseif ($args[$i] -in $AllowedStages) {
        $Stage = $args[$i]
    }
    elseif ($args[$i] -eq "-SkipScreenshots") {
        $SkipScreenshots = $true
    }
}

if ($Stage -notin $AllowedStages) {
    throw "Invalid stage '$Stage'. Allowed values: $($AllowedStages -join ', ')"
}

$ConfigPath = Join-Path $ScriptDir "loop.config.json"
$Config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$Root = $Config.root
$Timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$RunDir = Join-Path $Root "artifacts/loop/$Timestamp"
$ScreenshotDir = Join-Path $Root "artifacts/screenshots/$Stage/$Timestamp"
$RuntimeLogDir = Join-Path $Root "artifacts/runtime-logs"
$ApiLog = Join-Path $RuntimeLogDir "api-run.out.log"
$ApiErr = Join-Path $RuntimeLogDir "api-run.err.log"
$PublicLog = Join-Path $RuntimeLogDir "public-run.out.log"
$PublicErr = Join-Path $RuntimeLogDir "public-run.err.log"
$AdminLog = Join-Path $RuntimeLogDir "admin-run.out.log"
$AdminErr = Join-Path $RuntimeLogDir "admin-run.err.log"
$Report = Join-Path $RunDir "report.md"

New-Item -ItemType Directory -Force -Path $RunDir, $ScreenshotDir, $RuntimeLogDir | Out-Null
Set-Location -LiteralPath $Root
[Environment]::CurrentDirectory = (Get-Location).Path

# Next.js on Windows can crash its build worker with access violation in this repo.
# Keeping the build in the main process makes loop validation deterministic.
$env:NEXT_TELEMETRY_DISABLED = "1"
$env:NEXT_PRIVATE_BUILD_WORKER = "1"

$Results = [System.Collections.Generic.List[string]]::new()

function Add-Result {
    param([string]$Name, [string]$Status, [string]$Detail = "")
    $line = "| $Name | $Status | $Detail |"
    $Results.Add($line)
    Write-Host "[$Status] $Name $Detail"
}

function Invoke-Checked {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$Command,
        [string[]]$Arguments
    )

    Push-Location -LiteralPath $WorkingDirectory
    try {
        [Environment]::CurrentDirectory = (Get-Location).Path
        & $Command @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "$Command exited with $LASTEXITCODE"
        }
        Add-Result $Name "PASS"
    }
    catch {
        Add-Result $Name "FAIL" ($_.Exception.Message -replace "\|", "/")
        throw
    }
    finally {
        Pop-Location
    }
}

function Invoke-CheckedWithRetry {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$Command,
        [string[]]$Arguments,
        [int]$MaxAttempts = 2
    )

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        Push-Location -LiteralPath $WorkingDirectory
        try {
            [Environment]::CurrentDirectory = (Get-Location).Path
            & $Command @Arguments
            if ($LASTEXITCODE -ne 0) {
                throw "$Command exited with $LASTEXITCODE"
            }
            Add-Result $Name "PASS"
            return
        }
        catch {
            $message = $_.Exception.Message
            $isNodeCrash = $message.Contains("-1073741819") -or
                $message.Contains("3221225477") -or
                $message.Contains("-1073741795") -or
                $message.Contains("3221225501")
            if ($attempt -lt $MaxAttempts -and $isNodeCrash) {
                Add-Result "$Name-retry-$attempt" "WARN" "Node build process crashed; retrying once"
                Start-Sleep -Seconds 2
            }
            else {
                Add-Result $Name "FAIL" ($message -replace "\|", "/")
                throw
            }
        }
        finally {
            Pop-Location
        }
    }
}

function Stop-CheapAiProcesses {
    $escapedRoot = $Root.Replace("/", "\")
    $processes = Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -in @("CheapAI.Api.exe", "CheapAI.BackgroundJobs.exe", "dotnet.exe", "node.exe") -and
            (
                ($_.CommandLine -and ($_.CommandLine.Contains($Root) -or $_.CommandLine.Contains($escapedRoot))) -or
                ($_.ExecutablePath -and ($_.ExecutablePath.Contains($Root) -or $_.ExecutablePath.Contains($escapedRoot)))
            )
        }

    foreach ($process in $processes) {
        try {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop
            Add-Result "stop-process-$($process.ProcessId)" "PASS" $process.Name
        }
        catch {
            Add-Result "stop-process-$($process.ProcessId)" "WARN" ($_.Exception.Message -replace "\|", "/")
        }
    }
}

function Wait-Http {
    param([string]$Name, [string]$Url, [int]$TimeoutSeconds = 60)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Add-Result $Name "PASS" "$Url -> $($response.StatusCode)"
                return
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    Add-Result $Name "FAIL" "$Url timeout"
    throw "$Url timeout"
}

function Start-PreviewProcesses {
    Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "$Root/src/backend/CheapAI.Api/CheapAI.Api.csproj", "--urls", $Config.apiUrl) `
        -WorkingDirectory "$Root/src/backend/CheapAI.Api" `
        -RedirectStandardOutput $ApiLog `
        -RedirectStandardError $ApiErr `
        -WindowStyle Hidden

    Start-Process -FilePath "pnpm" `
        -ArgumentList @("--dir", "$Root/src/frontend/web-public", "start", "-p", "3000") `
        -WorkingDirectory "$Root/src/frontend/web-public" `
        -RedirectStandardOutput $PublicLog `
        -RedirectStandardError $PublicErr `
        -WindowStyle Hidden

    Start-Process -FilePath "pnpm" `
        -ArgumentList @("dlx", "serve", "$Root/src/frontend/web-admin/dist", "-l", "8000", "-s") `
        -WorkingDirectory $Root `
        -RedirectStandardOutput $AdminLog `
        -RedirectStandardError $AdminErr `
        -WindowStyle Hidden

    Wait-Http "api-ready" "$($Config.apiUrl)/api/health"
    Wait-Http "public-ready" $Config.publicUrl
    Wait-Http "admin-ready" $Config.adminUrl
}

function Invoke-SmokeChecks {
    $loginBody = @{
        username = $Config.seed.adminUsername
        password = $Config.seed.adminPassword
    } | ConvertTo-Json

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/health" -UseBasicParsing | Out-Null
    Add-Result "api-health" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/home/overview" -UseBasicParsing | Out-Null
    Add-Result "public-home-overview" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/rankings/models/$($Config.seed.modelSlug)?rankingType=price&window=7d&page=1&pageSize=20" -UseBasicParsing | Out-Null
    Add-Result "public-model-ranking" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/sites/$($Config.seed.siteSlug)" -UseBasicParsing | Out-Null
    Add-Result "public-site-detail" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/tests/latest?page=1&pageSize=10" -UseBasicParsing | Out-Null
    Add-Result "public-tests-latest" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/sites?page=1&pageSize=20" -UseBasicParsing | Out-Null
    Add-Result "public-sites-list" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/models?page=1&pageSize=20" -UseBasicParsing | Out-Null
    Add-Result "public-models-list" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/rankings/cheapest?modelSlugs=$($Config.seed.modelSlug)&limit=5" -UseBasicParsing | Out-Null
    Add-Result "public-cheapest-rankings" "PASS"

    $loginResponse = Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/auth/login" -Method Post -ContentType "application/json" -Body $loginBody -UseBasicParsing
    Add-Result "admin-login-api" "PASS"

    $accessToken = (($loginResponse.Content | ConvertFrom-Json).data.accessToken)
    if ($Stage -in @("p1", "p2", "all")) {
        Invoke-P1Checks $accessToken
    }
    if ($Stage -in @("p2", "all")) {
        Invoke-P2Checks $accessToken
    }
}

function Invoke-AdminGet {
    param([string]$Name, [string]$Path, [string]$AccessToken)

    Invoke-WebRequest -Uri "$($Config.apiUrl)$Path" -Headers @{ Authorization = "Bearer $AccessToken" } -UseBasicParsing | Out-Null
    Add-Result $Name "PASS"
}

function Invoke-AdminPost {
    param([string]$Name, [string]$Path, [string]$AccessToken)

    Invoke-WebRequest -Uri "$($Config.apiUrl)$Path" -Method Post -Headers @{ Authorization = "Bearer $AccessToken" } -ContentType "application/json" -Body "{}" -UseBasicParsing | Out-Null
    Add-Result $Name "PASS"
}

function Invoke-P1Checks {
    param([string]$AccessToken)

    Invoke-AdminPost "admin-manual-crawl" "/api/v1/admin/crawl-jobs/manual-run" $AccessToken
    Invoke-AdminPost "admin-manual-test" "/api/v1/admin/test-jobs/manual-run" $AccessToken
    Invoke-AdminPost "admin-risk-recalculate" "/api/v1/admin/risks/recalculate" $AccessToken
    Invoke-AdminPost "admin-ranking-rebuild" "/api/v1/admin/rankings/rebuild" $AccessToken
    $offersResponse = Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/admin/offers?page=1&pageSize=20" -Headers @{ Authorization = "Bearer $AccessToken" } -UseBasicParsing
    Add-Result "admin-offers-list" "PASS"
    $offerId = (($offersResponse.Content | ConvertFrom-Json).data.items | Select-Object -First 1).id
    if ($offerId) {
        Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/admin/offers/$offerId/verify" -Method Post -Headers @{ Authorization = "Bearer $AccessToken" } -ContentType "application/json" -Body "{}" -UseBasicParsing | Out-Null
        Add-Result "admin-offer-verify" "PASS"
    }
    Invoke-AdminGet "admin-test-records-list" "/api/v1/admin/test-records?page=1&pageSize=20" $AccessToken
    $risksResponse = Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/admin/risks?page=1&pageSize=20" -Headers @{ Authorization = "Bearer $AccessToken" } -UseBasicParsing
    Add-Result "admin-risks-list" "PASS"
    $riskId = (($risksResponse.Content | ConvertFrom-Json).data.items | Select-Object -First 1).id
    if ($riskId) {
        Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/admin/risks/$riskId" -Headers @{ Authorization = "Bearer $AccessToken" } -UseBasicParsing | Out-Null
        Add-Result "admin-risk-detail" "PASS"
        Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/admin/risks/$riskId/review" -Method Post -Headers @{ Authorization = "Bearer $AccessToken" } -ContentType "application/json" -Body "{""reviewStatus"":""confirmed""}" -UseBasicParsing | Out-Null
        Add-Result "admin-risk-review" "PASS"
    }
    Invoke-AdminGet "admin-job-logs-list" "/api/v1/admin/job-logs?page=1&pageSize=20" $AccessToken
    Verify-BackgroundJobs
}

function Invoke-P2Checks {
    param([string]$AccessToken)

    $challengeResponse = Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/self-tests/challenge" -UseBasicParsing
    Add-Result "public-self-test-challenge" "PASS"
    $challenge = ($challengeResponse.Content | ConvertFrom-Json).data
    $challengeAnswer = "0"
    if ($challenge.question -match "(\d+)\s*\+\s*(\d+)") {
        $challengeAnswer = [string]([int]$Matches[1] + [int]$Matches[2])
    }

    $selfTestBody = @{
        siteUrl = "https://relayport.example"
        modelName = $Config.seed.modelSlug
        apiKey = "sk-local-loop-not-persisted"
        isStream = $true
        testMode = "basic"
        challengeId = $challenge.id
        challengeAnswer = $challengeAnswer
    } | ConvertTo-Json
    $selfTestResponse = Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/self-tests" -Method Post -ContentType "application/json" -Body $selfTestBody -UseBasicParsing
    Add-Result "public-self-test-create" "PASS"
    $selfTestId = (($selfTestResponse.Content | ConvertFrom-Json).data.id)
    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/self-tests/$selfTestId" -UseBasicParsing | Out-Null
    Add-Result "public-self-test-get" "PASS"

    $submissionBody = @{
        siteName = "Loop Submit Site"
        siteUrl = "https://loop-submit.example"
        contact = "loop@example.com"
        description = "Loop verification submission"
    } | ConvertTo-Json
    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/submissions" -Method Post -ContentType "application/json" -Body $submissionBody -UseBasicParsing | Out-Null
    Add-Result "public-submission-create" "PASS"

    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/model-capabilities" -UseBasicParsing | Out-Null
    Add-Result "public-capabilities" "PASS"
    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/articles?page=1&pageSize=20" -UseBasicParsing | Out-Null
    Add-Result "public-articles-list" "PASS"
    Invoke-WebRequest -Uri "$($Config.apiUrl)/api/v1/public/articles/cheapai-data-policy" -UseBasicParsing | Out-Null
    Add-Result "public-article-detail" "PASS"

    Invoke-AdminGet "admin-submissions-list" "/api/v1/admin/submissions?page=1&pageSize=20" $AccessToken
    Invoke-AdminGet "admin-articles-list" "/api/v1/admin/articles?page=1&pageSize=20" $AccessToken
}

function Verify-BackgroundJobs {
    $jobsOut = Join-Path $RuntimeLogDir "jobs-run.out.log"
    $jobsErr = Join-Path $RuntimeLogDir "jobs-run.err.log"
    Remove-Item -LiteralPath $jobsOut, $jobsErr -ErrorAction SilentlyContinue

    $process = Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "$Root/src/backend/CheapAI.BackgroundJobs/CheapAI.BackgroundJobs.csproj") `
        -WorkingDirectory "$Root/src/backend/CheapAI.BackgroundJobs" `
        -RedirectStandardOutput $jobsOut `
        -RedirectStandardError $jobsErr `
        -WindowStyle Hidden `
        -PassThru

    Start-Sleep -Seconds 10
    if (!$process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    $output = ""
    if (Test-Path -LiteralPath $jobsOut) {
        $output += Get-Content -LiteralPath $jobsOut -Raw
    }
    if (Test-Path -LiteralPath $jobsErr) {
        $output += Get-Content -LiteralPath $jobsErr -Raw
    }

    if ($output.Contains("CheapAI recurring jobs registered.") -and $output.Contains("Starting Hangfire Server")) {
        Add-Result "background-jobs-startup" "PASS"
        return
    }

    Add-Result "background-jobs-startup" "FAIL" "Recurring jobs or Hangfire server startup log not found"
    throw "Background jobs startup verification failed"
}

function Capture-Screenshots {
    if ($SkipScreenshots) {
        Add-Result "screenshots" "SKIP"
        return
    }

    Invoke-Checked "playwright-install-chromium" "$Root/tools" "pnpm" @("exec", "playwright", "install", "chromium")
    Invoke-Checked "capture-screenshots" $Root "node" @(
        "$Root/tools/loop/capture-screenshots.cjs",
        $ScreenshotDir,
        $Config.publicUrl,
        $Config.adminUrl,
        $Config.apiUrl,
        $Config.seed.modelSlug,
        $Config.seed.siteSlug,
        $Config.seed.adminUsername,
        $Config.seed.adminPassword
    )
}

try {
    Stop-CheapAiProcesses

    Invoke-Checked "dotnet-build" $Root "dotnet" @("build", "$Root/CheapAI.slnx")
    Invoke-Checked "dotnet-test" $Root "dotnet" @("test", "$Root/CheapAI.slnx")
    Invoke-CheckedWithRetry "public-build" "$Root/src/frontend/web-public" "pnpm" @("build")
    Invoke-CheckedWithRetry "admin-build" "$Root/src/frontend/web-admin" "pnpm" @("build")

    Start-PreviewProcesses
    Invoke-SmokeChecks
    Capture-Screenshots
}
finally {
    $content = @(
        "# CheapAI Loop Report",
        "",
        "- Stage: $Stage",
        "- Timestamp: $Timestamp",
        "- Root: $Root",
        "- API: $($Config.apiUrl)",
        "- Public: $($Config.publicUrl)",
        "- Admin: $($Config.adminUrl)",
        "- Screenshots: $ScreenshotDir",
        "",
        "| Check | Status | Detail |",
        "|---|---|---|"
    ) + $Results

    $content | Set-Content -LiteralPath $Report -Encoding UTF8
    Write-Host "Report: $Report"
}
