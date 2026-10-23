# ASL Map Studio CC Declarations Plan

Branch `cc-both-sides-declare`, cut from `main` at 33bf147f (2026-10-22). The user's words: "Build the fix, and take back both rounds - without my intervention", and, while it was built, "we need an escape hatch as well: a 'Clear All' button or link to reset the event so we don't get stuck half-way before resolution."

## 1. What was found

In `dl-tractor-works-02`, Turn 4, the German CCPh, the rounds in [V2] and [X3] were resolved with the German attacks alone: the eight Russian units in [X3] were eliminated without attacking back. A11.11: "CC is usually simultaneous, so both sides attack the other even if one or both is thereby eliminated." The page let a side's view declare only its own side's attacks (ruling R5.20), and "Propose: resolve CC" resolved the round at once, so a round resolved from one side's view never held the other side's attacks. The German attack itself was correct (18 FP against 32 at 1-2, Kill Number 4; -3 leadership, -1 heroic; -1 vs CX, -4 vs the overstacked Location). Both rounds were taken back at the user's word (revisions 1011 to 1028 removed; backup kept).

## 2. The fix

- **Rules.** `ScenarioA1CloseCombatRules.UndeclaredSideBar`: a round that names the sides that declared is refused while a side that may declare has not, ATTACKER first (A11.11, the sequence of play 8.24). A side may declare no attack. A caller that names no declared sides (an MCP caller, the adjudicator's view, the existing tests) declares every side in one place, as before.
- **Play.** `PlanCloseCombat` asks it with `DeclaringSides`; not for the prisoners' round (A20.55) or an OVR's CC, which one side declares.
- **Page.** In a simultaneous round, in a side's view: the first side (the phasing side) declares its attacks, stacking, withdrawals, and infiltrations, and "Declare ... and hand over" keeps them (`LivePlay.Declarations`) and hands the screen to the other side; the other side's view opens on that Location, reads the first side's attacks, declares its own (or none), and resolves the round with every attack. Before the first side declares, the second side's view says it waits and is offered no attack. Once the first side has declared, the waiting signal (callout, banner tag, opening screen) points at the second side. The adjudicator declares both sides at once, as before.
- **The escape hatch.** "Clear all declarations" in either view drops the kept declaration and the view's draft; nothing is recorded until the round is resolved, so the round starts over, the ATTACKER first.

## 3. Memory and performance

The store holds one immutable record per game; a read compares the revision and the Location, so a declaration made at another revision is absent and nothing has to drop it. The page reads it per render with one dictionary lookup. Its cache statement is on `CloseCombatDeclarations`.

## 4. Tests

- Rules: `ASimultaneousRoundWaitsForEverySideThatMayDeclare`.
- Play: the U33 round refused with one declared side, resolved with both.
- Page: the U33, capture, and withdrawal rounds walk the two steps; `AFirstSidesDeclarationWaitsForTheOtherAndClearAllStartsTheRoundOver` covers the wait, the opening screen, and the escape hatch; icon coverage for the new buttons.
- Studio: on a copy of the game at revision 1010, the German side declared the [X3] attack and handed over, the Russian side read it, declared its own, and the round rolled both (the Russian attack at 3-2, Kill Number 6, +1 CX, +4 by the overstacked Location: no effect); "Clear all declarations" checked on [V2].

## 5. Backlog

Make `declaredSides` required of every caller, and update the Play tests that resolve a round in one call (they declare every side at once, as the adjudicator does).

## 6. Build outputs outside the repository

My Studio's build (`asl-gate-work/studio-6670`) and the filtered test runs (`asl-gate-work/test-classes-bin`) are registered with `outside_outputs.sh`; the gate's `ci` stage removes them once CI passes.
