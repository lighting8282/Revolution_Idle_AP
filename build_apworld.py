"""Package the Revolution Idle world into a distributable .apworld.

An .apworld is just a zip whose single top-level directory is the package folder
(revolution_idle/), containing all the .py files and data. Drop the result into
Archipelago's `custom_worlds/` (or `worlds/`) folder to install.
"""
import zipfile
from pathlib import Path

ROOT = Path(__file__).parent
PKG_DIR = ROOT / "apworld" / "revolution_idle"
DIST = ROOT / "dist"
PKG_NAME = "revolution_idle"

EXCLUDE_DIRS = {"__pycache__", ".pytest_cache"}
EXCLUDE_SUFFIXES = {".pyc", ".pyo"}


def main() -> None:
    DIST.mkdir(exist_ok=True)
    # Read version from archipelago.json for the filename, best-effort.
    version = "0.0.0"
    manifest = PKG_DIR / "archipelago.json"
    if manifest.exists():
        import json
        version = json.loads(manifest.read_text()).get("world_version", version)

    out = DIST / f"{PKG_NAME}.apworld"
    count = 0
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(PKG_DIR.rglob("*")):
            if any(part in EXCLUDE_DIRS for part in path.parts):
                continue
            if path.suffix in EXCLUDE_SUFFIXES:
                continue
            if path.is_file():
                # Arcname must be prefixed with the package dir name.
                arc = Path(PKG_NAME) / path.relative_to(PKG_DIR)
                zf.write(path, arc.as_posix())
                count += 1

    print(f"Built {out}  (v{version}, {count} files)")
    print("Install: copy into <Archipelago>/custom_worlds/")


if __name__ == "__main__":
    main()
