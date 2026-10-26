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

**F15. Decided at the reviews (2026-10-26, under the same word; the review files are in the pass's folder, `fixes_r1_sniper.md` to `fixes_r5_ui_boundary.md`).**
- **R71.18:** a Sniper Check is offered whenever the attacked Location still holds units of the attacked side under "?", whether or not any may take it, and whenever any may; a "take" with no eligible unit commits nothing and says so only in the owner's view. The offer is public and tells nothing of the stack (A14.4, A14.23, A12.141; the finding of R1, R2, and R5).
- **R71.19:** the forfeit's reposition choice and its hexes are the Sniper side's alone (its own hidden units shape the set, A12.3); the other side reads only that the Sniper forfeits and repositions, and then the counter's new hex, which is public (A14.2; R2's finding 2).
- **R71.20:** the PRC and crew of a concealed vehicle are part of its concealed stack (A12.2: "... so does its manning Infantry or PRC, and vice versa"): one "?" candidate for A14.21 and A14.23, counted whatever the vehicle's state, the number of targets declared only once it is chosen; a "?" lost by one is lost by all. Rules decides the membership (R2's finding 3).
- **R71.21:** a Sniper attack, and a Sniper Check, cost a "?" only when an unconcealed Good Order enemy ground unit sees it; where every enemy seer is itself concealed, the "?" stays. A12.14 (p. 78) lets such a viewer force the loss by forfeiting its own "?" momentarily; that option is not built inside a Sniper attack, a deviation in the target's favor with a backlog row (R2's finding 4; R70.21 built the option for movement).
- **R71.22:** when a target keeps its "?", the public record carries one neutral result ("concealed-kept") and an owner-only twin the real one; the attack's effect events on that target (its conditions, a crew's Severity dr) are the owner's, and the Sniper Check's dice take its record's visibility (A12.14, A14.23; R2's findings 5, 6, 11; R4's finding 7).
- **R71.23:** a Sniper attack's Random Selection inspects its Location: the Sniper side learns the unconcealed targets it chooses among, as A14.2's choices need them known, whatever A12.16 would withhold at a distance; a concealed stack stays one "?" (A14.23) (R2's finding 7).
- **R71.24:** the carried man is named in the public movement step as the movers already are (the boundary of the step's record, older than the pass), and the trigger's roll id on a public Sniper record is inherent to A14.1 (the attack is public, and revisions already count withheld events) (R2's findings 8, 9).
- **R71.25:** a Sniper attack's own LLMC and LLTC DRs that call further attacks are resolved before that attack's Sniper Check and before any later trigger of the step (A14.3, p. 82: an attack "is considered resolved only after the full resolution of all further Sniper attacks generated by ... all LLMC/LLTC"; A14.4: the check comes "After resolving (14.3)"). R71.13's "each resolved after the attacks before it" is read so (R1's finding 1).
- **R71.26:** a SMC Temporary crew of an unarmored vehicle or a Partially Armored AFV is attacked by a Sniper as a SMC: dr 1 eliminates him, dr 2 wounds him (A14.3: "Eliminates SMC ... breaks Inherent crew"; a Temporary crew, A21.22, is no Inherent crew); D5.343 keeps the fully armored case (R1's finding 2).
- **R71.27:** a broken SMC is never pinned by a wound (A7.8, p. 58: "Pinning affects broken units only during Interdiction"), so R71.14's pin needs him unbroken (R3's finding 1).
- **R71.28:** R71.16's readings, stated: a carried wounded leader still gives his carrier the two MF bonus for accompaniment (A17.2: "A wounded leader's two MF bonus for accompaniment throughout the MPh still applies"); a stack carrying a wounded man may not Double Time (stricter than the page, which bars the man only); the set-down that ends a carried man's move is this ruling's, not A17.2's (R3's findings 7, 8; R4's finding 9).

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

## 8. The Sniper's targets and results: 71.1, part I (increment 4a)

Built on 2026-10-26, 08:42 to about 09:23, in one commit (dbf22030) by a building agent to a brief (`brief_build.md` and `brief_4a.md` in the pass's folder of `code-builders/pass-history`), rulings R71.3 to R71.8 and section 5's items 2 to 4, its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-26; each is open to a veto.

- **R71.4's "no PRC" read with the Inherent crew among the PRC** (PRC are Passengers, Riders, and Crew): an unarmored vehicle is a target when it is manned or carries any PRC. Right.
- **The enemy Sniper's TEM is its own Location's:** Play lists no hex's Locations. Accepted.
- **A crew already broken is no crew candidate, and a crew or a Sniper already pinned takes no effect on a dr 2.** Accepted.
- **A concealed target that keeps its "?" was recorded with a null unit,** while the result string ("stunned", "pinned") stayed public and, for a concealed vehicle, told that the stack holds a vehicle. Given to the hidden-information referee; closed at fix Round A by R71.22 (section 15.2).
- **The A12.14 LOS read is `NearestGoodOrderEnemyInLos`,** at any range ("regardless of range") and not turned off at night. Given to the referee, who found it right at night (R2's finding 10); its concealed seers were narrowed at Round A (R71.21).
- **No catalog vehicle carries `asl:partially-armored`,** so the Partially Armored case is tested in Rules only.
- **4a's dr 1 against the enemy Sniper removed its counter always;** 4b corrected it to A14.4's reading (section 9.1).
- **Found by the session in the Studio, for the fixes:** the Sniper line named the counter and the trigger by raw ids ("(gs)", "the SAN roll fire-...-roll-1", "(rs)"), older than the pass, against the hex UI and no-raw-id rules. Fixed at Round D.
- **Found by 4b's agent, 4a's:** `SnipeUnit`'s pin of a concealed unit that keeps its "?" wrote a public `conditions-changed` naming the unit, though the record line named none. Fixed at Round A (R71.22).

### 8.2 What is built

Rules: `ScenarioA1Sniper.VehicleTargets` (A14.22, A14.33), `CrewResult`, `VehicleResult`, `SniperResult`, `VehicleConditions(result)`, `KeepsConcealment`, `PinnedConditions(keepsConcealment)`, `TargetTem(terrain, smokeSources)`, and the result and role constants; `FireDefinition.PartiallyArmored` read from the catalog's trait. Play: `Snipe` rewritten on the index (`SniperTarget`, `SniperTargets`, `SnipeVehicle`, `SnipeSniper`), one replay an attack (three when a leader is lost: the LLMC and the DM after it), where it had replayed up to four times and read every unit's Location; `VehicleConditionNames` split out of Fire. Units: the projector's `UnpinSnipers` at the CCPh's end. MapStudio: `PlayRecords.SniperResultText` and the Sniper lines hex first. `game_file.py` gained `passenger`, `san`, and `sniper`.

A unit inside a vehicle is a target only when Vulnerable (R71.3); the CE crew of a fully armored AFV is Recalled on a dr 1 and Stunned on a 2 (Recalled under Stun +1); the Inherent crew of an unarmored vehicle or Partially Armored AFV is broken or pinned and routs beneath as pass 65's `UnprotectedCrewEvents` places it; an unarmored vehicle is immobilized on a 1 (R71.5); the enemy Sniper counter is a candidate, eliminated on a 1 and pinned on a 2 until the CCPh ends (R71.6); SMOKE counts in the TEM comparison (R71.7); a concealed unit loses its "?" only in a Good Order enemy's LOS unless it breaks, is wounded, Reduced, or eliminated (R71.8).

**New in a record:** no JSON key, event, action, or choice kind; new result strings on `sniper-attacked` (`stunned`, `recalled`, `crew-broken`, `crew-pinned`, `immobilized`, `sniper-eliminated`, `sniper-pinned`), and a null unit where a target keeps its "?". **An older record** replays as recorded: the projector does not re-check eligibility, nothing pinned a Sniper before the pass, and `UnpinSnipers` touches Sniper counters only. One wording change: an older `sniper-attacked` with a target, a null unit, and a result other than none or dummy now reads "keeps its ?" (R4's absent-safety note).

### 8.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-sniper-ce-tank` | The Russian PFPh, dice 3 3 then the Sniper's: Sniper dr 2, "[Q8]: the crew of T-34 M41 R1 is Stunned (A14.3, D5.34)"; the file holds `sniper-attacked` (unit r-tk, result stunned) and `conditions-changed` r-tk Stunned, BU, Motion off. Played. |
| `p71-sniper-truck` | Sniper dr 1, Random Selection 1 1 6: "[Q8]: GAZ-MM R1 is immobilized (A14.33)". Played. |
| `p71-sniper-vs-sniper` | Sniper dr 2: "[Q8]: the enemy Sniper counter (rs) is pinned and makes no further attack this Player Turn (A14.31)"; the file holds the pin. Its removal at the CCPh's end not walked (the DFPh's end is the DEFENDER's control); asserted by `BacklogPass71Tests.TheEnemySniperIsPinnedUntilTheCloseCombatPhaseEnds`. Played. |

Rules: `AnAfvWithNoVulnerablePrcIsNoTargetAndACeOneOffersItsCrewAndItsExposedPassengers`, `APartiallyArmoredAfvsPrcAndCrewAreVulnerableEvenWhenBu`, `AnUnarmoredVehicleIsATargetApartFromItsPrcButNotWhenEmpty`, `WhoMayBeATargetAtAll`, `TheCatalogsVehiclesReadTheirPartiallyArmoredTrait`, `ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo`, `AnUnprotectedInherentCrewBreaksOnAOneAndIsPinnedOnATwo`, `AnUnarmoredVehicleIsImmobilizedOnlyOnAOneAndOnlyOnce`, `TheEnemySniperIsEliminatedOnAOnePinnedOnATwoAndCountsAsACandidateOfItsOwn`, `SmokeInATargetsHexCountsInTheTemComparison`, `AConcealedUnitAttackedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos`. Play: `ASnipersTwoStunsTheCeCrewOfATank`, `AButtonedUpHalftracksPassengersAreNoTargetsSoTheSniperFindsTheNextHex`, `ATrucksPassengerCrewAndTheTruckItselfAreThreeCandidatesAndAOneImmobilizesTheTruck`, `TheEnemySniperIsPinnedUntilTheCloseCombatPhaseEnds`, `ALeaderTheSniperEliminatesStillCausesTheLlmcOfHisLocation`, `AConcealedSquadPinnedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos`. Page: `TheSniperLineNamesTheHexAndTheStunnedCrewsTank`. By method: Rules 12 of 12; Play 15 of 15 and 1 of 1 (the new methods and the existing Sniper methods of passes 15, 16, and 55); MapStudio 3 of 3; build exit 0.

**The measurement after 4a was inconclusive** (section 16): the warm hand-over in the pass's Studio read about 585 ms against the base's 406 of 08:41, while 4a changes no code a hand-over runs; main's build in a fresh process read about 413, and the pass's Studio in a fresh process stopped taking the scripted clicks part of the way through each of three runs. The difference was read as the warm Studio's, not proven; the measurement was taken again after 4e.

## 9. The SAN as state, and Sniper Check: 71.1, part II (increment 4b)

Built on 2026-10-26, 09:43 to about 10:23, in one commit (379c77dc) by a building agent to a brief (`brief_4b.md`), rulings R71.9 and R71.10, its diff read and its games played in the Studio by the session.

### 9.1 Departures from the frame, first

- **A dr 1 against the enemy Sniper reduces its side's SAN and removes the counter only below 2** (A14.4, p. 83: an attack vs a Sniper "which eliminates a Sniper reduces that side's SAN by one ... If the SAN is reduced to 1, remove the Sniper Target Selection counter"). 4a had removed the counter always; R71.6 is read with R71.9.
- **A second new event, `sniper-checked`,** with its record line, the projector's check, and a `GameGate` case. Accepted.
- **A derived state fact, `GameState.FiredOrMovedThisPlayerTurn`** (the movers of each step, a vehicle's load or unload, each phase's firers, cleared at a new Player Turn), not recorded: frame B had found no moved set for Infantry. Accepted; for the gate's computed skip list.
- **Hidden units take no Sniper Check:** "take" would cost a hidden unit its status unasked. Accepted.
- **A heroic leader gives his leadership or the hero's -1, never both** (A15.21), and a wounded leader's leadership is one worse (A17.3). Accepted; A17.3 moved into Rules at Round C.
- **At night `san-reduced` is still written and `ActualSan` clamps at the printed SAN.** Accepted; the record line says so since Round D.
- **The planner's `ConditionName` gained `UnitCondition.Ti`;** it fell through to `CmgMalfunctioned` before. A fault older than the pass, fixed.
- **The choice is public and names no unit;** the `sniper-checked` record and the TI changes go to the owner only when a participant keeps its "?". Accepted; the offer's existence was found by three reviews to tell of a "?" stack, and R71.18 changed it at Round A.
- **The CA1861 slip:** two errors in `PlayPagePass71Tests.cs` (4a's and 4b's page tests) reached the user's Visual Studio before anything strict compiled the MapStudio test project; fixed by a static field, and `strict_test_build.sh` was made the building agents' last step before they report (c1ae622e).

### 9.2 What is built

Rules: `ActualSan`, `CounterRemoved`, `Triggers` with reductions, `VerifySanReduced`, `MayTakeSniperCheck`, `SniperCheckDrm`, `SniperCheckResult`, `SniperCheckConditions`, `VerifySniperCheck`, `SniperCheckChoiceText`. Units: `ChoicePending.SniperCheck`, the events `san-reduced` (side, cause `sniper-check` or `sniper-attack`) and `sniper-checked` (the Sniper, the units, the dice, `drm`, the result), `SideState.SanReductions`, the derived `GameState.FiredOrMovedThisPlayerTurn`, and the projector's `ReduceSan` and `CheckSniper`. Play: `SnipeInOrder`, `SniperCheckers`, `ReduceSan`, `PlanSniperCheckAnswer`: a Sniper attack pauses at the check, and the attacks still to resolve run in DR order after the answer; a `GameGate` case. MapStudio: record lines, Replay steps, and `ChoiceOptionText`.

The actual SAN is the printed one less its reductions, and at night the printed plus two, at most 7, never below the printed (E1.76); below 2 the counter is eliminated in the same commit (R71.9). A Final DR of 2 or less eliminates the Sniper, 3 pins it; every participant becomes TI and a concealed one loses its "?" in a Good Order enemy's LOS within 16 (A12.141); the DR calls no Sniper (R71.10).

**New in a record:** the events `san-reduced` and `sniper-checked`; the choice kind `sniper-check`; `SideState.SanReductions` (default 0, `JsonIgnore`). **An older record** holds none and replays as it was; `FiredOrMovedThisPlayerTurn` is derived from records every game holds, read by the planner only, and never verified.

### 9.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-sniper-check-eliminates` | The Russian PFPh: the 4-4-7's IFT DR of 1, 1 calls the German Sniper (SAN 2), whose dr 1 breaks the 4-4-7 in [Q8]; the Russian view shows `#play-choice` "The Russian side answers before play goes on: the Russian units in [Q8] that may take it make a Sniper Check ... (A14.4) (ruling R5.8)" with "take" and "decline"; taken with dice 2 2: "... DR 2, 2 = 4 - 2 = 2, the Sniper is eliminated; they are TI (A14.4). The German Sniper is eliminated by a Sniper Check: the German SAN is now 1, below 2, so its Sniper counter is removed (A14.1, A14.4)." The file holds `choice-pending` (kind sniper-check), `choice-made`, the check's `dice-rolled`, `sniper-checked` (units r-h, r-l; drm -2; result sniper-eliminated), two `asl:ti`, `san-reduced` (german, sniper-check), and `instance-eliminated` gs. The script's dice plan differed (the K/1 drew no MC, so the Sniper's dr was 1, not 2); the game still shows the check. Played. |
| `p71-sniper-check-pins` | A check of 3 pins the Sniper; asserted by `ASniperCheckOfThreePinsTheSniper`, not walked. |

Rules: `TheActualSanIsThePrintedLessItsReductionsAndAtNightNeverBelowThePrinted`, `ASanBelowTwoRemovesTheCounter`, `TheSniperCheckDrmFollowsTheExamplesThreeCases`, `ASniperCheckCountsEachHeroAndEachCrewAndAHeroicLeaderGivesOneOrTheOther`, `ASniperCheckFinalDrOfTwoEliminatesTheSniperAndAThreePinsIt`, `OnlyUnpinnedNonTiArmedGoodOrderInfantryThatHasNeitherFiredNorMovedMayTakeTheSniperCheck`. Play: `AnEffectiveSniperAttackOffersTheSniperCheckAndDecliningItResolvesNothingMore`, `TakingTheSniperCheckEliminatesTheSniperAndReducesItsSan`, `ASniperCheckOfThreePinsTheSniper`, `AtSanTwoTheSniperCheckRemovesTheCounterAndALaterDrOfTwoCallsNothing`, `ASnipersOneOnTheEnemySniperReducesItsSanAndAtTwoRemovesItsCounter`, `AtNightASniperCheckNeverTakesTheSanBelowThePrintedSoTheCounterStays`. Page: `TheSniperCheckIsTheGermanSidesChoiceAndItsLinesNameTheHex`. By method: Rules 9 of 9; Play 6 of 6 new (after one fix: Good Order read Unknown when berserk was unrecorded) and 16 of 16 in the blast radius; MapStudio 3 of 3; build exit 0. The Units tests compare no whole state or side record, so none was reached by the new state fields.

## 10. The Sniper counter at setup: 71.1, part III (increment 4c)

Built on 2026-10-26, 10:31 to about 11:37, in one commit (88f93d8c) by a building agent to a brief (`brief_4c.md`), ruling R71.11, its diff read and its game played in the Studio by the session.

### 10.1 Departures from the frame, first

- **The blast radius, and a departure from the unit-test rule's "over about 25, narrow first":** all four real cards have a SAN of 2 or more, so every card test that starts play must place Snipers first. A test helper, `CardSnipers.PlaceDue`, places each owed counter in the first hex offered, hooked into the `Act` and `Advance` helpers of `BacklogPass19`, `20`, `20TablePlayer`, `21`, `21TablePlayer`, `23`, `24`, `25`, and `26` (by `card_snipers_helpers_4c.py` and `card_snipers_helpers_4c_any_action.py`). The agent ran about 85 card-start methods in batches by file (a static scan, `card_start_tests_4c.py`, found 130 candidates). Accepted as the real radius: each method starts a card's play, which the change gates, and a narrower set would have left the helpers untried. Placed counters are live in those games, so a later DR equal to the SAN now calls a Sniper; every listed method passed. The session ran six more MapStudio methods that start or advance a card's game, by method: 6 of 6.
- **Two older tests moved their Sniper from [H8] to [E8]** (`BacklogPass16Tests.AtNightTheSanIsTwoHigher`, `BacklogPass16TablePlayerTests.SanCappedAtSeven`): A14.2 refuses [H8], more than six hexes from the only enemy. Their assertions are unchanged.
- **The `sniper` step is for card games only;** a game without a card places by hand under the same check, and a Sniper typed into a card game's `placements` is refused. "Unoccupied" is read from the placing side's view (a hidden enemy neither counts nor occupies); the candidates are the card's playable hexes at level 0. Accepted.
- **`icon-coverage.json` was regenerated,** and picked up Codex's `AcronymSearchPanel` controls, which the merged main left uncovered (`IconCoverageTests` would fail on main). Main's CI for 0b5b9a1d passed because its scoped run did not run the MapStudio tests; the pass's pull request carries the fix. For the user.
- **Found in the Studio, for the fixes:** the setup list named a placed Sniper counter "sniper-russian: a Dummy" (probably older than the pass), and the hand placement's refusal said "choose a hex the setup offers", which fits the card's block only. Fixed at Round D.

### 10.2 What is built

Rules: `PlacementRange`, `Placements`, `PlacementBar`, `PlacementsDue` (the DEFENDER first), `PlacementDueBar`, `PlacementTurnBar`, `CardPlacementBar`, `PlacementText`, `NoCounter`, `NoCounterText`. Play: `GamePlanner.SniperPlacement.cs` (`SniperPlacements` memoized per side, `SniperPlacementsDue`, `SniperCounterMissing`, `PlanSniperPlacement` for `asl.game.setup`'s `sniper: {side, at}`, `HandSniperBar`); `CardSetupIncomplete` holds play until each side owing a counter has placed it; `PlayStepMemo.SniperPlacements`. MapStudio: the setup's `#setup-sniper` block (`#sniper-hex-{side}`, `#propose-sniper-{side}`, `#sniper-waiting`, `#sniper-next`) and `#play-sniper-missing`.

A placement is legal by A14.2's tiers: an unoccupied hex within six hexes of the most enemy-occupied hexes any unoccupied hex has, at most six, counted from the placing side's view (enemy "?" counted; hidden units and the enemy Sniper not). The other side reads only that the side is placing its Sniper; the placed counter is public.

**New in a record:** the `sniper` argument of `asl.game.setup`, which writes the counter's `instance-created` (kind `asl:sniper`) as a hand placement always did; no new event, action, or choice kind. **An older record:** a game whose setup closed before the pass is never given a counter, so its replay stays as it was, and the page says once that the side has no Sniper counter.

### 10.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-sniper-by-hand` | The German RPh, setup open, the Russian side's list: the Russian Sniper counter by hand in [B5] refused: "[B5] is no hex for the russian Sniper counter: it goes in an unoccupied hex within six hexes of 1 or more enemy-occupied hexes, the most any unoccupied hex has; ... (A14.2; ruling ...)"; in [P9] "Every check passed", committed. Played. |

The card game's block (the DEFENDER first, the other side's wait line) is asserted by the page tests; a whole card's setup was not walked by hand. Rules: `ASniperGoesWithinSixHexesOfSixEnemyHexesWhenItCanAndOfFiveWhenNoHexHasSix`, `ASniperGoesInAnUnoccupiedHexAndAnywhereWhenNoEnemyIsOnTheBoard`, `ASnipersPlacementCountsTheEnemyUnitsItsSideSeesAndNotTheHiddenOnes`, `EverySideWithASanOfTwoOrMorePlacesItsSniperCounterBeforePlayTheDefenderFirst`, `AGameStartedWithoutASidesSniperCounterSaysItMakesNoSniperAttacks`. Play: `ACardGameStartsPlayOnlyOnceEachSideHasPlacedItsSniperCounterTheDefenderFirst`, `ACardsSniperHexesAreTheUnoccupiedPlayableHexesNearTheMostEnemyHexes`, `AGameMadeWithoutACardPlacesItsSniperByHandUnderTheSameCheck`, `ASnipersHexesCountTheEnemyQuestionMarksItsSideSeesAndNotTheHiddenUnits`, `AGameWhoseSetupClosedBeforeThePassIsGivenNoSniperCounterAndSaysSo`. Page: `TheSetupsSniperBlockOffersTheDefendersHexesFirstAndTheOtherSideSeesOnlyThatItPlaces`, `AGameStartedWithoutTheRussianSniperCounterSaysOnceThatItMakesNoSniperAttacks`. By method: Rules 5 of 5; Play 24 of 24 and the card-start batches 19, 38, 22, and 9; MapStudio the 4 page tests, 5 `SetupAnswersPageTests`, and the icon coverage; strict builds of the Rules, Play, and MapStudio tests exit 0 with no warning.

## 11. The Sniper player's choices: 71.1, part IV (increment 4d)

Built on 2026-10-26, 11:44 to about 12:20, in one commit (c6952864) by a building agent to a brief (`brief_4d.md`), ruling R71.12 (R15.5's game picks replaced) and A12.14's exception, its diff read and its game played in the Studio by the session.

### 11.1 Departures from the frame, first

- **The forfeit is asked only where 4c's placement allows another hex;** with a single legal hex the counter moves there at once. Accepted.
- **A concealed stack is never offered before Random Selection** (A14.23: the Sniper player cannot see into it). Accepted.
- **Tests changed as intended,** since they asserted R15.5's picks or ran without the pause: `ScenarioA1SequenceRulesTests`' Sniper trigger method, two `TargetHex` assertions, Pass71's helper and three methods, `BacklogPass15Tests.ADrEqual...`, three `BacklogPass15TablePlayerTests` methods, and the two page tests; each now answers the choice. R15.5's `TargetHex` name tie-break and `TargetLocation` were removed from Rules.
- **Found for the fixes:** the choice text named sides by raw id in lower case ("the german Sniper"); the hex said twice in the Sniper line ("[L4]: 4-4-7 squad in [L4]"); "(ruling R5.8)" after every choice's own citation (the panel's suffix). Fixed at Round D.
- **The games script's dice plan missed a die** (the K/1's Random Selection of the Reduced unit), so the first try drew a Sniper dr of 4; the games were rewritten, with the Studio stopped first, and played again.

### 11.2 What is built

Rules: the choice kind `sniper` with the steps `forfeit`, `reposition`, `hex`, `location`, `target`, and `first`; `ChoiceOffered`, `TargetOption`, `PriorTargets` (`SniperCandidate`), `ChoiceText`, `TargetHexes`; the result `forfeited` on `sniper-attacked`. Units: `ChoicePending.Sniper` in the projector's `PendChoice`. Play: `Snipe` resumable through `SniperResume` (the counter moves once; recorded rolls are read back; new dice are drawn in the answer's commit), `PlanSniperAnswer`, the attacks still to resolve through `SnipeInOrder`, a `GameGate` case; `MovementPlans.Waiting` stops on any pending choice as the choosing side's turn; `HandOverWhy` puts a waiting choice first. MapStudio: `PendingChoicePanel` turns more than six options into `#play-choice-select` and `#play-choice-propose` (the icon coverage regenerated).

The Sniper player decides, by a pending choice for his side made only where more than one option exists: to attack or to forfeit and reposition (then the legal hexes); the equidistant hexes left after the lowest TEM; the Location in a hex of several; before Random Selection, the enemy Sniper, a Vulnerable Inherent Crew, or an unarmored vehicle present, or Random Selection; and which of several targets takes the first dr. While it waits nothing else may happen, so no voluntary removal of a "?" comes between the Random Location DR and the relocation (A12.14's exception, p. 78).

**New in a record:** the choice kind `sniper` and its `choice-pending` and `choice-made`; the result `forfeited`. **An older record** holds R15.5's picks as recorded events and replays them as it was.

### 11.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-sniper-equidistant` | The Russian PFPh, dice 1 1, 1, 1 2, 1: the hand-over to the German side says "The German side answers before play goes on: the German Sniper's dr of 1 is effective: it attacks, or forfeits ... (A14.2) (ruling R5.8)"; the German view offers "Propose: attack: make the Random Location DR (A14.2)" and "Propose: forfeit the attack and reposition the Sniper counter (A14.2)"; "attack" with 1 3 onto the empty [M4] gives the equidistant choice "[L4]" or "[M5]"; [L4] gives "dr 1; [L4]: 4-4-7 squad in [L4] is broken (A14.3)". Played. |
| `p71-sniper-location`, `p71-sniper-tied` | The Location of a two-level hex and the tied target; asserted by their Play tests, not walked. |

Rules: `EquidistantHexesOfTheLowestTemAreTheSnipersChoiceAndAnyOtherTieIsNot`, `BeforeRandomSelectionTheSniperMayChooseTheEnemySniperACrewOrAnUnarmoredVehicleButNoConcealedStack`, `TheSnipersChoicesAreWordedByTheirStepAndCiteTheRule`. Play: `AnEffectiveDrAsksTheSniperPlayerToAttackOrToForfeitAndRepositionHisCounterInPublic`, `APinnedSniperNeitherAttacksNorRepositions`, `EquidistantHexesOfEqualTemAreTheSniperPlayersChoiceAndNoQuestionMarkIsRemovedWhileItWaits`, `TheSniperPlayerChoosesTheLocationOfAHexOfTwo`, `BeforeRandomSelectionTheSniperPlayerMayChooseAnUnarmoredVehicle`, `TheSniperPlayerChoosesWhichTiedTargetTakesTheDrAndTheOtherTakesANewOne`, `OfTwoSniperAttacksInOneStepTheSecondResolvesOnlyAfterTheFirstsChoice`. Page: `TheSnipersChoiceIsTheRussianViewsToAnswerTheGermanViewReadsItAndTheHexesToRepositionToAreOneList`, `AMovementPlanWaitsForTheSnipersChoiceAsTheChoosingSidesTurn`. By method: Rules 6 of 6; Play 26 of 26 (25 methods, one Theory twice); MapStudio 6 of 6 (the icon coverage run with the update flag, then without); strict builds of the Rules, Play, and MapStudio tests exit 0.

## 12. Every A14.1 trigger: 71.1, part V (increment 4e)

Built on 2026-10-26, 12:24 to about 13:03, in one commit (cc72d08a) by a building agent to a brief (`brief_4e.md`), ruling R71.13 (R55.30 superseded), its diff read and its game played in the Studio by the session.

### 12.1 Departures from the frame, first

- **The reader is idempotent rather than skipped on a second call:** a trigger already answered by a `sniper-attacked` (matched on `trigger`), or whose Sniper is gone or pinned, is not called again. Accepted.
- **Canister's maker is the firing side,** since its records name no firers. Accepted.
- **Not given a Play test:** a SMC's OVR NTC against a vehicle OVR, and a crew's `collateralCheck` and `taskCheck`; their makers are tested in Rules. Accepted.
- **Found by the agent, older than the pass, for the fixes:** a trigger in a record written before the same step's own pending choice is lost if the resume commit does not record it again (Spray Fire's first Location when the second asks); and a unit created in the same step (the HS a Reduction leaves) that takes a check is not among the step's units before it, so its DR finds no maker and calls no Sniper. Neither was given to a round: backlog rows (section 15.3).
- **For the hidden-information referee:** the trigger's roll id is public in `sniper-attacked.trigger` and in a public resume's `rest`, so a withheld check's DR is known to have equalled the SAN, as fire triggers always were. Judged inherent at the reviews (R71.24).
- **`BacklogPass55Tests`' Canister method reversed and renamed as R71.13 intends:** `ACanisterShotResumedAfterAnOwnersChoiceCallsTheSniperOnceOnItsDr`. R55.30's Canister skip was removed; the DC skip is kept.

### 12.2 What is built

Rules: `RollKey` strips `hit:`, `critical-hit:`, and `prc:`; `RollMaker(kind, unitNamed, firingSide, checker, vehicleSide)` names each roll's maker (`toHit`, `canister`, and `attack` the firing side's; `collateralCheck`, `taskCheck`, and an unnamed `crewCheck` the target vehicle's side's; `areaCrewCheck` and the PAATC, NTC, and Bail Out rolls the unit's, never a prisoner's); `FiringSide` with the Canister Gun's side. Play: a wrapper of every Ready plan's `Roll.Build` (after the pending-choice bars, before `WithAcquisitions`); `AddSniperAttacks` is now the one reader (`SniperTriggerWalk`, `ReadTriggers`) of `FireResolved`, `OrdnanceFired`, `MoraleCheckResolved`, `RiderBailedOut`, `PaatcTaken`, `TaskCheck`, and `InfantryOverrunNtc`, looping over each round's events and stopped by a pending choice; `KeepTriggers` carries a Sniper's own LLMC triggers into a pause's resume; `PlayStepMemo.Snipers(state)` holds the active counters by side, so a step with no enemy Sniper on the map returns at once.

**New in a record:** no event, action, choice kind, key, or text. **An older record** replays its recorded attacks as it did: the reader runs only when a step is planned. A new plan with a Canister shot, an ordnance To Hit, or any of the new kinds now calls a Sniper where its DR equals the enemy SAN.

### 12.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-sniper-to-hit` | The Russian PFPh, dice 1 2, 6 6, 4: "T-34 M41 R1 in [P8] fires HE at [P10]: ... DR 1 (colored), 2 = 3 ... Critical Hit ... The German Sniper (gs) attacks after the SAN roll gun-...-roll-1: dr 5, no effect (A14.3)." An ordnance To Hit DR calls the Sniper (R71.13). Played. |
| `p71-sniper-residual` | Residual FP on a move; asserted by `ResidualFpOnAMoveCallsTheSniperOnlyWhenItsDrEqualsTheEnemySan`, not walked. |

Rules: `AToHitACanisterAndAnHeHitsIftDrAreTheFiringSidesAndTheirNestedChecksTheUnits`, `AVehicleCrewsCheckIsItsVehiclesSideAndEveryOtherCheckItsUnitsButAPrisoners`. Play: `ResidualFpOnAMoveCallsTheSniperOnlyWhenItsDrEqualsTheEnemySan`, `AnOrdnanceToHitDrCallsTheSniperOnlyWhenItEqualsTheEnemySan`, `TheHeHitsCheckOfItsTargetCallsTheEnemySniper`, `TwoTriggersOfOneMoveAreResolvedInTheOrderOfTheirDrs`, `AFireLaneAttackOnALaterMoverCallsTheSniper`, `ASearchsLlmcInTheMphCallsTheSniper`, `ARidersBailOutNmcCallsTheSniper`, `AVehicleOvrsIftDrCallsTheSniper`, `APaatcInTheMphCallsTheSniper`, `APaatcInTheAphCallsNoSniper`, `AnInfantryOvrNtcCallsTheSniper`, and the LLMC chain (renamed at Round B as `ASnipersOwnLlmcCallsTheNextAttackBeforeItsSniperCheck`); `BacklogPass55Tests.ACanisterShotResumedAfterAnOwnersChoiceCallsTheSniperOnceOnItsDr`. By method: Rules 3 of 3; Play 15 of 15 new, 20 of 20 Pass71, 19 of 19 other Sniper and card methods; MapStudio 4 of 4; strict builds of the Rules and Play tests exit 0 (the MapStudio tests unchanged).

**The measurement after 4e** (section 16), each Studio alone in a fresh process at 1920x1080: the pass's warm hand-over median about 216 ms against main's 292; "end the phase" 58 to 86 against 69 to 172 after the first. Level or faster; the stop threshold was not reached.

## 13. The pin after more than 3 MF, the SMC crew's Stun wound, and A18.2's -1: 71.2, part I (increment 4f)

Built on 2026-10-26, 13:08 to about 13:38, in one commit (128fed24) by a building agent to a brief (`brief_4f.md`), rulings R71.14, R71.15, and R71.17, its diff read by the session; its two games did not replay (below).

### 13.1 Departures from the frame, first

- **Mopping Up is not reached:** A12.153 (p. 79) puts it in the PFPh, so R71.14's MPh pin never meets it. Right.
- **The Search's own extra MF count before its Casualty dr** (A12.152). Accepted; Rules-tested only, moved into Rules at Round C (`PinnedByWound(..., extraHalfMf)`).
- **`ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack` moved as intended** (now a 6+1, asserting `afvDrm` and `cc-vs-afv`); `AnOlderRecordedVehicleCc...` clears `AfvDrm`; a truck test was added to `BacklogPass70Tests`.
- **The Fire path's Stun of a SMC crew is not driven in Play:** the three writers share `AddSmcCrewSeverity`, and the Sniper path is driven in Play and on the page. Accepted.
- **`woundPins` and `afvDrm` were grepped against older records' keys:** none found; for `SKIP_JSON`, to be checked against the store at the gate (section 17).
- **Found for the fixes:** the record line "Wound Severity dr 1 for de-l" named the man by raw id and without his hex (fixed at Round D); a Sniper Stun of a concealed tank that keeps its "?" wrote a public `conditions-changed`, now with the crew's id in the roll id (4a's known leak; fixed at Round A).
- **The games did not replay:** `games_4f.py` started the leader in [P14], and board 3's rows end at 10. A four-MF route ending in a checked LOS was not found from the hex facts without the board viewer's reads. The two games were moved out of the live folder (not deleted, to `asl-retired-2026-10-26/p71-unstaged`), so no game that does not replay enters the gate's store; the wound pin and the SMC crew's Stun were owed to the whole-pass check (section 15.4).
- **A slip of the agent:** one `cd` into the repository that the hook did not refuse (the hook's gap at the repository root, pass 70's Round D). For the tools' backlog.

### 13.2 What is built

Rules: `ScenarioA1Wounds.PinnedByWound(smc, phase, mfSpent, halfMfSpent)` (a `broken` fact since Round B, an `extraHalfMf` since Round C); `FireTarget.WoundPins` (JSON `woundPins`, absent as before) and `TargetState.Wound()` pinning when it is true (the effect `pinned-wound`); `ScenarioA1Sniper.WoundedConditions(pinned)`; `ScenarioA1CapturedVehicleRules.SmcCrewOutcome(..., severityDr)` and `SmcCrewSeverityDue`; `VehicleCloseCombatFacts.AfvDrm` (JSON `afvDrm`) with `cc-vs-afv` -1 when it is true and the vehicle armored. Play: `WithWoundPins` on every attack, the verifiers taking it from the record; `SnipeUnit` pins a wounded SMC; the Search's casualty; `AddSmcCrewSeverity` (Fire, ordnance's `VehicleEffectsAfterHit`, `SnipeVehicle`) rolls `wound-severity` with the id `{attempt}-wound-{crew}` before the vehicle's change; `PlanVehicleAttack` writes `AfvDrm`. Units: `SmcCrewAfter` finds that roll by the event's attempt id. MapStudio: `PlayRecords` names the man of the dr from the roll id.

**New in a record:** `woundPins` on a fire target; `afvDrm` on vehicle CC facts; a `wound-severity` roll before a SMC crew's Stun. **An older record** carries none: its attack resolves as it did (absent means not pinned), its vehicle CC takes no -1, and a Stun with no Severity dr keeps the older result (a second wound eliminating) in `SmcCrewAfter`.

### 13.3 The games and the tests

No game of 4f replayed (13.1). The wound pin was played at the whole-pass check (`p71-check-wound-pin`, section 15.4); the SMC crew's Severity dr is shown by the page test `TheWoundLineOfASmcCrewsStunNamesHim` and Round D's record line.

Rules: `ASmcWoundedInTheMphAfterMoreThanThreeMfIsPinned`, `ASmcCrewsStunWoundTakesItsSeverityDrAndARecallStillEliminatesHim`, `LeaderCreationInCcAgainstAnAfvTakesMinusOne`. Play: `ALeaderWoundedByDefensiveFirstFireAfterMoreThanThreeMfIsPinned`, `ASniperWoundInTheMphAfterMoreThanThreeMfPins`, `ASniperStunOfATankCrewedByASmcRollsHisWoundSeverityDr`; `BacklogPass70Tests.ASquadsOriginalTwoAgainstATruckTakesNoAfvDrmForItsLeader`. Page: `TheWoundLineOfASmcCrewsStunNamesHim`. By method: Rules 8 of 8; Play 13 of 13; MapStudio 2 of 2; strict builds of the Rules, Play, and MapStudio tests exit 0.

## 14. Carrying a wounded man: 71.2, part II (increment 4g)

Built on 2026-10-26, 13:43 to about 14:07, in one commit (2dd2789d) by a building agent to a brief (`brief_4g.md`), ruling R71.16, its diff read and its game played in the Studio by the session. The increment R71.1 named as the one a session's end would leave out was built; nothing went to pass 72.

### 14.1 Departures from the frame, first

- **The carried man is a mover of the step,** so he is attacked with the stack and takes FFNAM, his MF recorded as 0. Accepted.
- **"Set down" is a planner bar:** a carried man whose carrier is not among a later step's movers moves no farther that MPh. Accepted; the projector runs the bar too since Round B, and R71.28 states it as the ruling's, not A17.2's.
- **A carry lasts the move;** the 5 PP stay counted (A4.4); a plotted route continues but cannot start a carry; Double Time stays refused (A17.2). Accepted; R71.28 records the Double Time bar as stricter than the page.
- **The carry in the APh is not built:** A4.7 and A4.72 (p. 52) with its example allow it. A backlog row.
- **Found for the fixes:** the proposal summary named units by raw id ("de-s carries the wounded de-l at five PP"); the existing `#move-ipc` select labelled its options "id (Location)" (the hex UI rule, older than the pass); the raw public `movement-step` names a concealed man in `carried` as it names the movers (for the hidden-information referee; R71.24). The first two fixed at Round D.

### 14.2 What is built

Rules: `ScenarioA1Wounds.CarriedManPp`, `Portage`, `MoverHalfMf`, `CarryBar`, `CarriedMoveBar`, `CarriedText`, `CarryFacts`, and `MfLeftMoverFacts.Carried`. Units: `CarriedMan`, `MovementStepped.Carried` (JSON `carried`: a list of `{man, carrier}`, written only with a carry), `MovementState.Carried`, the projector's check (UNIT-STATE-029), and no MF spent by the man. Play: `PlanMove` reads `carried`; the bars; the carrier's 5 PP through `Portage` and `WeaponPortage`. MapStudio: `#move-carry` and `#move-carrier` (through `UnitOptions`), and the record line's carry.

**New in a record:** the `carried` argument of `asl.game.move` and `carried` on `movement-step`. **An older record** has none, and its steps replay as before.

### 14.3 The games and the tests

| Game | What it shows |
|---|---|
| `p71-carry-wounded` | The German MPh, [D9] to [G10] Open Ground (checked with `hex_facts.py`): `#move-carry` offers "[D9]: 8-1 leader G1", `#move-carrier` "[D9]: 4-6-7 squad G1"; the move to [D10] committed: "8-1 leader G1, 4-6-7 squad G1 move from [D9] to [D10], 4-6-7 squad G1 carrying the wounded 8-1 leader G1 (A17.2)"; the file holds `movement-step` with `"carried": [{"man": "de-l", "carrier": "de-s"}]`. Played. |

The MF limit and the set-down are asserted by their tests. Rules: `AWoundedManIsCarriedOnlyInGoodOrderWithNoSwByAGoodOrderMmcOfHisStack`, `ACarriersFivePpReduceItsMfByThePortageRulesAndTheManSpendsNone`. Play: `ASquadCarriesAWoundedLeaderWhoSpendsNoMfAndItsOwnMfFallByFivePp`, `AWoundedLeaderWhoPortagesASwIsNotCarried`, `ACarriedManWhoseCarrierStopsIsSetDownAndMovesNoFarther`, and four portage methods for the `Portage` change. Page: `TheMovementPanelOffersToCarryAWoundedManOfTheStackByHexAndTheMoveRecordsIt`. By method: Rules 2 of 2; Play 7 of 7; MapStudio 2 of 2; strict builds of the Rules, Play, and MapStudio tests exit 0.

## 15. The five reviews, their fixes, and the Studio check

Five read-only reviews ran in parallel on 2dd2789d, from 14:11 to between 14:14 and 14:22 (briefs in `brief_reviews.md`): R1, the referee of the Sniper's rules; R2, the referee of hidden information, read from the side that must not see; R3, the referee of wounds and Field Promotions; R4, a table player who read the events of every `p71-*` game; and R5, the UI, the Rules boundary, and memory and performance. Beside them the gate's strict stage (`gate.sh 71 0b5b9a1d strict`, 14:11 to 14:14) passed: 0 warnings, 0 errors, and its formatting left no file changed. In sum: no BLOCKER, 7 MAJOR (R2 5, R3 1, R5 1), 32 MINOR, and 21 NOTE. No review found an older record whose replay changes. The findings as reported are `fixes_r1_sniper.md`, `fixes_r2_hidden.md`, `fixes_r3_wounds.md`, `fixes_r4_table.md`, and `fixes_r5_ui_boundary.md` in the pass's folder; `brief_fixes.md` ordered the fixes in four rounds, one agent each.

### 15.1 What departs from the plan's entry and the frame, first

- **Eleven rulings were made at the reviews** (F15, 14:22 to 14:23): R71.18 (the Sniper Check offered while a "?" stays), R71.19 (the reposition hexes the Sniper side's alone), R71.20 (a concealed vehicle's PRC and crew in its concealed stack), R71.21 (a "?" lost only to an unconcealed seer: a deviation in the target's favor), R71.22 (one neutral public result for a kept "?"), R71.23 (Random Selection inspects its Location), R71.24 (the carried man and the trigger's roll id public), R71.25 (a Sniper's own LLMC attacks before its check), R71.26 (a SMC Temporary crew of a vehicle not fully armored attacked as a SMC), R71.27 (a broken SMC never pinned by a wound), and R71.28 (R71.16's readings stated). R71.21 was read on the page first: A12.14's concealed-viewer sentence covers "a concealment-loss action (other than breaking or being Reduced/Wounded)", so it reaches a Sniper attack.
- **R71.13's "each resolved after the attacks before it" is read by R71.25:** the build had resolved a Sniper's own LLMC attacks after the whole batch and after its Sniper Check (R1's finding 1).
- **R71.12's public choice was narrowed** for the reposition step (R71.19), and R71.10's choice, offered only when a unit could take it, became an offer whenever a "?" stays (R71.18).
- **The proof games recorded before 4d show the older flow** (R4's finding 1: `p71-sniper-ce-tank`, `-truck`, `-vs-sniper`, `-check-eliminates` hold no forfeit choice and the truck's Random Selection no A14.2 EXC choice). No round re-recorded them: they replay to their last event (15.4), and they stand as records of a flow an older game can hold. No backlog row.
- **Found by Round B, outside its scope:** CA1860 in pass 70's `GamePlanner.StreetFighting.cs:87` (`.Any()` for `.Count > 0`) failed the strict Play test build, though the gate's strict stage of 14:14 had passed; the two builds disagree. For the tools' backlog.
- **Slips of the agents with the user's games:** a Grep pointed at `src/ASL` by frame agent D (a `!boards/**` glob: file names and two matching lines listed), R3 (lines and names under boards, `dl-tractor-works-*` among them), R5 (one test file), Round A (a `!**/boards/**` glob, nothing under boards shown), Round B (one test file name), and Round D (a `*.json` glob that listed seven `p70-*` names); none opened a game. The hook's gap at the repository root let through one `cd` each of 4f's agent, Round C's, and Round D's; 4a's and 4c's were refused.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 70's as the model: section 15.4.

### 15.2 Fixed

**Round A, hidden information** (14:23 to about 14:53, d1c18a3a; R2 1 to 8 and 11, R1 3, R5 11, R4 7, the notes' known item):
- R71.18: `SniperCheckOffered` offers the check when a unit may take it or a unit of the attacked side under "?", or a Dummy, remains; a "take" with none commits only `choice-made` (R2 1, MAJOR; R1 3; R5 11);
- R71.19: the reposition step's `choice-pending` is visible to the Sniper side only (R2 2, MAJOR);
- R71.20: `InConcealedStack`, `CountsAsConcealedVehicle`, and `StackTargets` in Rules: a concealed vehicle's PRC, crew, and itself are one candidate, and a concealed vehicle with no target one `concealed-vehicle` candidate; `AddUnconcealed` takes the "?" from all that share it (R2 3, MAJOR);
- R71.21: `NearestUnconcealedLossSeer` (the `LossInView` purpose), for the attack and for the Sniper Check's participants (R2 4, MAJOR);
- R71.22: `PublicResult` "concealed-kept" with an owner-only twin; a kept target's conditions owner-only; the check's dice with the record's visibility; `AddSmcCrewSeverity(visibility)` (R2 5, MAJOR; R2 6, 11; R4 7);
- R2's 7, 8, and 9 accepted under R71.23 and R71.24; R2's 10 needs no change; R2's 12 went to Round C.
The SMC crew's Severity dr made owner-only is untested (a concealed tank with a SMC Temporary crew needs a re-crew in the MPh); the vehicle's Stun record with the same visibility is tested. The vehicle tests set their concealed vehicles in Grain, since setup refuses a concealed vehicle in Open Ground (R6.7). Rules 3 of 3; Play 25 (22, then the three Grain fixes 3 of 3); MapStudio 5 (4, then the reworked new test 1 of 1); strict builds exit 0. Tests: Rules `AConcealedVehiclesPrcCrewAndItselfAreItsStackAndOneWithNoEligibleTargetIsStillOneCandidate`, `ATargetThatKeepsItsQuestionMarkHasOneNeutralPublicResult`, `TheSniperCheckIsOfferedWheneverAUnitMayTakeItOrAQuestionMarkStaysInTheLocation`; Play `AConcealedTargetThatKeepsItsQuestionMarkIsNamedWithItsResultToItsOwnerAlone`, `AConcealedTanksStunKeepsItsQuestionMarkAndIsItsOwnersAlone`, `TheSniperCheckIsOfferedWhileAQuestionMarkStaysThoughNoUnitThereMayTakeItAndTakingItRollsNothing`, `OnlyAnUnconcealedEnemySeerCostsTheQuestionMarkInASniperAttack`, `ASniperCheckSeenOnlyByAConcealedEnemyKeepsItsQuestionMarkAndItsDiceAreItsOwners`, `AConcealedTrucksPassengerCrewAndTruckAreOneQuestionMarkAndWhatOneLosesTheyAllLose`, `AConcealedButtonedUpHalftrackIsStillOneQuestionMarkCandidateAndIsAttackedToNoEffect`; page `AConcealedTargetThatKeepsItsQuestionMarkIsOneNeutralLineToTheEnemyAndNamedToItsOwner`.

**Round B, the Sniper's and the wound's rules** (14:53 to about 15:40, a47712b7; R5 1, 12, 13, R1 1, 2, 4, 5, 7, R3 1, 2, 3, 5, 7):
- R5's 1 (MAJOR): the trigger roll id in both choice keys, `sniper:{id}:{trigger}:{step}` and `sniper-check:{id}:{trigger}`, so two pauses of one Sniper in one commit no longer share a key the projector refuses;
- R71.25: an owed Sniper Check is a queue item; after T1, its LLMC attacks, then T1's check, then the rest; an LLMC-called attack's own check before T1's (R1 1);
- R71.26: a SMC crew of a vehicle not fully armored goes through `SnipeUnit`, his elimination Abandoning the vehicle with his LLMC (R1 2);
- R1's 4: a new step `stack-first`, answered by places among the tied units beneath the "?"; R1's 5: the lowest-TEM Location decides only among equidistant hexes at the closest distance, and SMOKE counts per Location; R1's 7: `VerifySniperAttack` refuses a pinned Sniper's attack;
- R71.27: `PinnedByWound(smc, broken, ...)`, `!Broken` in `Wound()` (R3 1, MAJOR); R3's 2: `LiveFire.RecordedWoundPins` in both verifiers; R3's 3: the projector runs `CarriedMoveBar`; R3's 5: "is wounded and pinned (A14.3, A17.2)", the Fire effect "wounded and pinned (A17.2)"; R3's 7: a Rules test of the carried leader's bonus (R71.28);
- R5's 12: the memo's comment corrected (the placement must read the state of that moment, since the step may have moved units); R5's 13: `ReplayInCommit`, in-commit projections kept out of the planner's 8-slot cache.
Not fixed: R3's 9 (a backlog row). Rules 7 of 7; Play 10 new or updated (9 of 11, then three reruns 1 of 1 each), the blast radius 29 of 30 then the updated one 1 of 1, older triggers 7 of 7, further 8 of 8; MapStudio 8 of 8; strict builds exit 0. Tests: Rules `ABrokenSmcIsNeverPinnedByAWound`, `ASmcTemporaryCrewOfAVehicleNotFullyArmoredIsAttackedAsTheSmcHeIs`, `WhereTheTemComparedEquidistantHexesOnlyTheLowestTemLocationIsTheTarget`, `ATiedTargetBeneathAQuestionMarkIsAnsweredByItsPlaceAlone`, `APinnedSnipersRecordIsRefused`, `ACarriedWoundedLeaderStillGivesHisCarrierTheTwoMfBonus`; Play `ASnipersOwnLlmcCallsTheNextAttackBeforeItsSniperCheck`, `TwoTriggersOfOneSniperInOneRecordReachTheForfeitEachUnderItsOwnKey`, `ASnipersOwnLlmcAttackWaitsOnItsOwnForfeitThenTheFirstAttacksSniperCheckFollows`, `ASmcTemporaryCrewOfATruckIsEliminatedOrWoundedAsTheSmcHeIs`, `RandomSelectionInAConcealedStackGivesItsTieToTheSniperPlayerAndTheOtherANewDr`, `WhereTheTemComparedEquidistantHexesTheLowestTemLocationIsTheTargetUnasked`, `SmokeCountsInItsOwnLocationAloneInTheTemComparison`, `ABrokenLeaderWoundedAfterMoreThanThreeMfIsNotPinned`, `ARecordedWoundPinTheStateDoesNotGiveIsRefused`, `AStepRecordedForACarriedManWithoutHisCarrierIsRefused`; page `ASniperWoundThatPinsIsSaidWoundedAndPinned`, `AFireAttacksWoundThatPinsIsSaidOnceWithItsRule`.

**Round C, the Rules boundary** (15:40 to about 15:54, 34862753; R5 5 to 10, R1 6, R2 12, R3 4, R4 10), no behavior changed:
- `ScenarioA1Sniper.PlacementDefender` (Play's `SniperDefender` removed); `SniperCheckActivity` and `FiredOrMoved` (the raw facts composed in Rules); `SniperCheckParticipant.Wounded` applied in `SniperCheckDrm` (A17.3); `CrewImpaired` and `Manned`; `ScenarioA1Wounds.PinnedByWound(..., extraHalfMf)` for the Search; one SMC test, `IsA(kind, "asl:smc")`, through a lazy vocabulary in the static `LiveFire`; `ScenarioA1VehicleCloseCombat.AfvDrmDeclared`; `Triggers(dice, ...)` requires two dice.
The one widening (a unit of kind `asl:smc` itself, only in the synthetic catalog, now a SMC) is harmless. Seen by the agent, older than the pass: the Fire path's PRC exposure counts only a Stunned or Shocked crew as impaired, the Sniper path a Recalled one too (a backlog row). Rules 18 of 18 (7 new); Play 29 of 29; the strict Rules test build exit 0. Tests: `TheDefenderPlacesFirstTheScenarioDefenderOrElseTheSideThatDoesNotMoveFirst`, `AUnitHasFiredOrMovedByAnyFireMarkerAnyMfAFireThisPhaseAGunShotOrAnEarlierPhase`, `AWoundedLeadersLeadershipIsOneWorseInTheSniperCheck`, `AVehicleIsMannedUnlessAbandonedOrItsCrewBrokenAndAStunnedRecalledOrShockedCrewSparesItsPassengers`, `ASearchCasualtyCountsTheSearchsMfAmongHisForTheWoundPin`, `OnlyARollOfTwoDiceCallsASniper`, `TheAfvsMinusOneIsDeclaredOnlyWithALeaderCreationAgainstAVehicleNotPrintedUnarmored`.

**Round D, the page and the record lines** (15:54 to about 16:29, efdc4dae; R4 2 to 6, 8, 9, 12 to 15, R5 2, 3, 4, 15, 16, and the notes' known items):
- the Sniper lines with no raw ids ("after the IFT DR 3 + 3 = 6, equal to the Russian SAN (A14.1)", or "after a DR equal to ..." when the view may not see the roll); "makes its dr ...: 5, no attack (A14.3)"; the hex said once (`HexFirstSaidAgain`); sides through `DisplayText.Side`; `DisplayText.RollWords` for the five new roll purposes;
- a wound's Severity dr and the pin's reason ("mortally wounded ... and eliminated"); the Fire text "wounded, and pinned: wounded after more than 3 MF (A17.2)"; the wound line hex first; a SMC crew's Recall "its SMC crew is eliminated (... D5.343)";
- the Sniper Check line's DRM causes (`SniperChecked.DrmCauses`, JSON `drmCauses`, written only when non-empty, on a pass 71 event only); the check's "take" naming its takers, hex first, and the DRM with its causes, in the answering side's view only (`GamePlanner.SniperCheckOffer`); the night SAN "stays N, never below the printed SAN at night (A14.4, E1.76)";
- the equidistant choice naming where the Random Location landed (`place` on the resume); the Sniper options hex first, "the vehicle itself (A14.33)"; one closing citation "(A14.4; ruling R5.8)";
- `NoCounterText` citing R71.11; `CarriedMoveBar` citing R71.16; "the Russian Sniper counter" in the setup list; `#move-ipc` through `UnitOptions`;
- titles on the disabled `#play-choice-propose` and `#propose-sniper-{side}`; `.setup-sniper` and `#play-choice-select` CSS; `sniperHex` cleared after a setup commits; `CarryMen` and `Carriers` cached.
Not fixed: R4's 11 (a backlog row). Rules 4 of 4; Play 10 of 10; MapStudio 17 of 17, then 1 of 1; strict builds exit 0. Page test added: `TheSnipersOptionsBeforeRandomSelectionNameTheHexFirstAndTellTheTruckFromItsCrew`.

### 15.3 Not fixed: a row each in the backlog

The backlog's section 74.2 has each: A12.14's concealed viewer forcing the loss of "?" inside a Sniper attack (R71.21's deviation; R2 4); each Random Selection die's target named in the record (R4 11); a PRC SMC's wound pin (R3 9); the Fire path's PRC exposure not counting a Recalled crew (Round C); a trigger lost across a pause in the same step, Spray Fire's first Location (4e); a unit created in the same step that takes a check calling no Sniper (4e); the hook's gap at the repository root (4f, Rounds C and D); and the strict test build and the gate's strict stage disagreeing on CA1860 (Round B). Accepted as the reviewers judged them, with no row: the projector trusting the recorded Sniper Check DRM (R1 7, its rest); a new record lacking the crew's Severity dr replaying with the older outcome (R3 6, intended); a carrier unable to take a second man after setting down the first (R3 10); the wrapper's cost with no Sniper on the map (R5 14); the older-flow proof games (R4 1, 15.1).

### 15.4 The Studio check

At 16:29 to 16:42, in my Studio on port 6670 on the build of efdc4dae. Its scope, decided under the user's word, with pass 70's as the model (the standing rules, section 1, point 4): every pass game replayed; the games 4f owed; a kept "?" read from the side that must not see; the new choice controls at 1024 and 320; a real Enter key on a proposal and its commit; the hex audit; the measurement. `gate.sh 71 0b5b9a1d strict` ran again beside it, 16:29 to 16:34: 0 warnings, 0 errors, "the strict build passed for this tree"; no file changed by the formatter.

- **Every pass game replayed:** `replay_p71.sh` (the proof tool built against my Studio's build, its Play.dll compared byte for byte) replayed the 12 games then recorded, every one to its last event, with no ERROR, UNREADABLE, or NOSTATE line (168 lines; from copies in `asl-gate-work/replay-71`, registered; the live folder never read by the tool).
- **The wound pin (owed by 4f),** `games_check.py` (a), `p71-check-wound-pin`: the German 8-1 from [A9] by [A10], [B10], [C10] to [D10] (Open Ground; the Russian passing each window), then the Russian 4-4-7 in [F10] First Fires (range 2, -1 FFNAM, -1 FFMO), every die a 2: "4-4-7 squad R1 in [F10] fires at [D10]: K/1; 8-1 leader in [D10]: wounded, and pinned: wounded after more than 3 MF (A17.2)"; the file holds `woundPins` true on the target and `conditions-changed` de-l pinned and wounded. The SMC crew's Stun game was not staged: its Severity dr is shown by the page test `TheWoundLineOfASmcCrewsStunNamesHim` and Round D's record line.
- **A kept "?" from the side that must not see,** `games_check_b.py` (b), `p71-check-kept-concealed`: the German 4-6-7 in [E2] fires at the concealed Russian 4-4-7 in [E3] (IFT 3 3, the Russian SAN 6); the Russian Sniper's dr 2, "attack", Random Location 1 1 onto [E6], the concealed German 4-6-7 g2 ([E3] to [E6] Blocked at [E4] by the board viewer's LOS tab). The Russian view: "[E6]: a concealed target is attacked and keeps its "?" (A12.14, A14.23)" and the public Sniper Check wait; the German view: "[E6]: 4-6-7 squad G2 is pinned (A14.3); it keeps its "?", and the enemy reads only that a concealed target was attacked" and the offer "take the Sniper Check: no unit here may take it, so none is made (A14.4)" (R71.18). The file holds the public `sniper-attacked` with no unit and `concealed-kept`, and its twin and g2's pin visible to the German side only. Two tries before it were moved out (not deleted) to `asl-retired-2026-10-26/p71-unstaged`: in the first r1 in [E4] saw [E6] and its PTC made it a seer; in the second the dice plan was a die off.
- **The keyboard:** the choice's "decline" focused, a real Enter key proposed it; Confirm focused, a real Enter committed it ("Answer a choice: Committed").
- **Widths:** at 1024 the choice's buttons inside the page (document 1024); at 320 the buttons wrap inside it (document 320, the Actions tab).
- **The hex audit** (`hex_audit.js`): `p71-check-kept-concealed` German 2 counter choices, Russian 1, none without a hex; `p71-carry-wounded` German MPh 0 (its stack had moved; the carry selects were read hex first at 4g).
- **The measurement** (section 16): level with main, back to back.
- **A slip:** one script ran on a stale tab showing `p70-perf-tw`; it changed the view only, proposed nothing, and the tab was closed.
- **Not done, as the scope allowed:** the five widths in each side's view for every control; a whole Player Turn at 320; the Replay page; the SMC crew's Stun game (above).

## 16. As built

**The pass in commits,** on `feature/asl-backlog-pass-71`, cut from 5379e06f (main, pull request 41): the frame (9e83e768, with the calibrated estimates of 302c6c71 and 48dd96ea); 4a (dbf22030); 4b (379c77dc); the strict test build for agents (c1ae622e); 4c (88f93d8c); 4d (c6952864); 4e (cc72d08a); 4f (128fed24); 4g (2dd2789d); the reviews and their rulings (6d3147b3); fix Rounds A to D (d1c18a3a, a47712b7, 34862753, efdc4dae); the whole-pass check (889d4f1e); these documents. Main's acronym work (0b5b9a1d, Codex's) was merged in at the user's word as 00fd8894, and the gate's base moved to 0b5b9a1d; it is not the pass's. Each increment was built by an agent to a brief, its diff read, its game played in the Studio by the session where one replayed, and its games added to `OWN` in `pass71.conf`.

**What the plan's entry asked, and where it went.** The plan names 11 inventory rows. After the pass 7 of them are built (1 with a deviation, A14.4) and 4 moved to the pass that takes what is left: pass 115 2 (A14.1's Entrenching, A14.21's Fortifications), pass 135 1 (A14.22's Interior Building Locations), pass 165 1 (A17.2's cavalry, with passes 170 and 175). The two tasks are built as sections 8 to 14 say, each with its departures; R71.2's moves and every departure have a row in the backlog's section 74.1, and the findings not fixed in 74.2. The three items moved in are built: Stun of an AFV by a Sniper (4a), the voluntary removal's Sniper exception (4d), and the SMC crew's Stun wound (4f). The inventory rows of other rules the pass's code touched have a note of it, and the stale notes of section 5, item 5 are corrected.

**The rulings:** R71.1 to R71.28, decided under the user's word of 2026-10-26 and open to a veto: R71.1 to R71.17 in the frame (R71.9 read with A14.4 at 4b: the counter leaves below 2); R71.18 to R71.28 at the reviews. They are in the Passes Plan's section 5.

**What a record gained** (each absent from every older record, or written only when true, and taken from the record at replay): on `sniper-attacked` the results `stunned`, `recalled`, `crew-broken`, `crew-pinned`, `immobilized`, `sniper-eliminated`, `sniper-pinned`, `forfeited`, and `concealed-kept`, with an owner-only twin; the events `san-reduced` and `sniper-checked` (with `drmCauses` since Round D); the choice kinds `sniper` (steps `forfeit`, `reposition`, `hex`, `location`, `target`, `first`, `stack-first`) and `sniper-check`, and `place` on the Sniper's resume; the `sniper` argument of `asl.game.setup`; on a fire target `woundPins`; on vehicle CC facts `afvDrm`; a `wound-severity` roll before a SMC crew's Stun; `carried` on `asl.game.move` and `movement-step`. New state properties: `SideState.SanReductions` (from `san-reduced`) and the derived `GameState.FiredOrMovedThisPlayerTurn`, computed from records every game holds and never verified.

**What a view gained, never recorded:** the Sniper Check's offer naming its takers and DRM in the answering side's view only (`SniperCheckOffer`); the reposition hexes in the Sniper side's view only (R71.19); a kept "?" read as one neutral line by the other side (R71.22).

**The tests:** `ScenarioA1Pass71RulesTests` (48 methods), `BacklogPass71Tests` (58), and `PlayPagePass71Tests` (12), each increment's and each round's run by method only (the user's rule of 2026-10-23), with 4c's card-start batches the one departure (section 10.1); the Rules, Play, Units, and MapStudio projects were not run whole, which is the gate's. The test helper `CardSnipers` places the Sniper counters every card test now owes.

**The measurement** (section 7), on `p70-perf-tw`, at 1920x1080, hand-over and "end the phase" proposal in ms:

| When | The pass | The base |
|---|---|---|
| The base, 08:41 (main-bin of 5379e06f on 6671, alone) | | hand-over 556, 421, 399, 262, 356, 427, 256, 414 (median about 406); proposal 446 (first), 167, 148, 76, 74 |
| After 4a, 09:31 to 09:42 (inconclusive) | warm: hand-over 1172 (first), then a median about 585; proposal 65 to 92; a fresh process stopped taking the scripted clicks in three runs | main's build in a fresh process: 1549, 1472 (the first two), then a median about 413 |
| After 4e, 13:05 to 13:07 (each alone, fresh) | hand-over 1073 (first), then a median about 216; proposal 112 (first), 86, 58, 60, 66 | hand-over 519 (first), then a median about 292; proposal 423 (first), 69, 91, 172, 95 |
| The whole-pass check, 16:29 to 16:42 (each alone, fresh, back to back) | hand-over 733 (first), then a median about 366; proposal 577 (first), 60, 65, 60, 63 | hand-over 830 (first), then a median about 379; proposal 135, 66, 73, 90, 85 |

Only a back-to-back pair is compared: the reading after 4a was taken in a warm process and at another width, and after 4e the pair was another session's level. Each comparable pair is level, and the stop threshold (a warm median more than 20% over the base's) was not reached.

**The time:** the frame 08:21 to 08:41 on 2026-10-26; the build 08:42 to 14:11, 5:29 by the clock, against the calibrated forecast's 5:41 and the plan's 1:20; the reviews and their rulings 14:11 to 14:23; the fixes 14:23 to 16:29; the whole-pass check and the measurement 16:29 to 16:42; then these documents. The time log has each row.

**The strict build** (`gate.sh 71 0b5b9a1d strict`) passed on 2dd2789d (14:11 to 14:14) and again on the tree after Round D (16:29 to 16:34), 0 warnings and 0 errors each time.

**The slips the notes record:** a `python -c` that rewrote a file inside a `cd` into the repository (refused by the hook, 08:39); four times written by estimate and corrected from the clock (the frame's lines, found at 08:43; 4d's Studio check, "to 12:29" for 12:24; 4f's, "to 13:47" for 13:43; the reviews' report, "14:17 to 14:21" unread), after the third of which a time enters the notes only from a `date` in the same call; CA1861 in two page tests reaching the user's Visual Studio before a strict build (10:21; `strict_test_build.sh` since); three games scripts whose dice plans were a die off (4b, 4d, the kept "?" game); `games_4f.py` placing a leader off board 3 (13:40); the agents' `cd`, heredoc, and Grep slips of section 15.1; one script on a stale Studio tab at the whole-pass check.

## 17. The gate

Not run. The user's word of 2026-10-26 ("Run Pass 71 without my intervention up to the merge gate") ends at the gate: the gate (`baseline`, `build`, `proofs`), the push of the pass's branch, its pull request, and the merge are each the user's to start. The gate's base is 0b5b9a1d (baseline 8c4014b6251b7114, 129 older games, made at 10:24 after the merge of main); its `strict` stage passed twice (section 16). `pass71.conf` names the pass's 14 recorded games (`OWN`; the measurement copy `p70-perf-tw` is not one), no older games beyond the swept game and the store's (`SWEEP_EXTRA`), `BOTH_SKIP` with `GameEvent.RulePackage`, `PRESENT="VoluntaryBreak"`, and `ABSENT="SniperCheck"`. `SKIP_JSON` is empty as committed: `woundPins` (a fire target) and `afvDrm` (vehicle CC facts) are the keys the pass writes on records of kinds that existed before, each to be checked against the store's older records (`skip_json_collisions.py`) before the proofs; the properties of event records (`carried`, `drmCauses`, `place`, `SanReductions`) and the derived `FiredOrMovedThisPlayerTurn` are for the computed `PROOF_SKIP`.

What the gate should expect to see differ, as intended changes: no Sniper exists in the store's games (design section 4), so the Sniper's targets, results, and choices reach new plans only; a card game whose setup is open now waits for each side's Sniper counter (R71.11), while a game whose setup closed before the pass gets none and its page says so once (the text list's new texts); new plans in which a DR of a kind A14.1 names but fire records did not carry (ordnance, PAATC, the Bail Out, the OVR NTCs, the morale checks outside fire) equals an enemy SAN where a Sniper counter is on the map; new plans whose hashes include the pass's null members; record lines with no raw ids (Round D). A pass 71 game holds events main cannot read (`san-reduced`, `sniper-checked`): on main's side the tool writes UNREADABLE for it and goes on, as expected.

**The swept game stays `dl-tractor-works-02`,** decided by the session after the user's "you decide": neither `-02` nor `-03` holds a `game-ended` event; `-02` reaches Turn 6 (the German APh, 1,441 events), `-03` only Turn 5 (the Russian APh, 1,476 events, last written 2026-10-25 18:16), so `-03` is not a finished game; and neither holds a Sniper, so making `-03` the swept game would cost a new baseline and test nothing of this pass. `-03` is revisited at the next pass's cut, or when the user says it is finished.
