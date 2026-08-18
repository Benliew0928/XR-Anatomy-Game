# Hospital Interior R03 S3D User Review Checklist

**Stage:** `S3D - full technical integration with empty floors`  
**Automated status:** `PASS` — static `17/17`, corrected Windows runtime `53/53`  
**Approval status:** correction build pending desktop and physical Quest Link/Air Link functional review

## Review player

`C:\CutMyBodyPlease\Exports\HospitalInterior\StageI1_R03_S3D_TechnicalIntegration\HospitalInterior_S3D_R03_TechnicalIntegration.exe`

F00-F06 are deliberately empty. This review approves only the combined technical baseline; it does not approve an F00 plan, interior design, anatomy content, standalone Quest/Android packaging, or final headset performance.

The current player includes the root-cause corrections for lawn collision, the drop-off wall, the F00 safety opening, the HUD reference, the F01-F06 main-window floor edge, and difficult elevator targeting. The floor edge now uses a solid `0.20 m` finish with the approved slab material, and controls use `5.5 m` reach with a `0.18 m` assisted target sweep. Repeat the desktop review from the beginning.

## Desktop functional route

Launch the player normally or with `-s3dMode Desktop`.

- [ ] The player starts at the site gate and can follow the approved route toward the hospital.
- [ ] WASD movement, Shift sprint, and held-RMB look operate correctly.
- [ ] Walk onto several visible grass/lawn areas near the gate, approach, and drop-off; the player remains supported everywhere grass is displayed.
- [ ] The outer and inner main-entrance leaves open automatically, remain open while occupied, and close after clearance without trapping the player.
- [ ] Approach from the drop-off and from the right-side lawn shown in the defect screenshot, then enter through the left, center, and right portions of the visible main doorway; no invisible wall blocks either path.
- [ ] Left-click operates the F00 landing call and cabin floor buttons without standing point-blank or aiming at a single tiny pixel.
- [ ] Complete `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00`.
- [ ] At each stop, only the current floor is present, the cabin/landing doors align, and the main-window floor edge is continuously closed with a natural floor-matching finish.
- [ ] Return through the automatic entrance and follow the exit route toward the gate.

Desktop result: `PENDING`  
Desktop notes:

## Quest Link/Air Link PC-VR functional route

Connect the Quest through Link or Air Link, make the Meta Quest Link OpenXR runtime active, then launch the same player normally or with `-s3dMode PCVR`.

- [ ] One XR rig is active with correct headset and controller tracking.
- [ ] Left-stick move and right-stick 45-degree snap turn operate correctly.
- [ ] Physically walk or use stick movement onto visible lawn; the rig remains supported. Lawn is deliberately not a teleport destination.
- [ ] A starts teleport aiming, B cancels it, and teleport lands only on approved site routes at F00 or the current empty-floor slab.
- [ ] Teleport is unavailable inside the cabin and throughout elevator travel, then rebinds after arrival.
- [ ] Right trigger operates both landing and cabin elevator buttons from a comfortable pointing distance with reasonable aim tolerance.
- [ ] Complete `F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00` without losing tracking, leaving the cabin, or loading more than one floor.
- [ ] Return through the automatic entrance and follow the exit route toward the gate.

PC-VR result: `PENDING`  
PC-VR notes:

## Approval decision

- [ ] Desktop functional route passed.
- [ ] Quest Link/Air Link functional route passed.
- [ ] No correction is required for the S3D technical baseline.

After both routes pass, record explicit approval as: `APPROVE S3D`.

S3D remains pending until that approval is received. The next authorized stage afterward is S4 F00 plan approval.
