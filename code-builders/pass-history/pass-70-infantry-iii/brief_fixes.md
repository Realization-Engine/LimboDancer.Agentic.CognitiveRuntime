# The fix rounds of pass 70

Read `brief_build.md` beside this file first; it binds a fixing agent as it bound a building agent (the Rules boundary, absent-safety, a side's view, memory and performance, the shell, one step at a time, tests by method, never `src/ASL/boards`). The findings are in the review files beside this file, named by file and number below (for example `fixes_referee_sight.md` 3). The rulings R70.41 to R70.47 of the design (section 6, F16) bind the fixes. Each round is one agent; the rounds run one after another, since they share files.

For each finding: fix it, or say why not and give it a backlog row (add it to `backlog_rows.md` under "From the fixes"). A finding a later round owns is left to it. Before each fix that touches a test, read the test first; a test that asserts the fault changes as intended, and the report names it.

## Round A: hidden information
- `fixes_referee_sight.md` 1 (the mover reads the viewer's option from the refusal and from Q15's pass), 2 (`prcHeldInMelee` names Passengers anywhere; gate it as the finding proposes, keeping older records as they replay), 3 (a LLMC's dice and its warning tell whether "?" are real), 4 (the case read bypasses the Right of Inspection), 5 (the Mopping Up proposal shows its result; also `ObstacleDummiesText`), 6 (R70.40 leaves out prisoners and Dummies), 7 (an uninspected stack's top counter: units aboard left out), 8 (an uninspected counter is "an uninspected unit", not "a concealed unit"), 10 (A12.151's loss needs a real concealed enemy unit).
- `fixes_referee_morale.md` 1 (a hidden or concealed unit named in a public `morale-check-resolved`).
- `fixes_referee_cc.md` 1 (the Ambush Withdrawal's plan shape tells what is under the "?").
- `fixes_ui.md` 14 (the hand-over's "A leader routed with a broken unit" line).
- `fixes_boundary.md` 16 (the same as `fixes_referee_sight.md` 4) and 2 (`GameView`'s `OutOfInspection` reads every enemy's Location when the set is empty).

## Round B: morale, rout, and Searching
- `fixes_referee_morale.md` 2 (the rout advice of `p70-rout-own-building`; also `fixes_table.md` 4: confirm from A10.51 and the p. 69 S3 case whether a legal step exists, and word the advice for each case, Failure to Rout or Surrender where none does), 3 (R70.47), 4 (R70.44: keep as built; check the text says why), 5 (R70.43).
- `fixes_table.md` 1 (R70.42), 3 (R70.43's refusal text), 5 (the Voluntary Rout LLMC's cause).
- R70.45: confirm the Search Casualty dr and Mopping Up's are always made, and word their texts so.
- `fixes_boundary.md` 5, 6, 8 (the Voluntary Break's effects, the rout planner's three decisions, the LLMC subjects into Rules; the index read for the subjects), 15 (the old who-sees members and their tests).

## Round C: Close Combat
- `fixes_referee_cc.md` 2 (R70.46), 3 (a berserk or reinforcing unit must attack enemy Infantry where a vehicle stands), 6 (`CaptureStands` for cause `passengers`: no uncaptured Passenger aboard), 7 (SW dropped before the withdrawer's elimination, both paths); 4 and 5 to backlog rows unless small.
- `fixes_ui.md` 2 (a laden unit offered destinations its proposal is refused for).
- `fixes_boundary.md` 11 (vehicle CC conditions into Rules), 13 and `fixes_table.md` 9 (one Rules predicate for an "enemy Location", read by `PlayRecords` and `BailOutPreview`), 14 (D5.42's exception through the Rules bar).

## Round D: the boundary, and memory and performance
- `fixes_boundary.md` 1 (MAJOR: a transient path for the views `UnitNames` builds over a history, no memo kept per revision; measure the retained heap of the cold open of `dl-tractor-works-02` before and after, in a Studio alone, and say the numbers), 3, 4, 7, 9, 10, 12, 17, 18, 19, 20 (as each proposes).
- `fixes_ui.md` 3, 12 (the CC panels' and Mopping Up's per-render scans: a memo by state and viewer).

## Round E: the page and the record lines
- `fixes_ui.md` 1, 4 (MAJOR: the Search button's `Busy`), 5 to 11, 13 (page tests for the uncovered controls).
- `fixes_table.md` 2 (record lines for a "?" given up, lost by an action, a HIP Gun placed), 6, 8.
- The notes' known items: the Records tab's list leaves out the LLMC line; the Interdiction's Heat of Battle line doubles its citation; the viewer's choice text ends "(ruling R5.8)." after R70.21's; the Search's Latest line leaves out the Casualty dr; a forced back's line reads "moves from [F5] to [F5]"; the enemy vehicle listed as "[F8]: vehicle"; `ChargeBarred` (R30.5) still says CC between Infantry beside a vehicle "is not built".

## Report (closing message, under 900 words)
1. What departs from this brief or a ruling, first.
2. For each finding of the round: fixed (the member and the change), or not and why (with its backlog row).
3. The build's result and each test run's counts, with the methods run.
4. For each fact or record changed: what an old record does.
5. A side's view: what the side that must not see is shown after the fix.
6. Memory and performance, and (Round D) the measurement's numbers.
7. How to see each visible fix in the Studio (game, phase, controls).
8. Anything seen outside the round.
