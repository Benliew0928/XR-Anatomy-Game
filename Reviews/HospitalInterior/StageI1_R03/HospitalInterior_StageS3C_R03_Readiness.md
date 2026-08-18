# Hospital Interior R03 — S3C Travel Review Readiness

**Authority:** S3B was user-approved on 2026-08-15 and remains locked. S3C E01 travel was implemented on 2026-08-16.  
**State:** automated gates pass; explicit user functional approval is pending. S4 is blocked.

## Locked inputs to preserve

- One persistent E01 core with the approved clinical-future cabin, seven landing portals, transparent hoistway, and observation cabin.
- Cabin centre: X `1.65 m`, Z `4.35 m`; approved S2 floor datums: F00 `0.0`, F01 `5.8`, F02 `9.7`, F03 `13.6`, F04 `17.5`, F05 `21.4`, F06 `25.3 m`.
- Shaft authority: Y `-0.200..28.830 m`; active travel aperture: X `0.300..3.000 m`, Z `3.000..5.700 m`.
- Sweep clearances: wall `0.075 m`, guide rail `0.040 m`, F00 base `0.090 m`, F06 roof clearance `0.940 m`.
- Cabin observation design: three transparent side/rear walls, retained lower stainless protection and handrail, `1.49 m` standing-eye viewing zone, and full shaft nesting.
- S3B door contract remains operational: `1.2 s` open, `4 s` dwell, `1.2 s` close, obstruction reversal, positive cabin/landing interlocks, and a `1.205 m` clear opening.
- Protected S2 baseline must remain `38/38` byte-identical. S2 arrival anchors remain validation/recovery positions only, never normal-travel teleports.

## Implemented S3C scope

1. `S3ElevatorController` now moves the approved cabin without editing the locked S3B scene or assets.
2. Cabin requests require the player inside; player and cabin move continuously together with less than `0.000002 m` measured relative drift in the automated route.
3. Doors close and interlock before motion. The destination loads and validates behind closed doors; only its aligned landing pair is enabled after exact arrival.
4. The Windows player completes F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00, same-floor reopen, invalid/busy/obstructed rejection, cancellation rollback, load-failure rollback, enclosed `FaultedSafe`, and review recovery.
5. Seven render-only floor plates preserve visible F00–F06 storey structure outside the E01 void; the six persistent soffits and every active floor slab remain aperture-cut, while the roof closure is retained.
6. The S3C-only copy disables the car ceiling, diffuser, trim, and ceiling collider, leaving the complete F00–F06 shaft view open from inside the cabin; the separate building roof closure above F06 remains intact.
7. The `Exterior_Base` global-structure mesh is replaced at S3C runtime by a generated copy cut through the complete E01 operating cell, removing the F00-only obstruction at Y `5.26 m` without editing the protected exterior source.
8. Static validation passes `25/25`; Windows-player runtime validation passes `39/39`; all protected S2 and S3B inputs remain unchanged.

## Review handoff

- Review player: `Exports/HospitalInterior/StageI1_R03_S3C_AutoGate/HospitalInterior_S3C_R03_TravelReview.exe`.
- Review scene: `Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S3C_R03_E01Travel_SIM_LOCAL.unity`.
- Reports and visual evidence: `Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel`.
- No S4 work, F00 layout work, E02 work, stairs, audio, or added upper-floor decoration has started or is authorized.
- Automated checks support the review; they do not replace user functional approval.
