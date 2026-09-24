"""Assemble a distributable Revolution Idle Archipelago release.

Produces dist/RevolutionIdleAP-v<version>.zip containing:
  - apworld/revolution_idle.apworld            (-> Archipelago/custom_worlds)
  - Install-into-Game-Folder/                  (extract into the game folder)
      winhttp.dll, doorstop_config.ini, .doorstop_version, changelog.txt
      dotnet/                                   (BepInEx's bundled runtime)
      BepInEx/core/                             (PATCHED toolchain: Cpp2IL .21 + matching deps)
      BepInEx/patchers/
      BepInEx/plugins/RevolutionIdleAP/         (plugin + AP client + Newtonsoft)
  - Revolution Idle.yaml                       (-> Archipelago/Players; the options template)
  - reset-save.ps1
  - README.md
  - LICENSE, THIRD-PARTY-NOTICES.md, licenses/  (bundled components incl. LGPL/Apache)

Pulls the BepInEx setup from the live game install (already patched + working). Excludes
per-user/generated content (interop, unity-libs, logs, configs) so the user generates fresh.
"""
import json
import shutil
import subprocess
import sys
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
    shutil.copy2(template_path(), STAGE / TEMPLATE_NAME)

    # Licences. The release bundles LGPL/Apache components (BepInEx, Doorstop, Il2CppInterop,
    # Dobby), so the licence texts and the attribution notice have to travel with it.
    shutil.copy2(ROOT / "LICENSE", STAGE / "LICENSE")
    shutil.copy2(ROOT / "THIRD-PARTY-NOTICES.md", STAGE / "THIRD-PARTY-NOTICES.md")
    shutil.copytree(ROOT / "licenses", STAGE / "licenses")


TEMPLATE_NAME = "Revolution Idle.yaml"


def refresh_template() -> None:
    """Regenerate the YAML template from this build's apworld (best effort).

    Needs a local Archipelago install, so a failure is a warning rather than a hard stop — the
    committed template is still shipped, and the freshness check below decides whether to complain.
    """
    try:
        subprocess.run([sys.executable, str(ROOT / "build_template.py")], check=True)
    except Exception as e:
        print(f"WARNING: could not regenerate the YAML template ({e}).")
        print("         Shipping the committed examples/ copy instead.")


def template_path() -> Path:
    """The template to ship, warning loudly if it doesn't match this release's version."""
    path = ROOT / "examples" / TEMPLATE_NAME
    if not path.exists():
        raise SystemExit(f"Missing {path} — run build_template.py.")
    stamp = f"Revolution Idle: {PKG_VERSION}"
    if stamp not in path.read_text(encoding="utf-8-sig"):
        print(f"WARNING: {path.name} does not declare '{stamp}'.")
        print("         It is out of date for this release; players may see stale option docs.")
    return path


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
    refresh_template()
    stage_release()
    out, files = zip_release()
    shutil.rmtree(STAGE)
    size_mb = out.stat().st_size / (1024 * 1024)
    print(f"Built {out}  (v{PKG_VERSION}, {files} files, {size_mb:.1f} MB)")


if __name__ == "__main__":
    main()
