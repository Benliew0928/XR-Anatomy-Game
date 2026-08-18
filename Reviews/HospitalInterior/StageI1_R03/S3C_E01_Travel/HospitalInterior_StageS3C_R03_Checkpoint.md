# Hospital Interior R03 Stage S3C Checkpoint

**Active stage:** `S3C - E01 all-floor travel and safe scene transition`  
**Status:** `APPROVED_COMPLETE_S3D_AUTHORIZED`  
**S3B visual/door baseline:** approved and protected  
**S4:** blocked until explicit user functional approval

## Implemented scope

- The approved glass cabin, transparent shaft, portals and normal door system remain in a separate S3C scene. The copied car ceiling assembly is disabled and the F00-only `Exterior_Base` structural crossing is replaced by a shaft-cut copy; the building roof above F06 remains intact.
- E01 accepts F00-F06 cabin requests only when the player is physically inside the car and carries the player continuously with the moving cabin.
- Destination floor scenes validate behind closed interlocked doors; the previous committed floor is retained until exact arrival.
- Exactly one floor remains loaded at every completed endpoint, with the S3-owned shaft aperture applied to that runtime instance.
- Same-floor requests reopen, busy/invalid/obstructed requests reject safely, and landing calls operate only from the active landing.
- Cancellation and load failure roll back to the last committed floor. Failed rollback leaves E01 enclosed in `FaultedSafe`, with an explicit review-only recovery command.
- The required route is `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00`.

## Test scene

`Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S3C_R03_E01Travel_SIM_LOCAL.unity`

## Automated evidence

- Static scene gate: `PASS 25/25`.
- Windows-player runtime gate: `PASS 39/39`.
- Protected S2 baseline: `38` files, `0` mismatches.
- Locked S3B inputs: `12` files, `0` mismatches.
- Runtime route: `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00`.
- Review player: `Exports/HospitalInterior/StageI1_R03_S3C_AutoGate/HospitalInterior_S3C_R03_TravelReview.exe`.

User functional approval was explicitly received on 2026-08-18. S3C is complete and locked. S3D full technical integration with empty floors is authorized next; S4 F00 planning remains deferred.
