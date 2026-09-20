"""Serve the Unity WebGL build for local testing and PythonAnywhere.

Local:
  cd pythonanywhere_flask
  python -m venv .venv
  .venv\\Scripts\\activate          # Windows
  pip install -r requirements.txt
  flask --app flask_app run --host 127.0.0.1 --port 5000

Then open http://127.0.0.1:5000/
"""

from __future__ import annotations

import os
from pathlib import Path

from flask import Flask, send_from_directory, abort

WEBGL_ROOT = Path(__file__).resolve().parent / "webgl"

app = Flask(__name__)


@app.get("/")
def index():
    index_path = WEBGL_ROOT / "index.html"
    if not index_path.is_file():
        abort(404, description="WebGL build missing. Build to pythonanywhere_flask/webgl/")
    return send_from_directory(WEBGL_ROOT, "index.html")


@app.get("/<path:asset_path>")
def webgl_assets(asset_path: str):
    """Serve Build/, TemplateData/, and any other WebGL files."""
    target = (WEBGL_ROOT / asset_path).resolve()
    if not str(target).startswith(str(WEBGL_ROOT.resolve())):
        abort(404)
    if not target.is_file():
        abort(404)
    return send_from_directory(WEBGL_ROOT, asset_path)


# PythonAnywhere WSGI entry: from flask_app import app as application
application = app


if __name__ == "__main__":
    port = int(os.environ.get("PORT", "5000"))
    app.run(host="127.0.0.1", port=port, debug=False)
