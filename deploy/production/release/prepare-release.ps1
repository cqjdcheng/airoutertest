param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../../..")).Path,
    [string]$ReleaseTag = (Get-Date -Format "yyyyMMddHHmmss"),
    [string]$OutputRoot = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $ProjectRoot "artifacts/release"
}

$releaseDir = Join-Path $OutputRoot $ReleaseTag
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null

$images = @(
    @{
        Service = "api"
        Repository = "realllmcn-api"
        Dockerfile = "deploy/production/Dockerfile.api"
        TarFile = "realllmcn-api_$ReleaseTag.tar"
    },
    @{
        Service = "jobs"
        Repository = "realllmcn-jobs"
        Dockerfile = "deploy/production/Dockerfile.jobs"
        TarFile = "realllmcn-jobs_$ReleaseTag.tar"
    },
    @{
        Service = "public-web"
        Repository = "realllmcn-public-web"
        Dockerfile = "deploy/production/Dockerfile.public"
        TarFile = "realllmcn-public-web_$ReleaseTag.tar"
    },
    @{
        Service = "admin-web"
        Repository = "realllmcn-admin-web"
        Dockerfile = "deploy/production/Dockerfile.admin"
        TarFile = "realllmcn-admin-web_$ReleaseTag.tar"
    }
)

$manifestImages = @()
$shaLines = @()

foreach ($image in $images) {
    $repository = $image.Repository
    $dockerfile = Join-Path $ProjectRoot $image.Dockerfile
    $tarPath = Join-Path $releaseDir $image.TarFile

    if (-not $SkipBuild) {
        docker build `
            -f $dockerfile `
            -t "${repository}:latest" `
            -t "${repository}:${ReleaseTag}" `
            $ProjectRoot
    }

    docker save `
        -o $tarPath `
        "${repository}:${ReleaseTag}" `
        "${repository}:latest"

    $hash = (Get-FileHash -Algorithm SHA256 -Path $tarPath).Hash.ToLowerInvariant()
    $bytes = (Get-Item -LiteralPath $tarPath).Length
    $shaLines += "$hash  $($image.TarFile)"

    $manifestImages += [ordered]@{
        service = $image.Service
        repository = $repository
        tag = $ReleaseTag
        latestTag = "latest"
        dockerfile = $image.Dockerfile
        tarFile = $image.TarFile
        sha256 = $hash
        bytes = $bytes
    }
}

$manifest = [ordered]@{
    releaseTag = $ReleaseTag
    createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    project = "CheapAI"
    composeDir = "/www/wwwroot/realllm.cn"
    composeFile = "docker-compose.yml"
    services = @("api", "jobs", "public-web", "admin-web")
    images = $manifestImages
    applyScript = "deploy/production/release/apply-release.sh"
}

$manifestPath = Join-Path $releaseDir "manifest.json"
$shaPath = Join-Path $releaseDir "SHA256SUMS"

$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
$shaLines | Set-Content -LiteralPath $shaPath -Encoding ASCII

Write-Host "Release tag: $ReleaseTag"
Write-Host "Release directory: $releaseDir"
Write-Host "Manifest: $manifestPath"
Write-Host "Checksums: $shaPath"
