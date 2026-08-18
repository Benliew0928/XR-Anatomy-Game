# Hospital Exterior Development Progress

Last updated: 2026-08-11  
Current phase: Stage 5 and Stage 5S are frozen; Stage 6A R01 has passed its technical gate and is ready for the required physical Quest Link/Air Link review  
Current working checkpoint: `HospitalExterior_Stage06A_R01_Checkpoint.md` at `READY_FOR_HEADSET_REVIEW`; Stage 6A approval and Stage 6B remain blocked on the physical session decision

## Project scope

The wider project is an anatomy/XR hospital game, but the current production focus is only the hospital environment. Use `ANATOMY_XR_FYP_APPROACH.md` for the core game idea, not as a required environment-development workflow.

The approved hospital massing and design language must not be redesigned unless a genuine modelling defect is discovered.

## Current status

| Stage | Status | Notes |
|---|---|---|
| 1 — Reference and design analysis | Complete | Hospital direction, dimensions and design language locked. |
| 2 — Architectural massing | Complete / approved | Seven levels, tower massing, entrance composition, side/rear/roof massing and 2.7 m façade grid locked. |
| 2B — Balcony stack and access doors | Complete / approved | All balconies and two access doors per balcony are present in R11. |
| 3A — Front tower production façade | Complete / approved | R15 is the protected source for Stage 3B. |
| 3B — Hero entrance and ground floor | Complete / approved | R20 includes the approved left-corner closure and is the protected Stage 3C source. |
| 3C — Sides, rear and roof | Complete / approved | R24 is frozen as the protected Stage 3D source. |
| 3D — Exterior materials and UV system | Complete / approved | R28 is frozen as the protected Stage 3E source. |
| 3E — Exterior interior-shell pass | Complete / approved | R32 remains the audited derived shell checkpoint; full interior design is deferred. |
| 3F — Architectural lighting and presentation | Complete / approved | R36 is frozen as the approved Stage 4 source; still-image QA accepted despite viewport-GPU limits. |
| Stage 4 — Performance restructuring, Unity export preparation and final QA | Complete / approved | R40 is the approved Unity-facing authority. It restores the approved floor-band system, closes the rear/right-wing return, hides editor-only pivot overlays, and contains 237 renderer candidates with zero cameras/lights/modifiers. |
| Stage 5 — Unity integration | Complete — R41–R44 PASS | Unity foundation, ten-zone/five-scene additive assembly, 30 shared materials, twelve doors, simple collision, scale checks, PC proxy profile and final integration QA are frozen. |
| Stage 5S — Hospital territory/site expansion | Complete / approved — S6 PASS and frozen | The exact approved R05B hash is integrated under a versioned site-only Unity hierarchy in three independently unloadable scenes. The S6 gate passes `16/16`; post-integration S5B `64/64` and R05 `87/87` regressions pass. Roads, paths, parking, crossings, landscaping and source FBXs remain unchanged. The user approved the S6 visual checkpoint on 2026-08-11. |
| Stage 6 — VR traversal and real-time optimization | 6A technical gate PASS / headset review pending | R01 now provides the isolated floor-tracked PC-VR traversal harness, exact 20-surface teleport allowlist, ordered route recording, three load profiles and an explicit nine-scene development build. Physical Quest evidence and an `APPROVED` or `CORRECTION_REQUIRED` decision are still required before 6B. |

## Locked architectural decisions

- Seven levels total: one 5.6 m ground floor and six 3.9 m upper floors.
- Main tower is approximately 54 × 34 m; overall composition is approximately 72 m.
- Production façade module is exactly 2.7 m with a 1.35 m internal subdivision.
- The approved R11 balconies are open, walkable terraces with minimal exterior glass guards.
- Each balcony retains two correctly placed room-access doors.
- Balcony geometry and R11 doors are protected from later façade edits.
- The continuous white horizontal bands and approved massing remain the geometric authority.
- R28 exterior materials use the approved Stage 3D PBR and UV system. R32 adds only derived non-emissive interior shells; R36 adds the controlled Stage 3F lighting overrides without altering either approved material library.

## Stage 3A work completed

### Gate 1 — Standard production bay

- Converted the approved 2.7 m test bay into a reusable production module.
- Preserved exact 1.35 m glazing divisions.
- Added recessed glazing, real mullions, frames and opaque-spandrel treatment.
- Matched the locked 3.9 m floor rhythm and existing façade grid.

### Gate 2 — Transition modules

- Added left balcony-adjacent transitions.
- Added right glass transitions.
- Added the front-facing opaque/service variant.
- Added one immediate 2.7 m right-corner return on every upper floor.
- Preserved balcony openings, guards, terraces and access doors.

The white panel at the right-front corner is an intentional opaque service/corner pier. It is not white glass and is not a rule for every glazed bay. It closes the curtain-wall corner, provides a clean termination for mullions and can conceal structure, services or privacy walls. Normal façade bays remain glazed.

### Gate 3 — Front propagation

- Replaced the front-facing upper-floor proxy façade with approved production modules.
- Propagated 84 standard bays across six upper floors.
- Completed the façade around the approved balcony stack.
- Completed only the immediate visible corner returns; full side and rear propagation was intentionally not performed.
- Resolved the original right-front gap by aligning the return glass with the established side-glass plane and trimming the first overlapping proxy strip.

### Stage 3A validation result

Automated R15 audit status: **PASS**

- 102 front proxy objects removed.
- 84 standard bays and 912 standard components present.
- 1,230 Stage 3A objects present.
- 552 protected balcony/door objects verified unchanged.
- 234 side/rear proxy objects intentionally retained.
- No exact duplicate geometry found.
- Right-corner geometric continuity passed.
- Nine review renders archived.

Audit record: `Archive/HospitalExterior/Reviews/Stage03A_FrontFacade/Stage03A_R15_FrontFacadeCornerResolved_Audit.json`

## Stage 3B work completed

### Gate 1 — Entrance and vestibule

- Rebuilt the outer and inner automatic sliding-door planes as separate production assemblies.
- Created eight animation-ready leaf roots with independent glass and frame components for future Unity movement.
- Added realistic door and sidelight glass thickness, head tracks, compact sensor housings and 12 mm flush thresholds.
- Refined the 8.1 m portal, clerestory, vestibule ceiling and non-emissive entrance signage.

### Gate 2 — Lobby curtain wall

- Replaced the visible lobby and projecting atrium proxies with production curtain-wall geometry.
- Added recessed 28 mm glazing, primary and secondary mullions, transoms, perimeter frames and shadow seals.
- Rebuilt both curved atrium returns and the shallow right transition with smooth, physically thick geometry.
- Rebuilt the visible structural-column hierarchy while retaining only the approved simplified lobby-depth geometry.

### Gate 3 — Canopy, ribbon and ground-floor composition

- Added a production canopy soffit grid, perimeter reveals and 24 instanced non-emissive downlight housings.
- Preserved the approved canopy and ribbon dimensions while refining ribbon smoothing and bevel continuity.
- Added a physical recessed ribbon LED channel and non-emissive diffuser; final lighting remains Stage 3F.
- Replaced the visible left-podium, connector and right-wing front proxies with production glazing and cladding details.
- Completed one immediate 2.7 m right-wing return and handed it cleanly to the retained Stage 3C side proxy.

### Stage 3B validation result

Automated R19 audit status: **PASS**

- 382 Stage 3B production objects present.
- Eight sliding-door animation roots verified.
- 82 production curtain-glass components and six curved glass components verified.
- 42 primary mullions and 24 instanced downlight housings present.
- No stale ground-front proxy, exact duplicate geometry or non-unit scale remains in the Stage 3B collection.
- R15 upper façade, R11 balconies/doors, continuous bands and tower massing are unchanged.
- Fifteen review renders archived.

Audit record: `Archive/HospitalExterior/Reviews/Stage03B_HeroEntrance/Stage03B_R19_HeroEntrance_Audit.json`

The user approved Stage 3B after the additive R20 left-corner closure. R20 is the immutable source for Stage 3C; the entrance, canopy, ribbon, front façade, balcony geometry and all balcony access doors remain protected.

## Stage 3C work completed

### Gate 1 — Side façades

- Replaced all remaining left/right upper glass and mullion proxies with recessed production curtain-wall components.
- Continued the locked 2.7 m / 1.35 m system and preserved all seven continuous tower bands.
- Completed the full left ground-floor glazing border system and the full right-wing side enclosure.
- Rebuilt both service cores as controlled opaque cladding with recessed slot glazing and aligned joints.
- Removed the retained right-side proxy intersection that caused the sharp color/shading seam.

### Gate 2 — Rear and service elevation

- Replaced the complete rear upper proxy façade with production glazing, mullions, pressure caps and frames.
- Rebuilt the staff and clinical glazing, loading door, service/fire exits, thresholds and service canopy.
- Closed both rear corners and the rear of the right wing.
- Rebuilt the former floating rear black object as a smaller framed ventilation louver physically integrated into the right-wing rear service wall.
- Kept four rear/roof door roots separate and ready for possible future Unity animation.

### Gate 3 — Roof and exterior completion

- Replaced the solid roof proxies with four independent parapet walls and caps around a usable recessed roof surface.
- Added a four-sided mechanical screen, roof-access headhouse, closed access door and visible drainage/scupper details.
- Completed the right-wing side/rear parapets, caps and roof-edge transitions.
- Removed every retained side, rear and roof façade proxy while keeping working materials and future-stage exclusions intact.

### Stage 3C validation result

Automated R24 audit status: **PASS**

- 2,296 Stage 3C production/review objects present.
- 636 upper production-glass components, 76 ground-glass components and 336 upper mullions verified.
- 33 physically integrated louver components and four animation-ready service/access roots verified.
- No stale side/rear/roof proxy, exact duplicate geometry or non-unit scale remains.
- R20 entrance and corner correction, R15 front façade, R11 balconies/doors and continuous tower bands are unchanged.
- Fifteen Stage 3C review renders archived.

Audit record: `Archive/HospitalExterior/Reviews/Stage03C_AllSidesRoof/Stage03C_R24_AllSidesRoof_Audit.json`

## Resolved Stage 3C conditions

The sharp color/shading intersection visible on the right-side horizontal bands is not an intended final architectural joint. The band itself is one continuous mesh using one material. The visible boundary is produced by the retained Stage 2 right-side service/proxy geometry and viewport contact/ambient-occlusion shading.

R24 removes that intersecting proxy geometry and replaces the side elevation with production modules. The right-side seam is therefore resolved. The previously exposed right-wing rear and floating loading louver are also enclosed and integrated in R24.

## Stage 3D work completed

### Gate 1 — Reusable exterior PBR library

- Upgraded 19 shared exterior materials in place so all approved modular instances retain their existing assignments.
- Added physically based cladding, aluminium, architectural glass, balcony guard glass, concrete, soffit, entrance metal, stone, signage and LED-diffuser responses.
- Kept LED/signage emission disabled for Stage 3F and retained the simplified lobby-depth materials for Stage 3E.
- Used deterministic procedural surface variation with no missing external image dependencies.

### Gate 2 — UV and trim system

- Added `UV_Exterior_1m` to all 991 unique R24 scene meshes, covering 4,621 mesh objects.
- Standardized material metadata at 512 px/m for later Unity texture baking and export.
- Added `UV_Trim_0_1` to all 359 architectural trim meshes while preserving existing UV layers.
- Preserved shared mesh datablocks and did not alter approved vertices, polygons, object transforms or hierarchy.

### Gate 3 — Propagation and material review

- Verified material coverage across the front, both sides, rear/service elevation, roof, entrance, ribbon and balcony stack.
- Produced 15 neutral-daylight 1200 × 800 review renders, including player-eye, close-detail, all-side and roof views.
- Corrected the initial underexposed look-development pass so glazing, bronze metal, rear cladding and roof materials remain inspectable without architectural lighting.

### Stage 3D validation result

Automated R28 audit status: **PASS**

- All 19 PBR materials exist, are used, and contain valid Principled BSDF/output graphs.
- All 991 unique source meshes have complete 1 m UV coverage; all 359 trim meshes have trim UV coverage.
- No unexpected or unintended null material remains in the exterior scope.
- R24 remains byte-for-byte unchanged and all 4,761 approved source objects retain identical geometry and transforms.
- Stage 3E interior shells, Stage 3F architectural lighting and Unity integration remain explicitly deferred.

Audit record: `Archive/HospitalExterior/Reviews/Stage03D_ExteriorMaterialsUV/Stage03D_R28_ExteriorMaterialsUV_Audit.json`

## Stage 3E work completed

### Interior-shell system

- Added 330 simplified shell objects behind exterior glazing using 24 reusable meshes and seven non-emissive interior materials.
- Completed all six upper floors with controlled perimeter depth, corridor cores, privacy shades, room partitions and sparse silhouettes.
- Added a separate ground-floor/lobby system with lobby depth walls, portals, feature fins, ground partitions, floor/ceiling surfaces and restrained furniture silhouettes.
- Kept every shell inside the approved envelope and trimmed front/left shells away from the protected balcony stack.
- Hid the superseded upper-interior proxy only in the derived Stage 3E file.

### Stage 3E validation result

Automated R32 audit status: **PASS**

- 330 shell objects, 24 unique shell meshes and seven used non-emissive materials verified.
- All shell meshes have `UV_Interior_1m`; all shell objects use unit scale.
- No exact duplicate shell, envelope violation or balcony intrusion remains.
- Approved R28 geometry, material assignments and the 19-material Stage 3D PBR library remain unchanged.
- Twelve 1200 × 800 Stage 3E review renders were visually checked before Stage 3F began.

Audit record: `Archive/HospitalExterior/Reviews/Stage03E_InteriorShells/Stage03E_R32_InteriorShells_Audit.json`

## Stage 3F work completed

### Architectural lighting and presentation

- Activated the approved ribbon diffuser, 24 canopy downlights, both hospital signs, six façade accent bands, nine lobby luminaires and six selected balcony diffusers with controlled material overrides.
- Added 58 documented architectural lights: 24 canopy, five ribbon wash, six lobby, 12 selected-room, three rear-service, two right-wing and six balcony lights.
- Added 12 emissive room panels across levels 2–7 so the tower reads selectively occupied rather than uniformly illuminated.
- Added four reusable emission materials with documented 3000 K, 3200 K and 3500 K roles; all architectural lights carry baked-lighting metadata for later Unity profiling.
- Produced eight close inspection views and eight matched daylight/evening pairs at 1200 × 800.
- Raised the Eevee shadow atlas to 1 GB after whole-building QA exposed the inherited 512 MB capacity limit; the final render pass has zero shadow-buffer warnings.

### Stage 3F validation result

Automated R36 audit status: **PASS**

- 58/58 architectural lights and all seven intended light categories verified.
- Exactly 48 controlled source material overrides and six visibility overrides verified.
- Stage 3E shell geometry, shell assignments, seven Stage 3E materials and 19 Stage 3D materials remain unchanged.
- Sixteen review cameras, six isolated review lights and all 24 final PNGs verified.
- No duplicate architectural light, invalid CCT, invalid bake metadata or out-of-range energy remains.

Audit record: `Archive/HospitalExterior/Reviews/Stage03EF_CombinedReview/Stage03F_R36_ArchitecturalLighting_Audit.json`

## Approval disposition — Stage 3E + 3F

The user accepted the Stage 3E interior-shell presentation and Stage 3F lighting based on the still-image review, with viewport smoothness limited by available GPU performance. R36 is frozen as the approved Stage 4 source.

The interior shells remain presentation placeholders only. Do not expand them into a full floor plan, medical-room set, gameplay interior or detailed hospital simulation during Stage 4.

## Pre-Stage 4 workspace cleanup completed

- Moved 29 historical or unused files into organized archive folders; deleted nothing.
- Reduced the active Blender source folder to R36 and its live `.blend1` recovery copy.
- Moved R11, R15, R20 and R24 to `Archive/HospitalExterior/Blender/ApprovedMilestones/`.
- Moved the audited and post-approval R28/R32 saves into their approved milestone folders.
- Moved superseded R10, R14 and R19 revisions into `LegacyRevisions/Stage02B-03C_ActiveCleanup/`.
- Moved all 13 completed Stage 3 scripts into `Archive/HospitalExterior/Tools/CompletedStages/Stage03/`; the active `Tools/` folder is reserved for Stage 4.
- Moved the unrelated motion-rig paper from the workspace root into `Archive/ProjectReferences/`.
- Verified that later R28/R32/R36 saves are semantically identical to their audited snapshots; only the harmless `assets reported` add-on property differs.

Cleanup record: `Archive/HospitalExterior/Docs/HospitalExterior_PreStage04_WorkspaceCleanup.md`

## Pre-Stage 4 performance diagnosis and user direction

The approved R36 scene is visually complete enough to stop adding exterior content, but its saved scene structure is inefficient for continued authoring or direct Unity use. A Blender 5.1.2 read-only audit recorded:

- 5,218 scene objects, including 4,963 mesh objects;
- 1,016 unique scene meshes and strong reuse of repeated façade meshes;
- approximately 648,960 evaluated triangles after modifiers, which is reasonable for the complete exterior;
- 3,633 live viewport/render Bevel modifiers;
- 97 persistent lights and 131 persistent cameras accumulated across production and review collections;
- 37 materials, two images and a compact 1.65 MB `.blend` file.

The primary bottleneck is therefore object, modifier, light, shadow and draw-submission overhead rather than raw polygon count or texture memory. The user has confirmed that the saved review cameras, review lights and Stage 3F presentation lighting were created for AI-agent review and are not required in the continuing production master.

Stage 4 must follow these decisions:

- Preserve R36 unchanged as the complete approved presentation archive and recovery source.
- Do not carry persistent review cameras, review lights or architectural presentation-light datablocks into the final Unity-facing production master or any Unity export.
- Preserve genuine architectural fixture meshes where they are part of the building, but treat Blender light datablocks and evening-presentation setup as archived review data rather than runtime content.
- Generate any future audit camera, neutral review light or presentation rig temporarily in a disposable review copy or background render session. Save review images externally, then remove the temporary review objects before saving a production checkpoint.
- Keep the production file in a lightweight authoring state by default. Full rendered presentation must never be the required working mode.
- Stop expanding the exterior master with real rooms, furniture or gameplay content. Future interiors use separate modular source files and Unity prefabs/scenes.
- Optimize the Unity-facing copies deliberately; do not import the current 4,963-object hierarchy and hope that later optimization will solve it.

## Stage 4 completed — four automatically gated rounds

The approved architecture and material appearance remained the authority throughout. Each round passed its own pipeline gate and a separate validator before the next round began.

### Round 1 — R37 performance cleanup: PASS

- Captured and preserved the R36 checksum.
- Removed 131 cameras, 97 Blender lights, 12 Stage 3F presentation panels, four presentation emission materials, and verified review/test/proxy payload.
- Restored 48 source material assignments and six source visibility states.
- Preserved the protected geometry semantic hash exactly.
- Disabled live viewport evaluation on 3,531 retained Bevel modifiers without changing evaluated export geometry.
- Saved `HospitalExterior_Stage04_R37_Gate1_MasterAuditCleanup.blend` in the archived Stage 4 gates folder.

### Round 2 — ground-entrance export prototype: PASS

- Assigned every production object to exactly one of ten spatial/runtime zones.
- Proved the export method on the 450-object ground entrance, producing 60 mesh renderers plus eight door-root empties.
- Exported and re-imported the prototype FBX with matching bounds, vertices, triangles, materials, UVs, normals, scale, transparency separation, and functional door hierarchy.
- Saved `HospitalExterior_Stage04_Round2_GroundEntrancePrototype.blend` as an archived checkpoint.

### Round 3 — R38 complete Unity export build: PASS

- Applied the approved prototype method to all ten zones.
- Baked export geometry and joined compatible static content by zone, culling cell, material, and rendering role.
- Kept glass separate and retained all twelve animation-ready door roots.
- Exported ten FBX files and re-imported them together into a blank verification scene.
- Verified common origin/metre scale, alignment, materials, UVs, normals, pivots, and zero exported cameras, lights, or modifiers.
- Achieved 235 renderer candidates and 621,012 triangles without visual decimation or architecture changes.
- Saved `HospitalExterior_Stage04_R38_Gate2_ExportReadyQA.blend` in the archived Stage 4 gates folder.

### Round 4 — R39 final QA and master freeze: PASS

- Created `HospitalExterior_Stage04_R39_MASTER.blend` with only the `UNITY_EXPORT` hierarchy, required materials/UVs, and interactive door roots.
- Final scene contains 247 objects, 235 meshes, twelve door-root empties, 29 materials, zero cameras, zero lights, and zero live modifiers.
- Re-ran full FBX round-trip and independent Stage 4 validation with zero failures.
- Generated and visually checked front, left, right, rear, roof, entrance, aerial, and player-eye neutral-daylight views.
- Removed all temporary QA cameras/lights before the final save.
- Wrote the zone/export manifests, four round audits, independent validation reports, and checksums.

### Combined-review corrections — R37B/R38B/R40: PASS

- User close inspection revealed that the collection named `PROXY_FACADE` actually contained seven approved continuous floor bands, seven shadow reveals, and six façade accent strips. R37B protects and renames this twenty-object architectural system instead of deleting it.
- Restored bands now conceal the temporary sofa/bed silhouettes at every inter-floor edge.
- Added an opaque, UV-mapped and trimmed closure to the open right-wing/rear service return without changing the approved massing.
- Confirmed the small viewport crosses and diagonal lines were non-rendering animation-root/relationship overlays, not stray mesh geometry. R40 hides those overlays and uses compact 0.15 m empty displays while preserving all twelve door pivots.
- Rebuilt the prototype, R38B, all ten FBXs, and R40 from the corrected source.
- Corrected final scene contains 249 objects, 237 meshes, twelve door-root empties, 30 materials, 644,984 triangles, zero cameras, zero lights, and zero live modifiers.
- All corrected gates, FBX round trips, eight standard views, and three targeted close views passed.

### Post-approval R40 authority clarification for Stage 5

- After the Stage 4 documentation freeze, the user intentionally made one later geometry edit in the active R40 file and confirmed on 2026-08-09 that the newest file is the correct authority.
- The active user-confirmed R40 SHA-256 is `f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126`; the exact frozen `.blend1` snapshot remains `0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58`. A 249-object structural comparison is identical. Its Stage 5 baseline is 249 objects, 237 mesh-renderer candidates, 644,996 triangles, 30 materials, twelve door roots, zero cameras/lights/modifiers.
- The twelve-triangle increase is isolated to `UE_EXT_GlobalStructure_OPAQUE_ALL_02`, where the intentional light-grey return/band geometry was extended. No other zone changed.
- Stage 5 uses the re-frozen ten-file set in `Exports/HospitalExterior/Stage05_R41_Input/`. `Hospital_EXT_GlobalStructure.fbx` was re-exported from the newest R40; the other nine FBXs remain checksum-identical to the corrected Stage 4 exports.
- The original 644,984-triangle R40 hash and corrected Stage 4 export set remain historical Stage 4 evidence; they are not the current Stage 5 import authority.

### Stage 4 boundaries

- No exterior redesign or new façade detailing. Stage 3F presentation lighting is archived rather than redesigned.
- No full interior planning or medical-room modelling.
- No Unity import, collider production, LOD production or headset optimization.
- No blind global join, purge, transform application or modifier application on the editable source. Optimization operations are permitted only on verified `UNITY_EXPORT` duplicates and must pass regression validation.
- No new persistent camera/light accumulation for agent review. Temporary QA objects must be removed before every Stage 4 production save.

## Immediate next action — complete the Stage 6A physical Quest review

Stage 6A R01 is implemented under `Assets/Hospital/Stage06A/R01/`. Its independent technical gate passes `10/10`; the expanded protected-input manifest rehashes 295 frozen scene/source/importer/material/prefab/LOD/grass/XR/package/project files with zero mismatches; and the explicit bootstrap-plus-eight Windows development build passes without changing the frozen eight-scene `EditorBuildSettings`. The immediate action is to run `AnatomyXR_Stage06A_R01.exe` on one available Quest Link or Air Link setup, complete Gate → Lobby Forecourt → Visitor Parking → Lake Path → Rear Service → Gate Return, import the session evidence and choose `APPROVED` or `CORRECTION_REQUIRED`. Stage 6B starts only after `APPROVED`; real frame-time acceptance remains Stage 6B–6D.

### Stage 5S coordination boundary

- Parent plan: this file remains the controlling hospital roadmap.
- Child plan: `HOSPITAL_SITE_EXPANSION_DESIGN_PLAN.md` controls only the site expansion from approved design through its Unity integration checkpoint.
- Frozen input: newest R40 plus all R41–R44 Stage 5 records remain immutable and reproducible without Stage 5S loaded.
- New content: the coordinated R05B site authority is integrated as versioned FBXs, prefabs, 20 controlled site materials and three additive site scenes without changing frozen hospital ownership.
- Merge-back state: authority/checksums, asset and scene matrices, site-only/combined counts, collision/LOD/grass inventories, visual evidence and limitations are recorded here and in the frozen S6 checkpoint.
- Return condition: satisfied on 2026-08-11. Stage 5S is complete and Stage 6 must profile hospital-only, site-only and hospital-plus-site states on Quest Link/Air Link.

## Remaining roadmap

### Stage 3C — Side, rear and roof completion (complete / approved)

- Propagate side façade modules and resolve all front-to-side transitions.
- Remove the right-side proxy color/shading intersection.
- Complete service-core treatment, rear entrances, fire exits, loading/service access, louvers and controlled opaque areas.
- Complete parapets, roof-access logic, screening and visible roof-edge details.

### Stage 3D — Exterior materials and UV system (complete / approved)

- Create reusable PBR materials for cladding, aluminium, architectural glass, balcony glass, concrete, soffits, LEDs and entrances.
- Use tiled materials, trim sheets, reusable UVs and consistent texel density.
- Keep the hospital clean and premium; avoid excessive weathering.

### Stage 3E — Exterior interior-shell pass (complete / approved)

- Add only the simplified floors, ceilings, columns, walls and silhouettes needed behind exterior glazing.
- Give the lobby convincing depth.
- Keep temporary shells in a separate `INTERIOR_SHELL` collection.

### Stage 3F — Architectural lighting and presentation (complete / approved)

- Add ribbon LEDs, façade accents, canopy downlights, lobby lighting and selective room lighting.
- Produce neutral-daylight and evening/golden-hour reviews.
- Use lighting to present completed architecture, not conceal defects.

### Stage 4 — Performance restructuring, Unity export preparation and final QA (complete / approved)

- Round 1 / R37: performance cleanup and lightweight authoring state passed.
- Round 2: ground-entrance export prototype and FBX round-trip passed.
- Round 3 / R38: ten-zone Unity export build and complete round-trip passed.
- Round 4 / R39: final independent validation and eight-view visual QA passed.
- Combined-review correction / R40: restored missing architectural bands, enclosed the rear return, rebuilt all exports, and passed targeted visual QA. R39 is superseded.

### Stage 5 — Unity integration (complete; R41–R44 PASS and frozen)

Stage 5 is a four-round, automatically gated Unity integration stage. It establishes a clean, measured runtime foundation from R40; it does **not** begin full room production, medical gameplay, final headset optimization, or an uncontrolled expansion of the Unity scene.

#### Authority and non-negotiable performance rules

- The only geometry authority is the newest user-confirmed `HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend` and the ten frozen FBXs in `Exports/HospitalExterior/Stage05_R41_Input/`. Never import a `.blend`, R39, the historical corrected Stage 4 FBX set, an original `Stage04` FBX, or a prototype FBX into a production Unity scene.
- Preserve R40 unchanged. A Unity problem is classified first as an import/material/prefab issue or a genuine source-geometry issue. Only the latter can create a new derived Blender correction checkpoint; no work may overwrite R40.
- Preserve the ten source zones: global structure, ground entrance, front tower, left façade/podium, right façade/wing, rear/service, balconies, roof/service, interior shell, and interactive doors.
- Treat the user-confirmed R40 baseline as a budget: 237 mesh-renderer candidates, 644,996 source triangles, 30 material assignments, twelve door roots, zero cameras and zero Blender lights. More than 255 Unity mesh renderers or more geometry than this baseline requires a written, measured exception.
- Disable automatic material generation, camera/light import and automatic collider generation. Make one explicit shared Unity material library and one material-map record instead of accepting a material copy for every imported mesh.
- Keep glass separate from opaque geometry and use a small controlled transparent material family. Do not solve appearance issues with stacked transparent planes, excessive emission, or many real-time lights.
- Keep the existing interior shell as a separately loadable depth-preview layer. It stays off in exterior-only tests and is not a substitute for real rooms.
- Doors stay dynamic. Their twelve roots and moving leaves are never static or merged into façade meshes. Exterior collision uses simple box/compound volumes; no MeshCollider is added to every façade, pane, trim piece, or future room.
- Existing R40 exterior detail—mullions, frames, bands, balcony guards, canopy assemblies, door hardware, roof/service pieces, UVs and material slots—arrives through these imports. New high-detail rooms, furniture, medical equipment, decals and interactive training props wait for modular Stage 7 production.

#### Preflight record and project layout

R41 froze Unity `6000.3.20f1`, Windows x86_64 PC VR, URP `17.3.0`, OpenXR `1.17.1`, XR Interaction Toolkit `3.5.1`, XR Management `4.7.0`, and Input System `1.20.0`. Headset scope is Quest 2 and Quest 3 through Quest Link/Air Link; Android standalone is excluded. Acceptance is 72 Hz / 13.89 ms with a preferred 90 Hz / 11.11 ms target. Package, pipeline, platform, and XR choices must not silently change after this record.

| Unity area | Purpose | Content rule |
|---|---|---|
| `Assets/Hospital/SourceFBX/R40/` | Immutable imports of the ten corrected FBXs | Source-only; do not hand-edit imported FBX hierarchies. |
| `Assets/Hospital/Materials/` | Shared Unity materials and material-map record | One controlled asset per approved material role; no auto-generated duplicates. |
| `Assets/Hospital/Prefabs/Zones/` | Zone prefabs created from the source FBXs | Keep static opaque and glass roles separated. |
| `Assets/Hospital/Prefabs/Doors/` | Twelve root-preserving door prefabs | Dynamic leaves and documented references only. |
| `Assets/Hospital/Scenes/IntegrationSandbox/` | Disposable import/comparison scene | Never becomes the production hospital scene. |
| `Assets/Hospital/Scenes/Additive/` | Runtime-loadable hospital scenes | `Exterior_Base`, `Exterior_Tower`, `Exterior_SidesService`, `Exterior_InteriorShellPreview`, and `Exterior_Interactions`. |
| `Assets/Hospital/Settings/` | Import, physics-layer, quality and manifest records | Changes are versioned and reviewed before broad application. |

The five additive scenes group the ten source zones without losing traceability: `Exterior_Base` contains global structure and ground entrance; `Exterior_Tower` contains front tower and balconies; `Exterior_SidesService` contains left façade/podium, right façade/wing, rear/service and roof/service; `Exterior_InteriorShellPreview` contains interior shell only; and `Exterior_Interactions` contains interactive doors. Future room scenes are loaded by floor or department, never placed permanently in this exterior scene.

#### Round 1 — R41 Unity foundation and ground-entrance proof: PASS

- Freeze an input manifest with the R40 SHA-256, ten corrected-FBX filenames/checksums, export-zone names, triangle/material baseline and twelve expected door roots.
- Create the project folder structure, Unity version/package record, layers and disposable `IntegrationSandbox` without moving or editing source FBXs.
- Import only `Hospital_EXT_GlobalStructure`, `Hospital_EXT_GroundEntrance` and `Hospital_ACT_InteractiveDoors` into the sandbox. Keep metre scale and common origin; disable material, camera/light and collider auto-generation.
- Build the first material map: original R40 material name, Unity material asset, opaque/glass/fixture role, shader/rendering mode, texture ownership and permitted zones. Recreate the approved exterior appearance with shared Unity materials, not Blender procedural graphs or per-mesh copies.
- Verify front/entrance bounds, scale, pivots, normals, UVs, glass ordering, door-root positions and visual appearance against the corrected R40 review pack. Use temporary neutral comparison setup only; save no presentation rig into production scenes.

Automatic Round 1 gate:

- Input checksums are unchanged and only corrected FBXs were imported.
- Metre scale, common origin, front/entrance bounds, normals, UVs and material roles match Stage 4 manifests.
- No imported/persistent Blender camera or light, generated collider, duplicate material set or unplanned scene object exists.
- The entrance proof has no visible regression, stays within the renderer allowance and finds all twelve door roots.
- Unity version, platform, pipeline, XR choice and target performance budget are frozen. A missing safe project/platform choice pauses here for user direction rather than guessing.

R41 completion record:

- Created `Unity/AnatomyXR` in Unity `6000.3.20f1` with Windows x86_64, D3D11, linear color, URP, OpenXR and the Input System. Standalone OpenXR has only the OpenXR loader, single-pass instanced rendering, Quest 2 Oculus Touch and Quest 3 Touch Plus profiles; no Android XR settings were created.
- Frozen input and material-source manifests reproduce the newest user-approved R40 at 644,996 source triangles. Only `Hospital_EXT_GlobalStructure`, `Hospital_EXT_GroundEntrance`, and `Hospital_ACT_InteractiveDoors` are imported for R41.
- Built 30 shared Unity material assets, three traceable zone prefabs and a disposable three-root integration sandbox. The proof uses 69 renderers and 20 shared materials and retains all twelve door roots.
- Unity measures 117,846 triangles from 118,060 source triangles. The exact 214-triangle variance is documented as zero-area source triangles in two ground-entrance meshes that Unity discards; this is not geometric simplification.
- The final R41 gate passed all 43 checks with zero failures, including checksums, metre scale/common origin, bounds, import policy, normals, UV0, material ownership, forbidden payload, XR/package versions, sandbox contents, and duplicate-XR-folder cleanup.
- Gate/report: `Reviews/HospitalExterior/Stage05_R41/Stage05_R41_UnityGate.json`. QA images: `Stage05_R41_Unity_EntranceProof.png` and `Stage05_R41_Unity_PlayerEye.png` in the same folder.

#### Round 2 — R42 complete zone assembly and material system: PASS

- Import the remaining corrected FBXs, make one controlled prefab per source zone and assemble all ten at an unchanged common origin in the sandbox before creating additive-scene copies.
- Build the five additive scenes from these prefabs. Source-zone names, zero transforms and manifest references remain visible in the hierarchy.
- Finish the shared material library and mapping record. Opaque cladding/metal/concrete stay separate from glass; exterior trim and fixtures share materials; an imported duplicate is removed only after its explicit replacement is assigned and visually verified.
- Keep the current interior shell independently enabled. It supplies depth through glazing when loaded, but it is excluded from exterior-only tests and no furniture or individual rooms are added.
- Compare front, left, right, rear, roof, entrance, aerial and player-eye Unity stills to the R40 review pack. Resolve seams, coordinate shifts, material mismatch, transparency sorting or import normals before advancing.

Automatic Round 2 gate:

- All ten zones align without doubled faces, changed scale, displaced pivots, new seams or gaps; restored inter-floor bands and the right-wing/rear closure remain intact.
- Every geometry object traces to exactly one source zone; additive scenes load without duplicate hospital geometry.
- Glass is separate, opaque materials are shared appropriately and the material map has no unresolved or auto-generated assignments.
- The complete hospital is at or below 255 mesh renderers, does not exceed the R40 triangle baseline without an approved exception and does not edit source FBXs in place.
- Full exterior visual comparison passes; `Exterior_InteriorShellPreview` can unload without damaging the exterior.

R42 completion record:

- Imported all ten checksum-frozen Stage 5 FBXs and built one zero-transform traceable prefab per source zone. The complete sandbox contains exactly ten connected zone roots with no cameras, lights or colliders.
- Built the five planned additive scenes: `Exterior_Base`, `Exterior_Tower`, `Exterior_SidesService`, `Exterior_InteriorShellPreview`, and `Exterior_Interactions`. Every source zone appears exactly once across the scene matrix; the interior shell remains independently loadable.
- The complete hospital retains the 237-renderer source count, uses all 30 controlled shared materials, preserves opaque/glass role separation, and retains all twelve door roots.
- Unity measures 644,332 triangles from the 644,996 source baseline. Blender diagnostics bind the exact 664-triangle difference to zero-area source triangles: 214 ground entrance, 92 rear/service, 302 right façade/wing and 56 roof/service.
- Eight Unity QA images were compared to the R40 review set. Alignment, protected floor bands, right-wing/rear closure, roof, glass/interior depth, entrance and 1.7 m player-eye appearance passed; the visual review is checksum-bound.
- The final R42 gate passed all 104 checks with zero failures. Gate/report: `Reviews/HospitalExterior/Stage05_R42/Stage05_R42_UnityGate.json`; scene matrix, material usage, diagnostics, visual review, QA evidence and logs are in the same folder.

#### Round 3 — R43 doors, navigation scale and collision prototype: PASS

- Turn each of the twelve root/leaf sets into a documented Unity door prefab. Confirm hinge/pivot orientation, closed pose, opening clearance, materials and independent transform before any final animation or gameplay behavior.
- Add only the minimum collision/navigation layer: simple volumes for walkable exterior ground, steps/ramps, perimeter/unsafe edges, entrance threshold and service boundaries. Doors use dynamic collision; decorative façade detail has no individual collision.
- Add a temporary player/XR-origin scale harness using a 1.7 m eye-height reference. Test entrance, canopy clearance, balcony access, exterior routes and loading/service limits. This is not the final game controller.
- Mark only verified opaque architecture static under the selected Unity workflow. Doors, interactive content and any object excluded by the glass/material strategy remain dynamic.
- Profile exterior-only, exterior-plus-interior-shell preview and door-interaction states. Record renderers, triangles, draw calls/batches, SetPass calls, CPU/GPU frame time, memory and transparent-overdraw observations for the selected platform.

Automatic Round 3 gate:

- All twelve doors retain correct roots, pivots, closed positions, clearance and dynamic/static separation; no leaf is welded into a zone mesh.
- Player/XR scale and traversal pass without accidental holes, blocked entrances, excessive collider complexity or collision against decorative detail.
- The collision inventory contains only documented simple volumes; no blanket MeshCollider or individual-façade-collider payload exists.
- Profile results are captured for all three states and remain attributable to the R40 baseline. A performance failure is corrected or documented before continuing; it is not hidden by reducing review quality.

R43 completion record:

- Built twelve independent root-preserving door prefabs. Each retains its exact source root/pivot and closed pose, contains one fitted `BoxCollider`, one kinematic `Rigidbody`, documented motion/clearance metadata, dynamic layer assignment and no static flags. The runtime interactions scene contains only these twelve connected prefabs: 44 renderers and 6,160 triangles with no duplicated source-zone mesh.
- Added one controlled 28-`BoxCollider` compound collision prefab covering exterior ground/entrance, threshold, perimeter/service limits and six balcony floor/guard sets. The twelve doors add twelve dynamic box colliders. No `MeshCollider` or decorative-façade collider was introduced.
- Marked 166 verified opaque renderers with the selected static flags while all 71 glass/door renderers remain dynamic.
- Built and passed a disposable `XROrigin` traversal harness with a disabled test camera, exact 1.7 m eye reference and five entrance/canopy/balcony/exterior/service route checks. It is excluded from build settings and is not a final controller.
- Captured three attributable editor/D3D11 proxy states on the i7-14700HX / RTX 4050 Laptop GPU host: exterior-only 145 renderers / 626,052 triangles; exterior plus shell 193 / 638,172; full door interaction 237 / 644,332. CPU render samples, memory and transparent counts are recorded.
- Unity's offscreen batch mode returned zero live draw/batch/SetPass counters and no headset GPU frame time. The report therefore labels draw/batch counts as raw submesh upper bounds and SetPass as a unique-material lower bound; it explicitly requires a Quest Link/Air Link stereo CPU/GPU capture before Stage 6 performance acceptance.
- The final R43 gate passed all 39 checks with zero failures. Gate/report: `Reviews/HospitalExterior/Stage05_R43/Stage05_R43_UnityGate.json`; door, collision, traversal and profiler records are in the same folder.

#### Round 4 — R44 integration QA and Stage 5 freeze: PASS

- Perform a clean import/scene-load verification from the frozen R40 input manifest. Load each additive scene alone and in approved combinations; verify no duplicate geometry, missing dependency, unexpected camera/light, broken material link or source-prefab override.
- Repeat the R40 eight-view comparison and targeted checks for floor bands, right-wing/rear closure and loading area. Verify glass and door positions at player eye level and at distance.
- Recheck hierarchy, common-origin alignment, material map, collider inventory, static flags, door-prefab inventory, renderer/triangle/material counts and target-platform profile baseline.
- Write the Stage 5 Unity manifest, import-settings record, material map, scene-load matrix, collision map, door-root report, profiler baseline and concise source-change log. Freeze a Stage 5 integration checkpoint without modifying R40 or corrected FBXs.
- Archive disposable sandbox screenshots, profiling captures and temporary test assets; keep the active project limited to approved prefabs, additive scenes, shared materials, required settings and frozen records.

R44 completion record:

- Force-reimported and checksum-verified all ten frozen FBXs without modifying the newest R40 authority or the external input set. Unity/project settings remain fixed at `6000.3.20f1`, Windows x86_64 D3D11/URP/OpenXR PC VR, Quest 2/3 through Link/Air Link, with no Android standalone configuration.
- Locked Build Settings to exactly five production additive scenes. The complete hospital remains 237 renderers / 644,332 Unity triangles / all 30 shared materials, with 28 static collision boxes, twelve dynamic door boxes and no `MeshCollider`.
- Validated every scene alone and four approved combinations: exterior-only 145 renderers / 626,052 triangles; exterior-plus-shell 193 / 638,172; exterior-plus-doors 189 / 632,212; full hospital 237 / 644,332.
- Repeated eight standard views and three targeted views for floor-band continuity, right-wing/rear closure and the loading area. All eleven checksum-bound views passed geometry, material, glass, scale and additive-alignment review.
- Removed the R41/R42 integration sandboxes and R43 traversal harness from the active Unity project after checksum-freezing their scene files and metadata under `Archive/HospitalExterior/Unity/Stage05_DisposableScenes/`. Only the five production additive scenes remain active.
- Froze the final manifest, import settings, material map, scene-load matrix, source-change log, QA evidence, visual review and Unity gate under `Reviews/HospitalExterior/Stage05_R44/`; the final manifest status is `FINAL_FROZEN`.
- R44 passed 32/32 technical checks and 33/33 final checks. Unity emitted `STAGE05_R44_GATE=PASS; STAGE05_FREEZE=COMPLETE`. The final checkpoint is `Archive/HospitalExterior/Docs/HospitalExterior_Stage05_Checkpoint.md`.
- Independently round-trip revalidated the immutable external input set with Blender 5.1.2 after the freeze; all ten zones and twelve door roots passed with zero failures in `Stage05_R44_InputRevalidation.json`.

Final Stage 5 acceptance requirements:

- The newest user-confirmed R40 and all ten Stage 5 input FBXs remain checksum-matched, unedited input authorities.
- All zones assemble at metre scale/common origin; the 237-renderer/644,996-source-triangle baseline is retained or any measured Unity import variance is documented and approved.
- No persistent Blender camera/light payload, legacy R39 asset, test asset, duplicate material set or unexplained prefab override enters runtime scenes.
- Twelve animation-ready door roots remain separate and correct; glass, interior-shell preview, opaque architecture and dynamic doors follow their defined loading/material rules.
- Exterior-only and interior-shell-preview performance baselines are measured on the selected target; no target-hardware claim is made without a captured profile.
- The hospital opens from lightweight additive scenes rather than one all-content scene.

Final Stage 5 disposition: all acceptance requirements above pass for the integration freeze. Headset performance remains deliberately unclaimed; real Quest Link/Air Link stereo profiling is a prerequisite for Stage 6 performance acceptance.

#### Detail allocation after Stage 5

- Stage 5 imports and preserves approved exterior detail already in R40, then establishes its Unity material and loading rules.
- Stage 6 uses the captured profile to add only justified real-time optimization: platform-specific LODs, occlusion/culling, instancing, texture budgets, streaming and lighting strategy. It does not blindly simplify the approved exterior.
- Stage 7 produces real rooms as separate modular Unity/Blender source packages by department/floor. Start with one approved representative room set, profile it in context, then expand in batches. Furniture, medical devices, decals, lighting and interactive training details load only with the room/department needed by the player.
- Unity-native daylight, baked/mixed lighting and limited interior lighting are introduced only after the Stage 5 material/import baseline and target hardware are fixed. Lighting must be reusable and zone-based, never hundreds of ad-hoc point lights used merely for review.

#### Stage 5 execution policy

- R41 through R44 continue automatically when their gates pass; a failed check is repaired and revalidated within the same round.
- Stop only for a missing project/platform decision, a source-geometry defect that cannot be safely classified, a design change needing approval or repeated validation that cannot be resolved safely.
- No script may silently accept a failed gate, overwrite R40/input FBXs or progress into Stage 6/7. The user reviews the combined frozen Stage 5 result after R44.

### Stage 5S — site Unity integration checkpoint (complete / approved / frozen)

The user approved the exact R05B site authority SHA-256 `68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d` on 2026-08-11. S6 integrates its seven hash-locked FBXs and five Grass005/mask textures only under `Assets/HospitalSite/Stage05S/R05B/`. The five R44 hospital scenes, ten hospital FBXs, hospital prefabs/materials and frozen records remain byte-identical. The source-change record reports zero hospital, site-source, road/path or landscaping-source changes.

Scene ownership is additive and independently unloadable: `Exterior_SiteCore` owns roads, parking, paths, crossings, drainage, route surfaces and rear service; `Exterior_SitePerimeter` owns the four fence chunks, five fence barriers, main gate and two gate-leaf roots; `Exterior_SiteLandscape` owns the lake placeholder, base lawn, 99 vegetation groups, furniture, tree collision and the single grass renderer. Build Settings contain these after the original five scenes. Site scenes alone, site-only, hospital-only, default combined and all-eight states pass without duplicate geometry, cameras, lights, materials or grass managers.

The controlled site inventory is 20 new materials plus five reused hospital materials; 153 fixed site renderers / `43,178` fixed triangles; exactly 99 LOD groups using 18 shared vegetation meshes; and exact vegetation totals of `5,367,488 / 1,180,274 / 348,274` triangles at LOD0/1/2. One vegetation tier per group gives 252 effective site mesh-renderer streams and 489 combined with the frozen 237-renderer hospital. Loaded inactive LOD components are reported separately and are not counted as simultaneously rendered tiers.

At the final proxy viewer, hybrid grass adds `15,744` triangles in two direct instanced submissions with zero steady-state GC allocation. Including that grass, site-only forced-tier totals are `5,426,410 / 1,239,196 / 407,196`; hospital-plus-site totals are `6,070,742 / 1,883,528 / 1,051,528`. Collision is limited to 20 route `MeshCollider`s, seven perimeter/gate boxes, five bench boxes, five bin capsules and 27 tree capsules. No visual road/path mesh or grass receives collision.

The independent Unity gate passes `16/16`; post-integration R05B and R05 regressions pass `64/64` and `87/87`. The user approved the S6 visual checkpoint on 2026-08-11, so Stage 5S is complete and frozen. Evidence and records are in `Reviews/HospitalExterior/Stage05S_RoundS6/`; the frozen checkpoint is `Archive/HospitalExterior/Docs/HospitalExterior_Stage05S_RoundS6_Checkpoint.md`. The editor/D3D11 proxy remains correctly non-headset evidence; Stage 6 must measure real Quest Link/Air Link stereo performance.

### Stage 6 — VR traversal and real-time optimization

#### Stage 6A R01 technical implementation record — PASS / READY_FOR_HEADSET_REVIEW

- All new harness assets are isolated under `Unity/AnatomyXR/Assets/Hospital/Stage06A/R01/`; no component, override or saved teleport behavior was added to the frozen R44 or R05B/S6 content.
- The project-owned input contract contains HMD and left/right controller pose/tracking actions, left-stick movement at `2.0 m/s`, right-stick `45°` snap turn, right `A` hold/release teleport and right `B` cancel, with no smooth turn, jump, climb, grab or gameplay interaction.
- One floor-tracked XR Origin owns the stereo camera, audio listener, interaction manager, character controller, locomotion mediator/providers, tracked controller proxies, curved right-hand ray and valid/invalid reticle. The body capsule follows real headset height with a `0.25 m` radius, `0.20 m` step offset and `45°` slope limit.
- The separate `Hospital_Stage06A_TraversalProfile` bootstrap defaults to `Combined` and also defines `HospitalOnly` and `SiteOnly`. It owns neutral QA daylight and loads the requested production scenes additively once while rejecting duplicate cameras, lights, roots or grass managers.
- Runtime teleport binding targets exactly the saved 20 route-collider paths/fingerprints. Landing validation rejects non-allowlisted surfaces, excessive slope, missing support, obstacle overlap and insufficient capsule/head clearance. The S6 scenic Lake marker at `(-57, 0, -81)` was found to be 14 m off-route, so the Stage 6A Lake Path checkpoint uses the nearest verified safe route point `(-58.37, 0, -94.93)` without editing S6.
- Six invisible ordered QA triggers record route progress, HMD/controller tracking, movement/snap use, teleport attempts/rejections, runtime errors and falls. A fall restores the last safe point so review can continue but permanently prevents that session from passing.
- The expanded freeze manifest protects 295 files and explicitly records both the active R40 authority (`f797abb...`) and the exact frozen `.blend1` snapshot (`0a530ff...`); an independent full-manifest rehash reports zero mismatches.
- `Stage06A_R01_UnityGate.json` passes all ten automated checks. The final incremental Windows x86_64 development build contains the bootstrap plus the eight production scenes, records `268,799,115` bytes in `2.25 s`, canonicalizes Unity's transient post-build project serialization, rehashes all 295 protected files before reporting PASS, and preserves the original eight-scene Build Settings.
- Technical disposition is `READY_FOR_HEADSET_REVIEW`, not `APPROVED`. No headset, connection-mode or performance claim exists until the required physical session and explicit user decision are archived.

#### Stage 6 delivery sequence

1. **6A — headset traversal harness:** technical implementation and controlled build are complete in R01. Physical Quest Link/Air Link traversal evidence, six route screenshots and an explicit `APPROVED` or `CORRECTION_REQUIRED` decision remain required.
2. **6B — real headset baseline:** capture hospital-only, site-only and combined route matrices on the actual Quest target through Link and/or Air Link. Record CPU main/render thread, GPU, P50/P95/P99 frame time, batches, SetPass, active triangles/renderers, mesh/texture memory and GC.
3. **6C — measured tuning:** tune vegetation thresholds/shadows first, then grass radii/density, batching, shadow distance, culling/streaming and texture memory. Every change requires before/after headset evidence and the frozen road/path and close-tree visual regressions.
4. **6D — final acceptance:** repeat the complete route/load matrix, freeze the chosen quality/profile configuration and publish the Stage 6 manifest, profiler captures, locomotion validation, visual regression and checkpoint.

#### Stage 6 completion gate

- A floor-tracked user can stand at the main gate, see both tracked controllers, physically step within the Guardian boundary, thumbstick-walk, snap-turn and teleport across every approved route surface without falling through, clipping obstacles or landing on blocked grass, mulch, water, hospital or decorative meshes.
- At least the actual supported target headset is captured; claiming both Quest 2 and Quest 3 support requires evidence from both. Link/Air Link mode, refresh rate, render resolution and PC specification must be recorded.
- Mandatory 72 Hz has a `13.89 ms` CPU/GPU budget across the full route matrix; preferred 90 Hz has `11.11 ms`. P50/P95/P99 and dropped-frame/spike evidence must be reported rather than averaged away.
- Steady-state locomotion and grass updates allocate zero GC; site unload releases its grass/runtime resources; automatic LOD does not leave multiple full tiers active outside its narrow cross-fade.
- Grass stays within 65,000 triangles and four submissions. No optimization displaces or simplifies the approved roads, paths, crossings, parking geometry or close-view tree source assets.
- The user completes and approves a headset walk-through, and the final Stage 6 gate, captures, configuration, limitations and checkpoint are frozen before Stage 7 or permanent gameplay expansion.

### Stage 7 — Real hospital interior

Detailed floor programme, source ownership, elevator/stair-first delivery sequence, anatomy-layer progression and merge-back contract: [HOSPITAL_INTERIOR_DESIGN_PLAN.md](HOSPITAL_INTERIOR_DESIGN_PLAN.md). This child plan is pre-production only until the Stage 6 headset completion gate passes.

- Treat the playable interior as a separate production project.
- Create a proper gameplay floor plan before detailed modelling and build only the required floors/wings first.
- Use separate Blender sources for the modular interior kit, shared hospital props, reusable room archetypes and major floor/wing layouts.
- Create one reusable source/prefab per room type, not one Blender file per repeated room instance and not one giant file containing every furnished room.
- Assemble room prefab instances and variants inside additive Unity floor/wing scenes.
- Replace temporary interior shells progressively.
- Require a Unity performance check after each room archetype and zone before expanding further.

### Stage 8 — Gameplay and XR interaction

- Promote the proven Stage 6 traversal controls into the permanent gameplay rig, then implement doors, elevators, room transitions, anatomy/XR content, UI, training interactions, audio and gameplay logic after the required architecture exists. Stage 6A provides the usable exterior test traversal; Stage 8 makes it the final product interaction system.

## Key files

- Approved balcony/door milestone: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage02B/HospitalExterior_Stage02B_R11_AccessDoorsAllBalconies.blend`
- Approved Stage 3A milestone: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03A/HospitalExterior_Stage03A_R15_FrontFacadeCornerResolved.blend`
- Gate 1 checkpoint: `Archive/HospitalExterior/Blender/Stage03A_Gates/HospitalExterior_Stage03A_R12_Gate1_StandardBay.blend`
- Gate 2 checkpoint: `Archive/HospitalExterior/Blender/Stage03A_Gates/HospitalExterior_Stage03A_R13_Gate2_Transitions.blend`
- Stage 3A review pack: `Archive/HospitalExterior/Reviews/Stage03A_FrontFacade/`
- Approved Stage 3B milestone: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03B/HospitalExterior_Stage03B_R20_LeftCornerClosed.blend`
- Stage 3B gate checkpoints: `Archive/HospitalExterior/Blender/Stage03B_Gates/`
- Stage 3B review pack and audit: `Archive/HospitalExterior/Reviews/Stage03B_HeroEntrance/`
- Approved Stage 3C milestone: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03C/HospitalExterior_Stage03C_R24_AllSidesRoofComplete.blend`
- Stage 3C gate checkpoints: `Archive/HospitalExterior/Blender/Stage03C_Gates/`
- Stage 3C review pack and audit: `Archive/HospitalExterior/Reviews/Stage03C_AllSidesRoof/`
- Approved Stage 3D audited checkpoint: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03D/HospitalExterior_Stage03D_R28_ExteriorMaterialsUVComplete_Audited.blend`
- Stage 3D gate checkpoints: `Archive/HospitalExterior/Blender/Stage03D_Gates/`
- Stage 3D review pack, material manifest and audit: `Archive/HospitalExterior/Reviews/Stage03D_ExteriorMaterialsUV/`
- Stage 3D builder: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/build_hospital_stage03d_exterior_materials_uv.py`
- Stage 3D validator: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/validate_hospital_stage03d_exterior_materials_uv.py`
- Stage 3E audited checkpoint: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03E/HospitalExterior_Stage03E_R32_InteriorShellsComplete_Audited.blend`
- Stage 3E gate checkpoints: `Archive/HospitalExterior/Blender/Stage03E_Gates/`
- Stage 3E review pack and audit: `Archive/HospitalExterior/Reviews/Stage03E_InteriorShells/`
- Stage 3E builder: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/build_hospital_stage03e_interior_shells.py`
- Stage 3E validator: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/validate_hospital_stage03e_interior_shells.py`
- Approved Stage 3F presentation source: `Archive/HospitalExterior/Blender/ApprovedMilestones/Stage03F/HospitalExterior_Stage03F_R36_ArchitecturalLightingComplete.blend`
- Stage 3F gate checkpoints: `Archive/HospitalExterior/Blender/Stage03F_Gates/`
- Combined Stage 3E + 3F review pack and Stage 3F audit: `Archive/HospitalExterior/Reviews/Stage03EF_CombinedReview/`
- Stage 3F builder: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/build_hospital_stage03f_architectural_lighting.py`
- Stage 3F validator: `Archive/HospitalExterior/Tools/CompletedStages/Stage03/validate_hospital_stage03f_architectural_lighting.py`
- Pre-Stage 4 cleanup record: `Archive/HospitalExterior/Docs/HospitalExterior_PreStage04_WorkspaceCleanup.md`
- Post-Stage 4 R40 cleanup record: `Archive/HospitalExterior/Docs/HospitalExterior_PostStage04_R40_WorkspaceCleanup.md`
- Superseded Stage 4 R37/R38/R39 Blender checkpoints: `Archive/HospitalExterior/Superseded/Stage04_R39_PreReviewCorrection/Blender/`
- Corrected Stage 4 authority: `ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend`
- Corrected Stage 4 gates and ground-entrance prototype: `Archive/HospitalExterior/Blender/Stage04_Gates/`
- Superseded Stage 4 Unity FBX exports: `Archive/HospitalExterior/Superseded/Stage04_R39_PreReviewCorrection/Exports/Stage04/`
- Corrected Stage 4 Unity FBX exports: `Exports/HospitalExterior/Stage04_ReviewCorrected/`
- Superseded R39 review pack: `Archive/HospitalExterior/Superseded/Stage04_R39_PreReviewCorrection/Reviews/Stage04_Master/`
- Corrected R40 review pack, manifests, audits and validator reports: `Archive/HospitalExterior/Reviews/Stage04_Master_ReviewCorrected/`
- Stage 4 checkpoint summary: `Archive/HospitalExterior/Docs/HospitalExterior_Stage04_Checkpoint.md`
- Stage 4 R40 correction record: `Archive/HospitalExterior/Docs/HospitalExterior_Stage04_ReviewCorrections_R40.md`
- Completed Stage 4 pipeline: `Archive/HospitalExterior/Tools/CompletedStages/Stage04/hospital_stage04_pipeline.py`
- Completed Stage 4 independent validator: `Archive/HospitalExterior/Tools/CompletedStages/Stage04/validate_hospital_stage04.py`
- Active Stage 5 Unity project: `Unity/AnatomyXR/`
- Frozen Stage 5 input FBXs and manifest: `Exports/HospitalExterior/Stage05_R41_Input/`
- Stage 5 tools and Unity automation templates: `Tools/Stage05/`
- R41 Unity gate, project/material records, diagnostics, logs and QA images: `Reviews/HospitalExterior/Stage05_R41/`
- R42 Unity gate, ten-zone/additive records, source diagnostics, logs and eight-view QA: `Reviews/HospitalExterior/Stage05_R42/`
- R43 Unity gate, door/collision/traversal records and attributable PC proxy profile: `Reviews/HospitalExterior/Stage05_R43/`
- R44 final Unity gate, manifest, import/material/scene records, logs and eleven-view QA: `Reviews/HospitalExterior/Stage05_R44/`
- Frozen Stage 5 checkpoint summary: `Archive/HospitalExterior/Docs/HospitalExterior_Stage05_Checkpoint.md`
- Archived disposable Stage 5 Unity scenes and checksum manifest: `Archive/HospitalExterior/Unity/Stage05_DisposableScenes/`
- Coordinated Stage 5S site-expansion design and merge-back contract: `HOSPITAL_SITE_EXPANSION_DESIGN_PLAN.md`
- Stage 5S generated masterplan, aerial and gate-arrival concept references plus hash manifest: `Reviews/HospitalExterior/Stage05S_Concept/`
- Approved Stage 5S S1 blockout evidence: `Reviews/HospitalExterior/Stage05S_RoundS1/`
- Approved Stage 5S S2 gate/perimeter evidence: `Reviews/HospitalExterior/Stage05S_RoundS2/`
- Stage 5S S3 circulation/hardscape candidate, validation and review pack: `Reviews/HospitalExterior/Stage05S_RoundS3/`
- Superseded Stage 5S S4 landscape evidence retained as `CORRECTION_REQUIRED`: `Reviews/HospitalExterior/Stage05S_RoundS4/`
- Approved Stage 5S S4B high-fidelity redesign, nine-view review pack and 51/51 validation: `Reviews/HospitalExterior/Stage05S_RoundS4B/`
- Preserved Stage 5S S5 final-Blender candidate, ten-view review pack and 87/87 validation: `Reviews/HospitalExterior/Stage05S_RoundS5/`
- Approved Stage 5S S5B hybrid-lawn authority, fourteen-view review pack, seven-FBX audit and 64/64 validation: `Reviews/HospitalExterior/Stage05S_RoundS5B/`
- Stage 5S S6 Unity gate, manifest, scene/material/LOD/collision/grass/performance records and 15-view QA: `Reviews/HospitalExterior/Stage05S_RoundS6/`
- Approved and frozen Stage 5S S6 checkpoint: `Archive/HospitalExterior/Docs/HospitalExterior_Stage05S_RoundS6_Checkpoint.md`
- Stage 6A R01 versioned traversal harness, rig, input actions, bootstrap scene and validators: `Unity/AnatomyXR/Assets/Hospital/Stage06A/R01/`
- Stage 6A protected-input, action, route, Unity-gate, build and headset-review records: `Reviews/HospitalExterior/Stage06A_R01/`
- Stage 6A Windows Quest Link/Air Link development build: `Exports/HospitalExterior/Stage06A_R01_PCVR/AnatomyXR_Stage06A_R01.exe`
- Stage 6A technical checkpoint pending physical approval: `Archive/HospitalExterior/Docs/HospitalExterior_Stage06A_R01_Checkpoint.md`

## New-chat handoff summary

I am continuing a Blender hospital environment project. The anatomy/XR document supplies the core game idea only; the present task is hospital environment production.

Stages 1 and 2 are complete, and the hospital massing must not be redesigned. The R11 balcony/access-door system, R15 Stage 3A front façade and R20 Stage 3B hero entrance/ground-front work are approved and protected.

Stages 3C–3F are approved. Their earlier milestone files, audited snapshots, completed tools, gates, review packs and audits are organized under `Archive/HospitalExterior/`. The audited R28, R32 and R36 hashes remain preserved in the cleanup record.

Stage 4 completed all four automatically gated rounds, but the combined user review then found two real defects in R39: the approved inter-floor band system had been removed because its source collection was misleadingly named `PROXY_FACADE`, and the rear/right-wing service return remained open. Small viewport sticks were separately identified as non-rendering door-root/relationship overlays.

R37B restores and protects seven continuous bands, seven shadow reveals and six façade accent fixtures; it also adds a simple opaque closure to the rear return. R38B and all ten corrected FBXs passed individual and combined round-trip validation. R40 then passed the independent final validator, eight standard review views and three targeted correction views.

The current authority is the newest user-confirmed `HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend`, SHA-256 `f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126`. The externally resaved active file is structurally identical across all 249 objects to the exact frozen `.blend1` snapshot at SHA-256 `0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58`. It contains one `UNITY_EXPORT` hierarchy, 249 total objects, 237 mesh renderer candidates, twelve interactive door-root empties, 644,996 triangles, 30 materials, zero cameras, zero Blender lights and zero live modifiers. The user confirmed its later twelve-triangle GlobalStructure edit is intentional. R36 remains byte-for-byte unchanged, and all superseded R37/R38/R39 or failed correction artifacts remain archived for traceability.

For Stage 5, use only the ten FBXs under `Exports/HospitalExterior/Stage05_R41_Input/`. The original corrected Stage 4 exports and review records remain historical evidence under their existing Stage 4 paths.

Stage 5 is complete and frozen for Quest 2/3 PC VR through Link/Air Link, not Android standalone. R41 passes 43/43 checks and freezes Unity 6.3.20f1/URP/OpenXR/XRI. R42 passes 104/104 checks: all ten FBXs/prefabs and five additive scenes align at common origin, 237 renderers use all 30 shared materials, and Unity's documented runtime mesh total is 644,332 triangles after discarding 664 exact zero-area source triangles. R43 passes 39/39 checks: twelve independent door prefabs, 28 simple static collision volumes plus twelve dynamic door boxes, verified static/dynamic separation, a disposable 1.7 m `XROrigin` harness and three PC proxy profile states are frozen. R44 passes 33/33 final checks after a clean forced import, final scene-combination validation, eleven-view QA, record freeze and disposable-scene archival. Its manifest is `FINAL_FROZEN`, and Unity emitted `STAGE05_R44_GATE=PASS; STAGE05_FREEZE=COMPLETE`. Stage 6A R01 now passes its `10/10` technical gate and explicit nine-scene Windows development build while preserving every protected input and the frozen eight-scene Build Settings. It is `READY_FOR_HEADSET_REVIEW`, not approved: run the physical Quest Link/Air Link route, import the evidence and record the user's decision before Stage 6B, Stage 7, final gameplay or real rooms.

The Stage 5S site expansion is governed by `HOSPITAL_SITE_EXPANSION_DESIGN_PLAN.md`. S1–S4B, the exact R05B authority SHA-256 `68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d` and the S6 Unity checkpoint are approved and frozen; rejected R04 and preserved R05 remain evidence. S6 is integrated under `Assets/HospitalSite/Stage05S/R05B/` as three independently unloadable site scenes beside the five byte-identical R44 scenes. It passes the independent Unity gate `16/16`, the post-integration R05B `64/64` regression and the R05 `87/87` regression. Exact scene ownership, materials, 99 LOD groups, collision, grass occupancy, site-only/combined proxy counts, 15-view QA and limitations are merged into this parent file and the archived checkpoint. Stage 5S is complete. Stage 6A's technical harness is complete and awaiting the physical headset decision; no headset-performance claim exists until Stage 6B captures the approved route on the tested Quest Link/Air Link setup.
