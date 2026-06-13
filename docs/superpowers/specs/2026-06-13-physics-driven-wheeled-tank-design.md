# Physics-Driven Wheeled Tank Design

## Goal

Replace the current crater-bridging tracked controller and paddle-wheel visuals with a silly eight-wheel armored vehicle that accelerates and brakes naturally, follows terrain into craters, can tip and roll, rotates its visible turret correctly, and provides useful third-person and barrel-aligned scope cameras.

## Locked Features

This change must preserve:

- Physical lobbed cannonballs.
- Projectile camera activation, impact linger, and return.
- Terrain crater deformation.
- Scope/rangefinder and ballistic information.
- Enemy standoff behavior.
- Tank, tree, castle, and turret destruction.
- Blender source/runtime model pairing.
- WebGL and PythonAnywhere release pipeline.

## Root Causes

### Crater Bridging

`TankController.TryGetTerrainCorrection` samples every collider corner and raises the whole tank to the highest terrain point found. This prevents terrain penetration, but it also prevents the vehicle from descending into holes or craters whenever one corner remains on higher ground.

### Paddle-Wheel Tracks

`TankVisualAnimator` rotates the long `LeftTrack` and `RightTrack` rectangular meshes. The meshes rotate around their local axes like two large paddles rather than representing moving tracks or wheels.

### Invisible Turret Rotation

The gameplay `TurretYawPivot` and `BarrelPivot` rotate, but the Blender `Turret` and `Barrel` meshes are siblings under `SillyModelVisual`. The compass reflects gameplay yaw while the visible turret remains stationary.

### Low Camera And Weak Scope

`TankOrbitCamera` focuses only `2.2m` above the tank and follows at `8m`. Right-click changes FOV and shows rangefinder UI but does not switch to a camera located and aligned at the barrel.

### Artificially Low Speed

`TankDriveController.OnEnable` hard-clamps the vehicle to `2.5 m/s` forward and `1 m/s` reverse with at least six seconds of acceleration.

## Vehicle Model

The player becomes a silly wheeled armored vehicle:

- Four chunky square-edged wheels per side.
- Eight wheel visuals named `LeftWheel_0` through `LeftWheel_3` and `RightWheel_0` through `RightWheel_3`.
- Wheels are visually square/chunky but rotate around the correct axle.
- Narrow decorative side guards replace the long track blocks.
- Existing grumpy face, helmet, antenna, hull, turret, barrel, and damage variants remain.
- Enemy vehicles may retain existing silhouettes initially, but the player model and animation define the new handling contract.

## Physics And Handling

### Contact And Suspension

Replace five broad track probes with eight wheel contact probes. Each wheel probe:

- Casts downward from its suspension mount.
- Records contact point, surface normal, compression, and grounded state.
- Applies spring and damper force at the contact point.
- Contributes traction only while grounded.

The vehicle must no longer continuously snap to the highest ground point. Anti-penetration correction is limited to startup/reset recovery or severe mesh penetration. Normal driving relies on Rigidbody collision, gravity, and wheel suspension.

### Speed And Response

Target defaults:

- Forward speed: `7.5 m/s`.
- Reverse speed: `3.5 m/s`.
- Time to near-full forward speed: approximately `4 seconds`.
- Braking time from full speed: approximately `2 seconds`.
- Steering remains speed-sensitive.
- Uphill travel loses speed and increases engine strain.
- Downhill travel retains gravity-driven momentum.
- Very steep slopes lose traction instead of becoming invisible walls.

### Tipping And Rolling

The vehicle is allowed to:

- Pitch into crater walls.
- Roll sideways on steep terrain.
- Bottom out.
- Become stuck.
- Fully overturn.

Continuous upright assistance is removed. Angular damping remains modest to avoid numerical instability, not to keep the vehicle upright. A recovery action may be added only as a deliberate player command after the vehicle is overturned and nearly stationary; it must not activate automatically during ordinary driving.

### Steering

Use skid-steer style handling suitable for an eight-wheel armored vehicle:

- Low-speed turning can be tight.
- High-speed turning is wider and less responsive.
- Steering applies differential longitudinal forces across left/right grounded wheels plus limited yaw torque.
- Lateral traction resists arcade-style sliding without preventing realistic slope slip.

## Turret And Barrel

The model installer must bind Blender visuals to gameplay pivots:

- Visible `Turret` and turret damage variant follow `TurretYawPivot`.
- Visible `Barrel` and barrel damage variant follow `BarrelPivot`.
- Rotation pivots remain at the authored gameplay pivot locations.
- Turret yaw is limited by configured rotation speed.
- Barrel elevation remains tied to ballistic aiming.
- The compass and visible turret must report/show the same yaw.

## Cameras

### Third-Person Camera

Raise the camera:

- Focus height: approximately `4.5m`.
- Follow distance: approximately `11m`.
- Add a slight downward viewing angle.
- Preserve terrain/castle collision avoidance.
- Keep the vehicle and terrain ahead visible.

### Barrel Scope

Right-click enters a barrel-aligned scope:

- Camera position is near the barrel/turret sight line.
- Camera forward direction follows actual cannon direction.
- Scope uses a narrow FOV.
- Existing rangefinder and ballistic information remain visible.
- Releasing right-click returns to third-person view.
- Firing while scoped still activates the projectile camera and returns correctly.

## Validation

Automated validation must cover:

- Forward speed near `7.5 m/s` and reverse near `3.5 m/s`.
- Gradual acceleration and braking.
- Wheel probe suspension force and independent grounded state.
- Vehicle center descends when wheel contacts enter a crater.
- No highest-corner terrain snap during normal driving.
- Vehicle may exceed safe roll/pitch angles without automatic correction.
- Eight wheel visuals exist and rotate around their correct axle.
- Visible turret follows gameplay turret yaw.
- Visible barrel follows gameplay barrel elevation.
- Third-person camera focus height and distance meet the new minimums.
- Scope camera aligns closely with cannon forward direction.
- Projectile camera, lobbed cannonball, damage, crater, enemy, and release tests remain green.

Manual validation must include driving into and out of several crater sizes, intentionally rolling the vehicle, observing wheel motion, rotating the visible turret, firing from scope, and confirming projectile-camera return.

## Delivery Checkpoints

1. Physics tests and eight-wheel suspension.
2. Speed, acceleration, braking, traction, tipping, and crater traversal.
3. Blender wheeled model generation and wheel animation.
4. Visible turret/barrel binding.
5. Raised third-person and barrel-aligned scope cameras.
6. Full tests, manual gameplay inspection, WebGL build, and PythonAnywhere ZIP.
