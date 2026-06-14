# Tank Stability And Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver a heavy but roll-capable player tank, sustained-rollover defeat, correct signed wheel animation, a contrasting proper turret model, and green-over-red enemy health bars.

**Architecture:** Keep Rigidbody physics unconstrained while enforcing heavy chassis configuration in `TankController`. Add a focused rollover policy component, preserve existing battle defeat ownership in `BattlefieldDirector`, correct presentation behavior in existing visual/UI components, and regenerate only the player tank model.

**Tech Stack:** Unity 6000.4.6f1, C#, Unity Test Framework, Rigidbody physics, Blender generator, FBX, uGUI/TMP.

---

## Operating Contract

- Work on `feature/silly-war-thunder-tank-overhaul`.
- Preserve projectile camera, cannonball lob, craters, scope/rangefinder, destruction, enemy standoff, and Unity gameplay-test workflow.
- Classify ordinary pass/fail, compile, setup, and HTTP results as `SIMPLE`.
- Do not build WebGL or create a ZIP.
- Do not stage unrelated `ProjectSettings/EditorSettings.asset`, screenshots, or previously generated unrelated Blender/FBX changes.

## Task 1: Heavy Chassis And Rollover Defeat

**Files:**
- Create: `Assets/Scripts/Tank/TankRolloverController.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Modify: `Assets/Scripts/GameplayTestApi.cs`
- Modify: `Assets/Tests/PlayMode/TankCorePolicyPlayModeTests.cs`

- [ ] Add failing tests requiring mass `>= 15000`, center of mass `<= -1.0m`, unconstrained rotation, and rollover only after `75 degrees` is sustained for `2 seconds`.
- [ ] Run focused PlayMode tests and classify the expected failures as `SIMPLE`.
- [ ] Add `TankRolloverController` with threshold/timer/reset behavior and a defeat callback.
- [ ] Configure heavy Rigidbody values in `TankController` and connect rollover defeat to player death plus `BattlefieldDirector.ForceDefeat("Player tank rolled over")`.
- [ ] Expose rollover angle/timer/state in `GameplayTestApi`.

## Task 2: Correct Signed Wheel Animation

**Files:**
- Modify: `Assets/Scripts/Visual/TankVisualAnimator.cs`
- Modify: `Assets/Tests/PlayMode/BattlefieldPresentationPlayModeTests.cs`

- [ ] Add failing tests proving forward/reverse signed speeds rotate wheels in opposite directions around local X without local Y rotation.
- [ ] Run focused tests and classify failures as `SIMPLE`.
- [ ] Derive signed forward speed from Rigidbody velocity and rotate authored wheels around local X.

## Task 3: Proper Contrasting Player Turret

**Files:**
- Modify: `scripts/generate_silly_tank_models.py`
- Regenerate: `BlenderSource/Tanks/SillyPlayerTank.blend`
- Regenerate: `Assets/Resources/Models/Tanks/SillyPlayerTank.fbx`
- Modify: `Assets/Editor/ModelImportValidation.cs`
- Modify: `Assets/Tests/EditMode/Editor/ModelImportValidationTests.cs`

- [ ] Add failing validation requiring `TurretRing`, `GunMantlet`, and `CommanderCupola`.
- [ ] Change only player material to mustard/tan and rebuild its turret silhouette while preserving binding names.
- [ ] Regenerate only the player `.blend` and `.fbx`.
- [ ] Verify non-zero bounds and required authored parts.

## Task 4: War Thunder-Inspired Enemy Health Bars

**Files:**
- Modify: `Assets/Scripts/EnemyHealthBar.cs`
- Modify: `Assets/Tests/PlayMode/EnemyTankOperationsPlayModeTests.cs`

- [ ] Add failing tests requiring green healthy fill, red damage background, and percentage text.
- [ ] Implement fixed green remaining-health fill over red background.
- [ ] Verify damage reduces fill while preserving percentage text.

## Task 5: Unity Gameplay-Test Checkpoint

**Files:**
- Modify: `AGENTS.md`
- Modify: `docs/GAMEPLAY_DESIGN_LOCK.md`
- Modify: `docs/TESTING_AND_VALIDATION.md`
- Modify: this plan

- [ ] Run all EditMode and PlayMode tests.
- [ ] Inspect Unity console and require zero errors.
- [ ] Inspect live player Rigidbody, model parts, enemy health UI, and screenshot.
- [ ] Document the manual Unity gameplay checklist: stability, rollover defeat, wheel direction, turret silhouette/color contrast, and enemy health.
- [ ] Commit the Unity gameplay-test checkpoint.
- [ ] Stop without WebGL build or ZIP creation.
