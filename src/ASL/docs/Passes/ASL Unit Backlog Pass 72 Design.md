# ASL Unit Backlog Pass 72 Design

**Status:** The frame, written on 2026-10-27 under the user's word of that day ("Run Pass 72 without my intervention up to the merge gate."). The frame's questions were put to three read-only agents, as the standing rules ask in this mode. Their answers were judged against the pages and the code. Each decision below is a ruling decided under that word, and each is open to the user's veto.

**Date:** 2026-10-27

**Related documents:**
- the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the entry "Pass 72: Infantry V: Heat of Battle and Unit Substitution";
- the [rule inventory](<../ASL Rule Inventory A to E.md>) (10 rows give pass 72);
- the [backlog](<../ASL Unit Backlog.md>), its berserk, hero, R9.10, and R15.9 rows, and section 72.1's row "Pass 72 (task 72.1)";
- the [pass 71 design](<ASL Unit Backlog Pass 71 Design.md>), sections 2 to 7;
- the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>);
- the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>);
- the session's prompt, `Prompts/Pass 72 Prompt I.md`.

**The rulebook.** I read on their pages: A15.2 to A15.24 (p. 83), A15.42 to A15.5 (p. 84), and A19 to A19.36 (p. 86). The agents read pp. 43, 51, 52, 60, 62, 65, 83, 84, 86, 88, 102, 110, 179, 182 to 185, 195, 201, 203, 221, 224, and 391, and searched every page for "berserk", "hero", "heroic", "Ammunition Shortage", "Inexperienced", and "underscored". Each increment reads its own pages again before it builds.

**What the frame rests on.** The three agents' reports are kept in `code-builders/pass-history/pass-72-heat-of-battle/`: `frame_charge.md` (A: the charge and its return to normal), `frame_heroes.md` (B: the hero's weapons), and `frame_substitution.md` (C: Unit Substitution). The session's running record is `notes.md` in the same folder.

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

Pass 72 completes the berserk unit's charge and its return to normal (A15.43 to A15.46), the hero's weapons (A15.23), and Unit Substitution's remainder (A19.131 to A19.32):
- **the charge as the rule has it:** the next nearest Known enemy when the nearest cannot be reached (A15.45), A15.44's Battle Hardening when none can, and the Location of an enemy vehicle and enemy Infantry entered (moved in from pass 70);
- **the return to normal** after AFPh TPBF or a FT clears the Location, and after a vehicle is destroyed in CC (A15.46);
- **the hero's weapons:** the -1 with a non-MG SW, a lone hero's PSK, and the ATR as Small Arms;
- **Unit Substitution:** Ammunition Shortage (A19.131) and an SSR ELR for underscored units (A19.132) as card rules; a MG's B# lowered in Inexperienced hands (A19.32, ruling R9.10 lifted).

**What departs from the plan's entry, first:**

- **The forecast: about 5:20 to the gate, of which about 2:50 is building, against the plan's 1:40 of building (2:55 in all).** The estimates below are calibrated as pass 71's were: the frame agents' raw estimates (11:50 in all, with the Studio games) divided by 4.5 and rounded up per increment (pass 71's forecast of 5:41 against 5:29 by the clock confirmed the ratio). The rest is the reviews, their fixes, the Studio check of the whole pass, and the documents, about 2:30. The pass stays one pass (ruling R72.1), built in the order of the table below; the last increment (the hero's weapons) is the one a session's end would leave out, to pass 73.
- **Moved out of the pass** (ruling R72.2), each with a backlog row in the documents step:
  - to pass 73 (Infantry VI): a berserk Dash across a road (with 73.3; a Dash ends the MPh, a charge does not); a heroic leader's heroic qualities in fire (A15.21: `FireFirer` has no heroic fact, so he fires a MG as a leader and takes no heroic -1);
  - to pass 105: the circled B# of vehicles and Guns and the Low Ammo counter at start under Ammunition Shortage (A19.131, D3.71; with 105.7); a hero firing an AFV's AAMG as a Rider (with 105.1);
  - to pass 110: a hero firing a Gun (at most 82mm as captured, above 82mm once a Game Turn, the CA bar of C2.27; with 110.6), a bazooka, a MOL-Projector, and a RCL (no counter; 110.7 to 110.9);
  - to pass 120: Wire as 1 MF in a charge's route; minefields; to pass 150: FFE, and OBA's Scarce Ammo under Ammunition Shortage (p. 391); to pass 185: a Blaze as a charge bar;
  - to the backlog with no pass: the ordnance package's Inexperienced B# read by class only (A19.3's Green-with-leader exemption missed there).
- **Smaller than the rows say:** A15.432's "not verified" items are built (frame A, section 4); the Gun crew's Location is no longer a case that ends in place (R70.27); DC Thrown only is built; A19.31's conveyance exception is built (passes 40 and 65). These are document corrections (section 5).

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 5a | 72.1 (I) | The charge: one search for every target, the next nearest (A15.45), A15.44's Battle Hardening at the MPh's end, the vehicle-and-Infantry Location entered, the mid-move PP read, the dead code and the stale comment. | 0:55 |
| 5b | 72.1 (II) | The return to normal after AFPh TPBF or a FT, and after a vehicle destroyed in CC (A15.46). | 0:20 |
| 5c | 72.2 (I) | The two SSR tokens; A19.132's SSR ELR; A19.32's MG B# behind the pass's stamp. | 0:30 |
| 5d | 72.2 (II) | Ammunition Shortage: the Original 12's Unit Replacement, the B#, X#, and Depletion Numbers one lower, no Fire Lanes. | 0:40 |
| 5e | 72.1 (III) | The hero's weapons: the -1 with a non-MG SW, a lone hero's PSK, the ATR as Small Arms. | 0:25 |
| | Overhead | The reviews, their fixes, the Studio check of the whole pass, the documents. | 2:30 |
| | **Forecast** | build 2:50 | **about 5:20** |

## 3. The rows, by task and increment

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 72.1 | A15.23 (83) | A hero's non-MG SW, two-man SW, Guns, CA, AAMG as Rider | 5e; Guns to 110, AAMG to 105 |
| 72.1 | A15.43 (84) | The charge the model cannot decide | 5a |
| 72.1 | A15.431 (84) | Mid-move berserk, Dash, Wire, DC Thrown | 5a (mid move); Dash to 73, Wire to 120; DC built |
| 72.1 | A15.432 (84) | The DEFENDER's FPF, halved AFPh TPBF, berserk fire | built (documents); 5a's stale comment |
| 72.1 | A15.45 (84) | The next nearest; A15.44 when none; Blaze, minefields, FFE, Wire | 5a; Blaze to 185, the rest to 120 and 150 |
| 72.1 | A15.46 (84) | The return after AFPh TPBF, FT, a vehicle in CC | 5b |
| 72.2 | A19.131 (86) | Ammunition Shortage | 5c (token), 5d; circled B# to 105 |
| 72.2 | A19.132 (86) | An SSR ELR of 4 or less for underscored units | 5c |
| 72.2 | A19.31 (86) | The conveyance exception | built (documents) |
| 72.2 | A19.32 (86) | A MG's B# in Inexperienced hands (R9.10) | 5c |

The item moved in from pass 70 (a charge into a vehicle-and-Infantry Location) is placed with 5a.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **The charge's search** (frame A, section 1): `SearchCharge` (GamePlanner.CloseCombat.cs:1502) hands the Known enemy Locations to `ScenarioA1ChargeCalculator.Steps`, which takes only the nearest group in LOS and runs one Dijkstra per target. When no route reaches them (a cliff, water, a slope, unreviewed terrain, marsh from below, a route boxed in by other enemy Locations), the steps are empty and the charge ends in place under R30.5. A15.45 sends it to the next nearest instead.
- **The vehicle-and-Infantry Location** is refused only by `ScenarioA1CloseCombatRules.ChargeBarred` (line 297). Pass 70 built what follows the entry: the MPh admits an all-berserk stack (ScenarioA1MovementCalculator.cs:273), A8.312's FPF is generic, and a berserk unit there owes its attack (`OwingAttack`).
- **A15.44 strips berserk status** where it applies to a unit already berserk (E1.533, p. 224). Battle Hardening has a built path: `ScenarioA1FireReference.HardenedOf` and the rally path's lineage event (Rally.cs:275 to 292); it can be refused (A15.3, R5.9; the fire path's `battleHardening:<key>` choice).
- **A15.46 is built only for CC** (the CC calculator's `BerserkEnded`) and for the end of a charge with no Known enemy in LOS (`BerserkReturns`, GamePlanner.cs:858).
- **The hero:** the ordnance package takes any Personnel firer, a hero included (`ScenarioA1OrdnanceCalculator.cs:439-447`): today a hero fires a light mortar, a PF, and an ATR at a vehicle with no -1; a PSK is refused for a leader and a hero (:442); an ATR as Small Arms is refused for a hero (`ScenarioA1FireCalculator.cs:753`). Backlog row 347 says otherwise.
- **SSRs are tokens** (`ScenarioCardRule.Tokens`, `GameState.SpecialRules`); pass 70's `fanatic:` token is parsed in Rules and checked on the card and for a new game. No built-in card and no proof game holds any token.
- **R15.9 and R9.10** are each one read: `ElrOf` (FireCalculator :4695-4706) and `WeaponEffects` (:4214-4220). The fire verifier recomputes a record from its facts and dice and refuses one that differs (LiveFire.cs:1048-1335).
- **The proof store:** two games hold a berserk unit (`dl-tractor-works-01`, eliminated before its MPh; `-02`, `r-squad-10`, whose charge at revision 1317 was decided, one step into [X3]); none passes a case 5a or 5b changes. Two heroes, in the Tractor Works games; `g-hero-1` fires a captured MMG, a path 5e does not change. One Inexperienced MG fire: `dl-tractor-works-01` revision 1267, a Conscript squad's LMG, recorded B11, Original DR 10 (section 6, F9). No Original 12 IFT DR by Inherent FP. No special rule.

**Needs a read at its increment.**

- 5a: the reason a route is refused (`IChargeFactReader.EntryCost`); where the MPh end can offer the owner's Battle Hardening choice; whether the CC calculator's A15.46 check counts a surviving vehicle.
- 5b: the fire commit's events after the effects (GamePlanner.Fire.cs near 1100 to 1130); whether the Fire package lets a FT attack its own Location (A22).
- 5c: `ScenarioA1GameStart.FanaticToken` and `RuleDiagnostics`; pass 45's stamp (`CoweringMarksAndWeaponResidual`).
- 5d: `AddFireEvents`, the projector's `FirerEffects`, `LiveFire.Rolls`, and the ordnance verifier.
- 5e: the ordnance To Hit DRM in full; whether a hero firing a light mortar forfeits his inherent FP in the record.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and the absent-safety of each):
- the Battle Hardening of A15.44 as a planner-written event, and its refusal as a choice (5a); the returns to normal as `conditions-changed` (5b): no fact derived in the projector from an older record;
- on a fire attack: the pass's stamp, `ammunitionShortage`, and on a target and FPF firers `underscoredElr` (5c, 5d); a new roll kind for the Random Selection of A19.131 (5d); on an ordnance shot, the Ammunition Shortage and the hero's -1 (5d, 5e).
Each to be checked against the store's older records before it enters `SKIP_JSON` (`skip_json_collisions.py`).

## 5. What the frame found outside the rows, and corrections

1. **Stale rows,** corrected in the documents step: the Gun crew's Location in backlog rows 595 and 940 and the inventory's A15.43 (lifted by R70.27); the inventory's A15.431 (DC Thrown only is built); A15.432's "not verified" items (built); A19.31's conveyance exception (built); backlog row 347 (what a hero is refused).
2. **Dead code:** `GamePlanner.ChargeBarred(GameState, ...)` (CloseCombat.cs:903) with a `state.At` scan; removed in 5a.
3. **An eager per-unit read:** GamePlanner.cs:859 reads `state.Location(unit.Id)` for every unit at the MPh's end; read through `LocationOf` in 5a.
4. **A stale comment:** GamePlanner.Fire.cs:274, "berserk fire ... is not reviewed"; removed in 5a.
5. **A heroic leader in fire** (A15.21): to pass 73 (R72.2).
6. **The ATR's Inexperienced -1** reads class only and misses A19.3's Green-with-leader exemption; fixed behind the stamp in 5c.
7. **Corrections to the prompt:** its proposal that the Gun crew's Location may stay "ends in place" is moot (R70.27). Its item "a hero's other weapons sit on other passes' code" stands, but the hero already fires three of them without the -1.

## 6. The frame's questions, each with its ruling

**F1. Is the pass kept whole?** The forecast is about 5:20 to the gate (section 2).
- **R72.1:** the pass stays one pass. The increments run in the table's order; whatever a session's end leaves out is the last increment's (5e, the hero's weapons), and it goes to pass 73 with its row.

**F2. What leaves the pass?**
- **R72.2:** the moves of section 2, each with a backlog row in the documents step.

**F3. The next nearest Known enemy (A15.43, A15.45).**
- **R72.3:** `Steps` runs one single-source search with every Known enemy Location terminal, and reads the targets off it in groups by range in hexes: the nearest group first; when no route reaches any of it, the next group, and so on. A15.45's exception is read so: the charge stays on a nearest unit whose route counts, in hexes stepped, no more than the next nearest group's range; a tie stays with the nearest. The answer stays under PR 32's memo key; a state that falls back makes one search, not more. A distance the map cannot read keeps R30.5.

**F4. When no Known enemy can be reached (A15.44, E1.533).**
- **R72.4:** a berserk unit whose charge reaches no Known enemy in LOS, where every refused entry on its search's frontier is a rule bar (a cliff, a water terrain), stays in place, and at the MPh's end loses berserk status and is Battle Hardened (A15.45, A15.44, E1.533). Its owner may refuse the Battle Hardening (A15.3, R5.9) by a pending choice of his side, kind `battle-hardening`; refused, the unit is simply Good Order. A refusal on the frontier that is a gap of the model ("not reviewed", R10.1) keeps R30.5: the unit ends in place and stays berserk. The change is a planner-written event, never derived in the projector.

**F5. The vehicle-and-Infantry Location (moved in from pass 70).**
- **R72.5:** the charge enters a Location holding an enemy vehicle and enemy Infantry like any other; `ChargeBarred`'s refusal and its text go. A8.312's FPF and pass 70's owed attack follow unchanged. A15.46's return after CC there needs every Known enemy unit eliminated, the vehicle included (read at 5a).

**F6. A unit made berserk in mid move (A15.431).**
- **R72.6:** built in substance (eight MF less those spent). A unit made berserk this MPh abandons its SW by A15.431's PP limits at once, not only 1 PP ones; one Play test pins the remaining MF.

**F7. The return to normal (A15.46).**
- **R72.7:** a berserk unit returns to Good Order when it, or the group it attacks with, eliminates every Known enemy unit in its Location (at least one) with TPBF in the AFPh or with a FT in the AFPh, or with CC that destroys an enemy vehicle there and leaves no other Known enemy. Decided in Rules on facts of the fire or the CC; written by Play as `conditions-changed` for each berserk member after the attack's effects. A FT attack on its own Location is built only if the Fire package already allows it (5b reads A22).

**F8. The hero's weapons (A15.23, A15.24, C13.25).**
- **R72.8:** a hero firing a non-MG SW takes -1: on the To Hit DR of an ordnance SW (light mortar, PF, PIAT, ATR at a vehicle), never again on the HE's IFT after a hit, never on a To Kill (C13.25); an ATR fired as Small Arms is allowed and takes A15.24's existing heroic -1 and no second one. A lone hero may fire a PSK: +1 for one man firing a two-man SW, negated by his heroic -1 (C13.45, C13.48). The DRM is named `hero:<id>` in the arithmetic and labeled in the record lines. Guns, the AAMG as a Rider, the bazooka, the RCL, and the MOL-Projector leave (R72.2).

**F9. A MG's B# in Inexperienced hands (A19.32, R9.10).**
- **R72.9:** R9.10 is lifted for attacks recorded with the pass's stamp: any SW used by Inexperienced Personnel has its B# (and X#) one lower, a MG included. Inexperienced is read from the firer's `Inexperienced` fact (A19.3: a Green MMC stacked with an unbroken leader is not), or by Conscript class where an older record lacks it. The ATR's existing -1 takes the same read. A record without the stamp replays as recorded: `dl-tractor-works-01`'s Conscript LMG at revision 1267 (B11, DR 10) stays a hit, as the verifier would otherwise refuse it.

**F10. SSRs as card tokens (A19.131, A19.132).**
- **R72.10:** two tokens on the pattern of `fanatic:`: `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>` with n from 0 to 4. Rules parses them; the card's diagnostics and a new game's `RulesBar` refuse a malformed one; the planner hands Rules a fact that is null when the token is absent. One small test card for each, for the Studio games. Both are public.

**F11. Ammunition Shortage's Unit Replacement (A19.131).**
- **R72.11:** when a side's on-board OB has Ammunition Shortage, an attack with an Original 12 IFT DR in which any MMC of that side used its Inherent FP Replaces one such MMC after the attack is resolved, chosen by Random Selection among them (A.9) after any SW malfunction or elimination; the Replaced unit keeps its broken or unbroken status (only the MMC "incapable of further quality loss" is broken, and a berserk one is not); an underscored squad is Replaced by its two HS, an underscored HS and a crew are incapable and break. No effect on CC, Sniper, or Residual FP attacks, nor on a To Hit DR of 12. The Random Selection dr and the Replacement are public events.
- **R72.16** (decided at 5d): on a tie for the highest Random Selection dr every tied MMC is Replaced, as A.9 says ("all of the applicable units which rolled that number are affected equally"); R72.11's "one such MMC" is read so.

**F12. Ammunition Shortage's numbers (A19.131, A.11, A21.11).**
- **R72.12:** under Ammunition Shortage every SW's Original B# and X# is one lower (not a DC, not a captured SW, A21.11), in IFT fire and as ordnance, and a lowered B# becomes an X# by A.11 as the existing reduction does; Ammunition Depletion Numbers are one lower; no Fire Lane may be declared. The fact is recorded on the attack or shot so the verifier checks it against the card, not taken as recorded like Extreme Winter's reduction. Vehicles' and Guns' circled B# go to pass 105 (R72.2).

**F13. An SSR ELR for underscored units (A19.132, R15.9).**
- **R72.13:** a target, and an FPF firer, carry an `underscoredElr` fact from the token; `ElrOf` reads it in place of R15.9's 5, and a failure beyond it Replaces the unit by the normal table. The German 8-3-8 has no entry: it is Replaced by the 4-3-6 Conscript, the only lower-Class German MMC none of whose factors exceeds the 8-3-8's (A19.13: none may increase, the Class must decrease; the 4-4-7 and 4-6-7 have a longer range). The Finnish 8-3-8 stays as R15.13 has it. R15.9 still holds where no token is given.
- **R72.15** (decided at 5c): with an SSR ELR, an underscored unit with no table entry but a HS of R19.7 is Replaced by that HS (the German 3-3-8 by the 2-3-7: lesser Class, no factor higher, A19.13); only one with neither is Disrupted (A19.12).

**F14. A19.31.**
- **R72.14:** built; verified and recorded only, with the per-unit `state.At` of `Experience.Inexperienced` read through the index if 5c touches it.

## 7. Memory and performance

**The checklist** (the guidelines' section "Design and review checklist").

- **Work added per state:** the charge's single search replaces one search per target (R72.3), under PR 32's `ChargeKey` in `PlayStepMemo.Charges`; the SSR facts read once per state from `GameState.SpecialRules`.
- **Per unit and per hex:** none outside the memo. The MPh end's eager `state.Location` read and the dead `ChargeBarred(GameState, ...)` go (section 5).
- **Per step:** A15.46's verdict reads only the attack's own Location; A19.131's Replacement runs only on an Original 12 with the fact set.
- **Per render:** the Battle Hardening choice uses pass 70's pending-choice panel; no new layer.
- **Ownership and keys:** unchanged; the memo stores are owned by `PlayStepMemo` (state lifetime).

**The measurement** (stated before building, as the user asked on 2026-10-19).
- **Workload:** `p70-perf-tw` (`dl-tractor-works-02` cut at revision 1316, the Russian Turn 5 MPh with `r-squad-10` to charge; the user's game untouched): the hand-over (German to Russian), an "end the phase" proposal, and the Movement panel's charge for `r-squad-10`, five runs each.
- **Baseline:** the gate's `main-bin` build of b0a36132 on port 6671, measured alone; then the pass's Studio on 6670, alone.
- **Expected:** the hand-over and the proposal level with the base; the charge level or faster (one search in place of one per target).
- **Stop threshold:** a warm median more than 20% over the base's stops the next increment until a trace finds the cause.
- **When:** after 5a in the Studio first, before any costly test; again at the whole-pass check.

**Build outputs outside the repository.** My Studio in `asl-gate-work/studio-6670` (registered by `build_studio.sh`); the gate's own (the base build, the proof tools, registered by `gate.sh`); the filtered test runs' `asl-gate-work/test-classes-bin/<project>` (registered by `run_test_classes.sh`); any strict build of a test project by an agent, registered as it is made. All are removed at the latest by the gate's `ci` stage; the last task confirms `outside_outputs.sh list` is empty.

## 8. The charge: 72.1, part I (increment 5a)

Built on 2026-10-27, 19:10 to about 20:10, by a building agent to a brief (`brief_5a.md`), rulings R72.3 to R72.6, its diff read and its games played in the Studio by the session, one round of fixes after the Studio check.

### 8.1 Departures from the frame, first

- **A15.45's comparison applies only where a rule bar refused a step cheaper than the target's route.** Everywhere else A15.43 stands unchanged (the nearest target, the shortest route in MF), so an ordinary charge charges as before. R72.3's comparison itself is as written: the charge stays while the route's hex count is no more than the next group's range; a tie stays. **Wrong as built, found at the reviews** (R1's finding 1): the bar test counted building connections and the first refused step anywhere in the explored map, not on the route, so in built-up terrain the comparison applied to nearly every charge and ordinary charges changed. Ruling R72.17 (section 13) narrowed the bars to A15.45's terrain, compares only where a bar lies across the route, makes "still closer" strict, and was built at fix Round A.
- **What a rule bar is:** a cliff, a Water Obstacle terrain (Water, Shallow Water, River, Shallow River, Canal, Ocean, Shallow Ocean, a Pond), and building Locations that do not connect (level, stairwell, Rooftop, Cellar); since R72.17 the building connections are neither bar nor gap. Everything else is a gap of the model, including marsh from below, slopes, unreviewed hexsides, and bridged hexes. **Two cases are read as gaps that are bars on the page:** a pond hexside around a hex with no water at its center, and a bridged hex (the movement model reads no bridge). Each keeps R30.5 (ends in place, stays berserk): a backlog row.
- **The Battle Hardening choice** reuses the kind `battle-hardening` with its own key, `berserkHardening:<unit>`, apart from the fire and rally keys; it is offered in the plan that ends the MPh, after `phase-changed` (so the unit, no longer berserk, cannot move again), one unit at a time. The answer is not recorded among a fire or rally record's choices.
- **A15.46 with a surviving vehicle** (R72.5's read): the CC calculator did not count a Known enemy vehicle; a new fact on the CC facts, `vehicleSides`, keeps a berserk unit berserk while one survives. A record without it resolves as before.
- **The page's undecided-charge line,** older than the pass, named raw ids, raw Locations, and the text's code; it is reworded through the page's `Words` ("4-4-7 squad R1 in [A8] is berserk and stays in place: ..."), and the MPh-end line names each unit's hex (found in the Studio check). No page test asserts the line: no MapStudio fixture gives an undecided charge.

### 8.2 What is built

- `ScenarioA1ChargeCalculator`: one single-source search with every enemy Location (and the charge's previous target) terminal, tracking each route's hex count and the cost at which a bar or a gap was first met; `Steps` reads the targets in range groups; `RuleBar`, `NoneReachable`, `HardeningDue`, `Hardening`, and their texts. `ChargeEntryFacts` (half MF, refusal, terrain) from `IChargeFactReader.EntryCost`.
- `ScenarioA1CloseCombatRules.ChargeBarred`'s refusal removed, with `ChargeOccupantFacts` and `IChargeFactReader.Occupants`.
- The MPh's end (GamePlanner.cs): the hardening list, the choice after the phase change, locations read through `LocationOf`. The choice's answer (GamePlanner.Choices.cs, GameGate, the projector): `conditions-changed` (berserk off), then `lineage` Replaced, or Fanatic for an elite unit, as the hardening path does; declined, the unit is Good Order.
- `CloseCombatFacts.VehicleSides` (`vehicleSides`), written by `LiveCloseCombat` for a sequential round, read by the A15.46 check; the verifier takes an older record's absence as recorded.
- `MfAllotment` counts a berserk unit's 1 PP SW only up to its IPC; the abandonment is read at every step (R72.6).
- The dead `GamePlanner.ChargeBarred(GameState, ...)` deleted; the stale comment at GamePlanner.Fire.cs:274 rewritten.

### 8.3 The games and the tests

- `p72-charge-next-nearest` (board 7, Russian MPh): a berserk 4-4-7 in [A8]; a German squad in [A3] at range 5 across the river, in LOS; another in [G8] at range 6, in LOS, reached by land. The panel: "4-4-7 squad R1 in [A8] is berserk and charges [G8] ... next step [A9]". (The first layout, from a terrain approximation, put the far target where [A3] cannot see it; the LosCalculator was read for the second.)
- `p72-charge-unreachable` (board 7): the only German squad across the river. The MPh's end offers the Russian side "take it" or "decline it"; taken: berserk off and the squad Replaced by the elite squad.
- `p72-charge-tank-and-squad` (board 3): a German tank and squad in [A2]; the charge enters [A2] and the German side may fire.
- Tests, by method (the agent's report): Rules 26 and Play 26 methods passed (`ScenarioA1Pass72RulesTests`, `BacklogPass72Tests`, and the charge, A15.46, portage, and Battle Hardening methods of passes 5, 10, 15, 27, and 70 that assert the changed members and texts); after the wording fix, 2 more. `ChargeSearches` stays 1 on a fall back.
- The measurement after 5a: level with the base (section 7; the notes).

## 9. The return to normal: 72.1, part II (increment 5b)

Built on 2026-10-27, about 20:12 to 20:47, by a building agent to a brief (`brief_5b.md`), ruling R72.7, its diff read and its games played in the Studio by the session; one game rewritten after the Studio check.

### 9.1 Departures from the frame, first

- **TPBF in the firer's own Location was refused in Play** (frame A had read it as built: it was, in Rules only). `LiveFire.FromState` listed every unit in the target Location as a target, the firer included, and the Fire package refused the attack ("unit listed twice"). `ScenarioA1FireEligibility.SelectTargets` gains an optional `tpbfSide`: outside the MPh's moving stack, when a firer on foot stands in the target Location, only the other side's units there are attacked (A7.21, A7.212). Every such attack was refused before the pass, so no record holds one. **For the referee's review:** where else this opens a fire that was refused (a Location shared after CC without Melee, a vehicle's Location).
- **A FT in its own Location is built** (A22.1 lets a FT fire TPBF; the Fire package already handled it), so no backlog row.
- **The verdict reads whether the firer stands in the target Location;** the record says "by its TPBF or FT in the AFPh". CC needs no phase (A15.46: "or CC"; CC Reaction Fire in the MPh counts).
- **Left conservative:** a PRC killed with its vehicle by fire is not subtracted (the unit stays berserk); Riders and Passengers left after a CC wreck count as Known enemies.
- **The second game** first had the berserk squad advance into the tank's Location in the APh, which A15.431 forbids; it now starts in the CCPh with both in [A2], on the German side.

### 9.2 What is built

- Rules: `BerserkAttackFacts`, `KnownEnemyCount`, `ScenarioA1SequenceCalculator.BerserkReturnsAfterAttack` (CC that destroyed a vehicle, or AFPh fire from inside the target Location, with a Known enemy before and none after; the Location read only when the attack can qualify), `BerserkClearedText`; `SelectTargets`'s `tpbfSide`.
- Play: `BerserkReturnsAfterFire` at the end of `AddFireEvents` (resumed attacks included) and `BerserkReturnsAfterVehicleKill` in vehicle CC (the Location read with the vehicle gone, as `NextAfter` does): `conditions-changed` (berserk off), caused by the fire or the CC record. No new event, key, or roll.
- MapStudio: `PlayRecords` words a berserk-off change caused by a fire or a vehicle CC record.

### 9.3 The games and the tests

- `p72-berserk-tpbf-return` (board 3, Russian AFPh): a berserk 4-4-7 and a German HS in [A2]; dice 1 1: "4 FP x 3 (TPBF) x 0.5 (Advancing Fire) = 6", 1KIA, then "4-4-7 squad R1 in [A2] eliminated every Known enemy unit in its Location by its TPBF or FT in the AFPh, so it is no longer berserk and returns to Good Order (A15.46)".
- `p72-berserk-vehicle-cc-return` (board 3, German CCPh): a berserk 4-6-7 and a lone T-34 in [A2]; "Infantry attack the vehicle", dice 1 2 against Kill Number 5: wrecked, and the same return line by destroying the vehicle in CC.
- Tests, by method (the agent's report): Rules 25 (including, by a slip, one whole class, `ScenarioA1SequenceRulesTests`); Play 15 and 1 more (the pass's class and five shared-Location fire methods for the targets change). Strict builds exit 0.

## 10. The SSR tokens, A19.132, and A19.32: 72.2, part I (increment 5c)

Built on 2026-10-27, about 21:00 to 21:23, by a building agent to a brief (`brief_5c.md`), rulings R72.9, R72.10, R72.13, R72.14, its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

- **The German 3-3-8 under an SSR ELR becomes the 2-3-7** (the agent's decision, now ruling R72.15 below): with the token, Replacement goes through `ScenarioA1FireReference.UnderscoredReplacementOf`, which is `ReplacementOf` or, where that has nothing, the HS of R19.7 (`CasualtyHalfSquadOf`). A19.13 allows it (lesser Class, no factor higher), so A19.12's Disruption does not apply. No table row was added: `ScenarioA1Pass19bTests.TheTablesKnowTheCardSquads` asserts `ReplacementOf(3-3-8)` is null.
- **A19.32's -1 is applied to every SW but the FT** under the stamp. **For the referee's review:** A19.32 reads "any SW"; whether a FT has a B# or X# the -1 can reach.
- **A PRC (a Passenger 8-3-8) keeps R15.9:** `FirePrc` gets no `underscoredElr`. A backlog row.
- **Morale checks carry no stamp:** their verifier checks `underscoredElr` against the state only when the record carries it.
- **Unverified before the gate:** the ten new token rows of `BacklogPass17Tests.TheSsrTokensAreCheckedAsANewGameChecksThem` and `ScenarioA1Pass19bTests.EveryPackageKnowsTheCardCounters` walk up to the repository's root, which the filtered runs (built outside the repository) do not have. The gate's suite runs them.

### 10.2 What is built

- `Rules/ScenarioA1UnitSubstitution.cs`: the tokens `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>` (n 0 to 4), their readers (`AmmunitionShortage(rules, side)`, `UnderscoredElr(rules, side, group, definition)`: underscored MMC only, a group's token before the side's), `RulesBar`; called from `ScenarioA1NightAndWeather.RulesBar` and the card's `RuleDiagnostics` (`card.ssr:`).
- New facts: `FireAttack.UnitSubstitution` (`unitSubstitution`, the pass's stamp, beside pass 45's); `FireTarget.UnderscoredElr` and `FireFirer.UnderscoredElr` (`underscoredElr`), copied to FPF firers.
- `ElrOf` returns `UnderscoredElr ?? 5` for an underscored MMC; `Replace` takes the table path when it is set; the German 8-3-8 is Replaced by the 4-3-6 (a new row). `AtrInexperience` became `Inexperience`: with the stamp, every SW but the FT is one lower in Inexperienced hands (the `Inexperienced` fact, or Conscript class where it is absent); without it, R9.10's ATR-only read by class.
- Play: `LiveFire.UnderscoredElr(state, unit)`; the verifier takes the stamp from the record and the SSR ELR from the state only on a stamped record.

### 10.3 The games and the tests

- `p72-underscored-elr` (board 3, `underscored-elr:german:3`, Russian PFPh): two 4-4-7 in [A3] fire at a German 8-3-8 in [A2]; dice 3 4, then 5 5: "2MC; 8-3-8 squad in [A2]: now 4-3-6 squad, broken".
- `p72-inexperienced-mg` (board 3, Russian PFPh): a Conscript 4-2-6 with a LMG in [A3] fires the LMG at [A1]; dice 4 6: recorded `breakdownNumber` 10, `malfunctioned` true; the records: "The LMG malfunctioned (A9.7)" (the line does not cite A19.32: for the fixes).
- Tests, by method (the agent's report): Rules 17 methods (two of its own rows fixed and rerun, 7 passed); Play 13 methods, all but the ten Pass17 rows passed. Strict builds exit 0.

## 11. Ammunition Shortage: 72.2, part II (increment 5d)

Built on 2026-10-27, about 21:27 to 22:00, by a building agent to a brief (`brief_5d.md`), rulings R72.11, R72.12, R72.16, its diff read and its games played in the Studio by the session.

### 11.1 Departures from the frame, first

- **A tie Replaces every tied MMC** (A.9; ruling R72.16).
- **The X# under the Shortage** is the printed B# less Extreme Winter's reduction less 1, as the existing reduction does, so a B11 LMG is B10 and X10; the Inexperienced and captured reductions keep the X# at the printed B#. **For the referee:** the two are not consistent.
- **Ordnance takes the B# only:** the ordnance package has no X# removal (no removal on a To Hit 12). A backlog row. The SW's ordnance B# is one lower (a light mortar, an ATR or PSK, a SW MG at an AFV; not a PF, a vehicle's MA, or a captured SW).
- **Who can be Replaced:** squads, HS, and crews; an underscored HS is incapable and breaks; an FPF firer its own NMC eliminated, Reduced, or Replaced is skipped, one it only broke is Replaced broken.
- **Spraying Fire is read once,** on the second Location's part after both.
- **No Shortage** on vehicle fire, Residual FP, a DC, or the HE hit's IFT (none has firers).
- **A FT's removal number takes the -1** (A19.131 excepts only the DC; for the referee with 5c's FT question). A captured MG's Fire Lane is not refused (A21.11).
- **An ordnance shot's fact is checked against the state with no stamp:** a shot recorded between 5c and 5d in a game holding the token would be refused (session test games only).
- **Not unit-tested:** the ordnance SW's B#.

### 11.2 What is built

- Rules: `FireAttack.AmmunitionShortage` (`ammunitionShortage`), `FireRolls.ShortageSelection` (`shortageSelection`, a dr of purpose `fire-shortage-selection`, no Sniper trigger), `FireResolution.ShortageEffects` (`shortageEffects`); `ShortageReplacements` after the weapon effects and the FPF checks; `Shortage(weapon)`; the Fire Lane refusal; `OrdnanceShot.AmmunitionShortage`; the Depletion Numbers one lower with their words.
- Play: the facts from the state (the fire verifier only on a stamped record); the new roll; the shortage effects written after the MG events (the Replacing unit keeps the fire counter); `BerserkReturnsAfterFire` leaves Replaced firers out.
- MapStudio: the record lines (shown to the firing side even where its fire record is withheld), the roll's word, the Depletion words.

### 11.3 The games and the tests

- `p72-ammo-shortage-replacement` (board 3, `ammo-shortage:russian`, Russian PFPh): two 4-4-7 in [A3], one with a LMG, fire at [A2]; dice 6 6, 1 1, 2 5: "4-4-7 squad R2 in [A3]: the Original 12 under Ammunition Shortage (Random Selection dr 5, A.9): Replaced by 4-2-6 squad (A19.131)".
- `p72-ammo-shortage-mmg`: an MMG at its lowered B# (the same read as 5c's game; not played again).
- `p72-ammo-shortage-fire-lane` (German MPh): after the German move to [A2], the Russian LMG's Defensive First Fire with a Fire Lane to [A1] is refused: "The firing side's OB has Ammunition Shortages, which prevent placement of Fire Lanes; its MG may fire without one (A19.131, A9.22)".
- Tests, by method (the agent's report): Rules 23 methods (one test of its own fixed and rerun); Play 8. Strict builds exit 0.

## 12. The hero's weapons: 72.1, part III (increment 5e)

Built on 2026-10-27, about 22:04 to 22:23, by a building agent to a brief (`brief_5e.md`), ruling R72.8, its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

- **A new optional fact guards the -1 and the PSK:** `OrdnanceShot.HeroWeapons` (`heroWeapons`), since an ordnance shot carries no `unitSubstitution` stamp. A shot recorded before the pass has none and replays as resolved; a lone hero's PSK is lifted only with the fact.
- **A hero's light mortar forfeits his inherent FP already** (the projector's `GunCrewsFired`); one gap is left: A7.353's crew exception still lets him add halved inherent FP later in the same phase (Subsequent First Fire, FPF, Final Fire adjacent), where A15.23 forfeits it for the phase. A backlog row (it needs the phase of the SW's use as state).
- **A MG fired as ordnance at an AFV by a hero** (A9.61) keeps no hero DRM. A backlog row.
- **Texts for the fixes:** the PSK is named "The PSK -" in the proposal and record; the proposal's DRM list reads "hero:1-4-9 hero G1 -1" (the record's own line is worded: "- 1 (hero's SW, 1-4-9 hero G1, A15.23)"). `ordnance.crew-outside` does not say a lone leader may not fire a PSK.

### 12.2 What is built

- Rules: `ScenarioA1OrdnanceCalculator.HeroDrm` (a hero crew, the fact, no vehicle, a light mortar or LATW): `hero:<id>` -1 (A15.23), and `hero-two-man:<id>` +1 for a PSK (C13.45, C13.48), on the To Hit DRM only, in the LATW, Area (mortar), and Armor (at a vehicle) paths; the To Kill and a hit's IFT take none. The PSK's crew check refuses a leader, and a hero only without the fact. The fire calculator no longer refuses a hero's ATR: A15.24's `heroic:` -1 applies within its range, and `hero-mg` stays MG-only.
- Play: `LiveOrdnance.FromState` sets the fact for a hero crew; the verifier keeps the state's value only when the record carries it. `RefusalReasons`' `fire.weapon-outside`: "a hero fires one MG, ATR, or FT".
- MapStudio: the labels "hero's SW, {0}" and "hero alone with a two-man SW, {0}".

### 12.3 The games and the tests

- `p72-hero-psk` (board 3, German PFPh): a lone German hero's PSK at a T-34 in [A1]; dice 2 3, 6 6: "DR 2, 3 = 5 - 1 (hero's SW, A15.23) + 1 (hero alone with a two-man SW, C13.45) = Final DR 5: hit ... TK DR 6, 6 = 12: a dud".
- `p72-hero-atr` (Russian PFPh): a Russian hero's ATR as Small Arms at [A1]: "1 FP ... DRM - 1 (heroic, A15.24)"; no `hero-mg`.
- `p72-hero-mortar` (German PFPh): a German hero's 50mm mortar at [A1]: "DR 2, 3 = 5 - 1 (hero's SW, A15.23) = Final DR 4: hit"; the IFT DR 6, 5 with no hero DRM.
- Tests, by method (the agent's report): Rules 8, Play 9 (the hero, PSK, ATR, mortar, and PF methods of passes 9, 9b, 15). Strict builds exit 0.

## 13. The four reviews, their fixes, and the Studio check

Four read-only reviews ran in parallel on db15561b (2026-10-27, 22:26 to about 22:40): R1, the referee for Heat of Battle; R2, the referee for Unit Substitution; R3, the table player; R4, the UI, the Rules boundary, and memory and performance. Their reports are kept in the pass's folder (`review_r1_heat_of_battle.md` to `review_r4_ui_boundary.md`); the fix brief is `brief_fixes.md`. `gate.sh 72 b0a36132 strict` ran beside them and passed (0 warnings, 0 errors; the formatter changed nothing).

**Decided at the reviews (2026-10-27, under the same word; open to a veto):**
- **R72.17** (amends R72.3 and R72.4; R1's findings 1 to 5, R3's 7): a rule bar is A15.45's terrain only, a cliff or an unbridged Water Obstacle; a building connection is ordinary movement the shortest route already prices. A15.45's comparison applies to a nearest target only when a bar lies across its would-be route (the route that ignores bars is cheaper than the real one), against the range of the next group that has a reachable target; with none, the charge stays on the nearest. "Still closer" is strict: at a tie the targets are equidistant and the ATTACKER chooses (A15.43). A15.44's verdict needs a Known enemy unit in LOS, none reachable, and only bars keeping them out of reach; a target cut off only by other enemy Locations, or a gap, keeps R30.5; a unit with no Known enemy in LOS returns to normal (A15.431) and is never hardened. As built in 5a, the bar test counted building connections and any refused step on the map, so ordinary charges in built-up terrain changed, against 5a's own claim (section 8.1).
- **R72.18** (amends R72.12; R2's 1, R3's 2): under Ammunition Shortage a SW's B# is one lower and its Original B# becomes its X# (A.11 and its example, p. 43): a B11 LMG is B10 and X11; only a printed X# is itself lowered. Depletion Numbers are not lowered for a captured Gun (A21.11).
- **R72.19** (R2's 4): a morale check record carries the stamp `unitSubstitution`, and with it its `underscoredElr` is checked against the state.
- **R72.20** (R1's 8 and 10): a destroyed vehicle's PRC are eliminated with it (A11.5, p. 74) and are not Known enemies left for A15.46; CC's A15.46 counts Known enemy units only, as fire does.
- **R72.21** (R1's 7): a vehicle's fire at range 0 into its own Location outside the MPh is TPBF (A7.211, p. 55) and stays allowed.
- **R72.22** (R2's 6): A19.132 reaches ELR failures only; the Shortage's Replacement keeps A19.13's EXC.
- R2's finding 2 settles 5c's question: the FT takes A19.32's -1 through `AssaultWeaponRemoval` (R15.1), so leaving it out of `Inexperience` avoids a double count.

In sum: no BLOCKER, 5 MAJOR (R1 2, R2 1, R3 2), 18 MINOR, and 18 NOTE. R2's finding 1 and R3's finding 2 are one fault (the X# under the Shortage). No review found an older record whose replay changes, but R1's finding 1 changes how the pass re-plans an older game's charge (section 15).

### 13.1 What departs from the plan's entry and the frame, first

- **Six rulings were made at the reviews** (22:36), R72.17 to R72.22 above: R72.17 amends R72.3 and R72.4, and R72.18 amends R72.12.
- **5a's claim that an ordinary charge charges as before was wrong as built** (section 8.1; R1's finding 1): its bar test counted building connections and the first refused step anywhere in the explored map, so in built-up terrain A15.45's comparison applied to nearly every charge. R72.17 narrowed it. Older games re-plan their charges under the narrowed rule; whether the swept charge keeps its nearest target is for the gate (section 15).
- **R4's finding 3 was in neither round's brief:** made by the session at 23:29, after Round B (`LiveCloseCombat`'s `vehicleSides` leaves out a hidden or captured vehicle, as the planner's Known predicate does).
- **R2's note 5 needed a page, not code:** Round B rendered A25.72's Finnish progression chart (p. 97, `render_p97_finnish.py`); the Sissi units are not on it, so no rows were added and the Finnish 8-3-8 and 3-3-8 stay as R15.13 has them.
- **R3 found eight of the 14 games then recorded at their setup:** the session had rerun their scripts after each Studio check. The whole-pass check played every game again (13.4).
- **Slips of the agents with the user's games:** two Greps of the 5b agent with path `src/ASL` listed file names under `src/ASL/boards`; none was opened.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 71's as the model: 13.4.

### 13.2 Fixed

**Round A, the charge and Battle Hardening** (22:36 to about 22:55, 76cac041; R1 1 to 5 and 6's bridge, R3 1 and 5 to 8, R4 1, 2, 4, 7, 9):
- R72.17 in `ScenarioA1ChargeCalculator`: one search still, carrying a relaxed cost beside the real one (a bar priced as Open Ground), read only when a bar was met; `RuleBar` a cliff or an unbridged Water Obstacle, a building connection neither bar nor gap; the comparison against the next group that has a reachable target, strict; at a tie both groups' steps; A15.44's verdict only with a Known enemy unit in LOS, a bar met, no gap, and no enemy Location reached; `ChargeSearches` stays 1 (R1 1 and 2, MAJOR; R1 3 to 5; R3 7);
- the bridge decided in Rules: `ChargeEntryFacts.Bridged`, which `RuleBar` reads, still a gap until the movement model reads bridges (R1 6; R4 4);
- the texts: the unreachable line names the bar met ("a cliff", "a Water Obstacle") and reads "except across"; the panel line says the hex and "stays in place" once; the no-route line "to no Known enemy unit in its LOS (the nearest [A3])", citing R30.5 for a gap; the hardening choice names the unit with its hex, "[A8]: 4-4-7 squad R1" (R3 5, 6, 8; R4 1);
- the record: a `choice-made` of key `berserkHardening:` is worded through `HardeningText` (the unit, its hex, taken or refused, and what it became), and Replay names the `battle-hardening` kind in its pending and answered titles (R3 1, MAJOR; R4 2);
- `PlanBerserkHardeningAnswer` applies `BerserkEndsConditions()` to the produced unit (R4 9); the phase advance tests the MPh and berserk before it reads a unit's Location (R4 7).
Rules 20 and Play 15 methods passed; strict Rules and Play exit 0 (the agent's report). Tests added: Rules `AtATieTheAttackerChoosesAmongBothGroupsFromOneSearch`, `AnOrdinaryChargePastAMultiLevelBuildingKeepsTheNearest`, `WithTheNextNearestUnreachableTheChargeKeepsTheNearest`, `ATargetCutOffByAnotherEnemyLocationKeepsRulingR305`, `WithNoKnownEnemyInLosTheChargeIsNeverHardened`, `TheRuleBarsAreACliffAndAnUnbridgedWaterObstacle`, and `PastABarAnAlternateRouteStaysOnTheNearestWhileStillCloserThanTheNextNearest` (renamed from 5a's "NoLongerThan"). `games_5a.py`'s three games were judged against R72.17 and kept their layouts. In the Studio (22:56 to 22:57): `p72-charge-unreachable`'s choice "[A8]: 4-4-7 squad R1 is berserk and can reach no Known enemy unit ..."; taken, "4-4-7 squad R1 in [A8] loses berserk status and is Battle Hardened: it is Replaced by an unbroken 4-5-8 squad (A15.44, A15.3)".

**Round B, fire, ordnance, Unit Substitution, and the hero** (22:58 to about 23:28, 1fa0a214; R2 1 and 3 to 6, R3 2 to 4 and 9 to 11, R1 6 to 8 and 10, R4 5, 6, 8, 11, 13, and the notes' known items):
- R72.18: `WeaponEffects` removes a weapon with no printed X# at its Original B# under the Shortage (a B11 LMG is B10 and X11); `ShotDepletion` leaves a captured Gun's Depletion Number as printed (R2 1, MAJOR; R3 2, MAJOR; R2 3);
- R72.19: the morale check record carries `unitSubstitution`, and with it its `underscoredElr` is checked against the state (R2 4);
- R72.20: A15.46 after a vehicle kill and after fire leaves the destroyed vehicle's PRC out of the Known enemies left; CC's own check counts Known enemies only, behind a new fact on the CC facts, `knownEnemiesOnly` (R1 8, 10);
- the boundary: `SelectTargets` decides TPBF from a firer-in-target fact Play hands it; `heroWeapons` is set for any hero crew and `HeroDrm` filters the weapon; the firers Replaced under the Shortage are a fact on `BerserkAttackFacts` (`Replaced`), decided in `BerserkReturnsAfterAttack`; R72.21's comment on `tpbfSide` (R1 6, 7; R3 11; R4 11);
- the texts: a weapon's lowered B# or X# said with its printed number and cause ("B10 (B11, less 1 for Ammunition Shortage, A19.131)", "less 1 in Inexperienced hands, A19.32"); the PSK's +1 cites A15.23; the return line "by its TPBF" or "by its FT"; the shortage record line lists every candidate's Random Selection dr; a weapon with no printed FP or range named without a dash ("PSK", not "PSK -", in `ScenarioCards.Describe`); the ordnance proposal's hero DRM worded as the record's ("hero's SW, 1-4-9 hero G1"); `ordnance.crew-outside` says a lone leader may not fire a PSK (R3 3, 4, 9, 10; the notes);
- memory and performance: `BerserkReturnsAfterFire` tests for a berserk firer before it takes the index; `BerserkClearedBy` reads a lazy dictionary by event id; the token parsers test the prefix before splitting (R4 5, 6, 8);
- the Fire Lane fields are hidden under Ammunition Shortage unless a captured MG is chosen (`ScenarioA1FireEligibility.FireLaneOffered`; R4 13);
- R72.22 pinned by a Rules test (R2 6); R2 5 as 13.1 says.
Rules 33, Play 23, and MapStudio 1 methods passed; strict Rules, Play, and MapStudio exit 0 (the agent's report). Tests added: Rules `AnUnderscoredSquadUnderBothSsrsIsStillReplacedByItsTwoHalfSquadsOnAShortage`, `ALoweredBreakdownNumberIsSaidWithItsPrintedNumberAndItsCause`, `ACapturedGunsDepletionNumberIsNotLoweredUnderAmmunitionShortage`, `AFirerReplacedUnderAmmunitionShortageDoesNotReturnToNormal`, `AConcealedEnemyLeftInCcKeepsNoBerserkUnitBerserkFromThePassOn`, `ATpbfIsSelectedOnlyWhenAFirerStandsInTheTargetLocation`, `TheHerosDrmReadsInTheProposalAsTheRecordSaysIt`, `TheFireLaneIsOfferedUnderAmmunitionShortageOnlyWithACapturedWeapon`; Play `ABerserkSquadThatDestroysATankCarryingARiderInCcReturnsToNormal`, `ACcRoundRecordsThePassStampAndAnOlderRoundReplaysWithoutIt`, `ACardNamesAWeaponWithoutPrintedNumbersWithNoDash`; pass 70's `ALeaderLostToInterdictionCausesALlmcOfTheUnitsInHisLocation` now asserts the morale check's stamp; MapStudio `CloseCombatComponentTests.TheFirePanelFollowsTheSelectionAndThePhase` asserts the hidden Fire Lane fields. Two games were added, `games_fixb.py`: `p72-fixb-mmg-x11` and `p72-fixb-rider-cc-return`.

**After Round B, by the session** (23:29): R4's finding 3, as 13.1 says. `build_studio.sh` exit 0; the three methods that assert `vehicleSides` (`BacklogPass72Tests.ABerserkSquadThatDestroysTheTankInCcReturnsToNormalOnlyWhenNoKnownEnemyIsLeft`, `BacklogPass72Tests.ABerserkSquadSetUpWithALoneTankInItsLocationDestroysItInTheCcphAndReturnsToNormal`, `BacklogPass70Tests.ABerserkSquadBesideItsTankOwesItsAttackOnTheEnemyInfantryThere`): 4 passed.

### 13.3 Not fixed: a row each in the backlog

The backlog's section 75.2 has each: a bridged hex and a pond hexside read as gaps in a charge (5a; Round A kept the bridge a gap); a PRC keeping R15.9 (5c); ordnance with no X# removal under the Shortage (5d); A7.353's same-phase forfeit of a hero who used a SW (5e); a MG fired as ordnance by a hero (5e); a destroyed vehicle's PRC left in the state (Round B, beside R72.20); a chosen Fire Lane weapon still sent when its field hides (Round B, beside R4's note 13); the Replacement's latest line naming only the chosen dr (R3's note 10, seen at the whole-pass check); the CC and ordnance verifiers accepting a post-pass record that drops `vehicleSides` or `heroWeapons` (R1's note 9). Accepted as the reviewers judged them, with no row: one stamp for 5c and 5d, so a fire record made between them in a game holding `ammo-shortage:` would be refused (R2 7; session games only); the ordnance shot's `ammunitionShortage` checked against the state with no stamp (R4 10; safe, since no token existed before 5c); the ordnance SW's lowered B# has no unit test (5d); no page test asserts the undecided-charge line (5a).

### 13.4 The Studio check

At about 23:31 to 23:46, in my Studio on port 6670, on the build of the tree after Round B and the session's fix of R4's finding 3. Its scope, decided under the user's word, with pass 71's as the model (the standing rules, section 1, point 4): every pass game replayed to its result; the hex audit; 320 pixels on a fire panel and the records; the measurement. No side's view changes beyond the new public lines, so no side-view check beyond them was owed.

- **Every pass game played to its result:** all six games scripts run again (15 games), each played in my Studio: the three charges (to [G8] by [A9]; the hardening declined, "... refuses Battle Hardening, so it is Good Order"; into [A2]); the TPBF return ("by its TPBF in the AFPh"); both CC tank kills, the second with a Rider and no Known enemy left; the 8-3-8 to a broken 4-3-6; the LMG at B10 ("less 1 in Inexperienced hands, A19.32"); the Shortage's Replacement; the MMG at B10 (malfunctioned on a 10) and X11 (removed on an 11); the Fire Lane field not offered under the Shortage; the three hero shots ("The PSK", the +1 citing A15.23).
- **Found:** the latest line of the Replacement names only the chosen dr; the dice line shows both (13.3).
- **The hex audit:** no choice names a counter without its hex, in either view (two games).
- **320 pixels:** no horizontal scroll on the fire panel and the records.
- **The measurement** (section 14): level with the base, the hand-over faster.
- `studio_play_72.js` keeps the page helpers the check used.
- **Not done, as the scope allowed:** the five widths in each side's view for every control; a whole Player Turn at 320; the Replay page.

## 14. As built

**The pass in commits,** on `feature/asl-backlog-pass-72`, cut from b0a36132 (main): the frame (6787a7c4); 5a (ce16561e); 5b (955321db); 5c (c5cd9717); 5d (993f3169); 5e (db15561b); the reviews and their rulings (3c928950); fix Round A (76cac041); fix Round B with the session's fix of R4's finding 3 and the whole-pass check (1fa0a214); these documents. Each increment was built by an agent to a brief, its diff read, its games played in the Studio by the session, its new JSON keys checked against the store's older records (`skip_json_collisions.py`, "no key is an older record's") and put in `SKIP_JSON`, and its games added to `OWN` in `pass72.conf`.

**What the plan's entry asked, and where it went.** The plan names 10 inventory rows. After the pass 5 of them are built (3 with a deviation: A15.43's R30.5 for a gap of the model, A19.132's PRC, A19.32's ordnance class read) and 5 moved to the pass that takes what is left: pass 73 1 (A15.431's Dash), pass 105 1 (A19.131's circled B# and Low Ammo), pass 110 1 (A15.23's Guns and other weapons), pass 120 1 (A15.45's Wire and minefields), and pass 180 1 (A19.31's Human Wave, which the row had always held). The two tasks are built as sections 8 to 12 say, each with its departures; R72.2's moves have a row in the backlog's section 75.1, and the findings not fixed in 75.2. The item moved in from pass 70 is built (a charge into a Location holding an enemy vehicle and enemy Infantry, 5a). The inventory rows of other rules the pass's code touched have a note of it (A15.21 became partly, its heroic leader in fire with pass 73; C13.45 partly, a lone hero's PSK built), and the stale notes of section 5, item 1 are corrected.

**The rulings:** R72.1 to R72.22, decided under the user's word of 2026-10-27 and open to a veto: R72.1 to R72.14 in the frame; R72.15 and R72.16 at the build (5c and 5d); R72.17 to R72.22 at the reviews. They are in the Passes Plan's section 5. R72.9 lifts R9.10 for stamped records, and R72.13 replaces R15.9 where a token is given.

**What a record gained** (each absent from every older record, or written only when set, and taken from the record at replay): on CC facts `vehicleSides` (5a) and `knownEnemiesOnly` (Round B); on a fire attack `unitSubstitution`, the pass's stamp (5c), and `ammunitionShortage` (5d); on a fire target and firer `underscoredElr` (5c); on a morale check `unitSubstitution` and `underscoredElr` (5c, Round B); on fire rolls `shortageSelection`, a dr of purpose `fire-shortage-selection` that calls no Sniper (5d); on a fire resolution `shortageEffects` (5d); on an ordnance shot `ammunitionShortage` (5d) and `heroWeapons` (5e). The choice kind `battle-hardening` gained the key `berserkHardening:<unit>`, answered by `conditions-changed` and `lineage` (5a); the returns to normal are `conditions-changed` events (5b). No new event type. `SKIP_JSON` names the eight keys.

**What a view gained, never recorded:** the hardening choice's and the undecided charge's wording; the B# and X# words with their causes; the Fire Lane fields hidden under the Shortage.

**The tests:** `ScenarioA1Pass72RulesTests` (42 methods) and `BacklogPass72Tests` (18), with methods changed or added in `ScenarioA1CloseCombatRulesTests` (5a), `BacklogPass17Tests.TheSsrTokensAreCheckedAsANewGameChecksThem` (5c's ten token rows), `BacklogPass70Tests` (Round B), and `CloseCombatComponentTests` (Round B). No page test class was needed. Each increment's and each round's tests were run by method only (the user's rule of 2026-10-23), with one slip: 5b ran one whole class, `ScenarioA1SequenceRulesTests`. The ten new token rows of `BacklogPass17Tests.TheSsrTokensAreCheckedAsANewGameChecksThem` and `ScenarioA1Pass19bTests.EveryPackageKnowsTheCardCounters` read the repository's root, which the filtered runs built outside it do not have: the gate's suite is their first run. The Rules, Play, Units, and MapStudio projects were not run whole, which is the gate's.

**The measurement** (section 7), on `p70-perf-tw`, at 1920x1080, hand-over and "end the phase" proposal in ms:

| When | The pass | The base |
|---|---|---|
| The base, 19:08 to 19:10 (main-bin of b0a36132 on 6671, alone) | | hand-over 402, 491, 498, 486, 506, 490, 533, 448, 499, 485 (median about 490); proposal 96 (first), 68, 79, 74, 79 |
| After 5a, 19:57 (my Studio alone, a fresh process) | hand-over 1463 (first), then 422, 431, 349, 370, 413, 352, 338, 381 (warm median about 381); proposal 20, 178, 85, 65, 99 (median 85) | |
| The whole-pass check, 23:31 to 23:46 (each alone, fresh, the page visible) | hand-over 744, 311, 305, 365, 299, 320, 323 (median about 320); proposal 329 (first), 71, 61, 69, 120 (median about 70) | main's build on 6671: hand-over 771, 495, 266, 791, 519, 566, 506, 248 (median about 510) |

The first readings of the whole-pass check, about 1,060 ms for the hand-over, were taken with `document.hidden` true (the Studio's window was behind another application, and a hidden page's timers are throttled); they are set aside, not readings. Every reading is level with the base or faster; the stop threshold (a warm median more than 20% over the base's) was not reached. The charge's own timing was not measured apart from the proposal; `ChargeSearches` stays 1 on a fall back (a Rules test).

**The time:** the frame 18:53 to 19:07 on 2026-10-27 (with `base` 18:54 to 19:00); the base measurement 19:08 to 19:10; the build of 5a to 5e 19:10 to 22:25, 3:15 by the clock, against the calibrated forecast's 2:50 and the plan's 1:40; the reviews and their rulings 22:26 to 22:36 (the strict stage beside them); the fixes 22:36 to about 23:29; the whole-pass check and the measurement about 23:31 to 23:46; then these documents. The time log has each row.

**The strict build** (`gate.sh 72 b0a36132 strict`) passed on db15561b (22:26 to 22:28:53), 0 warnings and 0 errors, the formatter changing nothing. Each fix round's agent built its test projects strictly to exit 0. After the fixes, the whole-pass check, and the session's `vehicleSides` edit, it ran again on 1fa0a214 and passed (to 23:49:31), 0 warnings and 0 errors, for the tree as the documents found it; `strict-check` says so for the tree.

**The slips the notes record:** the frame's, the commit's, and the base measurement's times first written by estimate (corrected from the files' and the commit's own times at 19:10; the frame's lines since through `notes_append.py`); the reviews' report time "22:41", an estimate ahead of the clock (corrected at 22:36); my Studio first started from the launch entry that runs the repository's old build (19:57); 5a's first game laid out by a terrain approximation, not the LosCalculator (its far target out of LOS; since then the brief asks for the LosCalculator); 5b's second game had its berserk squad advance in the APh, which A15.431 forbids; two Studio shots that went wrong by hand (5b's real dice before "Add", a 2MC; 5c's squad ticked in place of its LMG); the 5b agent's two Greps listing names under `src/ASL/boards` and its one whole class; the scripts rerun after their checks, which left eight games at their setup (R3); the first whole-pass readings with the page hidden.

## 15. The gate

Not run. The user's word of 2026-10-27 ("Run Pass 72 without my intervention up to the merge gate.") ends at the gate: the gate (`baseline`, `build`, `proofs`), the push of the pass's branch, its pull request, and the merge are each the user's to start. The gate's base is b0a36132 (`base` 18:54 to 19:00: baseline 7093ebc71392bd09, 102 older games in the store, the swept game's cuts among them); its `strict` stage passed on db15561b (section 14). `pass72.conf` names the pass's 15 recorded games (`OWN`; the measurement copy `p70-perf-tw` is not one), no older games beyond the swept game and the store's (`SWEEP_EXTRA`), `BOTH_SKIP` with `GameEvent.RulePackage`, `PRESENT="SniperCheck"`, `ABSENT="AmmunitionShortage"`, and `SKIP_JSON` with the eight keys of section 14, each checked against the store's older records.

**For the gate's stop, named now:**
- **The swept game.** Whether the user's newest game, `dl-tractor-works-03`, is finished and becomes the swept game. Pass 71's documents left it to the next pass's cut or the user's word; this session did not open it (the standing rule on the user's games). A new swept game costs a new baseline.
- **R72.17 re-plans older charges.** The sweep of `dl-tractor-works-02` reaches r-squad-10's charge (the Russian Turn 5 MPh, the cut at revision 1316; the charge decided at 1317, one step into [X3]); the narrowed bar test may keep its nearest target or not, and either result is to be named in the gate's report, not taken for a fault. No other store game holds a berserk unit that charges (section 4).

What the gate should expect to see differ, as intended changes: new plans of a berserk charge where a bar lies across the nearest target's route (A15.45) or none can be reached (A15.44); a charge into a Location holding an enemy vehicle and enemy Infantry, now entered; new plans whose hashes include the pass's null members; the text list's new and reworded texts (the undecided-charge line, the hardening choice and record line, the B# and X# words, "PSK" with no dash, `ordnance.crew-outside`, `fire.weapon-outside`). An older fire record replays as recorded: without the stamp, `dl-tractor-works-01`'s Conscript LMG at revision 1267 (B11, DR 10) stays a hit. No new event type was added, so no pass 72 game should be UNREADABLE on main for that reason; what main makes of the new choice key and the new facts in a pass 72 game is for the gate's report.

**The gate, run on 2026-10-28 at the user's word ("No vetoes, run gate", after the rulings R72.1 to R72.22 were shown; no veto).** `git fetch`: origin/main still b0a36132.
- 05:28:12 to 05:28:21 `baseline`: the 15 pass games copied into the store (102 to 117 games); `SKIP_JSON` "no key is an older record's"; the baseline 7093ebc71392bd09 of 2026-10-27 19:00 reused; the pass's games on main's side 151 replay lines. One game main's side cannot bring to a state, `p71-sniper-residual` (NOSTATE: its revision 2 names [P11], which board 3 does not have); older than the pass, read the same at pass 71's gate: a backlog row.
- 05:28:39 to 05:31:04 `build`: the solution with warnings as errors into bin/gate, 0 warnings, 0 errors; the proof tool compiles against it.
- 05:31:10 to 05:32:53 `proofs`: PROOF_SKIP 30 names from the diff; `SKIP_JSON` the eight keys; BOTH_SKIP `GameEvent.RulePackage`. **The early check passed** (10,296 lines, 0 changed). The text list 16 lines differ; the replay 3,316 lines against 3,288, **only the pass's own games differ**; the sweep 51,514 lines, **no group differs**.
- **Every differing line named:**
  - the text list: new, the Shortage's roll key (`asl.a1.fire.roll-missing:shortageSelection:`), the five Battle Hardening record lines (`play.berserk-hardening`), `play.charge-unreachable`, the Fire Lane refusal under the Shortage, the token refusal (`play.unit-substitution-rule`), and the four movement refusal codes the charge calculator now reads (`play.move-cliff:`, `-level:`, `-stairwell:`, `-upper-level:`); gone, the refusal of a charge into a vehicle-and-Infantry Location (`play.berserk-vehicle`, R72.5); changed, `play.charge-no-route` (R72.17, citing R30.5). None gone that was not the pass's.
  - the replay: ten of the 15 pass games differ. In nine, main's build stops at the first record holding a pass fact (`unitSubstitution` on a fire attack in six; `underscoredElr` on a fire target in one; `heroWeapons` on an ordnance shot in the two hero ordnance games), as expected for a new fact. In `p72-charge-unreachable` main's projector enters the Battle Hardening answer among the game's answered choices, which the pass's does not, so its last two states differ (5a's report). The other five replay alike on both sides.
  - the sweep: equal, and not a void run: the early check compared 10,296 lines, the gate's tool was compiled against this build, and the pass's own games differ in the replay. R72.17 left r-squad-10's charge in `dl-tractor-works-02` as it was (no rule bar lies across its route, so no comparison applies). **The gate passes.**
- Not decided at the gate: whether `dl-tractor-works-03` becomes the swept game (no word from the user; `dl-tractor-works-02` stays).
