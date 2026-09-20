#!/usr/bin/env python3
"""Zip the WebGL tree for PythonAnywhere + Linux Mint / Buildroot (one portable zip).

Usage (from repo root or this folder):
  python pythonanywhere_flask/package_webgl_zip.py

Creates:
  pythonanywhere_flask/dist/cannon-webgl-YYYYMMDD-HHMMSS.zip
  pythonanywhere_flask/dist/cannon-webgl-latest.zip
  pythonanywhere_flask/dist/cannon-webgl-latest.zip.sha256

Same zip: upload to PythonAnywhere, OR copy to Mint USB and unzip there.
"""

from __future__ import annotations

import hashlib
import json
import shutil
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent
WEBGL = ROOT / "webgl"
DIST = ROOT / "dist"
# Files at zip root (next to webgl/)
INCLUDE_ROOT_FILES = (
    "flask_app.py",
    "requirements.txt",
    "WEBGL_BUILD_MANIFEST.json",
    "README.md",
    "LINUX_MINT_HANDOFF.md",
    "serve_linux.sh",
)


def file_sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest().upper()


def write_manifest() -> Path:
    files = []
    total = 0
    for path in sorted(WEBGL.rglob("*")):
        if not path.is_file():
            continue
        rel = path.relative_to(WEBGL).as_posix()
        size = path.stat().st_size
        total += size
        files.append({"path": rel, "sizeBytes": size, "sha256": file_sha256(path)})

    manifest = {
        "generatedUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ"),
        "warningCount": 0,
        "fileCount": len(files),
        "totalSizeBytes": total,
        "files": files,
    }
    out = ROOT / "WEBGL_BUILD_MANIFEST.json"
    out.write_text(json.dumps(manifest, indent=4), encoding="utf-8")
    return out


def write_zip_checksum(zip_path: Path) -> Path:
    digest = file_sha256(zip_path).lower()
    out = zip_path.with_suffix(zip_path.suffix + ".sha256")
    # sha256sum-compatible: "<hash>  <filename>"
    out.write_text(f"{digest}  {zip_path.name}\n", encoding="utf-8")
    return out


def build_zip() -> Path:
    if not (WEBGL / "index.html").is_file():
        raise SystemExit(
            f"Missing {WEBGL / 'index.html'}. In Unity: File > Build Settings > WebGL, "
            f"output folder = pythonanywhere_flask/webgl"
        )

    write_manifest()
    DIST.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    stamped = DIST / f"cannon-webgl-{stamp}.zip"
    latest = DIST / "cannon-webgl-latest.zip"

    with zipfile.ZipFile(stamped, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(WEBGL.rglob("*")):
            if path.is_file():
                zf.write(path, arcname=f"webgl/{path.relative_to(WEBGL).as_posix()}")

        for name in INCLUDE_ROOT_FILES:
            src = ROOT / name
            if src.is_file():
                zf.write(src, arcname=name)

        agents = REPO / "AGENTS.md"
        if agents.is_file():
            zf.write(agents, arcname="AGENTS.md")

    shutil.copy2(stamped, latest)
    checksum = write_zip_checksum(latest)
    # Also checksum the stamped copy for archival
    write_zip_checksum(stamped)

    size_mb = stamped.stat().st_size / (1024 * 1024)
    print(f"Wrote {stamped} ({size_mb:.1f} MB)")
    print(f"Also updated {latest}")
    print(f"Checksum  {checksum}")
    print("PA: upload zip, unzip, WSGI flask_app:application")
    print("Mint: copy zip (+ .sha256), unzip, ./serve_linux.sh")
    return stamped


if __name__ == "__main__":
    build_zip()
