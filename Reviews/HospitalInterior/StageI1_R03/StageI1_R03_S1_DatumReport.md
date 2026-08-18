# Hospital Interior R03 S1 Measured Datum Report

**Status:** `PASS`  
**Coordinate contract:** Unity X/Z horizontal, Unity Y vertical; shared origin and unit scale.

## Protected exterior

- R40 authority: `f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126`
- Editor build settings: `62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb`
- `Assets/Hospital/Scenes/Additive/Exterior_Base.unity`: `b063aa138ac9c30ef79337c6973534d6343038456dfb507b45bcd4fba00935be`
- `Assets/Hospital/Scenes/Additive/Exterior_Tower.unity`: `660d26b776bb28496087af37abebf034b2ac8e4b5e67a44be40b5989719ebd6c`
- `Assets/Hospital/Scenes/Additive/Exterior_SidesService.unity`: `edc11308773951d173261ed54bc9f5b151a0dc62b1a9911fa5a48f5c398588a3`
- `Assets/Hospital/Scenes/Additive/Exterior_Interactions.unity`: `bf226e73789c4066e39fdad45f5c061211d69938bc4acdc9c70f824f36f91d47`

### Production root transforms

- `Exterior_Base/EXT_GlobalStructure: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Base/EXT_GroundEntrance: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Base/R43_CollisionPrototype: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Tower/EXT_FrontTower: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Tower/EXT_Balconies: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_SidesService/EXT_LeftFacadePodium: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_SidesService/EXT_RightFacadeWing: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_SidesService/EXT_RearService: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_SidesService/EXT_RoofService: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_INNER_SlidingLeaf_01_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_INNER_SlidingLeaf_02_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_INNER_SlidingLeaf_03_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_INNER_SlidingLeaf_04_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_OUTER_SlidingLeaf_01_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_OUTER_SlidingLeaf_02_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_OUTER_SlidingLeaf_03_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03B_DOOR_OUTER_SlidingLeaf_04_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03C_REAR_FireExit_B_Leaf_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03C_REAR_LoadingDoor_Leaf_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03C_REAR_ServiceDoor_A_Leaf_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`
- `Exterior_Interactions/DOORPREFAB_UE_S03C_ROOF_AccessDoor_Leaf_ROOT: position=(0.000,0.000,0.000); rotation=(0.000,0.000,0.000); scale=(1.000,1.000,1.000)`

## Expected versus measured datums

| Floor | Expected Y | Measured Y | X bounds | Z bounds | Result |
|---|---:|---:|---:|---:|---|
| F00 | 0.00 | 0.00 | -35.05..36.00 | -22.10..16.35 | PASS |
| F01 | 5.80 | 5.80 | -34.70..16.70 | -15.50..15.50 | PASS |
| F02 | 9.70 | 9.70 | -34.70..16.70 | -15.50..15.50 | PASS |
| F03 | 13.60 | 13.60 | -34.70..16.70 | -15.50..15.50 | PASS |
| F04 | 17.50 | 17.50 | -34.70..16.70 | -15.50..15.50 | PASS |
| F05 | 21.40 | 21.40 | -34.70..16.70 | -15.50..15.50 | PASS |
| F06 | 25.30 | 25.30 | -34.70..16.70 | -15.50..15.50 | PASS |

## Entrance and roof

- Entrance apron top: `0.060 m`
- Entrance threshold top: `0.100 m`
- F00 interior-floor vertical extent: `0.000..0.270 m`
- Threshold step from apron: `0.040 m`
- F06 floor top / ceiling bottom: `25.360 / 28.730 m`
- F06 clear height: `3.370 m`

## E01 reference

The reference cage occupies X `0.0..6.0 m` and Z `3.0..5.7 m`. Containment result: `PASS`.

- Expected datum/slab crossings: F00 ground-floor diagnostic plane, UE_EXT_InteriorShell_INTERIOR_L02_02, UE_EXT_InteriorShell_INTERIOR_L03_02, UE_EXT_InteriorShell_INTERIOR_L04_02, UE_EXT_InteriorShell_INTERIOR_L05_02, UE_EXT_InteriorShell_INTERIOR_L06_02, UE_EXT_InteriorShell_INTERIOR_L07_02
- Unexpected geometry intersections: `none detected`
- Method: Renderer AABB containment against the locked E01 X/Z footprint; seven datum-plane crossings are expected, while the four evidence views remain authoritative for mesh-level clipping.

## Integration results

- Upper floor band/slab alignment: PASS: F01-F06 expected/measured slab elevations and X/Z bounds are within 0.01/0.02 m tolerances.
- Duplicate production scene: PASS: explicit contract contains four unique production scenes; runtime gate verifies each loads exactly once.
- Duplicate shell: PASS: Exterior_InteriorShellPreview is measurement-only and is not loaded or referenced by the bootstrap scene.
- Clipping: PASS_STATIC: diagnostics have no colliders and no footprint/elevation bound exceedance; visual approval remains pending.
- Gaps/floating slabs: PASS_STATIC: all six upper slabs match the production envelope and locked elevations; visual approval remains pending.
- Blocked doors: PASS: diagnostic colliders=0 and approved entrance ground/apron/threshold collision remains traversable at a 0.040 m step.

No legacy interior-shell scene or site scene is loaded by S1. Automated measurements support, but never replace, user inspection of the four named evidence images.
