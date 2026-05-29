# Project Overview
- Game Title: Cannon Physics Simulator
- High-Level Concept: A physics-based tank/cannon simulator where players fire projectiles at cardboard-style targets in a split-screen view.
- Players: Single player
- Inspiration / Reference Games: Besiege, TABS
- Tone / Art Direction: Cardboard / Crafty
- Target Platform: PC (Standalone Windows)
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: Built-in

# Game Mechanics
## Core Gameplay Loop
The player drives a tank, aims a cannon using sliders on a dashboard, and fires projectiles to destroy buildings and towers made of cardboard.
## Controls and Input Methods
- WASD for tank movement.
- E/Q for turret rotation.
- R/F or Up/Down for barrel elevation.
- Space or Left Click to fire.
- Dashboard sliders for mass, elevation, power, and angle.

# UI
- **Dashboard**: Needs alignment. Elements (Sliders, Labels, Values) will be organized using a Vertical Layout Group for a clean, non-jumbled appearance.
- **Split-Screen**: 
    - Left side (Rect: 0, 0, 0.5, 1): First-person view from the tank.
    - Right side (Rect: 0.5, 0, 0.5, 1): Projectile tracking view during flight.

# Key Asset & Context
- `Assets/Prefabs/ExplosionEffect.prefab`: Needs material assignment (currently pink).
- `Assets/Prefabs/EnemyTank.prefab` & `PlayerTank`: Need material fixes (currently pink underneath).
- `Assets/Scripts/TankController.cs`: Handles player movement and firing.
- `Assets/Scripts/ProjectileCameraController.cs`: Handles camera tracking for the projectile.
- `CardBoardCastle`: Source of cardboard materials.

# Implementation Steps

## 1. Fix UI Alignment (Dashboard)
- **File**: `Assets/Scenes/Practice.unity`
- **Action**: 
    - Create a "DashboardPanel" under the Canvas.
    - Add a `VerticalLayoutGroup` to the DashboardPanel.
    - Move all sliders and their labels into this panel.
    - Fix the text alignment on the buttons and sliders.
- **Dependency**: None

## 2. Fix Pink Materials
- **File**: `Assets/Prefabs/ExplosionEffect.prefab`, `Assets/Prefabs/EnemyTank.prefab`
- **Action**:
    - Assign a valid material to the `ParticleSystemRenderer` on the `ExplosionEffect`. Use a default particle shader (e.g., `Particles/Standard Unlit`).
    - Check the `Body` mesh of the tanks and ensure their materials use the `Standard` shader and are not missing textures. If "pink underneath" refers to a specific mesh, fix its material.
- **Dependency**: None

## 3. Split-Screen Camera Setup
- **File**: `Assets/Scenes/Practice.unity`, `Assets/Scripts/ProjectileCameraController.cs`, `Assets/Scripts/TankController.cs`
- **Action**:
    - **Scene**: 
        - Setup the `Main Camera` (Left) with Viewport Rect `(0, 0, 0.5, 1)`. Attach it to the `PlayerTank` turret or a first-person position.
        - Create a `Projectile Camera` (Right) with Viewport Rect `(0.5, 0, 0.5, 1)`.
    - **ProjectileCameraController.cs**: 
        - Update to target the "Projectile Camera" instead of `Camera.main`.
        - Ensure it doesn't reset the `Main Camera` position on destruction.
    - **TankController.cs**:
        - Ensure it references the correct cameras.
- **Dependency**: Step 1

## 4. Environment: Cardboard Buildings & Ramp
- **File**: `Assets/Scripts/CityGenerator.cs` or new script `Assets/Scripts/TargetSpawner.cs`
- **Action**:
    - Find materials from the `CardBoardCastle` object.
    - Spawn exactly 4 Buildings and 4 Towers in random locations using these materials.
    - Create/Adjust a large `Ramp` object that is driveable.
- **Dependency**: None

## 5. Arc Visualization
- **File**: `Assets/Scripts/TankController.cs`
- **Action**:
    - Ensure the `LineRenderer` uses a non-pink material (e.g., a simple colored Unlit material).
    - Verify the `UpdateTrajectory` logic is visible and matches current physics settings.
- **Dependency**: None

# Verification & Testing
- **UI**: Run the game and verify the dashboard sliders are aligned and labels are readable.
- **Pink Effects**: Fire a projectile and verify the explosion and tanks are no longer pink.
- **Split-Screen**: Verify the left screen shows the tank's view and the right screen follows the projectile flight.
- **Buildings**: Verify 4 buildings and 4 towers spawn with cardboard materials.
- **Ramp**: Drive the tank onto the ramp to ensure it's functional.
- **Arc**: Adjust power/elevation and verify the prediction line updates correctly.
