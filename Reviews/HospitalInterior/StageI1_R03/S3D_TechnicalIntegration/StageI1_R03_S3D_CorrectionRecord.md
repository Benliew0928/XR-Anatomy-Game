# Hospital Interior R03 S3D Correction Record

**Date:** 2026-08-18  
**Status:** `CORRECTION IMPLEMENTED — AUTOMATED GATES PASS; USER RETEST PENDING`  
**Authority:** S3D integration layer only

## User findings

- Displayed grass/lawn was visual-only, allowing the player to fall through it.
- An invisible boundary around the drop-off blocked part of the visible hospital entrance.
- A continuous gap remained along the main-window edge on F01-F06 after full integration.
- Elevator controls required point-blank positioning and overly precise aiming.

## Cause

- The broad green surface is `S05S_S5_EXP_LandscapeHardscape_S05S_S4B_ContinuousLawnVisual`. It has one six-quad (`12` triangle) render mesh and no collider. The first correction incorrectly generated support only from the `2,848` grass-blade occupancy cells, so green lawn outside those blade cells remained non-solid.
- Creating a MeshCollider from the renderer at runtime was also invalid: Unity static batching replaces the source with a non-readable `Combined Mesh (root: scene)` in the Windows player.
- The invisible wall shown around the drop-off was not a door leaf. It was the legacy R43 standalone-exterior safety collider `COL_Perimeter_Front`, centered at `(0, 1, -34.1)` with size `(80, 2, 0.2)`. In the full R05B-site integration it cut across both the official drop-off route and the screenshot-side right-lawn approach.
- F00 additionally had a front safety opening centered around X `0`, while the real outer four-leaf entrance is centered around X `-4`; this separate aperture mismatch could obstruct part of the doorway.
- `S3DAdaptiveRig.bootstrap` was not serialized, so its edit-time assignment was lost and the HUD falsely displayed `Bootstrap missing` even though the integration bootstrap itself was running.
- The missing edge is on the entrance-facing front boundary: the floor stops at Z `-15.50` while the front facade sits farther toward negative Z. The first two S3D corrections extended the right-side X boundary, so they targeted the wrong axis and could not close the photographed slot.
- S3D interaction inherited S3C's review-only `3.5 m` single-pixel first-hit ray. Small trigger colliders and nearby panel geometry made valid controls hard to acquire except at close range.

## S3D-only correction

- During the S3D editor build, assign one non-rendered MeshCollider from the exact approved `ContinuousLawnVisual` source mesh before static batching. The collider is cooked into the S3D bootstrap scene, matches the rendered lawn bounds, and does not alter the approved R05B prefab, FBX, or additive scene.
- Keep the lawn support out of the teleport allowlist, preserving route-only site teleport at F00.
- Disable exactly one `COL_Perimeter_Front` collider at S3D initialization. The approved R05B front/east/west/rear perimeter colliders remain active.
- Whenever F00 loads, disable only its two original front safety-boundary colliders and provide two S3D-owned replacements around the actual outer-door collider bounds plus a `0.2 m` margin.
- Serialize the adaptive rig's integration-bootstrap reference so the HUD reports the actual loading/ready/error state.
- Add six S3D-owned, collidable entrance-facing main-glass edge finishes for F01-F06. Each is `0.20 m` thick, spans X `-23.95..18.80` and Z `-17.30..-15.48`, overlaps the existing floor and front facade, and uses the exact approved `MAT_S2_R03_EmptySlab` material.
- Whenever F01-F06 loads, relocate its original `S2_R03_SafetyBoundary_03` from Z `-15.50` to Z `-17.30` and widen it from X `-23.95..18.00` to X `-23.95..18.80`, matching the corrected floor instead of cutting across it.
- Recheck all four approved S2 facade/balcony coverage probes plus the solid edge finish after every upper-floor runtime swap, and capture a dedicated F01 main-glass seam frame.
- Increase elevator-control reach to `5.5 m` and replace the strict ray with a `0.18 m` sphere-assisted target search. Candidate selection favors the control closest to the aim line and rejects controls occluded by nearer solid geometry.
- Test the approach from `(0, -42)` and the screenshot-side right lawn `(14, -36)`, then test full-height player-capsule and real CharacterController lanes at X `-6.44`, `-4.00`, and `-1.56` from Z `-25.5` through `-17.5`.

## Gate result

- Static gate: `PASS 17/17`.
- Corrected Windows runtime gate: `PASS 53/53`.
- Lawn support: exact rendered source mesh, `12` triangles, aligned bounds, and `12/12` distributed accessible-lawn CharacterController fall-through checks stopped by solid surfaces.
- Entrance approach: both the official drop-off and screenshot-side routes reach the doorway with `0.00 m` remaining.
- Entrance traversal: three lanes, zero capsule blockers, and the real CharacterController reaches Z `-17.50` in every lane.
- Full elevator and failure route remains passing: `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`.
- Main-window closure: F01-F06 each pass all `4/4` approved slab probes plus alignment, approved material, and collider raycast checks on the new edge finish.
- Elevator interaction profile: `5.50 m` reach and `0.18 m` aim-assist radius are present in the built runtime.
- Approved S2/S3/R44/R05B sources, Stage 6A, and `EditorBuildSettings` remain protected.

The correction is not S3D approval. Desktop and Quest Link/Air Link functional reviews must be repeated before the user may approve S3D.
