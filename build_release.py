"""Assemble a distributable Revolution Idle Archipelago release.

Produces dist/RevolutionIdleAP-v<version>.zip containing:
  - apworld/revolution_idle.apworld            (-> Archipelago/custom_worlds)
  - Install-into-Game-Folder/                  (extract into the game folder)
      winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt
      dotnet/                                   (BepInEx's bundled runtime)
      BepInEx/core/                             (PATCHED toolchain: Cpp2IL .21 + matching deps)
      BepInEx/patchers/
      BepInEx/plugins/RevolutionIdleAP/         (plugin + AP client + Newtonsoft)
  - reset-save.ps1
  - README.md

Pulls the BepInEx setup from the live game install (already patched + working). Excludes
per-user/generated content (interop, unity-libs, logs, configs) so the user generates fresh.
"""
import json
import shutil
import zipfile
from pathlib import Path

ROOT = Path(__file__).parent
GAME = Path(r"A:\SteamLibrary\steamapps\common\Revolution Idle")
DIST = ROOT / "dist"
STAGE = DIST / "_release_stage"

PKG_VERSION = json.loads((ROOT / "apworld" / "revolution_idle" / "archipelago.json").read_text())["world_version"]

# Top-level doorstop files to ship.
DOORSTOP_FILES = ["winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt"]
# BepInEx subdirs to ship (NOT interop/ or unity-libs/ — regenerated on first run).
BEPINEX_DIRS = ["core", "patchers"]


def stage_release() -> None:
    if STAGE.exists():
        shutil.rmtree(STAGE)
    STAGE.mkdir(parents=True)

    # apworld
    apworld = DIST / "revolution_idle.apworld"
    if not apworld.exists():
        raise SystemExit("Build the apworld first: python build_apworld.py")
    (STAGE / "apworld").mkdir()
    shutil.copy2(apworld, STAGE / "apworld" / "revolution_idle.apworld")

    # game-folder payload
    game_out = STAGE / "Install-into-Game-Folder"
    game_out.mkdir()
    for f in DOORSTOP_FILES:
        src = GAME / f
        if src.exists():
            shutil.copy2(src, game_out / f)

    shutil.copytree(GAME / "dotnet", game_out / "dotnet")

    bep_out = game_out / "BepInEx"
    bep_out.mkdir()
    for d in BEPINEX_DIRS:
        shutil.copytree(GAME / "BepInEx" / d, bep_out / d)
    # plugin only (skip any other plugins the user happens to have)
    shutil.copytree(GAME / "BepInEx" / "plugins" / "RevolutionIdleAP",
                    bep_out / "plugins" / "RevolutionIdleAP")

    # AP Mode launchers go into the game folder (next to Revolution Idle.exe).
    for f in ["launch.ps1", "Play Revolution Idle (AP).bat", "Play Revolution Idle (Normal).bat"]:
        shutil.copy2(ROOT / f, game_out / f)

    # extras
    shutil.copy2(ROOT / "reset-save.ps1", STAGE / "reset-save.ps1")
    shutil.copy2(ROOT / "RELEASE_README.md", STAGE / "README.md")


def zip_release() -> Path:
    out = DIST / f"RevolutionIdleAP-v{PKG_VERSION}.zip"
    if out.exists():
        out.unlink()
    files = 0
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(STAGE.rglob("*")):
            if path.is_file():
                zf.write(path, path.relative_to(STAGE).as_posix())
                files += 1
    return out, files


def main() -> None:
    stage_release()
    out, files = zip_release()
    shutil.rmtree(STAGE)
    size_mb = out.stat().st_size / (1024 * 1024)
    print(f"Built {out}  (v{PKG_VERSION}, {files} files, {size_mb:.1f} MB)")


if __name__ == "__main__":
    main()
