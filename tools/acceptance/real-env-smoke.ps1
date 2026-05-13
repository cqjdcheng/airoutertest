param(
    [string]$ApiUrl = "http://127.0.0.1:5050",
    [switch]$RunExternal
)

$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path.Replace("\", "/")
$Timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$ReportDir = Join-Path $Root "artifacts/acceptance/$Timestamp"
$Report = Join-Path $ReportDir "real-env-report.md"
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null

$Results = [System.Collections.Generic.List[string]]::new()

function Add-Result {
    param([string]$Name, [string]$Status, [string]$Detail = "")
    $safeDetail = $Detail -replace "\|", "/"
    $Results.Add("| $Name | $Status | $safeDetail |")
    Write-Host "[$Status] $Name $safeDetail"
}

function Get-RequiredEnv {
    param([string]$Name)
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        if ($RunExternal) {
            Add-Result "env-$Name" "FAIL" "Missing required environment variable"
            throw "Missing required environment variable: $Name"
        }

        Add-Result "env-$Name" "WARN" "Not set; skipped because -RunExternal was not provided"
        return ""
    }

    Add-Result "env-$Name" "PASS" "Configured"
    return $value
}

try {
    Invoke-WebRequest -Uri "$ApiUrl/api/health" -UseBasicParsing -TimeoutSec 10 | Out-Null
    Add-Result "api-health" "PASS" $ApiUrl

    $relayBaseUrl = Get-RequiredEnv "CHEAPAI_REAL_RELAY_BASE_URL"
    $relayApiKey = Get-RequiredEnv "CHEAPAI_REAL_RELAY_API_KEY"
    $relayModel = Get-RequiredEnv "CHEAPAI_REAL_RELAY_MODEL"
    $priceSourceUrl = [Environment]::GetEnvironmentVariable("CHEAPAI_REAL_PRICE_SOURCE_URL")

    if ($RunExternal) {
        $selfTestBody = @{
            siteUrl = $relayBaseUrl
            modelName = $relayModel
            apiKey = $relayApiKey
            isStream = $true
            testMode = "basic"
        } | ConvertTo-Json

        $selfTestResponse = Invoke-WebRequest -Uri "$ApiUrl/api/v1/public/self-tests" `
            -Method Post `
            -ContentType "application/json" `
            -Body $selfTestBody `
            -UseBasicParsing `
            -TimeoutSec 30

        $selfTestId = (($selfTestResponse.Content | ConvertFrom-Json).data.id)
        if ([string]::IsNullOrWhiteSpace($selfTestId)) {
            throw "Self-test did not return an id."
        }

        Invoke-WebRequest -Uri "$ApiUrl/api/v1/public/self-tests/$selfTestId" -UseBasicParsing -TimeoutSec 30 | Out-Null
        Add-Result "real-self-test-chain" "PASS" "selfTestId=$selfTestId"

        if (![string]::IsNullOrWhiteSpace($priceSourceUrl)) {
            Invoke-WebRequest -Uri $priceSourceUrl -UseBasicParsing -TimeoutSec 30 | Out-Null
            Add-Result "real-price-source-reachable" "PASS" $priceSourceUrl
        }
        else {
            Add-Result "real-price-source-reachable" "WARN" "CHEAPAI_REAL_PRICE_SOURCE_URL not set"
        }
    }
    else {
        Add-Result "real-external-chain" "SKIP" "Use -RunExternal after setting real relay env vars"
    }
}
catch {
    Add-Result "real-env-smoke" "FAIL" ($_.Exception.Message)
    throw
}
finally {
    $content = @(
        "# CheapAI Real Environment Acceptance Report",
        "",
        "- Timestamp: $Timestamp",
        "- API: $ApiUrl",
        "- RunExternal: $RunExternal",
        "",
        "| Check | Status | Detail |",
        "|---|---|---|"
    ) + $Results

    $content | Set-Content -LiteralPath $Report -Encoding UTF8
    Write-Host "Report: $Report"
}
