# Physics-Driven Wheeled Tank Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a faster physics-driven eight-wheel armored vehicle that follows crater terrain, can tip and roll, visibly aims its turret/barrel, and provides improved third-person and barrel-aligned scope cameras.

**Architecture:** Replace broad tracked-ground assumptions with focused wheel-contact policy and suspension components while retaining `TankController` as the compatibility owner for weapons, damage, and scene references. Rebuild the Blender player tank with eight separately named wheels, bind authored visual parts to gameplay pivots, and introduce a dedicated scope-camera controller without changing projectile-camera behavior.

**Tech Stack:** Unity 6000.4.6f1, C#, Unity Test Framework, Unity MCP, Blender MCP/generator script, Rigidbody physics, URP, WebGL.

---

## Operating Contract

- Work on `feature/silly-war-thunder-tank-overhaul`.
- Preserve projectile camera, cannonball lob, crater deformation, rangefinder, destruction, enemy standoff, and deployment pipeline.
- Classify every test result before processing it.
- Use TDD: write a focused failing test, verify red, implement minimal behavior, verify green.
- Do not commit `ProjectSettings/EditorSettings.asset` or unrelated screenshot artifacts.
- End every checkpoint with Unity idle, zero console errors, focused tests, and a small commit.

## Checkpoint 1: Wheel Contact Physics And Crater Descent

**Files:**
- Create: `Assets/Scripts/Tank/WheeledSuspensionController.cs`
- Create: `Assets/Tests/PlayMode/WheeledTankHandlingPlayModeTests.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Scripts/Tank/TankDriveController.cs`
- Modify: `Assets/Scripts/GameplayTestApi.cs`

- [x] Add failing tests proving eight wheel contact points exist, suspension force is calculated per wheel, normal driving does not use highest-corner terrain correction, and the vehicle center can descend below its starting height inside a crater/ramp test fixture.
- [x] Run focused PlayMode tests; expected classification: `SIMPLE`, expected result: missing `WheeledSuspensionController` and failed crater-descent policy.
- [x] Implement eight local wheel mount definitions and per-wheel downward sphere/ray probes.
- [x] Apply spring/damper forces at grounded wheel contacts.
- [x] Disable continuous `SnapAboveTerrain` calls during `FixedUpdate`; retain correction only for startup/reset/severe penetration recovery.
- [x] Expose grounded wheel count and suspension evidence through `GameplayTestApi`.
- [x] Run focused automated tests; manual crater driving remains required.
- [x] Commit: `feat: add wheel suspension and crater-following physics`.

## Checkpoint 2: Faster Realistic Wheeled Handling And Rollover

**Files:**
- Modify: `Assets/Scripts/Tank/TankDriveController.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Tests/PlayMode/TankCorePolicyPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/WheeledTankHandlingPlayModeTests.cs`
- Modify: `AGENTS.md`
- Modify: `Docs/GAMEPLAY_DESIGN_LOCK.md`

- [x] Replace old speed-policy tests with failing assertions for `7.5 m/s` forward, `3.5 m/s` reverse, approximately four-second acceleration, approximately two-second braking, and wider high-speed steering.
- [x] Add policy coverage proving steep slopes lose traction instead of hard-blocking all movement.
- [x] Run focused tests; classification: `SIMPLE`, result: old hard clamps violated the new policy.
- [x] Implement gradual throttle acceleration, braking, coasting, downhill momentum retention, and differential wheel-side steering.
- [x] Remove continuous upright assistance and permit realistic pitch/roll/overturn.
- [x] Tune damping, suspension, and traction for stability without artificial leveling.
- [x] Update locked docs from slow tracked tank to faster wheeled armored vehicle while retaining deliberate aiming/combat pacing.
- [x] Run handling tests; manual crater traversal, tipping, rolling, and recovery verification remains required.
- [x] Commit: `feat: add realistic wheeled acceleration and rollover handling`.

## Checkpoint 3: Eight-Wheel Blender Model And Animation

**Files:**
- Modify: `scripts/generate_silly_tank_models.py`
- Regenerate: `BlenderSource/Tanks/SillyPlayerTank.blend`
- Regenerate: `Assets/Resources/Models/Tanks/SillyPlayerTank.fbx`
- Modify: `Assets/Scripts/Visual/SillyModelInstaller.cs`
- Modify: `Assets/Scripts/Visual/TankVisualAnimator.cs`
- Modify: `Assets/Editor/ModelImportValidation.cs`
- Modify: `Assets/Tests/EditMode/Editor/ModelImportValidationTests.cs`
- Modify: `Assets/Tests/PlayMode/BattlefieldPresentationPlayModeTests.cs`

- [x] Add model/import tests requiring `LeftWheel_0..3`, `RightWheel_0..3`, separate turret/barrel parts, and non-zero bounds.
- [x] Add visual tests proving each wheel rotates and long track blocks are absent from the player model.
- [x] Run focused/full tests; classification: `SIMPLE`.
- [x] Update the Blender generator to create eight chunky square-edged wheels plus narrow side guards while preserving the face, helmet, antenna, damage variants, turret, and barrel.
- [x] Regenerate and export the player `.blend` and `.fbx` with applied transforms and predictable wheel names.
- [x] Bind wheel transforms in `SillyModelInstaller`.
- [x] Animate all vehicle wheels around the authored axle without rotating side guards.
- [x] Validate renderer bounds and capture a new player model screenshot.
- [x] Commit: `feat: convert player tank to silly eight-wheel model`.

## Checkpoint 4: Visible Turret And Barrel Binding

**Files:**
- Modify: `Assets/Scripts/Visual/SillyModelInstaller.cs`
- Modify: `Assets/Scripts/Tank/TankTurretController.cs`
- Modify: `Assets/Scripts/Tank/TankAimController.cs`
- Modify: `Assets/Tests/PlayMode/SillyModelInstallerPlayModeTests.cs`
- Modify: `Assets/Tests/PlayMode/TankCorePolicyPlayModeTests.cs`

- [x] Add tests proving visible `Turret` follows `TurretYawPivot` and visible `Barrel` follows `BarrelPitchPivot`.
- [x] Run full tests; classification: `SIMPLE`.
- [x] Add visual-follow bindings that preserve model-space offsets while copying gameplay pivot yaw/elevation.
- [x] Ensure damaged turret/barrel variants follow the same pivots.
- [ ] Verify compass yaw, gameplay pivot yaw, and visible turret yaw agree.
- [ ] Run focused tests and manually rotate turret/barrel in play mode.
- [x] Commit: `fix: bind visible turret and barrel to aiming pivots`.

## Checkpoint 5: Raised Third-Person Camera And Barrel Scope

**Files:**
- Create: `Assets/Scripts/Camera/TankBarrelScopeCamera.cs`
- Create: `Assets/Tests/PlayMode/TankCameraPlayModeTests.cs`
- Modify: `Assets/Scripts/Camera/TankOrbitCamera.cs`
- Modify: `Assets/Scripts/SniperZoom.cs`
- Modify: `Assets/Scripts/SniperRangeFinder.cs`
- Modify: `Assets/Scripts/Weapons/TankScopeController.cs`
- Modify: `Assets/Scripts/TankController.cs`

- [x] Add tests requiring third-person target height `>= 4.2m`, distance `>= 10m`, and scope-camera forward alignment within two degrees of `cannonFirePoint.forward`.
- [ ] Add a failing transition test proving right-click scope and projectile-camera return do not leave cameras locked.
- [x] Run full tests; classification: `SIMPLE`.
- [x] Raise and tune `TankOrbitCamera` while preserving collision avoidance.
- [x] Implement `TankBarrelScopeCamera` to position and align the gameplay camera to a sight anchor near the barrel during right-click.
- [x] Keep rangefinder/ballistic UI active during scope and restore third-person view on release/projectile-camera return.
- [ ] Run focused tests and manually inspect third-person visibility and scoped firing.
- [x] Commit: `feat: add raised chase camera and barrel-aligned scope`.

## Checkpoint 6: Full Validation And WebGL Release

**Files:**
- Modify: `Docs/TESTING_AND_VALIDATION.md`
- Modify: `Docs/TANK_GAME_CURRENT_STATE_AUDIT.md`
- Modify: `Docs/WEBGL_PERFORMANCE_BUDGET.md`
- Modify: `README.md`
- Generate: `Builds/WebGL/`
- Generate: `pythonanywhere_flask/webgl/`
- Generate: `tank_game_pythonanywhere.zip`

- [x] Run all EditMode and PlayMode tests; require zero failures.
- [ ] Inspect Unity console; require zero errors.
- [ ] Manually verify crater descent, wheel suspension, acceleration/braking, tipping/rolling, visible turret/barrel, raised camera, scope alignment, lobbed shot, and projectile-camera return.
- [ ] Update docs with new handling values and manual evidence.
- [ ] Build WebGL using quiet 20-minute wait cycles.
- [ ] Verify manifests, PythonAnywhere copy, ZIP layout, ZIP size, and SHA-256.
- [ ] Commit: `release: validate physics-driven wheeled tank overhaul`.
