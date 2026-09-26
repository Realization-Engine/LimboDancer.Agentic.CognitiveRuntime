# ASL Unit Infantry OVR Design

**Status:** Proposed. Designed before code, on `feature/asl-unit-step11-design`; built in the order of section 10.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 11, and acceptance scenarios U11 and U12 (section 14). Dice: [.NET Dice Roller Requirements](<NET Dice Roller Requirements.md>), DICE-07 to DICE-12.

**Related documents:**

- the [Scenario A1 OVR NTC Review](<Scenario A1 OVR NTC Review 2026-09-26.md>) and its package `scenario-a1-concealment-ovr-ntc`, which decide every outcome this step commits;
- the [ASL Unit Random Selection and Declined OVR Design](<ASL Unit Random Selection and Declined OVR Design.md>) (step 9), whose dice store, open attempts, and declaration action this step extends;
- the [second-defender execution boundary review](<Scenario A1 Second Defender Execution Boundary Review 2026-09-24.md>), whose separate return aggregate this step retires.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

After an entry reveals a lone concealed SMC, step 9 lets the attacker decline an Infantry OVR and refuses an election. This step lets the attacker elect one when another concealed non-Dummy unit is in the location:

- **The NTC is rolled first** (A4.15, p. 49).
- **A failure** forces the mover back with the ordinary 2 MF. Nothing further is revealed (U11).
- **A pass** is followed by a Random Selection roll among the remaining concealed units (A.9, p. 43). They are revealed, the OVR is denied, and the mover is forced back with the ordinary 2 MF (U12).

An election against a lone SMC stays refused. The step also retires the Execution adapter's separate return aggregate, so the game log is the only record of unit state.

## 2. Principles

Everything in the step 9 design, section 2, still holds: every branch is decided before any roll, the rolls live in the commit, and rolls are public while identities are not. This step adds:

- **Rolls on demand.** One commit may draw several rolls. Each is drawn only when the outcome so far needs it. A roll for a branch not taken is never drawn, so the log never holds a roll that decided nothing.
- **A check is recorded with its arithmetic.** An NTC is recorded with the roll it used, the unit's Morale Level, and each DRM. Replay recomputes the result and refuses a record whose arithmetic disagrees.

## 3. Rolls on demand

- **The planned roll changes shape.** It becomes `PlannedRoll(Purpose, Build)`. `Build` takes a draw function (`Func<RollRequest, RollResult>`) and returns the complete batch of events. It calls the draw function once per roll it needs, in order, and it stays a pure function of the results it is given.
- **The store.** `IGameStore.AppendRolled` passes `request => roller.Roll(request)` to `Build`, inside the per-game lock and after the attempt and revision checks. Nothing else changes: replay before the write, a Replay for a committed attempt without drawing, a Stale revision without drawing, and nothing written when the batch does not replay (DICE-08, DICE-10, DICE-11).
- **Roll ids.** Roll ids are `{attempt}-roll-{n}`, numbered in draw order, so each `dice-rolled` event has a stable identity.
- **Existing plans.** The Random Selection plan of step 9 moves to the new shape with one draw. Its events and ids are unchanged.

**As built** (part 1).
- `PlannedRoll(Purpose, Build)` takes the draw function, and `AppendRolled` passes `roller.Roll`.
- `task-check` is replayed with UNIT-STATE-022 as section 4 states.
- `OpenAttempt` records `TaskCheckPassed` and `SecondSelection`. Every other forced back after an election is refused with UNIT-STATE-021, and a selection after an election is refused with UNIT-STATE-020 unless it follows a passed NTC.
- Tests:
  - Units, `TaskCheckEventTests`: 12 cases.
  - Store, `DiceStoreTests`: two cases in which the second roll is drawn only when the first calls for it, and a replay draws nothing.

## 4. The task check

`task-check` (new) records one Task Check:

| Field | Meaning |
|---|---|
| `id` | The unit that took it |
| `roll` | The `dice-rolled` event it used: two dice with six sides |
| `purpose` | `ovr-ntc` |
| `moraleLevel` | The unit's Morale Level: its printed morale from the reviewed catalog, since an unbroken unit uses the front face |
| `modifiers` | Each DRM, with its source: `B23.3` for the building TEM (+3 stone, +2 wooden) |
| `finalDr` | The sum of the dice and the DRM |
| `passed` | Whether the final DR is at or below the Morale Level (A10.1, p. 65; the NTC entry, p. 30) |

The projector checks every field that can be recomputed, with UNIT-STATE-022:

- the roll is recorded, with two dice of six sides;
- the final DR is the sum of the dice and the modifiers;
- `passed` agrees with the final DR and the Morale Level;
- the Morale Level equals the unit definition's printed front-face morale;
- the unit's attempt has an elected declaration and no earlier task check.

## 5. The election

`asl.game.declare-overrun` with `elect` is planned in `GamePlanner.PlanDeclareAsync`. Before any roll it requires:

- a pending attempt by the unit that revealed exactly one SMC (step 9);
- at least four MF left: the allowance of step 8 less the MF spent. The attempt's own 2 MF are not yet spent. A4.15 doubles the cost of entry to four (A12.15: "if possible");
- at least one other enemy unit in the location that is still concealed and a non-Dummy MMC or SMC. Without one, the SMC is alone and the election is refused with the generic reason (section 6);
- a clear return, the board 01 binding, and a mover that is no A4.14 exception, as for every forced back;
- **Definitive conclusions from the OVR NTC package** for both reachable branches: `A1-ovr-ntc-failed` and `A1-ovr-ntc-passed-second-defender-revealed`. Each is read through a live observation provider over a candidate state for that branch. Because both are checked before the roll, whichever branch the dice choose has a reviewed resolution.

The plan is Ready with a planned roll. Its build function:

1. adds `overrun-declared` with `elected`;
2. draws the NTC (two dice) and adds its `dice-rolled` event and the `task-check`;
3. on a **failure**, adds `entry-forced-back` with the ordinary 2 MF. Its causes are the attempt and the declaration.
4. on a **pass**:
   - draws one die per remaining concealed unit, in unit id order;
   - adds that `dice-rolled` event, a `random-selection` visible to the defender's side, and a reveal for each unit with the highest die;
   - adds `entry-forced-back` with the ordinary 2 MF.

**Projector changes.** `OpenAttempt` gains the task-check result. A forced back after an elected declaration is allowed when either:

- the task check failed; or
- the task check passed and a second selection has revealed every unit it chose.

One further selection is allowed on an attempt after a passed task check, and its `Revealing` replaces the first set, which is already revealed. Every other forced back after an election stays refused.

**Observation provider.** The Scenario A1 project gains `ScenarioA1OvrNtcObservationProvider`, with its snapshot record and source interface. It follows the other providers: it checks the board 01 terrain evidence and maps the snapshot to the package's exact facts. The Play project gains `LiveOvrNtcSnapshotSource`. The package's manifest, matrix, and digests are unchanged.

**As built** (part 2).
- `GamePlanner.PlanElectAsync` takes the election after the checks it shares with a decline: the pending attempt, the lone revealed SMC, the board 01 binding, and a clear return. On the synthetic board of the Studio tests, an election is therefore refused as outside the reviewed board, as a decline is.
- An election with fewer than four MF left is refused with `play.election-unavailable`, which cites the `A1-ovr-ntc-mf-insufficient` conclusion.
- Both reachable branches are read Definitive through `ScenarioA1OvrNtcObservationProvider` over `LiveOvrNtcSnapshotSource`, before the roll.
- Every event of an election carries the OVR NTC package as its rule package. The Random Selection after a pass draws one die per remaining concealed MMC or SMC, in ordinal id order.
- Tests:
  - Play, `DeclareOverrunTests`: U11 (a failed NTC, then a replay that draws nothing), U12, the wooden building's TEM of 2, and fewer than four MF. The lone SMC refusal stays in the existing test.
  - Scenario A1, `ScenarioA1OvrNtcObservationTests`: each of the five cases, and 13 snapshots outside the scope or the reviewed cases.
  - Map Studio, `PlayPageTests`: the NTC arithmetic. The Elect button's commit is covered by the Play tests, since the page tests run on a synthetic board.

## 6. The lone SMC and the probe

An election against a lone SMC is refused with the generic "the adjudicator cannot resolve" reason. The refusal, and equally an accepted election, tells the attacker whether another concealed unit is in the location. The requirements record this as a known limitation, accepted while the Studio's single user acts for both sides. Multi-user play must close it first. Among the ways to do so: elections could be offered only once A4.151 and A4.152 are reviewed, or an unresolvable election could halt for an adjudicator.

## 7. Map Studio

- The "elect the OVR" button commits when the election is allowed, and shows the refusal otherwise.
- The rolls list shows the NTC with its arithmetic, for example "OVR NTC: 3, 5 + 3 (TEM) = 11 against morale 7: failed", and then any Random Selection. It still shows the die-to-unit mapping only to the adjudicator and the defender's side.

## 8. Retiring the separate return aggregate

The Execution adapter keeps the second-defender return in its own journal, apart from the game log. Live games now carry that return. Part 3 removes:

- the `LimboDancer.Domains.Asl.Execution` project, with its return action, gate binding, and verified conclusion source;
- `ScenarioA1SecondDefenderReturnTransition`, `ScenarioA1ReturnSimulationStore`, and `ScenarioA1JournalReturnStore` from the Scenario A1 project;
- the runtime Host's reference to the Execution project, its registration in `ServiceCollectionExtensions`, and `ScenarioA1ReturnHostConstraintEvaluator`;
- the tests of those parts: the return transition, the simulation and journal stores, the gate binding, and the Host registration.

`ProductionDependencyTests` in the runtime's architecture tests then asserts that the runtime references no ASL project (ASL-UNIT-002). The second-defender execution review gains a note that its return aggregate was retired at step 11, and it stays as the record of that work. This part builds and tests the runtime solution as well as the ASL solution.

## 9. Tests

**Units:**
- `task-check` round-trips.
- Replay refuses each arithmetic error, a morale that is not the printed one, a task check without an elected declaration, and a second task check.
- Forced back after an election is allowed only in the two reviewed branches.
- A second selection is allowed after a pass and refused otherwise.

**Store:** a build function that draws twice records two rolls in order. One that draws once records one. A replay draws nothing.

**Play**, with fixed rolls from the Dice seam:
- U11: a failed NTC reveals nothing further and forces the mover back.
- U12: a passed NTC is followed by a Random Selection that reveals the other squad, and the mover is forced back.
- A repeated confirmation returns the recorded rolls.
- An election with fewer than four MF is refused. So is an election against a lone SMC, with the generic reason.
- Stone and wooden buildings give their TEM.
- A replay never calls the roller.

**Scenario A1:** the OVR NTC observation provider maps each reachable branch, and refuses a snapshot outside the package's scope.

**Map Studio:** the Elect button commits, and the rolls list shows the NTC arithmetic.

**Runtime (part 3):** the Host builds without the Execution project, and the architecture test holds.

The LF clone and Docker verify each part as before. Part 3 also runs the runtime solution's tests.

## 10. Build order

1. `feature/asl-unit-rolls-on-demand`: sections 3 and 4, with the projector changes of section 5.
2. `feature/asl-unit-ovr-election`: the rest of sections 5 to 7, and U11 and U12.
3. `feature/asl-unit-retire-return-aggregate`: section 8.

This design is merged first, on its own branch.

## 11. Not in this step

- The lone SMC's options and immediate CC (A4.151, A4.152).
- The leader's exemption from the NTC.
- LOS Hindrance in the location.
- Dummies.
- Multi-user play, and closing the election probe.
