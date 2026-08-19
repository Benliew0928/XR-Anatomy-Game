# Hospital Interior R03 S3D User Review Checklist

**Stage:** `S3D - full technical integration with empty floors`  
**Automated status:** `PASS` — static `17/17`, corrected Windows runtime `53/53`  
**Approval status:** `APPROVED 2026-08-19` — desktop and physical Quest Link/Air Link functional reviews passed

## Review player

`C:\CutMyBodyPlease\Exports\HospitalInterior\StageI1_R03_S3D_TechnicalIntegration\HospitalInterior_S3D_R03_TechnicalIntegration.exe`

F00-F06 are deliberately empty. This review approves only the combined technical baseline; it does not approve an F00 plan, interior design, anatomy content, standalone Quest/Android packaging, or final headset performance.

The current player includes the root-cause corrections for lawn collision, the drop-off wall, the F00 safety opening, the HUD reference, the F01-F06 main-window floor edge, and difficult elevator targeting. The floor edge now uses a solid `0.20 m` finish with the approved slab material, and controls use `5.5 m` reach with a `0.18 m` assisted target sweep. Repeat the desktop review from the beginning.

## Desktop functional route

Launch the player normally or with `-s3dMode Desktop`.

- [x] The player starts at the site gate and can follow the approved route toward the hospital.
- [x] WASD movement, Shift sprint, and held-RMB look operate correctly.
- [x] Walk onto several visible grass/lawn areas near the gate, approach, and drop-off; the player remains supported everywhere grass is displayed.
- [x] The outer and inner main-entrance leaves open automatically, remain open while occupied, and close after clearance without trapping the player.
- [x] Approach from the drop-off and from the right-side lawn shown in the defect screenshot, then enter through the left, center, and right portions of the visible main doorway; no invisible wall blocks either path.
- [x] Left-click operates the F00 landing call and cabin floor buttons without standing point-blank or aiming at a single tiny pixel.
- [x] Complete `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`.
- [x] At each stop, only the current floor is present, the cabin/landing doors align, and the main-window floor edge is continuously closed with a natural floor-matching finish.
- [x] Return through the automatic entrance and follow the exit route toward the gate.

Desktop result: `PASS`

Desktop notes: User completed and approved the corrected route.

## Quest Link/Air Link PC-VR functional route

Connect the Quest through Link or Air Link, make the Meta Quest Link OpenXR runtime active, then launch the same player normally or with `-s3dMode PCVR`.

- [x] One XR rig is active with correct headset and controller tracking.
- [x] Left-stick move and right-stick 45-degree snap turn operate correctly.
- [x] Physically walk or use stick movement onto visible lawn; the rig remains supported. Lawn is deliberately not a teleport destination.
- [x] A starts teleport aiming, B cancels it, and teleport lands only on approved site routes at F00 or the current empty-floor slab.
- [x] Teleport is unavailable inside the cabin and throughout elevator travel, then rebinds after arrival.
- [x] Right trigger operates both landing and cabin elevator buttons from a comfortable pointing distance with reasonable aim tolerance.
- [x] Complete `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00` without losing tracking, leaving the cabin, or loading more than one floor.
- [x] Return through the automatic entrance and follow the exit route toward the gate.

PC-VR result: `PASS`

PC-VR notes: User completed and approved the corrected Quest Link/Air Link route.

## Approval decision

- [x] Desktop functional route passed.
- [x] Quest Link/Air Link functional route passed.
- [x] No correction is required for the S3D technical baseline.

**User decision recorded 2026-08-19:** `APPROVE S3D`.

S3D is complete and locked. S4 F00 plan approval is authorized next; no F00 three-dimensional implementation is authorized before the S4 plan is explicitly approved.
