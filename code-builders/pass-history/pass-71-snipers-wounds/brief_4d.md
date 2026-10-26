# Increment 4d: the Sniper player's choices (71.1, part IV)

Read `brief_build.md` beside this file first; it binds you. Then the design's sections 2 to 7 and its sections for 4a to 4c, and `frame_state.md` section 3 (agent B's map of pass 70's pending-choice pattern and the five pause points). Read the diffs of the increments already committed, 4b's `sniper-check` choice above all: yours is the second Sniper choice kind and should share its plumbing.

## Pages
A14.2 and A14.21 (p. 82, every sentence that says "the Sniper player selects" or "chooses"), A14.31's repositioning (p. 82), A12.14's exception (p. 78).

## Build (ruling R71.12; R15.5's game picks replaced)
1. **A pending choice of kind `sniper`** for the Sniper's side, made only where more than one option exists, at five points of `Snipe`:
   1. on an effective dr of an unpinned Sniper, before the Random Location DR: attack, or forfeit and reposition (A14.2); on "reposition", the legal hexes by 4c's placement check against the state of the moment, and the counter moves there; the attack is forfeited;
   2. A14.21's equidistant hexes left after the lowest TEM;
   3. the Location, in a hex of more than one Location holding an eligible target;
   4. before Random Selection, the enemy Sniper counter, a Vulnerable Inherent Crew, or an unarmored vehicle present in the Location, or Random Selection (A14.2's EXC);
   5. which of several targets Random Selection gives takes the first dr (A14.2: "only one (Sniper's choice)").
2. **The resume:** the attack resumes on the answer from the rolls already recorded (the Sniper dr, the Random Location DR), draws its next dice in the answer's commit, may pause again, and then the triggers still to resolve follow in DR order (A14.1: no attack before the earlier ones are resolved). Keep a Sniper Check (4b) after the attack's own resolution.
3. **The choice and its options are public** (the dr is public); options are worded from the Sniper side's view, hex first (a concealed stack "a "?" stack in [G4]").
4. **While it waits nothing else may happen** (`ChoicePendingBar`), so no voluntary removal of a "?" comes between the Random Location DR and the relocation (A12.14's exception, p. 78): `ScenarioA1ConcealmentChoices.RemovalBar`'s note becomes a citation of the exception, and a test shows it.
5. **`MovementPlans.Review`** (MovementPlans.cs about :369) treats only `ConcealmentLoss` as the other side's turn: make it stop on any pending choice and show it as the choosing side's turn, with the page's hand-over saying whose answer is awaited.
6. **Older records** replay as they were: they hold R15.5's picks as recorded events.

## Not in 4d
The new triggers (4e). An empty unarmored vehicle stays ineligible (R71.4).

## Tests by method
Play: each of the five pauses offered only when it has more than one option, and its answer's effect; a forfeit that repositions (and is refused while pinned); two triggers in one step, the first paused, the second resolving only after the answer; the removal of a "?" refused while the choice waits; the move panel's turn while a Sniper choice waits. Rules for any new decision member. MapStudio: the choice panel's options in the Sniper side's view, and nothing of it hidden from the other side that it may know. Grep the Sniper tests (the ones that asserted R15.5's picks change as intended: name each).

## Studio game
`games_4d.py`: board 3, the German Sniper triggered with a Random Location DR that lands where (a) two hexes are equidistant with equal TEM, (b) a multi-level building hex has targets on two levels ([M5] and [L4] are one two-hex wooden building with a level 1), (c) Random Selection ties. Name the dice and the answers.
