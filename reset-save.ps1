<#
.SYNOPSIS
  Back up and wipe the Revolution Idle save for a clean Archipelago run, and restore it later.

.DESCRIPTION
  Revolution Idle stores its save in the Windows registry under
  HKCU\Software\Oni Gaming\Revolution Idle (values game_data_* / inventory_*), with cloud
  saves via Nakama (server.session_* / server.refreshToken_*).

  Default action (no switches): back up the whole key, then wipe the save so the game starts
  fresh. The cloud session tokens are also cleared so the game starts logged-out and does NOT
  re-download your cloud save over the fresh start (use -KeepCloud to leave them).

  Your ORIGINAL (pre-AP) save is preserved once, permanently, in revidle_save_ORIGINAL.reg and
  is never overwritten by later resets. Every reset also writes a timestamped backup.

.PARAMETER Restore
  Restore a backup instead of wiping. With no -BackupPath, restores revidle_save_ORIGINAL.reg.

.PARAMETER BackupPath
  Specific .reg file to restore (with -Restore).

.PARAMETER KeepCloud
  Do not clear the Nakama cloud session tokens when wiping.

.PARAMETER List
  List available backups and exit.

.PARAMETER Force
  Proceed even if the game appears to be running (NOT recommended; the game overwrites the
  registry on exit).

.EXAMPLE
  .\reset-save.ps1                 # back up + wipe -> fresh start
  .\reset-save.ps1 -Restore        # restore your original pre-AP save
  .\reset-save.ps1 -List
#>
[CmdletBinding()]
param(
    [switch]$Restore,
    [string]$BackupPath,
    [switch]$KeepCloud,
    [switch]$List,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$KeyPS   = "HKCU:\Software\Oni Gaming\Revolution Idle"
$KeyReg  = "HKCU\Software\Oni Gaming\Revolution Idle"
$Dir     = $PSScriptRoot
$Original = Join-Path $Dir "revidle_save_ORIGINAL.reg"

function Test-GameRunning {
    [bool](Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like "*Revolution*" })
}

if ($List) {
    Write-Host "Backups in $Dir :" -ForegroundColor Cyan
    Get-ChildItem -Path $Dir -Filter "revidle_save_*.reg" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime |
        ForEach-Object { "{0,-40} {1}" -f $_.Name, $_.LastWriteTime }
    return
}

if (Test-GameRunning -and -not $Force) {
    Write-Host "Revolution Idle appears to be running. Close it first (it rewrites the registry on exit)." -ForegroundColor Yellow
    Write-Host "Re-run with -Force to override." -ForegroundColor Yellow
    return
}

if ($Restore) {
    $src = if ($BackupPath) { $BackupPath } else { $Original }
    if (-not (Test-Path $src)) { throw "Backup not found: $src  (try -List)" }
    Write-Host "Restoring save from $src ..." -ForegroundColor Cyan
    reg import "$src" | Out-Null
    Write-Host "Restored. Your save is back." -ForegroundColor Green
    return
}

# --- default: back up + wipe ---
if (-not (Test-Path $KeyPS)) { throw "Save key not found ($KeyReg). Has the game been run at least once?" }

# Preserve the very first (pre-AP) save permanently.
if (-not (Test-Path $Original)) {
    reg export "$KeyReg" "$Original" /y | Out-Null
    Write-Host "Saved your ORIGINAL save to $Original (kept forever)." -ForegroundColor Green
}

# Always write a timestamped backup too.
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$tsBackup = Join-Path $Dir "revidle_save_backup_$stamp.reg"
reg export "$KeyReg" "$tsBackup" /y | Out-Null
Write-Host "Backed up current save to $tsBackup" -ForegroundColor Green

# Remove save values (match by prefix so a differing Unity hash suffix is still caught).
$props = (Get-ItemProperty $KeyPS).PSObject.Properties.Name
$wipePrefixes = @("game_data", "inventory")
if (-not $KeepCloud) { $wipePrefixes += @("server.session", "server.refreshToken") }

$removed = @()
foreach ($name in $props) {
    foreach ($p in $wipePrefixes) {
        if ($name -like "$p*") {
            Remove-ItemProperty -Path $KeyPS -Name $name -ErrorAction SilentlyContinue
            $removed += $name
            break
        }
    }
}

if ($removed.Count -eq 0) {
    Write-Host "No save values found to remove (already clean?)." -ForegroundColor Yellow
} else {
    Write-Host "Wiped: $($removed -join ', ')" -ForegroundColor Green
    Write-Host "Fresh start ready. Launch Revolution Idle for a new game." -ForegroundColor Cyan
    if (-not $KeepCloud) { Write-Host "(Cloud session cleared so it won't re-download your old save. Re-login in-game to use cloud again.)" -ForegroundColor DarkGray }
}
