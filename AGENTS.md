# Agent Context: Silly Tank Hill War

This Unity project uses Unity MCP and Blender MCP. Future agents must inspect the live editor, current scene, console, repository state, and this file before making changes.

## LOCKED CURRENT CONFIGURATION - DO NOT REGRESS

This block is the authoritative feature contract. Update it in the same validated change whenever intentionally revising a locked value or behavior.

### Game Identity And Feel

- Working title: `YET ANOTHER TANK GAME` (silly tank hill war).
- The target feel is silly cartoon armored vehicles with deliberate War Thunder-ish combat pacing.
- Player vehicle must follow terrain into craters.
- Player vehicle must lose traction on steep slopes and may realistically tip or roll.
- Player chassis must use heavy, low-center-of-mass physics; sustained rollover ends gameplay.
- Player wheels must rotate around their authored axle with forward/reverse direction preserved.
- Player tank must visually contrast with terrain and retain a readable proper turret silhouette.
- Enemy health bars must show green remaining health over a red damage background with percentage text.
- Shift may provide low gear or aim stabilization, never sprint.
- Main menu must keep three gameplay entries: Driving Practice, Cannon Practice, and WAR.
- `Assets/Scenes/Practice.unity` remains the WAR scene and is the only main-menu path that may start the ranged enemy battle/randomized generated map flow.
- `Assets/Scenes/TankDrivingPractice.unity` is a small fixed indoor-stadium obstacle course for tank handling, climbing, traction, rollover, and future AI route testing.
- `Assets/Scenes/TankDrivingPractice.unity` must lock out turret input, cannon firing, cannon power, machine gun, and sniper scope while preserving tank drive input.
- `Assets/Scenes/TankTargetPractice.unity` is a small fixed tank shooting range for normal cannon fire and right-click scope/rangefinder dialing.
- `Assets/Scenes/TankTargetPractice.unity` preserves drive + turret + cannon + scope; lock out machine gun only.
- Defeat screen must allow gameplay restart with left click.

### Best-Known Shared Driving Settings (LOCKED)

Authoritative baseline commit: [`4b6842e7`](https://github.com/bbartling/YetAnotherTankGame/commit/4b6842e7d37490288f86221949fc410643511e29) ("driving course good enough").
Shooting-range placement baseline: [`78210fb`](https://github.com/bbartling/YetAnotherTankGame/commit/78210fbd9f04f94f8b1a44c38122b4f60b6c0aec).

**Same settings apply to WAR (`Practice`), Driving Practice, and Cannon Practice.** Do not invent separate practice-only physics. Apply via `TankDrivingProfile` + `TankGameplayTuning` + `TankOverdriveSetup` only.

Source of truth files (do not regress without intentional validated change):

- `Assets/Scripts/Tank/TankDrivingProfile.cs`
- `Assets/Scripts/Tank/TankGameplayTuning.cs`
- `Assets/Scripts/Tank/TankDriveController.cs`
- `Assets/Scripts/Tank/WheeledSuspensionController.cs`
- `Assets/Scripts/Tank/TankOverdriveController.cs`
- `Assets/Scripts/TankController.cs` (physics body from `4b6842e7`)

Locked drive profile (`TankDrivingProfile`):

| Setting | Value |
|---|---|
| MaxForwardSpeed | `12` m/s |
| MaxReverseSpeed | `6` m/s |
| AccelerationSeconds | `0.62` |
| BrakingSeconds | `1.1` |
| MinimumUphillSpeedMultiplier | `0.72` |
| TractionLossSlopeDegrees | `44` |
| MaxClimbSlopeDegrees | `60` |
| TrackDriveResponse | `6.5` |
| ForwardAcceleration | `65` |
| ReverseAcceleration | `42` |

Locked chassis / overdrive (`TankGameplayTuning`):

| Setting | Value |
|---|---|
| ChassisMass | `42000` (planted MBT weight; centered low COM) |
| OverdriveHoldSeconds | `3` (hold W to charge) |
| OverdriveSpeedMultiplier | `3 * MassTuningRatio` |
| OverdriveAccelerationMultiplier | `3.5 * MassTuningRatio` |
| OverdriveClimbMultiplier | `3 * MassTuningRatio` |
| OverdriveBurstPush | `1.8` |
| PracticeBasePlanarSpeedCap | `13` |

Locked combat profile (shared turret feel, `TankCombatProfile`):

| Setting | Value |
|---|---|
| TurretYawSpeed | `55` °/s |
| MouseYawDegreesPerSecond | `32` |
| MouseWheelPitchSensitivity | `18` |
| KeyboardPitchSpeed | `24` |

Driving practice scene: restore from `4b6842e7` when physics feel regresses. Do **not** leave `DrivingPracticeAutopilot.autoRunOnPlay = true` in the scene (human drives by default). Autopilot is editor-test only.

Cannon practice: place tank with `TargetRangeTankAnchor` (`hullClearance = 1.05`, rear-lane spawn). Do **not** freeze rigidbody / disable suspension for range placement. Skip `SnapToTerrainClearance` on `TankTargetPractice` so the tank does not float or fall through.

### Locked Overdrive Exhaust Puff

- After speed/climb overdrive unlocks (hold forward until charge completes), emit **2–4 random black smoke puffs** from the tank rear (`ExhaustPoint`, local `0, 0.65, -2.35`) with random sizes and particle counts.
- Smoke only — **no fire**, no continuous plume, no pink default Unity particle material.
- Implemented in `TankOverdriveController.PlayRandomBlackSmokePuffs()` with an explicit black-tinted particle material.
- Do not remove or replace with default pink particles.

### Locked Rollover / OOF

- Practice "TANK OOF!" only when the tank is **fully upside-down** (`transform.up.y < -0.35` and rollover angle ≥ `155°`), not merely steeply tipped on a climb.
- Climb stability: low/forward COM and hard `PreventBackwardTip` — tanks must **never tip over backwards** on climbs (lose traction instead). Do not regress this.

### Locked Weapons And Cameras

- Player cannonballs remain physical, visible, and artillery-like with a lobbed arc.
- Projectile camera remains available after firing player cannonballs.
- Projectile camera follows the shell, shows impact, lingers, and returns to tank view.
- Do not remove the projectile camera unless replacing it with a strictly better tested version.
- Right-click scope/rangefinder must remain and must be aligned with actual aiming.
- Scope/rangefinder must report useful ballistic information.
- Trajectory preview or equivalent aim assistance must remain available.
- Scope/rangefinder must keep the aim-aligned crosshair centered while the range scale/readout sits off to the right side.

### Locked Enemy Behavior

- Enemy tanks remain active ranged opponents.
- Enemy tanks must spot and engage from distance.
- Enemy knowledge must respect line of sight and terrain obstruction.
- Enemy tanks must maintain standoff distance.
- Enemy tanks must not deliberately drive straight into or ram the player.
- Enemy tanks must stop to aim, fire imperfectly, reload, reposition, and recover when stuck.

### Locked Destruction And Environment

- Terrain crater effects must remain.
- Trees must remain damageable and knockdown-capable.
- Procedurally generated trees must include `BreakableTree`, flatten from tank collision, and have a fallback tree-flatten sound.
- Player tank engine audio must remain audible through the procedural fallback loop; do not reduce idle volume below `0.4` without replacing it with a tested authored loop/mix.
- Castle, turrets, player tank, and enemy tanks must have damage/destruction states.
- Destruction must be WebGL-safe and bounded. Do not add unbounded runtime debris, primitives, materials, projectiles, impact marks, or crater meshes.

### Locked Blender Asset Process

- Blender source files must be tracked under `BlenderSource/`.
- Approved runtime FBX/model assets must be tracked under `Assets/Resources/Models/`.
- Use `1 Unity unit = 1 meter`.
- Apply Blender transforms and use documented pivots before export.
- Keep hull, turret, barrel, and tracks separately addressable where gameplay requires them.
- Missing required model assets must log a clear error. Do not silently replace required Blender models forever with primitive placeholders.

### Locked WebGL And PythonAnywhere Process

- Target browsers: Chrome on Windows and Safari on Mac.
- WebGL compression stays disabled unless serving headers are explicitly configured and browser-tested.
- WebGL threads stay disabled.
- Avoid heavy shaders, massive post-processing, huge textures, and excessive real-time lights.
- Build output: `Builds/WebGL`.
- Deployment copy: `pythonanywhere_flask/webgl`.
- Release ZIP: `tank_game_pythonanywhere.zip`.
- Build and deployment manifests are required.
- PythonAnywhere ZIP output must be reproducible.

### Required Validation

- Every feature requires a validation hook, automated test, scene audit, or explicit manual checklist.
- Unity must compile with zero errors.
- Unity console must have zero errors before checkpoint completion.
- WebGL warnings are blockers unless explicitly allowlisted with reason and evidence.
- Run relevant EditMode and PlayMode tests after each checkpoint.
- Before release, verify projectile camera, lob arc, scope/rangefinder, craters, slope behavior, enemy standoff, destruction, deterministic win condition, WebGL build, deployment copy, and ZIP.

## Do Not Vibe-Code Out

- Projectile camera.
- Cannonball arc.
- Scope/rangefinder.
- Terrain craters.
- Tank slope alignment.
- Enemy spotting/standoff.
- Destructible models.
- WebGL deployment scripts.
- Validation tests.
- Best-known shared driving settings above (`TankDrivingProfile` / `TankGameplayTuning` / `4b6842e7` physics).
- Overdrive rear black smoke puff (single puff, no fire, not pink).
- HoneyFallScream void-fall audio on range fall-off.

## Required Startup Checks

1. Confirm repository branch and working tree:
   `git status --short --branch`
2. Confirm Unity project root is:
   `C:/Users/ben/Documents/CannonPhysicsSim`
3. Read `mcpforunity://custom-tools`, `mcpforunity://instances`, `mcpforunity://editor/state`, and `mcpforunity://project/info`.
4. Continue only when Unity is not compiling, not updating, and is ready for tools.
5. Inspect errors and warnings with `read_console`.
6. Confirm active scene and build target.
7. For Blender work, verify Blender MCP connectivity before editing source assets.

## Checkpoint Workflow

- Follow `docs/superpowers/plans/2026-06-12-silly-war-thunder-tank-overhaul.md`.
- Work checkpoint by checkpoint.
- Let Unity finish imports, compilation, tests, and builds.
- Use patient wait cycles up to 20 minutes. Do not assume failure because Unity is slow.
- After a slow cycle, inspect Unity state, console, `Editor.log`, and build logs before declaring failure.
- Use small validated commits.
- Do not delete or revert unrelated user changes.

## Current Starting Structure

- Active/build scene: `Assets/Scenes/Practice.unity`.
- Existing player controller: `Assets/Scripts/TankController.cs`.
- Existing projectile/camera controller: `Assets/Scripts/ProjectileCameraController.cs`.
- Existing terrain crater system: `Assets/Scripts/CraterTerrain.cs`.
- Existing scope scripts: `Assets/Scripts/SniperZoom.cs`, `Assets/Scripts/SniperRangeFinder.cs`.
- Existing enemy systems: `Assets/Scripts/EnemyTankAI.cs`, `Assets/Scripts/EnemyTankSpawner.cs`, `Assets/Scripts/EnemyTurret.cs`.
- Existing destruction: `Assets/Scripts/CastleDamageReceiver.cs`, `Assets/Scripts/BreakableTree.cs`.
- Existing test helper: `Assets/Scripts/GameplayTestApi.cs`.

## Reference Process

The process pattern is modeled after:

- `https://github.com/bbartling/TheHoneyManEscape`

Use its locked `AGENTS.md`, Blender source/runtime pairing, WebGL build script, Flask hosting layout, and regression audits as process references. Do not copy unrelated HoneyMan gameplay behavior into this tank game.

## Unity 6 API Notes

See `docs/UNITY_API_UPDATES.md` before editing `PracticeSceneBuilder.cs` or assigning physics materials. Use `PhysicsMaterial` (not `PhysicMaterial`). Rebuild dedicated practice scenes after course collision changes.
