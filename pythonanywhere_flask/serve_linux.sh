#!/usr/bin/env bash
# Serve the WebGL build with Flask on Linux Mint / Ubuntu.
# Run from the unzipped pythonanywhere_flask directory (same layout as the PA zip).
set -euo pipefail
cd "$(dirname "$0")"

if [[ ! -f webgl/index.html ]]; then
  echo "Missing webgl/index.html — unzip cannon-webgl-latest.zip here first." >&2
  exit 1
fi

python3 -m venv .venv
# shellcheck disable=SC1091
source .venv/bin/activate
pip install -q -r requirements.txt

HOST="${HOST:-0.0.0.0}"
PORT="${PORT:-5000}"
echo "Serving WebGL at http://127.0.0.1:${PORT}/  (LAN: http://<this-host>:${PORT}/)"
exec flask --app flask_app run --host "$HOST" --port "$PORT"
