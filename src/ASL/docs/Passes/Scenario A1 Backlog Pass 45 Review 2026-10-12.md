# Scenario A1 Backlog Pass 45: Infantry II

The review of pass 45, written on 2026-10-12 after the five reviews, their fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 45 Design](<ASL Unit Backlog Pass 45 Design.md>); its section 13 says what was built.

## Status

Merged into main as e5b6c0b5 on 2026-10-12, with the gate passed; the CI run of the merge (38295173548) was left for the user to read. Built on branch `feature/asl-backlog-pass-45`, cut from main at 5982767e: the frame, ten tasks and 45.1b in eight commits, then the five reviews, their fixes, the Studio check, and these documents. The frame's questions F1 to F10 were answered yes by the user on 2026-10-12. Everything after the frame was done under the user's word of that day, "Implement all parts of Pass 45 with out my intervention", and is open to a veto. The gate was run on 2026-10-12 at the user's word and passed after two fixes (design 14): style errors of the strict build in three test files (98d3dfca), and an older fire record with a berserk firer that no longer replayed (df8005f9). The merge and the push followed at the user's word.

## What departs from the frame

- Ordnance HE's Residual FP (A8.25) is not built; it goes to pass 110. The frame placed it in task 45.3 (design 9.1).
- Spraying Fire in the MPh (A9.52, A8.24) is not built. The frame placed it in tasks 45.3 and 45.4 (design 10.1).
- Fire Lanes are completed in one point of six, the forced cancellation (A9.223). Lanes against vehicles, attacks on later MF expenditures, the Alternate Hex Grain, the Fire Lane Snap Shot, and hexside TEM and Hindrance stay in the backlog (design 10.1).
- The mortars' Repair number is not manufactured, so a mortar is still not repaired (A9.72). The answer to question F9 was to manufacture it; that needs a catalog version, which the pass did not open (design 11.1).
- SW Self-Destruction (A9.73) is not built (design 11.1).
- Task 45.10 built two things of pass 40's movement rows: a CX leader lends no IPC, and the TI rule of question F8. A hero's IPC, the choice in the APh, the Recovery at once of A4.44, A4.61 by unit, and the overstack MF of A4.63 stay in the backlog (design 11.3).
- No Studio game for Mandatory Fire Direction (A9.4): it needs a clear LOS of 17 hexes, which I did not look for. A Rules test holds it.
- The frame's point 5.7 is corrected: A7.353's halving is the Area Fire's own, not a second one. Point 5.6 is withdrawn.

## What a player now meets

- A unit fires up or down a stairwell at the next level of its own hex as PBF, and a LOF from directly above or below counts toward Encirclement (A7.21, A7.7). A unit upstairs with no path to ground level free of an unbroken, armed, unconcealed enemy unit is Encircled (A7.72).
- A unit that cowers is marked Final Fire or Prep Fire with all its SW; in a group, the unit is found by Random Selection (A7.9).
- A weapon that kept its Multiple ROF in the MPh left no Residual FP for its own FP; the DEFENDER may give the ROF up at once and place it (A8.23). Residual FP is held to 12 after its reduction, is a column lower for a CX firer, is none for a malfunctioned weapon, and attacks a vehicle once a Location (A8.2 to A8.26).
- A squad whose Range is underscored uses Spraying Fire with its own FP (A7.34). A squad without the option does not add its FP to its MG's spray (ruling R45.5).
- A Fire Lane is gone when its MG or its squad fires in its own Location (A9.223).
- A MG in woods or a building fires its later shots of a phase only inside the CA of its first (A9.21). A MG fires beyond 16 hexes only when a leader directs it, and its target at 17 or more is treated as concealed (A9.4). A leader directs the later shots of a MG he directed (A7.53).
- A weapon whose B# an attack lowered is removed on a DR at least its printed B# (A.11). A lone SMC uses no Sustained Fire (A9.3). The weapons A9.8 names are dismantled, and a German dm MMG or HMG fires as a LMG.
- Units with different fire counters fire as one group, each by its own counter (ruling R45.1); the group's Locations are one chain; a berserk unit joins no group of several Locations; a crew that fired its Gun keeps its FP in Subsequent First Fire and FPF (A7.5, A7.54, A7.353).
- A MMG or HMG of the Scenario Defender Bore Sights a Location at setup and deducts two from its attack there, fired alone (C6.41 to C6.44).
- A K result eliminates a crew (A7.302). A Gun's shot, a thrown DC, and a Starshell wait behind the attack owed under A8.312 (ruling R45.2).
- A CX leader lends no IPC (A4.52). A TI unit Transfers, Drops, Recovers, Deploys, and Recombines in the RPh (ruling R45.6).

## The rulings

R45.1 to R45.6, in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. R45.1, R45.2, and R45.6 came from questions the user answered yes. R45.3, R45.4, and R45.5 were decided without the user, each by the proposal that would have been put.

## The reviews' findings and what was done

Two BLOCKERs, both fixed. The findings fixed:

| Review | What was wrong | What was done |
|---|---|---|
| Referee, first half, 1 (BLOCKER) | A squad marked First Fire could not fire its own FP in one group with a unit that fires its MG alone, which is the example of p. 62: the attack was read as First Fire because a firer fired a MG alone, and the marked squad was then refused. | `FireKindFacts.EveryFirstFireMarkedFiresAlone`: the attack is First Fire only when every marked firer fires a MG alone on its ROF. An unmarked unit fires beside a marked one. Rules test `TheReviewsFixesOfTheMixedGroup`. |
| Referee, first half, 2 | The "every MG" bar of A8.3 bound a unit with no counter that fired beside a marked one. | `EveryMgBar` reads the firers that use Subsequent First Fire or FPF. |
| Referee, first half, 3 | A crew that fired its Gun was judged by the attack's kind, not by its own counter. | Its own kind: with no counter it has no FP in the group's First Fire; marked Final Fire it joins FPF. |
| Referee, first half, 4 | A leader who had directed could direct a group that holds a unit with no counter. | `director-outside`, as the example says ("leader direction is NA"). |
| Referee, first half, 5 | Prep Fire at a "?" stack of Dummies took the Bore Sighting DRM. | A Dummy is Infantry to the firer: outside First Fire the DRM is given only where the Location holds no target at all. |
| Referee, first half, 6; Rules boundary, 7 | The four conditions of C6.43 and the dismantled weapon were decided in Play; the setup check read no Normal Range and no 16-hex limit. | `MgSightingFacts` and `MgBoreSighted` in Rules; `MgBoreSightBar` takes the Normal Range and the LOS with its range. |
| Referee, first half, 7 | A Residual FP attack did not read the Encirclement of A7.72. | It does. |
| Referee, second half, 1 | A HS that fires a MG, which adds no FP of its own, was refused Spraying Fire with its MG. | The capability test counts a firer's own FP only where it adds it (`AddsOwnFp`). |
| Referee, second half, 2 | Extreme Winter's lowered B# made an X#. | Only the causes A.11 lists do (Sustained Fire, capture, Inexperience); the weather lowers the B# and the X# alike. |
| Referee, second half, 3 and 12; Rules boundary, 1 | The Field of Fire gave a shot within the MG's own hex a bearing, and found a MG's earlier shots by its unit. | No bearing within the hex; the earlier shots are found by the weapon. |
| Referee, second half, 5 | The projector's leave for a MG's later shot at the same Location held for any MG fired alone. | `KeptRofThisPhase`: only a weapon that kept its ROF in an earlier record of the phase. |
| Referee, second half, 6 | A lone hero used Sustained Fire. | A hero is a SMC (A9.3). |
| Referee, second half, 7 | An ATR's FP was set apart from the Residual FP twice. | Once. |
| Referee, second half, 9 | The halving at 17 hexes and more was applied against every target, and a second time beyond the NVR. | Against Personnel only, and once. |
| Referee, second half, 10; Rules boundary, 4 | The projector took the Residual FP of a ROF given up without asking whether the weapons had kept it. | It takes the figure only when the weapons that kept their ROF carry a fire counter. |
| Referee, second half (A8.21, A8.22, A8.23) | The ROF was offered for a counter no larger than the one standing there; a Sniper Check written with the attack closed the offer; the once-a-Location rule was read for a stack, not a unit. | `RofForfeitOpen` takes the standing FP; `PartOfTheAttack`; `ResidualAttacksAgain` by unit. |
| Referee, second half (A4.52, A22.1, A9.8) | One CX leader took the IPC from a fresh leader of the stack; a FT at the next level of its hex was given PBF; a dismantled mortar's refusal was Play's. | The leader that lends is one that is not CX; the FT's band is Normal; `DismantledShotBar` and `LmgDefinition` in Rules. |
| Rules boundary (two members) | Play decided when the other acts wait behind the attack owed, and who holds a Location of the A7.72 path and whom the rule exempts. | `OtherActBar` takes the phase, the side, and a reader of the attack owed; `ScenarioA1UpperLevelEncirclement` takes `UpperLevelUnitFacts` and `PathUnitFacts` (`Applies`, `Holds`). A search that reaches its limit calls no unit Encircled. |
| Rules boundary (the bar's words) | `play.act-mandatory` could name the units that owe the attack to an ATTACKER whose shot named a DEFENDER's vehicle as its target. | The bar reads the acting units and weapons only. |
| UI 1 (BLOCKER) | The page dropped a possessed MG's Bore Sighted Location from the setup it sent, so no MG set up on the page was ever Bore Sighted. | Sent, as a Gun's is. Seen in game `p45-setup-bore`: the `bore-sighted` event, visible to its side only. |
| UI 4 and 5 | The A7.72 search ran for every unit at every render, and during setup, when a view does not hold what the rule counts. | `CutOffUpstairs`: once for a revision and a view, for units above ground level, and not while setup is open. |
| UI 7, 8, 10 | An AAMG's row called another level of its hex "out of range"; two Locations of one hex were compared through the map's layers; the Bore Sighted Location was shown without brackets. | The row says no attack at another level; the levels are compared directly; `PlaceOf`. |
| UI 9; table player, 5 | The ROF offer's note spoke of one weapon where several kept their ROF, and was not announced. | Singular and plural; `role="status"` and `aria-describedby`. |
| UI (the Spray field); table player (three words) | The Spray field's title sat on an empty label; the DRM list showed `bore-sighting` and `mandatory-fire-direction` raw; the dice list showed `fire-cower-selection` raw; a weapon removed on its X# was called malfunctioned. | The title is the field's; `DisplayText` has the three; `FireText` says the weapon is removed (A.11, A9.7). |
| Table player (two refusals) | `play.fire-field` did not say what the Field of Fire is; a TI unit's refusals did not say what it may still do. | Both reworded. |
| Table player (the games) | The Fire Lane game placed no lane: its DR of 11 malfunctioned the MMG. The two Bore Sighting games rolled 11 too. | Recorded again: `p45-fl-lane4` (the lane placed along [F10], [E10], [D9], and gone after the attack owed in [F10]); `p45-fg-bore3` (the MMG alone, DR 7, minus 2); `p45-fg-unsighted2` (its control); `p45-fg-bore2` (the MMG with its squad's FP: no DRM). |
| Table player (the labels) | The false starts and one game that is no control did not say so in their labels. | Seven labels now begin with SUPERSEDED, CONTROL, or NOT A CONTROL and the reason: `p45-fl-lane`, `p45-fl-lane2`, `p45-fl-lane3`, `p45-fg-bore`, `p45-fg-unsighted`, `p45-fg-bore2`, `p45-up-open`. The games are live Studio data and are not committed. |

The findings not fixed, each with a row in the backlog's section 57:

| Review | What | What happens now |
|---|---|---|
| Referee, second half, 4 | A Fire Lane's MG: the FP its lane takes, and the Residual FP and ROF offer of the attack that lays it (A9.22, A8.23) | The lane's FP is the MG's as built in pass 12; the attack that lays it leaves its own Residual FP and makes no ROF offer. |
| Referee, second half | The ROF is given up for all the weapons that kept it, not weapon by weapon (A8.23, p. 60) | One offer a record: every weapon of it that kept its ROF gives it up. |
| Rules boundary | `DirectedTheseMgs` is taken from the fire record and not checked against the phase's records at replay (A7.53, p. 57) | The planner writes it from the records; the projector trusts it. |
| Referee, first half | Directed FPF in the long example of p. 62, and the order of the director's NMC (A8.31, A7.53) | Older than the pass: a director of FPF takes his NMC with the firers. |
| Referee, first half | `WithinSubsequentFirstFireRange` is one fact of the attack, read over every firer's Location (A8.3, p. 61) | A group of several Locations is held to the fact of the farthest. |
| Referee, first half | TPBF against PRC from a higher Location (A7.211, p. 54) | Not built; pass 65. |
| Table player | The Cowering sentence, the Fire Lane's cancellation, the A7.72 morale penalty, and the ROF given up are not told in the "Latest" line or the outcome (A7.9, A9.223, A7.72, A8.23) | Each is in the record and on the Records tab; the view the sentence is built from lacks the data. |
| UI | The Target list offers the next level of a hex that no stairwell joins (A7.21, B23.25) | The proposal is refused with the reason. |
| UI | The Bore Sighted field shows for a LMG and for either side (C6.41) | The setup check refuses it with the reason. |
| Table player | The German LMG has B11 in the catalog, and the page's own example has B12 (A9.7; the catalog) | A catalog value, for the catalog's next version. |
| Referee, first half | A DC attack does not read the Encirclement of A7.72 (A7.72, A23) | A Set or Thrown DC resolves without the upper-level read. |
| Rules boundary | The test helper of `BacklogPass30Tests` is a copy of the page's `PlacementArguments` ((tests)) | It hid the page's dropped field: the test passed and the page did not send it. |

## The Studio check

In my Studio on port 6671, built into `bin/p31b` after the reviews' fixes, with scripted dice.

- **The five widths** (1920 by 1080, 1366 by 768, 1024 by 768, 683 by 384, 320), in each side's view and the adjudicator's: the ROF offer with its note and button (game `p45-walk-wide`), measured for anything past the right edge. Nothing at any width. The offer shows to the DEFENDER and the adjudicator and not to the ATTACKER. The setup's Bore Sighted field was measured at four widths (`p45-setup-bore`): inside the page at each.
- **A full Player Turn at 1920 by 1080:** the move into [D10], the LMG's shot alone that kept its ROF, the ROF given up with the Enter key on the offer and on Confirm, and every phase to the next Player Turn's MPh.
- **A full Player Turn at 320:** the same move and shot in each side's view, the offer, a second shot at the same MF refused with its reason (A8.3, A9.2), and every phase to the next Player Turn's PFPh; there the fire panel's Spray field with its title, and the dismantle button, inside the page. The inspector's tab row is 11 pixels wider than the page while a proposal is open, as pass 40 found; it has its backlog row.
- **The A7.72 tag:** "Encircled: no path to ground level (A7.72)" on the unit's row in `p45-up-cut` at 1920 and at 320, and absent in the control `p45-up-free`.
- **After the fixes:** a MMG set up by hand on the page with its Bore Sighted Location, and the event in the game's file; the Fire Lane placed and then gone after the attack owed in the MG's own Location; the Bore Sighting DRM on the MMG fired alone, and absent with its squad's FP and in the control.
- **Not walked:** every control of the pass at every width in every view (the offer and the setup field were; the fire panel's fields were measured at 320 and 1920 only); a turn that uses Spraying Fire or a Fire Lane at 320; the Replay page; the turn's phases beyond advancing them, since the two walk games hold one unit a side.

## What was not done

- A Studio game for Mandatory Fire Direction, and Play tests for the two levels of a hex and for A7.72: the test boards have no stairwell building, so the games and the Rules tests stand for them.
- Each control at each width in each view, and the Replay page.
