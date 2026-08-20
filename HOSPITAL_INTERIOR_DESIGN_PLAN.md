# Hospital Interior Sequential Integration and Floor Design Plan

**Status:** `R03 STAGE S4 F00 PLAN V03 APPROVED AND LOCKED — S5A STAIR A PLANNING ACTIVE`

**Current implementation authority:** S2, S3B, S3C, S3D, and the S4 F00 Plan V03 are approved and locked. The approved S3D baseline combines the R05B site, R44 exterior, S3C elevator, and empty S2 floors in one adaptive desktop/PC-VR build. Final evidence is static `PASS 17/17` and corrected Windows runtime `PASS 53/53`; the approved S4 plan-only gate is `PASS 13/13`. S5A is authorized for planning and documentation only. No Blender source, FBX export, Unity asset, scene, collision, runtime, or gameplay implementation is authorized until the user reviews and explicitly approves the S5A implementation plan.

**Core rule:** only one stage may be designed, implemented, reviewed, or corrected at a time.

## 1. Why this plan replaces R02

The R02 technical prototype passed its automated checks, but its design direction was not accepted. Its repeated finished-lobby rule caused all floors to be treated as one batch and hid the actual state of the layout.

R02 remains historical evidence only. Its technical pass does not approve its visual design, repeated floor template, elevator restrictions, stair scope, or floor layouts. New work must use versioned R03 sources and must not overwrite R02 evidence.

## 2. Approved project outcome

- F00 is the only floor that will receive an actual interior layout during the current plan.
- F01–F06 remain deliberately empty until the user explicitly selects one floor for a future design stage.
- Every floor from F00 through F06 must be reachable by the operational elevator.
- The interior must be aligned and tested inside the actual hospital exterior before detailed F00 design begins.
- No floor may receive rooms, corridors, furniture, decoration, clinical equipment, or an assumed programme without a separately reviewed floor plan.

For F01–F06, “empty” means the minimum required to test the building:

- one walkable floor slab with collision;
- the actual exterior shell surrounding it;
- one operational elevator landing and safe arrival area;
- one clearly visible floor name: `F01`, `F02`, `F03`, `F04`, `F05`, or `F06`;
- an elevator spawn anchor and only the invisible safety/runtime helpers required for testing.

No visible upper-floor content is allowed beyond the floor slab, elevator landing, exterior shell, and floor name. Review lighting may be temporary and neutral. Stairs, a second working elevator, decorative trim, ceilings, partitions, rooms, furniture, props, signs other than the floor name, and floor-specific styling are deferred.

## 3. Non-negotiable workflow rules

1. Work on one stage only. Do not start geometry, interaction, or visual work from a later stage in parallel.
2. End every stage with a user-testable Unity scene and a small, named evidence set.
3. Automated validation supports a review; it never replaces user visual approval.
4. If a stage fails, correct that same stage and present it again. Do not continue forward.
5. Keep F01–F06 empty even after F00 is finished.
6. Preserve the approved exterior. Interior work must adapt to its locked scale, origin, footprint, façade, doors, windows, and floor bands.
7. Use additive scenes: one persistent building/elevator core and exactly one active interior floor scene during travel.
8. Do not silently invent a hospital layout. The user must see and approve the F00 plan before walls or rooms are built.

## 4. Locked integration datums

The actual hospital exterior is the spatial authority. All new interior roots must use metre scale, the shared world origin, identity scale, and the established building orientation.

| Floor | Elevation |
|---|---:|
| F00 | 0.0 m |
| F01 | 5.8 m |
| F02 | 9.7 m |
| F03 | 13.6 m |
| F04 | 17.5 m |
| F05 | 21.4 m |
| F06 | 25.3 m |

Stage S1 verified and approved these values against the production exterior; they are now locked R03 integration datums:

- interior test envelope: X `-34.7 to 16.7 m`, Y `-15.5 to 15.5 m`;
- elevator reference bounds: X `0.0 to 6.0 m`, Y `3.0 to 5.7 m`.

The approved S1 checks found the elevator reference contained by the upper envelope with no unexpected geometry intersections. Never repair later alignment by applying an arbitrary scale, rotation, or per-floor offset.

## 5. Runtime scene contract

The final scene structure must remain simple and inspectable:

- one integration/bootstrap scene owns the player/XR rig and loading coordinator;
- the approved production exterior additive scenes form the visible hospital building;
- one persistent E01 elevator core owns the cabin, doors, controls, and travel state;
- seven independent floor scenes exist: F00, F01, F02, F03, F04, F05, and F06;
- exactly one floor scene is active after loading completes;
- the old exterior interior-shell preview is not allowed to duplicate new interior slabs or walls;
- every scene root remains at the shared origin with unit scale.

## 6. Sequential stages and approval gates

### S0 — Plan reset and R02 baseline freeze

**Scope:** documentation and preservation only.

- Mark the repeated R02 open-lobby direction as unaccepted.
- Preserve its sources, exports, scenes, evidence, and automated reports as historical reference.
- Record the known gap: Unity currently implements only F00, transit-only F01, and F02; elevator access is not available to every floor.
- Create no new design geometry.

**Exit gate:** this corrected sequential plan is accepted.  
**Next allowed work:** S1 only.

### S1 — Actual-building integration and datum test

**Scope:** scale, position, and building fit only. No interior design.

- Create one temporary Unity integration scene that loads the production hospital exterior.
- Add minimal floor-datum/slab proxies for F00–F06 at the locked elevations.
- Place only elevator/core reference markers needed to check alignment.
- Verify the exterior entrance threshold, floor bands, windows, roof clearance, elevator shaft position, overall footprint, player eye height, and world origin.
- Check for gaps, clipping, duplicate shells, floating slabs, blocked doors, and inconsistent floor heights.

**Review deliverables:**

- one playable integration scene;
- one exterior/interior overlay view;
- one entrance-threshold view at player height;
- one longitudinal section showing all seven floor datums;
- one elevator-core alignment view;
- a short measured datum report.

**Exit gate:** the user accepts the building fit and all measured alignment checks pass.  
**Do not start S2 while any size or location issue remains.**

### S2 — Seven empty floor scenes

**Scope:** empty floors only. No elevator travel yet and no F00 layout.

- Create independent Unity floor scenes for F00–F06.
- Keep F00 as an empty placeholder at this stage.
- Give every floor a walkable slab, required collision, safe boundary, elevator arrival position, and one floor name.
- Use the real exterior shell in the integration scene instead of adding decorative interior perimeter walls.
- Remove or hide all inherited repeated-lobby decoration, room-planning objects, stair presentation, excess signage, and unapproved core details.
- Hide the six exterior-only balcony interior-backer renderers in the S2 review runtime without editing the protected exterior source or production scenes.
- Keep non-collidable structural slab undersides persistent in the review bootstrap so the one-active-floor loader still shows correct storey separation; these are neutral structural context, not finished ceilings or floor design.
- Suppress the six duplicated production balcony floor skins only in the S2 review, allowing the exact-datum S2 slab to meet the balcony glazing without a 5 cm overlap or material step.
- Align the persistent structural undersides with the production balcony soffit underside and match the adjoining neutral production finishes so floor and ceiling junctions read as continuous surfaces.

**Review deliverables:**

- one simple screenshot from the elevator arrival point on each floor;
- one playable scene allowing the reviewer to load each floor manually;
- a scene inventory proving that no unapproved visible objects exist.

**Exit gate:** the user confirms that F00–F06 are clearly named, correctly positioned, safe to stand on, and otherwise empty.

#### S2 completion record — approved 2026-08-15

- S0 reset and R02 baseline freeze were completed before production work began; R02 remains historical evidence only.
- S1 actual-building integration was completed and approved. The locked datums are F00 `0.0 m`, F01 `5.8 m`, F02 `9.7 m`, F03 `13.6 m`, F04 `17.5 m`, F05 `21.4 m`, and F06 `25.3 m`.
- S2 delivered seven identity-root empty floor scenes, a one-floor review bootstrap, exact-datum walkable slabs, invisible safety boundaries, E01 arrival anchors/safe zones, and one visible `Fxx` label on every floor.
- The review loader loads only the four approved production exterior scenes and exactly one floor. Manual keys `0–6` remain a QA tool only, not elevator travel.
- The upper-floor integration slab was extended to the measured façade and balcony edges. Exterior-only balcony backers and overlapping exterior balcony floor skins are suppressed only at S2 review runtime; protected exterior sources remain unchanged.
- Persistent, non-collidable structural undersides provide storey separation. They were aligned to the production balcony soffit and matched to adjacent finish values, removing the reported balcony floor/ceiling material and connection discontinuity.
- Final automated evidence: Unity `6000.3.20f1`; static gate `PASS 62/62`; runtime route gate `PASS 48/48`; protected S1 assets, production exterior scenes, R40 authority, and `EditorBuildSettings` remain unchanged.
- User approval was explicitly received after review of the corrected playable build. S2 is complete; its reports, screenshots, checkpoint, and Windows review player remain the approved baseline.

### S3 — Designed hospital elevator with normal door operation

**Scope:** design and implement only E01 as a hospital-stretcher elevator serving F00–F06. No floor design, E02 function, or stair work.

#### S3A — Elevator design gate

- Author one new versioned R03 E01 cabin and one reusable landing portal without restoring R01, R02, or quarantined S3 geometry.
- Use a premium clinical-future language: warm hygienic panels, brushed stainless protection/doors, charcoal structure, restrained bronze accents, dark durable flooring, and a soft 3500 K diffuser.
- Target `2.35 × 2.45 × 2.60 m` outside, `2.10 × 2.20 m` clear floor, and a `1.20 × 2.20 m` clear doorway.
- Place E01 at X `1.65 m`, Z `4.35 m` in the left half of the approved S1 elevator core and prove containment against the exterior-verified datum.
- Provide F00–F06, door-open, and door-close controls; internal and landing indicators; threshold; handrail; lower-wall protection; and four independent centre-opening door leaves.
- Present six approval renders before any Unity runtime integration.

**S3A approval record — 2026-08-15:**

- A new R03 source, separate cabin/landing FBX exports, and the six required renders are ready in `Reviews/HospitalInterior/StageI1_R03/S3A_E01_Design`.
- Independent saved-source validation passes `21/21`, including S1 left-core containment, exact floor/threshold/door datum alignment, UV/material coverage, dimensions, four separate leaves, controls, lighting, exports, and review evidence.
- The approved S2 baseline remains unchanged: `38/38` protected files match and there are `0` mismatches.
- User visual approval was explicitly received. A 20 mm threshold datum discrepancy discovered during S3B integration preparation was corrected without changing the approved visual direction or exterior envelope.

#### S3B — Full-height transparent glass shaft, observation cabin, and normal door system (approved complete)

- Synchronize two cabin leaves and the two leaves at the current landing using smooth `1.2 s` opening/closing, a `4 s` dwell, obstruction reversal, open/close commands, and positive interlocks.
- Keep all non-current landing doors closed and locked; never expose an empty shaft.
- Require all four leaves and both interlocks safe before movement; colliders travel with the leaves and must leave the full `1.20 m` passage clear.
- Landing calls reopen E01 when present or summon it to the active floor when elsewhere.

**S3B implementation record — 2026-08-15:**

- The approved S3A cabin and reusable portal are integrated at identity scale into one persistent E01 core. The cabin remains centred at X `1.65 m`, Z `4.35 m` with nominal operating-cell clearances of `0.175 m` on each side and `0.125 m` front/rear.
- The continuous E01 hoistway now spans Y `-0.20 to 28.83 m`, covers the full F00–F06 building height, and includes uninterrupted side/rear walls, seven front spandrels between landing openings, two full-height guide rails, a base closure, and an S1-authority-aligned top closure.
- After the opaque F00 shaft mass was rejected, all 11 enclosure faces were changed to low-iron blue-grey transparent laminated glass. Forty-two slim non-colliding charcoal metallic mullions and restrained bronze landing transoms coordinate the tower with the approved premium clinical-future cabin and portal design while leaving the brushed guide rails visible.
- After the cabin was reported as visually outside that enclosure, its opaque side and rear upper walls were converted to three low-iron observation-glass walls. The stainless lower impact panels, rear handrail, opaque ceiling, controls, and doors remain. Ten slim charcoal/bronze cabin-frame members distinguish the moving inner car from the stationary outer glazing and provide a `1.49 m` clear observation zone for future floor transitions.
- The complete cabin render envelope is nested inside the shaft at X `0.475 to 2.825 m`, Z `3.116 to 5.575 m`, preserving nominal left/right/front/rear clearances of `0.075/0.075/0.116/0.075 m` throughout the future F00–F06 sweep.
- S3-owned replacement slab meshes cut the actual vertical path through every floor and persistent underside at X `0.300 to 3.000 m`, Z `3.000 to 5.700 m`. Only loaded runtime instances use these aperture meshes; no protected S2 floor scene, mesh, or contract was edited.
- The nominal cabin sweep clears the shaft walls by at least `0.075 m`, guide rails by `0.040 m`, the F00 base by `0.090 m`, and the F06 top closure by `0.940 m`.
- A documented `-0.110 m` model-root offset places the cabin floor, landing threshold, and approved S2 slab on the exact `0.000 m` walk datum. The open passage measures `1.205 m` and contains no invisible collision.
- Two cabin and two F00 landing leaves run as synchronized centre-opening pairs: smooth `1.2 s` opening, `4 s` dwell, `1.2 s` closing, door-open reopening/extension, requested early close, obstruction rejection/reversal, and positive cabin/landing interlocks.
- Seven portals use the approved F00–F06 datums. The twelve non-current landing leaves remain closed, collidable, and locked. F00–F06 cabin travel controls are present but deliberately disabled until S3C.
- Static integration validation passes `37/37`; the Windows-player runtime gate passes `22/22`, including the protected observation-cabin sightline and safety boundary, cabin-in-shaft nesting, transparent-glass material/frame contract, full-height sweep, aperture meshes, active-floor render/collision swap, door cycle, obstruction reversal, and interlocks. The approved S2 baseline remains `38/38` unchanged with `0` mismatches.
- User approval was explicitly received after review of the final glass shaft and nested observation cabin. S3B is complete and locked; its final static gate is `PASS 37/37`, its Windows-player runtime gate is `PASS 22/22`, and its final review evidence/player are retained.
- S3C began only after S3B approval. The approved S3B scene and twelve locked S3B inputs remain protected and unchanged; travel is implemented in a separate S3C scene.

#### S3C — Elevator travel (approved complete)

- Keep the designed cabin and seven portals in one persistent E01 core while preserving every approved S2 floor scene.
- Keep the player physically in the moving cabin; S2 arrival anchors are validation and emergency-recovery locations only.
- Close/interlock first, retain the committed floor until the destination validates, move to the exact floor datum, transition behind closed doors, then open only the cabin and destination landing doors.
- Reject busy requests, reopen for same-floor requests, ignore invalid floors, and roll back to the last committed floor on cancellation or load failure. A failed rollback leaves the cabin enclosed in `FaultedSafe` with review-only recovery.
- Implement `S3ElevatorContract`, `S3DoorController`, `S3ElevatorController`, and `S3ElevatorButton` with explicit ready, door, moving, recovery, and fault states.

**Required functional route after approval:** `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`.

**S3C implementation record — 2026-08-16:**

- A separate S3C bootstrap scene now owns `S3ElevatorController`, seven floor-specific door controllers, all sixteen routed physical controls, the moving observation cabin, the player rig, the live cabin floor/status display, and the one-floor transition coordinator.
- Cabin floor requests require the player to be physically inside. The cabin and player receive the same continuous vertical delta; normal travel never teleports the player to an S2 arrival anchor.
- Each request closes and positively interlocks the cabin/current landing doors, loads and validates the destination behind closed doors, moves to the exact approved datum, commits exactly one active floor scene, then enables and opens only the aligned destination door set.
- The Windows runtime gate completed `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`. Every leg was accepted, exact-datum and one-active-floor checks passed, the passenger remained inside, and maximum measured passenger/cabin relative drift was below `0.000002 m`.
- Busy, invalid, outside-cabin, obstructed, and non-current landing requests reject safely. Same-floor reopen, cancellation rollback, injected load-failure rollback, injected rollback failure to enclosed `FaultedSafe`, and explicit review recovery all pass.
- The F00–F06 building structure remains visible from the observation cabin: seven render-only, non-colliding storey floor plates sit outside the E01 aperture, while the six persistent structural soffits are also shaft-cut. The only hollow region is the continuous E01 path at X `0.300..3.000 m`, Z `3.000..5.700 m`; the roof/top closure remains intact.
- The copied S3C travel scene also disables only the imported car ceiling, diffuser, trim, and ceiling collider, so the cabin itself does not block the F00–F06 hoistway view. This does not change the locked S3B source or the actual building roof/top closure above F06.
- The production `Exterior_Base` mesh `UE_EXT_GlobalStructure_OPAQUE_ALL_02` originally crossed the complete E01 footprint above F00 at Y `5.26 m`. S3C now swaps only that renderer to a generated shaft-cut copy with the same X `0.300..3.000 m`, Z `3.000..5.700 m` vertical opening; the protected exterior source and the rest of its structure remain unchanged.
- Final S3C automated evidence is Unity `6000.3.20f1`; static gate `PASS 25/25`; Windows-player runtime gate `PASS 39/39`. The protected S2 baseline remains `38` files with `0` mismatches, all `12` locked S3B inputs have `0` mismatches, and `EditorBuildSettings` remains byte-identical.
- Six visually inspected endpoint/context renders and a new Windows review player are retained in the S3C review package.
- User functional approval was explicitly received on 2026-08-18. S3C is complete and locked; S3D full technical integration with empty floors is authorized next.

**S3 exit gate:** passed. The approved designed cabin completes the route with synchronized normal doors, exact datum alignment, one active floor, correct indicators, safe obstruction/interlock behavior, no threshold gap or invisible collision, and successful failure recovery in a Windows review player.

### S3D — Full technical integration with empty floors

**Scope:** combine only the already approved site, exterior, S3C elevator, and seven empty S2 floors. Do not begin F00 design.

- Create a separate S3D bootstrap derived from the approved S3C scene; preserve all approved sources and `EditorBuildSettings` byte-for-byte.
- Load the four production exterior scenes, three approved R05B site scenes, and exactly one active S2 floor. Exclude `Exterior_InteriorShellPreview`.
- Start at the site gate and support both desktop controls and Windows OpenXR through Quest Link/Air Link from one adaptive XROrigin.
- Operate only the eight hero entrance sliding leaves automatically from their approved motion metadata; leave rear/service/roof leaves unchanged.
- Keep the site/exterior persistent while E01 completes `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`.
- Re-run S3C travel/failure checks and add integrated entrance, route, teleport-safety, recovery, collision, duplicate-shell, and scene-lifetime gates.

**S3D implementation record — 2026-08-18:**

- A separate S3D bootstrap scene was copied from the approved S3C scene. It replaces the review-only bootstrap and locomotion while retaining the one approved XROrigin, E01 implementation, exterior shaft cut, and balcony suppression behavior.
- The adaptive Windows x86_64 player uses an explicit 15-scene list: one S3D bootstrap, four production exterior scenes, three approved R05B site scenes, and seven empty S2 floors. `Exterior_InteriorShellPreview` is absent and `EditorBuildSettings` remains byte-identical at SHA-256 `62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb`.
- The desktop/PC-VR router supports automatic mode selection and `-s3dMode Desktop|PCVR`. Desktop provides WASD, Shift sprint, RMB look, and left-click interaction. PC-VR provides left-stick movement, right-stick 45-degree snap turn, A teleport, B cancel, and right-trigger interaction.
- All eight approved hero-entrance leaves are discovered through `HospitalDoorPrototype` metadata and open together over `1.2 s`, dwell for `2 s` after clearance, close, and reverse when occupancy returns. Rear, service, and roof doors are not operated by S3D.
- The final static gate passes `17/17`. Desktop review exposed integration defects in rendered-lawn support, the obsolete R43 front perimeter collider, the F00 entrance safety boundary, the HUD bootstrap reference, the F01-F06 main-glass floor edge, and elevator targeting. S3D-only corrections were implemented without changing approved sources. The corrected Windows runtime gate passes `53/53`, including seven persistent environment scenes plus one floor, automatic entrance passage/close/reversal, approved route support, the full `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00` route, S3C failure cases, persistent-scene lifetime, teleport containment, emergency-only recovery, corrected upper-floor edge closure, assisted interaction targeting, and final return/exit.
- The lawn correction uses one editor-cooked MeshCollider from the exact `S05S_S5_EXP_LandscapeHardscape_S05S_S4B_ContinuousLawnVisual` source mesh (`12` triangles), aligned byte-for-byte in geometry and bounds with the rendered lawn before Unity static batching makes its runtime combined mesh non-readable. The real desktop CharacterController is physically stopped at all `12/12` distributed accessible-lawn samples; the blade occupancy grid remains diagnostic only. S3D disables only the obsolete `COL_Perimeter_Front`, retains the actual R05B perimeter, and remaps F00's two front safety-boundary segments around the outer-door metadata. Capsule and CharacterController traversal now pass from both the official drop-off and the screenshot-side right lawn, then through lanes X `-6.44`, `-4.00`, and `-1.56` to Z `-17.50` with zero blockers. Lawn remains excluded from the approved teleport allowlist. The adaptive rig's bootstrap reference is serialized, eliminating the false `Bootstrap missing` HUD state.
- Six distinct camera-rendered evidence frames cover gate spawn, lobby approach, automatic entrance passage, F00 elevator arrival, F06 arrival, and F00 return/exit. Protected counts remain S2 `38`, S3B `12`, S3C `17`, and Stage 6A `294` exact plus one pre-existing semantically validated material-serialization normalization.
- The review player is `Exports/HospitalInterior/StageI1_R03_S3D_TechnicalIntegration/HospitalInterior_S3D_R03_TechnicalIntegration.exe`. The user completed the desktop and Quest Link/Air Link routes and explicitly approved S3D on 2026-08-19. This functional approval does not make a standalone Quest/Android or final headset-performance claim.

**S3D approval record — 2026-08-19:**

- User decision: `APPROVE S3D`.
- S3D is complete and locked at final static `PASS 17/17` and corrected Windows runtime `PASS 53/53`.
- The S3D scene, Windows player, gates, build record, and protected input counts are frozen in `StageI1_R03_S3D_UserApproval.md`.
- S4 F00 plan approval is authorized next. No F00 three-dimensional implementation is authorized before the S4 plan is explicitly approved.

**Exit gate:** automated desktop gates pass, the user completes one desktop route and one Quest Link/Air Link route, and the user explicitly approves the combined empty-floor technical baseline. S4 remains blocked until then.

### S4 — F00 plan approval

**Scope:** the ground-floor layout on paper/plan view only. No finished 3D interior.

- Start from the real entrance, elevator, exterior envelope, columns/openings, and circulation constraints proven in S1.
- Produce a clearly labelled F00 plan that shows proposed zones and circulation without decorative detail.
- Explain what every proposed area is for; do not use unexplained rooms or generic hospital filler.
- Revise the plan until the user understands and approves the complete F00 layout.

**S4 design decision — approved 2026-08-19:**

- Candidate V01, an education-facility plan with fourteen zones and real-world support functions, was rejected as unnecessarily complex for the game. It remains historical evidence only and must never authorize S5.
- Candidate V02 simplified F00 to one open welcome lobby, but it omitted the space required by the previously designed playable staircase. It was not approved and is superseded by V03.
- Candidate V03 is the approved and locked F00 plan. The complete `2,598.865 m²` footprint remains one open welcome lobby hall with no programme rooms, while `A06` reserves the expanded playable Stair A footprint at X `-29.0..-22.0`, Z `-5.0..5.0` (`70.0 m²`).
- V03 retains the five minimal lobby anchors. The west `A02` seating cluster moves south so it does not overlap Stair A or its east-side clear landing.
- `R01` remains the `3.00 m` clear-target entrance-to-E01 route. `R02` adds a `2.00 m` clear-target branch from the open lobby to the Stair A door. Upper-floor purposes remain undecided; Stair A is shared building-circulation infrastructure only.
- The V03 plan-only gate passes `13/13`: the open lobby matches the locked footprint, the exact prior Stair A footprint and landing are reserved, no other anchor overlaps it, both routes stay inside F00, E01 remains clear, the rejected programme is absent, the staircase-first stage order is recorded, and S4 contains no 3D geometry.
- User decision: `APPROVE S4 F00 PLAN V03`.
- Approved evidence remains under `Reviews/HospitalInterior/StageI1_R03/S4_F00_Plan/`. Superseded V01/V02 review material, the correction history, and the completed review checklist are retained under `Archive/HospitalInterior/StageI1_R03/S4_F00_Plan/Superseded_2026-08-19/`.
- S4 approval authorizes S5A planning only. Staircase implementation remains blocked until the user approves the S5A implementation plan.

**Exit gate:** passed. The user explicitly approved F00 Plan V03 on 2026-08-19.
**The approved V03 layout and Stair A reservation are now locked inputs for S5A planning.**

### S5A — Full Stair A integration (APPROVED)

**Implementation status:** FULLY APPROVED BY USER (`APPROVE S5A STAIR A`, 2026-08-20).

**Completed implementation:**
- Continuous two-flight switchback Stair A created from F00 through F06 (12 flights, 164 steps, 7 floor landings, 6 mid-landings, 7 automatic doors, F06 top closure).
- Expanded A06 envelope to 7.0 × 10.0 m (X `[-29.0, -22.0]`, Z `[-5.0, 5.0]`, 70.0 m² area) for 100% clear 180° VR U-turn clearance.
- Mid-landing lane-blocking guard bars removed; 1.80 m lanes and 2.40 m deep landings are 100% clear.
- Elevator coexistence maintained; player movement inside E01 cabin during travel is fully unblocked.
- All automated gates passed: Blender `40/40`, Unity Static `31/31`, Unity Runtime `31/31`.

**Implementation exit gate:** PASSED & APPROVED. Stage S5B is now UNBLOCKED.

### S5B — Remaining F00 greybox walkthrough

**Scope:** approved F00 layout in simple 3D geometry.

- Starting from the approved S5A staircase integration, build only the remaining walls, openings, major fixed anchors, and circulation volumes shown in the approved S4 plan.
- Use neutral materials and temporary lighting.
- Integrate the remaining greybox with the actual exterior entrance, working elevator, and approved Stair A.
- Test human scale, visibility, clearances, walking distances, collision, and entrance/elevator/stair flow.
- Keep F01–F06 unchanged and empty except for the approved Stair A core and landings.

**Exit gate:** the user approves the F00 spatial layout during a playable walkthrough.

### S6 — F00 visual design and finish

**Scope:** visual treatment of the already approved F00 greybox only.

- Apply materials, architectural lighting, signage, fixtures, and approved furniture without changing the accepted layout silently.
- Match the established hospital exterior language where appropriate.
- Present visual changes in small review groups rather than one large reveal.
- Keep all upper floors unchanged and empty.

**Exit gate:** the user approves the finished F00 visual design.

### S7 — Final combined-building regression

**Scope:** post-design production integration and end-to-end verification.

- Load the approved site/exterior configuration, finished F00, empty F01–F06, persistent E01, and approved Stair A together.
- Test exterior approach → hospital entrance → finished F00 → elevator → every empty floor → Stair A return to F00 → exterior exit.
- Confirm there are no duplicated shells, blocked entrances, lighting conflicts, origin drift, floor offsets, or scene-loading leaks.
- Re-run desktop and XR traversal/collision checks without changing approved design.

**Exit gate:** the complete hospital with finished F00 is testable as one building and the user accepts the combined result.

### S8 — Optional future floor design, one floor at a time

F01–F06 remain empty indefinitely unless the user selects a specific floor. A selected floor must repeat the same three-step pattern used for F00:

1. labelled plan approval;
2. greybox walkthrough approval;
3. visual-finish approval and combined-building regression.

Finish and approve one floor before discussing or building the next. Batch design of several floors is not allowed.

## 7. Review controls

- `WASD`: walk at 2 m/s.
- Hold `Left Shift` with `WASD`: sprint at 4 m/s.
- Hold `Right Mouse Button` and move the mouse: look.
- Centre the reticle on a target and press `Left Mouse Button`: interact.
- `Y`: show or hide simulator help.

## 8. Versioned authority for new work

- Frozen exterior authority: `ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend`
- Production exterior Unity scenes: `Unity/AnatomyXR/Assets/Hospital/Scenes/Additive/`
- Historical rejected-direction interior work: existing R01/R02 paths
- New Blender sources: `ArtSource/Environment/Blender/HospitalInterior/` with explicit R03 filenames
- New exports: `Exports/HospitalInterior/StageI0_R03/`
- New Unity implementation: `Unity/AnatomyXR/Assets/HospitalInterior/PreProduction/I1/R03/`
- New review evidence: `Reviews/HospitalInterior/StageI1_R03/`

Every checkpoint must state the active stage, what changed, what was deliberately deferred, the exact test scene, and whether user approval was received.
