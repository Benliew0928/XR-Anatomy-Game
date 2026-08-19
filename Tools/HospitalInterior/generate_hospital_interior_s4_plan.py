"""Generate the S4 F00 planning base from the approved R40 Blender authority.

Run with Blender 5.1.2 in background mode after opening the R40 .blend. The
script is read-only with respect to the .blend and produces review-only JSON
and SVG evidence. It does not create or modify Unity geometry.
"""

from __future__ import annotations

import hashlib
import html
import json
import math
from pathlib import Path

import bpy


WORKSPACE = Path(r"C:\CutMyBodyPlease")
OUTPUT_DIR = WORKSPACE / "Reviews" / "HospitalInterior" / "StageI1_R03" / "S4_F00_Plan"
AUTHORITY = (
    WORKSPACE
    / "ArtSource"
    / "Environment"
    / "Blender"
    / "HospitalExterior"
    / "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"
)
CROSS_SECTION_Z_M = 1.20

FOOTPRINT = [
    (-35.05, -22.10),
    (36.00, -22.10),
    (36.00, 12.20),
    (11.50, 12.20),
    (11.50, 16.35),
    (-27.50, 16.35),
    (-27.50, 12.20),
    (-35.05, 12.20),
]

PLAN_CONTRACT = OUTPUT_DIR / "StageI1_R03_S4_F00_PlanContract.json"
SVG_OUTPUT = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan.svg"
PLAN_V2_CONTRACT = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V02_Contract.json"
SVG_V2_OUTPUT = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V02.svg"
PLAN_V3_CONTRACT = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V03_Contract.json"
SVG_V3_OUTPUT = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V03.svg"


GROUP_STYLE = {
    "arrival": ("#166f8f", "#d9f2fb"),
    "learning": ("#6d4ba8", "#eee6fa"),
    "circulation": ("#b36b00", "#fff0d5"),
    "support": ("#39734a", "#e1f2e5"),
}


def polygon_area(points: list[list[float]] | list[tuple[float, float]]) -> float:
    return abs(
        sum(
            points[index][0] * points[(index + 1) % len(points)][1]
            - points[(index + 1) % len(points)][0] * points[index][1]
            for index in range(len(points))
        )
        / 2.0
    )


def polygon_centroid(points: list[list[float]]) -> tuple[float, float]:
    signed_double_area = 0.0
    x_sum = 0.0
    y_sum = 0.0
    for index, point in enumerate(points):
        nxt = points[(index + 1) % len(points)]
        cross = point[0] * nxt[1] - nxt[0] * point[1]
        signed_double_area += cross
        x_sum += (point[0] + nxt[0]) * cross
        y_sum += (point[1] + nxt[1]) * cross
    if abs(signed_double_area) < 1.0e-8:
        return (
            sum(point[0] for point in points) / len(points),
            sum(point[1] for point in points) / len(points),
        )
    return (
        x_sum / (3.0 * signed_double_area),
        y_sum / (3.0 * signed_double_area),
    )


def wrap_words(text: str, limit: int) -> list[str]:
    lines: list[str] = []
    current = ""
    for word in text.split():
        candidate = word if not current else f"{current} {word}"
        if len(candidate) <= limit:
            current = candidate
        else:
            if current:
                lines.append(current)
            current = word
    if current:
        lines.append(current)
    return lines


def point_on_segment(point, start, end, tolerance: float = 1.0e-6) -> bool:
    px, py = point
    ax, ay = start
    bx, by = end
    cross = (px - ax) * (by - ay) - (py - ay) * (bx - ax)
    if abs(cross) > tolerance:
        return False
    return (
        min(ax, bx) - tolerance <= px <= max(ax, bx) + tolerance
        and min(ay, by) - tolerance <= py <= max(ay, by) + tolerance
    )


def point_in_polygon(point, polygon, include_boundary: bool = True) -> bool:
    for index, start in enumerate(polygon):
        if point_on_segment(point, start, polygon[(index + 1) % len(polygon)]):
            return include_boundary
    inside = False
    x, y = point
    for index, start in enumerate(polygon):
        end = polygon[(index + 1) % len(polygon)]
        if (start[1] > y) == (end[1] > y):
            continue
        crossing_x = (end[0] - start[0]) * (y - start[1]) / (end[1] - start[1]) + start[0]
        if x < crossing_x:
            inside = not inside
    return inside


def validate_plan(contract: dict, segments: list[dict]) -> dict:
    footprint = contract["lockedConstraints"]["f00Footprint"]
    footprint_area = polygon_area(footprint)
    zone_area = sum(polygon_area(zone["polygon"]) for zone in contract["zones"])
    zone_vertices_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for zone in contract["zones"]
        for point in zone["polygon"]
    )

    sample_step = 0.25
    sample_count = 0
    gap_count = 0
    overlap_count = 0
    x = -35.05 + sample_step / 2
    while x < 36.0:
        z = -22.10 + sample_step / 2
        while z < 16.35:
            if point_in_polygon((x, z), footprint, include_boundary=False):
                sample_count += 1
                membership = sum(
                    1
                    for zone in contract["zones"]
                    if point_in_polygon((x, z), zone["polygon"], include_boundary=False)
                )
                if membership == 0:
                    gap_count += 1
                elif membership > 1:
                    overlap_count += 1
            z += sample_step
        x += sample_step

    e01 = contract["lockedConstraints"]["e01Aperture"]
    e01_corners = [
        (e01["minX"], e01["minZ"]),
        (e01["maxX"], e01["minZ"]),
        (e01["maxX"], e01["maxZ"]),
        (e01["minX"], e01["maxZ"]),
    ]
    e01_zone = next(zone for zone in contract["zones"] if zone["id"] == "Z07")
    e01_contained = all(point_in_polygon(point, e01_zone["polygon"]) for point in e01_corners)

    route_points_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for route in contract["circulation"]
        for point in route["points"]
    )

    checks = [
        {
            "name": "approved_authority_cross_section_present",
            "pass": len(segments) > 0,
            "detail": f"segments={len(segments)} at {CROSS_SECTION_Z_M:.2f} m",
        },
        {
            "name": "zone_area_matches_complete_f00_footprint",
            "pass": abs(zone_area - footprint_area) <= 0.01,
            "detail": f"zones={zone_area:.3f} m2; footprint={footprint_area:.3f} m2",
        },
        {
            "name": "all_zone_vertices_inside_locked_f00_footprint",
            "pass": zone_vertices_inside,
            "detail": f"zones={len(contract['zones'])}",
        },
        {
            "name": "sampled_zone_coverage_has_no_gaps_or_overlaps",
            "pass": gap_count == 0 and overlap_count == 0,
            "detail": f"samples={sample_count}; gaps={gap_count}; overlaps={overlap_count}; step={sample_step:.2f} m",
        },
        {
            "name": "locked_e01_is_contained_by_wayfinding_hub",
            "pass": e01_contained,
            "detail": "E01 aperture 0.30..3.00 X, 3.00..5.70 Z inside Z07",
        },
        {
            "name": "all_planned_route_points_stay_inside_f00",
            "pass": route_points_inside,
            "detail": f"routes={len(contract['circulation'])}",
        },
        {
            "name": "s4_contains_no_unity_geometry",
            "pass": not any(OUTPUT_DIR.rglob("*.unity")) and not any(OUTPUT_DIR.rglob("*.fbx")),
            "detail": "review output contains plan/evidence only; no .unity or .fbx",
        },
    ]
    return {
        "schema": "HospitalInterior.R03.S4.PlanGate.v1",
        "status": "PASS" if all(check["pass"] for check in checks) else "FAIL",
        "approval": "PENDING_USER_APPROVAL",
        "passCount": sum(1 for check in checks if check["pass"]),
        "totalCount": len(checks),
        "footprintAreaM2": round(footprint_area, 3),
        "zoneAreaM2": round(zone_area, 3),
        "checks": checks,
    }


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def category_for(name: str) -> str:
    if "DOOR_" in name or "SlidingLeaf" in name:
        return "existing_door"
    if "GLASS" in name:
        return "existing_glazing"
    if "FIXTURE" in name:
        return "existing_fixture"
    return "existing_opaque"


def cross_section_segments() -> list[dict]:
    segments: list[dict] = []
    seen: set[tuple] = set()

    for obj in bpy.data.objects:
        if obj.type != "MESH" or not obj.name.startswith(("UE_EXT_", "UE_S03")):
            continue

        world = obj.matrix_world
        vertices = [world @ vertex.co for vertex in obj.data.vertices]
        for polygon in obj.data.polygons:
            points: list[tuple[float, float]] = []
            vertex_ids = list(polygon.vertices)
            for index, start_id in enumerate(vertex_ids):
                end_id = vertex_ids[(index + 1) % len(vertex_ids)]
                start = vertices[start_id]
                end = vertices[end_id]
                start_delta = start.z - CROSS_SECTION_Z_M
                end_delta = end.z - CROSS_SECTION_Z_M
                if start_delta * end_delta >= -1.0e-10:
                    continue
                t = start_delta / (start_delta - end_delta)
                points.append(
                    (
                        start.x + (end.x - start.x) * t,
                        start.y + (end.y - start.y) * t,
                    )
                )

            if len(points) < 2:
                continue

            start = points[0]
            end = points[1]
            length = math.dist(start, end)
            if length < 0.04:
                continue

            ordered = sorted((start, end))
            key = (
                obj.name,
                round(ordered[0][0], 3),
                round(ordered[0][1], 3),
                round(ordered[1][0], 3),
                round(ordered[1][1], 3),
            )
            if key in seen:
                continue
            seen.add(key)
            segments.append(
                {
                    "object": obj.name,
                    "category": category_for(obj.name),
                    "start": [round(start[0], 4), round(start[1], 4)],
                    "end": [round(end[0], 4), round(end[1], 4)],
                    "lengthM": round(length, 4),
                }
            )

    return sorted(
        segments,
        key=lambda item: (
            item["category"],
            item["object"],
            item["start"][0],
            item["start"][1],
        ),
    )


def build_svg(segments: list[dict], contract: dict) -> str:
    width = 1900
    height = 1240
    plan_left = 74.0
    plan_top = 176.0
    min_x, max_x = -38.0, 38.0
    min_z, max_z = -25.0, 19.0
    plan_width = 1240.0
    plan_height = 718.0
    scale = min(plan_width / (max_x - min_x), plan_height / (max_z - min_z))

    def tx(x: float) -> float:
        return plan_left + (x - min_x) * scale

    def ty(z: float) -> float:
        return plan_top + (max_z - z) * scale

    def points_attr(points) -> str:
        return " ".join(f"{tx(point[0]):.1f},{ty(point[1]):.1f}" for point in points)

    parts: list[str] = [
        '<svg xmlns="http://www.w3.org/2000/svg" width="1900" height="1240" viewBox="0 0 1900 1240">',
        "<defs>",
        '<marker id="arrowPrimary" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#1676a7"/></marker>',
        '<marker id="arrowLoop" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#c46f00"/></marker>',
        '<style>text{font-family:Arial,Helvetica,sans-serif;fill:#17222c}.title{font-size:30px;font-weight:700}.subtitle{font-size:16px;fill:#52616e}.section{font-size:18px;font-weight:700}.zone-id{font-size:15px;font-weight:700}.zone-name{font-size:14px;font-weight:700}.small{font-size:12px;fill:#52616e}.schedule-name{font-size:13px;font-weight:700}.schedule-purpose{font-size:11px;fill:#45545f}.grid{stroke:#d9e0e5;stroke-width:.7}.outline{fill:none;stroke:#111b23;stroke-width:3}.opaque{stroke:#26343e;stroke-width:1.05;opacity:.58}.glass{stroke:#187b99;stroke-width:1.15;opacity:.78}.fixture{stroke:#756a60;stroke-width:.7;opacity:.45}.door{stroke:#14733a;stroke-width:1.6;opacity:.9}.route-primary{fill:none;stroke:#1676a7;stroke-width:5;stroke-linecap:round;stroke-linejoin:round;stroke-dasharray:11 7}.route-loop{fill:none;stroke:#c46f00;stroke-width:3.5;stroke-linecap:round;stroke-linejoin:round;stroke-dasharray:8 6}.route-service{fill:none;stroke:#39734a;stroke-width:3;stroke-linecap:round;stroke-dasharray:4 6}.e01{fill:#ffe2e2;stroke:#b42318;stroke-width:2.3}.locked{font-size:11px;font-weight:700;fill:#8d1d14}</style>',
        "</defs>",
        '<rect width="1900" height="1240" fill="#ffffff"/>',
        '<text x="74" y="54" class="title">F00 Education-First Floor Plan — S4 Design Candidate</text>',
        '<text x="74" y="83" class="subtitle">Plan-only review • locked R40/S3D envelope, entrance and E01 • north/up = +Z • dimensions in metres</text>',
        '<rect x="74" y="104" width="1240" height="42" rx="6" fill="#fff4d6" stroke="#d39a1f"/>',
        '<text x="94" y="131" font-size="15" font-weight="700" fill="#684500">AWAITING USER APPROVAL — no S5 walls, doors, counters, furniture or Unity greybox geometry created</text>',
    ]

    for x in range(-35, 37, 5):
        parts.append(
            f'<line x1="{tx(x):.1f}" y1="{ty(-23):.1f}" x2="{tx(x):.1f}" y2="{ty(17):.1f}" class="grid"/>'
        )
        parts.append(
            f'<text x="{tx(x):.1f}" y="{ty(-23)-5:.1f}" text-anchor="middle" class="small">{x}</text>'
        )
    for z in range(-20, 17, 5):
        parts.append(
            f'<line x1="{tx(-36):.1f}" y1="{ty(z):.1f}" x2="{tx(36):.1f}" y2="{ty(z):.1f}" class="grid"/>'
        )
        parts.append(
            f'<text x="{tx(-36)-8:.1f}" y="{ty(z)+4:.1f}" text-anchor="end" class="small">{z}</text>'
        )

    for zone in contract["zones"]:
        stroke, fill = GROUP_STYLE[zone["group"]]
        parts.append(
            f'<polygon points="{points_attr(zone["polygon"])}" fill="{fill}" fill-opacity="0.78" stroke="{stroke}" stroke-width="1.5"/>'
        )

    class_by_category = {
        "existing_opaque": "opaque",
        "existing_glazing": "glass",
        "existing_fixture": "fixture",
        "existing_door": "door",
    }
    for segment in segments:
        start = segment["start"]
        end = segment["end"]
        css_class = class_by_category[segment["category"]]
        parts.append(
            f'<line x1="{tx(start[0]):.1f}" y1="{ty(start[1]):.1f}" x2="{tx(end[0]):.1f}" y2="{ty(end[1]):.1f}" class="{css_class}"/>'
        )

    parts.append(f'<polygon points="{points_attr(FOOTPRINT)}" class="outline"/>')

    e01 = contract["lockedConstraints"]["e01Aperture"]
    parts.append(
        f'<rect x="{tx(e01["minX"]):.1f}" y="{ty(e01["maxZ"]):.1f}" width="{(e01["maxX"]-e01["minX"])*scale:.1f}" height="{(e01["maxZ"]-e01["minZ"])*scale:.1f}" class="e01"/>'
    )
    parts.append(
        f'<text x="{tx((e01["minX"]+e01["maxX"])/2):.1f}" y="{ty((e01["minZ"]+e01["maxZ"])/2)+4:.1f}" text-anchor="middle" class="locked">E01</text>'
    )

    route_styles = {
        "R01": ("route-primary", "url(#arrowPrimary)"),
        "R02": ("route-loop", "url(#arrowLoop)"),
        "R03": ("route-service", ""),
    }
    for route in contract["circulation"]:
        css_class, marker = route_styles[route["id"]]
        marker_attr = f' marker-end="{marker}"' if marker else ""
        path = " ".join(
            ("M" if index == 0 else "L") + f" {tx(point[0]):.1f} {ty(point[1]):.1f}"
            for index, point in enumerate(route["points"])
        )
        parts.append(f'<path d="{path}" class="{css_class}"{marker_attr}/>')

    for zone in contract["zones"]:
        centre_x, centre_z = polygon_centroid(zone["polygon"])
        parts.append(
            f'<circle cx="{tx(centre_x):.1f}" cy="{ty(centre_z):.1f}" r="16" fill="#ffffff" stroke="#26343e" stroke-width="1.3"/>'
        )
        parts.append(
            f'<text x="{tx(centre_x):.1f}" y="{ty(centre_z)+5:.1f}" text-anchor="middle" class="zone-id">{zone["id"]}</text>'
        )

    parts.extend(
        [
            f'<text x="{tx(-4):.1f}" y="{ty(-23.6):.1f}" text-anchor="middle" class="zone-name">MAIN ENTRANCE</text>',
            f'<path d="M {tx(34):.1f} {ty(15):.1f} L {tx(34):.1f} {ty(18):.1f}" stroke="#17222c" stroke-width="2.5" marker-end="url(#arrowPrimary)"/>',
            f'<text x="{tx(34):.1f}" y="{ty(14.3):.1f}" text-anchor="middle" class="zone-name">N / +Z</text>',
            f'<line x1="{tx(-34):.1f}" y1="{ty(-24):.1f}" x2="{tx(-24):.1f}" y2="{ty(-24):.1f}" stroke="#17222c" stroke-width="4"/>',
            f'<line x1="{tx(-34):.1f}" y1="{ty(-24)-6:.1f}" x2="{tx(-34):.1f}" y2="{ty(-24)+6:.1f}" stroke="#17222c" stroke-width="2"/>',
            f'<line x1="{tx(-24):.1f}" y1="{ty(-24)-6:.1f}" x2="{tx(-24):.1f}" y2="{ty(-24)+6:.1f}" stroke="#17222c" stroke-width="2"/>',
            f'<text x="{tx(-29):.1f}" y="{ty(-24)+20:.1f}" text-anchor="middle" class="small">10 m</text>',
            '<text x="1360" y="118" class="section">Zone schedule</text>',
            '<line x1="1360" y1="132" x2="1840" y2="132" stroke="#ccd5dc"/>',
        ]
    )

    schedule_y = 158.0
    for zone in contract["zones"]:
        stroke, fill = GROUP_STYLE[zone["group"]]
        purpose_lines = wrap_words(zone["purpose"], 66)[:2]
        area = polygon_area(zone["polygon"])
        parts.append(
            f'<rect x="1360" y="{schedule_y-15:.1f}" width="34" height="22" rx="4" fill="{fill}" stroke="{stroke}"/>'
        )
        parts.append(
            f'<text x="1377" y="{schedule_y+1:.1f}" text-anchor="middle" class="zone-id">{zone["id"]}</text>'
        )
        parts.append(
            f'<text x="1406" y="{schedule_y:.1f}" class="schedule-name">{html.escape(zone["name"])} · {area:.0f} m²</text>'
        )
        for line_index, line in enumerate(purpose_lines):
            parts.append(
                f'<text x="1406" y="{schedule_y+17+line_index*14:.1f}" class="schedule-purpose">{html.escape(line)}</text>'
            )
        schedule_y += 59.0

    parts.extend(
        [
            '<text x="74" y="952" class="section">Circulation and fixed elements</text>',
            '<line x1="74" y1="969" x2="1314" y2="969" stroke="#ccd5dc"/>',
            '<line x1="82" y1="1000" x2="140" y2="1000" class="route-primary"/><text x="155" y="1005" class="zone-name">R01 primary accessible route · 2.40 m target</text>',
            '<line x1="505" y1="1000" x2="563" y2="1000" class="route-loop"/><text x="578" y="1005" class="zone-name">R02 learning loop · 1.80 m target</text>',
            '<line x1="892" y1="1000" x2="950" y2="1000" class="route-service"/><text x="965" y="1005" class="zone-name">R03 rear service route · 1.80 m target</text>',
            '<rect x="82" y="1031" width="30" height="20" class="e01"/><text x="124" y="1047" class="zone-name">Locked E01 aperture and landing</text>',
            '<line x1="365" y1="1041" x2="423" y2="1041" class="opaque"/><text x="438" y="1047" class="zone-name">Existing opaque architecture</text>',
            '<line x1="682" y1="1041" x2="740" y2="1041" class="glass"/><text x="755" y="1047" class="zone-name">Existing glazing</text>',
            '<line x1="925" y1="1041" x2="983" y2="1041" class="door"/><text x="998" y="1047" class="zone-name">Existing door leaves</text>',
            '<text x="74" y="1105" class="section">S4 gate</text>',
            '<text x="74" y="1134" class="subtitle">Approve this labelled zoning and circulation plan, or request corrections. Approval authorizes S5 greybox only; it does not approve finishes or upper floors.</text>',
            '<text x="74" y="1187" class="small">Source: exact R40 mesh cross-section at 1.20 m + locked S2/S3D contracts • Generated by Tools/HospitalInterior/generate_hospital_interior_s4_plan.py</text>',
            "</svg>",
        ]
    )
    return "\n".join(parts)


def v2_anchor_test_points(anchor: dict) -> list[tuple[float, float]]:
    shape = anchor["shape"]
    if shape == "rect":
        bounds = anchor["bounds"]
        return [
            (bounds["minX"], bounds["minZ"]),
            (bounds["maxX"], bounds["minZ"]),
            (bounds["maxX"], bounds["maxZ"]),
            (bounds["minX"], bounds["maxZ"]),
        ]
    if shape == "multi_rect":
        points: list[tuple[float, float]] = []
        for bounds in anchor["bounds"]:
            points.extend(
                [
                    (bounds["minX"], bounds["minZ"]),
                    (bounds["maxX"], bounds["minZ"]),
                    (bounds["maxX"], bounds["maxZ"]),
                    (bounds["minX"], bounds["maxZ"]),
                ]
            )
        return points
    if shape == "circle":
        centre_x, centre_z = anchor["centre"]
        radius = anchor["radiusM"]
        return [
            (centre_x - radius, centre_z),
            (centre_x + radius, centre_z),
            (centre_x, centre_z - radius),
            (centre_x, centre_z + radius),
        ]
    if shape == "multi_circle":
        points = []
        radius = anchor["radiusM"]
        for centre_x, centre_z in anchor["centres"]:
            points.extend(
                [
                    (centre_x - radius, centre_z),
                    (centre_x + radius, centre_z),
                    (centre_x, centre_z - radius),
                    (centre_x, centre_z + radius),
                ]
            )
        return points
    raise ValueError(f"Unsupported V02 anchor shape: {shape}")


def validate_v2_plan(contract: dict, segments: list[dict]) -> dict:
    footprint = contract["lockedConstraints"]["f00Footprint"]
    footprint_area = polygon_area(footprint)
    base_area = polygon_area(contract["baseSpace"]["polygon"])
    anchors = contract["anchors"]
    anchors_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for anchor in anchors
        for point in v2_anchor_test_points(anchor)
    )
    route_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for point in contract["circulation"]["points"]
    )
    e01 = contract["lockedConstraints"]["e01Aperture"]
    e01_points = [
        (e01["minX"], e01["minZ"]),
        (e01["maxX"], e01["minZ"]),
        (e01["maxX"], e01["maxZ"]),
        (e01["minX"], e01["maxZ"]),
    ]
    elevator_anchor = next(anchor for anchor in anchors if anchor["id"] == "A04")
    e01_contained = all(
        point_in_polygon(point, v2_anchor_test_points(elevator_anchor), include_boundary=True)
        for point in e01_points
    )
    banned_anchor_terms = (
        "toilet",
        "charging",
        "first-aid",
        "quiz",
        "lesson",
        "studio",
        "staff",
        "storage",
    )
    anchor_text = " ".join(
        f"{anchor['name']} {anchor['purpose']}".lower() for anchor in anchors
    )
    no_rejected_programme = not any(term in anchor_text for term in banned_anchor_terms)
    checks = [
        {
            "name": "approved_authority_cross_section_present",
            "pass": len(segments) > 0,
            "detail": f"segments={len(segments)} at {CROSS_SECTION_Z_M:.2f} m",
        },
        {
            "name": "single_open_lobby_matches_complete_f00_footprint",
            "pass": abs(base_area - footprint_area) <= 0.01,
            "detail": f"lobby={base_area:.3f} m2; footprint={footprint_area:.3f} m2",
        },
        {
            "name": "minimal_anchor_count",
            "pass": len(anchors) <= 5,
            "detail": f"anchors={len(anchors)}; maximum=5",
        },
        {
            "name": "all_minimal_anchors_inside_f00",
            "pass": anchors_inside,
            "detail": "welcome, seating, theme, E01 and decor anchors are contained",
        },
        {
            "name": "direct_entrance_to_e01_route_inside_f00",
            "pass": route_inside,
            "detail": f"widthTarget={contract['circulation']['widthM']:.2f} m",
        },
        {
            "name": "locked_e01_contained_by_clear_elevator_lobby",
            "pass": e01_contained,
            "detail": "E01 aperture 0.30..3.00 X, 3.00..5.70 Z inside A04",
        },
        {
            "name": "rejected_real_world_and_gameplay_programme_removed",
            "pass": no_rejected_programme,
            "detail": "no toilets, charging, first-aid, staff, storage, lesson, quiz or studio anchors",
        },
        {
            "name": "upper_floor_purposes_remain_undecided",
            "pass": "UNDECIDED" in contract["upperFloors"]["status"],
            "detail": contract["upperFloors"]["status"],
        },
        {
            "name": "s4_v02_contains_no_unity_geometry",
            "pass": not any(OUTPUT_DIR.rglob("*.unity")) and not any(OUTPUT_DIR.rglob("*.fbx")),
            "detail": "review output contains plan/evidence only; no .unity or .fbx",
        },
    ]
    return {
        "schema": "HospitalInterior.R03.S4.PlanGate.v2",
        "status": "PASS" if all(check["pass"] for check in checks) else "FAIL",
        "approval": "PENDING_USER_APPROVAL",
        "supersedes": "StageI1_R03_S4_F00_PlanGate.json",
        "passCount": sum(1 for check in checks if check["pass"]),
        "totalCount": len(checks),
        "footprintAreaM2": round(footprint_area, 3),
        "openLobbyAreaM2": round(base_area, 3),
        "checks": checks,
    }


def anchor_bounds(anchor: dict) -> list[dict]:
    shape = anchor["shape"]
    if shape == "rect":
        return [anchor["bounds"]]
    if shape == "multi_rect":
        return anchor["bounds"]
    if shape == "circle":
        centre_x, centre_z = anchor["centre"]
        radius = anchor["radiusM"]
        return [
            {
                "minX": centre_x - radius,
                "maxX": centre_x + radius,
                "minZ": centre_z - radius,
                "maxZ": centre_z + radius,
            }
        ]
    if shape == "multi_circle":
        radius = anchor["radiusM"]
        return [
            {
                "minX": centre_x - radius,
                "maxX": centre_x + radius,
                "minZ": centre_z - radius,
                "maxZ": centre_z + radius,
            }
            for centre_x, centre_z in anchor["centres"]
        ]
    raise ValueError(f"Unsupported anchor shape for bounds: {shape}")


def bounds_overlap(first: dict, second: dict, tolerance: float = 1.0e-6) -> bool:
    return not (
        first["maxX"] <= second["minX"] + tolerance
        or second["maxX"] <= first["minX"] + tolerance
        or first["maxZ"] <= second["minZ"] + tolerance
        or second["maxZ"] <= first["minZ"] + tolerance
    )


def validate_v3_plan(contract: dict, segments: list[dict]) -> dict:
    footprint = contract["lockedConstraints"]["f00Footprint"]
    footprint_area = polygon_area(footprint)
    base_area = polygon_area(contract["baseSpace"]["polygon"])
    anchors = contract["anchors"]
    stair = next(anchor for anchor in anchors if anchor["id"] == "A06")
    stair_bounds = stair["bounds"]
    expected_stair_bounds = {
        "minX": -27.5,
        "maxX": -22.5,
        "minZ": -3.6,
        "maxZ": 3.6,
    }
    stair_area = (stair_bounds["maxX"] - stair_bounds["minX"]) * (
        stair_bounds["maxZ"] - stair_bounds["minZ"]
    )
    anchors_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for anchor in anchors
        for point in v2_anchor_test_points(anchor)
    )
    stair_clear_of_other_anchors = all(
        not bounds_overlap(stair_bounds, other_bounds)
        for anchor in anchors
        if anchor["id"] != "A06"
        for other_bounds in anchor_bounds(anchor)
    )
    landing = stair["clearLanding"]
    landing_points = [
        (landing["minX"], landing["minZ"]),
        (landing["maxX"], landing["minZ"]),
        (landing["maxX"], landing["maxZ"]),
        (landing["minX"], landing["maxZ"]),
    ]
    landing_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for point in landing_points
    )
    routes = [contract["circulation"], contract["stairCirculation"]]
    routes_inside = all(
        point_in_polygon(point, footprint, include_boundary=True)
        for route in routes
        for point in route["points"]
    )
    e01 = contract["lockedConstraints"]["e01Aperture"]
    e01_points = [
        (e01["minX"], e01["minZ"]),
        (e01["maxX"], e01["minZ"]),
        (e01["maxX"], e01["maxZ"]),
        (e01["minX"], e01["maxZ"]),
    ]
    elevator_anchor = next(anchor for anchor in anchors if anchor["id"] == "A04")
    e01_contained = all(
        point_in_polygon(
            point,
            v2_anchor_test_points(elevator_anchor),
            include_boundary=True,
        )
        for point in e01_points
    )
    banned_anchor_terms = (
        "toilet",
        "charging",
        "first-aid",
        "quiz",
        "lesson",
        "studio",
        "staff",
        "storage",
    )
    anchor_text = " ".join(
        f"{anchor['name']} {anchor['purpose']}".lower() for anchor in anchors
    )
    no_rejected_programme = not any(
        term in anchor_text for term in banned_anchor_terms
    )
    sequence = contract["nextStageSequence"]
    staircase_first = (
        len(sequence) >= 2
        and sequence[0]["id"] == "S5A"
        and "stair" in sequence[0]["name"].lower()
        and sequence[1]["id"] == "S5B"
    )
    checks = [
        {
            "name": "approved_authority_cross_section_present",
            "pass": len(segments) > 0,
            "detail": f"segments={len(segments)} at {CROSS_SECTION_Z_M:.2f} m",
        },
        {
            "name": "single_open_lobby_matches_complete_f00_footprint",
            "pass": abs(base_area - footprint_area) <= 0.01,
            "detail": f"lobby={base_area:.3f} m2; footprint={footprint_area:.3f} m2",
        },
        {
            "name": "minimal_anchor_count_including_required_stair",
            "pass": len(anchors) == 6,
            "detail": f"anchors={len(anchors)}; five lobby anchors plus Stair A",
        },
        {
            "name": "all_anchors_inside_f00",
            "pass": anchors_inside,
            "detail": "five minimal lobby anchors and Stair A are contained",
        },
        {
            "name": "prior_playable_stair_a_footprint_reserved_exactly",
            "pass": stair_bounds == expected_stair_bounds and abs(stair_area - 36.0) <= 0.01,
            "detail": f"X=-27.5..-22.5; Z=-3.6..3.6; area={stair_area:.3f} m2",
        },
        {
            "name": "stair_a_clear_of_other_lobby_anchors",
            "pass": stair_clear_of_other_anchors,
            "detail": "west seating moved south; no anchor overlaps the stair core",
        },
        {
            "name": "stair_a_east_landing_inside_f00",
            "pass": landing_inside,
            "detail": "3.5 m by 3.0 m clear landing reserved outside east-side door",
        },
        {
            "name": "e01_and_stair_routes_inside_f00",
            "pass": routes_inside,
            "detail": "R01 width=3.00 m; R02 width=2.00 m",
        },
        {
            "name": "locked_e01_contained_by_clear_elevator_lobby",
            "pass": e01_contained,
            "detail": "E01 remains inside A04",
        },
        {
            "name": "rejected_real_world_and_gameplay_programme_removed",
            "pass": no_rejected_programme,
            "detail": "no toilets, charging, first-aid, staff, storage, lesson, quiz or studio anchors",
        },
        {
            "name": "upper_floor_purposes_remain_undecided",
            "pass": "PURPOSES REMAIN UNDECIDED" in contract["upperFloors"]["status"],
            "detail": contract["upperFloors"]["status"],
        },
        {
            "name": "full_staircase_integration_precedes_other_f00_work",
            "pass": staircase_first,
            "detail": "S5A full Stair A integration -> S5B remaining F00 greybox",
        },
        {
            "name": "s4_v03_contains_no_3d_geometry",
            "pass": not any(
                path
                for pattern in ("*.unity", "*.fbx", "*.blend", "*.obj")
                for path in OUTPUT_DIR.rglob(pattern)
            ),
            "detail": "review output contains plan/evidence only",
        },
    ]
    return {
        "schema": "HospitalInterior.R03.S4.PlanGate.v3",
        "status": "PASS" if all(check["pass"] for check in checks) else "FAIL",
        "approval": "PENDING_USER_APPROVAL",
        "supersedes": "StageI1_R03_S4_F00_Plan_V02_Gate.json",
        "passCount": sum(1 for check in checks if check["pass"]),
        "totalCount": len(checks),
        "footprintAreaM2": round(footprint_area, 3),
        "openLobbyAreaM2": round(base_area, 3),
        "stairAreaM2": round(stair_area, 3),
        "checks": checks,
    }


def build_v2_svg(segments: list[dict], contract: dict) -> str:
    width = 1900
    is_v3 = contract["schema"].endswith(".v3")
    height = 1200 if is_v3 else 1140
    plan_left = 74.0
    plan_top = 176.0
    min_x, max_x = -38.0, 38.0
    min_z, max_z = -25.0, 19.0
    plan_width = 1240.0
    plan_height = 718.0
    scale = min(plan_width / (max_x - min_x), plan_height / (max_z - min_z))

    def tx(x: float) -> float:
        return plan_left + (x - min_x) * scale

    def ty(z: float) -> float:
        return plan_top + (max_z - z) * scale

    def points_attr(points) -> str:
        return " ".join(f"{tx(point[0]):.1f},{ty(point[1]):.1f}" for point in points)

    title = (
        "F00 Welcome Lobby + Full Stair Reservation — S4 Candidate V03"
        if is_v3
        else "F00 Simple Welcome Lobby — S4 Candidate V02"
    )
    subtitle = (
        "One open game lobby • one full-height playable stair • locked entrance and E01 • F01-F06 purposes remain undecided"
        if is_v3
        else "One open game lobby • minimal anchors • locked R40/S3D entrance and E01 • F01-F06 purposes remain undecided"
    )
    banner = (
        "AWAITING USER APPROVAL — Stair A space reserved; no S5A geometry created"
        if is_v3
        else "AWAITING USER APPROVAL — V01 rejected; no S5 geometry created"
    )

    parts: list[str] = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="1900" height="{height}" viewBox="0 0 1900 {height}">',
        "<defs>",
        '<marker id="v2Arrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#1676a7"/></marker>',
    ]
    if is_v3:
        parts.append(
            '<marker id="v3StairArrow" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#39734a"/></marker>'
        )
    parts.extend(
        [
        '<style>text{font-family:Arial,Helvetica,sans-serif;fill:#17222c}.title{font-size:30px;font-weight:700}.subtitle{font-size:16px;fill:#52616e}.section{font-size:18px;font-weight:700}.anchor-id{font-size:14px;font-weight:700}.anchor-name{font-size:14px;font-weight:700}.small{font-size:12px;fill:#52616e}.purpose{font-size:11px;fill:#45545f}.grid{stroke:#d9e0e5;stroke-width:.7}.outline{fill:none;stroke:#111b23;stroke-width:3}.opaque{stroke:#26343e;stroke-width:1.05;opacity:.45}.glass{stroke:#187b99;stroke-width:1.15;opacity:.68}.fixture{stroke:#756a60;stroke-width:.7;opacity:.32}.door{stroke:#14733a;stroke-width:1.6;opacity:.88}.route{fill:none;stroke:#1676a7;stroke-width:6;stroke-linecap:round;stroke-linejoin:round;stroke-dasharray:12 7}.welcome{fill:#d9f2fb;stroke:#166f8f;stroke-width:2}.seat{fill:#eee6fa;stroke:#6d4ba8;stroke-width:2}.theme{fill:#eee6fa;stroke:#6d4ba8;stroke-width:2.5}.e01hub{fill:#fff0d5;stroke:#b36b00;stroke-width:2}.decor{fill:#e1f2e5;stroke:#39734a;stroke-width:2}.e01{fill:#ffe2e2;stroke:#b42318;stroke-width:2.3}.open-label{font-size:23px;font-weight:700;fill:#33434f}.open-sub{font-size:14px;font-weight:700;fill:#52616e}</style>',
        "</defs>",
        f'<rect width="1900" height="{height}" fill="#ffffff"/>',
        f'<text x="74" y="54" class="title">{title}</text>',
        f'<text x="74" y="83" class="subtitle">{subtitle}</text>',
        '<rect x="74" y="104" width="1240" height="42" rx="6" fill="#fff4d6" stroke="#d39a1f"/>',
        f'<text x="94" y="131" font-size="15" font-weight="700" fill="#684500">{banner}</text>',
        ]
    )

    for x in range(-35, 37, 5):
        parts.append(f'<line x1="{tx(x):.1f}" y1="{ty(-23):.1f}" x2="{tx(x):.1f}" y2="{ty(17):.1f}" class="grid"/>')
        parts.append(f'<text x="{tx(x):.1f}" y="{ty(-23)-5:.1f}" text-anchor="middle" class="small">{x}</text>')
    for z in range(-20, 17, 5):
        parts.append(f'<line x1="{tx(-36):.1f}" y1="{ty(z):.1f}" x2="{tx(36):.1f}" y2="{ty(z):.1f}" class="grid"/>')
        parts.append(f'<text x="{tx(-36)-8:.1f}" y="{ty(z)+4:.1f}" text-anchor="end" class="small">{z}</text>')

    parts.append(f'<polygon points="{points_attr(contract["baseSpace"]["polygon"])}" fill="#f5f1e8" fill-opacity="0.76"/>')

    class_by_category = {
        "existing_opaque": "opaque",
        "existing_glazing": "glass",
        "existing_fixture": "fixture",
        "existing_door": "door",
    }
    for segment in segments:
        start = segment["start"]
        end = segment["end"]
        css_class = class_by_category[segment["category"]]
        parts.append(f'<line x1="{tx(start[0]):.1f}" y1="{ty(start[1]):.1f}" x2="{tx(end[0]):.1f}" y2="{ty(end[1]):.1f}" class="{css_class}"/>')

    anchor_class = {
        "wayfinding": "welcome",
        "seating": "seat",
        "theme": "theme",
        "circulation": "e01hub",
        "decor": "decor",
        "vertical_circulation": "e01hub",
    }
    label_positions: dict[str, tuple[float, float]] = {}
    for anchor in contract["anchors"]:
        css_class = anchor_class[anchor["kind"]]
        shape = anchor["shape"]
        if shape == "rect":
            bounds = anchor["bounds"]
            parts.append(
                f'<rect x="{tx(bounds["minX"]):.1f}" y="{ty(bounds["maxZ"]):.1f}" width="{(bounds["maxX"]-bounds["minX"])*scale:.1f}" height="{(bounds["maxZ"]-bounds["minZ"])*scale:.1f}" rx="8" class="{css_class}"/>'
            )
            label_positions[anchor["id"]] = (
                (4.5, 1.3)
                if anchor["id"] == "A04"
                else (
                    (bounds["minX"] + bounds["maxX"]) / 2,
                    (bounds["minZ"] + bounds["maxZ"]) / 2,
                )
            )
        elif shape == "multi_rect":
            for bounds in anchor["bounds"]:
                parts.append(
                    f'<rect x="{tx(bounds["minX"]):.1f}" y="{ty(bounds["maxZ"]):.1f}" width="{(bounds["maxX"]-bounds["minX"])*scale:.1f}" height="{(bounds["maxZ"]-bounds["minZ"])*scale:.1f}" rx="8" class="{css_class}"/>'
                )
            bounds = anchor["bounds"][0]
            label_positions[anchor["id"]] = ((bounds["minX"] + bounds["maxX"]) / 2, (bounds["minZ"] + bounds["maxZ"]) / 2)
        elif shape == "circle":
            centre_x, centre_z = anchor["centre"]
            parts.append(f'<circle cx="{tx(centre_x):.1f}" cy="{ty(centre_z):.1f}" r="{anchor["radiusM"]*scale:.1f}" class="{css_class}"/>')
            label_positions[anchor["id"]] = (centre_x, centre_z)
        elif shape == "multi_circle":
            for centre_x, centre_z in anchor["centres"]:
                parts.append(f'<circle cx="{tx(centre_x):.1f}" cy="{ty(centre_z):.1f}" r="{anchor["radiusM"]*scale:.1f}" class="{css_class}"/>')
            label_positions[anchor["id"]] = tuple(anchor["centres"][0])

    if is_v3:
        stair = next(anchor for anchor in contract["anchors"] if anchor["id"] == "A06")
        stair_bounds = stair["bounds"]
        lane_x_values = (-26.62, -24.38)
        for lane_x in lane_x_values:
            for step_index in range(7):
                step_z = -2.75 + step_index * 0.92
                parts.append(
                    f'<line x1="{tx(lane_x-0.62):.1f}" y1="{ty(step_z):.1f}" '
                    f'x2="{tx(lane_x+0.62):.1f}" y2="{ty(step_z):.1f}" '
                    'stroke="#8a5b00" stroke-width="1.2" opacity="0.82"/>'
                )
        landing = stair["clearLanding"]
        parts.append(
            f'<rect x="{tx(landing["minX"]):.1f}" y="{ty(landing["maxZ"]):.1f}" '
            f'width="{(landing["maxX"]-landing["minX"])*scale:.1f}" '
            f'height="{(landing["maxZ"]-landing["minZ"])*scale:.1f}" '
            'fill="#e1f2e5" fill-opacity="0.46" stroke="#39734a" '
            'stroke-width="1.5" stroke-dasharray="6 4"/>'
        )
        door_centre = stair["priorDesign"]["doorCentre"]
        door_half = stair["priorDesign"]["doorClearWidthM"] * 0.5
        parts.append(
            f'<line x1="{tx(door_centre[0]):.1f}" y1="{ty(door_centre[1]-door_half):.1f}" '
            f'x2="{tx(door_centre[0]):.1f}" y2="{ty(door_centre[1]+door_half):.1f}" '
            'stroke="#39734a" stroke-width="5"/>'
        )

    e01 = contract["lockedConstraints"]["e01Aperture"]
    parts.append(
        f'<rect x="{tx(e01["minX"]):.1f}" y="{ty(e01["maxZ"]):.1f}" width="{(e01["maxX"]-e01["minX"])*scale:.1f}" height="{(e01["maxZ"]-e01["minZ"])*scale:.1f}" class="e01"/>'
    )
    route = contract["circulation"]
    route_path = " ".join(
        ("M" if index == 0 else "L") + f" {tx(point[0]):.1f} {ty(point[1]):.1f}"
        for index, point in enumerate(route["points"])
    )
    parts.append(f'<path d="{route_path}" class="route" marker-end="url(#v2Arrow)"/>')
    if is_v3:
        stair_route = contract["stairCirculation"]
        stair_route_path = " ".join(
            ("M" if index == 0 else "L")
            + f" {tx(point[0]):.1f} {ty(point[1]):.1f}"
            for index, point in enumerate(stair_route["points"])
        )
        parts.append(
            f'<path d="{stair_route_path}" fill="none" stroke="#39734a" '
            'stroke-width="4.5" stroke-linecap="round" stroke-linejoin="round" '
            'stroke-dasharray="8 6" marker-end="url(#v3StairArrow)"/>'
        )
    parts.append(f'<polygon points="{points_attr(FOOTPRINT)}" class="outline"/>')

    for anchor in contract["anchors"]:
        centre_x, centre_z = label_positions[anchor["id"]]
        parts.append(f'<circle cx="{tx(centre_x):.1f}" cy="{ty(centre_z):.1f}" r="17" fill="#ffffff" stroke="#26343e" stroke-width="1.2"/>')
        parts.append(f'<text x="{tx(centre_x):.1f}" y="{ty(centre_z)+5:.1f}" text-anchor="middle" class="anchor-id">{anchor["id"]}</text>')
    second_seat = contract["anchors"][1]["bounds"][1]
    second_seat_x = (second_seat["minX"] + second_seat["maxX"]) / 2
    second_seat_z = (second_seat["minZ"] + second_seat["maxZ"]) / 2
    parts.append(f'<circle cx="{tx(second_seat_x):.1f}" cy="{ty(second_seat_z):.1f}" r="17" fill="#ffffff" stroke="#26343e" stroke-width="1.2"/>')
    parts.append(f'<text x="{tx(second_seat_x):.1f}" y="{ty(second_seat_z)+5:.1f}" text-anchor="middle" class="anchor-id">A02</text>')
    second_decor = contract["anchors"][4]["centres"][1]
    parts.append(f'<circle cx="{tx(second_decor[0]):.1f}" cy="{ty(second_decor[1]):.1f}" r="17" fill="#ffffff" stroke="#26343e" stroke-width="1.2"/>')
    parts.append(f'<text x="{tx(second_decor[0]):.1f}" y="{ty(second_decor[1])+5:.1f}" text-anchor="middle" class="anchor-id">A05</text>')

    open_subtitle = (
        "no programme rooms • free movement • clear sightlines to E01 and Stair A"
        if is_v3
        else "no new programme rooms • free movement • clear sightline to E01"
    )
    anchor_heading = "Plan anchors" if is_v3 else "Minimal plan anchors"
    parts.extend(
        [
            f'<text x="{tx(9):.1f}" y="{ty(-14):.1f}" text-anchor="middle" class="open-label">OPEN WELCOME LOBBY HALL</text>',
            f'<text x="{tx(9):.1f}" y="{ty(-15.5):.1f}" text-anchor="middle" class="open-sub">{open_subtitle}</text>',
            f'<text x="{tx(-4):.1f}" y="{ty(-23.6):.1f}" text-anchor="middle" class="anchor-name">MAIN ENTRANCE</text>',
            f'<path d="M {tx(34):.1f} {ty(15):.1f} L {tx(34):.1f} {ty(18):.1f}" stroke="#17222c" stroke-width="2.5" marker-end="url(#v2Arrow)"/>',
            f'<text x="{tx(34):.1f}" y="{ty(14.3):.1f}" text-anchor="middle" class="anchor-name">N / +Z</text>',
            f'<text x="1360" y="118" class="section">{anchor_heading}</text>',
            '<line x1="1360" y1="132" x2="1840" y2="132" stroke="#ccd5dc"/>',
        ]
    )

    schedule_y = 170.0
    schedule_fill = {"wayfinding": "#d9f2fb", "seating": "#eee6fa", "theme": "#eee6fa", "circulation": "#fff0d5", "decor": "#e1f2e5", "vertical_circulation": "#fff0d5"}
    schedule_stroke = {"wayfinding": "#166f8f", "seating": "#6d4ba8", "theme": "#6d4ba8", "circulation": "#b36b00", "decor": "#39734a", "vertical_circulation": "#b36b00"}
    for anchor in contract["anchors"]:
        purpose_lines = wrap_words(anchor["purpose"], 64)[:3]
        parts.append(f'<rect x="1360" y="{schedule_y-18:.1f}" width="38" height="25" rx="4" fill="{schedule_fill[anchor["kind"]]}" stroke="{schedule_stroke[anchor["kind"]]}"/>')
        parts.append(f'<text x="1379" y="{schedule_y:.1f}" text-anchor="middle" class="anchor-id">{anchor["id"]}</text>')
        parts.append(f'<text x="1412" y="{schedule_y:.1f}" class="anchor-name">{html.escape(anchor["name"])}</text>')
        for line_index, line in enumerate(purpose_lines):
            parts.append(f'<text x="1412" y="{schedule_y+20+line_index*15:.1f}" class="purpose">{html.escape(line)}</text>')
        schedule_y += 92.0

    if is_v3:
        parts.extend(
            [
                '<text x="1360" y="742" class="section">Theme direction</text>',
                '<line x1="1360" y1="756" x2="1840" y2="756" stroke="#ccd5dc"/>',
                '<text x="1360" y="786" class="anchor-name">Premium clinical-future welcome</text>',
                '<text x="1360" y="812" class="purpose">Warm off-white + soft stone base</text>',
                '<text x="1360" y="834" class="purpose">Charcoal framing + restrained bronze accents</text>',
                '<text x="1360" y="856" class="purpose">Blue-grey glass + cool wayfinding light</text>',
                '<text x="1360" y="878" class="purpose">Soft indirect lighting + limited greenery</text>',
                '<text x="1360" y="900" class="purpose">One abstract anatomy-inspired centrepiece</text>',
                '<text x="1360" y="930" class="small">Exact finishes and objects remain deferred to S6.</text>',
                '<text x="74" y="978" class="section">S4 V03 gate</text>',
                '<line x1="74" y1="995" x2="1840" y2="995" stroke="#ccd5dc"/>',
                '<line x1="82" y1="1030" x2="150" y2="1030" class="route"/><text x="166" y="1035" class="anchor-name">R01 entrance-to-E01 · 3.00 m clear target</text>',
                '<line x1="680" y1="1030" x2="748" y2="1030" fill="none" stroke="#39734a" stroke-width="4.5" stroke-dasharray="8 6"/><text x="764" y="1035" class="anchor-name">R02 lobby-to-Stair-A · 2.00 m clear target</text>',
                '<text x="74" y="1092" class="subtitle">Approval authorizes S5A full Stair A integration first. All other F00 greybox work remains blocked until S5A approval.</text>',
                '<text x="74" y="1143" class="small">Source: exact R40 cross-section + locked S2/S3D contracts + prior playable Stair A footprint • upper-floor purposes remain undecided</text>',
                "</svg>",
            ]
        )
    else:
        parts.extend(
            [
                '<text x="1360" y="650" class="section">Theme direction</text>',
                '<line x1="1360" y1="664" x2="1840" y2="664" stroke="#ccd5dc"/>',
                '<text x="1360" y="694" class="anchor-name">Premium clinical-future welcome</text>',
                '<text x="1360" y="720" class="purpose">Warm off-white + soft stone base</text>',
                '<text x="1360" y="742" class="purpose">Charcoal framing + restrained bronze accents</text>',
                '<text x="1360" y="764" class="purpose">Blue-grey glass + cool wayfinding light</text>',
                '<text x="1360" y="786" class="purpose">Soft indirect lighting + limited greenery</text>',
                '<text x="1360" y="808" class="purpose">One abstract anatomy-inspired centrepiece</text>',
                '<text x="1360" y="850" class="small">Exact finishes and objects remain deferred to S6.</text>',
                '<text x="74" y="950" class="section">S4 V02 gate</text>',
                '<line x1="74" y1="967" x2="1840" y2="967" stroke="#ccd5dc"/>',
                '<line x1="82" y1="1002" x2="150" y2="1002" class="route"/><text x="166" y="1007" class="anchor-name">R01 direct entrance-to-E01 route · 3.00 m clear target</text>',
                '<text x="74" y="1060" class="subtitle">Approve this simple open-lobby direction, or request a specific anchor/route correction. Approval authorizes S5 greybox only.</text>',
                '<text x="74" y="1110" class="small">Source: exact R40 mesh cross-section at 1.20 m + locked S2/S3D contracts • F01-F06 remain empty and undecided</text>',
                "</svg>",
            ]
        )
    return "\n".join(parts)


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    segments = cross_section_segments()
    payload = {
        "schema": "HospitalInterior.R03.S4.ExistingCrossSection.v1",
        "status": "PLANNING_BASE_ONLY_NO_S5_GEOMETRY",
        "authority": str(AUTHORITY.relative_to(WORKSPACE)).replace("\\", "/"),
        "authoritySha256": sha256(AUTHORITY),
        "blenderVersion": bpy.app.version_string,
        "coordinateSystem": "Blender X/Y horizontal maps to Unity X/Z; metres",
        "crossSectionHeightM": CROSS_SECTION_Z_M,
        "approvedF00Footprint": [[x, y] for x, y in FOOTPRINT],
        "lockedEntrance": {
            "outerDoorBounds": {"x": [-6.79, -1.21], "z": [-23.04, -22.96]},
            "innerDoorBounds": {"x": [-6.65, -1.35], "z": [-20.60, -20.52]},
        },
        "lockedE01": {
            "operatingAperture": {"x": [0.30, 3.00], "z": [3.00, 5.70]},
            "cabinCentre": {"x": 1.65, "z": 4.35},
            "arrival": {"x": 3.00, "z": 1.90},
        },
        "segmentCount": len(segments),
        "segments": segments,
    }
    output = OUTPUT_DIR / "StageI1_R03_S4_F00_ExistingCrossSection.json"
    output.write_text(json.dumps(payload, indent=2), encoding="utf-8")
    contract = json.loads(PLAN_CONTRACT.read_text(encoding="utf-8"))
    SVG_OUTPUT.write_text(build_svg(segments, contract), encoding="utf-8")
    gate = validate_plan(contract, segments)
    gate_output = OUTPUT_DIR / "StageI1_R03_S4_F00_PlanGate.json"
    gate_output.write_text(json.dumps(gate, indent=2), encoding="utf-8")

    v2_contract = json.loads(PLAN_V2_CONTRACT.read_text(encoding="utf-8"))
    SVG_V2_OUTPUT.write_text(build_v2_svg(segments, v2_contract), encoding="utf-8")
    v2_gate = validate_v2_plan(v2_contract, segments)
    v2_gate_output = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V02_Gate.json"
    v2_gate_output.write_text(json.dumps(v2_gate, indent=2), encoding="utf-8")

    v3_contract = json.loads(PLAN_V3_CONTRACT.read_text(encoding="utf-8"))
    SVG_V3_OUTPUT.write_text(build_v2_svg(segments, v3_contract), encoding="utf-8")
    v3_gate = validate_v3_plan(v3_contract, segments)
    v3_gate_output = OUTPUT_DIR / "StageI1_R03_S4_F00_Plan_V03_Gate.json"
    v3_gate_output.write_text(json.dumps(v3_gate, indent=2), encoding="utf-8")

    print(f"S4_EXISTING_CROSS_SECTION={output}")
    print(f"S4_EXISTING_SEGMENTS={len(segments)}")
    print(f"S4_PLAN_SVG={SVG_OUTPUT}")
    print(f"S4_PLAN_GATE={gate['status']} {gate['passCount']}/{gate['totalCount']}")
    print(f"S4_PLAN_V02_SVG={SVG_V2_OUTPUT}")
    print(
        f"S4_PLAN_V02_GATE={v2_gate['status']} "
        f"{v2_gate['passCount']}/{v2_gate['totalCount']}"
    )
    print(f"S4_PLAN_V03_SVG={SVG_V3_OUTPUT}")
    print(
        f"S4_PLAN_V03_GATE={v3_gate['status']} "
        f"{v3_gate['passCount']}/{v3_gate['totalCount']}"
    )


if __name__ == "__main__":
    main()
