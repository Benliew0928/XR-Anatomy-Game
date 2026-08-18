# Hospital Interior R03 — S3A E01 Design Checkpoint

**Status:** `READY FOR USER VISUAL REVIEW`  
**Approval state:** approved by the user on 2026-08-15  
**Runtime boundary:** S3B door programming is authorized; S3C travel programming remains unstarted.

## Delivered design candidate

- New R03 E01 hospital-stretcher cabin authored from scratch; no R01, R02, or quarantined S3 geometry was loaded, linked, or restored.
- Premium clinical-future treatment using warm hygienic off-white panels, brushed stainless lower protection and doors, charcoal framing, refined-bronze trim/handrail, dark terrazzo flooring, and a 3500 K integrated ceiling diffuser.
- Outer cabin: `2.35 × 2.45 × 2.60 m`.
- Declared clear internal floor: `2.10 × 2.20 m`.
- Clear doorway: `1.20 × 2.20 m` with a flush threshold.
- Approved S1 core fit: cabin bounds X `0.475–2.825 m`, Z `3.125–5.575 m`, contained in the left half of the approved exterior-verified elevator core X `0–6 m`, Z `3–5.7 m`.
- Full-width rear handrail, protected lower-wall zone, chamfered contact edges, one cabin practical light, internal/landing F00 indicators, landing call panel, and nine cabin commands (`F00–F06`, `OPEN`, `CLOSE`).
- Four independent centre-opening meshes are present: two cabin leaves and two landing leaves, each with closed/open position metadata and independent pivots for the future normal door system.

## Design-review evidence

1. `01_S3A_E01_Landing_DoorsClosed.png` — landing and closed doors.
2. `02_S3A_E01_Landing_DoorsOpen.png` — full open passage and cabin reveal.
3. `03_S3A_E01_CabinInterior.png` — full cabin interior.
4. `04_S3A_E01_ControlPanelClose.png` — all seven floor commands plus door-open and door-close controls.
5. `05_S3A_E01_StretcherClearance.png` — `0.8 × 2.1 m` stretcher proxy through the `1.20 m` opening.
6. `06_S3A_E01_MaterialsAndCeilingLight.png` — wall, protection, handrail, bronze reveals, and ceiling diffuser.

## Source and pre-integration exports

- Blender source: `ArtSource/Environment/Blender/HospitalInterior/HospitalInterior_E01_Cabin_R03.blend`
- Cabin FBX: `Exports/HospitalInterior/StageI1_R03_S3A/HospitalInterior_E01_Cabin_R03.fbx`
- Reusable landing portal FBX: `Exports/HospitalInterior/StageI1_R03_S3A/HospitalInterior_E01_LandingPortal_R03.fbx`
- Deterministic builder: `Tools/HospitalInterior/build_hospital_interior_s3a_r03.py`
- Independent validator: `Tools/HospitalInterior/validate_hospital_interior_s3a_r03.py`

## Verification

- Saved-source validation: `PASS 21/21` in `StageI1_R03_S3A_Validation.json`.
- Post-approval integration correction: the threshold top and both door bottoms were aligned exactly to the `0.11 m` cabin-floor top. This removes a hidden `20 mm` threshold discrepancy without changing the approved envelope or visual direction.
- All `74/74` source meshes have UV layers and material assignments.
- Separate cabin/landing roots, four independent door leaves, flush floor/threshold metadata, nine controls, six renders, and both FBX exports passed.
- Approved S2 baseline recheck after authoring: `38/38` files unchanged; `0` missing or hash mismatches.
- No Unity S3 scene, runtime controller, prefab, or imported S3 asset was created before design approval.

## Approval record

The user explicitly approved S3A and authorized S3B. S3C and S4 remain blocked until their preceding gates pass.
