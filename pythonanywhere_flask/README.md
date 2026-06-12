# Silly Tank Hill War On PythonAnywhere

The release pipeline copies the complete uncompressed Unity WebGL build into
`webgl/` and creates `tank_game_pythonanywhere.zip` at the repository root.

Upload and extract the ZIP into the PythonAnywhere Flask application directory,
configure `/var/www/bensapi_pythonanywhere_com_wsgi.py` to import `app` from
`flask_app.py`, then add static mappings:

| URL | Directory |
| --- | --- |
| `/Build/` | `<pythonanywhere-app>/webgl/Build` |
| `/TemplateData/` | `<pythonanywhere-app>/webgl/TemplateData` |

Verify `/health`, `/`, and the generated `.wasm` URL after restarting the app.

The WSGI file must contain:

```python
import sys

project_home = "/home/bensApi/mysite"
if project_home not in sys.path:
    sys.path = [project_home] + sys.path

from flask_app import app as application
```
