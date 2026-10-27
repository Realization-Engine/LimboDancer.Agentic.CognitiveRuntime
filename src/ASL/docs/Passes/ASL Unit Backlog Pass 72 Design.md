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

**F12. Ammunition Shortage's numbers (A19.131, A.11, A21.11).**
- **R72.12:** under Ammunition Shortage every SW's Original B# and X# is one lower (not a DC, not a captured SW, A21.11), in IFT fire and as ordnance, and a lowered B# becomes an X# by A.11 as the existing reduction does; Ammunition Depletion Numbers are one lower; no Fire Lane may be declared. The fact is recorded on the attack or shot so the verifier checks it against the card, not taken as recorded like Extreme Winter's reduction. Vehicles' and Guns' circled B# go to pass 105 (R72.2).

**F13. An SSR ELR for underscored units (A19.132, R15.9).**
- **R72.13:** a target, and an FPF firer, carry an `underscoredElr` fact from the token; `ElrOf` reads it in place of R15.9's 5, and a failure beyond it Replaces the unit by the normal table. The German 8-3-8 has no entry: it is Replaced by the 4-3-6 Conscript, the only lower-Class German MMC none of whose factors exceeds the 8-3-8's (A19.13: none may increase, the Class must decrease; the 4-4-7 and 4-6-7 have a longer range). The Finnish 8-3-8 stays as R15.13 has it. R15.9 still holds where no token is given.

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
