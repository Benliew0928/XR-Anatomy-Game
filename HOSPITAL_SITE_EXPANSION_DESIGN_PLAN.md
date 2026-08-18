# Hospital Site Expansion Design Plan

Status: Stage 5S complete, approved and frozen through Round S6; Round S4 retained as correction-required evidence and original Round S5 preserved  
Date: 2026-08-11  
Proposed phase: Pre-Stage 6 site-content expansion (`Stage 5S`)  
Modeling status: Round S1 `APPROVED`; Round S2 `APPROVED`; Round S3 `APPROVED`; Round S4 `CORRECTION_REQUIRED`; Round S4B `APPROVED`; Round S5 `PRESERVED`; Round S5B `APPROVED`; Round S6 `APPROVED / FROZEN`

Parent roadmap: `HOSPITAL_EXTERIOR_PROGRESS.md`  
Parent checkpoint: frozen Stage 5 / R44  
Return point after completion: Stage 6 VR and real-time optimization

## 0. Parent-plan coordination contract

This document is a controlled child plan of `HOSPITAL_EXTERIOR_PROGRESS.md`; it does not replace or fork the main hospital roadmap.

- The existing Stage 5 R40/R41–R44 authorities, checksums, FBXs, Unity prefabs, materials, gates and five additive scenes remain immutable historical inputs.
- Site expansion is recorded as `Stage 5S`, positioned after the frozen Stage 5 checkpoint and before Stage 6. It adds site content without retroactively changing the meaning or gate results of Stage 5.
- The new site master uses the same metre scale, world origin, front/rear orientation and naming discipline as the frozen hospital.
- New Blender sources, FBXs, Unity prefabs, materials, additive scenes and gates live under Stage 5S-specific paths. They never overwrite R40, the ten frozen Stage 5 FBXs or R44 records.
- The Stage 5S Unity scenes load beside the existing five hospital scenes. Existing scene ownership remains unchanged, so the site can be removed or revalidated independently.
- Stage 6 must profile two attributable states: the frozen hospital-only baseline and the complete hospital-plus-site baseline. Any optimization must state which state it changes.
- Stage 7 interiors and Stage 8 gameplay remain outside Stage 5S. Future gate animation may use the prepared roots, but no interaction behavior is implemented during site modeling.

### Required merge-back package

Stage 5S is not considered complete until its child results are written back into the parent roadmap. The merge-back must add:

1. A Stage 5S completion record and final status row in `HOSPITAL_EXTERIOR_PROGRESS.md`.
2. The approved site-master path and SHA-256, exported-zone manifest and source-change lineage.
3. Final renderer, triangle, material, collider and repeated-instance counts for the site alone and hospital-plus-site.
4. The final Unity additive-scene matrix and approved load combinations.
5. Gate/fence animation-root inventory, even if the future interaction behavior remains unimplemented.
6. Standard and player-eye visual QA for gate, parking, lake reserve, both sides, rear service and whole-site aerial views.
7. A Stage 5S checkpoint document under `Archive/HospitalExterior/Docs/` and links to all gates/reviews.
8. A new parent-roadmap immediate action that returns explicitly to Stage 6 Quest Link/Air Link profiling.

Completion record: all eight merge-back requirements were satisfied and the user approved the S6 Unity checkpoint on 2026-08-11. Stage 5S is complete; `HOSPITAL_EXTERIOR_PROGRESS.md` again controls the active Stage 6 work.

This contract provides one-way lineage:

`Frozen Stage 5/R44 → Stage 5S site expansion → merge-back checkpoint → Stage 6 profiling`

## 1. Locked brief

- Preserve the frozen Stage 5 hospital and its newest R40 authority unchanged.
- Build a complete, freely walkable hospital territory: perimeter fence, main gate, pedestrian passage, loop road, entrance drop-off, surface parking, rear service treatment, landscape, seating and restrained futuristic decoration.
- The site must feel futuristic, secure, welcoming, natural, calm and classy—not militarized, neon-heavy or excessively science-fictional.
- Design only inside the property boundary. No public road, neighboring buildings or environment outside the fence is included now.
- Use a daytime presentation with a gentle golden-hour character. Malaysia informs practical drainage and weather resistance, but the planting does not need to look specifically Malaysian.
- Reserve and fully compose a future lake garden, including its path, seating and shoreline space. The actual water feature is deferred.
- Keep every pedestrian path physically walkable at real scale. No gameplay or interaction logic is included in this content phase.

## 2. Existing hospital datum

The frozen hospital occupies approximately:

- X: `-36.8 m` to `+36.6 m` — about `73.4 m` wide.
- Y: `-32.8 m` to `+21.1 m` — about `53.9 m` deep.
- Z: ground to about `32.7 m` high.
- The hero entrance faces negative Y; rear/service is positive Y.

The proposed site is approximately `180 m × 175 m`:

- X boundary: about `-90 m` to `+90 m`.
- Y boundary: about `-100 m` at the main gate to `+75 m` at the rear.

These are planning dimensions, not a forced final rectangle. They may move by a few metres during blockout to improve circulation and composition.

## 3. Site-layout concept

```text
                              REAR / +Y
  ┌────────────────────────────────────────────────────────────────────┐
  │  Screened loading/service yard     Rear staff + motorcycle parking│
  │  and utility landscape             with trees and shaded path      │
  │                                                                    │
  │  ┌──────────────────────── TWO-WAY LOOP ROAD ────────────────────┐ │
  │  │                                                              │ │
  │  │            ┌──────────────────────────────┐                  │ │
  │  │            │                              │                  │ │
  │  │            │       EXISTING HOSPITAL      │                  │ │
  │  │            │        FROZEN STAGE 5        │                  │ │
  │  │            │                              │                  │ │
  │  │            └──────── HERO ENTRANCE ───────┘                  │ │
  │  │                    DROP-OFF FORECOURT                         │ │
  │  │                                                              │ │
  │  │  Future lake garden                         Visitor parking   │ │
  │  │  promenade, trees, seating                  and shaded walks  │ │
  │  └──────────────────────── LOOP ROAD ────────────────────────────┘ │
  │                Landscaped arrival boulevard                       │
  │                + permanently open pedestrian path                 │
  │                         MAIN GATE                                  │
  └────────────────────────────────────────────────────────────────────┘
                              FRONT / -Y
```

### 3.1 Arrival and main gate

- Place the main gate on the hospital entrance axis at approximately `(X 0, Y -100)`.
- Use one logical two-way vehicle entrance rather than many competing lanes. A subtle planted median may separate entry and exit near the portal without creating a complex checkpoint.
- Keep a dedicated pedestrian passage at least `3.0–4.0 m` wide. It remains visually obvious and physically open throughout the design.
- Form a composed gateway about `26–30 m` wide and roughly `7 m` high, using a long horizontal canopy/beam, sculpted columns and integrated hospital signage.
- Echo the hospital's horizontal bands, rounded canopy transitions, charcoal framing and restrained bronze accents.
- Prepare separate gate-leaf roots and clear sliding/pivot zones for future animation, but add no gameplay behavior now.
- A small architectural reception/security pavilion may be integrated into the gate composition, but it must read as hospitality and wayfinding rather than a fortified checkpoint.

### 3.2 Perimeter fence

- Target an overall architectural height of about `6.0–6.5 m` with taller feature posts near corners and the gate.
- Avoid a continuous blank wall. Use a `0.8–1.2 m` stone/concrete plinth, alternating solid architectural panels and tall vertical fins/screens above it.
- Maintain partial visibility through selected bays. The lower hospital floors may be hidden from outside, but silhouettes, landscape and upper architecture should appear through the rhythm.
- Use approximately `6–8 m` repeatable fence bays with dedicated straight, corner, transition and gate-termination modules.
- Prohibit spikes, razor details, prison-like mesh and visually aggressive security hardware.
- Place dense planting and gentle landform inside selected fence runs to soften the perceived height.

### 3.3 Vehicle circulation

- Use a continuous two-way loop approximately `7 m` wide around the hospital.
- Connect the main gate to a calm arrival boulevard and front drop-off without forcing vehicles through the parking aisles.
- Maintain realistic turning radii at the gate, drop-off, rear loading area and loop corners.
- Preserve a clear rear service/ambulance route while keeping it visually understated from the hero entrance.
- Use raised or clearly paved pedestrian crossings at the gate, drop-off and parking connections.

### 3.4 Drop-off and pedestrian circulation

- Create a linear/softly curved drop-off aligned with the existing lobby canopy, with roughly five short-stay positions and two accessible positions during blockout.
- Provide continuous `2.4–3.0 m` primary walkways between the gate, lobby, parking, lake garden and rear loop.
- Secondary garden/perimeter paths may reduce to about `1.8–2.2 m`, but no path becomes a decorative dead end without a real destination.
- Include curb ramps, level crossings, believable thresholds and sufficient clearance around trees, seats, bollards and sign structures.
- Keep a walkable inner-perimeter route so the complete territory can be explored.

### 3.5 Parking

- Use surface parking only in this phase.
- Place the primary visitor parking on the east/right side, keeping the front-centre view open for the hospital and gate composition.
- Place a smaller staff/motorcycle area toward the rear/right, separated from the service yard by landscape and screening.
- Initial planning target: about `60–75` car spaces and `20–30` motorcycle spaces, adjusted after blockout rather than forced into a fixed number.
- Standard planning modules: approximately `2.5 × 5.0 m` car bays, `6.0 m` two-way aisles and `1.0 × 2.2 m` motorcycle bays. Accessible bays and access strips receive their real required width during detailed planning.
- Use tree islands and shaded pedestrian spines so the parking does not read as one exposed asphalt field.
- Exclude EV-specific infrastructure for now.

### 3.6 Rear and service side

- Organize loading, waste/utility access and service maneuvering near the existing rear loading geometry, toward the rear-left/west side.
- Screen utilitarian functions using the high architectural fence language, planting and one consistent service canopy—not ad-hoc walls.
- Keep the loop road and pedestrian route continuous while clearly separating the loading maneuver zone from normal walking desire lines.
- Use the rear-right/east side for staff/motorcycle parking and a quieter shaded path.

### 3.7 Future lake garden

- Reserve approximately `28 × 38 m` on the front-left/west side, positioned so it becomes a pleasant discovery after crossing the gate without blocking the lobby approach.
- Design the full perimeter promenade, shoreline setback, large-tree positions, shaded seating pockets and two viewing nodes now.
- Keep a replaceable, collision-safe temporary ground/placeholder surface in the future water area so the site remains fully walkable before the lake is implemented.
- Avoid detailed water shaders, fountains, pumps, bridges and gameplay logic in this phase.
- Shape the surrounding grading so the location can later function as a visual lake and a believable rainwater-management feature.

### 3.8 Landscape and decoration

- Use an international, climate-plausible hospital-campus landscape rather than a location-specific Malaysian planting showcase.
- Create a controlled hierarchy: large canopy trees, medium ornamental trees, structured shrubs, grasses/groundcover and limited accent planting.
- Concentrate detail at the gate, arrival walk, lobby forecourt and lake garden. Keep rear/service planting simpler.
- Include shaded benches, integrated planters, restrained sculptural markers, wayfinding pylons, low bollards and subtle linear lighting housings.
- Futuristic character comes from proportion, clean joints, repeated geometry, integrated details and material contrast—not neon strips, holograms or excessive glowing objects.

## 4. Visual language and materials

### 4.1 Palette

- Warm off-white and light-grey architectural concrete/stone.
- Charcoal powder-coated aluminium for frames, fence modules, signs and bollards.
- Restrained refined-bronze accents at the gate, wayfinding and selected seating details.
- Warm grey pavers and kerbs; dark neutral asphalt with clean, restrained markings.
- Deep green foliage with limited lighter/silvery accents.
- Timber or timber-look seating inserts used sparingly for warmth.

### 4.2 Reuse the existing hospital material language

Where the role matches, map site assets to the existing shared Unity materials:

- `MAT_Aluminium_Charcoal`
- `MAT_Composite_WarmOffWhite`
- `MAT_Composite_LightGrey`
- `MAT_Neutral_Concrete`
- `MAT_Entrance_RefinedBronze`
- `MAT_Entrance_HonedTravertine`
- `MAT_LED_Warm_3200K` for future integrated fixtures, without enabling night lighting now

### 4.3 New site material roles

Keep the new family to approximately 8–12 controlled roles:

- road asphalt
- road/parking markings
- warm-grey exterior paver
- kerb/service concrete
- perforated or ribbed fence accent
- soil/mulch
- bark/trunk
- primary foliage
- accent foliage/grass
- seating timber
- lake placeholder/temporary infill if required

No object receives a unique material merely to change a small colour. Use shared materials, trim regions, UV variation and vertex colour where appropriate.

## 5. Blender source strategy

### 5.1 Use one coordinated site master

Use one new authoritative site-layout file rather than many independent Blender files:

`HospitalSite_Stage05S_R01_MASTER.blend`

Reasoning:

- Roads, parking, fence, lake reserve and landscape depend on the same coordinates and clearances.
- A single master prevents origin drift, mismatched grading and duplicated materials.
- The existing R40 hospital remains external and protected; it is never overwritten or absorbed into the new site authority.

Bring the hospital into the site file only as a locked, non-exporting reference collection named `REFERENCE_HOSPITAL_LOCKED`. The editable/exportable site lives under a separate `SITE_EXPORT` hierarchy.

### 5.2 Authoring collections

```text
SITE_AUTHORING
├── A_GUIDES_BOUNDARY_LEVELS
├── B_ROADS_DROPOFF_PARKING
├── C_PERIMETER_FENCE_KIT
├── D_MAIN_GATE_UNIQUE
├── E_REAR_SERVICE
├── F_LAKE_RESERVE_HARDSCAPE
├── G_LANDSCAPE_PLACEMENT
├── H_PROPS_WAYFINDING_SEATING
└── Z_QA_TEMPORARY

SITE_EXPORT
├── SITE_CoreRoadParking
├── SITE_PerimeterFence
├── SITE_MainGate
├── SITE_RearService
├── SITE_LandscapeHardscape
└── SITE_PropsVegetation
```

Temporary cameras, lights, dimension guides and review objects remain in `Z_QA_TEMPORARY` and never enter `SITE_EXPORT`.

### 5.3 Reusable modules

Create linked/instanced authoring modules for:

- straight fence bays, posts, corners and transition bays
- gate columns, portal beams and future movable leaves
- kerbs, tactile/edge paving and drainage channels
- parking bays, wheel stops and marking groups
- bollards, light housings, benches, bins, signs and planters
- a small controlled set of trees, shrubs and grass clusters

Keep authoring modular. Produce optimized export duplicates only at the export boundary, consolidating static meshes by zone/material where measurement shows a benefit. Never destructively join the editable master.

### 5.4 3D asset-generation policy

The Stage 5S site is authored in Blender. AI-generated 3D meshes such as Meshy output are not required and are excluded from the core source authority.

- Model the fence, gate, roads, parking, kerbs, drainage, lake reserve, planters, benches, bollards, signs and all hardscape directly in Blender.
- Build the tree/plant placement system in Blender from a small controlled library of reusable meshes or later approved licensed assets; do not generate a unique AI tree/prop for every placement.
- Use AI-generated images only as visual concept references. They never become production geometry or override approved measurements.
- An exceptional imported asset may be considered later only if it is manually reviewed for legal provenance, metre scale, topology, UVs, materials, pivot, collider strategy, LOD readiness and Unity performance. It must then be recorded as a separately versioned dependency.
- Do not use AI-generated meshes for the hero gate, fence kit, circulation surfaces, collision, repeated site modules or architectural hardscape. Those elements need deterministic authoring and reuse.

## 6. Unity integration strategy

Export each site zone at metre scale and the same global origin as R40. Add three new additive scenes:

- `Exterior_SiteCore` — roads, drop-off, parking, walkways, drainage, rear-service hardscape and simple collision.
- `Exterior_SitePerimeter` — fence and main-gate architecture, with future movable gate leaves kept separate.
- `Exterior_SiteLandscape` — lake reserve, trees, planting, seating, wayfinding and restrained decoration.

The frozen five Stage 5 hospital scenes remain unchanged. The site scenes load beside them.

Use Unity prefabs for repeated vegetation, seats, bollards, fence-detail modules and signs where instancing is valuable. Do not bake hundreds of repeated props into unrelated unique FBX meshes.

## 7. Performance-aware authoring rules

These rules apply while modeling, before the final Stage 6 profiling pass:

- Reuse mesh data for repeated fence bays, parking elements, trees, bollards, seating and lights.
- Keep the main gate unique and detailed; reduce detail progressively away from the arrival sequence.
- Avoid modelling invisible undersides, internal solids and tiny bevel segments that do not affect player-eye silhouettes.
- Keep transparent materials out of fences and decorations unless genuinely required.
- Use opaque perforated/ribbed geometry or cutout solutions deliberately; do not create many stacked transparent screens.
- Combine parking markings sensibly instead of creating one renderer for every line.
- Create simple dedicated collision: low-complexity road/walk surfaces, box/compound fence barriers, simple gate volumes and selective tree-trunk collision.
- Do not give every kerb, plant, parking marking or decorative panel an individual collider.
- Make vegetation LOD-ready and instancing-friendly, but perform final LOD distances and compression decisions only after Quest Link/Air Link profiling.
- Keep the full site independently unloadable so interior gameplay does not permanently pay for distant exterior detail.

## 8. Execution-ready modeling and review rounds

### 8.0 Common execution contract

Every round uses the same controlled sequence:

1. **Preflight:** verify the previous approved source and all protected inputs before opening or generating a new revision.
2. **Build:** work only in the round-specific Blender revision and authoring collections. Never edit R40 or a previously approved Stage 5S checkpoint in place.
3. **Measure:** produce machine-readable counts, bounds, dimensions and object/collection inventories from the saved result.
4. **Validate:** run an independent round validator against the saved `.blend`, not only the live build session.
5. **Review:** render the required standard views plus any round-specific close views at consistent camera positions.
6. **Checkpoint:** write the source SHA-256, input lineage, validator result, known limitations and user decision into the round review folder.
7. **Stop:** do not begin the next round until the current round is explicitly approved or a correction revision is requested.

Round statuses use only these values:

- `NOT_STARTED` — no round source exists.
- `WORKING` — a candidate exists but has not passed its full technical gate.
- `READY_FOR_REVIEW` — the candidate, measurements, validator and required images all pass; user approval is still pending.
- `APPROVED` — the user accepted the round and its hash is frozen as the next round's input.
- `CORRECTION_REQUIRED` — review found a material issue; create a new revision and retain the rejected evidence.

The expected baseline quality for every `READY_FOR_REVIEW` handoff is:

- Blender uses metres with `scale_length = 1.0`, Z-up, hospital front at negative Y and the shared R40 world origin.
- The frozen hospital is visible only through `REFERENCE_HOSPITAL_LOCKED`, is non-selectable/non-exporting, and its source SHA-256 still equals `0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58`.
- All new content uses the `S05S_` naming prefix and belongs to exactly one intended Stage 5S authoring or export collection.
- Temporary cameras, lights, dimensions and annotations are excluded from `SITE_EXPORT`; final round sources contain no accidental camera or light payload.
- No validation report may claim `PASS` if a required image, manifest, measurement or protected-input check is missing.
- Review renders must be readable at full-property scale and at `1.7 m` player-eye height; screenshots that hide circulation conflicts are insufficient.

### 8.1 Round deliverable matrix

| Round | Versioned Blender authority | Required evidence folder | Review decision |
|---|---|---|---|
| S1 | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R01_MASTER.blend` | `Reviews/HospitalExterior/Stage05S_RoundS1/` | Approve zoning, dimensions and circulation only |
| S2 | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R02_GATE_PERIMETER.blend` | `Reviews/HospitalExterior/Stage05S_RoundS2/` | Approve gate/fence language before propagation |
| S3 | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R03_CIRCULATION.blend` | `Reviews/HospitalExterior/Stage05S_RoundS3/` | Approve construction-scale hardscape and service circulation |
| S4 | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R04_LANDSCAPE.blend` | `Reviews/HospitalExterior/Stage05S_RoundS4/` | `CORRECTION_REQUIRED`; retained as rejected visual evidence |
| S4B | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R04B_LANDSCAPE_REDESIGN.blend` | `Reviews/HospitalExterior/Stage05S_RoundS4B/` | Approve high-fidelity vegetation, formal planting, road/path finish and water separation |
| S5 | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05_FINAL.blend` | `Reviews/HospitalExterior/Stage05S_RoundS5/` | Approve final Blender source and FBX reproduction |
| S5B | `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05B_GRASS_REFINEMENT.blend` | `Reviews/HospitalExterior/Stage05S_RoundS5B/` | Approve the hybrid PBR/3D lawn revision and seven-FBX runtime asset contract |
| S6 | Approved R05B becomes the Blender authority; Unity receives versioned Stage 5S assets | `Reviews/HospitalExterior/Stage05S_RoundS6/` | Approve the additive Unity checkpoint and merge-back package |

### 8.2 Round S1 — complete site blockout

#### S1 objective

Prove the complete property layout at real scale before detailed modeling. S1 approves space allocation and routes, not final forms, materials or planting.

#### S1 step-by-step execution

1. Verify the R40 SHA-256 and record its measured bounds (`X -36.8 to +36.6`, `Y -32.8 to +21.05`, `Z -0.48 to +32.71`). Abort the build on a mismatch.
2. Create the Stage 5S tool, source and review directories without moving or overwriting any Stage 4/5 file.
3. Initialize `HospitalSite_Stage05S_R01_MASTER.blend` with metric units, shared origin, approved collection hierarchy and round metadata.
4. Link R40's `UNITY_EXPORT` collection as a locked collection instance under `REFERENCE_HOSPITAL_LOCKED`. Mark the reference and collection as non-exporting.
5. Establish the approximate `180 × 175 m` property envelope, ground datum, boundary opening, hospital keep-clear zone and gate envelope with simple labeled geometry.
6. Block the nominal `7 m` two-way loop, arrival boulevard, drop-off route and rear-service connection. Keep the gate-to-drop-off route independent from parking aisles.
7. Block the front-right visitor parking at `60–75` spaces, including visible accessible-space allowances; block the rear-right staff/motorcycle zone separately.
8. Block the rear-left service yard with a maneuvering zone that does not consume the pedestrian route or close the vehicle loop.
9. Reserve the front-left lake garden at approximately `28 × 38 m`, add a replaceable collision-safe placeholder and show its complete promenade.
10. Add the permanently open `3–4 m` pedestrian gate passage, `2.4–3.0 m` primary paths, `1.8–2.2 m` secondary/perimeter paths and explicit road-crossing locations. Every route must join a loop or destination.
11. Save the source, calculate its SHA-256 and run the independent S1 validator against a fresh background open of the saved file.
12. Produce top-down, gate approach, player-eye lobby approach, left, right and rear views plus a measurement manifest and a concise review checklist.

#### S1 required deliverables

- `Tools/Stage05S/build_stage05s_round_s1.py` — deterministic blockout builder and review renderer.
- `Tools/Stage05S/validate_stage05s_round_s1.py` — independent saved-file validator.
- `HospitalSite_Stage05S_R01_MASTER.blend` at the path in the matrix.
- `Stage05S_RoundS1_Measurements.json` — site bounds, road/path widths, reserve dimensions and capacity counts.
- `Stage05S_RoundS1_BuildAudit.json` — input/output hashes, object/collection counts and build status.
- `Stage05S_RoundS1_Validation.json` — named checks, measured quality data and overall result.
- Six `Stage05S_RoundS1_*.png` standard review views at no less than `1600 × 1000`.
- `Stage05S_RoundS1_ReviewChecklist.md` — what is approved, what remains proxy-only and the questions requiring user review.

#### S1 expected deliverable quality and pass gate

- Property and all blockout content stay inside the approved planning envelope except explicitly labeled review cameras, which are removed before the source is saved.
- Hospital reference transform is identity at the shared origin; no R40 mesh, material or animation root becomes local/editable Stage 5S content.
- Nominal loop-road width measures `7.0 m`; vehicle gate opening is at least `7.0 m`; permanent pedestrian gate passage is at least `3.0 m`.
- Primary paths are at least `2.4 m`, secondary/perimeter paths are at least `1.8 m`, and the lake promenade forms a closed walkable loop.
- Visitor parking contains `60–75` car-space placeholders and at least two accessible-space allowances. Rear staff/motorcycle capacity is counted separately.
- The service yard, visitor parking, lake reserve and drop-off do not overlap the frozen hospital keep-clear envelope.
- The top view communicates all zones without ambiguity; the gate and player-eye views preserve a recognizable hospital reveal; side and rear views show usable clearances rather than compressed leftover space.
- S1 contains only blockout-quality geometry and controlled proxy materials. Detailed fence bays, final gate styling, vegetation, final kerbs/drainage, unique props, final UVs and Unity assets remain absent.

S1 review asks the user to decide only:

1. Is the hospital-to-site proportion comfortable at approximately `180 × 175 m`?
2. Are the gate, lake, parking and service zones in the correct locations and relative sizes?
3. Is the arrival/drop-off route legible without making the parking dominate the hero view?
4. Is there enough landscape and walking space on both sides and at the rear?

**S1 gate:** the layout must reach `READY_FOR_REVIEW`, then receive explicit user approval before any S2 gate or fence detail is modeled.

### 8.3 Round S2 — main gate and perimeter language

#### S2 authorized implementation specification

The user approved S1 on 2026-08-10. S2 must therefore use the frozen R01 SHA-256 `631c6cbf38c99457b9e3bab9d84a4d48dbaf03512df7c4574b37c3d64de8c459` as its only editable Stage 5S predecessor. The builder must abort if that hash, the exact frozen R40 `.blend1` snapshot hash or the active R40 structural fingerprint changes. An active-R40 byte-only resave must be audited explicitly and is not evidence of a design change by itself.

The S2 prototype is locked to these review dimensions and limits:

- Preserve the approved `30 m × 7 m` gate envelope, `8 m` arrival road and `4 m` pedestrian path positions.
- Provide at least `14 m` clear width through one logical two-way vehicle portal and at least `4.0 m` clear width through the permanently open pedestrian passage.
- Use an off-white horizontal portal/canopy, charcoal frames/fins and restrained refined-bronze accents mapped from the R40 material language.
- Integrate a small reception/welcome pavilion mass into the right gate termination without adding a checkpoint lane, barrier arm or fortified hardware.
- Create two separate pivot-ready vehicle gate-leaf roots, each with a modeled leaf, documented pivot, open presentation pose and unobstructed future sweep guide. Add no keyframes, actions, scripts or gameplay behavior.
- Resolve a `7.2 m` repeatable straight fence bay with an approximately `1.0 m` plinth and a total architectural height of about `6.2 m`.
- Demonstrate a visibly permeable vertical-fin rhythm, repeatable post, gate transition bay and one 90-degree corner condition.
- Limit propagation to two prototype bays on each side of the gate plus one isolated corner demonstration at the front-left property corner. The rest of the S1 low boundary proxy remains unchanged until S2 approval.
- Retain all S1 roads, paths, parking, lake, service and zoning geometry at their approved positions.

Required S2 deliverables are:

- `Tools/Stage05S/build_stage05s_round_s2.py`
- `Tools/Stage05S/validate_stage05s_round_s2.py`
- `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R02_GATE_PERIMETER.blend`
- `Reviews/HospitalExterior/Stage05S_RoundS2/Stage05S_RoundS2_DesignAudit.json`
- `Reviews/HospitalExterior/Stage05S_RoundS2/Stage05S_RoundS2_BuildAudit.json`
- `Reviews/HospitalExterior/Stage05S_RoundS2/Stage05S_RoundS2_Validation.json`
- outside, pedestrian, inside, long-oblique, fence-bay, corner and technical gate-plan review images at no less than `1800 × 1400`
- `Reviews/HospitalExterior/Stage05S_RoundS2/Stage05S_RoundS2_ReviewChecklist.md`

#### S2 step-by-step execution

1. Duplicate the approved S1 source to R02 and record the approved R01 SHA-256 as its sole Stage 5S input.
2. Build one `6–8 m` straight fence bay, a post, a permeable/solid rhythm variant, a corner and a gate-termination module.
3. Resolve the `26–30 m` wide, approximately `7 m` high hero gate around the approved S1 openings without moving the roads or pedestrian path.
4. Create separate left/right gate-leaf roots, pivots and unobstructed future travel zones; keep all leaves static.
5. Add hospital identification/signage hierarchy and restrained bronze accents using shared material roles.
6. Prototype only enough adjacent fence bays to judge repetition, transparency, upper-building reveal and landscape compatibility.
7. Validate module dimensions, pivots, repeated mesh reuse, sightlines and absence of aggressive/militarized details.
8. Render outside/inside gate views, pedestrian passage, long obliques, fence-bay close-up and animation-root evidence.

#### S2 expected deliverable quality

- Gate reads as calm hospital hospitality and wayfinding, not a security checkpoint.
- Pedestrian passage remains permanently and visibly open with at least `4.0 m` clear width.
- Fence reaches the approved architectural height without becoming a continuous opaque wall; the permeability rhythm is obvious in long views.
- Straight, corner and transition modules meet without gaps, double posts or scale drift.
- Gate-leaf roots have documented names, pivots and clear travel envelopes but no gameplay scripts or keyframed behavior.
- Repeated bays share mesh data where practical; final full-perimeter propagation is still deferred.

**S2 gate:** approve the first-impression design and modular fence kit before copying it around the property.

### 8.4 Round S3 — roads, parking, drop-off and rear service

#### S3 step-by-step execution

1. Duplicate approved R02 to R03 without moving accepted site zones or hero-gate anchors.
2. Replace road proxies with construction-scale road, kerb, island, drainage and crossing modules.
3. Resolve drop-off short-stay/accessible positions, turning transitions, pedestrian refuges and lobby thresholds.
4. Resolve visitor and staff/motorcycle layouts with full bay/aisle dimensions, accessible access strips and shaded pedestrian spines.
5. Complete rear loading/service maneuvering, screening and its separate walking route.
6. Propagate the approved S2 fence modules around the verified property boundary.
7. Add dedicated simple collision proxies and document which geometry is visual-only.
8. Validate route continuity, turning envelopes, slopes/steps, curb-ramp clearances, renderer strategy and repeated-module reuse.
9. Render circulation diagrams and player-eye evidence at every crossing, parking-to-lobby route and rear service conflict point.

#### S3 expected deliverable quality

- Roads, aisles and bays use credible planning dimensions with no vehicle route ending in an impossible turn.
- A pedestrian can travel gate → lobby → visitor parking → lake promenade → rear route → gate without an unplanned step, curb trap or decorative dead end.
- Accessible routes are continuous and visually legible; accessible bays are not token markings without access strips and curb transitions.
- Parking markings are grouped sensibly, repeated hardscape is modular, and collision does not rely on detailed blanket MeshColliders.
- Rear service reads operational but does not dominate the front or right-side public views.

**S3 gate:** circulation, collision scale and service realism pass before landscape density can conceal or complicate them.

### 8.5 Round S4 — lake reserve, landscape and site furniture

#### S4 step-by-step execution

1. Duplicate approved R03 to R04 with all circulation and fence positions protected.
2. Resolve the future lake shoreline reserve, full promenade, viewing nodes and replaceable temporary infill.
3. Establish a small reusable vegetation library with large, medium, shrub/grass and groundcover roles.
4. Place shade first along arrival, accessible routes, parking spines, seating and lake promenade; keep required clear widths unobstructed.
5. Add benches, bins, bollards, planters, wayfinding and restrained sculptural markers from reusable modules.
6. Simplify planting and props toward the rear/service areas while keeping screening effective.
7. Validate reused mesh data, collision selectivity, player-eye visibility, route clearance and future LOD readiness.
8. Render daytime/golden-hour whole-site, arrival, lake, parking, side, rear and seating-comfort views.

#### S4 expected deliverable quality

- Landscape creates a calm, shaded campus hierarchy without hiding the hospital, gate, crossings or wayfinding.
- The future water area remains a clearly replaceable, safe placeholder; no water shader or bridge is smuggled into scope.
- Furniture never narrows an approved route below its gate width and has believable human-scale spacing.
- Vegetation comes from a controlled reusable set, has documented collision policy and is ready for later LOD tuning.
- Decorative character remains restrained and architectural; no neon-heavy or excessive science-fiction treatment appears.

**S4 gate:** comfort, openness, shade, visual hierarchy and full-site walkability pass.

### 8.6 Round S5 — materials, UVs, export zones and final Blender QA

#### S5 step-by-step execution

1. Duplicate approved R04 to R05 and freeze the final authoring hierarchy before export optimization.
2. Apply the controlled shared palette and the approved `8–12` new site material roles.
3. Complete UV/trim/vertex-colour treatment and remove accidental unique materials.
4. Build non-destructive optimized duplicates under the six `SITE_EXPORT` zone collections.
5. Validate origins, transforms, normals, UVs, bounds, materials, names, mesh reuse, animation roots and temporary-object exclusion.
6. Export six versioned FBXs and immediately round-trip them into a clean Blender validation scene.
7. Compare source/export bounds, triangles, materials, roots and standard views; correct the source rather than patching FBXs.
8. Freeze the R05 hash, export manifest and final Blender review pack.

#### S5 expected deliverable quality

- Export zones reproduce the approved authoring result at the common origin with no source damage or hidden dependency.
- No temporary camera/light/guide enters an FBX; no animation-ready gate root is merged into static geometry.
- Material roles remain controlled, repeated meshes remain identifiable, transforms/normals/UVs pass and site-only counts are documented.
- Round-tripped FBXs match source bounds and intended renderer organization, with any deliberate consolidation explained.

**S5 gate:** the final Blender source and all six FBXs pass independent QA before Unity receives them.

### 8.7 Round S6 — Unity integration checkpoint

#### S6 step-by-step execution

1. Reverify R44 and all frozen Stage 5 hospital assets before importing any Stage 5S FBX.
2. Import Stage 5S content under versioned site-only paths and map shared/new materials without editing frozen hospital assets.
3. Create `Exterior_SiteCore`, `Exterior_SitePerimeter` and `Exterior_SiteLandscape` additive scenes.
4. Build prefabs for repeated vegetation, seats, bollards, signs and other deliberate instances; preserve gate-leaf roots.
5. Add/document simple collision and verify complete player traversal at `1.7 m` eye height.
6. Validate hospital-only, site-only and hospital-plus-site load matrices with no duplicate cameras, lights, geometry or material families.
7. Measure site-only and combined renderer, triangle, material, collider and instance counts.
8. Produce standard/player-eye visual QA, freeze the Stage 5S checkpoint and perform the parent-plan merge-back.

#### S6 expected deliverable quality

- The original five R44 scenes still load and validate unchanged without Stage 5S.
- Each new additive scene is independently unloadable and has clear ownership.
- Complete-site traversal has deliberate simple collision; decorative objects do not create an uncontrolled collider payload.
- Hospital-only and combined metrics are attributable, repeatable and ready for Stage 6 Quest Link/Air Link profiling.
- The merge-back package in Section 0 is complete; Stage 6 does not start merely because Unity import succeeded.

**S6 gate:** approve the additive Unity checkpoint, merge results into the parent roadmap, then return directly to actual Stage 6 Quest Link/Air Link profiling and optimization.

## 9. Approved design direction and current Stage 5S checkpoints

The user approved these four decisions on 2026-08-10:

1. Approximate `180 × 175 m` site envelope.
2. Lake garden on the front-left/west side and visitor parking on the front/right/east side.
3. Rear-left service yard with rear-right staff/motorcycle parking.
4. A tall `6.0–6.5 m` partially permeable architectural fence and a roughly `7 m` hero gate.

### 9.1 Round S1 implementation record — approved and frozen

Round S1 was implemented on 2026-08-10 as a deterministic, linked-reference blockout candidate:

- Candidate master: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R01_MASTER.blend`
- Candidate SHA-256: `631c6cbf38c99457b9e3bab9d84a4d48dbaf03512df7c4574b37c3d64de8c459`
- Protected R40 SHA-256 reverified: `0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58`
- Builder: `Tools/Stage05S/build_stage05s_round_s1.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s1.py`
- Technical gate: `PASS`, `50/50` named checks, zero failed checks.
- Blockout payload: `48` local Stage 5S mesh objects, `2,953` vertices, `2,092` triangles and `12` controlled proxy materials.
- Review evidence: `Reviews/HospitalExterior/Stage05S_RoundS1/`
- Review checklist: `Reviews/HospitalExterior/Stage05S_RoundS1/Stage05S_RoundS1_ReviewChecklist.md`
- User decision: `APPROVE S1`, recorded on 2026-08-10 in the current project task.

The candidate establishes:

- the full `180 × 175 m` planning envelope and R40 hospital keep-clear zone;
- a nominal `7 m` two-way loop, `8 m` arrival boulevard and `7 m` drop-off/service routes;
- `64` visitor spaces (`60` standard plus `4` accessible allowances), `12` staff-car spaces and `24` motorcycle spaces;
- five short-stay and two accessible drop-off placeholders;
- four first-order `8 m` clear-inside-radius turning guides for S1 review;
- the `28 × 38 m` future lake placeholder and closed `3 m` promenade;
- continuous `4 m`, `3 m`, `2.5 m`, `2.4 m` and `2 m` path roles with three explicit crossing locations;
- a `30 m × 7 m` gate envelope and low boundary proxy, not the S2 architectural design.

The candidate deliberately does **not** approve or contain the final gate, fence kit, construction kerbs/drainage, swept-path engineering, vegetation, furniture, final materials/UVs, collision, FBXs or Unity integration. The four turning rings are first-order layout evidence; detailed swept-path confirmation remains an S3 task.

### 9.2 Round S2 implementation record — approved and frozen

Round S2 was implemented on 2026-08-10 as a deterministic prototype derived from the approved S1 authority:

- Candidate: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R02_GATE_PERIMETER.blend`
- Candidate SHA-256: `26f16386353dd4f87a7d88b0f3b74abb7e3dc46aec0af15e35cc54c642c31d06`
- Builder: `Tools/Stage05S/build_stage05s_round_s2.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s2.py`
- Technical gate: `PASS`, `60/60` named checks, zero failures.
- Payload: `53` local S2 objects (`51` meshes and `2` pivot roots), `2,508` vertices and `3,156` triangles.
- Review evidence/checklist: `Reviews/HospitalExterior/Stage05S_RoundS2/`
- Review status: `APPROVED`; the user explicitly approved S2 and authorized S3 on 2026-08-10.

The candidate provides a `30 × 7 m` portal, `18.0 m` clear vehicle opening, `4.75 m` open pedestrian passage, right-side welcome pavilion, static pivot-ready twin gate leaves, restrained hospital signage and a modular `7.2 m × 6.2 m` permeable fence language. Evidence is intentionally limited to two transition and two straight gate-adjacent bays, six posts and an isolated two-bay corner. The full perimeter remains unpropagated.

Protected-source audit found that the active R40 `.blend` was externally resaved and now has byte SHA-256 `f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126`. The exact frozen bytes remain in `.blend1` at the recorded SHA-256 `0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58`; a full comparison of all `249` objects produced the identical structural fingerprint `bee6fbe715b59dfa1e4a3b6d792b3f7f2f2ac4dd1ca157871d32ed96baa0be76`. Neither protected file was replaced. Future builders must verify both the exact snapshot and structural fingerprint.

### 9.3 Round S3 implementation record — approved and frozen

Round S3 was implemented on 2026-08-10 as a deterministic circulation and construction-scale hardscape candidate derived from the approved S2 authority:

- Candidate: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R03_CIRCULATION.blend`
- Candidate SHA-256: `f6a50578738bd4b2f104c7a16a317c2eaf73f3bf8ca5c35f3374a267c40693a1`
- Approved input: R02 SHA-256 `26f16386353dd4f87a7d88b0f3b74abb7e3dc46aec0af15e35cc54c642c31d06`
- Builder: `Tools/Stage05S/build_stage05s_round_s3.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s3.py`
- Technical gate: `PASS`, `77/77` named checks, zero failures.
- Local S3 payload: `448` mesh objects, `17,284` vertices, `25,232` triangles and `88` unique mesh datablocks; all `53` approved S2 objects retain their saved structural fingerprint.
- Review evidence/checklist: `Reviews/HospitalExterior/Stage05S_RoundS3/`
- Review status: `APPROVED`; the user accepted S3 on 2026-08-10 after clarification of the swept-path and lake-reserve overlays.

The candidate preserves the accepted S2 gate and anchors, replaces exactly `37` S1 planning proxies, and provides an `8 m` arrival boulevard; `7 m` loop, drop-off, parking and service routes; four `8.0/12.5 m` swept-path guides; three grouped crossings and six 1:12 curb ramps; `60 + 4` visitor spaces; `5 + 2` drop-off positions; `12` staff-car and `24` motorcycle spaces; three rear loading bays; a separate `2 m` service walk; five reused parking-spine shade modules; grouped kerb/drainage/marking objects; full property-boundary fence propagation using `86` new regular approved modules plus six measured terminal closures; and `25` dedicated simple collision proxies with no kerb, marking or drainage colliders.

The approved S3 road, turning and access geometry remains the protected authority for every later round. The yellow `8.0/12.5 m` swept-path rings are guide-only QA geometry in `Z_QA_TEMPORARY`; they are not roads, collision or export content. S4B preserves the approved S3 geometry exactly and gives the future-water boundary a measured dry setback so water cannot run beneath the road. FBX export, Unity integration and runtime LOD production remain deferred.

### 9.4 Round S4 correction and S4B implementation record — approved and frozen

The original R04 (`134ba4fbbefabc18c175ce2560eb5aeef9cea74dad2ddfeb7c891d926a45cda1`) passed its technical gate but failed user visual review. Its sphere-like crowns, isolated grass scatter, small architectural pots and underdeveloped road/path finish did not match the hospital. It remains unchanged as `CORRECTION_REQUIRED` evidence and is not an authority for later work.

Round S4B was rebuilt on 2026-08-10 as a deterministic high-fidelity derivative of the untouched approved R03 `.blend1` snapshot:

- Candidate: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R04B_LANDSCAPE_REDESIGN.blend`
- Candidate SHA-256: `688b8c6c78b8a9747be300cca55e994c6533708ea9dcd923314e08728c9f03ef`
- Approved input: `HospitalSite_Stage05S_R03_CIRCULATION.blend1`, SHA-256 `f6a50578738bd4b2f104c7a16a317c2eaf73f3bf8ca5c35f3374a267c40693a1`
- Active R03 drift: preserved separately at `3d4ceb64fed55a8c6e08bf13f98c21d775f6b56c24a816810f70ad08b41c07b8`; not used as authority.
- Builder: `Tools/Stage05S/build_stage05s_round_s4b.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s4b.py`
- Technical gate: `PASS`, `51/51` named checks, zero failures.
- Review evidence/checklist: `Reviews/HospitalExterior/Stage05S_RoundS4B/`, including nine `1600 × 1200` review images.
- Review status: `APPROVED` by explicit user instruction on 2026-08-10; this exact R04B hash is frozen as the S5 authority.

S4B preserves all `448` approved S3 mesh objects and their geometry fingerprint. Its new visual payload contains `207` mesh objects, `61` unique meshes, `461,739` unique vertices, `447,440` unique triangles and `5,498,936` evaluated instance triangles across `22` PBR materials. The clean authoring source contains zero cameras, zero lights, zero actions and no accidental `SITE_EXPORT` payload.

The vegetation library now uses three continuous, tapered branch-and-twig tree families with `7,840–10,080` individually modeled leaves and `94,802–116,610` source triangles per family; no solid sphere or ellipsoid canopy primitives remain. The placed landscape contains `27` trees, `15` clipped in-ground topiary columns, `41` hedge modules, `16` allium flower drifts and `24` ornamental-grass tufts confined to designed lake beds. There are zero pots and zero isolated random lawn-grass objects. Route-conflicting rear and parking planting was removed after independent validation.

The hardscape finish adds eight PBR road shells, three parking shells, nine joined path shells, six continuous bevelled kerbs, sixteen detailed drain grates, four tactile panels and five framed solar parking-canopy upgrades while retaining all approved circulation and markings. The future-water boundary remains fully separate from traffic: minimum road clearance is `8.1834 m` and minimum swept-path clearance is `7.7960 m`, against required dry buffers of `4.0 m` and `3.0 m`; both overlap tests are false.

An external 3D-model generator was not required for S4B because the procedural Blender sources meet the requested modeled-leaf, branch and close-view authoring target. Runtime LOD derivation remains a later S5/S6 task and must preserve the approved close-view source quality.

The user approved this exact candidate and authorized S5 on 2026-08-10, with an additional requirement to preserve realistic high-quality assets while avoiding unnecessary runtime complexity or resource cost. S5 therefore uses non-destructive export optimization and LOD-ready vegetation; the approved R04B close-view authoring meshes remain untouched.

### 9.5 Round S5 implementation record — preserved evidence

Round S5 was built non-destructively from the exact approved S4B authority on 2026-08-10 and received its circulation-review correction on 2026-08-11:

- Candidate: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05_FINAL.blend`
- Candidate SHA-256: `04fad3c3cc06c967478d9b0ec2e67f598e845cbd3e9e12cc11562bd95e096a78`
- Approved input SHA-256: `688b8c6c78b8a9747be300cca55e994c6533708ea9dcd923314e08728c9f03ef`
- Builder: `Tools/Stage05S/build_stage05s_round_s5.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s5.py`
- Technical, spatial-layout and FBX round-trip gate: `PASS`, `87/87` named checks, zero failures.
- Export package: `Exports/HospitalExterior/Stage05S_R05_Final/`, six versioned FBXs totaling `32,863,704` bytes.
- Review evidence: `Reviews/HospitalExterior/Stage05S_RoundS5/`, including ten `1600 × 1200` source/export views, one clean FBX round-trip aerial and the saved round-trip Blender scene.
- Historical review status: `PRESERVED`; R05 remains immutable evidence and still passes `87/87`, but the later user-approved R05B authority—not R05—is the only S6 input.

The approved S4B authoring hierarchy, geometry and material assignments remain fingerprint-identical. S5 adds export-only duplicates with complete tiled UVs, a controlled `12`-role site palette plus approved shared hospital materials, two preserved gate-leaf animation roots, and zero camera/light/action or QA-guide payload. The final export omits superseded overlapping walk shells, legacy guide-band kerbs and malformed multi-slab route pieces; uses continuous site-wide procedural coordinates for road/paver joins; and replaces the oversized lobby threshold with a bounded arrival forecourt. The corrected circulation now uses five consistent `3.0 m` clear pedestrian paths with `3.46 m` edge courses, restores the previously invisible lake-to-hospital link, and retains one smooth `7.0 m` drop-off ribbon with correctly oriented zebra crossings and paired tactile landings.

The 2026-08-11 parking correction also fixes the underlying S1/S3 planning error instead of hiding it with draw-order changes. The S5 export-only visitor lot is redrawn as three `5.0 m` rows separated by two real `6.0 m` drive aisles; the west access spur now lands at the lower aisle centre (`y = -86.5 m`) rather than a bay row. The parking field is limited to the measured conflict-free envelope `x = 24.5–77.5 m`, `y = -94.5–-64.25 m`, leaving physical clearance from both the rounded front perimeter path and the loop road. The parking-lobby path stays outside the vehicle field and the cross-spine occupies a dedicated band above the top row. Twenty-four tiled solar-canopy modules now cover all three car rows; the rejected one-strip canopy layout is hidden with the other superseded authoring overlays. Capacity remains within the approved range at `60` spaces (`57` standard + `3` accessible).

The follow-up circulation correction removes the five short-stay marks and two accessible-bay overlays that had been painted directly on the active drop-off carriageway. All four zebra/tactile pairs are rebuilt from explicit road-width and pedestrian-direction data. In particular, the lobby/drop-off zebra is perpendicular to the local east/west lane at `(6.0, -37.72)`, rather than following the old diagonal kink. The parking-to-lobby path is no longer derived from the sparse inherited ribbon: it uses a 60-segment cubic transition with matched vertical tangents, a maximum sampled heading change below `3 degrees`, and a maximum inner/outer edge-length ratio below `1.22`. The matching parking-loop zebra moves to the route's true centre at `(14.57, -60.5)`. Matching visual and collision geometry is regenerated together, and the validator now rejects the old conflicting source objects, wrong aisle targeting, path/parking intrusion, hidden canopy copies, missing top-down evidence, excessive path curvature or collision/visual disagreement. Collision and LOD helper geometry remains exportable but hidden from both the review viewport and renders. All six FBXs reproduce their intended source bounds, used material names, modifier-evaluated triangles and UV presence at the common origin.

The final LakeGate cleanup trims only that west-side pedestrian ribbon and its collision copy. Its centreline ends at `x = -4.05 m`, compensating for the slightly angled half-width so the complete finish and edge meshes stop at `x = -4.0162 m` and `x = -4.0110 m` respectively—fully west of the dark road boundary at `x = -4.0 m`. The former continuation to `x = +9.0 m` is absent; the gate-axis zebra and every other circulation, parking and canopy object remain unchanged.

Performance preparation is deliberately simple and attributable. The `396` modular perimeter authoring objects are exported as four spatial visual chunks plus five simple fence barriers. Collision uses `20` route-surface proxies, the five fence barriers and one combined trunk proxy for all `27` trees; decorative meshes receive no blanket collision. The vegetation export contains three switchable tiers: LOD0 `5,422,208` triangles, LOD1 `1,192,298` (`21.99%` of LOD0) and LOD2 `351,826` (`6.49%`). With one vegetation tier active, the corrected complete site-export estimate is `5,463,890` triangles at LOD0, `1,233,980` at LOD1 or `393,508` at LOD2. Final LOD distances, compression and platform-specific material settings remain correctly deferred to S6 Quest Link/Air Link profiling.

### 9.6 Round S5B hybrid-grass refinement — approved and frozen

Round S5B was built on 2026-08-11 from the immutable R05 candidate without overwriting it:

- Candidate: `ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05B_GRASS_REFINEMENT.blend`
- Candidate SHA-256: `68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d`
- Immutable R05 input SHA-256: `04fad3c3cc06c967478d9b0ec2e67f598e845cbd3e9e12cc11562bd95e096a78`
- Builder: `Tools/Stage05S/build_stage05s_round_s5b.py`
- Independent validator: `Tools/Stage05S/validate_stage05s_round_s5b.py`
- S5B technical and seven-FBX round-trip gate: `PASS`, `64/64` named checks, zero failures.
- Original R05 regression gate rerun: `PASS`, `87/87` named checks, unchanged R05 hash.
- Export package: `Exports/HospitalExterior/Stage05S_R05B_GrassRefinement/`, seven FBXs totaling `32,759,908` bytes plus portable textures, mask, licence record and `GrassRuntimeSpec.json`.
- Review evidence: `Reviews/HospitalExterior/Stage05S_RoundS5B/`, containing ten inherited site views, four grass-specific `1600 × 1200` views, a readable combined FBX round-trip aerial, validation JSON, round-trip audit and saved round-trip Blender scene.
- User approval: 2026-08-11, exact authority SHA-256 above; S6 authorized against this hash only.

The continuous lawn now uses the four selected 2K CC0 maps from ambientCG `Grass005`—Base Color, NormalGL, Roughness and Ambient Occlusion—at a `2 m` physical tile with a rotated secondary colour sample and `14 m` macro variation. Roughness is constrained to `0.72–0.90`, normal strength is `0.32`, and the inherited lawn geometry remains fingerprint-identical with no displacement, parallax, particles or tessellation. The single-channel `2048 × 2048` world-space coverage mask maps the full `180 × 175 m` property, has a measured `45.5327%` lawn coverage ratio and excludes the designed non-lawn surfaces with a `0.15 m` clearance.

All `24` rejected ornamental-grass tuft groups (`72` LOD export objects) are physically absent from the S5B export. Trees, flowers, hedges, mulch beds, roads, paths, kerbs, buildings and lake treatments remain preserved. Static vegetation now contains exactly `99` complete three-tier LOD groups with `5,367,488` LOD0, `1,180,274` LOD1 and `348,274` LOD2 triangles.

The seventh FBX contains only two reusable opaque, vertex-coloured, UV-mapped `2 × 2 m` blade patches: LOD0 has `384` blade roots and `768` tapered crossed-ribbon triangles (`96` roots/m²), while LOD1 has `160` roots and `320` triangles (`40` roots/m²). Blade heights remain `3–9 cm`; the fixed-world-grid runtime contract uses LOD0 to `5 m`, LOD1/fade to `12 m`, `0.5–1.5 cm` future wind-tip motion, no per-blade colliders/GameObjects, an estimated `44,988` active triangles and no more than four instanced draw submissions. The Blender source realizes this system only in two movable review previews; full player-following GPU instancing remains explicitly deferred to S6 after approval.

### 9.7 Round S6 Unity integration checkpoint — approved and frozen

S6 was implemented on 2026-08-11 against only the approved R05B SHA-256 `68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d`. All active assets are versioned under `Unity/AnatomyXR/Assets/HospitalSite/Stage05S/R05B/`. The five frozen R44 scenes, ten hospital FBXs, hospital prefabs/materials and R44 records remain unchanged; the S6 source-change record reports zero road/path, landscaping or source-FBX edits.

The three production scenes are now present and enabled after the original five:

| Site scene | Ownership |
|---|---|
| `Exterior_SiteCore` | Roads, parking, paths, four crossings, drainage, rear service and 20 controlled route-surface collision proxies |
| `Exterior_SitePerimeter` | Four spatial fence chunks, five fence barrier boxes, main gate and two independent gate-leaf roots |
| `Exterior_SiteLandscape` | Lake placeholder, base lawn, 99 vegetation groups, furniture, 27 tree capsules and one grass runtime manager |

All scene-alone, site-only, hospital-only, default combined and all-eight load states pass with no duplicate geometry, cameras, lights, materials or grass managers. The controlled material map contains exactly 20 Stage 5S materials plus five referenced hospital materials. Collision is limited to the 20 route `MeshCollider`s, seven perimeter/gate boxes, five bench boxes, five bin capsules and 27 tree capsules; visual road/path detail and grass remain collider-free.

Vegetation is converted into exactly 99 `LODGroup`s—27 trees, 15 topiary columns, 41 hedge modules and 16 allium drifts—using the approved 18 shared meshes and exact `5,367,488 / 1,180,274 / 348,274` LOD triangle totals. Screen-relative thresholds are `0.60 / 0.22 / 0.06` with a 5% cross-fade width. Vegetation is non-static and instancing-compatible; only the authorized tier-specific shadow casters remain enabled.

The hybrid grass runtime uses the immutable `HospitalExterior.Stage05S.GrassRuntimeSpec.v1`, a generated config asset and one `HospitalSiteGrassRenderer`. The renderer snaps to a 2 m fixed grid, uses preallocated arrays and at most two `Graphics.RenderMeshInstanced` submissions. A `90 × 88` editor-baked occupancy grid conservatively accepts only full-patch lawn cells; the non-readable runtime GPU mask clips individual blades. Explicit hardscape/lake/hospital samples are blocked, known lawn samples are occupied, and close/top-down QA shows no road/path intrusion. The final editor proxy uses `15,744` active grass triangles, two submissions and zero steady-state GC bytes.

Site-only triangle totals including that active grass are `5,426,410 / 1,239,196 / 407,196` for forced LOD0/1/2. Hospital-plus-site totals are `6,070,742 / 1,883,528 / 1,051,528`. The site contains 153 fixed mesh renderers and 99 effective vegetation renderer streams; combined with the frozen 237-renderer hospital, the one-tier effective mesh-renderer count is 489. Loaded inactive LOD renderer components are recorded separately in the performance proxy.

The independent S6 Unity gate passes `16/16`. After integration, the R05B gate again passes `64/64` and the original R05 gate again passes `87/87`. The user approved the S6 visual checkpoint on 2026-08-11. The review package is `Reviews/HospitalExterior/Stage05S_RoundS6/`; the approved checkpoint is frozen at `Archive/HospitalExterior/Docs/HospitalExterior_Stage05S_RoundS6_Checkpoint.md`.

### 9.8 Current development boundary

Stage 5S is complete, approved and frozen. Its performance record remains explicitly an editor/D3D11 proxy rather than a headset gate. Control returns to the parent roadmap for Stage 6A: build a separate floor-tracked Quest traversal/profile harness with tracked controllers, physical movement, continuous locomotion, snap turn and route-safe teleportation, then measure hospital-only, site-only and combined route matrices through Link/Air Link. Mandatory 72 Hz is `13.89 ms`; preferred 90 Hz is `11.11 ms`. Do not begin Stage 7, permanent gameplay or real-room production until the Stage 6 gate passes.

## 10. Parent-plan return checklist

Before returning control to `HOSPITAL_EXTERIOR_PROGRESS.md`, verify:

- R40 and all ten Stage 5 input FBXs still match their frozen hashes.
- The original five additive scenes still pass the R44 scene matrix when loaded without Stage 5S.
- All Stage 5S source/export assets have unique versioned paths and manifests.
- Hospital-only and hospital-plus-site combinations both load without duplicate geometry, materials, cameras or lights.
- The entire site is walkable with documented simple collision and no blanket decorative MeshCollider payload.
- The parent document contains the final Stage 5S evidence, limitations and Stage 6 handoff.
- The S6 checkpoint is user-approved and the parent roadmap defines the authorized Stage 6A Quest Link/Air Link traversal/profile harness.

Return checklist disposition: `PASS`. Control returned to `HOSPITAL_EXTERIOR_PROGRESS.md` on 2026-08-11.

## 11. Generated concept references

Three workspace-bound concept images support design review:

- `Reviews/HospitalExterior/Stage05S_Concept/Stage05S_Concept_01_Masterplan.png` — zoning, circulation and whole-property balance.
- `Reviews/HospitalExterior/Stage05S_Concept/Stage05S_Concept_02_WholeSiteAerial.png` — intended full-site character, material restraint and landscape hierarchy.
- `Reviews/HospitalExterior/Stage05S_Concept/Stage05S_Concept_03_GateArrival.png` — player-eye first impression, pedestrian openness, fence rhythm and hospital reveal.
- Manifest and hashes: `Reviews/HospitalExterior/Stage05S_Concept/Stage05S_ConceptImageManifest.json`.

These are generated concept references, not construction drawings or geometry authorities. They may reinterpret exact hospital proportions, parking counts, fence height and road curvature. Priority is:

1. Frozen R40/Stage 5 hospital geometry and origin.
2. Approved written dimensions, circulation rules and blockout measurements in this plan.
3. Generated images for mood, composition, landscape density, material direction and player-eye intent.

The concept images show lake water to communicate the future composition; the actual Stage 5S implementation keeps the water feature deferred and uses a replaceable collision-safe placeholder until the lake is authorized.
