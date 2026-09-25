# ASL Unit Random Selection and Declined OVR Design

**Status:** Proposed. Designed before code, on `feature/asl-unit-step9-design`; built in the order of section 12.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 9, and acceptance scenarios U9 and U10 (section 14). Dice: [.NET Dice Roller Requirements](<NET Dice Roller Requirements.md>), DICE-07 to DICE-12.

**Related documents:**

- the [Random Selection reveal review](<Scenario A1 Random Selection Reveal Review.md>), which admits the subset this design builds;
- the [ASL Unit Occupied and Concealed Entry Design](<ASL Unit Occupied and Concealed Entry Design.md>) (step 8), whose entry action, disclosure, and forced back this step extends;
- the [.NET Dice Roller Design](<NET Dice Roller Design.md>), whose sections 3 and 4 set the commit boundary this step implements.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

Step 8 refuses an entry into a location holding several concealed units, or a lone concealed SMC. This step resolves both:

- **Several concealed units.** A Random Selection roll (A.9, p. 43) decides which unit or units are revealed (A12.15, p. 78). The roll is drawn by the system inside the commit, recorded as an event, and never drawn again. Any revealed non-Dummy MMC, or more than one revealed SMC, forces the mover back in the same commit (U9).
- **A lone revealed SMC.** The attempt stays open as a pending OVR declaration. A new action records the attacker's choice:
  - declining commits the forced back, through the reviewed delegation (U10);
  - electing is refused with a generic reason until step 10.

## 2. Principles

Everything in the step 8 design, section 2, still holds. This step adds:

- **Every branch is decided before the roll.** The planner works out, before any die is drawn, what each possible outcome commits. It refuses an entry if any outcome would lead to an unreviewed state. The roll then only selects a branch, so no result is ever discarded (Random Selection review).
- **The roll lives in the commit.** Planning, previews, the gate, and replay never draw (DICE-08). The store draws once, inside its per-game lock, after the attempt and revision checks, then builds and validates the events and writes them.
- **Rolls are public; identities are not.** Every perspective sees the dice values. Which die belongs to which concealed unit is visible only to the defender's side and the adjudicator. The reveal events then show the attacker the units that were revealed.

## 3. Dice in the game store

- **Planned roll.** `GamePlan` gains an optional `PlannedRoll(Purpose, RollRequest Request, Func<RollResult, IReadOnlyList<GameEvent>> Build)`.
  - A plan with a planned roll is Ready with no events.
  - `Build` is a pure function from the roll to the complete batch of events. It holds no reference to the store.
- **`IGameStore.AppendRolled`.** The new operation takes the scope, label, expected revision, the attempt's first event id, the planned roll, a roller, and the replay function. Under the per-game lock it:
  1. reads the log;
  2. returns Replay if the first event id is already there, without drawing;
  3. returns Stale if the revision moved;
  4. draws once with `DiceRoller.Roll`;
  5. builds the events and checks that the first one carries the expected id;
  6. replays the whole log with them, returning Invalid on errors;
  7. writes through the temporary file and move, as `Append` does.

  A failure before the write leaves no trace of the draw (DICE-11).
- **The roller.** `GamePlay` and `GameActionExecutor` take a `DiceRoller`, which defaults to the production roller. Tests pass one built on the internal seam, which the Dice project exposes to the Play tests through `InternalsVisibleTo`, or a roller that fails if it is called.
- **Reused attempts (DICE-10).** An attempt id already in the log is a Replay only when its recorded inputs match: for an entry, the `entry-attempted` unit and target. Otherwise the plan is refused with `play.attempt-reused`.
- **Executor.**
  - `GameActionExecutor` calls `AppendRolled` when the plan has a roll, and `Append` otherwise.
  - It reads the effect back from the log and reports the recorded values from the log, never from memory.
  - The effect check covers the pending branch: the revealed SMC is known, and the attempt is open.
- **As built** (part 1).
  - `PlannedRoll`, `IGameStore.AppendRolled`, and the executor wiring are in the Play project. `Append` and `AppendRolled` share one commit path under the per-game lock, and `AppendRolled` draws only after the replay and revision checks.
  - `dice-rolled` is replayed with UNIT-STATE-019. Its checks: the source is `system`, the count is 1 to 100, the sides are at least 2, the count and the values agree, every value is in bounds, and each roll id is recorded once.
  - An entry attempt reused with other inputs is refused with `play.attempt-reused`.
  - Tests: `DiceEventTests` (Units, 9) and `DiceStoreTests` (Play, 7) cover:
    - one draw per commit;
    - a replay without a draw;
    - a stale revision without a draw;
    - a generator failure that writes nothing;
    - batches that do not replay or do not start the attempt;
    - four concurrent confirmations committing one roll;
    - a reused attempt.

    The executor's rolled path is exercised in part 2, where the first action with a roll exists.

## 4. Events

| Event | Payload | Visibility | Projection |
|---|---|---|---|
| `dice-rolled` (new) | `roll` (the attempt id and the roll's ordinal), `purpose` (`random-selection`), `count`, `sides`, `values` in order, `source` (`system`), `actor` (the principal) | Every perspective | No state change. The replay checks that count and values agree, that each value is in bounds, and that the roll id is unique. |
| `random-selection` (new) | `roll`, and `subjects`: the unit ids in die order | The defender's side | No state change. The replay checks that the roll exists, that the subject count equals the roll's count, and that each subject is at the attempt's target. |
| `overrun-declared` (new) | `id`, `attempt`, `choice` (`declined` or `elected`) | Every perspective | Records the choice. The replay checks that the attempt is open and pending, and that the unit is the attempt's. |

The events already in use keep their meaning:

- `entry-attempted`;
- `conditions-changed`, for the placements beneath a "?" and for the reveals;
- `entry-forced-back`, whose causes also name the declaration after a declined OVR.

**Open attempts become state.** `GameState.OpenAttempts` lists each open attempt with its unit, its target, and whether a declaration is pending. It replaces the projector's private dictionary. While an attempt is open:

- the phase may not change;
- its unit may not move or attempt another entry.

Each of these is refused with UNIT-STATE-018 on replay, and by the planner before that.

**Die order is deterministic.** The subjects are sorted by unit id, so a replay can recompute the revealed units from the recorded values, and checks that the reveal events match them.

## 5. The entry action

`asl.game.enter-building` keeps its routing (step 8 design, section 6) and gains two routes:

| Target location | Route | Commits |
|---|---|---|
| Two or more enemy units, all concealed or hidden, all non-Dummy MMC or SMC, no SMC hidden, clear return | `RandomSelection` | `entry-attempted`; a placement beneath "?" for each hidden unit; `dice-rolled`; `random-selection`; one reveal per revealed unit. Then, by branch, `entry-forced-back`, or nothing more (pending). |
| Exactly one enemy SMC, concealed, clear return | `LoneSmc` | `entry-attempted`, its reveal, and nothing more (pending) |
| One concealed or hidden enemy MMC | `Concealed` (step 8) | Unchanged |

The planner checks the forced-back branch once, before the roll: it asks PostReveal over a candidate state in which some non-Dummy unit is revealed, since the conclusion's facts do not depend on which one. The branch is committed only if that conclusion is Definitive. A hidden SMC, alone or among others, is refused as outside the reviewed cases.

Disclosure follows step 8, section 7. The proposal is withheld, and the side sees the roll and the reveal only once they are committed.

## 6. The OVR declaration

`asl.game.declare-overrun` (new) is a registered action with the permission `asl.game.play`. It is Irreversible and needs confirmation. Its arguments are the game, attempt id, expected revision, unit, and `choice`.

- **Declined.**
  1. The planner builds the ConcealedSmcOverrun snapshot for the pending attempt, with `OverrunElection = Declined` and every later fact unresolved.
  2. It requires the resolver's delegation reason, `asl.a1.ovr.declined-use-exact-post-reveal-package`.
  3. It then asks PostReveal with `overrunElection = none`, the delegation's reading (Random Selection review).
  4. On a Definitive conclusion it commits `overrun-declared` and `entry-forced-back`.
- **Elected.** Refused with the generic "the adjudicator cannot resolve" reason. Nothing changes and the attempt stays pending. Because every election is refused, the refusal discloses nothing.

The phase cannot advance while a declaration is pending. The page shows why.

## 7. Map Studio

- **Rolls.** The proposal result and the game's recent events show each roll: its purpose and its values. In the adjudicator's view and the defender's view they also show which die belongs to which unit.
- **Pending declaration.** While an attempt is pending, the Play page shows it, with Decline and Elect buttons that propose `asl.game.declare-overrun`, and explains why the phase cannot advance.
- **Placements.** Setup placements already set every condition the planner reads.

## 8. Tests

**Dice project:** `InternalsVisibleTo` for the Play tests, so they can use the deterministic seam.

**Units tests:**
- The three new events round-trip.
- Replay bounds and count checks for rolls.
- The subject checks.
- Open attempts block phase changes and the unit's moves.
- A declaration needs a pending attempt.
- The revealed set is recomputed from the recorded values on replay.

**Play tests:**
- U9: two concealed squads.
  - A fixed roll reveals one, and the mover is forced back.
  - The log holds the roll.
  - Replaying the game with a roller that throws if called reproduces the state.
  - Confirming the same attempt again returns the recorded values.
- The branches:
  - a tie revealing both squads;
  - a squad and an SMC;
  - two SMC revealed, which forces the mover back;
  - one SMC revealed, which leaves a pending declaration.
- U10:
  - a lone concealed SMC is revealed and pending;
  - the phase cannot advance;
  - an election is refused with nothing changed;
  - a decline commits the forced back, citing both packages.
- Refusals: a hidden SMC, alone or among others; a known unit mixed with concealed ones.
- Stale revisions.
- A reused attempt with different inputs.
- Concurrent confirmations, which commit one roll.
- A failure before the write, which leaves nothing in the log.
- A failure after the write, where a retry returns the recorded roll.

**Map Studio tests:** the roll and the pending declaration on the Play page, in a side's view and the adjudicator's.

The embedded-digest tests run in the LF clone, and the Linux check in Docker, as for step 8.

## 9. Not in this step

- Dummies.
- A hidden SMC.
- An elected OVR, its NTC, the second defender, and the Execution adapter's return aggregate (step 10).
- Follow-on fire.
- Defender choices of any kind. The only choice here, the OVR, is the attacker's.

## 10. Risks

- **Coupled plan and store.** The build function joins the planner's reasoning to the store's commit. Keeping it pure, and replaying its events before the write, is what keeps the store free of rules.
- **Disclosure through open attempts.** A pending declaration is visible to both sides, because the reveal is public.

## 11. Open questions

None blocking. The subject order (sorted by unit id) is a convention of this design, not of the rules. Any fixed order satisfies A.9.

## 12. Build order

Each part goes on its own feature branch, and each compiles, passes all tests, and is verified in the LF clone and in Docker before merging.

1. `feature/asl-unit-dice-store`: section 3, and the `dice-rolled` event and its replay checks.
2. `feature/asl-unit-random-selection`: sections 4 and 5, with open attempts as state and the `random-selection` event.
3. `feature/asl-unit-declined-ovr`: sections 6 and 7, and the tests of section 8 that remain.

This design and the Random Selection review are merged first, on their own branch.
