# Player Tank Playability State

Updated: 2026-06-21

## Controls

- `W` / Up Arrow: forward throttle.
- `S` / Down Arrow: reverse throttle.
- `A` / Left Arrow: steer left.
- `D` / Right Arrow: steer right.
- Shift: low gear / stabilized creeping.
- Mouse X: turret yaw.
- `Q` / `E`: lower / raise cannon barrel.
- Mouse wheel or PageUp / PageDown: fine barrel elevation.
- `+` / Numpad Plus: increase cannon power by 5%.
- `-` / Numpad Minus: decrease cannon power by 5%.
- Right mouse: scope / rangefinder.
- Left mouse or Space: fire cannon.

## Current Locked Gameplay Settings

- Player cannon power defaults to `100%`.
- Tank road speed policy remains `7.5 m/s` max forward and `3.5 m/s` max reverse.
- Tank drive acceleration remains deliberate at `4 s` to target speed.
- Continuous terrain clearance is enabled on the player tank.
- Wheel suspension uses `1.0 m` travel, `3.0` spring acceleration, and `1.2` damping so eight wheel contacts do not over-lift the chassis.
- Wheel suspension contacts are allowed to mark the tank grounded when the narrower track probe misses terrain.
- Enemy spawn ground lift is `0.35 m`, with collider-bottom snapping to terrain plus `0.05 m` skin.

## Camera And Scope State

- Chase camera follows behind the current cannon direction, not only the hull.
- When blocked behind the tank, chase camera slides into a low forward view instead of moving overhead and looking straight down.
- Scope camera uses the cannon fire point direction exactly and sits `0.75 m` forward from the fire point.
- Projectile camera remains enabled for player shells, follows the projectile, counts activations, and returns after impact linger.

## Validation Snapshot

- Forward-drive runtime smoke: tank moved about `65 m` over terrain with all 8 wheels grounded.
- Player visual bottom measured about `0.31 m` above sampled terrain after suspension tuning.
- Enemy visual bottom measured about `0.23 m` above sampled terrain after spawn snapping and model alignment.
- Scope alignment runtime check: dot product to cannon direction `1.000`, forward offset `0.75 m`.
- Projectile fired at enemy activated projectile camera and resolved a one-enemy battle to Victory.
- Fresh kill path confirmed enemy health reached `0%` and battle state advanced to Victory.
