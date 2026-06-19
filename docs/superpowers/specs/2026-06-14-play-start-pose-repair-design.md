# Play Start Pose Repair Design

## Goal

Pressing the same Play button a human uses must begin the battle without visibly changing the correctly presented menu tank, dropping it from the air, or turning its turret/barrel sideways. Victory and defeat must remain resolved until an explicit new Play-button start.

## Root Cause Evidence

The real `playButton.onClick` path calls `MenuManager.PlayGame`, then `GameplayTestApi.StartBattle`, then `ResetBattle`, `TankController.ResetForBattle`, and `PrepareForGameplay`.

Live reproduction recorded:

- Before Play: player Y `5.421`, turret local yaw `90` degrees, barrel local pitch about `-1.26` degrees.
- Immediately after Play: player Y `6.752`, root rotation reset to identity, turret local yaw reset to `0`, barrel forced to `-5` degrees.
- The player then falls back to terrain and can immediately reach defeat.

`GameplayTestApi.ResetBattle` raises the player from the captured menu pose to the highest sampled footprint plus an additional lift, with a `20` unit fallback. `TankController.ResetForBattle` then performs repeated terrain snaps and explicitly replaces the authored turret/barrel pose.

No scene reload, automatic restart, or death-click restart handler exists in project scripts. The reliable requirement is therefore to make resolved battles ignore gameplay input and remain resolved until the explicit Play-button path starts a new battle.

## Design

### Human Play Path

Keep `MenuManager` as the single human Play-button entry point. Expose a small public `PlayGameForTest` wrapper that invokes the same private implementation so PlayMode validation tests the real path rather than a separate test-only battle start.

### Player Reset

`GameplayTestApi.ResetBattle` will restore the captured initial player transform exactly. It will no longer compute a highest-footprint lift or use a `20` unit airborne fallback.

`TankController.ResetForBattle` will preserve the turret and barrel local rotations that exist at the time of reset. It will clear health and velocities, configure the rigidbody, and perform one penetration-only terrain correction. The correction may move the player upward only when a collider intersects terrain; it must not add clearance to an already valid menu pose.

`PrepareForGameplay` will not perform another start-clearance snap. Normal suspension and physics take over after Play.

### Resolved Battle Input

When `BattlefieldDirector` resolves victory or defeat, it will disable the player `TankController` and unlock/show the cursor. This prevents death-screen mouse clicks from firing or changing gameplay state. A later explicit Play-button press re-enables and resets the tank through the existing start path.

### Locked Features

The repair does not remove or simplify projectile camera, lobbed cannonballs, scope/rangefinder, terrain craters, destruction, enemy tanks, generated terrain, or WebGL deployment.

## Validation

PlayMode tests will:

1. Invoke the same Play-button callback used by a human.
2. Assert player position/root rotation/turret yaw/barrel pitch remain within small tolerances immediately after Play.
3. Assert the tank is not placed airborne above its menu pose.
4. Force defeat and assert player input is disabled and state remains defeat.
5. Start a deterministic one-enemy battle through the human Play path, destroy the enemy through the existing damage API, and assert victory is reachable.

Manual Unity validation will press Play in the Game view, use the visible Play button, inspect the tank immediately and after settling, and check console errors.

