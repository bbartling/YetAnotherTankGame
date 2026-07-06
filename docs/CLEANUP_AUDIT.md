# Cleanup Audit

## Scope

Audit target: simplify startup/menu architecture while preserving the three gameplay scenes and locked shared tank-driving system.

Required gameplay scenes remain:

- `Assets/Scenes/Practice.unity` - WAR / randomized battlefield.
- `Assets/Scenes/TankDrivingPractice.unity` - driving practice.
- `Assets/Scenes/TankTargetPractice.unity` - cannon practice / shooting range.

## Startup And Scene Baseline

| Check | Result |
|---|---|
| Git branch/status | `develop...origin/develop`; no pre-existing worktree changes at startup. |
| Unity project root | `C:/Users/ben/Documents/CannonPhysicsSim`. |
| Unity version/build target | Unity `6000.4.6f1`; project platform `WebGL`. |
| Active scene at startup | `Assets/Scenes/TankDrivingPractice.unity`. |
| Build settings before cleanup | `MainMenu`, `Practice`, `TankDrivingPractice`, `TankTargetPractice`. |
| Final scene strategy | Keep `MainMenu` as a static/authored three-button scene selector, with all three gameplay scenes directly loadable. |
| Scene script scan | No missing scripts in `Practice`, `TankDrivingPractice`, `TankTargetPractice`, or `MainMenu`. |
| Initial console | Repeated `BoxCollider does not support negative scale or size` errors from five mirrored `brace` roots in `TankDrivingPractice`. |
| Console fix | Normalized five `brace` root scales in `TankDrivingPractice` and saved the scene; Unity diagnostic found `negative BoxCollider effective scales=0`. |

## Menu / Startup Scripts

| Script/file | Current references found | Decision | Reason | Validation performed |
|---|---|---|---|---|
| `Assets/Scripts/MenuManager.cs` | Referenced by `Practice.unity`, `MainMenu.unity`, `GameplayTestApi`, `PlayStartFlowPlayModeTests`, docs/plans. | Kept and refactored smaller. | Required as current start-mode API and `GameplayMode` enum for tests and `GameplayTestApi`; stripped of runtime UI construction and presentation coupling. | `rg` references; Unity scene script summary; targeted red/green PlayMode test. |
| `Assets/Scripts/MainMenuPresentation.cs` | Previously referenced by `MenuManager`, `MainMenuSceneBuilder`, `WarMenuScenePatcher`; not attached in `MainMenu.unity` scene summary. | Removed. | Presentation-only runtime generation: title, volume slider, cutscene host, button styling, pulse. Not required for gameplay modes. | `rg` references; scene summary showed no scene attachment; compile passed after deletion. |
| `Assets/Scripts/MainMenuActionCutscene.cs` | Previously referenced by `MainMenuPresentation`, `MainMenuSceneBuilder`; not attached in `MainMenu.unity` scene summary. | Removed. | Menu-only decorative cutscene that creates tank, camera, shell, target, and ground at runtime. Not gameplay projectile camera or combat. | `rg` references; runtime creation search; scene summary showed no scene attachment; compile passed after deletion. |
| `Assets/Scripts/MainMenuBackdrop.cs` | Previously referenced by `SceneHierarchyCleanup` delete-name list and hidden by `MainMenuActionCutscene`; no scene attachment found. | Removed. | Legacy/decorative menu display tank only. No gameplay references. | `rg` references; scene summary showed no scene attachment; compile passed after deletion. |
| `Assets/Scripts/MainMenuWarButtonPulse.cs` | Previously referenced only by `MainMenuPresentation`; no scene attachment found. | Removed. | Presentation-only animated button effect. Not required for scene selection. | `rg` references; scene summary showed no scene attachment; compile passed after deletion. |
| `Assets/Scripts/MenuCanvasScalerBootstrap.cs` | Was attached in `MainMenu.unity` and `Practice.unity`; referenced by `MenuManager`, `MainMenuSceneBuilder`, `WarMenuScenePatcher`. | Removed after scene components were removed. | Menu/start-screen bootstrap fallback for generated UI. A normal authored `CanvasScaler` is enough for a static menu. | Removed components from `MainMenu` and `Practice`; saved scenes; compile passed after deletion. |

## Related Editor Scripts

| Script/file | Current references found | Decision | Reason | Validation performed |
|---|---|---|---|---|
| `Assets/Editor/MainMenuSceneBuilder.cs` | Menu item builder referenced `MainMenuPresentation`, `MainMenuActionCutscene`, `MenuCanvasScalerBootstrap`, and build-settings setup. | Removed. | Generated the old menu scene and decorative runtime menu dependencies. Conflicted with static/authored menu target. | File read; `rg` scene-name references; compile passed after deletion. |
| `Assets/Editor/WarMenuScenePatcher.cs` | Referenced `MainMenuSceneBuilder`, `MainMenuPresentation`, `MenuCanvasScalerBootstrap`, and created button rows/text. | Removed. | Programmatic menu patcher for the legacy menu stack. Not needed after static menu simplification. | File read; `rg` references; compile passed after deletion. |
| `Assets/Editor/SceneHierarchyCleanup.cs` | Referenced `MainMenuSceneBuilder.MainMenuScenePath` and legacy menu object names. | Refactored. | Useful cleanup tool, but no longer depends on removed menu builder. | File read; `rg` references; compile passed after refactor. |

## Runtime `new GameObject` / `AddComponent` Classification

| Area | Files | Classification | Decision |
|---|---|---|---|
| Menu/start-screen UI and presentation | `MenuManager`, removed menu presentation/cutscene/backdrop/pulse/bootstrap scripts, removed editor menu builders/patchers | Suspicious/wonky menu generation. | Refactored to tiny scene-selection layer and removed presentation-only code. |
| Gameplay battle systems | `BattlefieldDirector`, `EnemyTankAI`, `EnemyTankSpawner`, `EnemyTurret`, `EnemyHealthBar`, `MiniMapHUD` | OK gameplay runtime generation. | Keep. These support objectives, enemies, health UI, and battlefield flow. |
| Terrain/destruction/effects | `CraterTerrain`, `DestructibleGround`, `BreakableTree`, `CastleDamageReceiver`, `BattlefieldEffectController`, `TankOverdriveController` | OK gameplay runtime generation. | Keep. Locked crater/destruction/overdrive smoke behavior. |
| Weapons/cameras/scope | `TankController`, `TankMachineGun`, `ProjectileCameraController`, `SniperRangeFinder`, `TankVoidFallController` | OK gameplay runtime generation. | Keep. Locked projectile camera, lobbed shells, scope/rangefinder, void fall behavior. |
| Practice mode support | `PracticeModeInputPolicy`, `PracticeReturnController`, `PracticeCompletionFanfare`, `DrivingLapProgressHud`, `TargetPracticeProgressHud`, `GameplayTestApi` practice fixture helpers | OK practice/test support. | Keep unless later separated by a specific validation change. |
| Tank shared driving setup | `TankController`, `TankOverdriveSetup`, `BuiltInWheelTankDrive`, `TankAudioController`, visual installers | OK tank setup/runtime helpers. | Keep. Shared driving locks depend on these paths. |

## Shared Driving Verification

| File/reference | Finding | Decision |
|---|---|---|
| `Assets/Scripts/Tank/TankDrivingProfile.cs` | Explicitly applies to `Practice`, `TankDrivingPractice`, and `TankTargetPractice`. | Keep unchanged. |
| `Assets/Scripts/Tank/TankGameplayTuning.cs` | Contains locked shared practice/overdrive constants. | Keep unchanged. |
| `Assets/Scripts/Tank/TankDriveController.cs` | `ApplyMotocrossPracticeProfile()` delegates to `TankDrivingProfile`. | Keep unchanged. |
| `Assets/Scripts/Tank/BuiltInWheelTankDrive.cs` | Uses `TankDrivingProfile` speed limits. | Keep unchanged. |
| `Assets/Scripts/TankController.cs` | Calls `ApplySharedDrivingHandling()` for locked scenes. | Keep unchanged unless tests require a menu-only compile fix. |
| `Assets/Scripts/PracticeModeInputPolicy.cs` | Applies `TankDrivingProfile.ApplyToTankController(tank)`. | Keep. Required by practice scene input locks. |

## Files That Look Dead But Are Kept For Now

| File | Why kept |
|---|---|
| `Assets/Scripts/MenuManager.cs` | It was overgrown, but not dead. Tests and `GameplayTestApi` use its mode enum and start methods, so it was reduced instead of removed. |
| `Assets/Scripts/GameplayTestApi.cs` | Contains legacy programmatic practice fixtures, but also powers validation and restart/current-mode behavior. Not a menu-only cleanup target. |
| Practice HUD/return scripts | They create UI at runtime, but are gameplay/practice progress affordances rather than start-screen presentation. |

## Planned Cleanup Checkpoints

1. Add/adjust tests around the tiny menu contract.
2. Refactor `MenuManager` to serialized/static button handling only.
3. Remove menu presentation/cutscene/backdrop/pulse/bootstrap code and scene components.
4. Remove or refactor editor-only menu builders/patchers so compile remains clean.
5. Update build settings and docs to match the final scene strategy.
6. Re-run compile, EditMode tests, PlayMode tests, scene load checks, missing-script scan, and console audit.
