# Silly Tank Hill War

A deliberately slow, silly-looking tactical tank game built in Unity 6000.4.6f1. Tanks creep over hills, lob physical cannonballs, use a scope/rangefinder, watch shots through the projectile camera, and fight standoff enemies around destructible castles and trees.

## Controls

- `W/S`: slow forward/reverse throttle
- `A/D`: heavy hull steering
- Mouse: aim turret/camera
- Right mouse: aligned scope/rangefinder
- Left mouse or `Space`: fire lobbed cannonball
- Projectile camera returns to the tank after impact

## Development

Open `Assets/Scenes/Practice.unity`. Locked behavior and agent workflow are documented in `AGENTS.md` and `Docs/GAMEPLAY_DESIGN_LOCK.md`.

Run Unity Test Framework EditMode and PlayMode suites before release. The current release validation procedure is in `Docs/TESTING_AND_VALIDATION.md`.

## WebGL And PythonAnywhere

Run:

```powershell
.\scripts\build_webgl_pythonanywhere.ps1
```

The script runs EditMode tests, builds WebGL into `Builds/WebGL`, refreshes `pythonanywhere_flask/webgl`, and creates `tank_game_pythonanywhere.zip`. PythonAnywhere setup and Safari checks are in `Docs/WEBGL_PYTHONANYWHERE_DEPLOY.md`.
