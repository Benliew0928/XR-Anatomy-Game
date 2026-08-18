# Hospital Interior R03 Stage S3D Checkpoint

**Active stage:** `S3D - full technical integration with empty floors`  
**Status:** `IMPLEMENTED_PENDING_AUTOMATED_AND_USER_REVIEW`  
**S3C:** approved, complete, and protected  
**S4:** deferred until S3D user approval

## Integrated scope

- One bootstrap combines the approved R05B site, four R44 exterior scenes, the S3C E01 system, and exactly one approved empty S2 floor.
- `Exterior_InteriorShellPreview` is excluded so it cannot duplicate the S2 floor slabs.
- One adaptive XROrigin supports desktop review and Windows OpenXR through Quest Link/Air Link.
- Eight approved hero-entrance sliding leaves operate automatically at runtime from their frozen motion metadata.
- Site/exterior scenes remain persistent while E01 swaps exactly one F00-F06 scene.
- The exact approved ContinuousLawnVisual source mesh is editor-cooked as one S3D-only support collider before runtime static batching; it is walkable but remains excluded from teleport surfaces.
- The obsolete R43 `COL_Perimeter_Front` standalone safety wall is suppressed only in S3D because it crossed the integrated R05B drop-off route; the real R05B perimeter remains unchanged.
- F00's two original front safety-boundary segments are suppressed only at runtime and replaced around the actual metadata-derived entrance width.
- F01-F06 have a solid `0.20 m` entrance-facing main-glass floor-edge finish spanning X `-23.95..18.80` and Z `-17.30..-15.48`, using the exact approved S2 slab material.
- On every F01-F06 load, S3D moves the original invisible front safety boundary from Z `-15.50` to the enlarged floor edge at Z `-17.30` and widens it to X `-23.95..18.80`.
- Desktop and PC-VR elevator targeting uses `5.5 m` reach and a `0.18 m` occlusion-aware aim-assist sweep so the physical controls do not require point-blank positioning.
- S2/S3/R44/R05B authorities, the Stage 6A harness, and EditorBuildSettings remain protected.

## Test scene and player

`Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S3D_R03_FullTechnicalIntegration.unity`

`C:\CutMyBodyPlease\Exports\HospitalInterior\StageI1_R03_S3D_TechnicalIntegration\HospitalInterior_S3D_R03_TechnicalIntegration.exe`

## Deliberately deferred

F00 planning, rooms, corridors, furniture, visual finishing, upper-floor design, anatomy content, standalone Quest/Android packaging, final headset-performance acceptance, and S4-S8 work.

Automated gates support review but do not grant S3D approval. Complete one desktop route and one physical Quest Link/Air Link route before authorizing S4.
