# Testing And Validation

## Release Gate

A release is valid only when:

- Unity compiles with zero errors.
- EditMode and PlayMode suites pass.
- The console has zero gameplay or compile errors.
- Projectile camera, lob arc, scope/rangefinder, craters, enemy standoff, damage states, castle collapse, and tree knockdown remain present.
- WebGL builds successfully and required files are listed in both manifests.
- `tank_game_pythonanywhere.zip` contains `flask_app.py` and the complete `webgl/` directory.

## Automated Suites

- EditMode: ballistics, required Blender/runtime models, renderer bounds, build scenes, WebGL settings, Flask layout, and build pipeline.
- PlayMode: slow/heavy tank policy, slope policy, turret independence, enemy perception/standoff/pathing, damage states, bounded effects, model installation, recoil/track animation, audio fallbacks, and distance rolloff.

Use the Unity Test Runner or Unity MCP. Classify results before processing:

- `SIMPLE`: pass/fail, timeout/setup, syntax/import, HTTP, selector, or missing UI failures.
- `COMPLEX`: ambiguous behavior, timing races, security, performance degradation, or failures spanning components.

## Manual Locked-Feature Check

1. Load `Practice.unity` and drive forward; confirm the tank creeps rather than sprints.
2. Drive uphill; confirm speed loss and visual slope alignment.
3. Enter right-click scope; confirm reticle/range information follows the actual aim.
4. Fire a lobbed cannonball; confirm projectile camera activates, follows impact, and returns.
5. Confirm enemy tanks engage from range, stop to aim, reload, reposition, and avoid ramming.
6. Damage a tank until smoke/fire/wreck states appear.
7. Hit trees and castle pieces; confirm knockdown/crumble effects and bounded debris.

## Safari Manual Check

1. Open the direct PythonAnywhere URL in Safari, not an iframe.
2. Confirm loading completes and keyboard/mouse controls respond.
3. Verify scope, cannon fire, projectile camera, and return-to-tank camera.
4. Verify engine, cannon, projectile, and impact audio.
5. Play for at least three minutes and confirm no freeze or runaway debris/effects.

## Current Release Evidence

- EditMode: `10/10` passed.
- PlayMode: `40/40` passed.
- Local Flask smoke: `/`, `/Build/WebGL.loader.js`, and `/Build/WebGL.data` returned HTTP 200.
- `/robots.txt` returned HTTP 200.
- Automated local Chrome launch was blocked by the managed shell policy; direct Chrome and Safari play checks remain manual release checks.
