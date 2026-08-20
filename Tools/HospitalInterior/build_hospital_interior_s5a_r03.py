import hashlib
import json
import math
import shutil
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(r"C:\CutMyBodyPlease")
SOURCE_DIR = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior"
SOURCE = SOURCE_DIR / "HospitalInterior_StairA_S5A_R03.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI0_R03" / "S5A_StairA"
EXPORT = EXPORT_DIR / "HospitalInterior_StairA_S5A_R03.fbx"
UNITY_SOURCE_DIR = (
    ROOT
    / "Unity"
    / "AnatomyXR"
    / "Assets"
    / "HospitalInterior"
    / "PreProduction"
    / "I1"
    / "R03"
    / "S5A"
    / "SourceFBX"
)
UNITY_EXPORT = UNITY_SOURCE_DIR / EXPORT.name
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI1_R03" / "S5A_StairA"
CONTRACT_PATH = REVIEW_DIR / "StageI1_R03_S5A_StairA_SpatialContract.json"
BUILD_RECORD_PATH = REVIEW_DIR / "StageI1_R03_S5A_StairA_BlenderBuild.json"

FLOORS = {
    "F00": 0.0,
    "F01": 5.8,
    "F02": 9.7,
    "F03": 13.6,
    "F04": 17.5,
    "F05": 21.4,
    "F06": 25.3,
}

MIN_X, MAX_X = -29.0, -22.0
MIN_Y, MAX_Y = -5.0, 5.0
BASE_Z, TOP_Z = -0.20, 28.83
WALL = 0.12
SHELL_BOUNDARY_CLEARANCE = 0.02
INNER_MIN_X, INNER_MAX_X = MIN_X + 0.17, MAX_X - 0.17
INNER_WIDTH = INNER_MAX_X - INNER_MIN_X

EAST_LANE_X = -24.18
WEST_LANE_X = -26.82
LANE_WIDTH = 1.80
FLOOR_LANDING_MIN_Y = -4.96
FLOOR_LANDING_MAX_Y = -2.56
FLOOR_LANDING_CENTER_Y = (FLOOR_LANDING_MIN_Y + FLOOR_LANDING_MAX_Y) * 0.5
MID_LANDING_MIN_Y = 2.56
MID_LANDING_MAX_Y = 4.96
MID_LANDING_CENTER_Y = (MID_LANDING_MIN_Y + MID_LANDING_MAX_Y) * 0.5
FLIGHT_RUN = MID_LANDING_MIN_Y - FLOOR_LANDING_MAX_Y

DOOR_CENTER_Y = -3.76
DOOR_CLEAR_WIDTH = 1.20
DOOR_HEIGHT = 2.25
DOOR_OPEN_MIN_Y = DOOR_CENTER_Y - DOOR_CLEAR_WIDTH * 0.5
DOOR_OPEN_MAX_Y = DOOR_CENTER_Y + DOOR_CLEAR_WIDTH * 0.5
DOOR_SLIDE = 1.28

HANDRAIL_HEIGHT = 0.95
GUARD_HEIGHT = 1.10
MIN_HEADROOM = 2.20

MATERIALS = {
    "MAT_S5A_WarmOffWhite": ((0.76, 0.73, 0.66, 1.0), 0.0, 0.62, None),
    "MAT_S5A_StoneTread": ((0.34, 0.36, 0.36, 1.0), 0.0, 0.42, None),
    "MAT_S5A_Charcoal": ((0.025, 0.032, 0.038, 1.0), 0.52, 0.24, None),
    "MAT_S5A_Bronze": ((0.24, 0.095, 0.035, 1.0), 0.72, 0.24, None),
    "MAT_S5A_BlueGreyGlass": ((0.06, 0.16, 0.21, 0.38), 0.04, 0.12, None),
    "MAT_S5A_SoftLight": ((0.88, 0.80, 0.62, 1.0), 0.10, 0.25, (1.0, 0.72, 0.45, 3.0)),
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def ensure_dirs() -> None:
    for path in (SOURCE_DIR, EXPORT_DIR, UNITY_SOURCE_DIR, REVIEW_DIR):
        path.mkdir(parents=True, exist_ok=True)


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0
    # Blender 5.1 exposes the current Eevee implementation under BLENDER_EEVEE.
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.view_settings.look = "AgX - Medium High Contrast"
    world = bpy.data.worlds.new("S5A_R03_World")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.025, 0.032, 0.042, 1.0)
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.16, 0.18, 0.22, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.85
    scene.world = world
    scene["hospital_interior_revision"] = "R03"
    scene["hospital_interior_stage"] = "S5A"
    scene["stair_id"] = "STAIR_A"
    scene["unity_mapping"] = "Blender X/Y/Z -> Unity X/Z/Y"


def collection(name: str) -> bpy.types.Collection:
    found = bpy.data.collections.get(name)
    if found is not None:
        return found
    found = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(found)
    return found


def build_materials() -> None:
    for name, (base, metallic, roughness, emission) in MATERIALS.items():
        material = bpy.data.materials.new(name)
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = base
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        if base[3] < 1.0:
            bsdf.inputs["Alpha"].default_value = base[3]
            material.surface_render_method = "DITHERED"
        if emission is not None:
            bsdf.inputs["Emission Color"].default_value = emission[:3] + (1.0,)
            bsdf.inputs["Emission Strength"].default_value = emission[3]


def add_box(name: str, location, dimensions, parent: bpy.types.Collection, material: str,
            category: str, **metadata) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for owner in tuple(obj.users_collection):
        owner.objects.unlink(obj)
    parent.objects.link(obj)
    obj.data.materials.append(bpy.data.materials[material])
    obj["s5a_category"] = category
    obj["s5a_collision"] = "solid"
    for key, value in metadata.items():
        obj[key] = value
    return obj


def add_beam(name: str, start, end, width: float, depth: float,
             parent: bpy.types.Collection, material: str, category: str, **metadata) -> bpy.types.Object:
    start_v = Vector(start)
    end_v = Vector(end)
    delta = end_v - start_v
    length = delta.length
    midpoint = (start_v + end_v) * 0.5
    obj = add_box(name, midpoint, (width, length, depth), parent, material, category, **metadata)
    angle = math.atan2(delta.z, delta.y)
    obj.rotation_euler[0] = angle
    return obj


def add_text(name: str, text: str, location, rotation, size: float,
             parent: bpy.types.Collection) -> bpy.types.Object:
    curve = bpy.data.curves.new(name + "_Curve", "FONT")
    curve.body = text
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = size
    curve.extrude = 0.008
    obj = bpy.data.objects.new(name, curve)
    parent.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    obj.data.materials.append(bpy.data.materials["MAT_S5A_SoftLight"])
    obj["s5a_category"] = "FloorLabel"
    return obj


def interval_spec(index: int, lower: float, upper: float) -> dict:
    per_flight = 17 if index == 0 else 13
    interval = upper - lower
    return {
        "interval": interval,
        "per_flight": per_flight,
        "total_risers": per_flight * 2,
        "rise": interval / (per_flight * 2),
        "going": FLIGHT_RUN / per_flight,
    }


def build_shell(target: bpy.types.Collection) -> None:
    height = TOP_Z - BASE_Z
    centre_z = (TOP_Z + BASE_Z) * 0.5
    shell_min_x = MIN_X + SHELL_BOUNDARY_CLEARANCE
    shell_max_x = MAX_X - SHELL_BOUNDARY_CLEARANCE
    shell_min_y = MIN_Y + SHELL_BOUNDARY_CLEARANCE
    shell_max_y = MAX_Y - SHELL_BOUNDARY_CLEARANCE
    add_box("S5A_R03_StairA_Wall_West",
            (shell_min_x + WALL * 0.5, (shell_min_y + shell_max_y) * 0.5, centre_z),
            (WALL, shell_max_y - shell_min_y, height), target,
            "MAT_S5A_WarmOffWhite", "StairShell")
    add_box("S5A_R03_StairA_Wall_South",
            ((shell_min_x + shell_max_x) * 0.5, shell_min_y + WALL * 0.5, centre_z),
            (shell_max_x - shell_min_x, WALL, height), target,
            "MAT_S5A_WarmOffWhite", "StairShell")
    add_box("S5A_R03_StairA_Wall_North",
            ((shell_min_x + shell_max_x) * 0.5, shell_max_y - WALL * 0.5, centre_z),
            (shell_max_x - shell_min_x, WALL, height), target,
            "MAT_S5A_WarmOffWhite", "StairShell")

    floor_items = list(FLOORS.items())
    for floor_index, (floor_id, floor_z) in enumerate(floor_items):
        upper_z = floor_items[floor_index + 1][1] if floor_index + 1 < len(floor_items) else TOP_Z
        interval_height = upper_z - floor_z
        east_x = shell_max_x - WALL * 0.5
        south_length = DOOR_OPEN_MIN_Y - shell_min_y
        north_length = shell_max_y - DOOR_OPEN_MAX_Y
        if south_length > 0.01:
            add_box(f"S5A_R03_StairA_EastWall_{floor_id}_South",
                    (east_x, shell_min_y + south_length * 0.5, floor_z + interval_height * 0.5),
                    (WALL, south_length, interval_height), target,
                    "MAT_S5A_WarmOffWhite", "StairShell", s5a_floor_id=floor_id)
        add_box(f"S5A_R03_StairA_EastWall_{floor_id}_North",
                (east_x, DOOR_OPEN_MAX_Y + north_length * 0.5, floor_z + interval_height * 0.5),
                (WALL, north_length, interval_height), target,
                "MAT_S5A_WarmOffWhite", "StairShell", s5a_floor_id=floor_id)
        header_height = max(0.10, interval_height - DOOR_HEIGHT)
        add_box(f"S5A_R03_StairA_EastWall_{floor_id}_Header",
                (east_x, DOOR_CENTER_Y, floor_z + DOOR_HEIGHT + header_height * 0.5),
                (WALL, DOOR_CLEAR_WIDTH, header_height), target,
                "MAT_S5A_WarmOffWhite", "StairShell", s5a_floor_id=floor_id)

    add_box("S5A_R03_StairA_BaseClosure", ((MIN_X + MAX_X) * 0.5, 0.0, -0.10),
            (MAX_X - MIN_X - 0.12, MAX_Y - MIN_Y - 0.12, 0.20), target,
            "MAT_S5A_Charcoal", "StairBase")
    add_box("S5A_R03_StairA_TopClosure", ((MIN_X + MAX_X) * 0.5, 0.0, TOP_Z - 0.05),
            (MAX_X - MIN_X - 0.12, MAX_Y - MIN_Y - 0.12, 0.10), target,
            "MAT_S5A_WarmOffWhite", "StairTopClosure")


def build_floor_landings(target: bpy.types.Collection) -> None:
    for floor_id, floor_z in FLOORS.items():
        add_box(f"S5A_R03_StairA_FloorLanding_{floor_id}",
                ((INNER_MIN_X + INNER_MAX_X) * 0.5, FLOOR_LANDING_CENTER_Y, floor_z - 0.08),
                (INNER_WIDTH, FLOOR_LANDING_MAX_Y - FLOOR_LANDING_MIN_Y, 0.16), target,
                "MAT_S5A_StoneTread", "FloorLanding", s5a_floor_id=floor_id,
                s5a_surface_z=floor_z)
        add_box(f"S5A_R03_StairA_Threshold_{floor_id}",
                (MAX_X - 0.17, DOOR_CENTER_Y, floor_z - 0.035),
                (0.34, DOOR_CLEAR_WIDTH + 0.08, 0.07), target,
                "MAT_S5A_Charcoal", "DoorThreshold", s5a_floor_id=floor_id)
        add_text(f"S5A_R03_StairA_Label_{floor_id}", floor_id,
                 (MAX_X - 0.14, DOOR_CENTER_Y + 0.92, floor_z + 1.65),
                 (math.pi / 2.0, 0.0, math.pi / 2.0), 0.25, target)
        add_box(f"S5A_R03_StairA_LandingLight_{floor_id}",
                ((MIN_X + MAX_X) * 0.5, FLOOR_LANDING_CENTER_Y, floor_z + 2.70),
                (1.20, 0.18, 0.035), target, "MAT_S5A_SoftLight", "LandingLight",
                s5a_floor_id=floor_id, s5a_collision="none")


def build_doors(target: bpy.types.Collection) -> None:
    east_x = MAX_X - WALL - 0.035
    for floor_id, floor_z in FLOORS.items():
        frame_x = MAX_X - WALL - 0.02
        for suffix, frame_y in (("South", DOOR_OPEN_MIN_Y - 0.07), ("North", DOOR_OPEN_MAX_Y + 0.07)):
            add_box(f"S5A_R03_StairA_DoorFrame_{floor_id}_{suffix}",
                    (frame_x, frame_y, floor_z + DOOR_HEIGHT * 0.5),
                    (0.16, 0.14, DOOR_HEIGHT + 0.18), target,
                    "MAT_S5A_Bronze", "DoorFrame", s5a_floor_id=floor_id)
        add_box(f"S5A_R03_StairA_DoorFrame_{floor_id}_Header",
                (frame_x, DOOR_CENTER_Y, floor_z + DOOR_HEIGHT + 0.07),
                (0.16, DOOR_CLEAR_WIDTH + 0.28, 0.14), target,
                "MAT_S5A_Bronze", "DoorFrame", s5a_floor_id=floor_id)
        leaf = add_box(f"S5A_R03_StairA_DoorLeaf_{floor_id}",
                       (east_x, DOOR_CENTER_Y, floor_z + DOOR_HEIGHT * 0.5),
                       (0.07, DOOR_CLEAR_WIDTH + 0.04, DOOR_HEIGHT), target,
                       "MAT_S5A_Charcoal", "DoorLeaf", s5a_floor_id=floor_id,
                       s5a_closed_y=DOOR_CENTER_Y, s5a_open_y=DOOR_CENTER_Y + DOOR_SLIDE,
                       s5a_slide_distance=DOOR_SLIDE)
        leaf["s5a_collision"] = "dynamic"
        glass = add_box(f"S5A_R03_StairA_DoorVision_{floor_id}",
                        (east_x - 0.038, DOOR_CENTER_Y, floor_z + 1.47),
                        (0.018, 0.44, 0.66), target, "MAT_S5A_BlueGreyGlass", "DoorVision",
                        s5a_floor_id=floor_id, s5a_parent_leaf=leaf.name)
        glass["s5a_collision"] = "none"


def build_flight(interval_index: int, lower_id: str, upper_id: str,
                 lower_z: float, upper_z: float, target: bpy.types.Collection) -> None:
    spec = interval_spec(interval_index, lower_z, upper_z)
    mid_z = (lower_z + upper_z) * 0.5
    add_box(f"S5A_R03_StairA_MidLanding_{lower_id}_{upper_id}",
            ((INNER_MIN_X + INNER_MAX_X) * 0.5, MID_LANDING_CENTER_Y, mid_z - 0.08),
            (INNER_WIDTH, MID_LANDING_MAX_Y - MID_LANDING_MIN_Y, 0.16), target,
            "MAT_S5A_StoneTread", "IntermediateLanding", s5a_floor_from=lower_id,
            s5a_floor_to=upper_id, s5a_surface_z=mid_z)

    flights = (
        ("Northbound", EAST_LANE_X, 1.0, lower_z, FLOOR_LANDING_MAX_Y),
        ("Southbound", WEST_LANE_X, -1.0, mid_z, MID_LANDING_MIN_Y),
    )
    for flight_name, lane_x, direction, base_z, start_edge in flights:
        records = []
        for step_index in range(spec["per_flight"]):
            centre_y = start_edge + direction * spec["going"] * (step_index + 0.5)
            top_z = base_z + spec["rise"] * (step_index + 1)
            records.append((centre_y, top_z))
            add_box(
                f"S5A_R03_StairA_Step_{lower_id}_{upper_id}_{flight_name}_{step_index:02d}",
                (lane_x, centre_y, top_z - spec["rise"] * 0.5),
                (LANE_WIDTH, spec["going"] + 0.006, spec["rise"]), target,
                "MAT_S5A_StoneTread", "StairStep", s5a_floor_from=lower_id,
                s5a_floor_to=upper_id, s5a_flight=flight_name,
                s5a_step_index=step_index, s5a_step_top_z=top_z,
                s5a_rise=spec["rise"], s5a_going=spec["going"])
            add_box(
                f"S5A_R03_StairA_Nosing_{lower_id}_{upper_id}_{flight_name}_{step_index:02d}",
                (lane_x, centre_y - direction * spec["going"] * 0.48, top_z + 0.012),
                (LANE_WIDTH, 0.035, 0.035), target, "MAT_S5A_Charcoal", "StairNosing",
                s5a_floor_from=lower_id, s5a_floor_to=upper_id,
                s5a_flight=flight_name, s5a_step_index=step_index,
                s5a_collision="none")

        first_y, first_top = records[0]
        last_y, last_top = records[-1]
        add_beam(f"S5A_R03_StairA_Stringer_{lower_id}_{upper_id}_{flight_name}",
                 (lane_x, first_y, first_top - 0.18),
                 (lane_x, last_y, last_top - 0.18), LANE_WIDTH - 0.12, 0.16,
                 target, "MAT_S5A_Charcoal", "StairStringer",
                 s5a_floor_from=lower_id, s5a_floor_to=upper_id, s5a_flight=flight_name)
        for side, rail_x in (("West", lane_x - LANE_WIDTH * 0.5),
                             ("East", lane_x + LANE_WIDTH * 0.5)):
            add_beam(f"S5A_R03_StairA_Handrail_{lower_id}_{upper_id}_{flight_name}_{side}",
                     (rail_x, first_y, first_top + HANDRAIL_HEIGHT),
                     (rail_x, last_y, last_top + HANDRAIL_HEIGHT), 0.055, 0.055,
                     target, "MAT_S5A_Bronze", "StairHandrail",
                     s5a_floor_from=lower_id, s5a_floor_to=upper_id,
                     s5a_flight=flight_name, s5a_rail_side=side)
            post_indices = sorted({0, spec["per_flight"] // 4, spec["per_flight"] // 2,
                                   (spec["per_flight"] * 3) // 4, spec["per_flight"] - 1})
            for post_index in post_indices:
                post_y, post_top = records[post_index]
                add_box(
                    f"S5A_R03_StairA_RailPost_{lower_id}_{upper_id}_{flight_name}_{side}_{post_index:02d}",
                    (rail_x, post_y, post_top + HANDRAIL_HEIGHT * 0.5),
                    (0.045, 0.045, HANDRAIL_HEIGHT), target,
                    "MAT_S5A_Bronze", "StairRailPost", s5a_floor_from=lower_id,
                    s5a_floor_to=upper_id, s5a_flight=flight_name,
                    s5a_rail_side=side)

    well_x = (WEST_LANE_X + LANE_WIDTH * 0.5 + EAST_LANE_X - LANE_WIDTH * 0.5) * 0.5
    well_w = (EAST_LANE_X - LANE_WIDTH * 0.5) - (WEST_LANE_X + LANE_WIDTH * 0.5)

    north_y = MID_LANDING_MAX_Y - 0.06
    add_box(f"S5A_R03_StairA_MidGuard_{lower_id}_{upper_id}_{north_y:+.2f}",
            ((INNER_MIN_X + INNER_MAX_X) * 0.5, north_y, mid_z + GUARD_HEIGHT),
            (INNER_WIDTH, 0.055, 0.055), target, "MAT_S5A_Bronze", "LandingGuard",
            s5a_floor_from=lower_id, s5a_floor_to=upper_id)
    for post_x in (INNER_MIN_X + 0.04, well_x, INNER_MAX_X - 0.04):
        add_box(f"S5A_R03_StairA_MidGuardPost_{lower_id}_{upper_id}_{north_y:+.2f}_{post_x:+.2f}",
                (post_x, north_y, mid_z + GUARD_HEIGHT * 0.5),
                (0.045, 0.045, GUARD_HEIGHT), target,
                "MAT_S5A_Bronze", "LandingGuardPost",
                s5a_floor_from=lower_id, s5a_floor_to=upper_id)

    south_y = MID_LANDING_MIN_Y + 0.06
    add_box(f"S5A_R03_StairA_MidGuard_{lower_id}_{upper_id}_{south_y:+.2f}",
            (well_x, south_y, mid_z + GUARD_HEIGHT),
            (well_w, 0.055, 0.055), target, "MAT_S5A_Bronze", "LandingGuard",
            s5a_floor_from=lower_id, s5a_floor_to=upper_id)
    for post_x in (WEST_LANE_X + LANE_WIDTH * 0.5, well_x, EAST_LANE_X - LANE_WIDTH * 0.5):
        add_box(f"S5A_R03_StairA_MidGuardPost_{lower_id}_{upper_id}_{south_y:+.2f}_{post_x:+.2f}",
                (post_x, south_y, mid_z + GUARD_HEIGHT * 0.5),
                (0.045, 0.045, GUARD_HEIGHT), target,
                "MAT_S5A_Bronze", "LandingGuardPost",
                s5a_floor_from=lower_id, s5a_floor_to=upper_id)


def build_geometry() -> bpy.types.Collection:
    target = collection("HOSPITAL_INTERIOR_STAIR_A_S5A_R03")
    build_shell(target)
    build_floor_landings(target)
    build_doors(target)
    floor_items = list(FLOORS.items())
    for index in range(len(floor_items) - 1):
        lower_id, lower_z = floor_items[index]
        upper_id, upper_z = floor_items[index + 1]
        build_flight(index, lower_id, upper_id, lower_z, upper_z, target)
    return target


def add_review_lighting() -> None:
    key_data = bpy.data.lights.new("S5A_R03_Key", "AREA")
    key_data.energy = 2200
    key_data.shape = "RECTANGLE"
    key_data.size = 14
    key = bpy.data.objects.new("S5A_R03_Key", key_data)
    bpy.context.scene.collection.objects.link(key)
    key.location = (-18, -12, 22)
    key.rotation_euler = (math.radians(28), 0, math.radians(35))
    fill_data = bpy.data.lights.new("S5A_R03_Fill", "AREA")
    fill_data.energy = 1500
    fill_data.size = 12
    fill = bpy.data.objects.new("S5A_R03_Fill", fill_data)
    bpy.context.scene.collection.objects.link(fill)
    fill.location = (-34, 8, 15)
    fill.rotation_euler = (math.radians(65), 0, math.radians(-130))


def point_camera(camera: bpy.types.Object, location, target) -> None:
    camera.location = location
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def render_reviews() -> list[str]:
    add_review_lighting()
    camera_data = bpy.data.cameras.new("S5A_R03_ReviewCamera")
    camera_data.lens = 42
    camera_data.clip_end = 200
    camera = bpy.data.objects.new("S5A_R03_ReviewCamera", camera_data)
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    scene = bpy.context.scene
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.view_settings.exposure = 1.0
    views = (
        # name, camera, target, lens, review visibility mode
        ("01_S5A_StairA_F00_Entrance.png", (-18.5, -3.76, 1.65), (-24.5, -1.8, 1.85), 43, "entrance"),
        ("02_S5A_StairA_F00_F01_TallFlight.png", (-23.15, -4.00, 1.55), (-24.18, 0.55, 3.05), 38, "inside"),
        ("03_S5A_StairA_TypicalUpperInterval.png", (-23.15, -4.00, 10.85), (-24.18, 0.45, 11.75), 38, "inside"),
        ("04_S5A_StairA_F06_TopClosure.png", (-16.5, -8.0, 26.7), (-25.5, 0.0, 26.8), 52, "top"),
        ("05_S5A_StairA_FullVerticalSection.png", (-13.5, -12.0, 14.2), (-25.5, 0.0, 14.2), 58, "section"),
        ("06_S5A_StairA_MeasuredPlan.png", (-25.5, 0.0, 42.0), (-25.5, 0.0, 0.0), 52, "plan"),
    )
    outputs = []
    target_objects = tuple(bpy.data.collections["HOSPITAL_INTERIOR_STAIR_A_S5A_R03"].all_objects)
    for filename, location, target, lens, mode in views:
        for obj in target_objects:
            obj.hide_render = False
        if mode == "entrance":
            for obj in target_objects:
                if obj.name in {"S5A_R03_StairA_DoorLeaf_F00", "S5A_R03_StairA_DoorVision_F00"}:
                    obj.hide_render = True
        elif mode in {"top", "section", "plan"}:
            for obj in target_objects:
                if obj.name.startswith("S5A_R03_StairA_EastWall_") or obj.name == "S5A_R03_StairA_TopClosure":
                    obj.hide_render = True
        camera_data.lens = lens
        point_camera(camera, location, target)
        path = REVIEW_DIR / filename
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        outputs.append(filename)
    return outputs


def spatial_contract() -> dict:
    floor_items = list(FLOORS.items())
    intervals = []
    for index in range(len(floor_items) - 1):
        lower_id, lower = floor_items[index]
        upper_id, upper = floor_items[index + 1]
        spec = interval_spec(index, lower, upper)
        intervals.append({
            "from": lower_id,
            "to": upper_id,
            "heightM": round(spec["interval"], 6),
            "totalRisers": spec["total_risers"],
            "risersPerFlight": spec["per_flight"],
            "riseM": round(spec["rise"], 9),
            "goingM": round(spec["going"], 9),
            "comfort2RPlusG": round(2.0 * spec["rise"] + spec["going"], 9),
        })
    return {
        "schema": "HospitalInterior.R03.S5A.StairContract.v1",
        "status": "IMPLEMENTATION_CONTRACT_APPROVED_BY_USER_REQUEST",
        "coordinateSystem": "Unity X/Z horizontal and Y vertical; Blender X/Y horizontal and Z vertical",
        "authority": {
            "s4Approval": "Reviews/HospitalInterior/StageI1_R03/S4_F00_Plan/StageI1_R03_S4_F00_UserApproval.md",
            "s4Contract": "Reviews/HospitalInterior/StageI1_R03/S4_F00_Plan/StageI1_R03_S4_F00_Plan_V03_Contract.json",
        },
        "bounds": {"minX": MIN_X, "maxX": MAX_X, "minZ": MIN_Y, "maxZ": MAX_Y,
                   "baseY": BASE_Z, "topY": TOP_Z},
        "floorDatumsM": FLOORS,
        "lanes": {"eastCentreX": EAST_LANE_X, "westCentreX": WEST_LANE_X,
                  "widthM": LANE_WIDTH, "minimumUsableWidthM": 1.45},
        "landings": {
            "floor": {"minZ": FLOOR_LANDING_MIN_Y, "maxZ": FLOOR_LANDING_MAX_Y,
                      "depthM": FLOOR_LANDING_MAX_Y - FLOOR_LANDING_MIN_Y},
            "intermediate": {"minZ": MID_LANDING_MIN_Y, "maxZ": MID_LANDING_MAX_Y,
                             "depthM": MID_LANDING_MAX_Y - MID_LANDING_MIN_Y},
            "flightRunM": FLIGHT_RUN,
        },
        "doors": {"count": 7, "side": "east", "centreZ": DOOR_CENTER_Y,
                  "clearWidthM": DOOR_CLEAR_WIDTH, "clearHeightM": DOOR_HEIGHT,
                  "mode": "automatic_single_panel_slide_positive_Z", "slideM": DOOR_SLIDE},
        "safety": {"minimumHeadroomM": MIN_HEADROOM, "handrailHeightM": HANDRAIL_HEIGHT,
                   "guardHeightM": GUARD_HEIGHT, "teleportInsideCore": False,
                   "collision": "visible_tread_aligned_no_hidden_ramp"},
        "intervals": intervals,
        "counts": {"flights": 12, "steps": sum(item["totalRisers"] for item in intervals),
                   "floorLandings": 7, "intermediateLandings": 6,
                   "automaticDoors": 7, "topClosures": 1},
        "finish": "functional_restrained",
        "deferred": ["S5B lobby anchors", "F01-F06 programmes", "standalone Quest/Android"],
    }


def export_fbx(target: bpy.types.Collection) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in target.all_objects:
        if obj.type in {"MESH", "FONT"}:
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT),
        use_selection=True,
        object_types={"MESH", "OTHER"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    shutil.copy2(EXPORT, UNITY_EXPORT)


def main() -> None:
    ensure_dirs()
    reset_scene()
    build_materials()
    target = build_geometry()
    contract = spatial_contract()
    CONTRACT_PATH.write_text(json.dumps(contract, indent=2), encoding="utf-8")
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    export_fbx(target)
    review_files = render_reviews()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
    record = {
        "schema": "HospitalInterior.R03.S5A.BlenderBuild.v1",
        "status": "BUILT_PENDING_INDEPENDENT_VALIDATION",
        "blenderVersion": bpy.app.version_string,
        "source": {"path": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
                   "sha256": sha256(SOURCE), "bytes": SOURCE.stat().st_size},
        "export": {"path": str(EXPORT.relative_to(ROOT)).replace("\\", "/"),
                   "sha256": sha256(EXPORT), "bytes": EXPORT.stat().st_size},
        "unityImport": {"path": str(UNITY_EXPORT.relative_to(ROOT)).replace("\\", "/"),
                        "sha256": sha256(UNITY_EXPORT), "bytes": UNITY_EXPORT.stat().st_size},
        "reviewFiles": review_files,
        "counts": contract["counts"],
    }
    BUILD_RECORD_PATH.write_text(json.dumps(record, indent=2), encoding="utf-8")
    print("HOSPITAL_INTERIOR_R03_S5A_BLENDER_BUILD=PASS")


if __name__ == "__main__":
    main()
