# Slow Silly Tank Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a slow, tactical, silly-looking tank game with deliberate enemy operations, modular Blender models, visible fire/smoke/destruction, bounded audio/effects, and a validated WebGL/PythonAnywhere release.

**Architecture:** Preserve `Practice.unity` and its locked projectile-camera, ballistics, scope, crater, and deployment systems. Replace aggressive monolithic behavior incrementally with testable policy components, damage-state components, model assemblies, and pooled effects while retaining compatibility adapters in existing scripts.

**Tech Stack:** Unity 6000.4.6f1, C#, Unity Test Framework, Unity MCP, Blender MCP, URP, PowerShell, Flask, WebGL.

---

## Operating Contract

- Work only on `feature/silly-war-thunder-tank-overhaul`.
- Before scene mutations, confirm Unity is idle and `Practice.unity` has no conflicting unsaved edits.
- When Unity reports an external scene change and Git is clean, reload the committed on-disk scene.
- Classify every test result before processing it. Default to `SIMPLE`; use `COMPLEX` only for ambiguous, timing, performance, security, or cross-component failures.
- After imports, compilation, tests, or builds, wait patiently and inspect editor state plus console before deciding success or failure.
- End every checkpoint with zero compile errors, zero console errors, focused tests, manual locked-feature checks, and a small commit.

## Checkpoint 1: Tactical Pacing And Enemy Operations

**Files:**
- Create: `Assets/Scripts/AI/TankPerception.cs`
- Create: `Assets/Scripts/AI/TankCombatBrain.cs`
- Create: `Assets/Scripts/AI/TankPathingBrain.cs`
- Create: `Assets/Tests/PlayMode/EnemyTankOperationsPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/TankCorePolicyPlayModeTests.cs`
- Modify: `Assets/Scripts/Tank/TankDriveController.cs`
- Modify: `Assets/Scripts/EnemyTankAI.cs`
- Modify: `Assets/Scripts/EnemyTankSpawner.cs`
- Modify: `Assets/Scripts/GameplayTestApi.cs`

- [x] Tighten `TankMovement_SlowHeavyNotRaceCar` to require forward speed `<= 2.75 m/s`, reverse speed `<= 1.25 m/s`, acceleration `>= 5 seconds`, and high-speed steering multiplier `< 0.45`.
- [x] Add failing policy tests requiring enemy cruise speed `<= 2.5 m/s`, preferred standoff `>= 160m`, retreat distance `>= 90m`, reload `>= 6 seconds`, and no combat strafing.
- [x] Run PlayMode tests; expected classification: `SIMPLE`, expected result: failures against current fast defaults.
- [x] Tune `TankDriveController` defaults and compatibility values in `TankController`.
- [x] Implement `TankPerception` for LOS, sight memory, movement/noise awareness, and target visibility.
- [x] Implement `TankCombatBrain` with `Patrol`, `Suspicious`, `Spotting`, `HaltToAim`, `Firing`, `Reloading`, `Repositioning`, `Retreating`, `Disabled`, and `Destroyed`.
- [x] Implement `TankPathingBrain` with capped movement, standoff steering, slope rejection, and stuck recovery.
- [x] Adapt `EnemyTankAI` to delegate decisions to the new components without removing its existing health, projectile, crater, and death hooks.
- [x] Remove spawner difficulty scaling that increases movement speed or creates sub-six-second reloads.
- [x] Expose current state, speed, LOS, and distance through `GameplayTestApi`.
- [x] Run focused PlayMode tests; require all passes.
- [x] Manually verify player lobbed shot and projectile-camera return still work.
- [x] Commit: `feat: slow combat and add deliberate enemy operations`.

## Checkpoint 2: Modular Damage States And Bounded Effects

**Files:**
- Create: `Assets/Scripts/Damage/Damageable.cs`
- Create: `Assets/Scripts/Damage/DamageZone.cs`
- Create: `Assets/Scripts/Damage/DamageStateController.cs`
- Create: `Assets/Scripts/Damage/DestructibleModelSwap.cs`
- Create: `Assets/Scripts/Performance/EffectPool.cs`
- Create: `Assets/Scripts/Effects/BattlefieldEffectController.cs`
- Create: `Assets/Tests/PlayMode/DamageStatePlayModeTests.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Scripts/EnemyTankAI.cs`
- Modify: `Assets/Scripts/CastleDamageReceiver.cs`
- Modify: `Assets/Scripts/BreakableTree.cs`
- Modify: `Assets/Scripts/CraterTerrain.cs`
- Modify: `Assets/Scripts/ProjectileCameraController.cs`

- [x] Write failing tests for tank state progression, track/turret/barrel penalties, castle state progression, tree knockdown, and bounded effect counts.
- [x] Run tests; expected classification: `SIMPLE`, expected result: missing components and failed state assertions.
- [x] Implement normalized health and damage-zone contracts while preserving legacy damage entry points.
- [x] Implement `Intact`, `Smoking`, `BurningDisabled`, and `Wrecked` tank states.
- [x] Implement `Intact`, `Cracked`, `HeavilyDamaged`, and `Collapsed` castle states.
- [x] Implement bounded pooled smoke, fire, explosion, stone, dirt, metal, and wood effects.
- [x] Preserve terrain crater deformation and add an active-crater limit.
- [x] Run focused tests and inspect scene effect counts during repeated impacts.
- [x] Commit: `feat: add bounded modular battlefield damage states`.

## Checkpoint 3: Blender Model Production And Prefab Assembly

**Files:**
- Create: `BlenderSource/README.md`
- Create: `BlenderSource/Tanks/SillyPlayerTank.blend`
- Create: `BlenderSource/Tanks/SillyEnemyScout.blend`
- Create: `BlenderSource/Tanks/SillyEnemyStandard.blend`
- Create: `BlenderSource/Tanks/SillyEnemyCommander.blend`
- Create: `BlenderSource/Castle/SillyCastleKit.blend`
- Create: `BlenderSource/Turrets/SillyCastleTurrets.blend`
- Create: `BlenderSource/Trees/SillyTreeKit.blend`
- Create: `Assets/Resources/Models/Tanks/`
- Create: `Assets/Resources/Models/Castle/`
- Create: `Assets/Resources/Models/Turrets/`
- Create: `Assets/Resources/Models/Trees/`
- Create: `Assets/Editor/ModelImportValidation.cs`
- Create: `Assets/Tests/EditMode/Editor/ModelImportValidationTests.cs`
- Modify: player, enemy, castle, turret, and tree prefabs used by `Practice.unity`

- [x] Verify Blender MCP connectivity and save source files before export.
- [x] Write failing EditMode tests for required source/runtime assets, non-zero renderer bounds, required pivots, and damage-state references.
- [x] Create the player tank with grumpy eyes, oversized commander helmet/dome, chunky barrel, exaggerated tracks, comic armor plates, and wobbling antenna.
- [x] Create scout, standard, and commander enemy silhouettes with separate hull, turret, barrel, and tracks.
- [x] Create modular intact/cracked/damaged/collapsed castle pieces and separate castle turrets.
- [x] Create standing, damaged, fallen, and stump tree variants.
- [x] Apply transforms, use one-meter scale, and export predictable runtime FBX assets.
- [x] Assemble prefabs with gameplay pivots, colliders, damage states, and simple WebGL materials.
- [x] Add loud model-validation errors; required models must never silently fall back to primitives.
- [x] Run model tests, inspect renderer bounds, and capture player/enemy/castle screenshots.
- [x] Commit: `feat: add silly modular Blender battlefield models`.

## Checkpoint 4: Animation, Smoke, Fire, Explosions, And Audio

**Files:**
- Create: `Assets/Scripts/Visual/TankVisualAnimator.cs`
- Create: `Assets/Scripts/Visual/DestructionAnimator.cs`
- Create: `Assets/Scripts/Audio/BattlefieldAudioLimiter.cs`
- Create: `Assets/Scripts/Audio/ProjectileAudioController.cs`
- Create: `Assets/Scripts/Audio/ImpactAudioController.cs`
- Create: `Assets/Scripts/Audio/EnemyAudioController.cs`
- Create: `Assets/Tests/PlayMode/BattlefieldPresentationPlayModeTests.cs`
- Modify: `Assets/Scripts/Tank/TankAudioController.cs`
- Modify: `Assets/Scripts/CombatSoundSlots.cs`
- Modify: assembled prefabs and `Practice.unity`

- [x] Write failing tests for recoil, antenna wobble, track motion, burning-state effects, castle collapse animation, engine strain audio, proximity audio, and capped loud one-shots.
- [x] Implement transform-based recoil, hatch/antenna motion, track visuals, tree fall, and castle collapse.
- [x] Wire pooled smoke, fire, explosions, and material-specific impacts to damage events.
- [x] Implement replaceable audio slots for engine idle/strain, tracks, turret, barrel, reload, cannon, shell, impacts, fire, tree, and castle.
- [x] Enforce audio voice limits and distance rolloff.
- [x] Run presentation tests and manually inspect missing-reference warnings.
- [x] Commit: `feat: add animated damage effects and battlefield audio`.

## Checkpoint 5: Full Validation And Release

**Files:**
- Create: `Assets/Tests/PlayMode/SlowSillyTankReleasePlayModeTests.cs`
- Create: `Docs/TESTING_AND_VALIDATION.md`
- Create: `Docs/WEBGL_PERFORMANCE_BUDGET.md`
- Modify: `Assets/Scripts/GameplayTestApi.cs`
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `Docs/TANK_GAME_CURRENT_STATE_AUDIT.md`
- Modify: `Docs/GAMEPLAY_DESIGN_LOCK.md`
- Generate: `Builds/WebGL/`
- Generate: `pythonanywhere_flask/webgl/`
- Generate: `tank_game_pythonanywhere.zip`

- [ ] Add deterministic release tests proving slow player movement, slope struggle, aligned scope, projectile-camera return, enemy standoff/no-ram behavior, visible damage states, castle collapse, tree knockdown, and reachable win state.
- [ ] Run all EditMode and PlayMode tests; classify each result before processing and require zero failures.
- [ ] Inspect Unity console and require zero compile/runtime errors.
- [ ] Measure active effects, debris, craters, enemies, and build size against the WebGL budget.
- [ ] Build WebGL using patient wait cycles and inspect build plus Editor logs.
- [ ] Verify PythonAnywhere deployment copy, manifest, ZIP contents, ZIP size, and SHA-256.
- [ ] Perform Chrome smoke validation and document Safari direct-URL checks.
- [ ] Update locked docs with final values and evidence.
- [ ] Commit: `release: validate slow silly tank operations overhaul`.
