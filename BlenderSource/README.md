# Blender Source Contract

These low-poly source files are generated and maintained through Blender MCP.

- Run `scripts/generate_silly_tank_models.py` inside Blender to reproduce the tracked `.blend` and Unity FBX files.
- Scale is `1 Blender unit = 1 Unity meter`.
- Tank models contain separately named `Hull`, `Turret`, `Barrel`, `LeftTrack`, and `RightTrack` objects plus damaged variants.
- Castle, turret, and tree kits contain intact and damaged/collapsed variants.
- Unity runtime FBX files live under `Assets/Resources/Models`.
- Do not silently replace required models with primitive placeholders.
