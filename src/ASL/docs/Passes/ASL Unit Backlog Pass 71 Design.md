# ASL Unit Backlog Pass 71 Design

**Status:** The frame, written on 2026-10-26 under the user's word of that day ("Run Pass 71 without my intervention up to the merge gate, as passes 55 to 70 did: questions go to a sub-agent, decisions become rulings open to my veto, and the gate, push, and merge stay mine to start."). The frame's questions were put to four read-only agents, as the standing rules ask in this mode. Their answers were judged against the pages and the code. Each decision below is a ruling decided under that word, and each is open to the user's veto.

**Date:** 2026-10-26

**Related documents:**
- the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the entry "Pass 71: Infantry IV: Snipers and wounds";
- the [rule inventory](<../ASL Rule Inventory A to E.md>) (11 rows give pass 71);
- the [backlog](<../ASL Unit Backlog.md>), sections 25, 26, 55, and 72.1;
- the [pass 70 design](<ASL Unit Backlog Pass 70 Design.md>), sections 2 to 7;
- the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>);
- the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>);
- the session's prompt, `Prompts/Pass 71 Prompt I.md`.

**The rulebook.** I read on their pages: A14 in full with A14.4's EX (pp. 82 and 83), A17 and A18 (p. 85), A12.14's loss list and its Sniper exception (pp. 77 and 78), D5.34 to D5.343 (pp. 203 and 204), and E1.76 (p. 225). The agents read pp. 48, 65, 77, 78, 82, 83, 85, 109, 193, 194, 203, 204, 225, and 253, and searched every page for "Sniper", "SAN", "1.76", "wounded", "4 MF", and "G.M.D.". Each increment reads its own pages again before it builds.

**What the frame rests on.** The four agents' reports are kept in `code-builders/pass-history/pass-71-snipers-wounds/`: `frame_triggers.md` (A: which DRs call a Sniper), `frame_state.md` (B: the counter, placement, the SAN, the Sniper player's choices, Sniper Check), `frame_targets.md` (C: the targets and results), and `frame_wounds_codemap.md` (D: 71.2 and the code map). The session's running record is `notes.md` in the same folder.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The rows, by task and increment.
4. What is certain, and what needs a read.
5. What the frame found outside the rows, and corrections.
6. The frame's questions, each with its ruling.
7. Memory and performance: the checklist and the measurement.

Then, as each is built: one section per increment, then the reviews, the Studio check, as built, and the gate.

## 2. What the pass is

Pass 71 completes the Sniper (A14) and the wound's movement rules (A17.2), with Field Promotions' remainder (A18.2):
- **the Sniper as the rule has it:** the right targets and their results, vehicles and crews and the enemy Sniper among them; a SAN that changes; Sniper Check; the counter given at setup and placed by A14.2; the Sniper player's own choices in place of the game's (R15.5); every DR A14.1 names as a trigger;
- **wounds:** the pin of a man wounded after more than 3 MF, a wounded man carried for 5 PP, and the SMC crew's Stun wound with its Severity dr;
- **Field Promotions:** the -1 drm for CC against an AFV.

**What departs from the plan's entry, first:**

- **The forecast: about 8 hours to the gate, of which about 5:40 is building, against the plan's 1:20 of building (2:35 in all).** The estimates below are calibrated: the frame agents' raw estimates divided by 4.5, between pass 70's ratio (its frame estimated 33:05, the build took 8:07, 4.1) and 4a's (estimated 5:45, built in 1:01, 5.7); 4a's row is its time by the clock. The rest of the forecast is the reviews, their fixes, the Studio check of the whole pass, and the documents, about 2:30 (pass 70's took 3:22 for ten increments; the plan's 1:15 has been short in every pass since 55). The pass stays one pass (ruling R71.1), built in the order of the table below; the last increment (carrying a wounded man) is the one a session's end would leave out, to pass 72.
- **Moved out of the pass** (ruling R71.2), each with a backlog row in the documents step:
  - to pass 115 (Fortifications I): Entrenching as a Sniper trigger (Entrenching is not built), and the Fortification TEM and the reveal of hidden Fortifications in A14.21's comparison;
  - to pass 90 (Night and winter, task 90.6): the captured MG's automatic Sniper dr at night (E1.76, backlog 26);
  - to pass 135 (buildings): Interior Building Locations as non-targets (A14.22; Play knows no interior hex);
  - to pass 145 and Chapter G: subterranean Locations;
  - to passes 165 and 175, and the water pass: the 4 MF of a wounded man on cavalry, motorcycles, wagons, and boats (A17.2; vehicles are built);
  - to the backlog with no pass: a Sniper Check by a subset of the eligible units; the wound pin of a routing SMC in the RtPh; the Sniper player's option to treat an empty unarmored vehicle as eligible; a 1MT AFV's Stun as Recall (no catalog vehicle); an Armor Leader's elimination by Recall and its LLMC; G.M.D. leaders (no Chinese counters); the early-DR order of one step (section 4).
- **The ordnance triggers are kept in,** against agent A's proposal to move them to pass 110 (R71.13): the generic reader of R71.13 reads `OrdnanceFired.Rolls` as it reads fire records, so ordnance needs no hook of its own, and R15.5 already named the To Hit DR a trigger.

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 4a | 71.1 (I) | The Sniper's targets and results: the eligibility fix, Vulnerable PRC and crews, unarmored vehicles, the enemy Sniper as a target, Stun and Recall (D5.34), SMOKE in the TEM comparison, A12.14's LOS clause, the record's leak, the reads through the index, the record lines' hexes. | 1:01 (built) |
| 4b | 71.1 (II) | The SAN as state (`san-reduced`, E1.76's floor) and Sniper Check (A14.4). | 1:00 |
| 4c | 71.1 (III) | The counter given at setup and placed by A14.2. | 0:55 |
| 4d | 71.1 (IV) | The Sniper player's choices: forfeit and reposition, the equidistant hex, the Location, the prior target, the first tied target; A12.14's exception. | 1:15 |
| 4e | 71.1 (V) | Every A14.1 trigger: one reader for every record that carries a qualifying roll, called once per planned step; ordnance included; a Sniper's own checks chaining. | 0:20 |
| 4f | 71.2 (I) | The pin after more than 3 MF; the SMC crew's Stun wound and its Severity dr; A18.2's -1 against an AFV. | 0:35 |
| 4g | 71.2 (II) | Carrying a wounded man for 5 PP. | 0:35 |
| | Overhead | The reviews, their fixes, the Studio check of the whole pass, the documents (the plan's 1:15; pass 70's took 3:22). | 2:30 |
| | **Forecast** | build 5:41 | **about 8:10** |

(The frame agents' raw estimates, before calibration: 4a 5:45, 4b 4:30, 4c 4:00, 4d 5:30, 4e 1:35, 4f 2:45, 4g 2:30, a build of 26:35. They are kept for the calibration only; pass 70's ran about four times its build.)

## 3. The rows, by task and increment

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 71.1 | A14.01 (82) | The counter given to a side with a SAN of 2 or more | 4c |
| 71.1 | A14.1 (82) | Every trigger DR; a SAN that changes | 4e (triggers), 4b (the SAN) |
| 71.1 | A14.2 (82) | Setup placement, forfeit and reposition, the Sniper player's picks | 4c (placement), 4d (choices) |
| 71.1 | A14.21 (82) | The equidistant hex by the Sniper player; SMOKE in the TEM; the enemy Sniper counts | 4a (TEM, the enemy Sniper), 4d (the pick) |
| 71.1 | A14.22 (82) | Vulnerable PRC and crews, unarmored vehicles; Interior Building Locations | 4a; Interior Buildings to pass 135 |
| 71.1 | A14.3 (82) | Results against crews, vehicles, and the Sniper; LLMC after a hit | 4a (LLMC built in pass 70: documents) |
| 71.1 | A14.31 (82) | The Sniper as a target; a pinned Sniper | 4a |
| 71.1 | A14.33 (82) | An unarmored vehicle immobilized on a 1 | 4a |
| 71.1 | A14.4 (83) | Sniper Check | 4b |
| 71.2 | A17.2 (85) | The pin after more than 3 MF; carried for 5 PP; 4 MF on a conveyance | 4f, 4g; the conveyance built (documents) |
| 71.2 | A18.2 (85) | The -1 for CC against an AFV; G.M.D. | 4f; G.M.D. to the backlog |

The items moved in from passes 65 and 70 are placed with their task: Stun of an AFV by a Sniper (D5.34) with 4a; A12.14's Sniper exception to voluntary removal with 4d; the SMC crew's Stun wound (D5.34, A17.11) with 4f.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **The trigger reads fire records only** (frame A, section 1): `AddSniperAttacks` (GamePlanner.Snipers.cs:20) is called from three Fire paths and the choice resume, reads `FireResolved.Rolls` alone, and skips two A14.1 kinds inside them (`collateralCheck`, `taskCheck`). Residual FP on a move, a Fire Lane, Residual FP on a vehicle, the vehicle OVR's IFT, every ordnance To Hit and HE IFT, the morale checks of pass 70's member, the Bail Out NMC, PAATC in the MPh, and both OVR NTCs are rolled with a recorded roll in a trigger phase and never read.
- **The wrong units are eligible today** (frame C, section 1): `ScenarioA1Sniper.Eligible` reads `state.Location`, which follows containment, so every Passenger, Rider, and crew inside any vehicle, a BU tank's included, is a Sniper target, against A14.22.
- **The SAN is the side's printed one,** on `SideState.San` from `game-started`; nothing changes it, and the counter comes only from a hand placement at setup (frame B, section 1).
- **A pinned Sniper stays pinned for good:** the projector clears CCPh pins on units and equipment, not entities (GameProjector.cs:670-672).
- **A Sniper attack can pause inside a commit:** `AddSniperAttacks` stops when the commit holds a `ChoicePending`, and Fire's resume calls it after the answer (Choices.cs:216-227).
- **A12.14 (p. 77) costs a "?" for a Sniper attack only in the LOS of a Good Order enemy ground unit,** while a failed MC, a break, a wound, or a Reduction always costs it. Today a pin always strips it and an effective dr with no effect never does.
- **LLMC after a Sniper hit is built** (pass 70, 3b, `ScenarioA1MoraleCheck.Sniper`); the backlog and inventory rows that ask for it are stale.
- **4 MF on a conveyance is built** for vehicles (`ConveyanceAllowance`, pass 40 and R65.15).
- **The SMC crew's Stun is a wound with no Severity dr** in the projector (`SmcCrewAfter`), and D5.343 with A17.11 asks for the dr.
- **A18.2's "-1 CC vs AFV" is not applied** in vehicle CC's Leader Creation (`ScenarioA1VehicleCloseCombat.cs:393`).
- **The proof store holds no Sniper:** none of its 43 whole games has an `asl:sniper` entity or a `sniper-attacked` event (frame A's scan). Every change of the pass to the Sniper reaches only new plans with a counter on the map.

**Needs a read at its increment.**

- 4a: the Partially Armored trait (`asl:partially-armored`, about nine catalog vehicles) as a definition property; how a broken Inherent crew is placed (`UnprotectedCrewEvents`); `ScenarioA1VehicleSightRules.SmokeDrm`.
- 4b: how the projector marks TI, and what in the state says a unit has fired or moved this Player Turn (frame B found no moved set for Infantry).
- 4c: the card setup's gate (`CardSetupIncomplete`) and the non-OB "?" step it is modelled on.
- 4d: the five pause points' resume facts; `MovementPlans.Review` on any pending choice.
- 4e: the central plan wrapper (GamePlanner.cs near line 410, as `WithImmediateVictory`) and the ordnance records' key grammar.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and the absent-safety of each):
- new `sniper-attacked` result strings (stunned, recalled, crew-broken, crew-pinned, immobilized, sniper-eliminated, sniper-pinned); a null unit where a concealed unit keeps its "?" (4a);
- new event `san-reduced` (4b); new choice kinds `sniper` and `sniper-check` (4b, 4d); a `sniper` argument on `asl.game.setup` (4c);
- on a fire target: a wound-pin fact (4f); on vehicle CC: `afvDrm` for Leader Creation (4f); a `wound-severity` roll before a SMC crew's Stun (4f); a `carried` argument on `asl.game.move` and its step record (4g).
Each to be checked against the store's older records before it enters `SKIP_JSON` (`skip_json_collisions.py`, pass 70's gate).

**The early-DR order.** One existing deviation is left: a trigger by an early DR of one step resolves after the step's later DRs (a DC thrower's attack, Spray Fire's second Location), not between them. To the backlog.

## 5. What the frame found outside the rows, and corrections

1. **The eligibility of units inside vehicles** (section 4): repaired in 4a. Only new plans change; the projector does not re-check eligibility.
2. **A leak:** `SnipeUnit`'s no-effect branch names the unit of a concealed MMC that keeps its "?" (Snipers.cs:290-292). Repaired in 4a.
3. **The Sniper record lines break the hex UI rule:** "at {BoardLocation}: {unit}" with a raw Location and no hex (PlayRecords.cs:1629-1637). Repaired in 4a.
4. **`Snipe` replays the game up to four times an attack and scans every unit's Location** (Snipers.cs:23, :78, :209, :219, :123-142). Repaired in 4a through the index.
5. **Stale rows,** corrected in the documents step: A14.3's "no LLMC or LLTC follows" (inventory) and backlog 25's row of the same; A17.2's "4 MF on a conveyance" (built); backlog 55's Stun row naming "Passes 65 and 105".
6. **Corrections to the prompt:** none of its state. Its proposal for the triggers is answered by R71.13 (the item does not double the pass once one reader serves every record).

## 6. The frame's questions, each with its ruling

**F1. Is the pass kept whole?** The forecast is about 8 hours to the gate, about 5:40 of it building, against the plan's 1:20 of building (section 2: the frame agents' estimates divided by 4.5, between pass 70's ratio of 4.1 and 4a's of 5.7).
- **R71.1:** the pass stays one pass, as R70.1 kept pass 70. The increments run in the table's order; whatever a session's end leaves out is the last increment's (4g, carrying a wounded man), and it goes to pass 72 with its row.

**F2. What leaves the pass?**
- **R71.2:** the moves of section 2, each with a backlog row in the documents step.

**F3. Eligibility and the new targets (A14.22, A14.33).**
- **R71.3:** a unit inside a vehicle is a Sniper target only if it is Vulnerable: a PRC whose vehicle is CE (for a Carrier, its Passengers exposed), Partially Armored, or unarmored, and any Rider; the Inherent crew when the vehicle is CE, Partially Armored, or unarmored. An AFV with no Vulnerable PRC and no Vulnerable crew is no target. An unarmored vehicle is itself a target, and the vehicle and its PRC are never attacked by the same dr (A14.33). The decision is one Rules member on facts Play reads from the index; the Partially Armored trait becomes a definition property. Only new plans change.
- **R71.4:** an unarmored vehicle with no PRC is ineligible (A14.22 lets the Sniper player so treat it; a deviation where he would rather attack it, with a backlog row): an empty truck rarely beats the Alternate Target search, and offering the option at every such hex would pause for little.

**F4. Results (A14.3, D5.34, A14.33).**
- **R71.5:** dr 1 Recalls the CE crew of a fully armored AFV (D5.341, p. 204), breaks the Inherent crew of an unarmored vehicle or Partially Armored AFV, and immobilizes an unarmored vehicle (no effect if already immobilized); dr 2 Stuns the CE crew of a fully armored AFV (D5.34, p. 203; a crew already under Stun +1 is Recalled instead, D5.342), and pins such an Inherent crew. Stun, Recall, and Immobilization are written as the vehicle's `conditions-changed` the Fire path already writes (`VehicleConditions`), so the projector's Stun, Recall, and BU handling apply. The broken Inherent crew is placed broken in the vehicle and routs beneath it as pass 65's `UnprotectedCrewEvents` does: D5.311's "[EXC: Sniper attack]" keeps a Sniper from making a crew Vulnerable through an unarmored facing, and A14.3 itself breaks this crew; the routine is the nearest built treatment.

**F5. The enemy Sniper as a target (A14.31).**
- **R71.6:** the enemy Sniper counter counts in the Alternate Target search ("neither an eligible target nor the enemy Sniper counter"), is part of every Location of its hex, and is a candidate of Random Selection; dr 1 eliminates it (with R71.9's SAN reduction), dr 2 pins it. The projector clears a Sniper's pin with the other pins of the CCPh's end; a pinned Sniper makes no attack and no repositioning until then (A14.31). Absent-safe: nothing pins a Sniper today.

**F6. A14.21's TEM comparison.**
- **R71.7:** the in-hex SMOKE DRM (grenades and burning wrecks, the existing `SmokeDrm`) is added to each eligible target's TEM before the lowest is taken; the HA and Factory TEM are not in the table, so their NA already holds. Fortifications go to pass 115.

**F7. A12.14 and a Sniper attack on a concealed unit.**
- **R71.8:** an effective Sniper attack (dr 1 or 2) on a concealed unit costs its "?" when it is in the LOS of a Good Order enemy ground unit, read through pass 70's sight table (`ScenarioA1Sight`, the Loss purpose), whatever the result; a break, a wound, a Reduction, or an elimination costs it always (A12.14, p. 77). A record of an attack that leaves a unit concealed names no unit (section 5, item 2).

**F8. The SAN as state.**
- **R71.9:** a new event `san-reduced` (side, cause) and a count of reductions on the side's state, the printed SAN kept as recorded. The actual SAN is decided in Rules: by day the printed SAN less the reductions; at night the printed plus two, at most 7, less the reductions, never below the printed (E1.76, p. 225). When it falls below 2 the same commit eliminates the side's Sniper counter, and no more attacks follow (A14.1, A14.4). A Sniper eliminated by a Sniper Check or by an enemy Sniper's dr 1 writes it. Older records hold none and replay as they were.

**F9. Sniper Check (A14.4, p. 83).**
- **R71.10:** after an effective Sniper attack, if the target Location holds units of the attacked side that may take it (unpinned, not TI, armed, Good Order Infantry, not having fired or moved this Player Turn), that side gets a pending choice, kind `sniper-check`, to take it or not; taking it commits every eligible unit, so the one combined DR has the most benefit of its DRM (A14.4: "so as to maximize benefit"). The DR is made at the answer: a Final DR of 2 or less eliminates the Sniper (R71.9), 3 pins it; every participant becomes TI; the remaining triggers then resolve. The DRM: one leader's leadership when he directs another unit, none when he takes it alone (the EX); -1 each hero; -1 each crew or HS equivalent (a squad -2). A Sniper Check costs a "?" by A12.141 (p. 78, which names it). Its DR calls no Sniper (A14.1's list). A subset to the backlog. What "has fired or moved this Player Turn" reads is decided at 4b from the state's markers.

**F10. The counter at setup (A14.01, A14.2).**
- **R71.11:** a card game whose side has a SAN of 2 or more is not started until that side's Sniper counter is placed, by a `sniper` argument of `asl.game.setup` after both sides have set up, the DEFENDER first. A placement is legal by A14.2: an unoccupied hex within six hexes of the most enemy-occupied hexes there can be, at most six, counted from the placing side's view (enemy "?" counted, hidden units and the enemy Sniper not). A game made without a card places the counter by hand as today, under the same check. A game whose setup is already closed is never given a counter: its replay stays as it was, and the page says once that the side has no Sniper counter (A14.01).

**F11. The Sniper player's choices (A14.2, A14.21; R15.5 replaced).**
- **R71.12:** the Sniper player decides where the rule gives him a choice, by a pending choice of kind `sniper` for his side, made only where more than one option exists: (1) on an effective dr of an unpinned Sniper, to attack or to forfeit and reposition (then the legal hexes); (2) the equidistant hexes left after the lowest TEM; (3) the Location in a hex of several; (4) before Random Selection, the enemy Sniper, a Vulnerable Inherent Crew, or an unarmored vehicle present, or Random Selection; (5) which of several targets Random Selection gives takes the first dr. The attack resumes on the answer from the rolls already recorded, and the triggers still to resolve follow in DR order. The choice and its options are public (the dr is public); options are worded from the Sniper side's view. While it waits nothing else may happen, which is A12.14's exception (no voluntary removal of a "?" between the Random Location DR and the relocation). R15.5's game picks remain only for an older record's replay, which reads the recorded events.

**F12. The triggers (A14.1).**
- **R71.13:** one reader in Play finds every qualifying Original DR of a planned step: the fire records (with `collateralCheck` and `taskCheck`, made by the target vehicle's side), the ordnance records (To Hit, the HE hit's IFT and checks, crew checks), `morale-check-resolved`, the Bail Out, PAATC, and both OVR NTCs; Rules names each roll's maker by kind (`RollMaker`). It runs once per planned step through a central wrapper in the planner, in the four phases, in DR order, after the step's own effects; Fire's existing calls stay and the wrapper does not run it twice. This supersedes R55.30's "ordnance calls no Sniper", which recorded the code rather than the rule; R15.5 already named the To Hit DR a trigger. A Sniper attack's own LLMC DRs are triggers too and chain, each resolved after the attacks before it (A14.1, A14.3). A DR that can yield no result but a SAN is not newly guarded: none of the new triggers is such a DR (backlog row if one is found).

**F13. Wounds (A17.2) and the SMC crew (D5.343, A17.11).**
- **R71.14:** a SMC wounded in the MPh after spending more than 3 MF (3 1/2 counts) is pinned, decided in Rules: in Fire through a new optional fact on the target (absent, as before), and by the planner with the wound in a Sniper attack, a Search, or Mopping Up. The routing SMC's wound pin in the RtPh goes to the backlog.
- **R71.15:** a SMC Temporary crew Stunned is wounded with a Wound Severity dr (+1 if already wounded; a 5 or 6 eliminates), rolled by the planner before the vehicle's change and read by the projector; a record with no such dr keeps today's result. A Recall eliminates him as today.
- **R71.16:** a wounded man (Good Order, possessing no SW) may be carried by a Good Order MMC at 5 PP, as a `carried` argument of the move recorded on its step (absent, as before); he moves with his carrier and spends no MF of his own (4g).

**F14. Field Promotions (A18.2).**
- **R71.17:** "-1: CC vs AFV or per odds column < 1-1" is two causes: the -1 against an AFV is added to Leader Creation in vehicle CC when the vehicle is armored, through a new optional fact (absent, as before); the per-column drm in Infantry CC is built. G.M.D. (Chinese) leaders stay refused under R27.1 until a card brings Chinese counters.

## 7. Memory and performance

**The checklist** (the guidelines' section "Design and review checklist").

- **Work added per state:** the Sniper counters, the actual SANs, the eligible targets grouped by Location, and the Sniper Check's eligible units, each read once per state through `MemoOf(state)` and `PlayStepMemo`'s `ObjectIndex` (one pass over the units); the setup placement's legal hexes once per (state, side) at setup only (about 330 hexes by the enemy-occupied hexes, distances only, no LOS).
- **Per unit and per hex:** none outside the memo. `Snipe`'s four replays and per-unit `state.Location` reads are replaced by the index (section 5, item 4).
- **Per step:** the trigger reader walks the step's own events once; a step with no enemy Sniper counter on the map stops at once.
- **Per render:** the pending choice's panel is pass 70's; no new layer.
- **Ownership and keys:** the memo stores are owned by `PlayStepMemo` (state lifetime), keyed by side; read only; concurrent fills may compute equal answers twice with no side effect. Disclosure: the placement set is keyed by the placing side's view.

**The measurement** (stated before building, as the user asked on 2026-10-19).
- **Workload:** pass 70's `p70-perf-tw` (`dl-tractor-works-02` cut at revision 1316; the user's game untouched), copied under a pass 71 id: the hand-over (German to Russian) and an "end the phase" proposal, five runs each; and one Sniper attack in a small game, the commit timed.
- **Baseline:** the gate's `main-bin` build of 5379e06f on port 6671, measured alone; then the pass's Studio on 6670, alone (the lesson of pass 65).
- **Expected:** the hand-over and the proposal level with the base (no Sniper on that map; the trigger reader stops at once); a Sniper attack's commit faster than the base's, since its four replays go.
- **Stop threshold:** a warm median more than 20% over the base's stops the next increment until a trace finds the cause.
- **When:** after 4a in the Studio first, before any costly test; again after 4e and at the whole-pass check.

**Build outputs outside the repository.** My Studio in `asl-gate-work/studio-6670` (registered by `build_studio.sh`); the gate's own (the base build, the proof tools, registered by `gate.sh`); the filtered test runs' `asl-gate-work/test-classes-bin/<project>` (registered by `run_test_classes.sh`); any strict build of a test project by an agent, registered as it is made. All are removed at the latest by the gate's `ci` stage; the last task confirms `outside_outputs.sh list` is empty.
