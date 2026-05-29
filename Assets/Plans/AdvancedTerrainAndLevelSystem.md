# Project Overview
- Game Title: Cannon Physics Sim
- High-Level Concept: Physics-based tank simulator where the player destroys enemy turrets on a castle across multiple levels of increasing difficulty.
- Players: Single player.
- Inspiration / Reference Games: Tank games, Siege games.
- Tone / Art Direction: Cardboard / Low-poly.
- Target Platform: PC.
- Screen Orientation / Resolution: Landscape 1920x1080.
- Render Pipeline: Built-in.

# Game Mechanics
## Core Gameplay Loop
1. Start at a base.
2. Navigate mountainous terrain.
3. Aim and fire cannon at enemy turrets on a castle.
4. Destroy all turrets to progress to the next, harder level.
5. Zoom in for sniper-like precision shots.

## Controls and Input Methods
- Driving: Arrow keys.
- Turret: A/D.
- Elevation: W/S.
- Firepower: Q/E.
- Firing: Space or Left Click.
- Sniper Zoom: Right Click.

# UI
- Level counter.
- Turret count.
- Existing dashboard elements.

# Key Asset & Context
- `LevelManager.cs`: Controls level progression and spawning.
- `TerrainGenerator.cs`: Generates random terrain and mountain walls.
- `EnemyTurret.cs`: AI for castle defenses.
- `SniperZoom.cs`: Camera FOV handler.
- `ProjectileCameraController.cs`: Updated to detect mountain hits.

# Implementation Steps

## 1. Terrain & Map Scaling
- Create `TerrainGenerator.cs` to generate random terrain with Perlin noise.
- Implement "mountain walls" at the perimeter.
- Update `ProjectileCameraController.cs` to trigger instant explosion on "Mountain" tagged objects.
- Double the current map scale (target ~1000x800).
- Assign a "Mountain" tag to the generated terrain.

## 2. Enemy Castle & Turrets
- Create `EnemyTurret.cs` with AI for aiming and firing at the player.
- Implement level-based difficulty scaling (number of turrets, fire rate, accuracy).
- Create a `LevelManager` to spawn the castle and turrets at the start of each level.
- Turrets will be tagged "EnemyTurret".
- Update `ProjectileCameraController.cs` to detect hits on "EnemyTurret" and notify `LevelManager`.

## 3. Sniper Zoom
- Implement `SniperZoom.cs` to handle Right Click (Mouse 1) FOV reduction.
- Add smooth interpolation for the zoom effect.
- Attach to the player's Main Camera.

## 4. Integration & UI
- Modify `MenuManager.cs`:
    - Link the Play button to `LevelManager.StartGame()`.
- Update the UI to display the current Level and remaining Turrets.
- Ensure the Player Tank starts at a safe position (e.g., [0, 5, -300]).

## 4. Integration
- Connect `MenuManager` Play button to `LevelManager` level generation.
- Ensure old city generation is replaced by the new level system.

# Verification & Testing
- Verify terrain is randomized on "Play".
- Test that projectiles explode immediately on mountain hit.
- Confirm turret count increases with each level.
- Test right-click zoom functionality.
- Drive to the map edges to verify mountain walls.
