<#
Launches Revolution Idle, setting AP Mode on/off first. Lives in the game folder (next to
Revolution Idle.exe). Use the "Play Revolution Idle (AP)" / "(Normal)" shortcuts, or:
  powershell -ExecutionPolicy Bypass -File launch.ps1 -AP    # AP version (offline, isolated save)
  powershell -ExecutionPolicy Bypass -File launch.ps1        # normal version (cloud save, untouched)
#>
param([switch]$AP)

$ErrorActionPreference = "SilentlyContinue"
$root = $PSScriptRoot
$cfg  = Join-Path $root "BepInEx\config\com.jontrnka.revolutionidle.ap.cfg"
$exe  = Join-Path $root "Revolution Idle.exe"
$val  = if ($AP) { "true" } else { "false" }

# Section-aware set of [AP Mode] Enabled (the file also has an Enabled under [Connection]).
if (Test-Path $cfg) {
    $lines = Get-Content $cfg
    $inSection = $false; $set = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*\[(.+)\]\s*$') { $inSection = ($Matches[1] -eq 'AP Mode') }
        elseif ($inSection -and $lines[$i] -match '^\s*Enabled\s*=') { $lines[$i] = "Enabled = $val"; $set = $true }
    }
    if (-not $set) { $lines += @("", "[AP Mode]", "Enabled = $val") }
    Set-Content -Path $cfg -Value $lines
} else {
    # First run: seed a minimal config; the mod fills in the rest on launch.
    New-Item -ItemType Directory -Force -Path (Split-Path $cfg) | Out-Null
    Set-Content -Path $cfg -Value @("[AP Mode]", "Enabled = $val")
}

Write-Host ("Launching Revolution Idle with AP Mode = {0}" -f $val)
Start-Process -FilePath $exe -WorkingDirectory $root
