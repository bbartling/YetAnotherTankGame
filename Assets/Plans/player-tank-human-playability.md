# Player Tank Human Playability

## Controls

- W/S or arrow keys: forward and reverse track drive.
- A/D or arrow keys: tank-style steering and pivot turns.
- Mouse X: turret yaw.
- Q/E: lower and raise the cannon barrel.
- Mouse wheel or PageUp/PageDown: secondary fine barrel elevation.
- Left mouse or Space: fire cannon from `CannonFirePoint`.
- Right mouse: scope/rangefinder view aligned to `CannonFirePoint.forward`.
- F: machine gun.
- Shift: low gear / stabilized creeping, not sprint.

## Runtime Tank Hierarchy

- `PlayerTank` owns the Rigidbody, gameplay scripts, and child collider bodies.
- `TurretYawPivot` is the authoritative yaw pivot at the turret ring.
- `BarrelPitchPivot` is the authoritative pitch pivot for cannon elevation.
- `CannonFirePoint` is parented to `BarrelPitchPivot` and points down the muzzle.
- `SillyModelInstaller` loads `Resources/Models/Tanks/SillyPlayerTank` as `SillyModelVisual`.
- Legacy primitive `BodyVisual`, `TurretMesh`, and `BarrelVisual` keep colliders only. Their MeshRenderer/MeshFilter components were removed from `Practice.unity` so the Blender model is the only enabled tank mesh in human play.

## Physics Approach

The player uses raycast suspension through `WheeledSuspensionController`, not transform-position drive. The root Rigidbody uses gravity and is non-kinematic when the Play button starts battle. Eight track contact points are split left/right, with differential track forces driven by `TankController`.

Current `Practice` tuning:

- `probeHeight`: 0.85
- `suspensionTravel`: 2.2
- `wheelRadius`: 0.45
- `springStrength`: 22
- `damperStrength`: 12

## Manual Test Checklist

1. Open `Assets/Scenes/Practice.unity`.
2. Press Play in Unity.
3. Press the in-game Play button.
4. Confirm the player tank is the Blender `SillyPlayerTank` model on `MapRoot/Ground`, not old Unity block meshes.
5. Confirm the tank Rigidbody is dynamic, gravity is enabled, and suspension contact is present.
6. Drive with W/S and pivot with A/D.
7. Rotate turret with mouse X.
8. Lower and raise the barrel with Q/E.
9. Right-click scope view and confirm it follows turret yaw and barrel elevation exactly.
10. Fire with left mouse or Space and confirm the shell leaves `CannonFirePoint` along the barrel direction.

## Validation

Focused validation currently covers:

- `PlayerTankSceneHierarchyEditModeTests.PracticeScene_PlayerTankHasCleanPlayablePivotHierarchy`
- `SillyModelInstallerPlayModeTests.Installer_HidesLegacyPrimitiveRenderersWhenUsingBlenderModel`
- `PlayerTankHumanPlayabilityTests`
- `TankCameraPlayModeTests.ChaseCamera_DefaultsProvideRaisedTacticalView`
- `PlayStartFlowPlayModeTests` dynamic Play button startup expectations

Live MCP validation also invoked the actual `MenuManager.playButton.onClick` in Play Mode and confirmed:

- `SillyModelVisual` loaded from `Models/Tanks/SillyPlayerTank`
- required Blender hull/turret/barrel/wheel parts present
- zero enabled legacy tank mesh renderers
- generated `MapRoot/Ground` with `CraterTerrain`
- dynamic non-kinematic Rigidbody with gravity
- active suspension ground contacts
- scope camera forward angle to `CannonFirePoint.forward` is 0 degrees

## Known Limitations

- Suspension and slope behavior are playable but still need feel tuning after longer manual driving sessions.
- The generated terrain height varies sharply near spawn; suspension travel is intentionally long enough to maintain contact on that generated map.
- Wheel visuals are driven by the existing `TankVisualAnimator`; deeper track deformation is not implemented.
