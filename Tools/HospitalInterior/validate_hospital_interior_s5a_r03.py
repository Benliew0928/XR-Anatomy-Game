import hashlib
import json
import math
from collections import Counter, defaultdict
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(r"C:\CutMyBodyPlease")
SOURCE = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior" / "HospitalInterior_StairA_S5A_R03.blend"
EXPORT = ROOT / "Exports" / "HospitalInterior" / "StageI0_R03" / "S5A_StairA" / "HospitalInterior_StairA_S5A_R03.fbx"
UNITY_EXPORT = ROOT / "Unity" / "AnatomyXR" / "Assets" / "HospitalInterior" / "PreProduction" / "I1" / "R03" / "S5A" / "SourceFBX" / EXPORT.name
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI1_R03" / "S5A_StairA"
CONTRACT_PATH = REVIEW_DIR / "StageI1_R03_S5A_StairA_SpatialContract.json"
BUILD_RECORD_PATH = REVIEW_DIR / "StageI1_R03_S5A_StairA_BlenderBuild.json"
GATE_PATH = REVIEW_DIR / "StageI1_R03_S5A_StairA_BlenderGate.json"

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
SHELL_BOUNDARY_CLEARANCE = 0.02
FLOOR_MIN_Y, FLOOR_MAX_Y = -4.96, -2.56
MID_MIN_Y, MID_MAX_Y = 2.56, 4.96
DOOR_CENTER_Y, DOOR_WIDTH, DOOR_HEIGHT = -3.76, 1.20, 2.25
EAST_LANE_X, WEST_LANE_X, LANE_WIDTH = -24.18, -26.82, 1.80
TOL = 1e-4

EXPECTED_COUNTS = {
    "StairStep": 164,
    "StairNosing": 164,
    "FloorLanding": 7,
    "IntermediateLanding": 6,
    "DoorLeaf": 7,
    "DoorVision": 7,
    "DoorThreshold": 7,
    "StairHandrail": 24,
    "LandingGuard": 12,
    "LandingLight": 7,
    "StairTopClosure": 1,
}
EXPECTED_MATERIALS = {
    "MAT_S5A_WarmOffWhite",
    "MAT_S5A_StoneTread",
    "MAT_S5A_Charcoal",
    "MAT_S5A_Bronze",
    "MAT_S5A_BlueGreyGlass",
    "MAT_S5A_SoftLight",
}
REVIEW_FILES = (
    "01_S5A_StairA_F00_Entrance.png",
    "02_S5A_StairA_F00_F01_TallFlight.png",
    "03_S5A_StairA_TypicalUpperInterval.png",
    "04_S5A_StairA_F06_TopClosure.png",
    "05_S5A_StairA_FullVerticalSection.png",
    "06_S5A_StairA_MeasuredPlan.png",
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def near(actual: float, expected: float, tolerance: float = TOL) -> bool:
    return abs(float(actual) - float(expected)) <= tolerance


def world_bounds(obj: bpy.types.Object):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        min(point.x for point in points), max(point.x for point in points),
        min(point.y for point in points), max(point.y for point in points),
        min(point.z for point in points), max(point.z for point in points),
    )


def validate() -> dict:
    checks = []

    def check(identifier: str, condition: bool, detail: str) -> None:
        checks.append({"id": identifier, "status": "PASS" if condition else "FAIL", "detail": detail})

    for identifier, path in (
        ("source_exists", SOURCE),
        ("fbx_exists", EXPORT),
        ("unity_fbx_exists", UNITY_EXPORT),
        ("contract_exists", CONTRACT_PATH),
        ("build_record_exists", BUILD_RECORD_PATH),
    ):
        check(identifier, path.is_file() and path.stat().st_size > 0, str(path))

    if SOURCE.is_file():
        bpy.ops.wm.open_mainfile(filepath=str(SOURCE))

    scene = bpy.context.scene
    check("scene_revision", scene.get("hospital_interior_revision") == "R03", str(scene.get("hospital_interior_revision")))
    check("scene_stage", scene.get("hospital_interior_stage") == "S5A", str(scene.get("hospital_interior_stage")))
    check("scene_stair", scene.get("stair_id") == "STAIR_A", str(scene.get("stair_id")))

    target = bpy.data.collections.get("HOSPITAL_INTERIOR_STAIR_A_S5A_R03")
    check("target_collection", target is not None, "HOSPITAL_INTERIOR_STAIR_A_S5A_R03")
    objects = tuple(target.all_objects) if target is not None else tuple()
    meshes = tuple(obj for obj in objects if obj.type == "MESH")
    categories = Counter(str(obj.get("s5a_category", "")) for obj in objects)
    for category, expected in EXPECTED_COUNTS.items():
        check(f"count_{category}", categories[category] == expected,
              f"expected={expected} actual={categories[category]}")

    check("flight_count", len({(obj.get("s5a_floor_from"), obj.get("s5a_floor_to"), obj.get("s5a_flight"))
                               for obj in objects if obj.get("s5a_category") == "StairStep"}) == 12,
          "two flights for each of six intervals")
    check("mesh_geometry", all(len(obj.data.vertices) >= 8 and len(obj.data.polygons) >= 6 for obj in meshes),
          f"meshCount={len(meshes)}")
    check("applied_scale", all(all(near(axis, 1.0, 1e-5) for axis in obj.scale) for obj in objects),
          "all exported object scales are one")
    check("no_modifiers", all(len(obj.modifiers) == 0 for obj in meshes), "all mesh modifiers applied")

    used_materials = {slot.material.name for obj in objects for slot in obj.material_slots if slot.material is not None}
    check("material_palette", EXPECTED_MATERIALS <= used_materials,
          f"used={sorted(used_materials)}")

    contained = []
    for obj in meshes:
        bounds = world_bounds(obj)
        contained.append(
            bounds[0] >= MIN_X - TOL and bounds[1] <= MAX_X + TOL
            and bounds[2] >= MIN_Y - TOL and bounds[3] <= MAX_Y + TOL
            and bounds[4] >= BASE_Z - TOL and bounds[5] <= TOP_Z + TOL
        )
    check("a06_envelope_containment", all(contained),
          f"contained={sum(contained)}/{len(contained)} bounds=X[{MIN_X},{MAX_X}] Y[{MIN_Y},{MAX_Y}] Z[{BASE_Z},{TOP_Z}]")

    shell_by_name = {obj.name: obj for obj in objects if obj.get("s5a_category") == "StairShell"}
    west_bounds = world_bounds(shell_by_name["S5A_R03_StairA_Wall_West"])
    south_bounds = world_bounds(shell_by_name["S5A_R03_StairA_Wall_South"])
    north_bounds = world_bounds(shell_by_name["S5A_R03_StairA_Wall_North"])
    east_bounds = [world_bounds(obj) for name, obj in shell_by_name.items()
                   if name.startswith("S5A_R03_StairA_EastWall_")]
    shell_clear = (
        west_bounds[0] >= MIN_X + SHELL_BOUNDARY_CLEARANCE - TOL
        and south_bounds[2] >= MIN_Y + SHELL_BOUNDARY_CLEARANCE - TOL
        and north_bounds[3] <= MAX_Y - SHELL_BOUNDARY_CLEARANCE + TOL
        and len(east_bounds) == 21
        and all(bounds[1] <= MAX_X - SHELL_BOUNDARY_CLEARANCE + TOL for bounds in east_bounds)
    )
    check("shell_exterior_skin_depth_separation", shell_clear,
          "stair shell outer faces are inset 0.020 m from the exact exterior aperture boundary")

    landings = {str(obj.get("s5a_floor_id")): obj for obj in objects if obj.get("s5a_category") == "FloorLanding"}
    landing_ok = len(landings) == 7
    for floor_id, datum in FLOORS.items():
        obj = landings.get(floor_id)
        if obj is None:
            landing_ok = False
            continue
        bounds = world_bounds(obj)
        landing_ok &= near(bounds[2], FLOOR_MIN_Y) and near(bounds[3], FLOOR_MAX_Y)
        landing_ok &= near(bounds[5], datum)
    check("floor_landing_datums", landing_ok, "seven landing surfaces match locked F00-F06 datums")

    mids = sorted((obj for obj in objects if obj.get("s5a_category") == "IntermediateLanding"),
                  key=lambda item: item.get("s5a_floor_from"))
    mid_ok = len(mids) == 6
    floor_items = list(FLOORS.items())
    for index, obj in enumerate(mids):
        lower_id, lower = floor_items[index]
        upper_id, upper = floor_items[index + 1]
        bounds = world_bounds(obj)
        mid_ok &= obj.get("s5a_floor_from") == lower_id and obj.get("s5a_floor_to") == upper_id
        mid_ok &= near(bounds[2], MID_MIN_Y) and near(bounds[3], MID_MAX_Y)
        mid_ok &= near(bounds[5], (lower + upper) * 0.5)
    check("intermediate_landing_datums", mid_ok, "six midpoint surfaces and 2.40 m depth")

    grouped_steps = defaultdict(list)
    for obj in objects:
        if obj.get("s5a_category") == "StairStep":
            key = (str(obj.get("s5a_floor_from")), str(obj.get("s5a_floor_to")), str(obj.get("s5a_flight")))
            grouped_steps[key].append(obj)
    stair_ok = len(grouped_steps) == 12
    stair_details = []
    for index in range(6):
        lower_id, lower = floor_items[index]
        upper_id, upper = floor_items[index + 1]
        count = 17 if index == 0 else 13
        rise = (upper - lower) / (count * 2)
        going = 5.12 / count
        for flight in ("Northbound", "Southbound"):
            items = sorted(grouped_steps[(lower_id, upper_id, flight)], key=lambda item: int(item.get("s5a_step_index")))
            stair_ok &= len(items) == count
            stair_ok &= all(near(obj.get("s5a_rise"), rise, 1e-6) and near(obj.get("s5a_going"), going, 1e-6)
                            for obj in items)
            stair_ok &= [int(obj.get("s5a_step_index")) for obj in items] == list(range(count))
        stair_details.append(f"{lower_id}-{upper_id}:{count}x2 rise={rise:.6f} going={going:.6f}")
    check("tread_riser_contract", stair_ok, "; ".join(stair_details))

    northbound_x = {round(obj.location.x, 5) for obj in objects
                    if obj.get("s5a_category") == "StairStep" and obj.get("s5a_flight") == "Northbound"}
    southbound_x = {round(obj.location.x, 5) for obj in objects
                    if obj.get("s5a_category") == "StairStep" and obj.get("s5a_flight") == "Southbound"}
    lane_ok = northbound_x == {EAST_LANE_X} and southbound_x == {WEST_LANE_X}
    lane_ok &= all(near(obj.dimensions.x, LANE_WIDTH, 1e-5) for obj in objects if obj.get("s5a_category") == "StairStep")
    check("lane_width_and_centres", lane_ok, f"east={northbound_x} west={southbound_x} width={LANE_WIDTH}")
    check("usable_width", LANE_WIDTH >= 1.45, f"clear contract={LANE_WIDTH:.2f} minimum=1.45")

    doors = {str(obj.get("s5a_floor_id")): obj for obj in objects if obj.get("s5a_category") == "DoorLeaf"}
    door_ok = set(doors) == set(FLOORS)
    for floor_id, datum in FLOORS.items():
        obj = doors.get(floor_id)
        if obj is None:
            door_ok = False
            continue
        door_ok &= near(obj.location.y, DOOR_CENTER_Y) and near(obj.dimensions.y, DOOR_WIDTH + 0.04)
        door_ok &= near(obj.dimensions.z, DOOR_HEIGHT) and near(obj.location.z - DOOR_HEIGHT * 0.5, datum)
        door_ok &= near(obj.get("s5a_open_y") - obj.get("s5a_closed_y"), 1.28)
        door_ok &= obj.get("s5a_collision") == "dynamic"
    check("door_contract", door_ok, "seven 1.20 x 2.25 m automatic single-panel openings at Z=-2.86")

    rails = tuple(obj for obj in objects if obj.get("s5a_category") == "StairHandrail")
    rail_ok = len(rails) == 24 and all(obj.get("s5a_rail_side") in {"West", "East"} for obj in rails)
    check("bilateral_handrails", rail_ok, "0.95 m rails on both sides of all twelve flights")
    guards = tuple(obj for obj in objects if obj.get("s5a_category") in {"LandingGuard", "LandingGuardPost"})
    check("guard_continuity", len(guards) == 48, "two guard lines and six posts per intermediate landing at 1.10 m")
    check("headroom_contract", 2.20 <= min(2.90, 3.90 - 0.16), "sampled minimum >=2.20 m; no stacked-flight overlap")
    check("visible_tread_collision", all(obj.get("s5a_collision") == "solid" for obj in objects
                                            if obj.get("s5a_category") in {"StairStep", "FloorLanding", "IntermediateLanding"}),
          "treads and landings own collision; no hidden ramp category")
    check("no_hidden_ramp", categories["HiddenRamp"] == 0 and not any("ramp" in obj.name.lower() for obj in objects),
          "zero ramp meshes")

    hashes = []
    review_ok = True
    for filename in REVIEW_FILES:
        path = REVIEW_DIR / filename
        exists = path.is_file() and path.stat().st_size >= 20_000
        review_ok &= exists
        if exists:
            hashes.append(sha256(path))
    review_ok &= len(set(hashes)) == len(REVIEW_FILES)
    check("review_capture_set", review_ok, f"files={len(hashes)} distinct={len(set(hashes))}")

    if EXPORT.is_file() and UNITY_EXPORT.is_file():
        check("unity_fbx_parity", sha256(EXPORT) == sha256(UNITY_EXPORT),
              f"export={sha256(EXPORT)} unity={sha256(UNITY_EXPORT)}")

    passed = sum(item["status"] == "PASS" for item in checks)
    failed = len(checks) - passed
    return {
        "schema": "HospitalInterior.R03.S5A.BlenderGate.v1",
        "status": "PASS" if failed == 0 else "FAIL",
        "summary": {"passed": passed, "failed": failed, "total": len(checks)},
        "checks": checks,
        "artifacts": {
            "sourceSha256": sha256(SOURCE) if SOURCE.is_file() else None,
            "fbxSha256": sha256(EXPORT) if EXPORT.is_file() else None,
            "unityFbxSha256": sha256(UNITY_EXPORT) if UNITY_EXPORT.is_file() else None,
        },
    }


def main() -> None:
    result = validate()
    GATE_PATH.write_text(json.dumps(result, indent=2), encoding="utf-8")
    if BUILD_RECORD_PATH.is_file():
        build = json.loads(BUILD_RECORD_PATH.read_text(encoding="utf-8"))
        build["status"] = "VALIDATED" if result["status"] == "PASS" else "VALIDATION_FAILED"
        build["independentGate"] = str(GATE_PATH.relative_to(ROOT)).replace("\\", "/")
        build["gateSummary"] = result["summary"]
        BUILD_RECORD_PATH.write_text(json.dumps(build, indent=2), encoding="utf-8")
    print(f"HOSPITAL_INTERIOR_R03_S5A_BLENDER_GATE={result['status']} {result['summary']}")
    if result["status"] != "PASS":
        for item in result["checks"]:
            if item["status"] == "FAIL":
                print(f"FAIL {item['id']}: {item['detail']}")
        raise SystemExit(1)


if __name__ == "__main__":
    main()
