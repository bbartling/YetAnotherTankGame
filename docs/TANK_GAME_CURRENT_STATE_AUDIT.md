# Tank Game Current State Audit

Audit date: 2026-06-12  
Repository: `https://github.com/bbartling/YetAnotherTankGame`  
Local root: `C:/Users/ben/Documents/CannonPhysicsSim`  
Starting commit: `53fd50b`  
Unity: `6000.4.6f1`  
Render pipeline: URP `17.4.0`

## MCP Status

- Unity MCP is connected to `CannonPhysicsSim@f8f49182bf35fd11`.
- Unity editor is idle, not compiling, not updating, and ready for tools.
- Active scene is `Assets/Scenes/Practice.unity`.
- Blender MCP tools are exposed in the session, but no read-only Blender connection/status tool is exposed. Blender connectivity must be explicitly verified at the start of model work.

## Current Scenes And Build State

- Build settings contain one enabled scene: `Assets/Scenes/Practice.unity`.
- The active build target is `StandaloneWindows64`.
- Two Windows build profiles exist.
- A tracked WebGL/PythonAnywhere pipeline exists: `Assets/Editor/TankWebGLBuildPipeline.cs`, `scripts/build_webgl_pythonanywhere.ps1`, and `pythonanywhere_flask/`.
- WebGL player settings exist but are not locked or validated.

## Current Scene Structure

Important roots:

- `PlayerTank`
- `ProjectileCamera`
- inactive `TargetCamera`
- `Canvas`
- `MenuManager` with `EnemyTankSpawner`
- `MapRoot`
- `BattlefieldWind`
- `BattlefieldDirector`

Known scene concerns:

- `PlayerTank` has two active children named `Main Camera`.
- One player camera has `SniperZoom`; the other owns the active audio listener.
- The enemy base contains both `Castle` and `Castle(Clone)`.
- The scene is approximately 9 MB and has a history of large AI-generated rewrites.

## Current Player Controls

`Assets/Scripts/TankController.cs` owns movement, suspension, slope alignment, terrain correction, turret aiming, barrel aiming, cannon firing, health, collision damage, reset behavior, and test helpers.

Current input:

- `W/S`: forward/reverse.
- `A/D`: hull steering.
- Mouse X: turret yaw.
- Mouse wheel or Page Up/Page Down: barrel elevation.
- Left click or Space: fire cannon.
- Shift: machine gun.
- Right click: zoom/rangefinder.

Current feel is not locked as acceptable. The controller is large and the requested overhaul must make movement slower, heavier, less slippery, and more slope-aware.

## Current Cameras

- A `ProjectileCamera` root camera starts disabled.
- The projectile script enables it after player cannon fire, disables a discovered tank camera, follows the shell, lingers after impact, then returns to the tank camera.
- The active player setup contains two cameras, so camera discovery through `GetComponentInChildren<Camera>` and `Camera.main` is fragile.
- `TargetCamera` exists but is inactive.

## Current Projectile Camera Behavior

`Assets/Scripts/ProjectileCameraController.cs`:

- Finds `ProjectileCamera` by name if no reference is assigned.
- Finds a tank camera from `trackingBase`, then falls back to `Camera.main`.
- Smooth-follows the cannonball velocity direction.
- Keeps a static `ActivePlayerProjectile`.
- Prevents another player cannon shot while a player projectile is active.
- Lingers for approximately two seconds after impact before returning.
- Also owns projectile wind, impact routing, damage, explosion, audio, trail, and lifetime.

This camera behavior is a locked feature and may only be replaced by a tested improvement.

## Current Scope And Rangefinder

- `SniperZoom` changes the camera FOV while right mouse is held.
- `SniperRangeFinder` displays runtime-created range ticks and estimates ballistic travel distance while right mouse is held.
- The rangefinder uses a separate trajectory simulation and may drift from the actual shell launch calculation.
- Scope presentation is neat but is not reliably aligned with the barrel or actual impact point.

## Current Cannonball And Ballistics

- `Assets/Prefabs/CannonBall.prefab` contains a Rigidbody, sphere collider, trail, audio, fireball particles, and `ProjectileCameraController`.
- Player cannon fire uses a physical Rigidbody shell and preserves a visible lobbed arc.
- Player launch speed is based on power percentage and barrel forward direction.
- The tank has a `LineRenderer` intended for trajectory visualization.
- Enemy tanks and enemy turrets contain their own ballistic-solution implementations.
- Ballistic calculations are duplicated and must be unified without flattening the player cannonball arc.

## Current Enemy Behavior

- `EnemyTankAI.cs` is approximately 938 lines and owns state, movement, targeting, aiming, firing, health, death, debris, craters, audio, and camera disabling.
- Enemy tanks can patrol, find the player, aim, fire physical shells, and receive damage.
- Current behavior is reported to rush too directly toward the player and does not reliably maintain tactical standoff.
- `EnemyTankSpawner` dynamically places and tunes enemy tanks.
- `EnemyTurret` aims and fires physical bullets/shells from the castle area.

## Current Terrain And Craters

- `CraterTerrain` builds a runtime mesh approximately `1200 x 1400` units with hills and seeded craters.
- Projectile impacts call `CraterTerrain.ApplyImpact`.
- The terrain mesh and collider are updated at runtime.
- Trees are procedurally spawned on terrain.
- The crater system is a locked feature, but it needs active-effect and WebGL performance budgets.

## Current Destruction

- `CastleDamageReceiver` supports impact marks, progressive piece removal, health, collapse, and debris.
- `BreakableTree` supports collision/impact breakage and debris.
- Enemy tanks have health, explosion damage, death effects, kill craters, and debris.
- Enemy turrets can be destroyed.
- Several effects create primitives and materials at runtime with no shared pooling budget.

## Current UI And Game Loop

- `Canvas` contains dashboard, controls, menu, minimap, and inactive scope/rangefinder roots.
- Several UI systems create their own GameObjects at runtime.
- `BattlefieldDirector` manages victory, defeat, objective tracking, statistics, and runtime objective UI.
- `GameplayTestApi` provides smoke-test helpers but is not a Unity Test Framework assertion suite.
- Unity reports placeholder EditMode and PlayMode test entries with zero real assertions.

## Current Asset And Process State

- `GeneratedAssets` contains tracked generated PNG/WAV files and metadata.
- No `BlenderSource` convention exists in this repo.
- No required runtime model convention exists under `Assets/Resources/Models`.
- No `AGENTS.md` existed before this audit.
- The reference HoneyMan project demonstrates the desired locked configuration, Blender source/runtime pairing, WebGL build script, Flask host, and regression-test process.

## Known Broken Or Fragile Things

- Movement is too fast/slippery and slope behavior is poor.
- Enemy tanks rush too directly and need perception/standoff behavior.
- Scope/rangefinder is not aligned enough to actual ballistics.
- Two active player cameras create ambiguous camera ownership.
- Duplicate castle objects create ambiguous objective ownership.
- Large multi-responsibility MonoBehaviours make changes risky.
- Heavy use of `GameObject.Find`, `Camera.main`, scene-wide object searches, runtime primitive creation, and `Shader.Find`.
- Runtime debris, impact marks, tracers, and crater changes lack explicit WebGL budgets.
- No real automated regression suite.
- WebGL/PythonAnywhere release output is reproducible and validated through manifests plus `tank_game_pythonanywhere.zip`.

## June 13, 2026 Wheeled Handling Update

- The player visual is an authored silly eight-wheel armored vehicle with four separately animated square-edged wheels per side.
- Player road speed is capped at `7.5 m/s` forward and `3.5 m/s` reverse, with approximately four-second acceleration and two-second braking.
- Eight wheel-contact probes apply normalized spring/damper forces. Continuous highest-footprint terrain correction is disabled during normal driving so the vehicle can descend into craters.
- Artificial continuous hull leveling is disabled, allowing physics-driven pitch, tipping, and rolling.
- Visible authored turret and barrel parts follow the gameplay yaw/elevation pivots.
- Third-person camera defaults are raised to a `4.5m` target offset and `11m` follow distance.
- Right mouse uses a barrel-firepoint-aligned scope pose while preserving projectile-camera ownership after firing.
- `GameplayTestApi` exposes `playerGroundedWheelCount` for suspension validation.

## Features That Must Not Regress

- Physical player cannonball with visible artillery-like lob.
- Projectile camera after cannon fire, impact linger, and return to tank view.
- Terrain crater deformation/effect.
- Right-click scope/rangefinder concept.
- Trajectory/arc visualization concept.
- Damageable trees.
- Destructible castle, turrets, and enemy tanks.
- Enemy tanks as active ranged opponents.
- Wind and battlefield atmosphere where practical.
- A beatable single-player battle loop.
- Chrome Windows, Safari Mac, and PythonAnywhere WebGL goals.
