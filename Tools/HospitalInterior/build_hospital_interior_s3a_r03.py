"""Build the Hospital Interior R03 S3A E01 design-approval candidate.

This script deliberately creates a new elevator design without loading or linking
any R01/R02 or quarantined S3 source.  Run with Blender 5.1+:

    blender --background --python Tools/HospitalInterior/build_hospital_interior_s3a_r03.py

The source .blend, two pre-integration FBXs, six review renders, and a build audit
are generated deterministically under the controlled R03 paths.
"""

from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
SOURCE_DIR = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior"
SOURCE_PATH = SOURCE_DIR / "HospitalInterior_E01_Cabin_R03.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI1_R03_S3A"
CABIN_FBX = EXPORT_DIR / "HospitalInterior_E01_Cabin_R03.fbx"
LANDING_FBX = EXPORT_DIR / "HospitalInterior_E01_LandingPortal_R03.fbx"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI1_R03" / "S3A_E01_Design"
AUDIT_PATH = REVIEW_DIR / "StageI1_R03_S3A_BuildAudit.json"

REVISION = "R03"
STAGE = "S3A"
ELEVATOR_ID = "E01"

CENTER_X = 1.65
CENTER_Y = 4.35
OUTER_WIDTH = 2.35
OUTER_DEPTH = 2.45
OUTER_HEIGHT = 2.60
INNER_WIDTH = 2.10
INNER_DEPTH = 2.20
DOOR_CLEAR_WIDTH = 1.20
DOOR_CLEAR_HEIGHT = 2.20
DOOR_CLOSED_LEFT_X = CENTER_X - 0.3025
DOOR_CLOSED_RIGHT_X = CENTER_X + 0.3025
DOOR_OPEN_LEFT_X = 0.75
DOOR_OPEN_RIGHT_X = 2.55
DOOR_LEAF_WIDTH = 0.595
DOOR_BOTTOM_Z = 0.11

COL_CABIN = "S3A_R03_E01_CABIN"
COL_LANDING = "S3A_R03_E01_LANDING_PORTAL"
COL_REVIEW = "S3A_R03_REVIEW_ONLY"


def ensure_directories() -> None:
    for path in (SOURCE_DIR, EXPORT_DIR, REVIEW_DIR):
        path.mkdir(parents=True, exist_ok=True)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)


def make_collection(name: str) -> bpy.types.Collection:
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    return collection


def move_to_collection(obj: bpy.types.Object, collection: bpy.types.Collection) -> None:
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def set_principled_input(bsdf: bpy.types.Node, name: str, value) -> None:
    socket = bsdf.inputs.get(name)
    if socket is not None:
        socket.default_value = value


def material_principled(
    name: str,
    color: tuple[float, float, float, float],
    metallic: float,
    roughness: float,
    *,
    emission: tuple[float, float, float, float] | None = None,
    emission_strength: float = 0.0,
) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    set_principled_input(bsdf, "Base Color", color)
    set_principled_input(bsdf, "Metallic", metallic)
    set_principled_input(bsdf, "Roughness", roughness)
    if emission is not None:
        set_principled_input(bsdf, "Emission Color", emission)
        set_principled_input(bsdf, "Emission Strength", emission_strength)
    material.diffuse_color = color
    material["s3a_material_role"] = name.removeprefix("MAT_S3A_R03_")
    return material


def material_brushed_steel() -> bpy.types.Material:
    material = material_principled(
        "MAT_S3A_R03_BrushedStainless",
        (0.33, 0.37, 0.40, 1.0),
        0.88,
        0.28,
    )
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    ramp = nodes.new("ShaderNodeValToRGB")
    bump = nodes.new("ShaderNodeBump")
    noise.inputs["Scale"].default_value = 95.0
    noise.inputs["Detail"].default_value = 2.0
    noise.inputs["Roughness"].default_value = 0.38
    ramp.color_ramp.elements[0].color = (0.28, 0.31, 0.33, 1.0)
    ramp.color_ramp.elements[1].color = (0.39, 0.43, 0.46, 1.0)
    bump.inputs["Strength"].default_value = 0.035
    bump.inputs["Distance"].default_value = 0.006
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    set_principled_input(bsdf, "Coat Weight", 0.12)
    set_principled_input(bsdf, "Anisotropic IOR Level", 0.35)
    return material


def material_terrazzo() -> bpy.types.Material:
    material = material_principled(
        "MAT_S3A_R03_DarkTerrazzo",
        (0.075, 0.09, 0.10, 1.0),
        0.05,
        0.38,
    )
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    texcoord = nodes.new("ShaderNodeTexCoord")
    noise = nodes.new("ShaderNodeTexNoise")
    ramp = nodes.new("ShaderNodeValToRGB")
    bump = nodes.new("ShaderNodeBump")
    noise.inputs["Scale"].default_value = 28.0
    noise.inputs["Detail"].default_value = 3.0
    noise.inputs["Roughness"].default_value = 0.63
    ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1])
    for position, color in (
        (0.26, (0.035, 0.045, 0.05, 1.0)),
        (0.46, (0.09, 0.105, 0.115, 1.0)),
        (0.63, (0.22, 0.23, 0.23, 1.0)),
        (0.78, (0.36, 0.31, 0.24, 1.0)),
    ):
        element = ramp.color_ramp.elements.new(position)
        element.color = color
    bump.inputs["Strength"].default_value = 0.15
    bump.inputs["Distance"].default_value = 0.02
    links.new(texcoord.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def create_materials() -> dict[str, bpy.types.Material]:
    return {
        "offwhite": material_principled(
            "MAT_S3A_R03_HygienicWarmOffWhite", (0.64, 0.60, 0.53, 1.0), 0.0, 0.42
        ),
        "offwhite_light": material_principled(
            "MAT_S3A_R03_CeilingSoftWhite", (0.82, 0.78, 0.70, 1.0), 0.0, 0.48
        ),
        "steel": material_brushed_steel(),
        "charcoal": material_principled(
            "MAT_S3A_R03_CharcoalFrame", (0.028, 0.036, 0.043, 1.0), 0.52, 0.24
        ),
        "bronze": material_principled(
            "MAT_S3A_R03_RefinedBronze", (0.27, 0.105, 0.032, 1.0), 0.86, 0.25
        ),
        "floor": material_terrazzo(),
        "emissive": material_principled(
            "MAT_S3A_R03_Diffuser3500K",
            (0.98, 0.83, 0.62, 1.0),
            0.0,
            0.32,
            emission=(1.0, 0.74, 0.46, 1.0),
            emission_strength=3.2,
        ),
        "display": material_principled(
            "MAT_S3A_R03_DisplayGlass", (0.008, 0.015, 0.020, 1.0), 0.22, 0.16
        ),
        "display_text": material_principled(
            "MAT_S3A_R03_DisplayText",
            (0.49, 0.93, 0.95, 1.0),
            0.0,
            0.28,
            emission=(0.25, 0.95, 1.0, 1.0),
            emission_strength=2.5,
        ),
        "button": material_principled(
            "MAT_S3A_R03_ControlButton", (0.13, 0.16, 0.18, 1.0), 0.72, 0.22
        ),
        "button_text": material_principled(
            "MAT_S3A_R03_ControlText",
            (0.94, 0.92, 0.84, 1.0),
            0.0,
            0.30,
            emission=(0.9, 0.78, 0.55, 1.0),
            emission_strength=1.1,
        ),
        "review_floor": material_principled(
            "MAT_S3A_R03_ReviewFloor", (0.12, 0.13, 0.14, 1.0), 0.0, 0.50
        ),
        "stretcher": material_principled(
            "MAT_S3A_R03_StretcherProxy", (0.70, 0.82, 0.84, 1.0), 0.08, 0.36
        ),
        "stretcher_frame": material_principled(
            "MAT_S3A_R03_StretcherFrame", (0.18, 0.21, 0.23, 1.0), 0.78, 0.26
        ),
    }


def apply_bevel(obj: bpy.types.Object, width: float, segments: int = 3) -> None:
    if width <= 0.0 or obj.type != "MESH":
        return
    modifier = obj.modifiers.new("S3A_ProductionEdgeSoftening", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def cube(
    name: str,
    location: tuple[float, float, float],
    dimensions: tuple[float, float, float],
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    *,
    parent: bpy.types.Object | None = None,
    bevel: float = 0.015,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    move_to_collection(obj, collection)
    obj.data.materials.append(material)
    if parent is not None:
        world_matrix = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world_matrix
    apply_bevel(obj, min(bevel, min(dimensions) * 0.30), 3)
    obj["s3a_revision"] = REVISION
    obj["s3a_stage"] = STAGE
    return obj


def cylinder(
    name: str,
    location: tuple[float, float, float],
    radius: float,
    depth: float,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    *,
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0),
    parent: bpy.types.Object | None = None,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    move_to_collection(obj, collection)
    obj.data.materials.append(material)
    if parent is not None:
        world_matrix = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world_matrix
    obj["s3a_revision"] = REVISION
    obj["s3a_stage"] = STAGE
    return obj


def join_mesh_detail(target: bpy.types.Object, detail: bpy.types.Object) -> None:
    """Bake a detail into a moving leaf while retaining the leaf object and pivot."""
    bpy.ops.object.select_all(action="DESELECT")
    target.select_set(True)
    detail.select_set(True)
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.join()
    target.select_set(False)


def text_mesh(
    name: str,
    body: str,
    location: tuple[float, float, float],
    size: float,
    material: bpy.types.Material,
    collection: bpy.types.Collection,
    *,
    rotation: tuple[float, float, float],
    parent: bpy.types.Object | None = None,
    extrude: float = 0.006,
    bevel: float = 0.002,
) -> bpy.types.Object:
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.body = body
    obj.data.align_x = "CENTER"
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = extrude
    obj.data.bevel_depth = bevel
    obj.data.materials.append(material)
    move_to_collection(obj, collection)
    if parent is not None:
        world_matrix = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world_matrix
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    if not obj.data.uv_layers:
        obj.data.uv_layers.new(name="UVMap")
    obj.select_set(False)
    obj["s3a_revision"] = REVISION
    obj["s3a_stage"] = STAGE
    return obj


def text_facing_negative_x() -> tuple[float, float, float]:
    """Keep text horizontal on the cabin's right wall while facing inward."""
    local_x_world = Vector((0.0, -1.0, 0.0))
    local_y_world = Vector((0.0, 0.0, 1.0))
    local_z_world = Vector((-1.0, 0.0, 0.0))
    rotation = Matrix((local_x_world, local_y_world, local_z_world)).transposed().to_euler("XYZ")
    return tuple(rotation)


def empty(name: str, collection: bpy.types.Collection) -> bpy.types.Object:
    obj = bpy.data.objects.new(name, None)
    collection.objects.link(obj)
    obj.empty_display_type = "PLAIN_AXES"
    obj["s3a_revision"] = REVISION
    obj["s3a_stage"] = STAGE
    return obj


def add_panel_reveals(
    root: bpy.types.Object,
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> None:
    rear_y = CENTER_Y + OUTER_DEPTH * 0.5 - 0.115
    for index, x in enumerate((CENTER_X - 0.63, CENTER_X + 0.63), start=1):
        cube(
            f"S3A_R03_E01_RearPanelReveal_{index:02d}",
            (x, rear_y - 0.012, 1.72),
            (0.018, 0.018, 1.40),
            materials["bronze"],
            collection,
            parent=root,
            bevel=0.004,
        )
    for side, x, normal in (("L", CENTER_X - OUTER_WIDTH * 0.5 + 0.115, 1), ("R", CENTER_X + OUTER_WIDTH * 0.5 - 0.115, -1)):
        for index, y in enumerate((CENTER_Y - 0.38, CENTER_Y + 0.38), start=1):
            cube(
                f"S3A_R03_E01_{side}WallPanelReveal_{index:02d}",
                (x + normal * 0.012, y, 1.72),
                (0.018, 0.018, 1.40),
                materials["bronze"],
                collection,
                parent=root,
                bevel=0.004,
            )


def create_cabin(
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    root = empty("S3A_R03_E01_CabinRoot", collection)
    root["elevator_id"] = ELEVATOR_ID
    root["outer_width_m"] = OUTER_WIDTH
    root["outer_depth_m"] = OUTER_DEPTH
    root["outer_height_m"] = OUTER_HEIGHT
    root["inner_width_m"] = INNER_WIDTH
    root["inner_depth_m"] = INNER_DEPTH
    root["door_clear_width_m"] = DOOR_CLEAR_WIDTH
    root["door_clear_height_m"] = DOOR_CLEAR_HEIGHT
    root["design_direction"] = "premium_clinical_future_hospital_stretcher"

    floor = cube(
        "S3A_R03_E01_CabinFloor",
        (CENTER_X, CENTER_Y, 0.055),
        (OUTER_WIDTH, OUTER_DEPTH, 0.11),
        materials["floor"],
        collection,
        parent=root,
        bevel=0.018,
    )
    floor["walkable"] = True
    cube(
        "S3A_R03_E01_CabinCeiling",
        (CENTER_X, CENTER_Y, OUTER_HEIGHT - 0.055),
        (OUTER_WIDTH, OUTER_DEPTH, 0.11),
        materials["offwhite_light"],
        collection,
        parent=root,
        bevel=0.025,
    )

    wall_height = OUTER_HEIGHT - 0.20
    rear_y = CENTER_Y + OUTER_DEPTH * 0.5 - 0.05
    cube(
        "S3A_R03_E01_RearWall",
        (CENTER_X, rear_y, 0.10 + wall_height * 0.5),
        (OUTER_WIDTH - 0.12, 0.10, wall_height),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.026,
    )
    left_x = CENTER_X - OUTER_WIDTH * 0.5 + 0.05
    right_x = CENTER_X + OUTER_WIDTH * 0.5 - 0.05
    for side, x in (("L", left_x), ("R", right_x)):
        cube(
            f"S3A_R03_E01_{side}SideWall",
            (x, CENTER_Y, 0.10 + wall_height * 0.5),
            (0.10, OUTER_DEPTH - 0.12, wall_height),
            materials["offwhite"],
            collection,
            parent=root,
            bevel=0.026,
        )

    lower_height = 0.88
    cube(
        "S3A_R03_E01_RearProtectionPanel",
        (CENTER_X, rear_y - 0.058, 0.13 + lower_height * 0.5),
        (OUTER_WIDTH - 0.25, 0.035, lower_height),
        materials["steel"],
        collection,
        parent=root,
        bevel=0.020,
    )
    for side, x, inset in (("L", left_x + 0.058, 1), ("R", right_x - 0.058, -1)):
        cube(
            f"S3A_R03_E01_{side}ProtectionPanel",
            (x, CENTER_Y + 0.08, 0.13 + lower_height * 0.5),
            (0.035, OUTER_DEPTH - 0.42, lower_height),
            materials["steel"],
            collection,
            parent=root,
            bevel=0.020,
        )
        cube(
            f"S3A_R03_E01_{side}LowerBumper",
            (x + inset * 0.012, CENTER_Y + 0.05, 0.28),
            (0.055, OUTER_DEPTH - 0.36, 0.105),
            materials["charcoal"],
            collection,
            parent=root,
            bevel=0.025,
        )

    add_panel_reveals(root, collection, materials)

    # Full-width bronze rear handrail and three wall brackets.
    cylinder(
        "S3A_R03_E01_RearHandrail",
        (CENTER_X, rear_y - 0.105, 1.02),
        0.032,
        1.78,
        materials["bronze"],
        collection,
        rotation=(0.0, math.pi * 0.5, 0.0),
        parent=root,
    )
    for index, x in enumerate((CENTER_X - 0.72, CENTER_X, CENTER_X + 0.72), start=1):
        cylinder(
            f"S3A_R03_E01_HandrailBracket_{index:02d}",
            (x, rear_y - 0.052, 1.02),
            0.022,
            0.115,
            materials["bronze"],
            collection,
            rotation=(math.pi * 0.5, 0.0, 0.0),
            parent=root,
        )

    # Front pocket returns keep the open door leaves visually contained.
    front_y = CENTER_Y - OUTER_DEPTH * 0.5 + 0.055
    side_return_width = (OUTER_WIDTH - DOOR_CLEAR_WIDTH) * 0.5
    for side, x in (
        ("L", CENTER_X - DOOR_CLEAR_WIDTH * 0.5 - side_return_width * 0.5),
        ("R", CENTER_X + DOOR_CLEAR_WIDTH * 0.5 + side_return_width * 0.5),
    ):
        cube(
            f"S3A_R03_E01_{side}FrontPocketReturn",
            (x, front_y, 1.30),
            (side_return_width, 0.11, OUTER_HEIGHT - 0.20),
            materials["charcoal"],
            collection,
            parent=root,
            bevel=0.018,
        )
    cube(
        "S3A_R03_E01_InteriorDoorHeader",
        (CENTER_X, front_y, 2.45),
        (DOOR_CLEAR_WIDTH + 0.16, 0.12, 0.20),
        materials["bronze"],
        collection,
        parent=root,
        bevel=0.025,
    )

    doors: list[bpy.types.Object] = []
    for side, x, open_x in (
        ("L", DOOR_CLOSED_LEFT_X, DOOR_OPEN_LEFT_X),
        ("R", DOOR_CLOSED_RIGHT_X, DOOR_OPEN_RIGHT_X),
    ):
        door = cube(
            f"S3A_R03_E01_CabinDoor_{side}",
            (x, front_y - 0.012, DOOR_BOTTOM_Z + DOOR_CLEAR_HEIGHT * 0.5),
            (DOOR_LEAF_WIDTH, 0.075, DOOR_CLEAR_HEIGHT),
            materials["steel"],
            collection,
            parent=root,
            bevel=0.018,
        )
        door["door_role"] = "cabin"
        door["door_side"] = side
        door["closed_x"] = x
        door["open_x"] = open_x
        door["travel_m"] = abs(open_x - x)
        doors.append(door)
        edge = cube(
            f"S3A_R03_E01_CabinDoor_{side}_BronzeEdge",
            (x + (0.5 if side == "L" else -0.5) * (DOOR_LEAF_WIDTH - 0.025), front_y - 0.055, 1.20),
            (0.022, 0.018, 2.12),
            materials["bronze"],
            collection,
            bevel=0.004,
        )
        join_mesh_detail(door, edge)

    # Ceiling diffuser, perimeter reveal, and one moving practical light.
    diffuser_z = OUTER_HEIGHT - 0.125
    cube(
        "S3A_R03_E01_CeilingDiffuser",
        (CENTER_X, CENTER_Y + 0.05, diffuser_z),
        (1.35, 1.38, 0.035),
        materials["emissive"],
        collection,
        parent=root,
        bevel=0.050,
    )
    for name, location, dimensions in (
        ("Front", (CENTER_X, CENTER_Y - 0.71, diffuser_z - 0.015), (1.55, 0.045, 0.045)),
        ("Rear", (CENTER_X, CENTER_Y + 0.81, diffuser_z - 0.015), (1.55, 0.045, 0.045)),
        ("Left", (CENTER_X - 0.755, CENTER_Y + 0.05, diffuser_z - 0.015), (0.045, 1.48, 0.045)),
        ("Right", (CENTER_X + 0.755, CENTER_Y + 0.05, diffuser_z - 0.015), (0.045, 1.48, 0.045)),
    ):
        cube(
            f"S3A_R03_E01_DiffuserTrim_{name}",
            location,
            dimensions,
            materials["bronze"],
            collection,
            parent=root,
            bevel=0.008,
        )
    light_data = bpy.data.lights.new("S3A_R03_E01_CabinPracticalLightData", "AREA")
    light_data.energy = 250.0
    light_data.color = (1.0, 0.77, 0.54)
    light_data.shape = "RECTANGLE"
    light_data.size = 1.30
    light_data.size_y = 1.30
    light_data.use_shadow = True
    practical = bpy.data.objects.new("S3A_R03_E01_CabinPracticalLight", light_data)
    practical.location = (CENTER_X, CENTER_Y + 0.05, diffuser_z - 0.045)
    practical.parent = root
    collection.objects.link(practical)
    practical["runtime_role"] = "single_performance_conscious_cabin_light"

    create_control_panel(root, collection, materials)
    return root, doors


def create_control_panel(
    root: bpy.types.Object,
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> None:
    wall_x = CENTER_X + OUTER_WIDTH * 0.5 - 0.115
    panel_y = CENTER_Y - 0.02
    cube(
        "S3A_R03_E01_ControlPanelBronzeSurround",
        (wall_x - 0.045, panel_y, 1.37),
        (0.055, 0.82, 1.58),
        materials["bronze"],
        collection,
        parent=root,
        bevel=0.035,
    )
    cube(
        "S3A_R03_E01_ControlPanel",
        (wall_x - 0.080, panel_y, 1.37),
        (0.045, 0.76, 1.50),
        materials["charcoal"],
        collection,
        parent=root,
        bevel=0.028,
    )
    cube(
        "S3A_R03_E01_CabinFloorDisplay",
        (wall_x - 0.108, panel_y, 1.96),
        (0.030, 0.55, 0.25),
        materials["display"],
        collection,
        parent=root,
        bevel=0.025,
    )
    text_mesh(
        "S3A_R03_E01_CabinFloorDisplayText",
        "F00",
        (wall_x - 0.128, panel_y, 1.96),
        0.15,
        materials["display_text"],
        collection,
        rotation=text_facing_negative_x(),
        parent=root,
        extrude=0.004,
    )

    labels = ["F00", "F01", "F02", "F03", "F04", "F05", "F06", "OPEN", "CLOSE"]
    positions = [
        (CENTER_Y - 0.22, 1.68),
        (CENTER_Y + 0.10, 1.68),
        (CENTER_Y - 0.22, 1.40),
        (CENTER_Y + 0.10, 1.40),
        (CENTER_Y - 0.22, 1.12),
        (CENTER_Y + 0.10, 1.12),
        (CENTER_Y - 0.22, 0.84),
        (CENTER_Y + 0.10, 0.84),
        (CENTER_Y - 0.06, 0.60),
    ]
    for index, (label, (y, z)) in enumerate(zip(labels, positions), start=1):
        width = 0.26 if label in ("OPEN", "CLOSE") else 0.23
        cube(
            f"S3A_R03_E01_ControlButton_{index:02d}_{label}",
            (wall_x - 0.118, y, z),
            (0.040, width, 0.18),
            materials["button"],
            collection,
            parent=root,
            bevel=0.045,
        )
        text_mesh(
            f"S3A_R03_E01_ControlLabel_{index:02d}_{label}",
            label,
            (wall_x - 0.143, y, z),
            0.064 if label in ("OPEN", "CLOSE") else 0.075,
            materials["button_text"],
            collection,
            rotation=text_facing_negative_x(),
            parent=root,
            extrude=0.003,
            bevel=0.001,
        )


def create_landing_portal(
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    root = empty("S3A_R03_E01_LandingPortalRoot", collection)
    root["elevator_id"] = ELEVATOR_ID
    root["reusable_at_floor_datums"] = "F00,F01,F02,F03,F04,F05,F06"
    root["door_clear_width_m"] = DOOR_CLEAR_WIDTH
    root["door_clear_height_m"] = DOOR_CLEAR_HEIGHT

    portal_front_y = 2.96
    wall_depth = 0.20
    cell_min_x, cell_max_x = 0.30, 3.00
    opening_min_x = CENTER_X - DOOR_CLEAR_WIDTH * 0.5
    opening_max_x = CENTER_X + DOOR_CLEAR_WIDTH * 0.5
    left_width = opening_min_x - cell_min_x
    right_width = cell_max_x - opening_max_x
    cube(
        "S3A_R03_E01_LandingWall_L",
        (cell_min_x + left_width * 0.5, portal_front_y, 1.30),
        (left_width, wall_depth, 2.60),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.020,
    )
    cube(
        "S3A_R03_E01_LandingWall_R",
        (opening_max_x + right_width * 0.5, portal_front_y, 1.30),
        (right_width, wall_depth, 2.60),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.020,
    )
    cube(
        "S3A_R03_E01_LandingWall_Header",
        (CENTER_X, portal_front_y, 2.46),
        (DOOR_CLEAR_WIDTH, wall_depth, 0.28),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.020,
    )
    frame_y = portal_front_y - 0.115
    for side, x in (("L", opening_min_x - 0.075), ("R", opening_max_x + 0.075)):
        cube(
            f"S3A_R03_E01_LandingFrame_{side}",
            (x, frame_y, 1.22),
            (0.15, 0.09, 2.44),
            materials["bronze"],
            collection,
            parent=root,
            bevel=0.022,
        )
        cube(
            f"S3A_R03_E01_LandingFrame_{side}_CharcoalInset",
            (x + (0.040 if side == "L" else -0.040), frame_y - 0.052, 1.22),
            (0.050, 0.025, 2.32),
            materials["charcoal"],
            collection,
            parent=root,
            bevel=0.008,
        )
    cube(
        "S3A_R03_E01_LandingFrame_Header",
        (CENTER_X, frame_y, 2.40),
        (DOOR_CLEAR_WIDTH + 0.30, 0.09, 0.18),
        materials["bronze"],
        collection,
        parent=root,
        bevel=0.025,
    )
    threshold = cube(
        "S3A_R03_E01_LandingThreshold",
        (CENTER_X, 3.04, 0.055),
        (DOOR_CLEAR_WIDTH + 0.18, 0.42, 0.11),
        materials["steel"],
        collection,
        parent=root,
        bevel=0.012,
    )
    threshold["flush_threshold"] = True

    door_y = portal_front_y - 0.020
    doors: list[bpy.types.Object] = []
    for side, x, open_x in (
        ("L", DOOR_CLOSED_LEFT_X, DOOR_OPEN_LEFT_X),
        ("R", DOOR_CLOSED_RIGHT_X, DOOR_OPEN_RIGHT_X),
    ):
        door = cube(
            f"S3A_R03_E01_LandingDoor_{side}",
            (x, door_y, DOOR_BOTTOM_Z + DOOR_CLEAR_HEIGHT * 0.5),
            (DOOR_LEAF_WIDTH, 0.075, DOOR_CLEAR_HEIGHT),
            materials["steel"],
            collection,
            parent=root,
            bevel=0.018,
        )
        door["door_role"] = "landing"
        door["door_side"] = side
        door["closed_x"] = x
        door["open_x"] = open_x
        door["travel_m"] = abs(open_x - x)
        doors.append(door)
        edge = cube(
            f"S3A_R03_E01_LandingDoor_{side}_BronzeEdge",
            (x + (0.5 if side == "L" else -0.5) * (DOOR_LEAF_WIDTH - 0.025), door_y - 0.050, 1.20),
            (0.022, 0.018, 2.12),
            materials["bronze"],
            collection,
            bevel=0.004,
        )
        join_mesh_detail(door, edge)

    cube(
        "S3A_R03_E01_LandingIndicatorFrame",
        (CENTER_X, frame_y - 0.035, 2.55),
        (0.62, 0.055, 0.24),
        materials["bronze"],
        collection,
        parent=root,
        bevel=0.035,
    )
    cube(
        "S3A_R03_E01_LandingIndicator",
        (CENTER_X, frame_y - 0.069, 2.55),
        (0.52, 0.025, 0.16),
        materials["display"],
        collection,
        parent=root,
        bevel=0.020,
    )
    text_mesh(
        "S3A_R03_E01_LandingIndicatorText",
        "F00",
        (CENTER_X, frame_y - 0.087, 2.55),
        0.115,
        materials["display_text"],
        collection,
        rotation=(math.pi * 0.5, 0.0, 0.0),
        parent=root,
        extrude=0.004,
    )

    call_x = 2.67
    cube(
        "S3A_R03_E01_LandingCallPanelFrame",
        (call_x, frame_y - 0.050, 1.12),
        (0.28, 0.055, 0.54),
        materials["bronze"],
        collection,
        parent=root,
        bevel=0.042,
    )
    cube(
        "S3A_R03_E01_LandingCallPanel",
        (call_x, frame_y - 0.086, 1.12),
        (0.22, 0.025, 0.46),
        materials["charcoal"],
        collection,
        parent=root,
        bevel=0.034,
    )
    cube(
        "S3A_R03_E01_LandingCallButton",
        (call_x, frame_y - 0.107, 1.15),
        (0.13, 0.025, 0.13),
        materials["button"],
        collection,
        parent=root,
        bevel=0.050,
    )
    text_mesh(
        "S3A_R03_E01_LandingCallLabel",
        "CALL",
        (call_x, frame_y - 0.128, 0.94),
        0.052,
        materials["button_text"],
        collection,
        rotation=(math.pi * 0.5, 0.0, 0.0),
        parent=root,
        extrude=0.003,
    )
    return root, doors


def create_stretcher_proxy(
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> bpy.types.Object:
    root = empty("S3A_R03_REVIEW_StretcherProxyRoot", collection)
    root["proxy_width_m"] = 0.80
    root["proxy_length_m"] = 2.10
    root["review_only"] = True
    cube(
        "S3A_R03_REVIEW_StretcherMattress",
        (CENTER_X, CENTER_Y - 0.08, 0.79),
        (0.80, 2.10, 0.14),
        materials["stretcher"],
        collection,
        parent=root,
        bevel=0.070,
    )
    cube(
        "S3A_R03_REVIEW_StretcherDeck",
        (CENTER_X, CENTER_Y - 0.08, 0.68),
        (0.72, 1.98, 0.08),
        materials["stretcher_frame"],
        collection,
        parent=root,
        bevel=0.025,
    )
    for side, x in (("L", CENTER_X - 0.39), ("R", CENTER_X + 0.39)):
        cylinder(
            f"S3A_R03_REVIEW_StretcherRail_{side}",
            (x, CENTER_Y - 0.08, 0.98),
            0.018,
            1.86,
            materials["stretcher_frame"],
            collection,
            rotation=(math.pi * 0.5, 0.0, 0.0),
            parent=root,
        )
    for index, (x, y) in enumerate(
        (
            (CENTER_X - 0.28, CENTER_Y - 0.82),
            (CENTER_X + 0.28, CENTER_Y - 0.82),
            (CENTER_X - 0.28, CENTER_Y + 0.66),
            (CENTER_X + 0.28, CENTER_Y + 0.66),
        ),
        start=1,
    ):
        cylinder(
            f"S3A_R03_REVIEW_StretcherLeg_{index:02d}",
            (x, y, 0.39),
            0.022,
            0.55,
            materials["stretcher_frame"],
            collection,
            parent=root,
        )
        cylinder(
            f"S3A_R03_REVIEW_StretcherWheel_{index:02d}",
            (x, y, 0.13),
            0.085,
            0.035,
            materials["charcoal"],
            collection,
            rotation=(0.0, math.pi * 0.5, 0.0),
            parent=root,
        )
    return root


def create_review_environment(
    collection: bpy.types.Collection,
    materials: dict[str, bpy.types.Material],
) -> tuple[bpy.types.Object, list[bpy.types.Object]]:
    root = empty("S3A_R03_REVIEW_EnvironmentRoot", collection)
    root["review_only"] = True
    cube(
        "S3A_R03_REVIEW_LandingFloor",
        (CENTER_X, 0.85, -0.055),
        (7.2, 4.2, 0.11),
        materials["review_floor"],
        collection,
        parent=root,
        bevel=0.008,
    )
    cube(
        "S3A_R03_REVIEW_LandingCeiling",
        (CENTER_X, 0.90, 2.90),
        (7.2, 4.1, 0.10),
        materials["offwhite_light"],
        collection,
        parent=root,
        bevel=0.018,
    )
    cube(
        "S3A_R03_REVIEW_LeftWingWall",
        (-1.40, 2.96, 1.45),
        (3.4, 0.20, 2.90),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.020,
    )
    cube(
        "S3A_R03_REVIEW_RightWingWall",
        (4.70, 2.96, 1.45),
        (3.4, 0.20, 2.90),
        materials["offwhite"],
        collection,
        parent=root,
        bevel=0.020,
    )

    lights: list[bpy.types.Object] = []
    for name, location, energy, size, color in (
        ("LandingKey", (CENTER_X - 1.55, 0.40, 2.55), 460.0, 2.3, (1.0, 0.84, 0.68)),
        ("LandingFill", (CENTER_X + 2.20, 1.10, 2.15), 310.0, 2.0, (0.68, 0.79, 1.0)),
        ("CabinFill", (CENTER_X - 0.85, 4.00, 2.15), 210.0, 1.2, (0.80, 0.88, 1.0)),
    ):
        data = bpy.data.lights.new(f"S3A_R03_REVIEW_{name}Data", "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        data.color = color
        light = bpy.data.objects.new(f"S3A_R03_REVIEW_{name}", data)
        light.location = location
        light.rotation_euler = (0.0, 0.0, 0.0)
        light.parent = root
        collection.objects.link(light)
        light["review_only"] = True
        lights.append(light)
    return root, lights


def camera(name: str, location: tuple[float, float, float], target: tuple[float, float, float], lens: float) -> bpy.types.Object:
    data = bpy.data.cameras.new(name + "Data")
    data.lens = lens
    data.sensor_width = 36.0
    data.dof.use_dof = False
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = location
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = obj
    return obj


def set_door_state(doors: list[bpy.types.Object], open_state: bool) -> None:
    for door in doors:
        door.location.x = float(door["open_x"] if open_state else door["closed_x"])


def set_hidden(root: bpy.types.Object, hidden: bool) -> None:
    root.hide_render = hidden
    for child in root.children_recursive:
        child.hide_render = hidden


def configure_render() -> None:
    scene = bpy.context.scene
    # Blender 5.1 exposes Eevee Next under the consolidated BLENDER_EEVEE id.
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.use_file_extension = True
    scene.render.image_settings.color_depth = "8"
    scene.render.resolution_percentage = 100
    scene.world.color = (0.015, 0.018, 0.022)
    try:
        scene.view_settings.look = "AgX - Medium High Contrast"
    except TypeError:
        pass
    scene.view_settings.view_transform = "AgX"
    scene.view_settings.exposure = 0.0


def render_views(
    all_doors: list[bpy.types.Object],
    stretcher_root: bpy.types.Object,
) -> list[Path]:
    views = [
        (
            "01_S3A_E01_Landing_DoorsClosed.png",
            False,
            False,
            (1.65, -2.20, 1.58),
            (1.65, 3.02, 1.36),
            44.0,
        ),
        (
            "02_S3A_E01_Landing_DoorsOpen.png",
            True,
            False,
            (1.65, -2.10, 1.58),
            (1.65, 4.18, 1.34),
            42.0,
        ),
        (
            "03_S3A_E01_CabinInterior.png",
            True,
            False,
            (1.65, 2.20, 1.48),
            (1.65, 4.78, 1.34),
            31.0,
        ),
        (
            "04_S3A_E01_ControlPanelClose.png",
            True,
            False,
            (0.82, 3.48, 1.34),
            (2.72, 4.29, 1.26),
            29.0,
        ),
        (
            "05_S3A_E01_StretcherClearance.png",
            True,
            True,
            (1.65, -3.65, 1.72),
            (1.65, 3.85, 1.16),
            45.0,
        ),
        (
            "06_S3A_E01_MaterialsAndCeilingLight.png",
            True,
            False,
            (1.35, 3.30, 1.20),
            (1.70, 4.80, 1.55),
            25.0,
        ),
    ]
    outputs: list[Path] = []
    for filename, open_doors, show_stretcher, location, target, lens in views:
        set_door_state(all_doors, open_doors)
        set_hidden(stretcher_root, not show_stretcher)
        current_camera = camera("S3A_R03_REVIEW_Camera", location, target, lens)
        output = REVIEW_DIR / filename
        bpy.context.scene.render.filepath = str(output)
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(current_camera, do_unlink=True)
        outputs.append(output)
    set_door_state(all_doors, False)
    set_hidden(stretcher_root, True)
    return outputs


def select_hierarchy(root: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root


def export_fbx(root: bpy.types.Object, path: Path) -> None:
    select_hierarchy(root)
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"EMPTY", "MESH", "LIGHT"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        use_space_transform=True,
        bake_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_custom_props=True,
        path_mode="AUTO",
        bake_anim=False,
    )
    bpy.ops.object.select_all(action="DESELECT")


def configure_scene_metadata() -> None:
    scene = bpy.context.scene
    scene.name = "HospitalInterior_E01_Cabin_R03"
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.length_unit = "METERS"
    scene.unit_settings.scale_length = 1.0
    scene["project"] = "CutMyBodyPlease.HospitalInterior"
    scene["revision"] = REVISION
    scene["stage"] = STAGE
    scene["status"] = "DESIGN_CANDIDATE_AWAITING_USER_APPROVAL"
    scene["legacy_geometry_reused"] = False
    scene["runtime_integration_authorized"] = False
    scene["core_bounds_x"] = "0.0..6.0"
    scene["core_bounds_y"] = "3.0..5.7"
    scene["e01_provisional_center_xy"] = f"{CENTER_X:.2f},{CENTER_Y:.2f}"


def write_audit(render_paths: list[Path]) -> None:
    mesh_objects = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    source_meshes = [
        obj for obj in mesh_objects if not obj.name.startswith("S3A_R03_REVIEW_")
    ]
    triangles = sum(
        len(loop_triangles(obj)) for obj in source_meshes
    )
    audit = {
        "schema": "HospitalInterior.R03.S3A.BuildAudit.v1",
        "status": "BUILT_AWAITING_USER_APPROVAL",
        "blenderVersion": bpy.app.version_string,
        "revision": REVISION,
        "stage": STAGE,
        "elevatorId": ELEVATOR_ID,
        "legacyGeometryReused": False,
        "runtimeIntegrationAuthorized": False,
        "dimensionsM": {
            "outerWidth": OUTER_WIDTH,
            "outerDepth": OUTER_DEPTH,
            "outerHeight": OUTER_HEIGHT,
            "innerWidth": INNER_WIDTH,
            "innerDepth": INNER_DEPTH,
            "doorClearWidth": DOOR_CLEAR_WIDTH,
            "doorClearHeight": DOOR_CLEAR_HEIGHT,
            "stretcherProxyWidth": 0.80,
            "stretcherProxyLength": 2.10,
        },
        "counts": {
            "objects": len(bpy.data.objects),
            "sourceMeshObjects": len(source_meshes),
            "sourceTriangles": triangles,
            "materials": len(bpy.data.materials),
            "camerasSaved": len([obj for obj in bpy.data.objects if obj.type == "CAMERA"]),
        },
        "outputs": [
            {"kind": "blend", "path": str(SOURCE_PATH.relative_to(ROOT)).replace("\\", "/"), "sha256": sha256(SOURCE_PATH)},
            {"kind": "cabin_fbx", "path": str(CABIN_FBX.relative_to(ROOT)).replace("\\", "/"), "sha256": sha256(CABIN_FBX)},
            {"kind": "landing_fbx", "path": str(LANDING_FBX.relative_to(ROOT)).replace("\\", "/"), "sha256": sha256(LANDING_FBX)},
        ]
        + [
            {
                "kind": "review_render",
                "path": str(path.relative_to(ROOT)).replace("\\", "/"),
                "sha256": sha256(path),
                "bytes": path.stat().st_size,
            }
            for path in render_paths
        ],
    }
    AUDIT_PATH.write_text(json.dumps(audit, indent=2), encoding="utf-8")


def loop_triangles(obj: bpy.types.Object):
    obj.data.calc_loop_triangles()
    return obj.data.loop_triangles


def main() -> None:
    ensure_directories()
    reset_scene()
    configure_scene_metadata()
    configure_render()
    materials = create_materials()
    cabin_collection = make_collection(COL_CABIN)
    landing_collection = make_collection(COL_LANDING)
    review_collection = make_collection(COL_REVIEW)
    cabin_root, cabin_doors = create_cabin(cabin_collection, materials)
    landing_root, landing_doors = create_landing_portal(landing_collection, materials)
    stretcher_root = create_stretcher_proxy(review_collection, materials)
    create_review_environment(review_collection, materials)
    all_doors = cabin_doors + landing_doors

    # The saved and exported design authority is closed and has no visible review proxy.
    set_door_state(all_doors, False)
    set_hidden(stretcher_root, True)
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_PATH), check_existing=False)
    export_fbx(cabin_root, CABIN_FBX)
    export_fbx(landing_root, LANDING_FBX)
    render_paths = render_views(all_doors, stretcher_root)
    set_door_state(all_doors, False)
    set_hidden(stretcher_root, True)
    bpy.context.scene.render.filepath = ""
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_PATH), check_existing=False)
    write_audit(render_paths)
    print(
        "HOSPITAL_INTERIOR_R03_S3A_BUILD=COMPLETE_AWAITING_USER_APPROVAL; "
        f"source={SOURCE_PATH}; renders={len(render_paths)}"
    )


if __name__ == "__main__":
    main()
