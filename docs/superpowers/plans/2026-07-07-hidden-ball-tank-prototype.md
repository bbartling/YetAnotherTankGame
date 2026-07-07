# Hidden Ball Tank Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prototype a visible tank riding on the existing `BallDriveTest` rolling sphere while preserving the sphere's Rigidbody, collider, and physics feel.

**Architecture:** Keep `BallDriveTestPlayer` as the only physical body in `Assets/Scenes/BallDriveTest.unity`. Hide the ball renderer, add a visual-only tank follower child, and change `BallDriveTestController` input helpers so W/S drive along a tank heading while A/D rotates that heading. This first pass is isolated to `BallDriveTest` and does not modify WAR, Driving Practice, Cannon Practice, combat, projectile camera, or old tank gameplay scenes.

**Tech Stack:** Unity 6000.4.6f1, C#, Unity Test Framework, Unity MCP.

---

## Files

- Modify: `Assets/Scripts/BallDriveTestController.cs`
- Create: `Assets/Scripts/BallDriveTankVisualFollower.cs`
- Modify: `Assets/Tests/EditMode/BallDriveTestControllerTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/BallDriveTestSceneEditModeTests.cs`
- Modify: `Assets/Scenes/BallDriveTest.unity`

## Task 1: Tank-Style Ball Input Tests

- [ ] Add an EditMode test proving forward input follows a supplied tank heading.
- [ ] Add an EditMode test proving turn input updates heading without requiring camera-relative strafe.
- [ ] Run the targeted EditMode tests and confirm they fail before production changes.

## Task 2: Controller Helpers

- [ ] Add public static helper methods to `BallDriveTestController` for tank heading update and heading-relative drive direction.
- [ ] Change `FixedUpdate` to use W/S for throttle and A/D for heading rotation.
- [ ] Preserve existing roll force, torque force, max speed, gravity, braking, camera, Rigidbody, and collision setup.

## Task 3: Visual Follower

- [ ] Create `BallDriveTankVisualFollower` as a visual-only component.
- [ ] Hide non-visual ball renderers on the physics root while keeping collider and Rigidbody active.
- [ ] Keep the visual tank upright and positioned above the hidden ball with smoothed yaw matching the tank heading.

## Task 4: Scene Wiring

- [ ] In `BallDriveTest`, attach `BallDriveTankVisualFollower` to `BallDriveTestPlayer`.
- [ ] Create a simple visual-only tank child with hull, turret, and barrel shapes.
- [ ] Disable the ball `MeshRenderer`.
- [ ] Save the scene.

## Task 5: Verification

- [ ] Run targeted EditMode tests for `BallDriveTestControllerTests` and `BallDriveTestSceneEditModeTests`.
- [ ] Check Unity editor state and console.
- [ ] Report exact verification results and any remaining warnings/errors.
