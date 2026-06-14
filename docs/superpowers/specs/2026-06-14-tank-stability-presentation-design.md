# Tank Stability And Presentation Design

## Goal

Make the player tank feel like a very heavy armored machine without preventing genuine rollover, end gameplay after a sustained rollover, correct wheel animation direction, improve the authored player turret and color contrast, and present enemy health in a War Thunder-inspired green-over-red format.

## Root Causes

- `TankController` configures damping and a `-0.55m` center-of-mass offset but does not enforce a heavy mass or sufficiently low center of mass for the tall cartoon model.
- There is no gameplay rule for a tank that remains overturned.
- `TankVisualAnimator` uses unsigned planar speed and rotates wheels around local `Y`, producing outward rotation and no reverse animation.
- The player model uses terrain-adjacent olive and a rounded cube turret that lacks a clear turret ring, mantlet, armored turret body, and cupola.
- `EnemyHealthBar` defines red as its healthy color and uses a black background rather than red lost-health feedback.

## Approved Design

### Heavy Chassis Physics

- Set player Rigidbody mass to `18000 kg`.
- Lower center of mass to approximately `-1.15m`.
- Use angular damping around `4.5` and cap angular velocity to reduce rapid arcade-like tumbling.
- Preserve unconstrained pitch and roll. Do not add automatic upright torque or rotation constraints.

### Rollover Defeat

- A rollover is an upright angle of at least `75 degrees`.
- The tank must remain past that threshold for `2 seconds` before defeat.
- Brief impacts or steep-slope transitions reset the timer when the tank returns below the threshold.
- Rollover defeat destroys/disables the player and tells `BattlefieldDirector` to complete defeat with a clear reason.

### Wheel Animation

- Derive signed speed from Rigidbody velocity projected onto the tank's forward direction.
- Rotate authored wheels around their local axle, `X`.
- Forward and reverse motion rotate in opposite directions.

### Player Model

- Change player body material to a contrasting mustard/tan color that is visually distinct from green/brown terrain.
- Preserve the silly face and helmet personality.
- Add a readable turret ring, armored turret body, gun mantlet, commander cupola, hatch, and separate barrel.
- Preserve required names and damaged variants used by Unity binding and validation.

### Enemy Health Presentation

- Red background represents lost health.
- Green horizontal fill represents remaining health and shrinks from right to left as damage is taken.
- Percentage text remains visible above the bar.
- Damaged health remains green rather than shifting toward yellow/red because the red loss layer already communicates damage.

## Validation

- Tests lock heavy Rigidbody policy and sustained rollover defeat timing.
- Tests lock signed forward/reverse wheel animation around local X.
- Model validation requires turret ring, mantlet, and cupola parts.
- Enemy health tests require green healthy fill and red damage background.
- Full EditMode and PlayMode suites pass with zero console errors.
- Unity gameplay test remains manual before any later WebGL build.

## Explicit Exclusion

Do not build WebGL or create a new PythonAnywhere ZIP in this checkpoint.
