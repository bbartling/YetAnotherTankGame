# WebGL Performance Budget

Target: playable in Chrome on Windows and Safari on Mac. No mobile claim is made.

## Runtime Limits

- Player forward speed: at most `2.75 m/s`.
- Enemy cruise speed: at most `2.5 m/s`.
- Enemy combat uses standoff behavior rather than high-speed pursuit.
- Debris: bounded by battlefield effect limits.
- Impact marks, explosions, smoke/fire effects, projectiles, and craters: bounded or temporary.
- Enemy audio: linear rolloff with maximum audible distance at most `60 m`.
- Loud one-shots: voice-limited.

## Asset And Rendering Rules

- Use low-poly Blender-authored models and simple Unity materials.
- Keep model pivots and damage variants separate without runtime mesh generation.
- Avoid heavy post-processing, HDR, MSAA, WebGL threads, and expensive shaders.
- Prefer compressed/replaceable audio; generated fallbacks use an `11025 Hz` mono sample rate.
- Keep active enemies, real-time lights, shadow casters, debris, and craters limited.

## Build Budget Evidence

Each release records file sizes and SHA-256 values in:

- `Builds/WebGL_BUILD_MANIFEST.json`
- `pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json`

Review manifest total size and warnings before deployment. WebGL warnings block release unless documented in `Docs/WEBGL_WARNING_ALLOWLIST.md`.
