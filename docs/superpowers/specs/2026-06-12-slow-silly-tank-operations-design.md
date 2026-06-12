# Slow Silly Tank Operations Design

## Purpose

Transform `SILLY TANK HILL WAR` into a deliberately paced, silly-looking tactical tank game without removing its existing projectile camera, lobbed cannonballs, scope/rangefinder, terrain craters, enemies, or destructible castle gameplay.

The game should feel like cartoon tank crews attempting careful long-range operations with heavy, awkward machines. It should not feel like a fast first-person shooter or a realistic military simulator.

## Chosen Approach

Use a state-driven tactical gameplay overhaul paired with modular low-poly Blender models and bounded Unity effects.

This approach replaces the enemy's continuous high-force chase-and-fire behavior with readable operational states, while preserving and integrating the existing gameplay systems. Blender assets will be constructed as separate gameplay-addressable pieces so damage and animation do not depend on expensive skeletal rigs or runtime-generated primitive replacements.

Rejected alternatives:

- Parameter-only slowdown: faster to implement, but the enemy would still behave like a rushing FPS opponent.
- Full simulation with advanced armor, navigation, and vehicle physics: too complex and costly for the intended silly tone and WebGL target.

## Player Tank Pacing

The player tank must move at a slow creeping pace suitable for observing terrain and aiming.

- Target maximum forward speed: approximately `2.5 m/s`.
- Target maximum reverse speed: approximately `1.0 m/s`.
- Target acceleration time: `5-7 seconds`.
- Reverse, braking, turning, and slope traversal must feel heavy and readable.
- Shift provides low gear or aim stabilization, never sprint.
- High-speed steering is limited; low-speed pivoting remains possible but slow.
- Uphill movement reduces speed and increases engine strain.
- Slopes beyond the maximum climb angle cause traction loss or refusal instead of launching or jittering.
- Hull visuals smoothly align to terrain while the physics body remains stable.

Turret rotation and barrel elevation remain independent of hull movement. Turret rotation, elevation, reload, and aiming transitions must be slow enough that positioning and anticipation matter.

## Enemy Tank Operations

Enemy tanks use explicit operational states:

1. `Patrol`
2. `Suspicious`
3. `Spotting`
4. `HaltToAim`
5. `Firing`
6. `Reloading`
7. `Repositioning`
8. `Retreating`
9. `Disabled`
10. `Destroyed`

Enemies must:

- Detect the player through line of sight, movement, firing noise, and nearby explosions.
- Stop or nearly stop before final aiming and firing.
- Maintain a preferred long-range standoff band.
- Back away or reposition when the player is too close.
- Avoid strafing, rushing, and deliberate ramming.
- Rotate turrets and hulls at believable heavy-machine speeds.
- Reload slowly and miss occasionally.
- Lose sight behind terrain or castle obstructions.
- Avoid steep slopes and recover from stuck conditions.
- Use limited speeds comparable to or slower than the player.

Enemy types may express different personalities while following the same operational rules:

- Nervous scout: spots quickly, relocates often, weak armor.
- Grumpy standard tank: balanced standoff combat.
- Oversized commander tank: slow, durable, deliberate, and visibly silly.

## Ballistics, Scope, And Cameras

The physical player cannonball and its visible lobbed arc remain central gameplay.

- Right-click scope/rangefinder aligns with the actual ballistic solution.
- Scope displays distance, barrel elevation, estimated flight time, reload state, and a useful holdover or landing indication.
- Left-click fires a physical cannonball, not hitscan.
- Projectile camera activates after firing, follows the shell, shows impact, lingers, and returns to the tank camera.
- Projectile-camera cancellation remains available.
- Existing terrain crater effects remain and may be improved only within bounded WebGL budgets.

## Visual Direction And Blender Models

No required Blender tank, castle, turret, or tree models currently exist. Creating them is a first-class implementation checkpoint.

The art direction is chunky, readable, low-poly, and deliberately silly:

- Player tank: grumpy cartoon eyes, oversized commander helmet/dome, chunky barrel, exaggerated tracks, comic armor plates, dents, rivets, and a wobbling antenna.
- Enemy tanks: distinct silhouettes and personalities readable at long range.
- Castle: toy-like chunky stone blocks, exaggerated battlements, modular towers, and expressive cannon turrets.
- Trees: simple chunky low-poly silhouettes with standing, damaged, fallen, and stump states.

Blender source files live under `BlenderSource/`. Approved runtime models live under `Assets/Resources/Models/`.

Model assemblies use separately addressable objects:

- Tank hull, turret, barrel, left track, right track, hatch/dome, and optional antenna.
- Castle wall sections, corner pieces, battlements, tower body, turret mount, and turret cannon.
- Tree trunk, crown, stump, and fallen variant.

Transforms are applied, scale is `1 Unity unit = 1 meter`, and pivots are placed at actual rotation or damage-swap points.

Animation favors inexpensive transform animation:

- Turret yaw and barrel elevation.
- Hatch, antenna wobble, track visual motion, recoil, and damage wobble.
- Tree falling.
- Castle chunk collapse.

Skeletal animation is used only where it provides clear value and remains WebGL-friendly.

## Damage And Destruction

Damage is readable through discrete bounded states rather than unlimited runtime fragments.

Tank states:

1. Intact
2. Damaged and smoking
3. Burning or disabled
4. Destroyed wreck

Tank damage zones may affect behavior:

- Track damage reduces movement or turning.
- Turret damage slows rotation.
- Barrel damage worsens aim.
- Hull damage advances the overall damage state.

Castle and turret states:

1. Intact
2. Cracked
3. Heavily damaged
4. Collapsed or destroyed

Castle collapse uses modular model swaps and a strict maximum number of pooled debris chunks. Turret weapons can be disabled or destroyed separately.

Trees use standing, damaged/falling, fallen, and stump variants. Small trees may be crushed; large trees obstruct tanks until damaged.

## Effects And Audio

Effects must be dramatic, readable, silly, and bounded:

- Smoke plumes for damaged tanks and structures.
- Fire for burning tanks and heavily damaged castle pieces.
- Muzzle flashes, cannon recoil, shell trails, impact flashes, and explosions.
- Dirt, metal, wood, and stone impact variants.
- Limited pooled debris and crater effects.

Audio communicates machine state and battlefield events:

- Engine idle, movement, uphill strain, track clank, squeak, and grinding.
- Turret motor, barrel creak, reload clunk, and cannon boom.
- Shell whistle, impact variants, explosion, fire crackle, tree crack, and castle crumble.
- Enemy engine audio is audible only at appropriate range.
- Simultaneous loud effects are capped to avoid clipping and WebGL overload.

Audio assets and controller structure must allow later replacement without changing gameplay logic.

## WebGL Performance Contract

- Pool or strictly cap projectiles, explosions, smoke, fire, debris, craters, and impact marks.
- Use low-poly meshes, simple materials, compressed textures, and compressed audio.
- Avoid heavy shaders, excessive real-time lights, large post-processing stacks, and runtime mesh explosion spam.
- Keep active enemy counts and shadow casters limited.
- Preserve Chrome on Windows, Safari on Mac, and PythonAnywhere deployment compatibility.

## Unity Scene Disk-Change Handling

Automated work may modify `Assets/Scenes/Practice.unity` on disk while Unity has the scene open.

- Before making scene changes, verify Git status and Unity scene state.
- If the working tree is clean and Unity has no intentional unsaved in-memory scene edits, the committed on-disk scene is authoritative and Unity should reload it.
- If Unity has intentional unsaved in-memory edits, save or inspect those edits before reloading.
- After an external scene change, reload the scene, wait for Unity readiness, inspect the console, and validate the active scene.
- Scene-changing checkpoints must end with a cleanly saved scene and clean Unity console.

## Validation Strategy

Each implementation checkpoint requires focused automated tests or a documented manual verification hook.

Required behavioral validation includes:

- Player maximum speed and acceleration remain within locked slow thresholds.
- Uphill and excessive-slope behavior is correct.
- Turret moves independently and slowly.
- Scope and rangefinder align with the ballistic cannonball solution.
- Projectile camera activates, shows impact, and returns.
- Enemy halts to aim, fires from standoff range, loses line of sight, and does not ram.
- Tanks enter smoking, burning/disabled, and wreck states.
- Castle pieces show damage and collapse within debris limits.
- Trees fall or swap state when damaged.
- Effects and audio trigger without missing references.
- Unity compiles with zero errors and the console has zero errors.
- Final WebGL build and PythonAnywhere ZIP succeed.

Test-result analysis must be classified before processing:

- `SIMPLE`: pass/fail results, HTTP status errors, missing UI/selectors, environment setup failures, syntax errors, or import failures.
- `COMPLEX`: ambiguous behavior, race or timing failures, security issues, performance degradation patterns, or failures spanning multiple components.
- Default classification is `SIMPLE`.

## Delivery Order

1. Lock design, plan, and scene-change handling.
2. Slow player movement and tactical enemy operations.
3. Integrate ballistic aiming and projectile-camera validation.
4. Add modular damage-state architecture and bounded effects/audio.
5. Create Blender tank, castle, turret, tree, and damage-state models.
6. Assemble and validate Unity prefabs.
7. Expand automated gameplay validation.
8. Produce and verify the final WebGL/PythonAnywhere build.

All checkpoints preserve the locked existing features and finish with compilation, console, test, and Git verification.
