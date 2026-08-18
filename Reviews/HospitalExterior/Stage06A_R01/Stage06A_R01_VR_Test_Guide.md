# Stage 6A R01 — PC Simulator and Meta Quest 2/3 Test Guide

This guide explains two ways to exercise the Stage 6A PC-VR traversal harness:

1. **PC-only XR Interaction Simulator** — an Editor functional smoke test when no headset is available.
2. **Meta Quest 2 or Quest 3 through Quest Link or Air Link** — the required physical-headset review.

Stage 6A is currently `READY_FOR_HEADSET_REVIEW`. A simulator run is useful, but **can never approve Stage 6A**. Only a real Quest session, its evidence, and the explicit `APPROVED` / `CORRECTION_REQUIRED` decision can do that.

## What each method proves

| Method | It can verify | It cannot verify |
| --- | --- | --- |
| Unity XR Interaction Simulator | The action-based rig starts, the simulated HMD/controllers drive the project input actions, continuous move and snap-turn logic work, and the right-hand teleport logic accepts/rejects targets as designed. | True floor height, physical scale, tracking quality, Guardian movement, comfort, cable/Wi-Fi stability, real controller ergonomics, or headset performance. |
| Quest Link / Air Link with a Quest 2 or Quest 3 | The complete Stage 6A review checklist, including physical tracking, floor calibration, comfort and the real route traversal. | Results for a different headset, PC, cable, router or connection mode. |

The project uses Unity `6000.3.20f1`, XRI `3.5.1`, OpenXR `1.17.1` and the Input System `1.20.0`. It already contains the XRI package and its **XR Interaction Simulator** sample; importing the sample is not a package upgrade.

## Stage 6A test contract

Use the `Combined` profile for both the PC smoke test and the first physical review. It starts at the main gate and loads the eight frozen production scenes once.

| Harness control | Quest controller | Simulator input |
| --- | --- | --- |
| Continuous move | Left stick | Select the left controller with `[` and use `I` / `K` / `J` / `L` on its primary stick. |
| Snap turn | Right stick left/right | Select the right controller with `]` and use `J` / `L` on its primary stick. |
| Teleport aim and commit | Hold right **A**, aim, release | Select the right controller with `]`; hold `1` to emulate A, position/rotate the controller until the ray reaches a target, then release `1`. |
| Cancel teleport | Right **B** | With the right controller selected, press `2`. |

The expected review route is:

`Gate -> Lobby Forecourt -> Visitor Parking -> Lake Path -> Rear Service -> Gate Return`

Only the exact 20 approved route colliders accept teleportation. Grass, mulch, lake, hospital meshes, benches, trees, fences, furniture and other decorative objects must reject it.

## Before either test

1. Open `C:\CutMyBodyPlease\Unity\AnatomyXR` with Unity `6000.3.20f1`.
2. Do not edit or save any R44 hospital or R05B/S6 site scene or prefab. The protected production inputs must remain unchanged.
3. Do not change the normal eight-scene **File > Build Settings** list. The Stage 6A Windows player has its own explicit nine-scene build list.
4. In Unity, run **Hospital > Stage 6A > Validate R01 technical gate** if you need a pre-test check. The expected result is `PASS` with zero protected-input mismatches.
5. For a physical headset review, use the supplied development player, not a rebuilt player:

   `C:\CutMyBodyPlease\Exports\HospitalExterior\Stage06A_R01_PCVR\AnatomyXR_Stage06A_R01.exe`

## A. PC-only test with Unity XR Interaction Simulator

### What this route is for

Use this route to prove that the VR action paths and basic traversal logic are wired correctly before a headset is available. It runs inside the Unity Editor with simulated tracked devices; it is not a desktop/non-VR version of the player.

Do not add keyboard bindings to the project-owned Stage 6A action asset merely for this test. The simulator creates virtual XR devices, so the existing HMD, controller, stick, A and B bindings are exercised unchanged.

### 1. Import the local simulator sample

1. In Unity, open **Window > Package Management > Package Manager**.
2. Select **XR Interaction Toolkit** (`3.5.1`) from **In Project**.
3. Open the **Samples** tab and click **Import** for **XR Interaction Simulator**.
4. Unity imports the sample beneath `Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/`. These are test-support assets, not production content.

Unity documents this simulator as an Input System-driven virtual HMD and controller pair. The Stage 6A rig is action-based, which is required for the simulator to work. See [Unity's XR Device Simulator documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/xr-device-simulator.html) and [the XRI sample reference](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/samples.html).

### 2. Open the ready-to-run local test scene

1. Open `Assets/Hospital/Stage06A/R01/Scenes/Hospital_Stage06A_TraversalProfile_SIM_LOCAL.unity` **by itself**. The local scene is intentionally excluded from the supplied player build and technical gate.
2. This copy already contains the enabled **XR Interaction Simulator** prefab at the scene root, including its Input Action Manager and runtime control overlay. It does not modify the original traversal-profile scene or any frozen R44/R05B/S6 scene.
3. Keep the existing `Stage06A_XROrigin`, camera, interaction manager and bootstrap unchanged. There must still be only one project XR Origin and one project camera.
4. If the Editor has production scenes already open additively, double-click this local scene in the Project window so Unity opens it in **Single** mode before entering Play mode. The bootstrap then loads the production scenes itself; this avoids the expected duplicate-scene safeguard.

To recreate the local scene later, use **File > Save As** on `Hospital_Stage06A_TraversalProfile.unity`, name the copy `Hospital_Stage06A_TraversalProfile_SIM_LOCAL.unity`, and add **XR Interaction Simulator.prefab** only to that copy.

### 3. Start simulated VR

1. Click inside the Game view, then press **Play**.
2. Wait for the profile to finish loading. The default profile is `Combined`; it should load the eight production scenes and initialize 20 route-safe teleport surfaces.
3. If the simulator overlay is hidden, press `X` to show/hide its action menu and `Y` to show/hide its input-selection panel.
4. Move the simulated head as needed: press `H` to select/toggle head manipulation; use `W` / `A` / `S` / `D` to translate, `Q` / `E` to move vertically, and the arrow keys or right mouse button plus mouse movement to rotate. Press `R` to reset the selected simulated device.
5. Select/toggle the simulated left controller with `[` and the right controller with `]`. The simulator overlay identifies the selected device. Keep the right controller active before testing A, B, its stick or the teleport ray.

### 4. Run the simulator smoke-test sequence

1. Select the **left** controller (`[`). Ensure its primary 2D-axis target is active, then press `I` / `K` / `J` / `L`. Confirm camera-relative forward/back/strafe movement at the gate.
2. Select the **right** controller (`]`). Press `J` then `L` separately. Confirm one 45-degree snap in each direction; there must be no smooth turn.
3. With the right controller active, move and rotate it until the curved teleport ray reaches a road, path, crossing, parking surface or the entrance forecourt. Hold `1`, verify the green valid reticle, then release `1`. Confirm the teleport commits.
4. Repeat step 3 while aiming at grass, mulch, lake, hospital, bench, tree and fence. The reticle must be red or absent and releasing `1` must not move the rig.
5. Start a valid teleport aim, then press `2`. Confirm that B cancels it without moving the rig.
6. Optionally use simulated HMD and controller movement to inspect the six route locations in order. Treat this only as a visual/logic walk-through, not a completion of the headset route.
7. Exit Play mode. Unity discards runtime changes. Keep or delete the `LocalTest` scene at your discretion; it is not a Stage 6A deliverable and must not be added to Build Settings.

### Simulator controls that commonly matter

| Need | Default XRI 3.5.1 simulator control |
| --- | --- |
| Move selected HMD/controller | `W` / `A` / `S` / `D`; `Q` / `E` for vertical movement |
| Rotate selected HMD/controller | Arrow keys, or toggle mouse rotation with right mouse button |
| Select/toggle simulated HMD / left controller / right controller | `H` / `[` / `]` |
| Reset selected simulated device | `R` |
| Drive the selected controller's primary stick | `I` forward, `K` back, `J` left, `L` right |
| Right-controller A / B | `1` / `2` while the right controller is selected |
| Toggle the primary-stick target | `9` |
| Runtime simulator menus | `X` action menu; `Y` input-selection menu |

If `I` / `K` / `J` / `L` moves the controller but does not move or turn the rig, open the simulator input-selection panel and ensure **Primary 2D Axis** is enabled for the selected controller; `9` toggles that target. Do not change `Hospital_Stage06A_XR_Actions.inputactions` to work around a simulator configuration issue.

### Expected simulator limitations

- The Editor Game view is a monitor view, not stereo headset validation.
- The simulator can mark virtual devices as tracked, but it cannot prove real Quest tracking or hand assignment.
- A simulator session must not be imported as the required headset evidence and must not change the review decision.
- The compiled Stage 6A player is a PC-VR application; without an active XR runtime/headset it is not the right way to perform the simulation smoke test.

## B. Connect a real Meta Quest 2 or Quest 3

Use a **Quest Link cable first** for the initial acceptance run when possible. It reduces Wi-Fi variables. Air Link is appropriate when the local network is known to be stable.

### 1. Prepare the PC and headset

1. Update Windows, the Quest headset and the graphics driver, then restart the PC and headset.
2. Confirm the PC meets Meta's current Quest Link compatibility requirements. Download/install the current Meta Horizon Link / Meta Quest Link Windows application from [Meta Quest setup](https://www.meta.com/quest/setup/), sign in, and complete its device setup.
3. In the Meta PC application, verify that the Quest is listed as a device. If Unity starts flat on the monitor instead of in the headset, use the application's **OpenXR runtime** setting to make Meta Quest Link the active OpenXR runtime, then restart the player.
4. Charge both Touch controllers. In the headset, re-create or confirm a clear room-scale Guardian boundary and recalibrate the floor before testing.
5. Clear the real room of trip hazards. Keep enough room to take a few physical steps without reaching furniture, walls or the cable.

> The exact names and position of Quest system menus can vary with Horizon OS updates. If a label below is different, follow the current on-screen **Quest Link** / **Air Link** flow rather than changing Unity project settings.

### 2. Connect through a Quest Link cable

1. Start the Meta Horizon Link / Meta Quest Link PC application.
2. Connect the Quest to a direct USB 3.x port on the PC with a verified USB-C Link cable. Avoid low-speed hubs and extension chains for the acceptance test.
3. Put on the headset. If asked about USB data access, decline it for this test; then choose the in-headset **Quest Link** prompt/quick-setting to enter the PC-VR environment.
4. Confirm the PC application shows the headset as connected and the headset displays the Quest Link PC-VR home environment.
5. Leave the Meta PC application running, then continue to **Launch the Stage 6A player** below.

### 3. Connect through Air Link

1. Connect the PC to the router by Ethernet where possible. Connect the Quest to the same local network; a dedicated uncongested 5 GHz or 6 GHz Wi-Fi connection is preferred.
2. Start the Meta Horizon Link / Meta Quest Link PC application and enable its Air Link / wireless PC-VR option if it is not already available.
3. In the Quest quick settings or **System > Quest Link** area, choose **Air Link**, select this PC, and pair it. Confirm the matching code when the headset and PC application request it.
4. Launch the PC-VR connection from the headset and confirm the Quest Link PC-VR home environment appears.
5. If the PC does not appear, verify that both devices are on the same LAN, temporarily disable VPN isolation, restart both devices, and pair again. Do not change the Unity input or OpenXR project settings to compensate for a networking problem.

### 4. Launch the Stage 6A player

1. In the active Quest Link/Air Link PC-VR environment, run:

   `C:\CutMyBodyPlease\Exports\HospitalExterior\Stage06A_R01_PCVR\AnatomyXR_Stage06A_R01.exe`

2. Do not start SteamVR for this project unless a separate PC/runtime setup explicitly requires it. Stage 6A is configured for OpenXR and is intended to run through the active Meta Quest Link OpenXR runtime.
3. Allow the player to load `Combined`. At startup, you should be at the main gate with floor tracking, stereo view, two controller proxies and neutral QA daylight.
4. The development player accepts an optional profile argument, but do not use it for the Stage 6A acceptance route:

   ```powershell
   & 'C:\CutMyBodyPlease\Exports\HospitalExterior\Stage06A_R01_PCVR\AnatomyXR_Stage06A_R01.exe' -stage06aProfile HospitalOnly
   ```

   `HospitalOnly` and `SiteOnly` exist for later Stage 6B profile work. The required Stage 6A review profile is `Combined`.

## C. Physical headset review procedure

1. At the main gate, confirm natural exterior scale, correct floor contact, and no artificial camera-height offset.
2. Check that the left proxy follows the left Touch controller and the right proxy follows the right Touch controller.
3. Take a few physical steps inside the Guardian boundary; confirm that the virtual view follows naturally without drift.
4. Test left-stick forward/back/strafe and right-stick 45-degree snap turns. There is no smooth turn, jump, climb, grab or gameplay interaction in Stage 6A.
5. Hold right **A** to aim the curved ray. Teleport only onto approved roads, paths, crossings, parking, or entrance-forecourt surfaces. A green reticle indicates a valid landing; releasing A commits it.
6. Aim deliberately at grass, mulch, lake, hospital, benches, trees and fences. Confirm a red or absent reticle and no teleport on A release. Start a valid aim, press right **B**, and confirm cancellation.
7. Traverse the full ordered route: `Gate -> Lobby Forecourt -> Visitor Parking -> Lake Path -> Rear Service -> Gate Return`.
8. Inspect kerbs/crossings, gate clearance, canopy/head clearance, trees, benches, fences and rear-service boundaries. A fall recovery, clipping, getting stuck, a duplicate camera/object, missing material or broken grass is a review failure.
9. Exit the player normally after the route. It writes `Stage06A_RuntimeSession.json` and six route screenshots to the latest Windows session folder.
10. Back in Unity, run **Hospital > Stage 6A > Import latest headset session**. This copies the session JSON and screenshots into `Reviews/HospitalExterior/Stage06A_R01/HeadsetSession_<timestamp>/`.
11. Complete [Stage06A_R01_HeadsetReviewChecklist.md](Stage06A_R01_HeadsetReviewChecklist.md), including the headset model, Link/Air Link mode, refresh rate, render resolution, PC CPU/GPU/RAM and floor-calibration confirmation.
12. Select exactly one review decision: `APPROVED` or `CORRECTION_REQUIRED`. Do not claim performance acceptance; frame-time and optimization acceptance begin in Stage 6B–6D.

## Troubleshooting

| Symptom | Check first |
| --- | --- |
| Player starts on the monitor but not in the headset | Confirm the headset is inside Quest Link/Air Link PC-VR home, the Meta PC application is running, and Meta Quest Link is the active OpenXR runtime. Restart the player after changing the runtime. |
| Headset is missing in the Meta PC application | Restart headset and PC, reconnect to a direct USB 3.x port or re-pair Air Link, and update the Meta PC application. |
| Air Link cannot find the PC | Confirm PC Ethernet and Quest Wi-Fi use the same LAN; disconnect a VPN; re-pair after restarting both devices. |
| Floor is too high/low or body feels wrong | Recalibrate the Quest Guardian floor, recenter, exit the player and launch it again. Do not add a camera-height offset in Unity. |
| Left/right controller is missing or mapped incorrectly | Charge/wake the Touch controller, verify it appears in Quest Link home, recenter the headset, then restart the player. |
| Simulator appears but its controls do nothing | Confirm Play mode has focus, the XR Interaction Simulator prefab is enabled in the `_SIM_LOCAL` copy, and its input-selection panel has the selected device plus Primary 2D Axis enabled. |
| Simulator test seems to pass but no real session is available | Record it only as a functional smoke test. Stage 6A remains `READY_FOR_HEADSET_REVIEW`. |

## Evidence and decision boundary

After a physical run, the imported review folder must contain the runtime JSON and six route screenshots. The existing technical-gate evidence remains in this folder:

- [Stage06A_R01_UnityGate.json](Stage06A_R01_UnityGate.json)
- [Stage06A_R01_ProtectedInputManifest.json](Stage06A_R01_ProtectedInputManifest.json)
- [Stage06A_R01_PCVRBuild.json](Stage06A_R01_PCVRBuild.json)
- [Stage06A_R01_HeadsetReviewChecklist.md](Stage06A_R01_HeadsetReviewChecklist.md)

The Stage 6A technical gate already validates the saved assets. The simulator helps catch editor-side action and traversal regressions; the one real Quest 2/3 Link or Air Link run supplies the remaining physical evidence required for the user review decision.
