"""Independent saved-file validator for Hospital Interior R03 S3A."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parents[2]
SOURCE_PATH = ROOT / "ArtSource" / "Environment" / "Blender" / "HospitalInterior" / "HospitalInterior_E01_Cabin_R03.blend"
EXPORT_DIR = ROOT / "Exports" / "HospitalInterior" / "StageI1_R03_S3A"
REVIEW_DIR = ROOT / "Reviews" / "HospitalInterior" / "StageI1_R03" / "S3A_E01_Design"
REPORT_PATH = REVIEW_DIR / "StageI1_R03_S3A_Validation.json"
S1_DATUM_REPORT = ROOT / "Reviews" / "HospitalInterior" / "StageI1_R03" / "StageI1_R03_S1_DatumReport.json"

EXPECTED_RENDERS = [
    "01_S3A_E01_Landing_DoorsClosed.png",
    "02_S3A_E01_Landing_DoorsOpen.png",
    "03_S3A_E01_CabinInterior.png",
    "04_S3A_E01_ControlPanelClose.png",
    "05_S3A_E01_StretcherClearance.png",
    "06_S3A_E01_MaterialsAndCeilingLight.png",
]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


class Gate:
    def __init__(self) -> None:
        self.checks: list[dict] = []

    def add(self, name: str, passed: bool, detail: str) -> None:
        self.checks.append({"name": name, "pass": bool(passed), "detail": detail})

    def finish(self) -> None:
        REVIEW_DIR.mkdir(parents=True, exist_ok=True)
        passed = sum(1 for check in self.checks if check["pass"])
        report = {
            "schema": "HospitalInterior.R03.S3A.Validation.v1",
            "status": "PASS" if passed == len(self.checks) else "FAIL",
            "blenderVersion": bpy.app.version_string,
            "passCount": passed,
            "totalCount": len(self.checks),
            "sourceSha256": sha256(SOURCE_PATH) if SOURCE_PATH.exists() else "",
            "checks": self.checks,
        }
        REPORT_PATH.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(
            f"HOSPITAL_INTERIOR_R03_S3A_VALIDATION={report['status']}; "
            f"{passed}/{len(self.checks)}; {REPORT_PATH}"
        )
        if report["status"] != "PASS":
            raise SystemExit(1)


def object_named(name: str):
    return bpy.data.objects.get(name)


def approximately(value: float, expected: float, tolerance: float = 0.005) -> bool:
    return abs(float(value) - expected) <= tolerance


def main() -> None:
    gate = Gate()
    scene = bpy.context.scene
    gate.add("source_path", SOURCE_PATH.exists(), str(SOURCE_PATH))
    gate.add(
        "metric_identity_scene",
        scene.unit_settings.system == "METRIC"
        and approximately(scene.unit_settings.scale_length, 1.0)
        and scene.get("revision") == "R03"
        and scene.get("stage") == "S3A",
        f"system={scene.unit_settings.system}; scale={scene.unit_settings.scale_length}; revision={scene.get('revision')}; stage={scene.get('stage')}",
    )
    gate.add(
        "approval_boundary",
        scene.get("status") == "DESIGN_CANDIDATE_AWAITING_USER_APPROVAL"
        and scene.get("runtime_integration_authorized") is False,
        f"status={scene.get('status')}; runtimeAuthorized={scene.get('runtime_integration_authorized')}",
    )
    gate.add(
        "no_legacy_geometry",
        scene.get("legacy_geometry_reused") is False
        and all("R01" not in obj.name and "R02" not in obj.name for obj in bpy.data.objects)
        and not list(bpy.data.libraries),
        f"libraries={len(bpy.data.libraries)}; objects={len(bpy.data.objects)}",
    )

    cabin = object_named("S3A_R03_E01_CabinRoot")
    landing = object_named("S3A_R03_E01_LandingPortalRoot")
    gate.add(
        "separate_cabin_and_landing_roots",
        cabin is not None and landing is not None and cabin.parent is None and landing.parent is None,
        f"cabin={cabin is not None}; landing={landing is not None}",
    )
    if cabin is not None:
        dims_ok = all(
            (
                approximately(cabin.get("outer_width_m"), 2.35),
                approximately(cabin.get("outer_depth_m"), 2.45),
                approximately(cabin.get("outer_height_m"), 2.60),
                approximately(cabin.get("inner_width_m"), 2.10),
                approximately(cabin.get("inner_depth_m"), 2.20),
                approximately(cabin.get("door_clear_width_m"), 1.20),
                approximately(cabin.get("door_clear_height_m"), 2.20),
            )
        )
        gate.add("stretcher_cabin_dimensions", dims_ok, str(dict(cabin.items())))

    s1_datum = json.loads(S1_DATUM_REPORT.read_text(encoding="utf-8")) if S1_DATUM_REPORT.exists() else {}
    measurements = s1_datum.get("measurements", s1_datum)
    core_min_x = float(measurements.get("elevatorMinX", -999.0))
    core_max_x = float(measurements.get("elevatorMaxX", -999.0))
    core_min_z = float(measurements.get("elevatorMinZ", -999.0))
    core_max_z = float(measurements.get("elevatorMaxZ", -999.0))
    e01_max_x = (core_min_x + core_max_x) * 0.5
    cabin_min_x, cabin_max_x = 1.65 - 2.35 * 0.5, 1.65 + 2.35 * 0.5
    cabin_min_z, cabin_max_z = 4.35 - 2.45 * 0.5, 4.35 + 2.45 * 0.5
    core_fit = (
        S1_DATUM_REPORT.exists()
        and measurements.get("elevatorContainedByUpperEnvelope") is True
        and approximately(core_min_x, 0.0)
        and approximately(core_max_x, 6.0)
        and approximately(core_min_z, 3.0)
        and approximately(core_max_z, 5.7)
        and cabin_min_x >= core_min_x
        and cabin_max_x <= e01_max_x
        and cabin_min_z >= core_min_z
        and cabin_max_z <= core_max_z
    )
    gate.add(
        "approved_s1_left_core_fit",
        core_fit,
        (
            f"approvedCoreX={core_min_x:.3f}..{core_max_x:.3f}; "
            f"approvedCoreZ={core_min_z:.3f}..{core_max_z:.3f}; "
            f"E01LeftCellX={core_min_x:.3f}..{e01_max_x:.3f}; "
            f"cabinX={cabin_min_x:.3f}..{cabin_max_x:.3f}; "
            f"cabinZ={cabin_min_z:.3f}..{cabin_max_z:.3f}"
        ),
    )

    floor = object_named("S3A_R03_E01_CabinFloor")
    threshold = object_named("S3A_R03_E01_LandingThreshold")
    gate.add(
        "walkable_flush_floor_and_threshold",
        floor is not None and floor.get("walkable") is True and threshold is not None and threshold.get("flush_threshold") is True,
        f"floor={floor is not None}; threshold={threshold is not None}",
    )
    cabin_door_left = object_named("S3A_R03_E01_CabinDoor_L")
    landing_door_left = object_named("S3A_R03_E01_LandingDoor_L")
    landing_frame_header = object_named("S3A_R03_E01_LandingFrame_Header")
    floor_top = max((floor.matrix_world @ Vector(corner)).z for corner in floor.bound_box) if floor else -999.0
    threshold_top = max((threshold.matrix_world @ Vector(corner)).z for corner in threshold.bound_box) if threshold else -999.0
    cabin_door_bottom = min((cabin_door_left.matrix_world @ Vector(corner)).z for corner in cabin_door_left.bound_box) if cabin_door_left else -999.0
    landing_door_bottom = min((landing_door_left.matrix_world @ Vector(corner)).z for corner in landing_door_left.bound_box) if landing_door_left else -999.0
    landing_header_bottom = min((landing_frame_header.matrix_world @ Vector(corner)).z for corner in landing_frame_header.bound_box) if landing_frame_header else -999.0
    gate.add(
        "exact_floor_threshold_door_datum",
        approximately(floor_top, 0.11)
        and approximately(threshold_top, floor_top)
        and approximately(cabin_door_bottom, floor_top)
        and approximately(landing_door_bottom, floor_top)
        and approximately(landing_header_bottom - floor_top, 2.20),
        (
            f"floorTop={floor_top:.3f}; thresholdTop={threshold_top:.3f}; "
            f"cabinDoorBottom={cabin_door_bottom:.3f}; landingDoorBottom={landing_door_bottom:.3f}; "
            f"landingClearHeight={landing_header_bottom - floor_top:.3f}"
        ),
    )

    required_parts = [
        "S3A_R03_E01_CabinFloor",
        "S3A_R03_E01_CabinCeiling",
        "S3A_R03_E01_RearWall",
        "S3A_R03_E01_RearProtectionPanel",
        "S3A_R03_E01_RearHandrail",
        "S3A_R03_E01_CeilingDiffuser",
        "S3A_R03_E01_ControlPanel",
        "S3A_R03_E01_CabinFloorDisplay",
        "S3A_R03_E01_LandingIndicator",
        "S3A_R03_E01_LandingCallPanel",
        "S3A_R03_E01_LandingThreshold",
    ]
    missing_parts = [name for name in required_parts if object_named(name) is None]
    gate.add("required_design_parts", not missing_parts, "missing=" + ",".join(missing_parts))

    cabin_doors = [object_named(f"S3A_R03_E01_CabinDoor_{side}") for side in ("L", "R")]
    landing_doors = [object_named(f"S3A_R03_E01_LandingDoor_{side}") for side in ("L", "R")]
    all_doors = cabin_doors + landing_doors
    door_contract = all(door is not None for door in all_doors) and all(
        door.get("door_role") in ("cabin", "landing")
        and door.get("door_side") in ("L", "R")
        and isinstance(door.get("closed_x"), float)
        and isinstance(door.get("open_x"), float)
        and abs(float(door.get("open_x")) - float(door.get("closed_x"))) >= 0.59
        for door in all_doors
    )
    gate.add("four_independent_center_opening_leaves", door_contract, ",".join(door.name for door in all_doors if door))
    edge_materials = all(
        door is not None
        and {material.name for material in door.data.materials if material is not None}
        >= {"MAT_S3A_R03_BrushedStainless", "MAT_S3A_R03_RefinedBronze"}
        for door in all_doors
    )
    gate.add(
        "door_edge_details_baked_into_moving_leaves",
        edge_materials,
        ",".join(f"{door.name}:{len(door.data.materials)}materials" for door in all_doors if door is not None),
    )

    controls = [obj for obj in bpy.data.objects if obj.name.startswith("S3A_R03_E01_ControlButton_")]
    labels = [obj for obj in bpy.data.objects if obj.name.startswith("S3A_R03_E01_ControlLabel_")]
    gate.add("nine_legible_control_commands", len(controls) == 9 and len(labels) == 9, f"buttons={len(controls)}; labels={len(labels)}")

    practical_lights = [obj for obj in bpy.data.objects if obj.name == "S3A_R03_E01_CabinPracticalLight" and obj.type == "LIGHT"]
    gate.add("single_cabin_practical_light", len(practical_lights) == 1, f"count={len(practical_lights)}")

    required_materials = {
        "MAT_S3A_R03_HygienicWarmOffWhite",
        "MAT_S3A_R03_BrushedStainless",
        "MAT_S3A_R03_CharcoalFrame",
        "MAT_S3A_R03_RefinedBronze",
        "MAT_S3A_R03_DarkTerrazzo",
        "MAT_S3A_R03_Diffuser3500K",
    }
    material_names = set(bpy.data.materials.keys())
    gate.add("premium_clinical_material_roles", required_materials.issubset(material_names), "missing=" + ",".join(sorted(required_materials - material_names)))

    source_meshes = [obj for obj in bpy.data.objects if obj.type == "MESH" and not obj.name.startswith("S3A_R03_REVIEW_")]
    meshes_with_uvs = [obj for obj in source_meshes if obj.data.uv_layers]
    meshes_with_material = [obj for obj in source_meshes if obj.data.materials and obj.data.materials[0] is not None]
    gate.add("source_mesh_uvs", len(meshes_with_uvs) == len(source_meshes), f"uv={len(meshes_with_uvs)}; meshes={len(source_meshes)}")
    gate.add("source_mesh_materials", len(meshes_with_material) == len(source_meshes), f"materials={len(meshes_with_material)}; meshes={len(source_meshes)}")

    stretcher = object_named("S3A_R03_REVIEW_StretcherProxyRoot")
    gate.add(
        "stretcher_clearance_proxy",
        stretcher is not None and approximately(stretcher.get("proxy_width_m"), 0.80) and approximately(stretcher.get("proxy_length_m"), 2.10),
        str(dict(stretcher.items())) if stretcher else "missing",
    )

    fbx_paths = [
        EXPORT_DIR / "HospitalInterior_E01_Cabin_R03.fbx",
        EXPORT_DIR / "HospitalInterior_E01_LandingPortal_R03.fbx",
    ]
    gate.add(
        "preintegration_fbx_exports",
        all(path.exists() and path.stat().st_size > 1024 for path in fbx_paths),
        ",".join(f"{path.name}:{path.stat().st_size if path.exists() else 0}" for path in fbx_paths),
    )
    render_paths = [REVIEW_DIR / name for name in EXPECTED_RENDERS]
    gate.add(
        "six_design_review_renders",
        all(path.exists() and path.stat().st_size > 50000 for path in render_paths),
        ",".join(f"{path.name}:{path.stat().st_size if path.exists() else 0}" for path in render_paths),
    )

    gate.add(
        "saved_file_has_no_review_camera",
        len(bpy.data.cameras) == 0,
        f"cameras={len(bpy.data.cameras)}",
    )
    gate.finish()


if __name__ == "__main__":
    main()
