from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path
from typing import Iterable, Sequence

import bpy


ROOT = Path(r"C:\CutMyBodyPlease")
R40 = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalExterior" / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"
EXPECTED_R40 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126"
SOURCE_DIR = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior"
MODULE_SOURCE = SOURCE_DIR / "HospitalInterior_ModuleKit_R02.blend"
ASSEMBLY_SOURCE = SOURCE_DIR / "HospitalInterior_I0_R02_AllFloorAssembly.blend"
CORE_SOURCE = SOURCE_DIR / "HospitalInterior_Core_R02.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI0_R02"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI0_R02"

FLOORS = {
    "F00": {"z": 0.0},
    "F01": {"z": 5.8},
    "F02": {"z": 9.7},
    "F03": {"z": 13.6},
    "F04": {"z": 17.5},
    "F05": {"z": 21.4},
    "F06": {"z": 25.3},
}

UPPER_CLEAR_HEIGHT = 3.48
OUTER = (-34.7, 16.7, -15.5, 15.5)
ELEVATOR = (0.0, 6.0, 3.0, 5.7)
STAIR_A = (-27.5, -22.5, -3.6, 3.6)
STAIR_B = (4.7, 9.5, -4.0, 2.6)

MATERIAL_SPECS = {
    "MAT_R02_WarmOffWhite": ((0.72, 0.74, 0.71, 1.0), 0.0, 0.58, None),
    "MAT_R02_LightGrey": ((0.43, 0.48, 0.50, 1.0), 0.0, 0.52, None),
    "MAT_R02_Charcoal": ((0.026, 0.034, 0.041, 1.0), 0.56, 0.24, None),
    "MAT_R02_Bronze": ((0.20, 0.085, 0.032, 1.0), 0.72, 0.26, None),
    "MAT_R02_BlueGreyGlass": ((0.055, 0.14, 0.19, 0.34), 0.05, 0.12, None),
    "MAT_R02_Terrazzo": ((0.39, 0.38, 0.35, 1.0), 0.0, 0.34, None),
    "MAT_R02_CeilingSoftWhite": ((0.78, 0.76, 0.70, 1.0), 0.0, 0.68, None),
    "MAT_R02_ElevatorSteel": ((0.31, 0.33, 0.33, 1.0), 0.78, 0.22, None),
    "MAT_R02_DarkRubber": ((0.018, 0.020, 0.021, 1.0), 0.0, 0.76, None),
    "MAT_R02_IndicatorWarm": ((0.92, 0.58, 0.20, 1.0), 0.18, 0.28, (1.0, 0.42, 0.08, 2.8)),
    "MAT_R02_IndicatorLocked": ((0.28, 0.30, 0.31, 1.0), 0.15, 0.62, None),
    "MAT_R02_SignText": ((0.93, 0.88, 0.70, 1.0), 0.1, 0.36, (0.95, 0.58, 0.20, 1.4)),
}

MODULE_CACHE: dict[tuple, bpy.types.Mesh] = {}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def ensure_dirs() -> None:
    for path in (SOURCE_DIR, EXPORT_DIR, REVIEW_DIR):
        path.mkdir(parents=True, exist_ok=True)


def reset_scene() -> None:
    global MODULE_CACHE
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MODULE_CACHE = {}
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGB"
    scene.view_settings.look = "AgX - Medium High Contrast"
    world = bpy.data.worlds.new("R02_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.045, 0.055, 0.065, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.32
    scene.world = world
    scene["hospital_interior_stage"] = "I0/I1"
    scene["hospital_interior_revision"] = "R02"
    scene["scope"] = "Repeated clean open lobby shell with elevator and stair core"
    scene["r40_sha256"] = EXPECTED_R40


def collection(name: str, parent: bpy.types.Collection | None = None) -> bpy.types.Collection:
    result = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(result)
    return result


def build_materials() -> None:
    for name, (base, metallic, roughness, emission) in MATERIAL_SPECS.items():
        material = bpy.data.materials.new(name)
        material.diffuse_color = base
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = base
            bsdf.inputs["Metallic"].default_value = metallic
            bsdf.inputs["Roughness"].default_value = roughness
            if base[3] < 1.0:
                bsdf.inputs["Alpha"].default_value = base[3]
                bsdf.inputs["Transmission Weight"].default_value = 0.18
            if emission:
                bsdf.inputs["Emission Color"].default_value = emission[:3] + (1.0,)
                bsdf.inputs["Emission Strength"].default_value = emission[3]
        if base[3] < 1.0:
            if hasattr(material, "surface_render_method"):
                material.surface_render_method = "DITHERED"
            material.use_transparency_overlap = False
        material["hospital_interior_revision"] = "R02"
        material["source_palette"] = "Approved exterior R40 visual language"


def module_mesh(dimensions: Sequence[float], material_name: str, bevel: float = 0.018) -> bpy.types.Mesh:
    dimensions = tuple(round(float(value), 4) for value in dimensions)
    key = (dimensions, material_name, round(bevel, 4))
    if key in MODULE_CACHE:
        return MODULE_CACHE[key]
    bpy.ops.mesh.primitive_cube_add(location=(0.0, 0.0, 0.0))
    obj = bpy.context.object
    obj.name = "R02_MODULE_TEMP"
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0 and min(dimensions) > bevel * 2.2:
        modifier = obj.modifiers.new("R02_EdgeSoftening", "BEVEL")
        modifier.width = min(bevel, min(dimensions) * 0.2)
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(bpy.data.materials[material_name])
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    try:
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    except TypeError:
        bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh = obj.data
    mesh.name = f"R02_MODULE_{material_name}_{len(MODULE_CACHE):03d}"
    mesh["hospital_interior_revision"] = "R02"
    mesh["dimensions_m"] = json.dumps(dimensions)
    mesh["uv0_required"] = True
    bpy.data.objects.remove(obj, do_unlink=True)
    MODULE_CACHE[key] = mesh
    return mesh


def add_module(name: str, location: Sequence[float], dimensions: Sequence[float], target: bpy.types.Collection,
               material_name: str, bevel: float = 0.018, rotation_z: float = 0.0, **props) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, module_mesh(dimensions, material_name, bevel))
    target.objects.link(obj)
    obj.location = tuple(float(value) for value in location)
    obj.rotation_euler[2] = rotation_z
    obj.scale = (1.0, 1.0, 1.0)
    obj["hospital_interior_revision"] = "R02"
    obj["asset_quality"] = "production_visual_preproduction_scope"
    for key, value in props.items():
        obj[key] = value
    return obj


def add_sloped_module(name: str, start: Sequence[float], end: Sequence[float], width: float, depth: float,
                      target: bpy.types.Collection, material_name: str, bevel: float = 0.014,
                      **props) -> bpy.types.Object:
    """Create a reusable box whose long local-Y axis follows a Y/Z stair slope."""
    start_x, start_y, start_z = (float(value) for value in start)
    end_x, end_y, end_z = (float(value) for value in end)
    if abs(start_x - end_x) > 0.0001:
        raise ValueError("R02 sloped stair modules must remain in one X lane")
    dy = end_y - start_y
    dz = end_z - start_z
    length = math.sqrt(dy * dy + dz * dz)
    obj = add_module(name, (start_x, (start_y + end_y) * 0.5, (start_z + end_z) * 0.5),
                     (width, length, depth), target, material_name, bevel, **props)
    obj.rotation_euler[0] = math.atan2(dz, dy)
    return obj


def add_empty(name: str, location: Sequence[float], target: bpy.types.Collection, **props) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    target.objects.link(obj)
    obj.location = location
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = 0.35
    obj["hospital_interior_revision"] = "R02"
    for key, value in props.items():
        obj[key] = value
    return obj


def add_text(name: str, body: str, location: Sequence[float], target: bpy.types.Collection,
             size: float = 0.32, rotation=(math.pi / 2.0, 0.0, 0.0), floor_id: str = "",
             convert_to_mesh: bool = False) -> bpy.types.Object:
    curve = bpy.data.curves.new(name + "_CURVE", "FONT")
    curve.body = body
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = size
    curve.extrude = 0.003 if convert_to_mesh else 0.012
    curve.bevel_depth = 0.001 if convert_to_mesh else 0.004
    curve.resolution_u = 2
    curve.bevel_resolution = 0 if convert_to_mesh else 2
    obj = bpy.data.objects.new(name, curve)
    target.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    obj.data.materials.append(bpy.data.materials["MAT_R02_SignText"])
    if not convert_to_mesh:
        obj["int_review_only"] = True
        obj["int_floor_id"] = floor_id
        return obj
    for selected in bpy.context.selected_objects:
        selected.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    try:
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    except TypeError:
        bpy.ops.uv.smart_project(island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    # Converted signage is production geometry and remains in the versioned FBX.
    obj["int_review_only"] = False
    obj["int_category"] = "ArchitecturalSignage"
    obj["int_floor_id"] = floor_id
    return obj


def add_wall_run(name: str, start: float, end: float, fixed: float, z: float, height: float,
                 target: bpy.types.Collection, along_x: bool, material="MAT_R02_WarmOffWhite", thickness=0.12,
                 floor_id="", category="PartitionWall") -> None:
    length = end - start
    if length <= 0.05:
        return
    location = ((start + end) * 0.5, fixed, z + height * 0.5) if along_x else (fixed, (start + end) * 0.5, z + height * 0.5)
    dimensions = (length, thickness, height) if along_x else (thickness, length, height)
    add_module(name, location, dimensions, target, material, 0.014,
               int_floor_id=floor_id, int_category=category, int_collision="solid")
    trim_location = ((start + end) * 0.5, fixed - (0.066 if along_x else 0.0), z + 0.075) if along_x else (fixed - 0.066, (start + end) * 0.5, z + 0.075)
    trim_dimensions = (length, 0.035, 0.15) if along_x else (0.035, length, 0.15)
    add_module(name + "_BaseTrim", trim_location, trim_dimensions, target, "MAT_R02_Charcoal", 0.008,
               int_floor_id=floor_id, int_category="BaseTrim")


def intersects(rect: Sequence[float], hole: Sequence[float]) -> bool:
    x0, x1, y0, y1 = rect
    a0, a1, b0, b1 = hole
    return x0 < a1 and x1 > a0 and y0 < b1 and y1 > b0


def build_floor_slab(floor_id: str, z: float, target: bpy.types.Collection) -> None:
    x0, x1, y0, y1 = OUTER
    xcuts = sorted({x0, x1, ELEVATOR[0], ELEVATOR[1], STAIR_A[0], STAIR_A[1], STAIR_B[0], STAIR_B[1]})
    ycuts = sorted({y0, y1, ELEVATOR[2], ELEVATOR[3], STAIR_A[2], STAIR_A[3], STAIR_B[2], STAIR_B[3]})
    index = 0
    for xa, xb in zip(xcuts, xcuts[1:]):
        for ya, yb in zip(ycuts, ycuts[1:]):
            rect = (xa, xb, ya, yb)
            if any(intersects(rect, hole) for hole in (ELEVATOR, STAIR_A, STAIR_B)):
                continue
            index += 1
            add_module(f"INT_{floor_id}_FloorTile_{index:02d}", ((xa + xb) * 0.5, (ya + yb) * 0.5, z + 0.06),
                       (xb - xa, yb - ya, 0.12), target, "MAT_R02_Terrazzo", 0.006,
                       int_floor_id=floor_id, int_category="Floor", int_collision="walkable")


def build_outer_shell(floor_id: str, z: float, target: bpy.types.Collection) -> None:
    clear = UPPER_CLEAR_HEIGHT
    x0, x1, y0, y1 = OUTER
    add_wall_run(f"INT_{floor_id}_Envelope_Front", x0, x1, y0, z, clear, target, True,
                 floor_id=floor_id, category="EnvelopeWall")
    add_wall_run(f"INT_{floor_id}_Envelope_Rear", x0, x1, y1, z, clear, target, True, floor_id=floor_id, category="EnvelopeWall")
    add_wall_run(f"INT_{floor_id}_Envelope_West", y0, y1, x0, z, clear, target, False, floor_id=floor_id, category="EnvelopeWall")
    add_wall_run(f"INT_{floor_id}_Envelope_East", y0, y1, x1, z, clear, target, False, floor_id=floor_id, category="EnvelopeWall")


def build_open_lobby(floor_id: str, z: float, target: bpy.types.Collection) -> None:
    clear = UPPER_CLEAR_HEIGHT
    x0, x1, y0, y1 = OUTER
    ceiling_z = z + clear
    xcuts = sorted({x0, x1, ELEVATOR[0], ELEVATOR[1], STAIR_A[0], STAIR_A[1], STAIR_B[0], STAIR_B[1]})
    ycuts = sorted({y0, y1, ELEVATOR[2], ELEVATOR[3], STAIR_A[2], STAIR_A[3], STAIR_B[2], STAIR_B[3]})
    index = 0
    for xa, xb in zip(xcuts, xcuts[1:]):
        for ya, yb in zip(ycuts, ycuts[1:]):
            if any(intersects((xa, xb, ya, yb), hole) for hole in (ELEVATOR, STAIR_A, STAIR_B)):
                continue
            index += 1
            add_module(f"INT_{floor_id}_CeilingPanel_{index:02d}",
                       ((xa + xb) * 0.5, (ya + yb) * 0.5, ceiling_z),
                       (xb - xa, yb - ya, 0.08), target, "MAT_R02_CeilingSoftWhite", 0.006,
                       int_floor_id=floor_id, int_category="Ceiling")
    for index, x in enumerate((-25.0, -16.0, -7.0, 2.0, 11.0)):
        add_module(f"INT_{floor_id}_LobbyLight_{index:02d}", (x, 0.0, ceiling_z - 0.055),
                   (0.10, 9.6, 0.035), target, "MAT_R02_IndicatorWarm", 0.006,
                   int_floor_id=floor_id, int_category="ReviewLightFixture")
    number_x, number_y = -9.0, y1 - 0.071
    add_module(f"INT_{floor_id}_FloorNumberPlaque", (number_x, number_y, z + 2.0),
               (2.8, 0.035, 1.25), target, "MAT_R02_Charcoal", 0.018,
               int_floor_id=floor_id, int_category="FloorNumberPlaque")
    add_text(f"INT_{floor_id}_FloorNumber", floor_id, (number_x, number_y - 0.025, z + 2.0),
             target, 0.42, (math.pi / 2.0, 0.0, 0.0), floor_id, convert_to_mesh=True)


def build_spawns(floor_id: str, z: float, target: bpy.types.Collection) -> None:
    # Stair travel resolves on the corridor side of the finished landing door,
    # rather than at the old greybox centre of the shaft.
    spawns = [("ELEVATOR", (1.65, 1.95, z), math.pi),
              ("STAIR_A", (STAIR_A[1] + 1.30, stair_level_y("A", STAIR_A), z), math.pi / 2.0)]
    if floor_id == "F00":
        spawns.append(("ENTRY", (-9.0, -6.9, z), math.pi / 2.0))
    for spawn, location, rotation in spawns:
        obj = add_empty(f"INT_{floor_id}_SPAWN_{spawn}", location, target,
                        int_floor_id=floor_id, int_category="Spawn", int_spawn_id=spawn)
        obj.rotation_euler[2] = rotation


def build_floor(floor_id: str, target: bpy.types.Collection) -> None:
    z = FLOORS[floor_id]["z"]
    build_floor_slab(floor_id, z, target)
    build_outer_shell(floor_id, z, target)
    build_open_lobby(floor_id, z, target)
    build_spawns(floor_id, z, target)


def add_elevator_portal(floor_id: str, z: float, centre: float, shaft: str, functional: bool,
                        target: bpy.types.Collection) -> None:
    door_w, door_h = 1.52, 2.25
    leaf_w = door_w * 0.5
    y = 2.64
    frame_mat = "MAT_R02_Bronze" if functional else "MAT_R02_Charcoal"
    state_mat = "MAT_R02_IndicatorWarm" if functional else "MAT_R02_IndicatorLocked"
    for side, x in (("L", centre - leaf_w * 0.5), ("R", centre + leaf_w * 0.5)):
        add_module(f"INT_CORE_{floor_id}_{shaft}_Door_{side}", (x, y, z + door_h * 0.5),
                   (leaf_w - 0.012, 0.10, door_h), target, "MAT_R02_ElevatorSteel", 0.012,
                   int_floor_id=floor_id, int_category="LandingDoor", int_shaft=shaft,
                   int_functional=functional, int_collision="dynamic")
    for suffix, x in (("Frame_L", centre - door_w * 0.5 - 0.07), ("Frame_R", centre + door_w * 0.5 + 0.07)):
        add_module(f"INT_CORE_{floor_id}_{shaft}_{suffix}", (x, y - 0.035, z + door_h * 0.5),
                   (0.14, 0.18, door_h + 0.24), target, frame_mat, 0.012,
                   int_floor_id=floor_id, int_category="ElevatorFrame", int_shaft=shaft)
    add_module(f"INT_CORE_{floor_id}_{shaft}_Frame_H", (centre, y - 0.035, z + door_h + 0.12),
               (door_w + 0.28, 0.18, 0.14), target, frame_mat, 0.012,
               int_floor_id=floor_id, int_category="ElevatorFrame", int_shaft=shaft)
    add_module(f"INT_CORE_{floor_id}_{shaft}_Indicator", (centre, y - 0.13, z + 2.62),
               (0.46, 0.055, 0.16), target, state_mat, 0.018,
               int_floor_id=floor_id, int_category="ElevatorIndicator", int_shaft=shaft)
    add_module(f"INT_CORE_{floor_id}_{shaft}_Threshold", (centre, y - 0.03, z + 0.035),
               (door_w + 0.08, 0.34, 0.07), target, "MAT_R02_ElevatorSteel", 0.008,
               int_floor_id=floor_id, int_category="ElevatorThreshold", int_shaft=shaft)


def stair_level_y(stair: str, bounds: Sequence[float]) -> float:
    _, _, ya, yb = bounds
    return ya + 0.55 if stair == "A" else yb - 0.55


def build_stair_landing(floor_id: str, z: float, stair: str, bounds: Sequence[float], playable: bool,
                        target: bpy.types.Collection, next_floor_id: str | None = None,
                        next_z: float | None = None) -> None:
    xa, xb, ya, yb = bounds
    level_height = (next_z - z) if next_z is not None else UPPER_CLEAR_HEIGHT
    mat = "MAT_R02_WarmOffWhite" if playable else "MAT_R02_LightGrey"
    rail_material = "MAT_R02_Bronze" if playable else "MAT_R02_Charcoal"
    door_side = 1.0 if stair == "A" else -1.0
    direction_y = 1.0 if stair == "A" else -1.0
    level_y = stair_level_y(stair, bounds)
    mid_y = (yb - 0.55) if stair == "A" else (ya + 0.55)
    stair_centre_x = (xa + xb) * 0.5
    door_x = xb if stair == "A" else xa
    outer_x = xa if stair == "A" else xb
    lane_width = 1.56
    lane_offset = 0.88
    first_lane_x = stair_centre_x + door_side * lane_offset
    return_lane_x = stair_centre_x - door_side * lane_offset
    landing_depth = 1.02

    # The enclosure now spans the real datum-to-datum interval and closes the
    # corridor side around a properly positioned landing door.
    add_wall_run(f"INT_CORE_{floor_id}_Stair{stair}_Front", xa, xb, ya, z, level_height, target, True, mat,
                 floor_id=floor_id, category="StairShell")
    add_wall_run(f"INT_CORE_{floor_id}_Stair{stair}_Rear", xa, xb, yb, z, level_height, target, True, mat,
                 floor_id=floor_id, category="StairShell")
    add_wall_run(f"INT_CORE_{floor_id}_Stair{stair}_Outer", ya, yb, outer_x, z, level_height, target, False, mat,
                 floor_id=floor_id, category="StairShell")
    opening_half = 0.72
    if level_y - opening_half > ya:
        add_wall_run(f"INT_CORE_{floor_id}_Stair{stair}_Inner_Front", ya, level_y - opening_half,
                     door_x, z, level_height, target, False, mat, floor_id=floor_id, category="StairShell")
    if level_y + opening_half < yb:
        add_wall_run(f"INT_CORE_{floor_id}_Stair{stair}_Inner_Rear", level_y + opening_half, yb,
                     door_x, z, level_height, target, False, mat, floor_id=floor_id, category="StairShell")
    header_height = max(0.12, level_height - 2.52)
    add_module(f"INT_CORE_{floor_id}_Stair{stair}_Inner_Header",
               (door_x, level_y, z + 2.52 + header_height * 0.5), (0.12, 1.44, header_height),
               target, mat, 0.012, int_floor_id=floor_id, int_category="StairShell")

    door_name = f"INT_CORE_{floor_id}_Stair{stair}_Door"
    add_module(door_name, (door_x, level_y, z + 1.125), (0.10, 1.20, 2.25), target,
               "MAT_R02_Charcoal" if playable else "MAT_R02_IndicatorLocked", 0.018,
               int_floor_id=floor_id, int_category="StairDoor", int_stair=stair,
               int_playable=playable, int_collision="dynamic")
    frame_x = door_x - door_side * 0.07
    add_module(door_name + "_Frame_H", (frame_x, level_y, z + 2.34),
               (0.18, 1.48, 0.14), target, rail_material, 0.012,
               int_floor_id=floor_id, int_category="StairDoorFrame")
    for suffix, frame_y in (("L", level_y - 0.66), ("R", level_y + 0.66)):
        add_module(door_name + f"_Frame_{suffix}", (frame_x, frame_y, z + 1.13),
                   (0.18, 0.14, 2.40), target, rail_material, 0.012,
                   int_floor_id=floor_id, int_category="StairDoorFrame")
    add_module(door_name + "_Vision", (door_x - door_side * 0.061, level_y, z + 1.52),
               (0.025, 0.42, 0.68), target, "MAT_R02_BlueGreyGlass", 0.008,
               int_floor_id=floor_id, int_category="StairDoorVision")
    add_module(f"INT_CORE_{floor_id}_Stair{stair}_StatePlate",
               (door_x - door_side * 0.115, level_y, z + 2.62), (0.045, 0.56, 0.18), target,
               "MAT_R02_IndicatorWarm" if playable else "MAT_R02_IndicatorLocked", 0.014,
               int_floor_id=floor_id, int_category="StairStateIndicator", int_stair=stair,
               int_playable=playable)
    add_module(f"INT_CORE_{floor_id}_Stair{stair}_Landing_Level",
               (stair_centre_x, level_y, z + 0.08), (xb - xa - 0.34, landing_depth, 0.16),
               target, "MAT_R02_Terrazzo", 0.012, int_floor_id=floor_id,
               int_category="StairLanding", int_stair=stair, int_landing="level")

    if next_z is None or next_floor_id is None:
        add_module(f"INT_CORE_{floor_id}_Stair{stair}_ShaftTopClosure",
                   (stair_centre_x, (ya + yb) * 0.5, z + level_height - 0.05),
                   (xb - xa - 0.24, yb - ya - 0.24, 0.10), target,
                   "MAT_R02_CeilingSoftWhite", 0.012, int_floor_id=floor_id,
                   int_category="StairCeiling", int_stair=stair)
        return

    interval = next_z - z
    total_risers = max(4, int(round(interval / 0.17)))
    if total_risers % 2 != 0:
        total_risers += 1
    step_count = total_risers // 2
    rise = interval / total_risers
    horizontal_run = abs(mid_y - level_y) - landing_depth
    tread = min(0.30, horizontal_run / step_count)
    first_start_y = level_y + direction_y * (landing_depth * 0.5 + tread * 0.5)
    second_start_y = mid_y - direction_y * (landing_depth * 0.5 + tread * 0.5)
    middle_z = z + interval * 0.5
    add_module(f"INT_CORE_{floor_id}_Stair{stair}_Landing_Mid",
               (stair_centre_x, mid_y, middle_z - 0.08),
               (xb - xa - 0.34, landing_depth, 0.16), target, "MAT_R02_Terrazzo", 0.012,
               int_floor_id=floor_id, int_category="StairLanding", int_stair=stair,
               int_landing="intermediate", int_interval_to=next_floor_id)

    flight_specs = (
        ("F01", first_lane_x, direction_y, z, first_start_y),
        ("F02", return_lane_x, -direction_y, middle_z, second_start_y),
    )
    for flight_name, lane_x, travel_sign, flight_z, start_y in flight_specs:
        step_records = []
        for index in range(step_count):
            step_y = start_y + travel_sign * index * tread
            step_top = flight_z + (index + 1) * rise
            step_records.append((step_y, step_top))
            add_module(f"INT_CORE_{floor_id}_Stair{stair}_Step_{flight_name}_{index:02d}",
                       (lane_x, step_y, step_top - 0.10), (lane_width, tread + 0.018, 0.20),
                       target, "MAT_R02_Terrazzo", 0.0, int_floor_id=floor_id,
                       int_category="StairStep", int_stair=stair, int_flight=flight_name,
                       int_interval_to=next_floor_id, int_step_top_z=step_top, int_playable=playable)
            add_module(f"INT_CORE_{floor_id}_Stair{stair}_Nosing_{flight_name}_{index:02d}",
                       (lane_x, step_y - travel_sign * tread * 0.5, step_top + 0.012),
                       (lane_width, 0.035, 0.035), target, "MAT_R02_Charcoal", 0.0,
                       int_floor_id=floor_id, int_category="StairNosing", int_stair=stair,
                       int_flight=flight_name, int_interval_to=next_floor_id)

        first_y, first_top = step_records[0]
        last_y, last_top = step_records[-1]
        add_sloped_module(f"INT_CORE_{floor_id}_Stair{stair}_{flight_name}_Soffit",
                          (lane_x, first_y, first_top - 0.22), (lane_x, last_y, last_top - 0.22),
                          lane_width - 0.12, 0.16, target, "MAT_R02_Charcoal", 0.0,
                          int_floor_id=floor_id, int_category="StairSoffit", int_stair=stair,
                          int_flight=flight_name, int_interval_to=next_floor_id)
        post_indices = sorted({0, step_count // 4, step_count // 2, (step_count * 3) // 4, step_count - 1})
        for rail_side, rail_x in (("L", lane_x - lane_width * 0.5), ("R", lane_x + lane_width * 0.5)):
            add_sloped_module(f"INT_CORE_{floor_id}_Stair{stair}_{flight_name}_Handrail_{rail_side}",
                              (rail_x, first_y, first_top + 0.96), (rail_x, last_y, last_top + 0.96),
                              0.055, 0.055, target, rail_material, 0.0,
                              int_floor_id=floor_id, int_category="StairHandrail", int_stair=stair,
                              int_flight=flight_name, int_interval_to=next_floor_id)
            add_sloped_module(f"INT_CORE_{floor_id}_Stair{stair}_{flight_name}_Stringer_{rail_side}",
                              (rail_x, first_y, first_top - 0.18), (rail_x, last_y, last_top - 0.18),
                              0.12, 0.24, target, "MAT_R02_Charcoal", 0.0,
                              int_floor_id=floor_id, int_category="StairStringer", int_stair=stair,
                              int_flight=flight_name, int_interval_to=next_floor_id)
            for post_index in post_indices:
                post_y, post_top = step_records[post_index]
                add_module(f"INT_CORE_{floor_id}_Stair{stair}_{flight_name}_RailPost_{rail_side}_{post_index:02d}",
                           (rail_x, post_y, post_top + 0.48), (0.045, 0.045, 0.96), target,
                           rail_material, 0.0, int_floor_id=floor_id, int_category="StairRail",
                           int_stair=stair, int_flight=flight_name, int_interval_to=next_floor_id)

    # Guard the intermediate landing edges between both opposed flights.
    for rail_y in (mid_y - direction_y * landing_depth * 0.42, mid_y + direction_y * landing_depth * 0.42):
        add_module(f"INT_CORE_{floor_id}_Stair{stair}_MidGuard_{'A' if rail_y < mid_y else 'B'}",
                   (stair_centre_x, rail_y, middle_z + 0.98), (xb - xa - 0.38, 0.055, 0.055),
                   target, rail_material, 0.0, int_floor_id=floor_id, int_category="StairGuard",
                   int_stair=stair, int_interval_to=next_floor_id)


def build_core(target: bpy.types.Collection) -> None:
    top = FLOORS["F06"]["z"] + UPPER_CLEAR_HEIGHT
    for shaft, xa, xb, functional in (("E01", 0.3, 3.0, True), ("E02", 3.0, 5.7, False)):
        for suffix, location, dimensions in (
            ("West", (xa, 4.35, top * 0.5), (0.12, 2.7, top)),
            ("East", (xb, 4.35, top * 0.5), (0.12, 2.7, top)),
            ("Rear", ((xa + xb) * 0.5, 5.7, top * 0.5), (xb - xa, 0.12, top)),
        ):
            add_module(f"INT_CORE_{shaft}_Shaft_{suffix}", location, dimensions, target, "MAT_R02_Charcoal", 0.012,
                       int_category="ElevatorShaft", int_shaft=shaft, int_functional=functional, int_collision="solid")
    cabin = add_empty("INT_CORE_E01_CabinRoot", (0.0, 0.0, 0.0), target,
                      int_category="ElevatorCabinRoot", int_functional=True)
    cabin_parts = (
        ("Floor", (1.65, 4.35, 0.07), (1.92, 2.20, 0.14), "MAT_R02_Terrazzo"),
        ("Rear", (1.65, 5.39, 1.22), (1.92, 0.10, 2.44), "MAT_R02_ElevatorSteel"),
        ("West", (0.73, 4.35, 1.22), (0.08, 2.20, 2.44), "MAT_R02_ElevatorSteel"),
        ("East", (2.57, 4.35, 1.22), (0.08, 2.20, 2.44), "MAT_R02_ElevatorSteel"),
        ("Ceiling", (1.65, 4.35, 2.44), (1.92, 2.20, 0.08), "MAT_R02_CeilingSoftWhite"),
        ("RearAccent", (1.65, 5.32, 1.25), (1.42, 0.035, 1.72), "MAT_R02_Bronze"),
        ("Skirting", (1.65, 5.30, 0.17), (1.62, 0.05, 0.12), "MAT_R02_Charcoal"),
        ("Handrail", (1.65, 5.20, 1.02), (1.30, 0.055, 0.055), "MAT_R02_Bronze"),
        ("Light", (1.65, 4.35, 2.385), (1.10, 1.30, 0.035), "MAT_R02_IndicatorWarm"),
    )
    for suffix, location, dimensions, material in cabin_parts:
        obj = add_module(f"INT_CORE_E01_Cabin_{suffix}", location, dimensions, target, material, 0.014,
                         int_category="ElevatorCabin", int_functional=True)
        obj.parent = cabin
    for side, x in (("L", 1.28), ("R", 2.02)):
        obj = add_module(f"INT_CORE_E01_CabinDoor_{side}", (x, 3.29, 1.125), (0.72, 0.08, 2.25), target,
                         "MAT_R02_ElevatorSteel", 0.012, int_category="CabinDoor", int_functional=True,
                         int_collision="dynamic")
        obj.parent = cabin
    panel = add_module("INT_CORE_E01_CabinControlPanel", (2.515, 4.52, 1.18), (0.035, 0.54, 1.18), target,
                       "MAT_R02_Charcoal", 0.012, int_category="CabinControlPanel", int_functional=True)
    panel.parent = cabin
    floor_ids = list(FLOORS.keys())
    for floor_index, (floor_id, spec) in enumerate(FLOORS.items()):
        z = spec["z"]
        for suffix, centre, width in (("Left", 0.38, 0.76), ("Centre", 3.0, 0.66), ("Right", 5.92, 0.56)):
            add_module(f"INT_CORE_{floor_id}_ElevatorBankWall_{suffix}", (centre, 2.78, z + 1.55),
                       (width, 0.16, 3.10), target, "MAT_R02_WarmOffWhite", 0.014,
                       int_floor_id=floor_id, int_category="ElevatorBankWall")
        add_module(f"INT_CORE_{floor_id}_ElevatorBankWall_Header", (3.0, 2.78, z + 2.93),
                   (6.4, 0.16, 0.34), target, "MAT_R02_WarmOffWhite", 0.014,
                   int_floor_id=floor_id, int_category="ElevatorBankWall")
        add_elevator_portal(floor_id, z, 1.65, "E01", True, target)
        add_elevator_portal(floor_id, z, 4.35, "E02", False, target)
        add_module(f"INT_CORE_{floor_id}_CallPanel", (0.18, 2.64, z + 1.10), (0.16, 0.08, 0.34), target,
                   "MAT_R02_Charcoal", 0.025, int_floor_id=floor_id, int_category="CallPanel",
                   int_interaction_height=1.1, int_collision="interaction")
        add_module(f"INT_CORE_{floor_id}_CallPanel_Light", (0.18, 2.59, z + 1.10), (0.055, 0.025, 0.055), target,
                   "MAT_R02_IndicatorWarm", 0.014, int_floor_id=floor_id, int_category="CallPanelLight")
        next_floor_id = floor_ids[floor_index + 1] if floor_index + 1 < len(floor_ids) else None
        next_z = FLOORS[next_floor_id]["z"] if next_floor_id else None
        build_stair_landing(floor_id, z, "A", STAIR_A, True, target, next_floor_id, next_z)
        build_stair_landing(floor_id, z, "B", STAIR_B, False, target, next_floor_id, next_z)
    for name, location in (("INT_DATUM_ORIGIN", (0, 0, 0)), ("INT_DATUM_X_1M", (1, 0, 0)),
                           ("INT_DATUM_Y_1M", (0, 1, 0)), ("INT_DATUM_Z_1M", (0, 0, 1))):
        add_empty(name, location, target, int_category="DatumProbe")


def build_module_kit() -> None:
    reset_scene()
    build_materials()
    kit = collection("HOSPITAL_INTERIOR_MODULE_KIT_R02")
    specs = [
        ("MOD_Wall_2400", (0, 0, 1.5), (2.4, 0.12, 3.0), "MAT_R02_WarmOffWhite"),
        ("MOD_Wall_1200", (3.0, 0, 1.5), (1.2, 0.12, 3.0), "MAT_R02_WarmOffWhite"),
        ("MOD_BaseTrim_2400", (0, -0.15, 0.075), (2.4, 0.035, 0.15), "MAT_R02_Charcoal"),
        ("MOD_CeilingPanel", (6.0, 0, 2.95), (2.4, 2.4, 0.08), "MAT_R02_CeilingSoftWhite"),
        ("MOD_FloorPanel", (6.0, 3.0, 0.06), (2.4, 2.4, 0.12), "MAT_R02_Terrazzo"),
        ("MOD_ElevatorLeaf", (9.0, 0, 1.125), (0.74, 0.10, 2.25), "MAT_R02_ElevatorSteel"),
        ("MOD_StairTread", (12.0, 0, 0.10), (1.8, 0.30, 0.20), "MAT_R02_LightGrey"),
        ("MOD_Handrail", (15.0, 0, 0.04), (1.8, 0.05, 0.08), "MAT_R02_Charcoal"),
    ]
    for name, location, dimensions, material in specs:
        add_module(name, location, dimensions, kit, material, 0.018, int_category="ReusableModule")
    bpy.context.scene["module_count"] = len(specs)
    bpy.ops.wm.save_as_mainfile(filepath=str(MODULE_SOURCE))


def link_r40_reference() -> bpy.types.Collection:
    reference = collection("REFERENCE_R40_LOCKED_REVIEW_ONLY")
    with bpy.data.libraries.load(str(R40), link=True) as (source, target):
        if "UNITY_EXPORT" not in source.collections:
            raise RuntimeError("R40 UNITY_EXPORT collection missing")
        target.collections = ["UNITY_EXPORT"]
    instance = bpy.data.objects.new("REFERENCE_HOSPITAL_R40_LOCKED", None)
    reference.objects.link(instance)
    instance.instance_type = "COLLECTION"
    instance.instance_collection = bpy.data.collections.get("UNITY_EXPORT")
    instance.display_type = "WIRE"
    instance["linked_review_only"] = True
    return reference


def all_objects(root: bpy.types.Collection) -> list[bpy.types.Object]:
    return list(root.all_objects)


def export_collection(root: bpy.types.Collection, path: Path) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in all_objects(root):
        if obj.type in {"MESH", "EMPTY"} and not obj.get("int_review_only"):
            obj.hide_set(False)
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
        bake_space_transform=False, add_leaf_bones=False, use_mesh_modifiers=True,
        mesh_smooth_type="FACE", use_tspace=True, path_mode="AUTO")
    bpy.ops.object.select_all(action="DESELECT")


def add_review_lighting() -> None:
    rig = collection("R02_REVIEW_RIG")
    sun_data = bpy.data.lights.new("R02_Sun", "SUN")
    sun_data.energy = 2.0
    sun_data.angle = math.radians(12)
    sun = bpy.data.objects.new("R02_Sun", sun_data)
    rig.objects.link(sun)
    sun.rotation_euler = (math.radians(42), 0.0, math.radians(-32))
    for index, (location, energy, size) in enumerate((
        ((-8, -5, 7), 1100, 8.0), ((3, 1, 6), 950, 6.0), ((-24, 0, 6), 900, 5.0))):
        light_data = bpy.data.lights.new(f"R02_Area_{index}", "AREA")
        light_data.energy = energy
        light_data.shape = "DISK"
        light_data.size = size
        light = bpy.data.objects.new(f"R02_Area_{index}", light_data)
        rig.objects.link(light)
        light.location = location
        light.rotation_euler = (0.0, 0.0, 0.0)
    for index, (location, energy, color) in enumerate((
        ((1.65, 4.25, 2.05), 260.0, (1.0, 0.73, 0.48)),
        ((-24.2, 0.0, 2.65), 340.0, (1.0, 0.78, 0.58)),
        ((-4.0, -10.5, 2.8), 360.0, (0.82, 0.90, 1.0)),
    )):
        light_data = bpy.data.lights.new(f"R02_Point_{index}", "POINT")
        light_data.energy = energy
        light_data.color = color
        light_data.shadow_soft_size = 1.2
        light = bpy.data.objects.new(f"R02_Point_{index}", light_data)
        rig.objects.link(light)
        light.location = location
    camera_data = bpy.data.cameras.new("R02_ReviewCamera")
    camera_data.lens = 30
    camera = bpy.data.objects.new("R02_ReviewCamera", camera_data)
    rig.objects.link(camera)
    bpy.context.scene.camera = camera


def point_camera(camera: bpy.types.Object, location: Sequence[float], target: Sequence[float], lens=30.0) -> None:
    camera.location = location
    direction = mathutils_vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = lens


def mathutils_vector(values: Sequence[float]):
    from mathutils import Vector
    return Vector(values)


def set_review_visibility(floor_id: str | None, floors: dict[str, bpy.types.Collection], core: bpy.types.Collection,
                          reference: bpy.types.Collection, show_reference=False, plan=False) -> None:
    for fid, floor_collection in floors.items():
        floor_collection.hide_render = floor_id is not None and fid != floor_id
    core.hide_render = False
    reference.hide_render = not show_reference
    for obj in all_objects(core):
        obj.hide_render = bool(floor_id and obj.get("int_floor_id") not in (None, "", floor_id))
    for floor_collection in floors.values():
        for obj in all_objects(floor_collection):
            obj.hide_render = bool(plan and obj.get("int_category") in {"Ceiling", "ReviewLightFixture"})


def render_view(path: Path, camera: bpy.types.Object, location: Sequence[float], target: Sequence[float], lens=30.0) -> None:
    point_camera(camera, location, target, lens)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def build_reviews(floors: dict[str, bpy.types.Collection], core: bpy.types.Collection,
                  reference: bpy.types.Collection) -> None:
    camera = bpy.context.scene.camera
    for index, floor_id in enumerate(FLOORS, 1):
        z = FLOORS[floor_id]["z"]
        set_review_visibility(floor_id, floors, core, reference, plan=True)
        camera.data.type = "ORTHO"
        camera.data.ortho_scale = 60.0
        render_view(REVIEW_DIR / f"{index:02d}_{floor_id}_AnnotatedPlan.png", camera,
                    (-9.0, 0.0, z + 54.0), (-9.0, 0.0, z), 35)
        camera.data.type = "PERSP"
    set_review_visibility(None, floors, core, reference)
    render_view(REVIEW_DIR / "08_AllFloors_LongitudinalSection.png", camera,
                (42, -62, 22), (-8, 0, 13), 48)
    set_review_visibility("F00", floors, core, reference, show_reference=True)
    render_view(REVIEW_DIR / "09_Core_ExteriorAlignment.png", camera,
                (32, -42, 14), (-2, 0, 5), 46)
    render_view(REVIEW_DIR / "10_F00_OpenLobbyAlignment.png", camera,
                (-9, -24, 4.2), (-9, 0.0, 1.5), 36)
    set_review_visibility("F00", floors, core, reference)
    views = (
        ("11_F00_OpenLobbyWide.png", (-9.0, -11.5, 1.7), (-9.0, 5.0, 1.55), 32),
        ("12_F00_ElevatorBank.png", (-5.4, -3.6, 1.7), (2.8, 2.9, 1.35), 30),
        ("15_F00_StairA.png", (-24.9, -2.58, 1.72), (-24.9, 2.28, 3.62), 30),
        ("17_F00_FloorNumber.png", (-9.0, 9.2, 1.7), (-9.0, 15.4, 2.0), 34),
        ("18_F00_ClearLobbyBoundary.png", (11.5, -10.5, 1.7), (-10.0, 8.0, 1.55), 34),
        ("19_F00_StairB.png", (7.1, 1.58, 1.72), (7.1, -2.75, 3.62), 30),
    )
    for name, location, target, lens in views:
        render_view(REVIEW_DIR / name, camera, location, target, lens)
    cabin_review_names = {
        "INT_CORE_F00_E01_Door_L", "INT_CORE_F00_E01_Door_R",
        "INT_CORE_E01_CabinDoor_L", "INT_CORE_E01_CabinDoor_R",
    }
    hidden_for_cabin = [obj for obj in all_objects(core) if obj.name in cabin_review_names]
    for obj in hidden_for_cabin:
        obj.hide_render = True
    render_view(REVIEW_DIR / "13_E01_Cabin.png", camera,
                (1.65, 1.75, 1.62), (1.65, 5.0, 1.25), 27)
    for obj in hidden_for_cabin:
        obj.hide_render = False
    set_review_visibility("F02", floors, core, reference)
    render_view(REVIEW_DIR / "14_F02_ElevatorArrival.png", camera,
                (-1.8, -1.4, FLOORS["F02"]["z"] + 1.7), (2.8, 2.9, FLOORS["F02"]["z"] + 1.4), 30)
    set_review_visibility("F01", floors, core, reference)
    render_view(REVIEW_DIR / "16_F01_StairLanding.png", camera,
                (-24.9, -2.58, FLOORS["F01"]["z"] + 1.72),
                (-24.9, 2.28, FLOORS["F01"]["z"] + 3.62), 30)
    set_review_visibility("F06", floors, core, reference)
    render_view(REVIEW_DIR / "20_F06_FloorNumber.png", camera,
                (-9.0, 9.2, FLOORS["F06"]["z"] + 1.7),
                (-9.0, 15.4, FLOORS["F06"]["z"] + 2.0), 34)


def save_core_copy(floors: dict[str, bpy.types.Collection], reference: bpy.types.Collection) -> None:
    bpy.ops.wm.save_as_mainfile(filepath=str(ASSEMBLY_SOURCE))
    for floor_collection in list(floors.values()):
        bpy.data.collections.remove(floor_collection)
    bpy.data.collections.remove(reference)
    for name in ("R02_REVIEW_RIG",):
        found = bpy.data.collections.get(name)
        if found:
            bpy.data.collections.remove(found)
    bpy.ops.wm.save_as_mainfile(filepath=str(CORE_SOURCE))
    bpy.ops.wm.open_mainfile(filepath=str(ASSEMBLY_SOURCE))


def write_build_manifest(floors: dict[str, bpy.types.Collection], core: bpy.types.Collection) -> None:
    floor_counts = {}
    for floor_id, target in floors.items():
        meshes = [obj for obj in all_objects(target) if obj.type == "MESH"]
        floor_counts[floor_id] = {
            "mesh_objects": len(meshes),
            "triangles": sum(len(obj.data.loop_triangles) if obj.data.loop_triangles else triangle_count(obj.data) for obj in meshes),
            "shared_mesh_datablocks": len({obj.data.name for obj in meshes}),
        }
    core_meshes = [obj for obj in all_objects(core) if obj.type == "MESH"]
    manifest = {
        "schema": "HospitalInterior.OpenLobby.R02.BuildManifest.v2",
        "status": "BUILT_PENDING_INDEPENDENT_VALIDATION",
        "sources": {path.name: sha256(path) for path in (MODULE_SOURCE, ASSEMBLY_SOURCE, CORE_SOURCE)},
        "protected_r40_sha256": sha256(R40),
        "floor_datums_m": {floor_id: spec["z"] for floor_id, spec in FLOORS.items()},
        "floor_template": "identical clean open lobby shell with elevator and stair openings",
        "per_floor_difference": "wall-mounted floor number only",
        "material_count": len(MATERIAL_SPECS),
        "floor_counts": floor_counts,
        "core": {"mesh_objects": len(core_meshes), "triangles": sum(triangle_count(obj.data) for obj in core_meshes)},
        "exports": {path.name: sha256(path) for path in sorted(EXPORT_DIR.glob("*.fbx"))},
        "content_contract": ["open floor", "perimeter walls", "ceiling", "lighting", "elevator core", "stair core", "floor number"],
    }
    (REVIEW_DIR / "StageI0I1_R02_BuildManifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")


def triangle_count(mesh: bpy.types.Mesh) -> int:
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def build_assembly() -> None:
    reset_scene()
    build_materials()
    floors: dict[str, bpy.types.Collection] = {}
    for floor_id in FLOORS:
        target = collection(f"EXPORT_I0_R02_{floor_id}")
        floors[floor_id] = target
        build_floor(floor_id, target)
    core = collection("EXPORT_I1_R02_CORE")
    build_core(core)
    reference = link_r40_reference()
    add_review_lighting()
    bpy.ops.wm.save_as_mainfile(filepath=str(ASSEMBLY_SOURCE))
    export_collection(core, EXPORT_DIR / "Hospital_INT_Core_I1_R02.fbx")
    for floor_id, target in floors.items():
        export_collection(target, EXPORT_DIR / f"Hospital_INT_{floor_id}_I0_R02.fbx")
    build_reviews(floors, core, reference)
    bpy.ops.wm.save_as_mainfile(filepath=str(ASSEMBLY_SOURCE))
    save_core_copy(floors, reference)
    reopened_floors = {floor_id: bpy.data.collections[f"EXPORT_I0_R02_{floor_id}"] for floor_id in FLOORS}
    reopened_core = bpy.data.collections["EXPORT_I1_R02_CORE"]
    write_build_manifest(reopened_floors, reopened_core)


def main() -> None:
    ensure_dirs()
    if not R40.exists() or sha256(R40) != EXPECTED_R40:
        raise RuntimeError("Protected R40 reference missing or changed")
    build_module_kit()
    build_assembly()
    print("HOSPITAL_INTERIOR_R02_BLENDER_BUILD=PASS")


if __name__ == "__main__":
    main()
