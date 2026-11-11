param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/publish"
)

$ErrorActionPreference = "Stop"

# Automatically resolve and anchor to repository root
$repoRoot = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repoRoot

$targets = @(
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DevStack Studio - Build and Package Pipeline" -ForegroundColor Cyan
Write-Host "Repo Root: $repoRoot" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan

# Create output structure
$distDir = "artifacts/dist"
if (Test-Path $distDir) { Remove-Item -Recurse -Force $distDir }
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

$manifestEntries = @()

foreach ($rid in $targets) {
    Write-Host "`n--> Publishing for target: $rid..." -ForegroundColor Yellow
    $targetPubDir = "$OutputDir/$rid"
    
    dotnet publish src/DevStackStudio.App/DevStackStudio.App.csproj `
        -c $Configuration `
        -r $rid `
        --self-contained `
        -p:PublishSingleFile=true `
        -o $targetPubDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed for $rid."
    }

    # Create ZIP archive for this platform
    $zipName = "devstack-studio-0.1.0-$rid.zip"
    $zipPath = "$distDir/$zipName"
    Write-Host "    Archiving $rid to $zipName..." -ForegroundColor Gray
    Compress-Archive -Path "$targetPubDir/*" -DestinationPath $zipPath -Force

    # Compute SHA256
    $hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -Path "$zipPath.sha256" -Value "$hash  $zipName"

    $manifestEntries += [PSCustomObject]@{
        Target = $rid
        Archive = $zipName
        Sha256 = $hash
        Size = (Get-Item $zipPath).Length
    }
}

# Create Universal Offline Bundle (contains all target builds + smart launcher + docs)
Write-Host "`n--> Creating Universal Offline Bundle..." -ForegroundColor Yellow
$universalStaging = "artifacts/staging/universal"
if (Test-Path $universalStaging) { Remove-Item -Recurse -Force $universalStaging }
New-Item -ItemType Directory -Force -Path "$universalStaging/platforms" | Out-Null
New-Item -ItemType Directory -Force -Path "$universalStaging/docs" | Out-Null

Copy-Item -Path "README.md" -Destination "$universalStaging/"
if (Test-Path "docs") { Copy-Item -Path "docs/*" -Destination "$universalStaging/docs/" -Recurse }

foreach ($rid in $targets) {
    Copy-Item -Path "$OutputDir/$rid" -Destination "$universalStaging/platforms/$rid" -Recurse
}

# Copy the universal standalone launchers (install.bat for Windows, install.command for macOS & Linux)
Copy-Item -Path "scripts/install.bat" -Destination "$universalStaging/install.bat"
Copy-Item -Path "scripts/install.command" -Destination "$universalStaging/install.command"
Copy-Item -Path "scripts/launcher.ps1" -Destination "$universalStaging/launcher.ps1"

$universalZip = "devstack-studio-0.1.0-universal-offline.zip"
$universalPath = "$distDir/$universalZip"
Write-Host "    Compressing Universal Offline Bundle to $universalZip..." -ForegroundColor Gray
Compress-Archive -Path "$universalStaging/*" -DestinationPath $universalPath -Force

$universalHash = (Get-FileHash -Path $universalPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$universalPath.sha256" -Value "$universalHash  $universalZip"

$manifestEntries += [PSCustomObject]@{
    Target = "universal-offline"
    Archive = $universalZip
    Sha256 = $universalHash
    Size = (Get-Item $universalPath).Length
}

# Write release manifest JSON
$releaseManifest = [PSCustomObject]@{
    Version = "0.1.0"
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    Artifacts = $manifestEntries
}

$releaseManifest | ConvertTo-Json -Depth 5 | Set-Content -Path "$distDir/release-manifest.json"

# Also copy dist artifacts to web/downloads for the smart download site
New-Item -ItemType Directory -Force -Path "web/downloads" | Out-Null
Copy-Item -Path "$distDir/*" -Destination "web/downloads/" -Force

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Publishing and Packaging Complete!" -ForegroundColor Green
Write-Host "Universal bundle: $distDir/$universalZip" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
