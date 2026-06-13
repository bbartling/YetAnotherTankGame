# Gameplay Design Lock

## Working Identity

Working title: `SILLY TANK HILL WAR`

The game is a silly cartoon armored-vehicle battle with deliberate War Thunder-ish pacing: cross rough terrain, spot threats at range, carefully aim lobbed cannonballs, watch the projectile camera, and break tanks, trees, turrets, castles, and terrain.

## Locked Player Experience

- Player wheeled vehicle reaches approximately `7.5 m/s` with gradual acceleration and braking, never FPS-sprint movement.
- Reverse is slower than forward.
- Low-speed turning is possible but heavy.
- High-speed turning is wide and limited.
- Uphill movement loses speed and produces engine strain.
- The vehicle follows terrain into craters and may realistically tip or roll.
- The vehicle loses traction and speed on steep slopes instead of being held upright artificially.
- Turret rotates independently from the hull.
- Barrel elevation controls a visibly arcing physical cannonball.
- Left click fires the player cannonball; firing is not hitscan.
- Right click enters a useful scope/rangefinder aligned with actual aiming.
- Scope reports distance, elevation, and estimated time of flight.
- Shift is low gear or aim stabilization, never sprint.

## Locked Camera Experience

- Third-person tank camera is the default gameplay view.
- The camera avoids terrain and castle clipping.
- Projectile camera remains available after every player cannonball shot.
- Projectile camera follows the lobbed cannonball, shows impact/explosion, lingers, and returns automatically.
- A manual projectile-camera cancel input is allowed.
- Projectile camera may only be replaced by a strictly better tested implementation.

## Locked Enemy Experience

- Enemy tanks spot and engage from distance.
- Enemy knowledge respects line of sight, terrain, castle obstruction, movement, and firing noise.
- Enemy tanks maintain standoff distance and do not deliberately ram.
- Enemy tanks stop to aim, fire with imperfect accuracy, reload slowly, and reposition.
- Enemy tanks can lose sight behind terrain.
- Enemy tanks avoid steep slopes and recover from stuck states.
- Enemy engine audio is distance-limited.

## Locked Destruction Experience

- Terrain craters remain.
- Trees can be damaged, knocked down, or crushed according to size.
- Castle walls and turrets have visible damage/destruction states.
- Tanks have visible damage/destruction states.
- Destruction is bounded and WebGL-friendly through pooling or strict limits.
- The game must not leak unlimited debris, craters, impact marks, or projectiles.

## Locked Art Direction

- Silly expressive low-poly cartoon military tanks.
- Player tank should have a grumpy face, oversized helmet/commander dome, chunky barrel, and comic metal plates.
- The mood reference is inspiration only. Do not copy it, reproduce it directly, or include watermarks.
- Blender source files live under `BlenderSource`.
- Approved runtime models live under `Assets/Resources/Models`.
- Missing required models must log clear errors instead of silently remaining primitive placeholders.

## Locked Release Contract

- WebGL targets Chrome on Windows and Safari on Mac.
- WebGL build is uncompressed unless serving headers are explicitly configured and tested.
- Threads remain disabled.
- Heavy shaders, massive textures, excessive post-processing, and unbounded runtime mesh effects are prohibited.
- A reproducible PythonAnywhere upload ZIP must be created from the Flask app plus complete WebGL output.
- Tests, manifests, console audit, browser smoke, and Safari manual checklist are release requirements.

## Do Not Vibe-Code Out

- Projectile camera.
- Cannonball lob arc.
- Scope/rangefinder.
- Trajectory preview concept.
- Terrain craters.
- Tank slope alignment and steep-slope struggle.
- Enemy spotting and standoff.
- Damageable trees.
- Destructible tanks, castle, and turrets.
- WebGL deployment scripts.
- Blender source/runtime model pairing.
- Automated validation tests and manual release checklist.
