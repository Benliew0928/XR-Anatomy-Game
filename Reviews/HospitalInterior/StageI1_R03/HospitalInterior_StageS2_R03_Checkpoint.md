# Hospital Interior R03 Stage S2 Checkpoint

**Active stage:** `S2 - seven empty floor scenes`  
**Status:** `AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW`
**User approval:** not yet received

## Implemented scope

- Seven independent identity-root floor scenes at the approved S1 datums.
- One neutral walkable slab, collision, invisible safety boundary, E01 arrival anchor/safe zone, and one floor-name label per scene.
- One review-only bootstrap loads the approved exterior and exactly one selected floor; keys 0-6 switch floors manually.
- F01-F06 keep the approved S1 core bounds and extend the same single slab beneath the measured R40 left/right/rear facade and balcony edges, closing the user-reported seams without modifying the protected exterior.
- Six non-collidable, downward-facing structural undersides remain persistent so an isolated active floor does not look through the full building height. They align with the production balcony soffit undersides and use the adjoining warm off-white finish.
- Six exterior-only balcony backer panels and six overlapping production balcony floor skins are hidden at review runtime; the protected exterior source and production scene hashes remain unchanged.
- The one exact-datum S2 slab uses the same neutral finish values as the adjoining balcony floor, removing the visible material step while preserving the approved elevations.

## Automated verification

- Static gate: `PASS 62/62` after the complete pipeline succeeds.
- Runtime route gate: `PASS 48/48` for `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00` after the complete pipeline succeeds.
- Windows review player: `Exports/HospitalInterior/StageI1_R03_S2_AutoGate/HospitalInterior_S2_R03_Review.exe`.
- Seven arrival images plus `08_S2_F01_BalconySeam_Corrected.png`, `09_S2_F01_MainGlassSeam_Corrected.png`, `10_S2_F01_StackSeparation_Corrected.png`, and `11_S2_F04_BalconyJunction_Smoothed.png`.
- The production exterior, R40 authority, and EditorBuildSettings remain hash-protected. Any explicitly recorded Unity material serialization normalization must also pass semantic validation.

## Deliberately deferred

Elevator travel, elevator cabin/doors, stairs, rooms, corridors, ceilings, furniture, decoration, clinical equipment, F00 planning, and S3-S8 remain unstarted.

## Test scene

`Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S2_R03_EmptyFloors_SIM_LOCAL.unity`

S2 automated checks support review but do not grant approval. S3 planning remains blocked until the user explicitly accepts all seven arrival views and the playable review.
