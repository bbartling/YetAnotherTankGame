# Silly War Thunder Tank Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the existing Unity tank prototype into an AI-maintainable, silly-but-tactical tank game without regressing its projectile camera, lobbed shells, craters, scope concept, enemy tanks, or destruction.

**Architecture:** Use an incremental strangler refactor. Preserve the playable `Practice` scene while extracting shared ballistics, tank controls, enemy perception/combat, damage, audio, testing, and deployment into focused components. Every checkpoint ends with compilation, console inspection, relevant tests, manual feature checks, and a small commit.

**Tech Stack:** Unity 6000.4.6f1, C#, URP 17.4, Unity Test Framework, Unity MCP, Blender MCP, PowerShell, Flask, WebGL, PythonAnywhere.

---

## Operating Rules

- Work on `feature/silly-war-thunder-tank-overhaul`.
- Before each checkpoint, read `AGENTS.md` and `Docs/GAMEPLAY_DESIGN_LOCK.md`.
- Do not remove a legacy behavior until its replacement passes the matching validation.
- Use Unity MCP batches for independent inspections.
- For imports, compilation, tests, and WebGL builds, use patient checks with waits up to 20 minutes per cycle.
- Treat console errors and WebGL warnings as blockers unless an explicit allowlist documents evidence.
- Keep each checkpoint independently playable and commit only after fresh verification.

## Checkpoint A: Audit And Non-Regression Lock

**Files:**
- Create: `AGENTS.md`
- Create: `Docs/TANK_GAME_CURRENT_STATE_AUDIT.md`
- Create: `Docs/GAMEPLAY_DESIGN_LOCK.md`
- Create: `docs/superpowers/plans/2026-06-12-silly-war-thunder-tank-overhaul.md`

- [x] Inspect repository, scene, scripts, prefabs, build settings, console, Unity MCP, Blender MCP tool availability, and the HoneyMan reference process.
- [x] Create branch `feature/silly-war-thunder-tank-overhaul`.
- [x] Record current behavior, known defects, and locked non-regression promises.
- [x] Verify Unity remains idle, scene remains clean, console has zero errors, and git diff contains documentation only.
- [x] Commit with `docs: lock tank overhaul baseline and execution plan`.

## Checkpoint B: WebGL And PythonAnywhere Pipeline

**Files:**
- Create: `Assets/Editor/TankWebGLBuildPipeline.cs`
- Create: `Assets/Tests/EditMode/Editor/TankBuildPipelineEditModeTests.cs`
- Create: `pythonanywhere_flask/flask_app.py`
- Create: `pythonanywhere_flask/requirements.txt`
- Create: `pythonanywhere_flask/README.md`
- Create: `pythonanywhere_flask/webgl/.gitkeep`
- Create: `scripts/build_webgl_pythonanywhere.ps1`
- Create: `Docs/WEBGL_PYTHONANYWHERE_DEPLOY.md`
- Modify: `.gitignore`
- Modify: `ProjectSettings/ProjectSettings.asset`

- [x] Write EditMode tests asserting the build script, Flask folder, safe WebGL settings, build scene, and manifest contract.
- [x] Run EditMode tests and confirm the new tests fail.
- [x] Implement `TankWebGLBuildPipeline.ConfigureWebGL`, `BuildWebGL`, manifest creation, deployment copy, required-file validation, and ZIP creation.
- [x] Implement Flask MIME handling and direct static WebGL routes.
- [x] Implement the patient PowerShell test/build/deploy wrapper with logs under `Logs/`.
- [x] Run EditMode tests and confirm they pass.
- [x] Configure WebGL and inspect Unity console for errors and blocking warnings.
- [x] Build WebGL using patient wait cycles, then verify:
  - `Builds/WebGL/index.html`
  - `Builds/WebGL/Build/*.loader.js`
  - `Builds/WebGL/Build/*.framework.js`
  - `Builds/WebGL/Build/*.wasm`
  - `Builds/WebGL/Build/*.data`
  - `Builds/WebGL_BUILD_MANIFEST.json`
  - `pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json`
  - `tank_game_pythonanywhere.zip`
- [x] Commit with `build: add reproducible WebGL PythonAnywhere pipeline`.

## Checkpoint C: Slow Heavy Tank Core

**Files:**
- Create: `Assets/Scripts/Tank/TankDriveController.cs`
- Create: `Assets/Scripts/Tank/TankSuspensionVisual.cs`
- Create: `Assets/Scripts/Tank/TankTurretController.cs`
- Create: `Assets/Scripts/Tank/TankAimController.cs`
- Create: `Assets/Scripts/Tank/TankAudioController.cs`
- Create: `Assets/Scripts/Camera/TankOrbitCamera.cs`
- Create: `Assets/Tests/PlayMode/TankMovementPlayModeTests.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Scenes/Practice.unity`

- [x] Write PlayMode tests for forward movement, locked maximum speed, uphill speed penalty, maximum climb slope, slope visual alignment, independent turret yaw, and stable grounding.
- [x] Run tests and confirm they fail against the current controller.
- [x] Introduce compatibility-facing focused controllers while keeping `TankController` as the temporary scene adapter.
- [x] Tune locked defaults: slow forward pace, slower reverse, heavy steering, no sprint, slope traction loss, steep-slope refusal, and engine strain output.
- [x] Add a collision-aware third-person orbit camera without changing projectile-camera ownership.
- [x] Run movement tests and a five-minute simulated terrain drive audit.
- [x] Verify projectile firing and camera still work before commit.
- [x] Commit with `feat: add slow heavy terrain-aware tank controls`.

## Checkpoint D: Ballistics, Scope, Rangefinder, Projectile Camera

**Files:**
- Create: `Assets/Scripts/Weapons/TankBallistics.cs`
- Create: `Assets/Scripts/Weapons/TankScopeController.cs`
- Create: `Assets/Scripts/Weapons/RangeFinder.cs`
- Create: `Assets/Scripts/UI/TankHudController.cs`
- Create: `Assets/Scripts/Camera/ProjectileCameraDirector.cs`
- Create: `Assets/Tests/EditMode/Editor/TankBallisticsEditModeTests.cs`
- Create: `Assets/Tests/PlayMode/TankAimingPlayModeTests.cs`
- Modify: `Assets/Scripts/ProjectileCameraController.cs`
- Modify: `Assets/Scripts/SniperRangeFinder.cs`
- Modify: `Assets/Scripts/SniperZoom.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Prefabs/CannonBall.prefab`
- Modify: `Assets/Scenes/Practice.unity`

- [x] Write deterministic ballistic tests at 25m, 50m, and 100m plus camera transition validation hooks.
- [x] Run tests and confirm failures expose current aim/range drift.
- [x] Implement one shared ballistic calculation used by shell launch, scope, rangefinder, and tests.
- [x] Align scope ray, target point, barrel solution, range, elevation, and time of flight.
- [x] Extract camera transition control while preserving tracking, impact linger, automatic return, and manual cancel.
- [x] Run tests and manually verify lob aim and camera return.
- [x] Commit with `feat: align tactical scope ballistics and projectile camera`.

## Checkpoint E: Enemy Standoff And Perception

**Files:**
- Create: `Assets/Scripts/AI/TankPerception.cs`
- Create: `Assets/Scripts/AI/TankCombatBrain.cs`
- Create: `Assets/Scripts/AI/TankPathingBrain.cs`
- Create: `Assets/Tests/PlayMode/EnemyTankAIPlayModeTests.cs`
- Modify: `Assets/Scripts/EnemyTankAI.cs`
- Modify: `Assets/Scripts/EnemyTankSpawner.cs`
- Modify: `Assets/Prefabs/EnemyTank.prefab`

- [ ] Write PlayMode tests for LOS spotting, hidden player, standoff distance, ranged fire, no early ram, repositioning, and stuck recovery.
- [ ] Run tests and confirm current rush behavior fails the contract.
- [ ] Implement explicit states: Idle, Patrol, Suspicious, SpottedPlayer, TakingAim, Firing, Relocating, Retreating, Disabled, Destroyed.
- [ ] Add LOS, movement/noise detection, memory, standoff steering, slope avoidance, and stuck recovery.
- [ ] Run tests and a deterministic 60-second no-ram scenario.
- [ ] Commit with `feat: add enemy perception and tactical standoff combat`.

## Checkpoint F: Damage, Destruction, Trees, Craters

**Files:**
- Create: `Assets/Scripts/Damage/Damageable.cs`
- Create: `Assets/Scripts/Damage/DamageZone.cs`
- Create: `Assets/Scripts/Damage/DestructibleModelSwap.cs`
- Create: `Assets/Scripts/Damage/BreakApartOnDeath.cs`
- Create: `Assets/Scripts/Environment/TreeDamageController.cs`
- Create: `Assets/Scripts/Environment/TerrainCraterController.cs`
- Create: `Assets/Scripts/Performance/EffectPool.cs`
- Create: `Assets/Tests/PlayMode/DamageDestructionPlayModeTests.cs`
- Modify: `Assets/Scripts/CraterTerrain.cs`
- Modify: `Assets/Scripts/BreakableTree.cs`
- Modify: `Assets/Scripts/CastleDamageReceiver.cs`
- Modify: `Assets/Scripts/ProjectileCameraController.cs`

- [ ] Write tests for tank zones, tree knockdown, castle damage state, turret destruction, crater creation, and bounded debris.
- [ ] Run tests and record current behavior.
- [ ] Introduce common damage contracts and limited/poolable visual effects.
- [ ] Preserve terrain mesh craters while adding an active-crater budget and WebGL-safe update policy.
- [ ] Run destruction tests and inspect allocations/debris counts.
- [ ] Commit with `feat: add bounded destructible battlefield damage`.

## Checkpoint G: Blender Models And Prefabs

**Files:**
- Create: `BlenderSource/README.md`
- Create: `BlenderSource/Tanks/`
- Create: `BlenderSource/Trees/`
- Create: `BlenderSource/Castle/`
- Create: `BlenderSource/Turrets/`
- Create: `BlenderSource/Props/`
- Create: `Assets/Resources/Models/Tanks/`
- Create: `Assets/Resources/Models/Trees/`
- Create: `Assets/Resources/Models/Castle/`
- Create: `Assets/Resources/Models/Turrets/`
- Create: `Assets/Resources/Models/Props/`
- Create: `Assets/Editor/ModelImportValidation.cs`
- Create: `Assets/Tests/EditMode/Editor/ModelImportEditModeTests.cs`
- Modify: player, enemy, castle, turret, tree, and projectile prefabs

- [ ] Verify Blender MCP connection before editing or generating any Blender source.
- [ ] Create a small player-tank prototype first and validate scale, pivots, renderer bounds, and WebGL material usage.
- [ ] Create low-poly player tank, two enemy variants, castle/turret chunks, trees, and shells using the documented source/runtime layout.
- [ ] Implement required-model validation and loud missing-model errors.
- [ ] Assemble Unity prefabs with separate hull, turret, barrel, and track references.
- [ ] Run model import tests, gameplay smoke tests, and renderer-bounds audit.
- [ ] Commit with `feat: add tracked silly low-poly tank battlefield models`.

## Checkpoint H: Audio System

**Files:**
- Create: `Assets/Scripts/Audio/TankAudioController.cs`
- Create: `Assets/Scripts/Audio/ProjectileAudioController.cs`
- Create: `Assets/Scripts/Audio/ImpactAudioController.cs`
- Create: `Assets/Scripts/Audio/EnemyAudioController.cs`
- Create: `Assets/Scripts/Audio/MusicAmbienceController.cs`
- Create: `Assets/Tests/PlayMode/TankAudioPlayModeTests.cs`
- Modify: relevant tank, projectile, enemy, tree, castle, and scene audio wiring

- [ ] Write tests for required clip references, engine response, strain response, proximity audio, and capped one-shots.
- [ ] Implement focused audio controllers with WebGL-friendly clips and distance rolloff.
- [ ] Run audio tests and manually inspect console for missing-clip warnings.
- [ ] Commit with `feat: add responsive bounded battlefield audio`.

## Checkpoint I: Gameplay Loop And Validation Harness

**Files:**
- Create: `Assets/Scripts/Game/TankGameManager.cs`
- Create: `Assets/Scripts/Game/TankObjectiveManager.cs`
- Create: `Assets/Scripts/Game/TankSpawnManager.cs`
- Create: `Assets/Scripts/Testing/TankGameplayTestApi.cs`
- Create: `Assets/Tests/EditMode/Editor/TankProjectRobustnessEditModeTests.cs`
- Create: `Assets/Tests/PlayMode/TankGameplayRobustnessPlayModeTests.cs`
- Create: `Docs/TESTING_AND_VALIDATION.md`
- Create: `Docs/WEBGL_PERFORMANCE_BUDGET.md`
- Modify: `Assets/Scripts/GameplayTestApi.cs`
- Modify: `Assets/Scripts/BattlefieldDirector.cs`
- Modify: `Assets/Scripts/MenuManager.cs`
- Modify: `Assets/Scenes/Practice.unity`

- [ ] Add locked public validation hooks without coupling them to normal gameplay behavior.
- [ ] Implement deterministic beatable smoke scenario and restart validation.
- [ ] Add project robustness tests covering assets, scenes, WebGL settings, docs, build pipeline, and references.
- [ ] Add PlayMode tests covering movement, slope, turret, scope, projectile camera, enemy AI, damage, and win state.
- [ ] Add debug-only FPS/entity/camera/version overlay.
- [ ] Run complete EditMode and PlayMode suites.
- [ ] Commit with `test: add deterministic tank game validation harness`.

## Checkpoint J: Release Build And Report

**Files:**
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `Docs/TANK_GAME_CURRENT_STATE_AUDIT.md`
- Modify: `Docs/GAMEPLAY_DESIGN_LOCK.md`
- Modify: `Docs/WEBGL_PYTHONANYWHERE_DEPLOY.md`
- Modify: `Docs/TESTING_AND_VALIDATION.md`
- Modify: `Docs/WEBGL_PERFORMANCE_BUDGET.md`
- Generate: `Builds/WebGL/`
- Generate: `Builds/WebGL_BUILD_MANIFEST.json`
- Generate: `pythonanywhere_flask/webgl/`
- Generate: `pythonanywhere_flask/WEBGL_BUILD_MANIFEST.json`
- Generate: `tank_game_pythonanywhere.zip`

- [ ] Run complete EditMode and PlayMode suites with zero failures.
- [ ] Inspect Unity state and console; require zero compile/runtime errors.
- [ ] Build WebGL using patient 20-minute wait cycles and inspect Editor/build logs.
- [ ] Reject or explicitly allowlist every WebGL warning with evidence.
- [ ] Serve Flask locally and perform Chrome browser console/screenshot smoke test when Playwright is available.
- [ ] Write manual Safari checklist for direct PythonAnywhere URL, loading, controls, projectile camera, audio, and three-minute stability.
- [ ] Verify ZIP contents, manifest, size, and SHA-256.
- [ ] Update locked docs with final authoritative values.
- [ ] Commit with `release: validate silly tank WebGL overhaul`.
