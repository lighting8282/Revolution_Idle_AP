"""Regenerate examples/Revolution Idle.yaml from the built apworld.

The YAML template is Archipelago's own output for our options, so it should never be hand-edited:
doing that lets it drift from the code silently. (It did — before this script existed, the committed
template still declared world version 0.17.0 and was missing option docs added several versions
later.)

Pipeline: build the apworld -> install it into a local Archipelago -> ask Archipelago to generate
option templates -> copy the result back into examples/ and dist/.

Archipelago location: --ap-dir, or the REVIDLE_AP_DIR environment variable, else the first of
AP_DEFAULTS that exists. Needs a real Archipelago install, so it is best-effort: build_release.py
warns and keeps the committed template if this can't run.
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).parent
DIST = ROOT / "dist"
GAME_NAME = "Revolution Idle"
TEMPLATE_NAME = f"{GAME_NAME}.yaml"

AP_DEFAULTS = [
    Path(r"A:\Archipelago"),
    Path(r"C:\ProgramData\Archipelago"),
    Path(os.path.expandvars(r"%LOCALAPPDATA%\Archipelago")),
]

PKG_VERSION = json.loads((ROOT / "apworld" / "revolution_idle" / "archipelago.json").read_text(encoding="utf-8"))["world_version"]


def find_ap(explicit: str | None) -> Path:
    candidates = [Path(explicit)] if explicit else []
    if os.environ.get("REVIDLE_AP_DIR"):
        candidates.append(Path(os.environ["REVIDLE_AP_DIR"]))
    candidates += AP_DEFAULTS
    for c in candidates:
        if (c / "ArchipelagoLauncher.exe").exists():
            return c
    raise SystemExit(
        "Could not find an Archipelago install (looked for ArchipelagoLauncher.exe).\n"
        "Pass --ap-dir <path> or set REVIDLE_AP_DIR."
    )


def apworld_version(path: Path) -> str:
    with zipfile.ZipFile(path) as z:
        name = next(n for n in z.namelist() if n.endswith("archipelago.json"))
        return json.loads(z.read(name).decode("utf-8-sig"))["world_version"]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--ap-dir", help="Path to the Archipelago install")
    args = ap.parse_args()
    ap_dir = find_ap(args.ap_dir)

    built = DIST / "revolution_idle.apworld"
    if not built.exists() or apworld_version(built) != PKG_VERSION:
        print(f"Building apworld v{PKG_VERSION} first...")
        subprocess.run([sys.executable, str(ROOT / "build_apworld.py")], check=True)

    # Install it, so the template is generated from THIS build and not whatever was there before.
    installed = ap_dir / "custom_worlds" / "revolution_idle.apworld"
    installed.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(built, installed)
    print(f"Installed apworld v{apworld_version(installed)} -> {installed}")

    out_dir = ap_dir / "Players" / "Templates"
    generated = out_dir / TEMPLATE_NAME
    before = generated.stat().st_mtime if generated.exists() else 0

    print("Generating option templates (this loads every installed world; takes a minute)...")
    proc = subprocess.run(
        [str(ap_dir / "ArchipelagoLauncher.exe"), "Generate Template Options"],
        capture_output=True, text=True, timeout=600,
    )
    if proc.returncode != 0:
        sys.stderr.write(proc.stdout[-2000:] + proc.stderr[-2000:])
        raise SystemExit(f"Template generation failed (exit {proc.returncode}).")

    if not generated.exists():
        raise SystemExit(f"Archipelago did not produce {generated}.")
    if generated.stat().st_mtime == before:
        raise SystemExit(f"{generated} was not rewritten — is the apworld loading?")

    text = generated.read_text(encoding="utf-8-sig")

    # Guard against shipping a template generated from a stale apworld: Archipelago will happily
    # skip loading one (e.g. if the game name is already registered) and leave the old file in place.
    stamp = f"{GAME_NAME}: {PKG_VERSION}"
    if stamp not in text:
        raise SystemExit(
            f"Generated template does not declare '{stamp}'.\n"
            "Archipelago probably generated it from a different/cached apworld build."
        )

    for dest in [ROOT / "examples" / TEMPLATE_NAME, DIST / TEMPLATE_NAME]:
        dest.parent.mkdir(parents=True, exist_ok=True)
        # Normalise to UTF-8 (no BOM) + LF so the committed copy has a readable diff.
        dest.write_text(text, encoding="utf-8", newline="\n")
        print(f"Wrote {dest}")

    print(f"Template is current for v{PKG_VERSION}.")


if __name__ == "__main__":
    main()
