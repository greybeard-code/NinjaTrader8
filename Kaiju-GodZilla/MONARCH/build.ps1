#Requires -Version 5.1
<#
.SYNOPSIS
    MONARCH Intelligence Report System - Build Script

.DESCRIPTION
    Compiles src/monarch.py into a single standalone exe using Nuitka,
    then deploys it to NinjaTrader 8\MONARCH\.

    First-time builds take 2-5 minutes; Nuitka auto-downloads a C compiler
    (MinGW-w64) if Visual Studio Build Tools are not installed.

.EXAMPLE
    .\build.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$SrcEntry  = Join-Path $PSScriptRoot 'src\monarch.py'
$DistDir   = Join-Path $PSScriptRoot 'dist'
$DeployDir = Join-Path $env:USERPROFILE 'Documents\NinjaTrader 8\MONARCH'

Write-Host ""
Write-Host " ============================================================" -ForegroundColor Cyan
Write-Host "  MONARCH Intelligence Report System - Build" -ForegroundColor Cyan
Write-Host " ============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Source  : $SrcEntry"
Write-Host "  Deploy  : $DeployDir\MONARCH.exe"
Write-Host ""

# ── Find the Python that owns Nuitka ──────────────────────────────────────────
Write-Host " [CHECK] Locating Nuitka..." -ForegroundColor Yellow

$pipInfo = pip show nuitka 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host " [INFO] Nuitka not found. Installing..." -ForegroundColor Yellow
    pip install nuitka
    if ($LASTEXITCODE -ne 0) {
        Write-Error "pip install nuitka failed. Is pip in your PATH?"
    }
    $pipInfo = pip show nuitka 2>&1
}

# Parse the Location line to find the site-packages folder
$locationLine = $pipInfo | Where-Object { $_ -match '^Location:' }
if (-not $locationLine) {
    Write-Error "Could not determine Nuitka location from: $pipInfo"
}
$sitePkgs = ($locationLine -replace '^Location:\s*', '').Trim()
# python.exe is two levels up from site-packages (Lib/site-packages -> Lib -> root)
$pyRoot   = Split-Path (Split-Path $sitePkgs -Parent) -Parent
$pyExe    = Join-Path $pyRoot 'python.exe'

if (-not (Test-Path $pyExe)) {
    Write-Host " [WARN] Could not derive python.exe from pip location ($sitePkgs)." -ForegroundColor Yellow
    Write-Host "        Falling back to 'python' in PATH." -ForegroundColor Yellow
    $pyExe = 'python'
}

$pyVer = & $pyExe --version 2>&1
$niVer = ($pipInfo | Where-Object { $_ -match '^Version:' }) -replace '^Version:\s*', ''
Write-Host "  Python : $pyVer  ($pyExe)" -ForegroundColor Green
Write-Host "  Nuitka : $niVer" -ForegroundColor Green
Write-Host ""

# ── Extract version from config.py ────────────────────────────────────────────
$configFile    = Join-Path $PSScriptRoot 'src\config.py'
$configContent = Get-Content $configFile -Raw
$appVersion    = if ($configContent -match 'VERSION\s*=\s*"([^"]+)"') { $Matches[1] } else { '1.0.0' }
$vParts        = $appVersion.Split('.')
while ($vParts.Count -lt 4) { $vParts += '0' }
$winVersion    = ($vParts[0..3]) -join '.'
Write-Host "  Version: $appVersion  (exe metadata: $winVersion)" -ForegroundColor Green
Write-Host ""

# ── Clean previous build artifacts ────────────────────────────────────────────
Write-Host " [CLEAN] Removing previous build..." -ForegroundColor Yellow
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Path $DistDir | Out-Null

# ── Optional icon ─────────────────────────────────────────────────────────────
$IconArgs = @()
$IconFile = Join-Path $PSScriptRoot 'monarch.ico'
if (Test-Path $IconFile) {
    Write-Host "  Icon: $IconFile" -ForegroundColor Green
    $IconArgs = @("--windows-icon-from-ico=$IconFile")
} else {
    Write-Host "  Icon: none  (run: python make_icon.py  to generate monarch.ico)" -ForegroundColor Yellow
}
Write-Host ""

# ── Compile ───────────────────────────────────────────────────────────────────
Write-Host " [BUILD] Compiling MONARCH.exe with Nuitka..." -ForegroundColor Yellow
Write-Host "         First run downloads a C compiler and takes 2-5 minutes." -ForegroundColor DarkGray
Write-Host ""

$NuitkaArgs = @(
    '--onefile',
    '--windows-console-mode=force',
    "--output-filename=MONARCH.exe",
    "--output-dir=$DistDir",
    '--company-name=GreyBeard',
    "--product-name=MONARCH Intelligence Report System",
    "--file-version=$winVersion",
    "--product-version=$winVersion",
    "--copyright=(c) GreyBeard - greybeardconsulting.net",
    '--assume-yes-for-downloads'
) + $IconArgs + @($SrcEntry)

& $pyExe -m nuitka @NuitkaArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Nuitka compilation failed (exit code $LASTEXITCODE)."
}

# ── Deploy ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host " [DEPLOY] Copying to $DeployDir\MONARCH.exe..." -ForegroundColor Yellow

if (-not (Test-Path $DeployDir)) {
    New-Item -ItemType Directory -Path $DeployDir | Out-Null
    Write-Host "          Created $DeployDir"
}

Copy-Item (Join-Path $DistDir 'MONARCH.exe') `
          (Join-Path $DeployDir 'MONARCH.exe') -Force

Write-Host ""
Write-Host " ============================================================" -ForegroundColor Green
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "  Deployed to:"
Write-Host "  $DeployDir\MONARCH.exe" -ForegroundColor White
Write-Host ""
Write-Host "  First run creates:"
Write-Host "    $DeployDir\CastleBravo.html"
Write-Host "    $DeployDir\logs\"
Write-Host "    $DeployDir\reports\"
Write-Host " ============================================================" -ForegroundColor Green
Write-Host ""

$run = Read-Host " Run MONARCH.exe now? [Y/N]"
if ($run -ieq 'Y') {
    Write-Host ""
    & (Join-Path $DeployDir 'MONARCH.exe')
}
