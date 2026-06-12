# Silly Tank Hill War On PythonAnywhere

The release pipeline copies the complete uncompressed Unity WebGL build into
`webgl/` and creates `tank_game_pythonanywhere.zip` at the repository root.

Upload and extract the ZIP into the PythonAnywhere Flask application directory,
configure the WSGI file to import `app` from `app.py`, then add static mappings:

| URL | Directory |
| --- | --- |
| `/Build/` | `<pythonanywhere-app>/webgl/Build` |
| `/TemplateData/` | `<pythonanywhere-app>/webgl/TemplateData` |

Verify `/health`, `/`, and the generated `.wasm` URL after restarting the app.

