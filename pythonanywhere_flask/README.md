# WebGL + Flask host (`pythonanywhere_flask`)

## Unity: where to build WebGL

1. Open this project in Unity.
2. **File → Build Settings…**
3. Platform: **WebGL** (Switch Platform if needed).
4. Open Scene: `Assets/Scenes/Practice.unity` (only scene needed).
5. **Build** (or Build And Run).
6. Output folder must be:

```text
<repo>/pythonanywhere_flask/webgl
```

That folder should contain `index.html`, `Build/`, and `TemplateData/` (Unity overwrites them).

Compression tip: in **Player Settings → WebGL → Publishing Settings**, try **Gzip** or **Disabled** first for local Flask testing (Brotli sometimes needs extra server config).

## Test locally (Flask)

```powershell
cd pythonanywhere_flask
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
flask --app flask_app run --host 127.0.0.1 --port 5000
```

Open: http://127.0.0.1:5000/

You can also open `webgl/index.html` via a tiny static server, but Flask matches PythonAnywhere.

## One portable zip (PythonAnywhere + Linux Mint / Buildroot)

```powershell
cd pythonanywhere_flask
python package_webgl_zip.py
```

Creates:

- `dist/cannon-webgl-<timestamp>.zip`
- `dist/cannon-webgl-latest.zip`
- `dist/cannon-webgl-latest.zip.sha256`

Zip includes `webgl/`, Flask host, `serve_linux.sh`, `LINUX_MINT_HANDOFF.md`, and `AGENTS.md`.

### PythonAnywhere

Upload either zip → unzip → WSGI:

```python
# typically in /var/www/<you>_pythonanywhere_com_wsgi.py
import sys
path = "/home/<you>/CannonPhysicsSim/pythonanywhere_flask"  # or wherever you unzipped
if path not in sys.path:
    sys.path.insert(0, path)

from flask_app import application
```

Static mapping is optional; this Flask app serves `/` and all WebGL assets itself.

### Linux Mint (USB / dual-boot — preferred while Windows virt is broken)

```bash
unzip -o cannon-webgl-latest.zip -d pythonanywhere_flask
cd pythonanywhere_flask
./serve_linux.sh
```

See `LINUX_MINT_HANDOFF.md` for Buildroot next steps. Same zip’s `webgl/` folder is what you embed in the rootfs.
