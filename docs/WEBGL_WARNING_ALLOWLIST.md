# WebGL Warning Allowlist

This allowlist is temporary and applies to the clean WebGL validation build
completed on 2026-06-13 with Unity 6000.4.6f1.

## Allowed Until Migration

- 34 `CS0618` warnings from legacy gameplay scripts using Unity object lookup
  APIs deprecated in Unity 6. These are compile-time compatibility warnings,
  not WebGL runtime or shader failures. Replace them during the owning gameplay
  checkpoints without changing lookup semantics.
- 10 Unity MCP websocket keep-alive warnings emitted while the editor was busy
  with the 23-minute build. The build job reconnected and reported success with
  zero errors. These warnings are tooling transport warnings and are not
  included in the player.
- Unity's Emscripten toolchain logs a Firefox-before-version-149 WebGPU warning.
  The locked targets are Chrome on Windows and Safari on Mac, and the build does
  not depend on Firefox WebGPU.
- Unity logs six `Shader Unsupported` messages while stripping non-WebGL URP
  terrain subshaders (`Terrain/Lit` and its hidden base pass). The WebGL build
  succeeds, retains the terrain scene/data, and reports no shader warning in
  the `BuildReport` warning count beyond the 34 compile-time deprecations.
  This is platform variant stripping, not a missing runtime material. Recheck
  visually in Chrome and Safari for every release.

## Not Allowed

- Shader unsupported warnings other than the explicitly documented URP terrain
  platform-variant stripping above.
- Missing references.
- `NullReferenceException`, `ArgumentException`, or other gameplay exceptions.
- Failed build steps.
- Chrome or Safari compatibility warnings.

## Evidence

- Clean WebGL build completed: `2026-06-13T17:55:37Z`
- Result: succeeded
- Build errors: 0
- Build warnings: 34
- Output size: 61,487,783 bytes
- Unity console errors after build: 0
- PythonAnywhere ZIP size: 18,914,770 bytes
- PythonAnywhere ZIP SHA-256:
  `9836D9B8A7D9AC771B9F2D589C56C84291471677D0D0C31BE698A58BCD410D53`
- No Sentis/Inference shader warnings after removing the unused
  `com.unity.ai.assistant` and `com.unity.ai.inference` packages.
