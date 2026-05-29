# Project Overview
- Game Title: Army Tank Game
- High-Level Concept: A physics-based tank game where players navigate a procedurally generated cardboard city, destroying box structures and engaging enemy tanks.
- Players: Single-player vs AI tanks.
- Inspiration: Battlefield, World of Tanks, traditional tank arcade games.
- Tone / Art Direction: Cardboard aesthetic, physics-driven chaos.
- Target Platform: PC.
- Render Pipeline: Built-in Render Pipeline.

# Game Mechanics
## Core Gameplay Loop
1.  **Navigate**: Use WASD to drive the tank through the city.
2.  **Aim & Fire**: Use EQ or Mouse to aim the turret and Fire (Space/Click) to launch fireball shells.
3.  **Destroy**: Collapse building structures made of cardboard boxes.
4.  **Combat**: Engage and destroy enemy AI tanks before they destroy you.

## Controls and Input Methods
- **W/S**: Move Forward/Backward.
- **A/D**: Turn Left/Right.
- **E/Q**: Rotate Turret (Angle) / Adjust Elevation (Vertical).
- **Space/Left Click**: Fire Shell.
- **ESC**: Pause/Menu.

# UI
## Main Menu
- **Play Button**: Starts the game.
- **Controls Button**: Shows a panel with the keyboard layout.
- **HUD**: A simple crosshair in the center of the screen (1st person view).

# Key Asset & Context
- **Scripts**:
    - `TankController.cs`: Handles player movement, turret rotation, and firing (extending or replacing `CannonManager`).
    - `CityGenerator.cs`: Procedurally generates piles of boxes (buildings), ramps, and bridges.
    - `EnemyTankAI.cs`: Simple AI to target the player and fire.
    - `GameManager.cs`: Handles state (Menu, Playing) and map setup.
- **Prefabs**:
    - `PlayerTank`: Updated cannon with tracks/wheels and turret.
    - `EnemyTank`: Similar to player but with AI logic.
    - `BoxBuilding`: A prefab or script-generated collection of rigidbodies.
    - `FireballProjectile`: Updated projectile with fireball trail and explosion VFX.

# Implementation Steps
## 1. Map & Environment Setup
- Scale "Ground" object to double its current size.
- Create `CityGenerator.cs` to spawn `CardBoardCastle`-style buildings, ramps, and bridges randomly across the map.
- **Dependency**: None.

## 2. Tank Movement & Control
- Create `TankController.cs`.
- Implement Rigidbody-based movement (WASD).
- Implement Turret rotation and barrel elevation (EQ/Mouse).
- Integrate existing `CannonManager` logic for power and fire points.
- **Dependency**: 1.

## 3. First-Person Camera
- Attach the `Main Camera` to the tank's turret or barrel.
- Update `ProjectileCameraController` to ensure the camera returns to the turret after projectile destruction.
- **Dependency**: 2.

## 4. Enemy AI Tanks
- Create `EnemyTank` prefab.
- Implement `EnemyTankAI.cs` for basic tracking and firing at the player.
- **Dependency**: 2, 3.

## 5. VFX & SFX Update
- Update `ProjectileCameraController` and projectile prefab:
    - Replace current trail with a "Fireball" particle system/trail.
    - Enhance impact explosion VFX and sound.
- Ensure existing "fight" sounds are maintained.
- **Dependency**: None.

## 6. UI & Main Menu
- Create a `Canvas` with a "Play" and "Controls" menu.
- Logic to transition from Menu to Game state.
- **Dependency**: 2.

# Verification & Testing
- **Movement Test**: Verify WASD moves the tank naturally.
- **Aiming Test**: Verify EQ/Mouse adjusts angle and elevation correctly.
- **Firing Test**: Verify projectiles launch with expected power and trajectory.
- **Generation Test**: Verify buildings/ramps/bridges spawn and are interactable (physics-enabled).
- **Combat Test**: Verify AI tanks detect and fire at the player.
- **UI Test**: Verify Play button starts the game and Controls menu displays correctly.
