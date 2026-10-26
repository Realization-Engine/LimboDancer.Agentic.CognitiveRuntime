# R3: the referee of wounds and Field Promotions (the reviewer's report, kept as it came, 2026-10-26)

Read: p. 85 (A17 to A17.3, A18.1 to A18.2 and its table), pp. 203, 204 (D5.34 to D5.343), p. 50 (A4.4 to A4.44), p. 58 (A7.8), pp. 83, 84 (A15.2, A15.21, A15.42), p. 79 (A12.152, A12.153); the 4f and 4g diffs, `TargetState`, `Wound`, `Reduce`, `MfAllotment`, `MfCheck`, `LeaderBonus`, `LeaderIpcRecipient`, `SmcCrewAfter`, `RoutPinnedBar`, the Stun and wound writers. Holding up: the MPh-only pin, 3 1/2 counting, heroes and berserk pinned by a wound (A15.2, A15.42); the crew's Severity dr and Recall; every Stun writer through `AddSmcCrewSeverity`; the -1 only against an armored vehicle, taken from the record; the carry's bars, 5 PP not recouped (A4.4), no MF, a wounded leader lends no IPC.

1. **MAJOR: a broken SMC is pinned by a wound, and the pin then bars his rout.** `TargetState.Wound()` (ScenarioA1FireCalculator.cs about 5233), `ScenarioA1Wounds.PinnedByWound` (line 23), `SnipeUnit`'s WoundSeverity case (Snipers.cs about 893). A7.8 (p. 58): "Pinning affects broken units only during Interdiction ... there is no need to place Pin counters on broken units". A leader who spends more than 3 MF, breaks to DFF, and is later wounded in the same MPh is recorded broken and pinned; `RoutPinnedBar` (RoutCalculator.cs 709; Rout.cs 575) then refuses his rout. Fix: a `broken` fact in `PinnedByWound`; in `Wound()` pin only when not broken; a Rules and a Play test.
2. **MINOR: `woundPins` taken from the record and never checked** (`FireRecordVerifier`, LiveFire.cs 1147; `OrdnanceRecordVerifier`, LiveOrdnance.cs 871). Fix: refuse a recorded `true` when `PinnedByWound` from the state says false (older records never carry `true`).
3. **MINOR: the projector checks the carry but not the set-down** (movement-step apply, GameProjector.cs about 2359 to 2365). Fix: run `CarriedMoveBar` too.
4. **MINOR (boundary): the Search casualty's MF for A17.2 added up in Play** (Searching.cs 209 to 214). Fix: a Rules member taking the half-MF spent plus the Search's cost.
5. **MINOR: the Sniper's record says "wounded", never "pinned"** (`SnipeUnit` 877 to 893); the Fire path's `pinned-wound` has no record text of its own in MapStudio. Fix: "is wounded and pinned (A14.3, A17.2)" when the wound pins.
6. **NOTE: a new record lacking the crew's Severity dr replays with the old outcome** (`SmcCrewAfter` 4392 to 4399). Intended absent-safety; no fix now.
7. **NOTE: a carried wounded leader still gives his carrier the +2 MF bonus** (`LeaderBonus`, Rules 54 to 71). A17.2: "A wounded leader's two MF bonus for accompaniment throughout the MPh still applies". Defensible; state it in R71.16 with a test.
8. **NOTE: Double Time refused for any stack carrying a wounded man** (`MoveTiming`). Stricter than the page; keep and record in R71.16 as a reading.
9. **NOTE: a Passenger SMC not covered by the A17.2 pin** (`WithWoundPins`, PassengerTargets.cs 56). Backlog row.
10. **NOTE: a carrier cannot take a second man after setting down the first** (`CarryBar`). Harmless (10 PP leaves a MMC no MF).

Common questions: rules outside Rules: finding 4; the carry-keeping is a planner mechanism; `AfvDrm`'s precondition duplicates Rules' check (harmless). Absent-safety: none changes. Hidden information: nothing new beyond the known items.

Slip of the reviewer: one Grep for "pinned-ptc" pointed at `src/ASL` listed matching lines and file names under `src/ASL/boards`, `dl-tractor-works-01`, `-02`, `-03` and `p70-perf-tw` among them; none opened, nothing changed.
