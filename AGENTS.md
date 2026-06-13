# Agent Context: Silly Tank Hill War

This Unity project uses Unity MCP and Blender MCP. Future agents must inspect the live editor, current scene, console, repository state, and this file before making changes.

## LOCKED CURRENT CONFIGURATION - DO NOT REGRESS

This block is the authoritative feature contract. Update it in the same validated change whenever intentionally revising a locked value or behavior.

### Game Identity And Feel

- Working title: `SILLY TANK HILL WAR`.
- The target feel is silly cartoon armored vehicles with deliberate War Thunder-ish combat pacing.
- Player vehicle reaches approximately `7.5 m/s` with gradual acceleration, never FPS-sprint movement.
- Player vehicle must follow terrain into craters.
- Player vehicle must lose traction on steep slopes and may realistically tip or roll.
- Shift may provide low gear or aim stabilization, never sprint.

### Locked Weapons And Cameras

- Player cannonballs remain physical, visible, and artillery-like with a lobbed arc.
- Projectile camera remains available after firing player cannonballs.
- Projectile camera follows the shell, shows impact, lingers, and returns to tank view.
- Do not remove the projectile camera unless replacing it with a strictly better tested version.
- Right-click scope/rangefinder must remain and must be aligned with actual aiming.
- Scope/rangefinder must report useful ballistic information.
- Trajectory preview or equivalent aim assistance must remain available.

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
