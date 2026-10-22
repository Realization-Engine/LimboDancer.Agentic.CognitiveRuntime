# ASL Map Studio Enemy Names and Rout Order Plan

Branch `enemy-names-rout-order`, cut from `main` at ad8cc42e (2026-10-22). The user's words: "Option 2, and fix the rout order on a new branch", then "Try to implement this fix without my intervention."

## 1. What was found

In `dl-tractor-works-02`, Turn 3, the Russian Rout Phase, the German unit in [Y4] could not rout to [AA5]: the gate refused it with "The ATTACKER's 4-4-7 squad R35 must rout first (A10.5)". Two faults lay behind the stop.

- **The rout order was not said.** A10.5: broken units rout "ATTACKER first, one unit at a time". Three Russian units in [W4] (9-2 leader R1, 4-4-7 squads R4 and R5) still owed a rout. Yet the Russian view's end-of-phase line read "The German side still has a unit that must rout ... hand over to the German side first", and the German view offered routes the gate would refuse.
- **One unit had two names.** "R35" in the German view was a Russian 4-4-7 the Russian view calls R4 or R5; the Russian view's own R35 is a 5-2-7 in Melee in [X3]. Each view numbered the other side's units in the order it learned of them (pass 31c, design D11), so a message read in one view named a different counter in the other. The user: "That alone will lead to bugs."

## 2. Part 1: the rout order

- **Rules.** `ScenarioA1RoutCalculator.RoutPhaseHolder(attacker, defender, attackerOwes, defenderOwes)`: the side whose broken units hold the RtPh up, the ATTACKER first; the DEFENDER's units are read only when the ATTACKER owes none. `ScenarioA1SequenceCalculator.RoutPhaseEndBar` takes the fact that the proposer is the ATTACKER and still owes a rout, and then tells it to rout first ("rout them, then hand over to the German side") instead of to hand over.
- **Play.** `GamePlanner.AttackerRoutOwed(state)`: the ATTACKER's first unit that still owes a rout, the same answer the gate's rout order reads (moved out of `PlanRout`), kept per settled state in `PlayStepMemo`. `GamePlanner.RoutPhaseHolder(state, existing)` asks Rules with it and the Failure to Rout list.
- **Page.** The waiting signal of PR 29 (callout, banner tag, opening screen) covers the RtPh: the DEFENDER's view, while the ATTACKER owes, reads "Waiting for the Russian side: the Russian side routs first ... (A10.5)" and is offered no Rout panel; once the ATTACKER has routed, its view reads "Waiting for the German side: it still has broken units that must rout". A page opened on the game opens behind the side that holds the phase.

## 3. Part 2: the other side's units named by type and hex

Ruling (the user, 2026-10-22, option 2): no tag on the other side's units. A view's own units keep their tags by entry ("4-4-7 squad R4"); the adjudicator reads every unit by its owner's tag; a unit of the other side that the view holds by name reads by its counter and its Location at that moment ("4-4-7 squad in [W4]"). A record keeps the Location of the unit at its event. A unit the view does not hold still reads "a concealed unit".

- **Why.** One unit has one name in every view: a message, a record, or a refusal read in either view points at the same counter, and only the hex, which both sides know, says which. Nothing follows a unit through concealment, which a shared number would.
- **Lists.** The unit lists already head each group with its hex ("[W4]: ..."); an enemy unit's name there drops its own " in [W4]", so the hex is not said twice.
- **What goes.** The per-view numbering of the other side's units (`seen`, `seenCount`, `See`), and the parent-tag rules that served it.

## 4. Memory and performance

- `AttackerRoutOwed` is computed once per settled state (`PlayStepMemo`, volatile flag, benign race); the gate's rout order reads the same memo, so a DEFENDER's rout no longer scans every unit with the rout's searches when the state is settled.
- The page asks `RoutPhaseHolder` once per state (`routHolderMemo`), not per render.
- `UnitNames` drops one dictionary pair and a per-revision tag pass; an enemy unit's name reads its Location from the state at the revision, one lookup.
- No new cache outlives a state. No render-time side effect.

## 5. Tests

- Rules: the holder's order and laziness; the end-of-phase bar's two texts.
- Play: on the cut at revision 381 of `guards-dl-01` (the Russian RtPh with two Russian squads that must rout), the holder is the Russian side and `AttackerRoutOwed` is a Russian unit.
- Page: the RtPh test of pass 31 reads the waiting callout in the German view and the Rout panel in the Russian view.
- Names: `UnitNamesTests` reads the other side's units by counter and hex in every view and record, own units by tag.
- The DEFENDER's view with no Rout panel while the ATTACKER owes is checked in my Studio on a copy of `dl-tractor-works-02`; the page tests' synthetic board (3 by 2 hexes) holds no layout in which both sides owe a rout without surrendering.

## 6. Build outputs outside the repository

My Studio's build (`asl-gate-work/studio-6670`) and the filtered test runs (`asl-gate-work/test-classes-bin`) are registered with `outside_outputs.sh`; the gate's `ci` stage removes them once CI passes. Last task: `outside_outputs.sh list` is empty after the merge.
