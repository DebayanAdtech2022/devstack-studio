# DevStack Studio - Windows Smart Launcher & Prerequisite Installer
param([string[]]$AppArgs)

$repoOrBundleRoot = $PSScriptRoot
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
$rid = if ($arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) { 'win-arm64' } else { 'win-x64' }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DevStack Studio - Universal Launcher" -ForegroundColor Cyan
Write-Host "Detected Platform: $rid ($arch)" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan

# 1. Check Docker prerequisite
$hasDocker = [bool](Get-Command docker -ErrorAction SilentlyContinue)

if (-not $hasDocker) {
    Write-Host "`n[Prerequisite Check] Docker is not detected in PATH." -ForegroundColor Yellow
    Write-Host "DevStack Studio uses Docker Compose to run generated databases and apps." -ForegroundColor Gray
    
    $installed = $false
    # Attempt installation via winget if present
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        Write-Host "--> Attempting automated installation of Docker Desktop via winget..." -ForegroundColor Cyan
        try {
            winget install --id Docker.DockerDesktop --accept-source-agreements --accept-package-agreements --silent
            if ($LASTEXITCODE -eq 0) {
                Write-Host "--> Docker Desktop installed successfully!" -ForegroundColor Green
                $installed = $true
            }
        } catch {
            Write-Host "Warning: winget automated install encountered an issue: $_" -ForegroundColor DarkYellow
        }
    }
    
    # Fallback to chocolatey if present and not installed
    if (-not $installed -and (Get-Command choco -ErrorAction SilentlyContinue)) {
        Write-Host "--> Attempting installation via Chocolatey..." -ForegroundColor Cyan
        try {
            choco install docker-desktop -y
            if ($LASTEXITCODE -eq 0) {
                Write-Host "--> Docker Desktop installed successfully via Chocolatey!" -ForegroundColor Green
                $installed = $true
            }
        } catch {
            Write-Host "Warning: choco automated install encountered an issue: $_" -ForegroundColor DarkYellow
        }
    }

    if (-not $installed) {
        Write-Host "`nNotice: Docker Desktop can be installed manually from: https://docs.docker.com/desktop/setup/install/windows-install/" -ForegroundColor DarkCyan
        Write-Host "Proceeding with launching DevStack Studio Control Plane..." -ForegroundColor Gray
    }
}

# 2. Locate and launch the application executable
$exe = Join-Path $repoOrBundleRoot "platforms\$rid\DevStackStudio.App.exe"

# If running directly inside repo or artifacts structure
if (-not (Test-Path $exe)) {
    $altExe = Join-Path $repoOrBundleRoot "..\artifacts\publish\$rid\DevStackStudio.App.exe"
    if (Test-Path $altExe) {
        $exe = (Resolve-Path $altExe).Path
    }
}

if (-not (Test-Path $exe)) {
    Write-Error "DevStack Studio executable not found at: $exe"
    exit 1
}

Write-Host "`nLaunching DevStack Studio Control Plane..." -ForegroundColor Green
& $exe $AppArgs
exit $LASTEXITCODE