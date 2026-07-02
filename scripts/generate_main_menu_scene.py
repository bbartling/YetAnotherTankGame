"""Generate the Yet Another Tank Game main menu scene in Blender."""
import bpy
import os

BLEND = r"C:\Users\ben\Documents\CannonPhysicsSim\BlenderSource\MainMenu\YetAnotherTankMainMenu.blend"
SCRIPT_NAME = "GenerateMainMenuScene"

if bpy.data.is_saved and bpy.data.filepath != BLEND:
    bpy.ops.wm.open_mainfile(filepath=BLEND)

if SCRIPT_NAME not in bpy.data.texts:
    raise RuntimeError("Open YetAnotherTankMainMenu.blend in Blender and run from MCP, or regenerate via Blender MCP execute_blender_code.")

exec(compile(bpy.data.texts[SCRIPT_NAME].as_string(), SCRIPT_NAME, "exec"))
