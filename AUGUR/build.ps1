#Requires -Version 5.1
<#
.SYNOPSIS
    AUGUR Trade Intelligence - Build Script

.DESCRIPTION
    Compiles src/augur.py into a single standalone exe using Nuitka,
    then deploys it to Documents\AUGUR\AUGUR.exe.

    First-time builds take 2-5 minutes; Nuitka auto-downloads a C compiler
    (MinGW-w64) if Visual Studio Build Tools are not installed.

.EXAMPLE
    .\build.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$SrcEntry  = Join-Path $PSScriptRoot 'src\augur.py'
$DistDir   = Join-Path $PSScriptRoot 'dist'

# Locate AUGUR deploy dir via Windows registry (handles OneDrive redirection)
try {
    $DocsPath = (Get-ItemProperty `
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders' `
        -Name 'Personal').Personal
} catch {
    $DocsPath = Join-Path $env:USERPROFILE 'Documents'
}
$DeployDir = Join-Path $DocsPath 'AUGUR'

Write-Host ""
Write-Host " ============================================================" -ForegroundColor Cyan
Write-Host "  AUGUR Trade Intelligence - Build" -ForegroundColor Cyan
Write-Host " ============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Source  : $SrcEntry"
Write-Host "  Deploy  : $DeployDir\AUGUR.exe"
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

$locationLine = $pipInfo | Where-Object { $_ -match '^Location:' }
if (-not $locationLine) {
    Write-Error "Could not determine Nuitka location from: $pipInfo"
}
$sitePkgs = ($locationLine -replace '^Location:\s*', '').Trim()
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
$IconFile = Join-Path $PSScriptRoot 'augur.ico'
if (Test-Path $IconFile) {
    Write-Host "  Icon: $IconFile" -ForegroundColor Green
    $IconArgs = @("--windows-icon-from-ico=$IconFile")
} else {
    Write-Host "  Icon: none  (run: python make_icon.py  to generate augur.ico)" -ForegroundColor Yellow
}
Write-Host ""

# ── Compile ───────────────────────────────────────────────────────────────────
Write-Host " [BUILD] Compiling AUGUR.exe with Nuitka..." -ForegroundColor Yellow
Write-Host "         First run downloads a C compiler and takes 2-5 minutes." -ForegroundColor DarkGray
Write-Host ""

$NuitkaArgs = @(
    '--onefile',
    '--windows-console-mode=force',
    "--output-filename=AUGUR.exe",
    "--output-dir=$DistDir",
    '--company-name=GreyBeard',
    "--product-name=AUGUR Trade Intelligence",
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
Write-Host " [DEPLOY] Copying to $DeployDir\AUGUR.exe..." -ForegroundColor Yellow

if (-not (Test-Path $DeployDir)) {
    New-Item -ItemType Directory -Path $DeployDir | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $DeployDir 'imports')  | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $DeployDir 'reports')  | Out-Null
    Write-Host "          Created $DeployDir"
}

Copy-Item (Join-Path $DistDir 'AUGUR.exe') `
          (Join-Path $DeployDir 'AUGUR.exe') -Force

Write-Host ""
Write-Host " ============================================================" -ForegroundColor Green
Write-Host "  Build complete!" -ForegroundColor Green
Write-Host ""
Write-Host "  Deployed to:"
Write-Host "  $DeployDir\AUGUR.exe" -ForegroundColor White
Write-Host ""
Write-Host "  To use AUGUR:"
Write-Host "    1. Export from NT8 Executions tab (right-click -> Export)"
Write-Host "    2. Run: AUGUR.exe --import ""path\to\export.csv"""
Write-Host "    3. Open: $DeployDir\Templum.html"
Write-Host " ============================================================" -ForegroundColor Green
Write-Host ""

$run = Read-Host " Run AUGUR.exe now? [Y/N]"
if ($run -ieq 'Y') {
    Write-Host ""
    & (Join-Path $DeployDir 'AUGUR.exe')
}
