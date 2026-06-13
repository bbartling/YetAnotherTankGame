import bpy
import math
import os


REPO = r"C:\Users\ben\Documents\CannonPhysicsSim"


def ensure_dirs():
    for path in [
        "BlenderSource/Tanks",
        "BlenderSource/Castle",
        "BlenderSource/Turrets",
        "BlenderSource/Trees",
        "Assets/Resources/Models/Tanks",
        "Assets/Resources/Models/Castle",
        "Assets/Resources/Models/Turrets",
        "Assets/Resources/Models/Trees",
    ]:
        os.makedirs(os.path.join(REPO, path), exist_ok=True)


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, color, metallic=0.0):
    result = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    result.metallic = metallic
    result.roughness = 0.75
    return result


def root(name):
    result = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(result)
    return result


def cube(name, location, scale, mat, parent, bevel=0.08, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    result = bpy.context.object
    result.name = name
    result.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = result.modifiers.new("ComicBevel", "BEVEL")
    modifier.width = bevel
    modifier.segments = 1
    result.data.materials.append(mat)
    result.parent = parent
    return result


def cylinder(name, location, radius, depth, mat, parent, rotation=(0, 0, 0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    result = bpy.context.object
    result.name = name
    result.data.materials.append(mat)
    result.parent = parent
    return result


def sphere(name, location, scale, mat, parent):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1, location=location)
    result = bpy.context.object
    result.name = name
    result.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    result.data.materials.append(mat)
    result.parent = parent
    return result


def save_and_export(blend_relative, fbx_relative):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(REPO, blend_relative))
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(REPO, fbx_relative),
        use_selection=False,
        object_types={"MESH", "EMPTY"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
    )


GREEN = material("CartoonOlive", (0.22, 0.32, 0.12), 0.25)
DARK = material("TrackDark", (0.055, 0.065, 0.05), 0.35)
METAL = material("ComicMetal", (0.28, 0.30, 0.26), 0.5)
BLACK = material("EyeBlack", (0.015, 0.01, 0.01))
WHITE = material("EyeWhite", (0.9, 0.86, 0.68))
RED = material("EnemyRed", (0.48, 0.12, 0.08), 0.25)
BLUE = material("ScoutBlue", (0.12, 0.28, 0.42), 0.25)
PURPLE = material("CommanderPurple", (0.32, 0.12, 0.38), 0.3)
STONE = material("CastleStone", (0.48, 0.45, 0.38))
CRACK = material("CrackedStone", (0.25, 0.22, 0.18))
WOOD = material("TreeTrunk", (0.25, 0.12, 0.04))
LEAVES = material("TreeLeaves", (0.1, 0.35, 0.12))
ORANGE = material("DamageOrange", (0.8, 0.22, 0.03), 0.1)


def make_tank(label, body_material, size, barrel_length, boss=False):
    clear()
    model_root = root(label)
    cube("Hull", (0, 0, 1.15 * size), (1.7 * size, 2.4 * size, 0.55 * size), body_material, model_root, 0.18)
    cube("Hull_Damaged", (0, -0.05, 1.1 * size), (1.72 * size, 2.38 * size, 0.5 * size), ORANGE, model_root, 0.12, (0.08, 0.03, -0.04))
    cube("LeftSideGuard", (-1.82 * size, 0, 1.05 * size), (0.18 * size, 2.4 * size, 0.18 * size), DARK, model_root, 0.08)
    cube("RightSideGuard", (1.82 * size, 0, 1.05 * size), (0.18 * size, 2.4 * size, 0.18 * size), DARK, model_root, 0.08)
    for axle, y in enumerate((-1.75, -0.58, 0.58, 1.75)):
        cube(f"LeftWheel_{axle}", (-1.9 * size, y * size, 0.62 * size), (0.48 * size, 0.48 * size, 0.48 * size), DARK, model_root, 0.16)
        cube(f"RightWheel_{axle}", (1.9 * size, y * size, 0.62 * size), (0.48 * size, 0.48 * size, 0.48 * size), DARK, model_root, 0.16)
    cube("LeftWheel_Damaged", (-1.95 * size, 0.65 * size, 0.55 * size), (0.46 * size, 0.46 * size, 0.46 * size), ORANGE, model_root, 0.1, (0.12, 0.04, 0.08))
    cube("RightWheel_Damaged", (1.95 * size, -0.65 * size, 0.55 * size), (0.46 * size, 0.46 * size, 0.46 * size), ORANGE, model_root, 0.1, (-0.1, -0.04, -0.08))
    cube("Turret", (0, 0.15 * size, 2.05 * size), (1.25 * size, 1.25 * size, 0.48 * size), body_material, model_root, 0.2)
    cube("Turret_Damaged", (0, 0.1 * size, 2.0 * size), (1.2 * size, 1.2 * size, 0.42 * size), ORANGE, model_root, 0.12, (0.06, 0.1, 0))
    cylinder("Barrel", (0, -(1.2 + barrel_length * 0.5) * size, 2.15 * size), 0.22 * size, barrel_length * size, METAL, model_root, (math.radians(90), 0, 0))
    cylinder("Barrel_Damaged", (0.18 * size, -(1.2 + barrel_length * 0.45) * size, 2.0 * size), 0.2 * size, barrel_length * 0.85 * size, ORANGE, model_root, (math.radians(80), 0, math.radians(8)), 10)
    sphere("CommanderHelmet", (0, 0.35 * size, 2.75 * size), (0.9 * size, 0.9 * size, 0.52 * size), body_material, model_root)
    cylinder("Hatch", (0, 0.3 * size, 3.12 * size), 0.65 * size, 0.16 * size, METAL, model_root)
    cylinder("Antenna", (0.75 * size, 0.45 * size, 3.45 * size), 0.035 * size, 1.35 * size, METAL, model_root, vertices=8)
    for x in (-0.48, 0.48):
        sphere("LeftEye" if x < 0 else "RightEye", (x * size, -1.15 * size, 2.25 * size), (0.32 * size, 0.16 * size, 0.24 * size), WHITE, model_root)
        sphere("LeftPupil" if x < 0 else "RightPupil", (x * 0.88 * size, -1.3 * size, 2.22 * size), (0.11 * size, 0.07 * size, 0.12 * size), BLACK, model_root)
    cube("LeftBrow", (-0.48 * size, -1.34 * size, 2.48 * size), (0.38 * size, 0.08 * size, 0.07 * size), BLACK, model_root, 0.03, (0, 0, -0.25))
    cube("RightBrow", (0.48 * size, -1.34 * size, 2.48 * size), (0.38 * size, 0.08 * size, 0.07 * size), BLACK, model_root, 0.03, (0, 0, 0.25))
    if boss:
        cube("CommanderBadge", (0, -1.32 * size, 2.75 * size), (0.45 * size, 0.1 * size, 0.45 * size), ORANGE, model_root, 0.08, (0, 0, math.radians(45)))


def generate_tanks():
    configs = [
        ("SillyPlayerTank", GREEN, 1.0, 3.5, False),
        ("SillyEnemyScout", BLUE, 0.8, 2.7, False),
        ("SillyEnemyStandard", RED, 1.0, 3.2, False),
        ("SillyEnemyCommander", PURPLE, 1.3, 4.0, True),
    ]
    for name, mat, size, barrel, boss in configs:
        make_tank(name, mat, size, barrel, boss)
        save_and_export(f"BlenderSource/Tanks/{name}.blend", f"Assets/Resources/Models/Tanks/{name}.fbx")


def generate_castle():
    clear()
    model_root = root("SillyCastleKit")
    for index, x in enumerate((-6, -3, 0, 3, 6)):
        cube(f"WallBlock_{index}", (x, 0, 1.5), (1.45, 1.5, 1.5), STONE, model_root, 0.14)
        cube(f"Battlement_{index}", (x, 0, 3.5), (0.85, 1.3, 0.65), STONE, model_root, 0.12)
    for index, x in enumerate((-8, 8)):
        cylinder(f"Tower_Intact_{index}", (x, 0, 2.5), 2.1, 5.0, STONE, model_root)
        cylinder(f"Tower_Cracked_{index}", (x, 0, 2.5), 2.12, 5.02, CRACK, model_root, (0.04, 0.02, 0.03), 10)
    for index in range(8):
        cube(f"CollapsedChunk_{index}", (-5 + index * 1.5, 3.5 + index % 2, 0.4), (0.6, 0.7, 0.4), CRACK, model_root, 0.1, (index * 0.11, index * 0.07, index * 0.19))
    save_and_export("BlenderSource/Castle/SillyCastleKit.blend", "Assets/Resources/Models/Castle/SillyCastleKit.fbx")


def generate_turrets():
    clear()
    model_root = root("SillyCastleTurrets")
    for index, x in enumerate((-3, 3)):
        cylinder(f"TurretBase_{index}", (x, 0, 0.6), 1.4, 1.2, STONE, model_root)
        cube(f"Turret_{index}", (x, 0, 1.7), (1.2, 1.1, 0.65), RED, model_root, 0.18)
        cylinder(f"Barrel_{index}", (x, -2.1, 1.8), 0.22, 3.3, METAL, model_root, (math.radians(90), 0, 0))
        sphere(f"TurretEye_{index}", (x, -1.12, 2.0), (0.35, 0.16, 0.25), WHITE, model_root)
        cube(f"TurretDamaged_{index}", (x, 3, 1.0), (1.1, 1.0, 0.55), ORANGE, model_root, 0.12, (0.1, 0.05, 0.18))
    save_and_export("BlenderSource/Turrets/SillyCastleTurrets.blend", "Assets/Resources/Models/Turrets/SillyCastleTurrets.fbx")


def generate_trees():
    clear()
    model_root = root("SillyTreeKit")
    cylinder("StandingTrunk", (0, 0, 2.0), 0.45, 4.0, WOOD, model_root, vertices=8)
    sphere("StandingCrown", (0, 0, 5.0), (2.0, 2.0, 2.4), LEAVES, model_root)
    cylinder("DamagedTrunk", (5, 0, 1.8), 0.42, 3.6, WOOD, model_root, (0.18, 0.05, 0.12), 8)
    sphere("DamagedCrown", (5.5, 0, 4.4), (1.7, 1.7, 1.8), LEAVES, model_root)
    cylinder("FallenTrunk", (-5, 0, 0.5), 0.45, 4.0, WOOD, model_root, (0, math.radians(90), 0), 8)
    sphere("FallenCrown", (-7, 0, 0.8), (1.6, 1.6, 1.4), LEAVES, model_root)
    cylinder("Stump", (0, 5, 0.45), 0.55, 0.9, WOOD, model_root, vertices=8)
    save_and_export("BlenderSource/Trees/SillyTreeKit.blend", "Assets/Resources/Models/Trees/SillyTreeKit.fbx")


ensure_dirs()
generate_tanks()
generate_castle()
generate_turrets()
generate_trees()
print("GENERATED_SILLY_TANK_MODEL_KIT")
