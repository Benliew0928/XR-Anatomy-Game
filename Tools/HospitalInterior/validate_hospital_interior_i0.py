from __future__ import annotations

import hashlib
import json
from pathlib import Path

import bpy


ROOT = Path(r"C:\CutMyBodyPlease")
R40 = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalExterior" / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"
EXPECTED_R40 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126"
SOURCE = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior" / "HospitalInterior_I0_R01_AllFloorGreybox.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI0_R01"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI0_R01"

FLOORS = {"F00": 0.0, "F01": 5.8, "F02": 9.7, "F03": 13.6, "F04": 17.5, "F05": 21.4, "F06": 25.3}
HOLES = {
    "Elevator": (0.0, 6.0, 3.0, 5.7),
    "StairA": (-27.5, -22.5, -3.6, 3.6),
    "StairB": (4.7, 9.5, -4.0, 2.6),
}


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def check(results: list[dict], name: str, passed: bool, detail: str) -> None:
    results.append({"name": name, "pass": bool(passed), "detail": detail})


def rect_overlap(a, b) -> bool:
    epsilon = 0.0002
    return a[0] < b[1] - epsilon and a[1] > b[0] + epsilon and a[2] < b[3] - epsilon and a[3] > b[2] + epsilon


def main() -> None:
    if not SOURCE.exists():
        raise RuntimeError("I0 source is missing")
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    results: list[dict] = []
    scene = bpy.context.scene

    actual_r40 = sha256(R40)
    check(results, "frozen_r40_hash", actual_r40 == EXPECTED_R40, actual_r40)
    check(results, "metric_common_origin", scene.unit_settings.system == "METRIC" and scene.unit_settings.scale_length == 1.0,
          f"system={scene.unit_settings.system}; scale={scene.unit_settings.scale_length}")
    check(results, "stage_identity", scene.get("hospital_interior_stage") == "I0" and scene.get("hospital_interior_revision") == "R01",
          f"stage={scene.get('hospital_interior_stage')}; revision={scene.get('hospital_interior_revision')}")

    required_collections = ["INT_EXPORT", "INT_CORE", "INT_GUIDES", "INT_REVIEW", *[f"INT_{floor}" for floor in FLOORS]]
    missing_collections = [name for name in required_collections if bpy.data.collections.get(name) is None]
    check(results, "collection_ownership", not missing_collections, "missing=" + ",".join(missing_collections))

    export = bpy.data.collections.get("INT_EXPORT")
    export_objects = list(export.all_objects) if export else []
    reference = bpy.data.objects.get("REFERENCE_HOSPITAL_LOCKED")
    check(results, "locked_reference_isolated", reference is not None and reference.get("reference_only") is True
          and reference not in export_objects and reference.instance_collection is not None,
          "linked R40 reference exists outside INT_EXPORT")
    check(results, "export_has_no_camera_light", not [o.name for o in export_objects if o.type in {"CAMERA", "LIGHT"}],
          "INT_EXPORT contains mesh/empty ownership only")

    recorded = json.loads(scene.get("floor_datums_json", "{}"))
    datum_ok = recorded == FLOORS
    per_floor: dict[str, dict] = {}
    for floor_id, z in FLOORS.items():
        root = bpy.data.collections.get(f"INT_{floor_id}")
        objects = list(root.all_objects) if root else []
        tiles = [o for o in objects if o.get("int_category") == "Floor"]
        routes = [o for o in objects if o.get("int_category") == "Route"]
        spawns = [o for o in objects if o.get("int_category") == "Spawn"]
        ceilings = [o for o in objects if o.get("int_category") == "Ceiling"]
        floor_z_ok = bool(tiles) and all(abs((o.location.z - o.dimensions.z / 2.0) - z) <= 0.0002 for o in tiles)
        hole_intrusions = []
        for tile in tiles:
            rect = (tile.location.x - tile.dimensions.x / 2, tile.location.x + tile.dimensions.x / 2,
                    tile.location.y - tile.dimensions.y / 2, tile.location.y + tile.dimensions.y / 2)
            for hole_name, bounds in HOLES.items():
                if rect_overlap(rect, bounds):
                    hole_intrusions.append(f"{tile.name}:{hole_name}")
        spawn_ids = [o.get("int_spawn_id") for o in spawns]
        required_spawns = {"ELEVATOR", "STAIR_A"} | ({"ENTRY"} if floor_id == "F00" else set())
        per_floor[floor_id] = {
            "z_m": z,
            "tiles": len(tiles),
            "routes": len(routes),
            "spawns": sorted(spawn_ids),
            "ceiling_count": len(ceilings),
            "floor_z_ok": floor_z_ok,
            "hole_intrusions": hole_intrusions,
        }
        datum_ok &= floor_z_ok
        check(results, f"{floor_id.lower()}_complete_owned_greybox",
              bool(root) and len(tiles) >= 10 and len(routes) == 4 and len(ceilings) == 1
              and set(spawn_ids) == required_spawns and not hole_intrusions,
              json.dumps(per_floor[floor_id], sort_keys=True))
    check(results, "exact_floor_datums", datum_ok, json.dumps(recorded, sort_keys=True))

    # Core geometry and selected design choices.
    core = bpy.data.collections.get("INT_CORE")
    core_objects = list(core.all_objects) if core else []
    categories: dict[str, int] = {}
    for obj in core_objects:
        category = str(obj.get("int_category", "<none>"))
        categories[category] = categories.get(category, 0) + 1
    check(results, "rear_right_elevator_bank", json.loads(scene.get("elevator_bounds_json", "[]")) == list(HOLES["Elevator"]),
          scene.get("elevator_bounds_json", "missing"))
    check(results, "separate_nonoverlapping_stairs",
          json.loads(scene.get("stair_a_bounds_json", "[]")) == list(HOLES["StairA"])
          and json.loads(scene.get("stair_b_bounds_json", "[]")) == list(HOLES["StairB"])
          and not rect_overlap(HOLES["Elevator"], HOLES["StairA"])
          and not rect_overlap(HOLES["Elevator"], HOLES["StairB"]),
          json.dumps(HOLES, sort_keys=True))
    check(results, "core_components_complete",
          categories.get("ElevatorShaft", 0) == 6 and categories.get("LandingDoor", 0) == 28
          and categories.get("CallPanel", 0) == 7 and categories.get("StairDoor", 0) == 14
          and categories.get("ElevatorCabinRoot", 0) == 1,
          json.dumps(categories, sort_keys=True))

    guides = bpy.data.collections.get("INT_GUIDES")
    guide_objects = list(guides.all_objects) if guides else []
    zones = [o for o in guide_objects if o.get("int_category") == "RoomZone"]
    clearances = [o for o in guide_objects if o.get("int_category") == "TurningClearance"]
    check(results, "annotated_room_and_clearance_plan", len(zones) == 21 and len(clearances) == 14
          and all(abs(float(o.get("int_clear_diameter_m", 0)) - 1.5) <= 0.0001 for o in clearances),
          f"zones={len(zones)}; turning_clearances={len(clearances)}")
    check(results, "locked_route_dimensions",
          abs(float(scene.get("corridor_primary_width_m", 0)) - 2.4) <= 0.0001
          and abs(float(scene.get("corridor_secondary_width_m", 0)) - 1.8) <= 0.0001,
          f"primary={scene.get('corridor_primary_width_m')}; secondary={scene.get('corridor_secondary_width_m')}")

    exports = sorted(EXPORT_DIR.glob("*.fbx"))
    images = sorted(REVIEW_DIR.glob("*.png"))
    check(results, "eight_separate_origin_exports", len(exports) == 8 and all(path.stat().st_size > 1024 for path in exports),
          ",".join(path.name for path in exports))
    check(results, "i0_review_pack", len(images) == 11 and all(path.stat().st_size > 10_000 for path in images),
          ",".join(path.name for path in images))

    datum_manifest = {
        "schema": "HospitalInterior.I0.R01.DatumManifest.v1",
        "blender_units": "meters",
        "blender_axis": {"plan": ["X", "Y"], "up": "Z"},
        "fbx_export": {"axis_forward": "-Z", "axis_up": "Y", "unit": "meter"},
        "unity_axis_verification": "INT_DATUM_ORIGIN/X_1M/Y_1M/Z_1M must be measured by the I1 validator",
        "floors": [{"floor": key, "exterior_alias": ("L00" if key == "F00" else f"L{int(key[1:]) + 1:02d}"), "z_m": z}
                   for key, z in FLOORS.items()],
        "clear_heights_m": {"F00": 5.05, "upper": 3.48},
        "core_bounds_blender_xy_m": HOLES,
    }
    (REVIEW_DIR / "StageI0_R01_DatumManifest.json").write_text(json.dumps(datum_manifest, indent=2), encoding="utf-8")

    route_map = {
        "schema": "HospitalInterior.I0.R01.RouteInteractionMap.v1",
        "floor_routes": per_floor,
        "primary_route_width_m": 2.4,
        "secondary_route_width_m": 1.8,
        "turning_diameter_m": 1.5,
        "interaction_height_range_m": [0.9, 1.4],
        "elevator": {"functional": "E01", "architectural_only": "E02", "selectable_floors_i1": ["F00", "F02"]},
        "stairs": {"playable": "A", "locked_visual": "B", "i1_route": ["F00", "F01_TRANSIT", "F02"]},
    }
    (REVIEW_DIR / "StageI0_R01_RouteInteractionMap.json").write_text(json.dumps(route_map, indent=2), encoding="utf-8")

    replacement = {
        "schema": "HospitalInterior.I0.R01.ShellReplacementContract.v1",
        "frozen_scene": "Assets/Hospital/Scenes/Additive/Exterior_InteriorShellPreview.unity",
        "profiles": {
            "ExteriorPresentation": {"shell_preview_loaded": True, "interior_loaded": False},
            "InteriorPrototype": {"shell_preview_loaded": False, "interior_core_loaded": True, "active_floor_count": 1},
            "FutureIntegrated": {"status": "DEFERRED_UNTIL_STAGE6_APPROVAL", "rule": "unload shell preview before loading playable interior"},
        },
        "frozen_assets_modified": False,
    }
    (REVIEW_DIR / "StageI0_R01_ShellReplacementContract.json").write_text(json.dumps(replacement, indent=2), encoding="utf-8")

    failures = [item for item in results if not item["pass"]]
    report = {
        "schema": "HospitalInterior.I0.R01.Gate.v1",
        "status": "PASS" if not failures else "FAIL",
        "stage_status": "TECHNICAL_PASS_PENDING_REVIEW" if not failures else "CORRECTION_REQUIRED",
        "source": str(SOURCE.relative_to(ROOT)).replace("\\", "/"),
        "source_sha256": sha256(SOURCE),
        "r40_sha256": actual_r40,
        "checks": results,
        "failures": [item["name"] for item in failures],
        "exports": [{"file": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)} for path in exports],
        "review_images": [{"file": path.name, "bytes": path.stat().st_size, "sha256": sha256(path)} for path in images],
    }
    (REVIEW_DIR / "StageI0_R01_Gate.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"HOSPITAL_INTERIOR_I0_GATE={report['status']}; CHECKS={len(results)}; FAILURES={len(failures)}")
    if failures:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
