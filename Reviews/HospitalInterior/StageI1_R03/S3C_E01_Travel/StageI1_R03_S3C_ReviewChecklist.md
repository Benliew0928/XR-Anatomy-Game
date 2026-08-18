# Hospital Interior R03 — S3C Functional Review

**Automated status:** `PASS — static 25/25; Windows runtime 39/39`  
**User functional approval:** `PENDING`  
**Next stage:** S4 remains blocked until explicit approval.

## Launch

Run `Exports/HospitalInterior/StageI1_R03_S3C_AutoGate/HospitalInterior_S3C_R03_TravelReview.exe`.

## Controls

- `WASD`: walk; hold `Left Shift` to sprint.
- Hold `Right Mouse Button` and move the mouse: look.
- Aim the centre reticle at a control and press `Left Mouse Button`: interact.
- `0`–`6`: request F00–F06 while physically inside the cabin.
- `O` / `C`: door open / close.
- `L`: call E01 from the currently active landing.
- `K`: toggle the doorway obstruction test proxy.
- `X`: cancel an active trip and test rollback.
- `R`: review-only recovery after a `FaultedSafe` test.

## User review route

1. At F00, open the doors and walk fully into the cabin.
2. Travel F00 → F01 → F02 → F03 → F04 → F05 → F06 → F00.
3. At each arrival, confirm the cabin stops level with the slab, the display names the correct floor, and only that landing door opens with the cabin doors.
4. During travel, confirm the player remains physically in the moving observation cabin and the shaft/floors visibly pass outside the glass.
5. At F00, look straight up through the cabin: neither the car roof nor the former `Exterior_Base` structural slab may obstruct the hollow F00–F06 path. The actual building roof is retained only above F06.
6. Confirm doors close before movement, do not expose an empty shaft, and reopen when the obstruction proxy is active.
7. Confirm a same-floor request reopens the doors and a request made while busy is rejected.
8. Confirm the final return to F00 is level, walkable, and connected to the existing hospital exterior.

## Approval decision

- [x] Approve S3C elevator travel and authorize S3D full technical integration with empty floors.
- [ ] Request S3C corrections; keep S4 blocked.

**User decision recorded 2026-08-18:** S3C approved. S3D technical integration is authorized before S4; F00 plan/design work remains deferred.

Automated reports and endpoint renders are in this folder. They support this review but do not replace the decision above.
