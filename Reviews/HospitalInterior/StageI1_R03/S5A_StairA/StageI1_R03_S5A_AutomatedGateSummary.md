# Hospital Interior Stage I1 R03 — S5A Stair A Automated Gate Summary

Status: `APPROVED_BY_USER_S5B_UNBLOCKED`

S5A implements the continuous playable Stair A integration. Stage S5B lobby greybox work is now unblocked.

## Automated results

| Gate | Result | Evidence |
|---|---:|---|
| Blender geometry and export | PASS `40/40` | `StageI1_R03_S5A_StairA_BlenderGate.json` |
| Unity static integration | PASS `31/31` | `StageI1_R03_S5A_StaticGate.json` |
| Windows runtime integration | PASS `31/31` | `StageI1_R03_S5A_RuntimeGate.json` |
| Windows desktop/PC-VR player | BUILT | `Exports/HospitalInterior/StageI1_R03_S5A_StairA/HospitalInterior_S5A_R03_StairA.exe` |

The runtime gate returned to steady state with active floor `F00`, elevator cabin floor `F00`, and a maximum of two simultaneously loaded floor scenes during validated transitions.

## Locked implementation

- Exact A06 envelope: X `-29.0..-22.0`, Z `-5.0..5.0`, Y `-0.20..28.83 m`. (Expanded from 5.0×7.2m to 7.0×10.0m for VR turning clearance).
- Two-flight switchback: 12 flights, 164 visible tread-aligned steps, 7 floor landings, 6 intermediate landings, 7 automatic sliding doors, and F06 top closure.
- Dual E01/Stair A slab, underside, persistent floor-plate, and exterior-structure apertures are S5A-owned runtime replacements.
- Exactly one S5A floor coordinator owns full-floor loading; only origin and destination coexist during a validated transition.
- Stair travel preserves the elevator cabin floor. Elevator and stair requests share serialized transition ownership.
- Stair treads and internal landings are excluded from teleport and use visible tread-aligned collision; fall recovery targets the latest safe floor or intermediate landing.
- The protected S2/S3/S4 authority set contains 140 files and remains valid.

## Automated failure coverage

The runtime gate passed destination-load rollback, origin-unload rollback, missing-aperture rejection, unloaded-floor door rejection, door-obstruction reversal, concurrent request serialization, fall recovery, stair/cabin separation, landing-call behavior, ascent/descent, entrance regression, and lawn-collision regression.

## User Approval Status

S5A Stair A integration is **FULLY APPROVED BY USER**. Exact phrase recorded: `APPROVE S5A STAIR A`. S5B greybox planning is now unblocked.

