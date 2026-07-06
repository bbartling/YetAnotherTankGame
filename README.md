# Silly Tank Hill War

A silly-looking tactical armored-vehicle game built in Unity 6000.4.6f1. The eight-wheel player tank accelerates deliberately, follows rough terrain, can tip or roll, lobs physical cannonballs, uses a barrel-aligned scope/rangefinder, watches shots through the projectile camera, and fights standoff enemies around destructible castles and trees.

## Controls

- `W/S`: slow forward/reverse throttle
- `A/D`: heavy hull steering
- Mouse: aim turret/camera
- Right mouse: aligned scope/rangefinder
- Left mouse or `Space`: fire lobbed cannonball
- Projectile camera returns to the tank after impact

## Development

Open `Assets/Scenes/MainMenu.unity` for the simple scene selector, or open a gameplay scene directly:

- `Assets/Scenes/TankDrivingPractice.unity` - driving practice.
- `Assets/Scenes/TankTargetPractice.unity` - cannon practice / shooting range.
- `Assets/Scenes/Practice.unity` - WAR / randomized battlefield.
- `Assets/Scenes/BallDriveTest.unity` - duplicated driving course with a bare-bones WASD rolling sphere test controller.

The menu is authored/static and loads those gameplay scenes plus the ball-drive test scene. Current agent workflow is documented in `AGENTS.md`.

Run Unity Test Framework EditMode and PlayMode suites before release. The current release validation procedure is in `Docs/TESTING_AND_VALIDATION.md`.

## WebGL And PythonAnywhere

Run:

```powershell
.\scripts\build_webgl_pythonanywhere.ps1
```

The script runs EditMode tests, builds WebGL into `Builds/WebGL`, refreshes `pythonanywhere_flask/webgl`, and creates `tank_game_pythonanywhere.zip`. PythonAnywhere setup and Safari checks are in `Docs/WEBGL_PYTHONANYWHERE_DEPLOY.md`.
