# Scenario A1 Backlog Pass 71: Infantry IV: Snipers and wounds

The review of pass 71, written on 2026-10-26 after the five reviews, their four rounds of fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 71 Design](<ASL Unit Backlog Pass 71 Design.md>); its sections 8 to 14 say what each increment built, its section 15 holds the reviews, their fixes, and the Studio check, and its sections 16 and 17 the pass as built and the gate.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-71`, cut from 5379e06f (main, pull request 41): the frame (9e83e768, its estimates calibrated in 302c6c71 and 48dd96ea), seven increments in seven commits (dbf22030, 379c77dc, 88f93d8c, c6952864, cc72d08a, 128fed24, 2dd2789d), the reviews (6d3147b3), four rounds of fixes (d1c18a3a, a47712b7, 34862753, efdc4dae), the whole-pass check (889d4f1e), and these documents. Main's acronym work (0b5b9a1d, Codex's) was merged in at the user's word as 00fd8894; it is not the pass's, and the gate's base moved to it. Everything was done under the user's word of 2026-10-26, "Run Pass 71 without my intervention up to the merge gate, as passes 55 to 70 did: questions go to a sub-agent, decisions become rulings open to my veto, and the gate, push, and merge stay mine to start." The rulings R71.1 to R71.28 are decided under that word and open to the user's veto, as are the departures and the passes given to the rows left open. **The gate has not been run:** the word ends at the gate, and the gate, the push, the pull request, and the merge are each the user's to start. The gate's strict stage passed on 2dd2789d and again after fix Round D (0 warnings, 0 errors each time).

## What departs from the plan's entry and the frame

- **The pass is about four times the plan's size** (design section 2; ruling R71.1). The plan names 11 rows, two tasks, and 1:20 of building (2:35 in all); the calibrated forecast was 5:41 of building, about 8:10 to the gate. It stayed one pass, in seven increments, built in 5:29 by the clock; no increment was left out, so nothing went to pass 72.
- **Moved on** (ruling R71.2): Entrenching as a trigger, and the Fortification TEM and hidden Fortifications in A14.21's comparison, to pass 115; the captured MG's automatic Sniper dr at night to pass 90 (task 90.6); Interior Building Locations as non-targets to pass 135; subterranean Locations to pass 145 and Chapter G; a wounded man's 4 MF on cavalry, motorcycles, wagons, and boats to passes 165, 175, and 170; to the backlog: a Sniper Check by a subset, the RtPh wound pin, an empty unarmored vehicle as eligible (R71.4, a deviation), a 1MT AFV's Stun as Recall, an Armor Leader's Recall LLMC, G.M.D. leaders, and the early-DR order of one step.
- **The ordnance triggers were kept in** against a frame agent's proposal to move them to pass 110 (R71.13): one reader serves every record. R55.30 ("ordnance calls no Sniper") is superseded.
- **R71.9 was read with A14.4 at 4b:** a Sniper eliminated by a check or by an enemy Sniper reduces its side's SAN, and the counter leaves when the SAN falls below 2 (A14.4's "reduced to 1"); 4a had removed it always.
- **Eleven rulings were made at the reviews,** R71.18 to R71.28 (design F15), one of them a deviation in the target's favor (R71.21).
- **The unit-test rule's "over about 25, narrow first" was departed from once:** 4c's gate reaches every card test that starts play, and its agent ran about 85 such methods in batches by file (design section 10.1).
- **The frame's section 5, item 5 found stale notes:** A14.3's "no LLMC or LLTC follows" (inventory and backlog 25), A17.2's "4 MF on a conveyance" (built for vehicles), and backlog 55's Stun row naming "Passes 65 and 105": corrected.
- **Main moved under the branch:** Codex's acronym page was merged in (00fd8894), and its MapStudio controls were missing from the Icon Lab's coverage file, which 4c regenerated; main's scoped CI had not run the MapStudio tests. For the user.
- **Left out, with a backlog row each (section 74.1 of the backlog):** the moves above; the carry of a wounded man in the APh (A4.72; 4g).
- **The Studio check of the whole pass was scoped under the user's word,** with pass 70's as the model: the section below. The SMC crew's Stun game was not staged (the section says why).

**Departures of sections 8.1 to 14.1 that the reviews closed,** and so have no row: the public result and `conditions-changed` of a target that keeps its "?" (4a, 4f; R71.22); the Sniper lines' raw ids, the doubled hex and citation, and the sides in lower case (4a, 4d); the setup list's "a Dummy" for a Sniper counter and the hand placement's refusal (4c); the wound line's raw id (4f); the carry's summary by raw id and `#move-ipc`'s options (4g).

## What was built, by task

- **4a, 71.1 part I: the Sniper's targets and results** (design section 8; A14.21, A14.22, A14.3, A14.31, A14.33, D5.34, D5.341, D5.342). Vulnerable PRC and crews, unarmored vehicles, and the enemy Sniper as targets; Stun, Recall, a broken or pinned Inherent crew, Immobilization; SMOKE in the TEM comparison; A12.14's LOS clause; `Snipe` on the index, one replay an attack.
- **4b, 71.1 part II: the SAN as state, and Sniper Check** (design section 9; A14.1, A14.31, A14.4, E1.76). The events `san-reduced` and `sniper-checked`; the choice kind `sniper-check`; the actual SAN in Rules with E1.76's floor.
- **4c, 71.1 part III: the counter at setup** (design section 10; A14.01, A14.2). The setup's `sniper` step, the DEFENDER first, by A14.2's tiers from the placing side's view; a closed game told it has no counter.
- **4d, 71.1 part IV: the Sniper player's choices** (design section 11; A14.2, A14.21, A12.14). The choice kind `sniper`: forfeit and reposition, the equidistant hex, the Location, the prior target, the first of tied targets; A12.14's exception; a movement plan waits on the choice.
- **4e, 71.1 part V: every A14.1 trigger** (design section 12; A14.1, A14.3). One reader of fire, ordnance, morale check, Bail Out, PAATC, and OVR NTC records, once per planned step; each roll's maker in Rules; a Sniper's own LLMC chaining.
- **4f, 71.2 part I** (design section 13; A17.2, A17.11, D5.343, A18.2). The pin after more than 3 MF (`woundPins`); the SMC crew's Stun wound with its Severity dr; A18.2's -1 against an AFV (`afvDrm`).
- **4g, 71.2 part II: carrying a wounded man** (design section 14; A17.2, A4.4). `carried` on the move, 5 PP, no MF of his own, the set-down.

## The reviews

Five read-only agents in one message on 2dd2789d: three referees (the Sniper's rules; hidden information, read from the side that must not see; wounds and Field Promotions), a table player who read the events of every `p71-*` game, and a UI, Rules boundary, and memory review. Their briefs are `brief_reviews.md` in `code-builders/pass-history/pass-71-snipers-wounds`; each finding was judged by the session against its page and the code, and the findings as reported are `fixes_r1_sniper.md`, `fixes_r2_hidden.md`, `fixes_r3_wounds.md`, `fixes_r4_table.md`, and `fixes_r5_ui_boundary.md` in the same folder. Four agents fixed them in four rounds, one commit each (design section 15.2).

**No BLOCKER.** **Seven MAJOR findings, all fixed:**

- **A pending Sniper Check told the Sniper side that a real Good Order unit sits under a "?"** (hidden information 1; with the Sniper referee's 3 and the UI review's 11): the check is offered whenever a "?" of the attacked side stays there, and a "take" with none eligible does nothing (ruling R71.18).
- **The forfeit's reposition hexes revealed the Sniper side's own hidden units** (hidden information 2): the choice is the Sniper side's alone (R71.19).
- **The PRC of a concealed vehicle were treated as unconcealed targets** (hidden information 3): a concealed vehicle with its PRC and crew is one "?" candidate, decided in Rules (R71.20).
- **A Sniper attack stripped a "?" when the only enemy seer was itself concealed** (hidden information 4): only an unconcealed Good Order enemy seer costs it; with only concealed seers the "?" stays, a deviation in the target's favor (R71.21).
- **The public result said what a kept "?" is** (hidden information 5): one neutral public result, `concealed-kept`, and an owner-only twin (R71.22).
- **A broken SMC was pinned by a wound, which then barred his rout** (wounds 1): a broken SMC is never pinned by a wound (A7.8; R71.27).
- **Two Sniper pauses in one commit could share a choice key, and the projector refused the second** (UI and boundary 1): the trigger roll id is in both keys.

**Fixed, the other findings:**

- **Hidden information** (hidden information 6, 11; table 7): the Sniper Check's dice with the record's visibility; a kept target's conditions and a crew's Severity dr owner-only; the owner told what happened, the other side one sentence.
- **The Sniper's and the wound's rules** (Sniper 1, 2, 4, 5, 7; wounds 2, 3, 5, 7; UI and boundary 12, 13): a Sniper's own LLMC attacks before its check (R71.25); a SMC Temporary crew of a vehicle not fully armored attacked as a SMC (R71.26); Random Selection inside a concealed stack honoring ties; the lowest-TEM Location carried out of the tie-break and SMOKE per Location; a pinned Sniper's record refused; a recorded `woundPins` checked; the projector running the set-down bar; "wounded and pinned" said; the carried leader's bonus tested (R71.28); the forfeit's placement read from the state of the moment; in-commit replays kept out of the planner's cache.
- **The boundary** (UI and boundary 5 to 10; Sniper 6; hidden information 12; wounds 4; table 10): the A14.2 DEFENDER, "has fired or moved", A17.3's wounded leadership, "impaired" and "manned", the Search's MF for the wound pin, one SMC test, the AFV test, and the two-dice test into Rules, with no behavior changed.
- **The page and the texts** (table 2 to 6, 8, 9, 12 to 15; UI 2, 3, 4, 15, 16; the notes' known items): no raw ids in the Sniper lines; one citation; the hex once; the Sniper Check's takers and DRM with causes (`drmCauses`); the night SAN's floor said; where the Random Location landed; a wound's Severity dr and the pin's reason; a SMC crew's Recall said to eliminate him; "no attack" for an ineffective dr; the options hex first, the vehicle told from its crew; rulings cited as rulings; disabled controls with titles; the narrow layout's CSS; the setup's hex cleared after a commit; the carry lists cached.

**Not fixed: a row each in the backlog's section 74.2:**

| Review | What |
|---|---|
| Referee, hidden information (ruling R71.21) | A12.14's concealed viewer forcing the loss of "?" inside a Sniper attack (finding 4, its rest) |
| Table player | Each Random Selection die's target named in the record (finding 11) |
| Referee, wounds | A PRC SMC's wound pin (finding 9) |
| Fix Round C | The Fire path's PRC exposure counts only a Stunned or Shocked crew as impaired; the Sniper path a Recalled one too |
| The build (4e) | A trigger lost across a pause in the same step (Spray Fire's first Location); a unit created in the same step that takes a check calls no Sniper |
| The build and Rounds C and D | The hook's gap at the repository root |
| Fix Round B | The strict test build and the gate's strict stage disagreeing on CA1860 |

Accepted as judged, with no row: the projector trusting the recorded Sniper Check DRM (Sniper 7, its rest); a new record lacking the crew's Severity dr replaying with the older outcome (wounds 6); a carrier unable to take a second man (wounds 10); the wrapper's cost with no Sniper (UI 14); the proof games recorded before 4d, which show the older flow and replay to their last event (table 1).

## The games

In the session's Studio on port 6670, board 3, scripted dice; each played game's file read back. The pass's 14 games are `OWN` in `pass71.conf`; the measurement copy `p70-perf-tw` is not.

- **4a** (design 8.3): `p71-sniper-ce-tank`, `p71-sniper-truck`, `p71-sniper-vs-sniper` (played).
- **4b** (design 9.3): `p71-sniper-check-eliminates` (played); `p71-sniper-check-pins`.
- **4c** (design 10.3): `p71-sniper-by-hand` (played).
- **4d** (design 11.3): `p71-sniper-equidistant` (played); `p71-sniper-location`, `p71-sniper-tied`.
- **4e** (design 12.3): `p71-sniper-to-hit` (played); `p71-sniper-residual`.
- **4f** (design 13.3): its two games did not replay (a leader placed off board 3) and were moved out of the live folder; the wound pin is `p71-check-wound-pin` (played at the whole-pass check).
- **4g** (design 14.3): `p71-carry-wounded` (played).
- **The whole-pass check:** `p71-check-wound-pin`, `p71-check-kept-concealed` (played).

At the whole-pass check `replay_p71.sh` replayed the 12 games then recorded from copies outside the live folder: every one to its last event, with no ERROR, UNREADABLE, or NOSTATE line, those recorded before the fix rounds included.

## The tests and the build

Each increment's and each fix round's new Rules, Play, and page tests were run by method only, the user's rule of 2026-10-23, with 4c's card-start batches the one departure; the counts are in the design's sections 8 to 15 and the notes. The pass's own classes are `ScenarioA1Pass71RulesTests` (48 methods), `BacklogPass71Tests` (58), and `PlayPagePass71Tests` (12). The Rules, Play, Units, and MapStudio projects were not run whole: that is the gate's. The strict stage passed on 2dd2789d and again on the tree after Round D; each building agent's and each round's build of MapStudio ended with exit 0, and since 10:31 each agent's strict builds of the test projects it changed exit 0 before it reported.

## The replay digest

Not run on the older games after the tasks; the gate's proofs make it. The store holds no Sniper (design section 4), so the Sniper's new targets, results, choices, and triggers reach new plans only. Every effect that changes what a record made before the pass does is gated by a fact on the record or by a new record type: `woundPins` on a fire target, `afvDrm` on vehicle CC facts, a `wound-severity` roll before a SMC crew's Stun, `carried` on a move, and the new events and choice kinds; a game whose setup closed before the pass is given no Sniper counter (R71.11). Play tests that replay an older record or hold to it: `AGameWhoseSetupClosedBeforeThePassIsGivenNoSniperCounterAndSaysSo`, `ARecordedWoundPinTheStateDoesNotGiveIsRefused`, `AStepRecordedForACarriedManWithoutHisCarrierIsRefused`, and pass 70's `AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader` (it clears `AfvDrm`).

## What is open to a veto

- **The rulings R71.1 to R71.28,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-26. Those most likely to be read again: R71.1 (the pass kept whole), R71.4 (an empty unarmored vehicle never eligible, a deviation), R71.10 and R71.18 (the whole eligible set takes the check, and the offer whenever a "?" stays), R71.12 (the Sniper player's choices replacing R15.5), R71.13 (ordnance triggers, superseding R55.30), R71.21 (the deviation in the target's favor), R71.24 (the carried man and the trigger's roll id on public records), and R71.28 (Double Time refused to a stack carrying a wounded man, and the set-down).
- **The departures** listed above, and the passes this document gives the rows the pass did not finish (the inventory has each row's own pass).

## What the gate will meet

- **New facts in a record** (design section 16): `woundPins` and `afvDrm` sit on records kept as JSON of kinds that existed before; `pass71.conf`'s `SKIP_JSON` is empty as committed, so each is to be checked against the store's older records and named there before the proofs. The properties of event records (`carried`, `drmCauses`, `place`) and the state's `SanReductions` and derived `FiredOrMovedThisPlayerTurn` are for the computed `PROOF_SKIP`; the new events (`san-reduced`, `sniper-checked`) and choice kinds are the pass's own, and main writes UNREADABLE for a game that holds one.
- **Intended differences** (design section 17): a card game whose setup is open waits for each side's Sniper counter; new plans in which a DR of a kind A14.1 names equals an enemy SAN with a counter on the map; record lines with no raw ids; the text list's new texts.
- **The store** holds none of the pass's games until the gate copies the 14 named in `OWN`.
- **The swept game stays `dl-tractor-works-02`** (decided by the session after the user's "you decide"): neither it nor `-03` holds a `game-ended` event, `-03` reaches only Turn 5 against `-02`'s Turn 6, and neither holds a Sniper, so `-03` would cost a new baseline and test nothing of this pass; it is revisited at the next pass's cut, or when the user says it is finished.

## The Studio check

At 16:29 to 16:42 on 2026-10-26, in my Studio on port 6670, on the build of efdc4dae, its scope decided under the user's word (design 15.4):

- **Every pass game replayed** to its last event (12 games).
- **The wound pin** owed by 4f, in `p71-check-wound-pin`: "8-1 leader in [D10]: wounded, and pinned: wounded after more than 3 MF (A17.2)", the file holding `woundPins` true.
- **The Russian view of a kept "?"** in `p71-check-kept-concealed`: one neutral line and the public Sniper Check wait; the German view the unit, its pin, and the offer that no unit there may take the check.
- **A real Enter key** proposed the choice's "decline" and committed it.
- **1024 and 320:** the choice's buttons inside the page, wrapping at 320.
- **The hex audit,** both views: no counter choice without a hex.
- **The measurement,** back to back with main: hand-over median about 366 ms against 379, the proposal level (60 to 65 against 66 to 90 after the first).
- **Not done:** the SMC crew's Stun game (its Severity dr is shown by the page test `TheWoundLineOfASmcCrewsStunNamesHim` and Round D's record line); and, as the scope allowed, the five widths in each side's view for every control, a whole Player Turn at 320, and the Replay page.
