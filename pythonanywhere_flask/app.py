import mimetypes
from pathlib import Path

from flask import Flask, abort, send_from_directory


BASE_DIR = Path(__file__).resolve().parent
WEBGL_DIR = BASE_DIR / "webgl"

mimetypes.add_type("application/wasm", ".wasm")
mimetypes.add_type("application/octet-stream", ".data")
mimetypes.add_type("text/javascript", ".js")

app = Flask(__name__)


@app.after_request
def add_unity_headers(response):
    response.headers["X-Content-Type-Options"] = "nosniff"
    return response


@app.route("/")
def index():
    return send_from_directory(WEBGL_DIR, "index.html")


@app.route("/health")
def health():
    return {"status": "ok", "game": "Silly Tank Hill War"}


@app.route("/<path:filename>")
def webgl_file(filename):
    requested = (WEBGL_DIR / filename).resolve()
    if WEBGL_DIR.resolve() not in requested.parents or not requested.is_file():
        abort(404)
    return send_from_directory(WEBGL_DIR, filename)


if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5000, debug=True)

