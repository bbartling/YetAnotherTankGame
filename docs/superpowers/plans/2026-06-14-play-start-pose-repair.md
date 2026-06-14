# Play Start Pose Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Start battles through the human Play button without lifting or re-posing the tank, and keep resolved battles inert until an explicit new start.

**Architecture:** Preserve the captured menu transform and current local weapon pivot pose across battle reset. Restrict terrain correction to actual penetration, and make `BattlefieldDirector` own the resolved-state player input lock.

**Tech Stack:** Unity 6, C#, Unity Test Framework, Unity MCP

---

### Task 1: Lock Human Play Start Behavior With Tests

**Files:**
- Create: `Assets/Tests/PlayMode/PlayStartFlowPlayModeTests.cs`
- Modify: `Assets/Scripts/MenuManager.cs`

- [ ] Add a test fixture that loads `Assets/Scenes/Practice.unity`, waits for initialization, records the player root/turret/barrel pose, and invokes `MenuManager.PlayGameForTest()`.
- [ ] Assert immediate position and rotations remain within tolerance and the battle enters `Running`.
- [ ] Run the focused PlayMode tests and verify they fail because Play currently lifts and re-poses the player.
- [ ] Add `PlayGameForTest()` as a public wrapper around the same Play-button implementation.
- [ ] Re-run the focused tests; pose assertions must remain red until Task 2.

### Task 2: Preserve Player Pose And Remove Airborne Start

**Files:**
- Modify: `Assets/Scripts/GameplayTestApi.cs`
- Modify: `Assets/Scripts/TankController.cs`
- Test: `Assets/Tests/PlayMode/PlayStartFlowPlayModeTests.cs`

- [ ] Remove highest-footprint lift and `20` unit fallback from `GameplayTestApi.ResetBattle`; pass the captured initial transform directly.
- [ ] Change `TankController.ResetForBattle` to preserve current turret/barrel local rotations and controller desired angles.
- [ ] Replace repeated start-clearance snaps with one penetration-only correction using `terrainSurfaceSkin`.
- [ ] Remove terrain snapping from `PrepareForGameplay`.
- [ ] Run focused PlayMode tests and verify the Play-start pose test passes.

### Task 3: Lock Resolved Battles And Verify Beatability

**Files:**
- Modify: `Assets/Scripts/BattlefieldDirector.cs`
- Modify: `Assets/Tests/PlayMode/PlayStartFlowPlayModeTests.cs`

- [ ] Add a failing test that forces defeat and asserts the tank controller is disabled while state remains `Defeat`.
- [ ] Add a deterministic test that starts one enemy through `PlayGameForTest`, destroys it through the existing damage API, and asserts `Victory`.
- [ ] Disable player gameplay input and show/unlock cursor in both victory and defeat completion paths.
- [ ] Run focused PlayMode tests and verify both pass.

### Task 4: Validate And Commit

**Files:**
- Modify only files listed above.

- [ ] Run all EditMode tests and classify the result before processing.
- [ ] Run all PlayMode tests and classify the result before processing.
- [ ] Enter Play Mode, invoke the real `playButton.onClick`, record before/immediate/later pose and battle state, and verify no airborne drop or sideways turret.
- [ ] Inspect Unity console for zero errors.
- [ ] Review `git diff`, leave unrelated screenshots untouched, and commit the validated repair.

