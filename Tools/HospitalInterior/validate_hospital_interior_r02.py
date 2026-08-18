from __future__ import annotations

import hashlib
import json
from collections import Counter
from pathlib import Path

import bpy


ROOT = Path(r"C:\CutMyBodyPlease")
SOURCE_DIR = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior"
ASSEMBLY = SOURCE_DIR / "HospitalInterior_I0_R02_AllFloorAssembly.blend"
MODULE_KIT = SOURCE_DIR / "HospitalInterior_ModuleKit_R02.blend"
CORE_SOURCE = SOURCE_DIR / "HospitalInterior_Core_R02.blend"
R40 = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalExterior" / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"
R40_BACKUP = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalExterior" / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend1"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI0_R02"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI0_R02"
EXPECTED_R40 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126"
EXPECTED_R40_BACKUP = "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58"
FLOORS = {"F00": 0.0, "F01": 5.8, "F02": 9.7, "F03": 13.6, "F04": 17.5, "F05": 21.4, "F06": 25.3}
ELEVATOR = (0.0, 6.0, 3.0, 5.7)
STAIR_A = (-27.5, -22.5, -3.6, 3.6)
STAIR_B = (4.7, 9.5, -4.0, 2.6)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def triangles(mesh: bpy.types.Mesh) -> int:
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


checks: list[dict] = []


def check(name: str, passed: bool, detail: str) -> None:
    checks.append({"name": name, "pass": bool(passed), "detail": detail})
    print(f"R02_BLENDER_CHECK {name}={'PASS' if passed else 'FAIL'}: {detail}")


def objects_in(collection_name: str) -> list[bpy.types.Object]:
    found = bpy.data.collections.get(collection_name)
    return list(found.all_objects) if found else []


def main() -> None:
    REVIEW_DIR.mkdir(parents=True, exist_ok=True)
    check("protected_r40_master", R40.exists() and sha256(R40) == EXPECTED_R40, sha256(R40) if R40.exists() else "MISSING")
    check("protected_r40_backup", R40_BACKUP.exists() and sha256(R40_BACKUP) == EXPECTED_R40_BACKUP,
          sha256(R40_BACKUP) if R40_BACKUP.exists() else "MISSING")
    check("r02_sources_exist", all(path.exists() for path in (ASSEMBLY, MODULE_KIT, CORE_SOURCE)),
          ";".join(path.name for path in (ASSEMBLY, MODULE_KIT, CORE_SOURCE)))
    check("assembly_revision_metadata", bpy.context.scene.get("hospital_interior_revision") == "R02",
          str(bpy.context.scene.get("hospital_interior_revision")))

    expected_exports = [EXPORT_DIR / "Hospital_INT_Core_I1_R02.fbx"] + [
        EXPORT_DIR / f"Hospital_INT_{floor_id}_I0_R02.fbx" for floor_id in FLOORS]
    check("versioned_export_set", all(path.exists() and path.stat().st_size > 1024 for path in expected_exports),
          ";".join(path.name for path in expected_exports))

    material_names = sorted(material.name for material in bpy.data.materials if material.name.startswith("MAT_R02_"))
    check("controlled_material_budget", 8 <= len(material_names) <= 16, f"materials={len(material_names)}")
    check("material_nodes", all(bpy.data.materials[name].use_nodes for name in material_names), ",".join(material_names))

    all_export_objects: list[bpy.types.Object] = []
    floor_report = {}
    for floor_id, z in FLOORS.items():
        objects = objects_in(f"EXPORT_I0_R02_{floor_id}")
        meshes = [obj for obj in objects if obj.type == "MESH"]
        all_export_objects.extend(meshes)
        floor_triangles = sum(triangles(obj.data) for obj in meshes)
        floor_report[floor_id] = {
            "z_m": z,
            "mesh_objects": len(meshes),
            "triangles": floor_triangles,
            "shared_meshes": len({obj.data.name for obj in meshes}),
            "template_mesh_objects": len([obj for obj in meshes if obj.name != f"INT_{floor_id}_FloorNumber"]),
            "template_triangles": sum(triangles(obj.data) for obj in meshes if obj.name != f"INT_{floor_id}_FloorNumber"),
            "template_shared_meshes": len({obj.data.name for obj in meshes if obj.name != f"INT_{floor_id}_FloorNumber"}),
            "spawns": sorted(obj.name.split("_SPAWN_")[-1] for obj in objects if "_SPAWN_" in obj.name),
        }
        expected_spawns = {"ELEVATOR", "STAIR_A"} | ({"ENTRY"} if floor_id == "F00" else set())
        actual_spawns = set(floor_report[floor_id]["spawns"])
        check(f"{floor_id.lower()}_art_shell", len(meshes) >= 70 and floor_triangles <= 60000,
              f"meshes={len(meshes)}; triangles={floor_triangles}")
        check(f"{floor_id.lower()}_spawn_contract", actual_spawns == expected_spawns,
              f"actual={sorted(actual_spawns)}")
        scale_ok = all(max(abs(value - 1.0) for value in obj.scale) < 0.0001 for obj in meshes)
        check(f"{floor_id.lower()}_applied_scale", scale_ok, f"mesh_objects={len(meshes)}")
        forbidden = ("RoomBoundary", "RoomDoor", "Partition", "FuturePortal", "Vestibule", "FloorTitle")
        unwanted = sorted(obj.name for obj in objects if any(token in obj.name for token in forbidden))
        floor_number = [obj for obj in meshes if obj.name == f"INT_{floor_id}_FloorNumber"]
        floor_plaque = [obj for obj in meshes if obj.name == f"INT_{floor_id}_FloorNumberPlaque"]
        check(f"{floor_id.lower()}_clean_open_lobby",
              not unwanted and len(floor_number) == 1 and len(floor_plaque) == 1,
              f"unwanted={unwanted}; floor_number={len(floor_number)}; plaque={len(floor_plaque)}")

    template_counts = {(item["template_mesh_objects"], item["template_triangles"], item["template_shared_meshes"])
                       for item in floor_report.values()}
    check("repeated_lobby_template", len(template_counts) == 1,
          f"unique_signatures_excluding_allowed_number_text={len(template_counts)}; signatures={sorted(template_counts)}")

    core_objects = objects_in("EXPORT_I1_R02_CORE")
    core_meshes = [obj for obj in core_objects if obj.type == "MESH"]
    all_export_objects.extend(core_meshes)
    core_triangles = sum(triangles(obj.data) for obj in core_meshes)
    check("core_art_budget", len(core_meshes) >= 300 and core_triangles <= 150000,
          f"meshes={len(core_meshes)}; triangles={core_triangles}")
    check("core_datums", all(any(obj.name == name for obj in core_objects) for name in
          ("INT_DATUM_ORIGIN", "INT_DATUM_X_1M", "INT_DATUM_Y_1M", "INT_DATUM_Z_1M")), "four datum probes")
    check("functional_cabin_art", all(any(obj.name == name for obj in core_objects) for name in
          ("INT_CORE_E01_CabinRoot", "INT_CORE_E01_CabinControlPanel", "INT_CORE_E01_CabinDoor_L", "INT_CORE_E01_CabinDoor_R")),
          "cabin root, panel, and two cabin leaves")
    check("two_lift_portals_each_floor", all(sum(1 for obj in core_objects if obj.name.startswith(f"INT_CORE_{floor_id}_E0") and "_Door_" in obj.name) == 4
          for floor_id in FLOORS), "E01/E02 left+right leaves on F00-F06")
    interval_ids = list(FLOORS.keys())[:-1]
    check("stair_art_each_floor", all(
          sum(1 for obj in core_objects if obj.name.startswith(f"INT_CORE_{floor_id}_Stair{stair}_Step_F01_")) >= 10
          and sum(1 for obj in core_objects if obj.name.startswith(f"INT_CORE_{floor_id}_Stair{stair}_Step_F02_")) >= 10
          for floor_id in interval_ids for stair in ("A", "B")),
          "two real-height flights per floor interval")
    stair_continuity = True
    stair_continuity_details = []
    floor_items = list(FLOORS.items())
    for floor_index, (floor_id, z) in enumerate(floor_items[:-1]):
        next_floor_id, next_z = floor_items[floor_index + 1]
        for stair in ("A", "B"):
            steps = [obj for obj in core_objects if obj.name.startswith(f"INT_CORE_{floor_id}_Stair{stair}_Step_")]
            tops = sorted(float(obj.get("int_step_top_z", -999.0)) for obj in steps)
            first_top = tops[0] if tops else -999.0
            last_top = tops[-1] if tops else -999.0
            passed = bool(steps) and first_top > z and abs(last_top - next_z) < 0.02
            stair_continuity = stair_continuity and passed
            stair_continuity_details.append(f"{floor_id}-{next_floor_id}/{stair}:{first_top:.2f}->{last_top:.2f}")
    check("stair_complete_switchback", stair_continuity,
          "real datum intervals; " + "; ".join(stair_continuity_details))
    check("stair_termination_finish", all(
          all(any(obj.name == f"INT_CORE_{floor_id}_Stair{stair}_{suffix}" for obj in core_objects)
              for suffix in ("F01_Soffit", "F02_Soffit", "Landing_Mid", "StatePlate"))
          for floor_id in interval_ids for stair in ("A", "B"))
          and all(any(obj.name == f"INT_CORE_F06_Stair{stair}_ShaftTopClosure" for obj in core_objects)
                  for stair in ("A", "B")),
          "soffits, intermediate landing, state plate per interval and top closure on F06")
    check("stair_door_contract", all(any(obj.name == f"INT_CORE_{floor_id}_StairA_Door" for obj in core_objects)
          and any(obj.name == f"INT_CORE_{floor_id}_StairB_Door" for obj in core_objects) for floor_id in FLOORS),
          "A playable, B locked door leaves")

    uv_ok = all(obj.data.uv_layers and len(obj.data.uv_layers[0].data) == len(obj.data.loops) for obj in all_export_objects)
    check("uv0_complete", uv_ok, f"mesh_objects={len(all_export_objects)}")
    normals_ok = all(all(polygon.area > 1e-8 for polygon in obj.data.polygons) for obj in all_export_objects)
    check("nondegenerate_geometry", normals_ok, f"mesh_objects={len(all_export_objects)}")
    mesh_use = Counter(obj.data.name for obj in all_export_objects)
    reused = sum(1 for count in mesh_use.values() if count >= 7)
    check("reusable_module_instances", reused >= 12, f"mesh_datablocks_reused_7plus={reused}")
    check("no_live_modifiers", all(len(obj.modifiers) == 0 for obj in all_export_objects),
          f"mesh_objects={len(all_export_objects)}")

    expected_reviews = [REVIEW_DIR / f"{index:02d}_{floor_id}_AnnotatedPlan.png" for index, floor_id in enumerate(FLOORS, 1)] + [
        REVIEW_DIR / name for name in (
            "08_AllFloors_LongitudinalSection.png", "09_Core_ExteriorAlignment.png",
            "10_F00_OpenLobbyAlignment.png", "11_F00_OpenLobbyWide.png", "12_F00_ElevatorBank.png",
            "13_E01_Cabin.png", "14_F02_ElevatorArrival.png", "15_F00_StairA.png",
            "16_F01_StairLanding.png", "17_F00_FloorNumber.png", "18_F00_ClearLobbyBoundary.png",
            "19_F00_StairB.png", "20_F06_FloorNumber.png")]
    check("review_evidence_complete", all(path.exists() and path.stat().st_size > 10000 for path in expected_reviews),
          f"expected={len(expected_reviews)}")

    datum = {
        "schema": "HospitalInterior.I0.R02.DatumManifest.v1",
        "units": "meters", "axis": {"blender_up": "+Z", "fbx_forward": "-Z", "fbx_up": "+Y"},
        "floors": [{"floor": floor_id, "z_m": z} for floor_id, z in FLOORS.items()],
        "core_bounds_xy_m": {"Elevator": ELEVATOR, "StairA": STAIR_A, "StairB": STAIR_B},
        "unchanged_from_r01": True,
    }
    (REVIEW_DIR / "StageI0_R02_DatumManifest.json").write_text(json.dumps(datum, indent=2), encoding="utf-8")
    art = {
        "schema": "HospitalInterior.OpenLobby.R02.ArtInventory.v2",
        "palette_source": "Approved R40 exterior entrance and player-eye language",
        "materials": material_names,
        "floor_report": floor_report,
        "core": {"mesh_objects": len(core_meshes), "triangles": core_triangles},
        "design_contract": "The same clean open lobby is repeated on F00-F06; the wall-mounted floor number is the only visual floor variation.",
        "allowed_content": ["open floor", "perimeter shell", "ceiling", "lighting", "elevator core", "stair core", "floor number"],
    }
    (REVIEW_DIR / "StageI0I1_R02_ArtInventory.json").write_text(json.dumps(art, indent=2), encoding="utf-8")
    report = {
        "schema": "HospitalInterior.OpenLobby.R02.BlenderGate.v2",
        "status": "PASS" if all(item["pass"] for item in checks) else "FAIL",
        "passCount": sum(1 for item in checks if item["pass"]),
        "failureCount": sum(1 for item in checks if not item["pass"]),
        "checks": checks,
    }
    (REVIEW_DIR / "StageI0I1_R02_BlenderGate.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"HOSPITAL_INTERIOR_R02_BLENDER_GATE={report['status']}; CHECKS={len(checks)}; FAILURES={report['failureCount']}")
    if report["status"] != "PASS":
        raise SystemExit(1)


if __name__ == "__main__":
    main()
