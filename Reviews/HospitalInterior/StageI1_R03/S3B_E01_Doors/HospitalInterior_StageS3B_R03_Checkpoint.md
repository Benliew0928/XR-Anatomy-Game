# Hospital Interior R03 Stage S3B Checkpoint

**Active stage:** `S3B - approved glass observation cabin and normal door system`  
**Status:** `S3B_APPROVED_COMPLETE_S3C_AUTHORIZED_NOT_STARTED`  
**S3A:** explicitly approved  
**S3C travel:** not implemented; authorized as the next stage and intentionally not started

## Implemented scope

- Approved S3A cabin and landing FBXs imported byte-for-byte at identity scale.
- E01 remains centred at X 1.65 m / Z 4.35 m with 0.175 m side and 0.125 m front/rear structural clearances inside the locked operating cell.
- Cabin floor, threshold, and S2 slab share the exact 0.000 m walk datum after the documented -0.110 m model-root offset.
- Seven portals are fixed to the approved F00-F06 datums; only the F00 landing pair is active in S3B.
- A continuous E01 hoistway now spans Y -0.20 m through the approved S1 roof limit at Y 28.83 m, with uninterrupted side/rear walls, seven between-storey front closures, two full-height guide rails, a structural base closure, and a top closure.
- The full-height enclosure is now low-iron blue-grey transparent glass with 11 continuous glazed faces, 42 slim non-colliding charcoal/bronze frame members, visible brushed guide rails, and no opaque between-storey monolith above F00.
- The cabin is fully nested inside that shaft and now uses three transparent side/rear observation walls above the retained stainless impact panels and rear handrail. Ten slim charcoal/bronze frame members keep the cabin legible as a moving architectural object while preserving a clear standing-eye view of future floor transitions.
- S3-owned floor and underside meshes cut a continuous X 0.300..3.000 m / Z 3.000..5.700 m aperture through all seven datums. Runtime swaps only the loaded floor instance; protected S2 source assets stay byte-identical.
- The nominal cabin sweep keeps at least 0.075 m to the shaft walls, 0.040 m to the guide rails, 0.090 m at the F00 base, and 0.940 m below the F06 top closure.
- Two cabin and two current-landing leaves open and close together in 1.2 s, dwell for 4 s, reverse on obstruction, and interlock before movement can ever be declared safe.
- All twelve non-current landing leaves remain closed, collidable, and locked.
- Door-open, door-close, and seven landing-call controls are interactive. F00-F06 cabin floor requests remain safely disabled until S3C.

## Test scene

`Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S3B_R03_Doors_SIM_LOCAL.unity`

User approval is recorded. S3C may be planned or implemented next in a separate task; no S3C controller, travel, or scene-transition code exists in this checkpoint.
