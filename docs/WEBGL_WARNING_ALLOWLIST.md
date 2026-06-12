# WebGL Warning Allowlist

This allowlist is temporary and applies to the clean WebGL validation build
completed on 2026-06-12 with Unity 6000.4.6f1.

## Allowed Until Migration

- 35 `CS0618` warnings from legacy gameplay scripts using Unity object lookup
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

## Not Allowed

- Shader unsupported warnings.
- Missing references.
- `NullReferenceException`, `ArgumentException`, or other gameplay exceptions.
- Failed build steps.
- Chrome or Safari compatibility warnings.

## Evidence

- Clean WebGL build job: `build-a7e721e024`
- Result: succeeded
- Build errors: 0
- Build warnings: 45
- Output size: 58.35 MB
- Unity console errors after build: 0
- No Sentis/Inference shader warnings after removing the unused
  `com.unity.ai.assistant` and `com.unity.ai.inference` packages.
