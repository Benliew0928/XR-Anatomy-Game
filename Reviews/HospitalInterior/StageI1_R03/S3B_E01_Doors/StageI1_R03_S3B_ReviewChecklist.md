# Hospital Interior R03 — S3B Approval Record and Final Review

**User approval:** received 2026-08-15  
**Final state:** approved complete; S3C travel is authorized next but not started.  
**Final automated evidence:** static `PASS 37/37`; Windows-player runtime `PASS 22/22`; protected S2 baseline `38/38` unchanged.

Run `HospitalInterior_S3B_R03_DoorReview.exe` from the S3B export folder.

## Controls

- `WASD`: walk; `Left Shift`: sprint; hold `RMB`: look.
- `O`: door open; `C`: door close; `L`: F00 landing call.
- `K`: toggle the visible threshold obstruction proxy.
- `Y`: show or hide the review overlay.
- `Left click`: activate a physical elevator control while within reach.

## Approval checks

1. Confirm both cabin leaves and both F00 landing leaves open together and reach the full clear position in about `1.2 s`.
2. Walk through the doorway and confirm there is no threshold step, fall gap, clipping, or invisible blocker.
3. Let the doors dwell for `4 s` and close automatically in about `1.2 s`.
4. Press `O` while the doors are closing and confirm that all four leaves reopen smoothly.
5. Press `C` while open and confirm an early close request is accepted only when the threshold is clear.
6. Press `K`, request close, and confirm closing is blocked or immediately reversed; clear the obstruction and confirm normal closing resumes.
7. Use the landing call and the physical `OPEN`/`CLOSE` controls. Confirm the F00–F06 travel buttons remain disabled in S3B.
8. Review `06_S3B_SevenPortal_StoreyFit.png` and confirm the landing portals are joined by the full-height hoistway structure instead of floating independently.
9. Review `07_S3B_ContinuousShaft_Section.png` and confirm a continuous enclosed path runs from below F00 through F06 to the roof limit. The cyan line is review-only and marks the future cabin centreline.
10. Confirm the current player intentionally stays at F00: floor travel is S3C work and has not been smuggled into this correction.
11. Review `08_S3B_F00_GlassHoistway_AtriumView.png` and confirm the former opaque mass above F00 now reads as a transparent panoramic glass lift tower.
12. Confirm the glass remains visibly present through its restrained blue-grey tint and reflections, while the charcoal mullions, bronze transoms, brushed guide rails, and existing portal form one coherent hospital design.
13. Review `09_S3B_CabinNestedInGlassShaft.png` and confirm the transparent cabin is visibly a separate inner car fully enclosed by the stationary outer glass tower.
14. Review `10_S3B_CabinObservation_Interior.png` and confirm a standing player can see outward through the rear and side walls while the lower stainless impact protection, rear handrail, ceiling, controls, and doors remain appropriate for a hospital stretcher lift.

## Verified shaft dimensions

- Hoistway vertical authority: Y `-0.200 to 28.830 m`.
- Floor/underside aperture: X `0.300 to 3.000 m`, Z `3.000 to 5.700 m`.
- Minimum nominal sweep clearances: walls `0.075 m`, guide rails `0.040 m`, F00 base `0.090 m`, F06 top closure `0.940 m`.
- The aperture uses S3-owned meshes at runtime; the protected S2 baseline remains byte-identical.
- Architectural finish: 11 low-iron transparent glazed faces, 42 non-colliding charcoal/bronze frame members, and visible brushed-steel guide rails.
- Observation cabin: three transparent side/rear walls, ten non-colliding cabin frame members, a `1.49 m` clear observation zone above the retained protection panels, and safety colliders on all glass boundaries.
- Cabin nesting: X `0.475..2.825 m`, Z `3.116..5.575 m` inside the shaft, with left/right/front/rear clearances of `0.075/0.075/0.116/0.075 m`.

The observation design is ready for S3C movement, but actual moving-floor transition footage cannot exist until S3C is separately implemented.

S3B is locked as approved. The next task may start S3C travel only; it must preserve this cabin, glass shaft, door system, S2 baseline, and all stated clearances.
