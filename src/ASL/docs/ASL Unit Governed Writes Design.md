# ASL Unit Governed Writes Design

**Status:** Built: setup, the sequence of play, and one reviewed transition (entry into an empty building), each committed only through the Execution Gate, with a Play page in Map Studio

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 7: ASL-UNIT-042 (writes are governed) and ASL-UNIT-050 (a chosen live source), with decision D2 (section 12). The governed action path is ASL-RD-015.

**Related documents:** the [ASL Unit State Model Design](<ASL Unit State Model Design.md>) for games, events, and replay; the [ASL Unit Read Contract Design](<ASL Unit Read Contract Design.md>) for the map read API and the facts a case reads; and the [Decision Memo for D1 to D4](<ASL Unit Model Decisions D1 to D4.md>) for the D2 options.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

Until this step every game was a synthetic fixture, read but never changed. This step makes a game that changes, and it changes only one way: a registered action, checked by the Execution Gate against the current game, boards, and catalog, and committed after the user confirms it. It delivers:

- the live game source chosen for D2: games set up and played in Map Studio (section 3);
- a new project, `LimboDancer.Domains.Asl.Play`, holding the store, the actions, the planner, and the gate wiring;
- three registered actions: setup, advance the phase, and enter an empty building (section 5);
- the first reviewed transition, which commits only on a Definitive conclusion of the Scenario A1 first case (section 8);
- a propose-then-confirm flow over the runtime gate, with an audit trail (section 9);
- a Play page in Map Studio, and live games in the Game states page and board viewer (section 10).

## 2. What the state model gains

- `GameEventWriter` writes a game record in the form `GameEventReader` reads, so a record round-trips.
- `GameStarted` carries `SpecialRules`; `GameState` carries `SpecialRules`, `FirstSide`, and `Source`.
- `GameProjector.Project` takes the accepted live sources. A game that is not synthetic is refused with UNIT-STATE-017 unless its source is one of them. Every other reader passes none, so a live game can never be passed off as a fixture or read as one elsewhere.

## 3. D2: the live game source

Decided on 2026-09-26: a Map Studio setup and play editor, as the decision memo recommended. A VASL saved-game import may follow later as a read-only source; it is not part of this step.

A live game belongs to one tenant, has the source `map-studio`, and is stored as `{boards folder}/units/live/{tenant}/{game}.game.json`. `FileGameStore` writes through a temporary file and a move, under a lock per game, and checks the expected revision before it appends. An append returns Committed, Replay (the same attempt was already committed), Stale, or Invalid (the events do not replay); only Committed changes the file.

The Studio has one local user, `studio-user`, in a fixed tenant, holding both permissions.

## 4. Principles

- **Plan from server state.** `GamePlanner` re-reads the game, the boards, and the catalog every time. The caller supplies only the action, its arguments, and the revision it saw. The gate and the executor each plan afresh, so nothing the caller computed is trusted.
- **Replay before commit.** Every plan replays the whole log with the new events, against the exact board versions in play. A plan whose events do not replay is refused.
- **Commit only what is reviewed.** A transition commits only when a reviewed resolver returns a Definitive conclusion. Indeterminate and Abstained conclusions are refusals, and the page shows why.
- **Idempotent by attempt.** Each proposal carries an attempt id; its events are `{attempt}-{n}`. Confirming the same attempt twice is a Replay, not a second write.

## 5. The actions

All three are Irreversible and Internal, with idempotency keys required, so the gate asks for confirmation before any of them runs.

| Action | Permission | Precondition | Commits |
|---|---|---|---|
| `asl.game.setup` | `asl.game.setup` | Setup is open | `game-started` (first call only) and one `instance-created` per placement |
| `asl.game.advance-phase` | `asl.game.play` | The game replays | One `phase-changed` |
| `asl.game.enter-empty-building` | `asl.game.play` | The reviewed first case, named by the Scenario A1 package identity | One `instance-moved` with 2 MF and the package as its rule package |

## 6. Setup

Setup is open while every event is `game-started` or `instance-created`; the first phase change closes it (`play.setup-closed`). A new game needs:

- two sides and the side that moves first (`play.sides`, `play.two-sides`);
- a published catalog (`play.catalog`); the synthetic catalog is refused;
- boards that read as Verified or AuthoredValid (`play.boards`, ASL-MAP-044).

Each placement must name a definition in the catalog and a location that resolves on a board in play. A concealed or hidden placement is visible only to its own side, so the other side's projection shows at most a sealed presence (A12, pp. 76 to 80).

## 7. The sequence of play

Advance follows A3 (p. 47): RPh, PFPh, MPh, DFPh, AFPh, RtPh, APh, CCPh. After the CCPh the other side becomes the phasing side; the turn increments when the first side is phasing again. A new phase resets MF spent, as the state model already does.

## 8. The reviewed transition: entry into an empty building

The entry case is the Scenario A1 first case, whose reviewed decision permits a Good Order Infantry squad, during its MPh, to enter an adjacent empty ground-level ordinary wooden or stone building for 2 MF. The planner derives the nine facts the reviewed `ScenarioA1ConclusionResolver` asks for from the game and the board, then asks it. A fact is true, false, or unknown; any unknown fact makes the case Indeterminate.

| Fact | Derived from |
|---|---|
| `isKnownGoodOrderInfantrySquad` | The unit is a squad, Good Order, and known (not concealed or hidden) |
| `isAttackerMovementPhase` | The phase is the MPh and the unit's side is phasing |
| `canMoveThisPhase` | Not broken, TI, or held in Melee (A4.1, p. 48). Fire is not yet an action, so no live unit has fired |
| `isAdjacentGroundLevelOrdinaryBuilding` | The target is one hex away, at level 0, and its terrain is a wooden or stone building of any height, as VASL names them (B23.1, p. 134) |
| `isDestinationKnownEmpty` | No other unit is in the target location. The planner reads the whole game, concealed and hidden units included, never a side's view |
| `hasNoRoadBypassElevationOrAdditionalTerrain` | Same base level; the crossed hexside has no hexside terrain, road, cliff, slope, or embankment; neither location is in a depression |
| `hasEnoughMovementFactors` | A Good Order MMC has 4 MF, 3 if Inexperienced (A4.11, p. 48), which the model does not track: 1 MF or less spent is true, 3 or more is false, and 2 is unknown |
| `isBelowStackingLimit` | The target location is empty, so stacking holds |
| `hasNoSpecialRuleOrOtherModifier` | The game names no special rules |

On a Definitive conclusion the plan commits `instance-moved` with 2 MF, and the event's rule package is the Scenario A1 package identity, so the conclusion that allowed it is traceable.

## 9. The gate, confirmation, and audit

- `GameConstraintEvaluator` plans the action; a Ready or Replay plan is Satisfied and carries the game's version key and the expected revision. Stale and refused plans fail the check with the plan's reasons.
- The runtime has no protocol for confirming an Irreversible action, so `ConfirmationPolicy` wraps the risk policy and allows exactly one confirmed correlation. `GamePlay.ProposeAsync` returns NeedsConfirmation; `ConfirmAsync` with the same correlation runs the gate again and, if it authorizes, the executor.
- `GameActionExecutor` checks the token's validated revision, plans again, appends at that revision, and reads the effect back. It reports `play.committed`, `play.replay`, `play.gate-state-stale`, or `play.effect-readback-failed`.
- Every run goes through `AuditedActionExecutor`; the Studio's `FileAuditSink` appends each audit event to `live/audit.jsonl`, and the Play page shows the latest lines.

## 10. Map Studio

- **Play** (`/games/play`): start a game (label, board, sides), stage placements (concealed or hidden), and propose setup; then propose a phase advance or an entry. Each proposal shows the gate's outcome and reasons; an entry also shows the nine facts and the conclusion. Confirm commits; Cancel drops the proposal. The page shows the units and the audit tail, and links to the game on its board.
- **Game states** and the **board viewer** list live games as `live:{game}`, labelled "(live)", and replay them against the Studio's real boards. The viewer's footer says a live game changes only through the governed path.

A live test on the real VASL board 01 found that its buildings are named with their height ("Stone Building, 2 Level"), which the first version of the fact did not accept; the fact now covers every height, since the reviewed case concerns only the ground level.

## 11. Tests

- `LimboDancer.Domains.Asl.Play.Tests` (25): confirmation before commit, the live source check, visibility of hidden and concealed placements, setup refusals, the sequence of play over two full turns, setup closing, the Definitive entry and its event, buildings of every height, a replayed attempt, refusals for each false fact, unknown facts, staleness, invalid appends, and the action registrations.
- `PlayPageTests` in the Map Studio tests (4): setup through the page, a Definitive entry through the page on a verified synthetic board with a painted building, a refused entry with distinct reasons, and a live game in the game library.

## 12. Not in this step

- Fire, Opportunity Fire, and every other transition; each needs its own reviewed case first.
- Scenario OB and SSR checks at setup, and special rules beyond recording their names.
- Inexperienced status, so an entry after exactly 2 MF stays Indeterminate.
- Multi-user play and per-side sign-in; the Studio's single user acts for both sides.
- A VASL saved-game import (D2's later, read-only source).
