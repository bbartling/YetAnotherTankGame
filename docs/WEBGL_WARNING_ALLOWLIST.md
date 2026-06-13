# WebGL Warning Allowlist

This allowlist is temporary and applies to the clean WebGL validation build
completed on 2026-06-12 with Unity 6000.4.6f1.

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

- Clean WebGL build completed: `2026-06-13T01:39:36.6369392Z`
- Result: succeeded
- Build errors: 0
- Build warnings: 34
- Output size: 61,447,699 bytes
- Unity console errors after build: 0
- PythonAnywhere ZIP size: 18,901,608 bytes
- PythonAnywhere ZIP SHA-256:
  `9651DAA1CF1690B308A451814387852CB4C62263A8AF28769B97764CAFC76736`
- No Sentis/Inference shader warnings after removing the unused
  `com.unity.ai.assistant` and `com.unity.ai.inference` packages.
