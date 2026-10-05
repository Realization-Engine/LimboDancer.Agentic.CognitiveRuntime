# Scenario A1 Backlog Pass 31d: the leftovers of pass 31c

**Date:** 2026-10-04

**Related documents:** the [pass 31d design](<ASL Unit Backlog Pass 31d Design.md>) (section 2 for the measurements, section 14 for what was built); the [pass 31c review](<Scenario A1 Backlog Pass 31c Review 2026-10-04.md>), whose third play test this pass answers; the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 51; the [ASL Unit Backlog Passes Plan](<../ASL Unit Backlog Passes Plan.md>), rulings R31d.1 to R31d.6.

## Status

Built on branch `feature/asl-backlog-pass-31d`, at the user's word ("Build, all as recommended"): six tasks, two reviews and their fixes, the Studio check, the tests, and the documents. Merged into main as 5120516 on 2026-10-04 at the user's word and pushed.

## What a player now meets

- **A phase's end takes a tenth of a second to check and half a second to confirm,** at Turn 1 and at Turn 7 of The Tractor Works alike. It took 2.2 and 3.8 seconds at Turn 5 on the same machine, and more with other work beside it. Every other proposal is quicker by the same cause.
- **A laden unit's rout says what it leaves and keeps, and does the leaving:** "8-3-8 squad G2 carries 7 PP and may rout with 3 PP (A10.4). It leaves the FT (5 PP) in [X4] and keeps the DC (2 PP)." The review says "leaves the FT (5 PP) in [X4], unpossessed" before Confirm, and the button reads "Confirm, leaving SW behind". The refusal a player met at every such rout is gone.
- **The squad keeps the right SW.** In the play test the same squad was made to leave its DC and then its FT; by A10.4 it keeps the DC.
- **The rout status gives a way:** "Its route must end in [AA4] (by [Z5])", with a button, "Use the route to [AA4]", that chooses the unit and fills the route.
- **Fire by concealed units at a "?" stack is made, not refused.** A concealed firer loses its "?" when a Good Order enemy unit within 16 hexes sees it, and keeps it otherwise (A12.14). A refusal no longer tells a Dummy stack from a real one.
- **A Dummy stack's removal is in the records of both sides,** and on the hand-over screen: "A German Dummy stack is removed in [S5]: it moved without Assault Movement, or into Open Ground, in the LOS of a Good Order enemy unit (A12.11)"; "The Russian Dummies in [X4] are removed before Close Combat (A11.19)"; "the Dummies there are removed (A12.14)" in a fire's line. Before Confirm the owner alone is warned, and of a move only with "if".
- **Fire at a Location that holds the firer's own captured units** says what A20.54 does to them, and the Target list marks the Location: "[X4]: 1 hex, PBF; holds your captured units".
- **A refusal tells a side what is wrong with its own group** (a FT fired with another unit) where the target holds a "?", and nothing else.
- **The Close Combat Phase's end says what was not fought:** "No Close Combat was fought in [Y5] this phase; the units of both sides stay there, held in Melee unless they keep their "?" (A11.15)", with the button "Confirm with a Close Combat not fought". The due line gives the reason in words: "a prisoner's Guard is not an enemy unit in its Location (A20.5)".
- **A FT reads "fires alone, without its holder's own FP (A22.31)",** and holds Propose when ticked with anything else; a weapon that has fired or malfunctioned is listed and cannot be ticked.
- **Words:** "the Russian side places 9 units under "?""; "Heat of Battle DR 4, 4 = 8"; "+ 1 (Assault Fire, A7.36)"; "PTC; 4-4-7 squad R4: passed"; a rout of two hexes in one sentence; "building [N4]"; "stone building", "Open Ground", "in the Prep Fire Phase"; "enters" and "advances" for one unit.
- **A blocked LOS is said as one** in the record: "the LOS is blocked, so the attack has no effect and its firers have fired (A6.11)".

## Rulings

R31d.1 (what a broken unit leaves, and the rout that does it; it corrects R13.5's check), R31d.2 (a concealed firer's "?"), R31d.3 (what is said of a Dummy stack's removal; it amends R31.7), R31d.4 (free LOS checks, a departure from A6.11 at the user's choice), R31d.5 (which of a refusal's reasons a side reads; it extends R31c.7), R31d.6 (the Close Combat Phase's end with a Location not fought). Each rule was read in the PDF's text before its change, and A10.4 again from the rendered page, whose text layer is mixed with an example's hex labels.

## The measurements

On copies of `p31c-tw` cut at a revision, in my Studio on port 6670; the stopwatch is the page's.

| Copy cut at | Check before | Check now | Confirm before | Confirm now |
|---|---|---|---|---|
| Turn 1, revision 250 | 0.57 s | 0.05 to 0.08 s | 1.1 s | 0.48 s |
| Turn 5, revision 607 | 1.9 to 2.4 s | 0.07 to 0.08 s | 3.75 s | 0.43 s |
| Turn 7, revision 693 | 2.2 to 2.3 s | 0.08 to 0.10 s | 3.77 s | 0.43 to 0.58 s |

A sampling profiler showed where the time went: 15 whole-game replays in a check and 26 in a confirm, 92 to 96% of the time. The planner replayed the stored log at each of its guards, the gate and the executor planned again, and the store parsed the file at every read. A game is now read once; a request applies its new events to what was kept. Every event is still applied and verified once.

## The two reviews

Two read-only reviews by agents in the main checkout, while the suite ran.

**The table player,** on how the page reads. Eleven faults, all fixed:

| # | Finding | What was done |
|---|---|---|
| 1 | The "Use the route" buttons ran together on one line | One to a line |
| 2 | A fact that failed read as true: "outside the reviewed case: the unit has the MF for the entry" | "this does not hold: the unit has the MF for the entry" |
| 3 | The drop's refusal never said the drop was refused, and broke with several loads | "... may not leave the DC (2 PP): it carries 7 PP, routs with at most 3 PP, and keeps the most it can ..." |
| 4 | Two SW of one kind could not be told apart in "It keeps" and in the refusal | Loads that differ only in which of two like counters is kept are one choice, and the game takes it |
| 5 | The captured-units line for one unit lacked its article and said "counts double" of nothing | A sentence for one unit and one for several; "counts double for the Victory Conditions" |
| 6 | The advance's summary stayed plural for one unit | "advances", "becomes CX" |
| 7 | "stone-rubble" and "wooden-rubble" still showed | Worded |
| 8 | "the Dummies among ..." when all that advance are Dummies; "may be removed" for a removal that is certain | "these Dummies"; the button says "are removed" where there is no "if" |
| 9 | The status named a place to end in that the game would refuse | Such a place is given no way and is not said while another is |
| 10 | "A American Dummy stack" | "An American" |
| 11 | The hand-over screen's Dummy line; none for the A11.19 removal | One line for a stack; the A11.19 removal said |

Left as they are: "the FT" where the design had "its FT"; a rout's merged record placed before its Interdiction (backlog); "the Close Combat package" in a player's line.

**The referee,** on disclosure, the rules, and the replay. Four faults fixed, three rows for the backlog:

| # | Finding | What was done |
|---|---|---|
| 1 | A hidden unit in the target Location still took the firer's "?", since the Fire package asked the target Location first; so did a prisoner of the firing side and a unit of it in a Melee | Where the planner's read is recorded for every concealed firer, it decides alone |
| 2 | Movement reads A12.14 by "not broken" and counts a hidden viewer and a Passenger; fire's new read does not | Backlog section 51. Movement's read is not this pass's, and changing it changes what a move reveals |
| 3 | A Dummy stack's own line was left out of any attempt that held a fire, so a stack removed by its move was not said when Residual FP attacked in the same attempt | The line is left out only where a fire line the view reads says it |
| 4 | A unit with more than ten SW could no longer drop | The load is read to sixteen SW |
| 5 | The rulings the code cites were not written | Written |
| 6 | The hand-over screen counted Dummy counters, where the records say one stack; nothing for A11.19 | One line for a stack; the A11.19 removal said |
| 7 | The store's kept record after a commit pairs old event objects with the new file's hash | A test: the kept record written again is the file, byte for byte |

Found sound: the mover's "if" warning (it rests on the mover's stack alone); the advance's "a concealed stack"; the seven reasons of the firer's own group; the phase's end consequence; the A10.4 reading and the leaving inside the rout; the copy of what the replay carries, and the match by the identity of the events.

## The Studio check

In my Studio on port 6670, on cuts of `p31c-tw`, each in the view of the side that acts:

- **Revision 638, German:** the rout of 8-3-8 squad G2 with its FT and DC, from the way's button through the load line and the consequence to the confirmed rout and its record; read again after the reviews' fixes.
- **Revision 237, Russian:** 81 fire proposals, every Location's units at every target in range. All were accepted; the play test met three "does not decide" refusals here.
- **Revision 699, Russian:** the due line with the package's reason, and the consequence at the Close Combat Phase's end.
- **Revision 600, Russian:** "[X4]: 1 hex, PBF; holds your captured units", and the consequence with "Confirm and fire on your own units".
- **Revisions 191 and 148, German:** the Dummy stack's line after its move, the hand-over screen's line, and "the Dummies there are removed (A12.14)".
- **Revisions 567 and 580, German:** the FT's row and a malfunctioned HMG that cannot be ticked.
- **The played games:** `guards-dl-01` with 221 steps, `p31c-tw` with 231, `p31c-play` with 134.

Not seen in the Studio: a choice among loads, a route of more than one hex, a blocked LOS confirmed, a refusal with a reason of the firer's own group at a "?" stack, the setup map's tooltips, "Since you last looked" after the view acts. The first four are in the tests. The widths of pass 31c's list were not walked again: the pass moves no pane.

## Tests

The merge gate, 2026-10-04, on commit f6c7d8d (the commits after it are documents):

| Step | Result |
|---|---|
| Solution build with `--warnaserror` | Clean |
| The full local suite, one project at a time | 2,935 passed, 30 skipped, none failed: Dice 24, Authoring 167, Maps Rendering 34, Maps 244, Maps Vasl 121, Counter Sheets 19, Units Rendering 352, Units 408, Rules 498, Play 687, Map Studio 381 |
| CI's Node test of the map viewport, by hand | 5 of 5 |
| The Docker Linux check | Every step at exit 0: locked restore, Release build with `--warnaserror`, every test project (2,935 passed, 30 skipped), the source verification and the pending comparison regenerations |
| The chart supplement, regenerated locally | Identical to the committed file, with sorted keys |

No existing test was changed but one: `BacklogPass20TablePlayerTests` reads the rout advice's third value. The suite had 2,901 tests passing before the pass; it has 34 more.

- **New, Play (23):** `BacklogPass31dTests` (7): the played Guards Counterattack continued one event at a time equal to it replayed whole, state for state and diagnostic for diagnostic; The Tractor Works continued phase by phase equal to it replayed whole; a list replayed again and a caller's growing list; an error not built on, and what was kept left as it was; the store's same events for the same bytes and new ones for a file changed; a commit's kept record written again equal to the file, byte for byte; every way offered for a rout at the start of each Rout Phase of The Tractor Works planned as that rout. `FireConcealedFirerTests` (5): concealed units fire at Dummies and keep their "?" with no enemy in LOS, lose it when another Good Order unit sees them, keep it when only a hidden unit does, and keep it when the target Location holds only a hidden unit; the proposal reads the same before Confirm. `FireOwnReasonsTests` (3). In `BacklogPass13Tests` (6): the load read with each SW's own PP whatever their order (the play test's FT and DC); the rout that leaves the rest; a choice among loads; like counters as no choice; a unit not laden; the way offered taken. In `BacklogPass14Tests` (2): the phase's end with a Location not fought, and after a round.
- **New, Map Studio (11):** `PlayRecordsPass31dTests` (7), on The Tractor Works: the game replays with its 716 revisions; a Dummy stack removed by its move said once to both sides, with no id and no count; Dummies removed by fire said in the fire's line; the A11.19 line at each phase change that removed Dummies and at no other; no record that says a DR twice or a result alone; a rout of several hexes as one sentence; what a view missed ending where it came back. `PlayPagePass31dTests` (4), on `WideBoards`: a target out of every ticked firer's range said, disabled, and refused with its range; the Rout panel's way, load line, and consequence through to the confirmed rout; a FT's row and the held Propose; a malfunctioned MG listed and not tickable.
- **A second played fixture:** `p31c-tw.game.json`, The Tractor Works as it was played (716 revisions), beside `guards-dl-01.game.json`.
- **Found while the tests were written,** and fixed: a least-cost way to one place a rout may end could run through another and out into the open again, which the game refuses (A10.51).
- **Not written, and why:** the setup map's tooltips and "Since you last looked" after the view acts on the page (each needs a long walk through the page's setup or two hand-overs; the second is covered at `PlayRecords.Since`); a blocked LOS confirmed (the test boards have no obstacle between two hexes that take units); the due list where the package refuses the Ambush (it needs a prisoner without its Guard). They are a row of backlog section 51.

## Left out

Backlog section 51. In short: A6.11 kept for a side; the viewing side's option not to force a loss of "?"; movement's read of A12.14; setup by hand in a player's words; why a prisoner stood without its Guard; three tests of pass 31c and four page tests of this pass.

## Questions for the user

Answered 2026-10-04: "Agree with 1. and 2. and then hold." The pass is merged; movement's read of A12.14 is brought in line with fire's in the next rules pass (backlog section 51); nothing further is started until the user says.

| # | Question | Recommendation |
|---|---|---|
| 1 | Merge pass 31d into main? | Yes, once the gate below is green. The pass changes the engine's read path and one decision of the Fire package; the tests hold a continued game equal to a whole replay on both played games, and every recorded attack verifies as before. |
| 2 | Movement's read of A12.14 ("Good Order (not just unbroken)", and a hidden viewer) differs from the read this pass gave fire. Bring it in line? | Yes, in the next rules pass and not here: it changes when a move takes a "?" or removes a Dummy stack, which deserves its own ruling and tests. |
| 3 | What comes next: pass 32 (DYO I, deferred), or pass 34 (Armored combat I)? | Pass 34, as the plan has it, unless another play test comes first. |
