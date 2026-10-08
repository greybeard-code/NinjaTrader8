#Requires -Version 5.1
<#
.SYNOPSIS
    Finds misplaced GodZilla Suite .cs files under NinjaTrader 8 and moves them
    to Downloads\OldGreybeard-Delete for review before deletion.

.DESCRIPTION
    Searches the entire NinjaTrader 8 folder tree for known GodZilla Suite
    source files that may have been installed in the wrong location. Each file
    found is moved to a staging folder in the user's Downloads directory.

    Run this with NinjaTrader 8 closed to avoid file-lock errors.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Target filenames ──────────────────────────────────────────────────────────

$TargetFiles = @(
    # Sub-indicators
    'gbBarStatus.cs',
    'gbKingOrderBlock.cs',
    'gbPANAKanal.cs',
    'gbSumoPullback.cs',
    'gbSuperJumpBoost.cs',
    'gbThunderZilla.cs',
    'gbNobleCloud.cs',
    'gbNobelCloud.cs',          # alternate spelling found on some installs
    'NewsSignals.cs',

    # Wrapper / strategy files
    'gbKingPanaZilla.cs',
    'gbKingPanaZillaKillah.cs',
    'GodZilla.cs',
    'GodZillaKilla.cs',
    'GodZuki.cs'
)

# ── Paths ─────────────────────────────────────────────────────────────────────

$SearchRoot  = Join-Path $env:USERPROFILE 'Documents\NinjaTrader 8'
$Destination = Join-Path $env:USERPROFILE 'Downloads\OldGreybeard-Delete'

# ── Pre-flight checks ─────────────────────────────────────────────────────────

if (-not (Test-Path $SearchRoot)) {
    Write-Warning "NinjaTrader 8 folder not found: $SearchRoot"
    exit 1
}

if (-not (Test-Path $Destination)) {
    New-Item -ItemType Directory -Path $Destination | Out-Null
    Write-Host "Created: $Destination"
}

# ── Search ────────────────────────────────────────────────────────────────────

Write-Host "Searching: $SearchRoot`n"

# Search per filename using -Filter so NT8 subdirectory permission errors
# on one file do not suppress results for the others.
$FoundList = [System.Collections.Generic.List[System.IO.FileInfo]]::new()

foreach ($Name in $TargetFiles) {
    $Hits = Get-ChildItem -Path $SearchRoot -Recurse -File -Filter $Name -ErrorAction SilentlyContinue
    if ($Hits) {
        foreach ($Hit in $Hits) {
            Write-Host "  Found: $($Hit.FullName)"
            $FoundList.Add($Hit)
        }
    }
}

if ($FoundList.Count -eq 0) {
    Write-Host 'No matching files found.'
    exit 0
}

Write-Host "`nFound $($FoundList.Count) file(s).`n"
$Found = $FoundList

# ── Move ──────────────────────────────────────────────────────────────────────

$Moved  = [System.Collections.Generic.List[string]]::new()
$Failed = [System.Collections.Generic.List[string]]::new()

foreach ($File in $Found) {

    # Resolve collision: append parent-folder name before extension
    $DestName = $File.Name
    $DestPath = Join-Path $Destination $DestName

    if (Test-Path $DestPath) {
        $Base     = [System.IO.Path]::GetFileNameWithoutExtension($File.Name)
        $Ext      = $File.Extension
        $Parent   = $File.Directory.Name
        $DestName = "${Base}_${Parent}${Ext}"
        $DestPath = Join-Path $Destination $DestName
    }

    try {
        Move-Item -Path $File.FullName -Destination $DestPath
        $Moved.Add("  MOVED  $($File.FullName)")
        $Moved.Add("      -> $DestName`n")
    }
    catch {
        $Failed.Add("  FAILED $($File.FullName)")
        $Failed.Add("         $($_.Exception.Message)`n")
    }
}

# ── Report ────────────────────────────────────────────────────────────────────

Write-Host "-- Moved $($Moved.Count / 2) of $($Found.Count) file(s) to:"
Write-Host "   $Destination`n"
$Moved | ForEach-Object { Write-Host $_ }

if ($Failed.Count -gt 0) {
    Write-Warning "-- $($Failed.Count / 2) file(s) could not be moved (is NinjaTrader 8 running?):"
    $Failed | ForEach-Object { Write-Warning $_ }
}

Write-Host "`nClosing in 60 seconds..."
Start-Sleep -Seconds 60
