# Hospital Interior R03 Stage S4 Checkpoint

**Completed stage:** `S4 - F00 plan approval`  
**Approved plan:** `V03 - simple welcome lobby + one full-height playable staircase reservation`  
**Status:** `APPROVED_AND_LOCKED`  
**User decision:** `APPROVE S4 F00 PLAN V03` on 2026-08-19  
**S3D:** approved, complete, and protected  
**S5A:** planning/documentation authorized; implementation blocked pending separate plan approval

## Correction outcome

Candidate V01 was rejected because its fourteen-zone education-facility programme introduced unnecessary real-world support and gameplay spaces. Its files remain historical evidence only and cannot authorize S5.

Candidate V02 simplified F00 correctly but omitted the required staircase footprint. It remains unapproved historical evidence.

Candidate V03 uses the complete approved `2,598.865 m²` F00 footprint as one open, welcoming game lobby and reserves one full-height playable staircase. It adds no programme rooms or enclosed gameplay areas beyond the necessary stair enclosure. The user approved V03 on 2026-08-19; it is now the locked S4 authority.

The plan has five minimal lobby anchors plus the required staircase:

- `A01` — small game-facing welcome and F00-F06 directory marker, with no upper-floor purposes assigned;
- `A02` — two restrained lounge seating clusters at the hall edges;
- `A03` — one abstract anatomy-inspired decorative theme feature;
- `A04` — a generous, uncluttered E01 elevator landing;
- `A05` — two compact biophilic accents.
- `A06` — the exact prior playable Stair A footprint, X `-27.5..-22.5`, Z `-3.6..3.6`, with a clear east-side door landing.

`R01` is the direct entrance-to-E01 path with a `3.00 m` clear target. `R02` branches from the open lobby to Stair A with a `2.00 m` clear target. The west seating cluster moves south so the stair core, landing and approach remain unobstructed.

S5A must plan, then—only after separate approval of that plan—integrate and validate the complete Stair A from F00 through F06 first. `S5B` remaining F00 greybox work stays blocked until the implemented S5A staircase receives separate user approval.

## Theme direction

V03 follows the approved premium clinical-future language: warm off-white and soft stone, charcoal framing, restrained bronze, blue-grey glass/cool wayfinding light, soft indirect lighting, limited greenery, and one abstract anatomy cue. Exact finishes and objects remain deferred to S6.

## Locked constraints represented

- Exact R40 authority cross-section at `1.20 m`, containing `1,254` existing architecture segments.
- Approved eight-vertex F00 footprint, locked automatic entrance, S3D E01 aperture/cabin centre, and S2 arrival point.
- Exact prior playable Stair A footprint and switchback design parameters from `Tools/HospitalInterior/build_hospital_interior_r02.py`.
- Approved S2/S3D sources remain unchanged.
- F01-F06 purposes remain undecided. Future Stair A openings and landings are circulation infrastructure only.

## Approved V03 evidence

- `StageI1_R03_S4_F00_Plan_V03.png` — primary review board.
- `StageI1_R03_S4_F00_Plan_V03.svg` — measurable vector plan.
- `StageI1_R03_S4_F00_Plan_V03_Contract.json` — exact open-lobby, stair, anchor, route, stage-order, theme, and deferral contract.
- `StageI1_R03_S4_F00_Plan_V03_Gate.json` — automated plan-only gate.
- `StageI1_R03_S4_F00_Plan_V03_EvidenceRecord.json` — V03 hashes and authority references.
- `StageI1_R03_S4_F00_UserApproval.md` — explicit approval record and next-step authorization boundary.

Superseded V01/V02 review files, the correction record, and the completed review checklist are retained at `Archive/HospitalInterior/StageI1_R03/S4_F00_Plan/Superseded_2026-08-19/`. They remain recoverable historical evidence and cannot authorize implementation.

## Validation

The V03 plan gate passes `13/13`: the R40 cross-section is present; the single lobby matches the locked footprint; all six necessary anchors are inside F00; the exact `36.0 m²` prior Stair A footprint is reserved; no lobby anchor overlaps it; its east landing is inside F00; both routes are contained; E01 remains clear; the rejected programme is absent; upper-floor purposes remain undecided; Stair A integration is ordered before all other F00 work; and the review output contains no 3D geometry.

Automated validation proved consistency; the user's explicit approval completed the S4 design gate. S5A planning may now begin. No S5A implementation may begin until the user reviews and explicitly approves its implementation plan.
