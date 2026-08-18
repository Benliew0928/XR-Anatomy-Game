from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path
from typing import Iterable, Sequence

import bpy
from mathutils import Vector


ROOT = Path(r"C:\CutMyBodyPlease")
R40 = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalExterior" / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"
EXPECTED_R40 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126"
SOURCE = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior" / "HospitalInterior_I0_R01_AllFloorGreybox.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI0_R01"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI0_R01"

FLOORS = {
    "F00": {"alias": "L00", "z": 0.0, "name": "Arrival Lobby and Reception"},
    "F01": {"alias": "L02", "z": 5.8, "name": "Anatomy Discovery Gallery"},
    "F02": {"alias": "L03", "z": 9.7, "name": "Skeletal Lab"},
    "F03": {"alias": "L04", "z": 13.6, "name": "Muscle and Movement Lab"},
    "F04": {"alias": "L05", "z": 17.5, "name": "Nerve and Vessel Lab"},
    "F05": {"alias": "L06", "z": 21.4, "name": "Integrated Lower-Limb Theatre"},
    "F06": {"alias": "L07", "z": 25.3, "name": "Assessment and Research"},
}

UPPER_CLEAR_HEIGHT = 3.48
GROUND_CLEAR_HEIGHT = 5.05
OUTER = (-34.7, 16.7, -15.5, 15.5)
ELEVATOR = (0.0, 6.0, 3.0, 5.7)
STAIR_A = (-27.5, -22.5, -3.6, 3.6)
STAIR_B = (4.7, 9.5, -4.0, 2.6)
CORE_ZONE = (-27.5, 9.5, -5.7, 5.7)

MATERIALS = {
    "MAT_I0_Floor": (0.23, 0.28, 0.32, 1.0),
    "MAT_I0_Wall": (0.72, 0.75, 0.76, 1.0),
    "MAT_I0_Core": (0.08, 0.32, 0.48, 1.0),
    "MAT_I0_Elevator": (0.80, 0.49, 0.12, 1.0),
    "MAT_I0_StairPlayable": (0.15, 0.55, 0.34, 1.0),
    "MAT_I0_StairLocked": (0.45, 0.20, 0.22, 1.0),
    "MAT_I0_Route": (0.12, 0.65, 0.78, 1.0),
    "MAT_I0_ZonePrimary": (0.33, 0.58, 0.86, 1.0),
    "MAT_I0_ZoneSecondary": (0.46, 0.70, 0.48, 1.0),
    "MAT_I0_ZoneSupport": (0.58, 0.46, 0.70, 1.0),
    "MAT_I0_Spawn": (0.95, 0.82, 0.12, 1.0),
    "MAT_I0_Clearance": (0.95, 0.30, 0.18, 1.0),
    "MAT_I0_Text": (0.95, 0.97, 1.0, 1.0),
}

ROOM_PROGRAMME = {
    "F00": ("Arrival / mode selection", "Reception / directory", "Background support"),
    "F01": ("Orientation exhibits", "Body planes / systems", "Background support"),
    "F02": ("Femur learning stage", "Guided task / quiz", "Preparation backdrop"),
    "F03": ("Muscle movement stage", "Layer controls", "Preparation backdrop"),
    "F04": ("Pathway trace stage", "System legend", "Preparation backdrop"),
    "F05": ("Integrated layer-peel", "Find-structure missions", "Preparation backdrop"),
    "F06": ("Assessment floor", "Results / research", "Background support"),
}


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def ensure_dirs() -> None:
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    SOURCE.parent.mkdir(parents=True, exist_ok=True)


def reset() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 800
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    if scene.world is None:
        scene.world = bpy.data.worlds.new("I0_World")
    scene.world.color = (0.055, 0.065, 0.078)
    scene["hospital_interior_stage"] = "I0"
    scene["hospital_interior_revision"] = "R01"
    scene["r40_sha256"] = EXPECTED_R40
    scene["scope"] = "All-floor datum, zoning and circulation greybox only"
    scene["stage6_gate_required_for_permanent_work"] = True


def collection(name: str, parent: bpy.types.Collection | None = None) -> bpy.types.Collection:
    result = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(result)
    return result


def material(name: str, color: Sequence[float]) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = 0.72
    mat["hospital_interior_stage"] = "I0"
    return mat


def mesh_box(name: str, dimensions: Sequence[float]) -> bpy.types.Mesh:
    x, y, z = (float(v) / 2.0 for v in dimensions)
    vertices = [
        (-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
        (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z),
    ]
    faces = [(0, 1, 2, 3), (4, 7, 6, 5), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (4, 0, 3, 7)]
    mesh = bpy.data.meshes.new(name + "_MESH")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    return mesh


def add_box(name: str, location: Sequence[float], dimensions: Sequence[float], target: bpy.types.Collection,
            mat: str, **props) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, mesh_box(name, dimensions))
    target.objects.link(obj)
    obj.location = location
    obj.data.materials.append(bpy.data.materials[mat])
    obj["hospital_interior_stage"] = "I0"
    for key, value in props.items():
        obj[key] = value
    return obj


def add_empty(name: str, location: Sequence[float], target: bpy.types.Collection, **props) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    target.objects.link(obj)
    obj.location = location
    obj.empty_display_type = "ARROWS"
    obj.empty_display_size = 0.45
    obj["hospital_interior_stage"] = "I0"
    for key, value in props.items():
        obj[key] = value
    return obj


def add_text(name: str, body: str, location: Sequence[float], target: bpy.types.Collection, floor_id: str,
             size: float = 0.6) -> bpy.types.Object:
    curve = bpy.data.curves.new(name + "_CURVE", "FONT")
    curve.body = body
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = size
    curve.extrude = 0.01
    obj = bpy.data.objects.new(name, curve)
    target.objects.link(obj)
    obj.location = location
    obj.data.materials.append(bpy.data.materials["MAT_I0_Text"])
    obj["int_floor_id"] = floor_id
    obj["int_review_only"] = True
    return obj


def link_r40_reference(review: bpy.types.Collection) -> bpy.types.Object:
    with bpy.data.libraries.load(str(R40), link=True) as (source, target):
        if "UNITY_EXPORT" not in source.collections:
            raise RuntimeError("R40 UNITY_EXPORT collection is missing")
        target.collections = ["UNITY_EXPORT"]
    instance = bpy.data.objects.new("REFERENCE_HOSPITAL_LOCKED", None)
    instance.instance_type = "COLLECTION"
    instance.instance_collection = bpy.data.collections.get("UNITY_EXPORT")
    instance.display_type = "WIRE"
    instance.hide_render = True
    instance["reference_only"] = True
    instance["source_sha256"] = EXPECTED_R40
    review.objects.link(instance)
    return instance


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
            add_box(
                f"INT_{floor_id}_FloorTile_{index:02d}",
                ((xa + xb) / 2.0, (ya + yb) / 2.0, z + 0.06),
                (xb - xa, yb - ya, 0.12), target, "MAT_I0_Floor",
                int_floor_id=floor_id, int_category="Floor", int_collision="walkable")


def build_floor_enclosure(floor_id: str, z: float, target: bpy.types.Collection) -> None:
    clear = GROUND_CLEAR_HEIGHT if floor_id == "F00" else UPPER_CLEAR_HEIGHT
    x0, x1, y0, y1 = OUTER
    wall_h = clear
    for suffix, loc, dims in (
        ("Front", ((x0 + x1) / 2, y0, z + wall_h / 2), (x1 - x0, 0.12, wall_h)),
        ("Rear", ((x0 + x1) / 2, y1, z + wall_h / 2), (x1 - x0, 0.12, wall_h)),
        ("West", (x0, (y0 + y1) / 2, z + wall_h / 2), (0.12, y1 - y0, wall_h)),
        ("East", (x1, (y0 + y1) / 2, z + wall_h / 2), (0.12, y1 - y0, wall_h)),
    ):
        add_box(f"INT_{floor_id}_Envelope_{suffix}", loc, dims, target, "MAT_I0_Wall",
                int_floor_id=floor_id, int_category="EnvelopeWall")
    add_box(f"INT_{floor_id}_Ceiling", ((x0 + x1) / 2, (y0 + y1) / 2, z + clear),
            (x1 - x0, y1 - y0, 0.08), target, "MAT_I0_Wall",
            int_floor_id=floor_id, int_category="Ceiling")


def build_routes_and_zones(floor_id: str, z: float, target: bpy.types.Collection,
                           guides: bpy.types.Collection) -> None:
    # Exported route markers become simple Unity walkable/collision references.
    routes = (
        ("PrimaryFront", (-9.0, -6.9, z + 0.135), (37.0, 2.4, 0.03)),
        ("ElevatorApproach", (3.0, -1.8, z + 0.135), (2.4, 7.8, 0.03)),
        ("StairAApproach", (-20.6, 0.0, z + 0.135), (3.8, 2.4, 0.03)),
        ("RearLoop", (-9.0, 6.9, z + 0.135), (37.0, 2.4, 0.03)),
    )
    for route_name, loc, dims in routes:
        add_box(f"INT_{floor_id}_ROUTE_{route_name}", loc, dims, target, "MAT_I0_Route",
                int_floor_id=floor_id, int_category="Route", int_route_width=2.4)

    programme = ROOM_PROGRAMME[floor_id]
    zones = (
        ("Primary", (-5.5, -11.0, z + 0.16), (29.0, 7.6, 0.04), "MAT_I0_ZonePrimary", programme[0]),
        ("Secondary", (-5.5, 11.0, z + 0.16), (29.0, 7.6, 0.04), "MAT_I0_ZoneSecondary", programme[1]),
        ("SupportWest", (-29.5, 10.0, z + 0.16), (8.0, 9.0, 0.04), "MAT_I0_ZoneSupport", programme[2]),
    )
    for zone, loc, dims, mat, label in zones:
        add_box(f"INT_{floor_id}_GUIDE_ZONE_{zone}", loc, dims, guides, mat,
                int_floor_id=floor_id, int_category="RoomZone", int_zone_label=label, int_review_only=True)
        add_text(f"INT_{floor_id}_TEXT_{zone}", label, (loc[0], loc[1], z + 0.24), guides, floor_id, 0.48)

    add_text(f"INT_{floor_id}_TEXT_Title", f"{floor_id} — {FLOORS[floor_id]['name']}",
             (-9.0, 14.2, z + 0.25), guides, floor_id, 0.72)


def build_spawns(floor_id: str, z: float, target: bpy.types.Collection, guides: bpy.types.Collection) -> None:
    spawn_specs = [
        ("ELEVATOR", (1.65, 4.25, z), (0.0, 0.0, math.pi)),
        ("STAIR_A", (-21.5, 0.0, z), (0.0, 0.0, math.pi / 2.0)),
    ]
    if floor_id == "F00":
        spawn_specs.append(("ENTRY", (-4.0, -12.5, z), (0.0, 0.0, 0.0)))
    for spawn, loc, rot in spawn_specs:
        empty = add_empty(f"INT_{floor_id}_SPAWN_{spawn}", loc, target,
                          int_floor_id=floor_id, int_category="Spawn", int_spawn_id=spawn)
        empty.rotation_euler = rot
        add_box(f"INT_{floor_id}_GUIDE_SPAWN_{spawn}", (loc[0], loc[1], z + 0.03),
                (0.65, 0.65, 0.06), guides, "MAT_I0_Spawn",
                int_floor_id=floor_id, int_category="SpawnGuide", int_review_only=True)

    for name, x, y in (("ElevatorTurn", 1.65, 1.9), ("StairTurn", -21.0, 0.0)):
        bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=0.75, depth=0.025, location=(x, y, z + 0.18))
        obj = bpy.context.object
        obj.name = f"INT_{floor_id}_GUIDE_CLEARANCE_{name}"
        for owner in list(obj.users_collection):
            owner.objects.unlink(obj)
        guides.objects.link(obj)
        obj.data.materials.append(bpy.data.materials["MAT_I0_Clearance"])
        obj["int_floor_id"] = floor_id
        obj["int_category"] = "TurningClearance"
        obj["int_clear_diameter_m"] = 1.5
        obj["int_review_only"] = True


def build_core(target: bpy.types.Collection, guides: bpy.types.Collection) -> None:
    top = FLOORS["F06"]["z"] + UPPER_CLEAR_HEIGHT
    # Two shafts share the selected rear-right bank. E01 is functional; E02 remains architectural.
    for shaft, xa, xb, functional in (("E01", 0.3, 3.0, True), ("E02", 3.0, 5.7, False)):
        for suffix, loc, dims in (
            ("West", (xa, 4.35, top / 2), (0.12, 2.7, top)),
            ("East", (xb, 4.35, top / 2), (0.12, 2.7, top)),
            ("Rear", ((xa + xb) / 2, 5.7, top / 2), (xb - xa, 0.12, top)),
        ):
            add_box(f"INT_CORE_{shaft}_Shaft_{suffix}", loc, dims, target, "MAT_I0_Core",
                    int_category="ElevatorShaft", int_shaft=shaft, int_functional=functional)

    # One movable cabin starts at F00; Unity moves this root under fade.
    cabin = add_empty("INT_CORE_E01_CabinRoot", (0.0, 0.0, 0.0), target,
                      int_category="ElevatorCabinRoot", int_functional=True)
    for suffix, loc, dims in (
        ("Floor", (1.65, 4.35, 0.06), (1.8, 2.1, 0.12)),
        ("Rear", (1.65, 5.35, 1.2), (1.8, 0.10, 2.4)),
        ("West", (0.78, 4.35, 1.2), (0.08, 2.1, 2.4)),
        ("East", (2.52, 4.35, 1.2), (0.08, 2.1, 2.4)),
        ("Ceiling", (1.65, 4.35, 2.4), (1.8, 2.1, 0.08)),
    ):
        obj = add_box(f"INT_CORE_E01_Cabin_{suffix}", loc, dims, target, "MAT_I0_Elevator",
                      int_category="ElevatorCabin", int_functional=True)
        obj.parent = cabin

    for floor_id, spec in FLOORS.items():
        z = spec["z"]
        clear = GROUND_CLEAR_HEIGHT if floor_id == "F00" else UPPER_CLEAR_HEIGHT
        for shaft, centre, functional in (("E01", 1.65, True), ("E02", 4.35, False)):
            # Door leaves are visual at I0; I1 replaces E01 behavior.
            for side, x in (("L", centre - 0.31), ("R", centre + 0.31)):
                add_box(f"INT_CORE_{floor_id}_{shaft}_Door_{side}", (x, 2.94, z + 1.1),
                        (0.60, 0.10, 2.2), target, "MAT_I0_Elevator",
                        int_floor_id=floor_id, int_category="LandingDoor", int_shaft=shaft,
                        int_functional=functional)
        add_box(f"INT_CORE_{floor_id}_CallPanel", (0.0, 2.88, z + 1.1), (0.16, 0.10, 0.30),
                target, "MAT_I0_Elevator", int_floor_id=floor_id, int_category="CallPanel",
                int_interaction_height=1.1)

        # Landing-only stair shells. A opens east into the loop; B opens west and stays locked.
        for stair, bounds, mat, playable in (
            ("A", STAIR_A, "MAT_I0_StairPlayable", True),
            ("B", STAIR_B, "MAT_I0_StairLocked", False),
        ):
            xa, xb, ya, yb = bounds
            for suffix, loc, dims in (
                ("Front", ((xa + xb) / 2, ya, z + clear / 2), (xb - xa, 0.12, clear)),
                ("Rear", ((xa + xb) / 2, yb, z + clear / 2), (xb - xa, 0.12, clear)),
                ("Outer", ((xa if stair == "A" else xb), (ya + yb) / 2, z + clear / 2),
                 (0.12, yb - ya, clear)),
            ):
                add_box(f"INT_CORE_{floor_id}_Stair{stair}_{suffix}", loc, dims, target, mat,
                        int_floor_id=floor_id, int_category="StairShell", int_stair=stair,
                        int_playable=playable)
            door_x = xb if stair == "A" else xa
            add_box(f"INT_CORE_{floor_id}_Stair{stair}_Door", (door_x, 0.0, z + 1.1),
                    (0.10, 1.2, 2.2), target, mat, int_floor_id=floor_id,
                    int_category="StairDoor", int_stair=stair, int_playable=playable)

        for name, bounds, mat in (("Elevator", ELEVATOR, "MAT_I0_Elevator"),
                                  ("StairA", STAIR_A, "MAT_I0_StairPlayable"),
                                  ("StairB", STAIR_B, "MAT_I0_StairLocked")):
            xa, xb, ya, yb = bounds
            add_box(f"INT_{floor_id}_GUIDE_CORE_{name}", ((xa + xb) / 2, (ya + yb) / 2, z + 0.19),
                    (xb - xa, yb - ya, 0.035), guides, mat, int_floor_id=floor_id,
                    int_category="CoreFootprint", int_review_only=True)


def all_in_collection(root: bpy.types.Collection) -> list[bpy.types.Object]:
    return list(root.all_objects)


def export_collection(root: bpy.types.Collection, path: Path) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in all_in_collection(root):
        if obj.type in {"MESH", "EMPTY"}:
            obj.hide_viewport = False
            obj.select_set(True)
    bpy.context.view_layer.objects.active = next((obj for obj in all_in_collection(root) if obj.type == "MESH"), None)
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={"MESH", "EMPTY"},
        axis_forward="-Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_anim=False, add_leaf_bones=False, use_mesh_modifiers=False, mesh_smooth_type="FACE")
    bpy.ops.object.select_all(action="DESELECT")


def configure_workbench() -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "rim.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.display.shading.show_specular_highlight = False
    scene.display.shading.background_type = "WORLD"


def camera(name: str, location: Sequence[float], target: Sequence[float], ortho: float | None = None,
           lens: float = 42.0) -> bpy.types.Object:
    data = bpy.data.cameras.new(name + "_DATA")
    data.lens = lens
    if ortho is not None:
        data.type = "ORTHO"
        data.ortho_scale = ortho
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    obj["int_review_only"] = True
    return obj


def set_visibility(floor_id: str | None, export_roots: Iterable[bpy.types.Collection], guides: bpy.types.Collection,
                   reference: bpy.types.Object, show_all: bool = False, show_reference: bool = False,
                   plan_view: bool = False) -> None:
    reference.hide_render = not show_reference
    for root in export_roots:
        for obj in root.all_objects:
            if show_all:
                obj.hide_render = False
            elif root.name == "INT_CORE":
                obj.hide_render = True
            else:
                obj.hide_render = obj.get("int_floor_id") != floor_id
                if plan_view and obj.get("int_category") == "Ceiling":
                    obj.hide_render = True
    for obj in guides.all_objects:
        obj.hide_render = not show_all and obj.get("int_floor_id") != floor_id


def render(path: Path, cam: bpy.types.Object) -> None:
    bpy.context.scene.camera = cam
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def build_reviews(floor_collections: dict[str, bpy.types.Collection], core: bpy.types.Collection,
                  guides: bpy.types.Collection, reference: bpy.types.Object) -> list[str]:
    configure_workbench()
    roots = [core, *floor_collections.values()]
    images: list[str] = []
    for floor_id, spec in FLOORS.items():
        set_visibility(floor_id, roots, guides, reference, plan_view=True)
        cam = camera(f"CAM_{floor_id}_Plan", (-9.0, 0.0, spec["z"] + 62.0), (-9.0, 0.0, spec["z"]), ortho=58.0)
        name = f"{len(images)+1:02d}_{floor_id}_AnnotatedPlan.png"
        render(REVIEW_DIR / name, cam)
        images.append(name)

    set_visibility(None, roots, guides, reference, show_all=True)
    for obj in guides.all_objects:
        obj.hide_render = True
    cam = camera("CAM_AllFloorsSection", (48.0, -0.5, 15.0), (-9.0, -0.5, 15.0), ortho=36.0)
    name = f"{len(images)+1:02d}_AllFloors_LongitudinalSection.png"
    render(REVIEW_DIR / name, cam)
    images.append(name)

    # Plan overlay proving the selected rear-right core stays inside the frozen envelope bounds.
    set_visibility("F00", roots, guides, reference, plan_view=True)
    cam = camera("CAM_CoreOverlay", (-9.0, 0.0, 62.0), (-9.0, 0.0, 0.0), ortho=58.0)
    name = f"{len(images)+1:02d}_Core_ExteriorOverlay.png"
    render(REVIEW_DIR / name, cam)
    images.append(name)

    for floor_id, cam_loc, look in (
        ("F00", (-4.0, -12.5, 1.7), (3.0, 3.0, 1.2)),
        ("F02", (-4.0, -9.0, FLOORS["F02"]["z"] + 1.7), (3.0, 3.0, FLOORS["F02"]["z"] + 1.2)),
    ):
        set_visibility(floor_id, roots, guides, reference)
        # Show only same-floor core pieces for player-eye evidence.
        for obj in core.all_objects:
            obj.hide_render = obj.get("int_floor_id") not in {floor_id, None}
            if obj.get("int_floor_id") is None and "Shaft" in obj.name:
                obj.hide_render = True
        cam = camera(f"CAM_{floor_id}_PlayerEye", cam_loc, look, lens=54.0)
        name = f"{len(images)+1:02d}_{floor_id}_PlayerEye_CoreRoute.png"
        render(REVIEW_DIR / name, cam)
        images.append(name)
    return images


def main() -> None:
    ensure_dirs()
    actual = sha256(R40)
    if actual != EXPECTED_R40:
        raise RuntimeError(f"R40 hash mismatch: expected={EXPECTED_R40}; actual={actual}")
    reset()
    for name, color in MATERIALS.items():
        material(name, color)

    root = collection("INT_EXPORT")
    core = collection("INT_CORE", root)
    floor_collections = {floor_id: collection(f"INT_{floor_id}", root) for floor_id in FLOORS}
    guides = collection("INT_GUIDES")
    review = collection("INT_REVIEW")
    reference = link_r40_reference(review)

    for floor_id, spec in FLOORS.items():
        target = floor_collections[floor_id]
        build_floor_slab(floor_id, spec["z"], target)
        build_floor_enclosure(floor_id, spec["z"], target)
        build_routes_and_zones(floor_id, spec["z"], target, guides)
        build_spawns(floor_id, spec["z"], target, guides)
    build_core(core, guides)

    # Datum probes document/validate the FBX axis conversion in Unity.
    for name, loc in (("ORIGIN", (0, 0, 0)), ("X_1M", (1, 0, 0)), ("Y_1M", (0, 1, 0)), ("Z_1M", (0, 0, 1))):
        add_empty(f"INT_DATUM_{name}", loc, core, int_category="DatumProbe", int_probe=name)

    bpy.context.scene["floor_datums_json"] = json.dumps({key: value["z"] for key, value in FLOORS.items()}, sort_keys=True)
    bpy.context.scene["elevator_bounds_json"] = json.dumps(ELEVATOR)
    bpy.context.scene["stair_a_bounds_json"] = json.dumps(STAIR_A)
    bpy.context.scene["stair_b_bounds_json"] = json.dumps(STAIR_B)
    bpy.context.scene["corridor_primary_width_m"] = 2.4
    bpy.context.scene["corridor_secondary_width_m"] = 1.8
    bpy.context.scene["turning_clearance_m"] = 1.5

    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), check_existing=False)
    export_collection(core, EXPORT_DIR / "Hospital_INT_Core_I0_R01.fbx")
    for floor_id, target in floor_collections.items():
        export_collection(target, EXPORT_DIR / f"Hospital_INT_{floor_id}_I0_R01.fbx")

    images = build_reviews(floor_collections, core, guides, reference)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), check_existing=False)
    manifest = {
        "schema": "HospitalInterior.I0.R01.BuildManifest.v1",
        "status": "BUILT_PENDING_VALIDATION",
        "r40_sha256": actual,
        "source": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
        "source_sha256": sha256(SOURCE),
        "floor_datums_blender_z_m": {key: value["z"] for key, value in FLOORS.items()},
        "core": {"elevator_bounds": ELEVATOR, "stair_a_bounds": STAIR_A, "stair_b_bounds": STAIR_B},
        "clearances_m": {"primary_route": 2.4, "secondary_route": 1.8, "turning_diameter": 1.5, "head": 2.3},
        "exports": [path.name for path in sorted(EXPORT_DIR.glob("*.fbx"))],
        "review_images": images,
    }
    (REVIEW_DIR / "StageI0_R01_BuildManifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    print("HOSPITAL_INTERIOR_I0_BUILD=PASS")


if __name__ == "__main__":
    main()
