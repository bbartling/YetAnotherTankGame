# WebGL And PythonAnywhere Deployment

## Build

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build_webgl_pythonanywhere.ps1
```

Set `UNITY_EDITOR_PATH` or pass `-UnityEditorPath` if Unity Hub is not installed
in the default location.

The script runs EditMode tests first, waits in patient 20-minute cycles, builds
WebGL, refreshes the Flask deployment copy, and creates the upload ZIP.

## Required Output

- `Builds/WebGL/`
- `Builds/WebGL_BUILD_MANIFEST.json`
- `pythonanywhere_flask/webgl/`
- `pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json`
- `tank_game_pythonanywhere.zip`
- `Logs/webgl-tests.log`
- `Logs/webgl-build.log`

WebGL output is intentionally uncompressed for direct Chrome, Safari, and
PythonAnywhere compatibility without custom content-encoding headers.

## Warning Policy

WebGL warnings block release unless classified in
`docs/WEBGL_WARNING_ALLOWLIST.md` with evidence. The 2026-06-12 clean validation
build succeeded with zero errors. Its remaining warnings are legacy Unity 6 API
deprecations and Unity MCP transport warnings, not WebGL runtime or shader
warnings.

## PythonAnywhere

1. Upload `tank_game_pythonanywhere.zip`.
2. Extract it into the Flask app directory.
3. Configure `/var/www/bensapi_pythonanywhere_com_wsgi.py`:

```python
import sys

project_home = "/home/bensApi/mysite"
if project_home not in sys.path:
    sys.path = [project_home] + sys.path

from flask_app import app as application
```
4. Add direct static mappings for `/Build/` and `/TemplateData/`.
5. Restart the app.
6. Verify `/health`, `/`, `.wasm`, `.data`, and browser console output.
