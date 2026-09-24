<#
Launches Revolution Idle in AP or Normal mode. Lives in the game folder (next to
Revolution Idle.exe). Use the "Play Revolution Idle (AP)" / "(Normal)" shortcuts, or:
  powershell -ExecutionPolicy Bypass -File launch.ps1 -AP    # AP version (offline, isolated save)
  powershell -ExecutionPolicy Bypass -File launch.ps1        # normal version (cloud save, untouched)

The mode is passed to the game as a launch argument (--archipelago), NOT stored in the config.
Sticky config state can silently disagree with how the game was actually launched; an argument
can't. The script still forces the legacy [AP Mode] Enabled override to false so an old config
can never contradict the argument.

-WorkingDirectory is required: Doorstop resolves the BepInEx paths in doorstop_config.ini relative
to it, so launching without it starts the game with BepInEx (and therefore the mod) not loaded.
#>
param([switch]$AP)

$ErrorActionPreference = "SilentlyContinue"
$root = $PSScriptRoot

# Doorstop stamps DOORSTOP_INITIALIZED into its process so it can't re-enter itself, and child
# processes inherit it. If this script was started FROM the game (the in-game AP Mode toggle), the
# marker would be inherited all the way down and the relaunched game would quietly run without
# BepInEx. Clearing it here makes the launcher safe no matter who invoked it.
Get-ChildItem Env: | Where-Object { $_.Name -like 'DOORSTOP_*' } | ForEach-Object { Remove-Item "Env:$($_.Name)" }
$cfg  = Join-Path $root "BepInEx\config\com.jontrnka.revolutionidle.ap.cfg"
$exe  = Join-Path $root "Revolution Idle.exe"

# Section-aware clear of [AP Mode] Enabled (the file also has an Enabled under [Connection]).
if (Test-Path $cfg) {
    $lines = Get-Content $cfg
    $inSection = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^\s*\[(.+)\]\s*$') { $inSection = ($Matches[1] -eq 'AP Mode') }
        elseif ($inSection -and $lines[$i] -match '^\s*Enabled\s*=') { $lines[$i] = "Enabled = false" }
    }
    Set-Content -Path $cfg -Value $lines
}

if ($AP) {
    Write-Host "Launching Revolution Idle in AP Mode (offline, isolated save)."
    Start-Process -FilePath $exe -ArgumentList "--archipelago" -WorkingDirectory $root
} else {
    Write-Host "Launching Revolution Idle normally (your usual cloud save)."
    Start-Process -FilePath $exe -WorkingDirectory $root
}
