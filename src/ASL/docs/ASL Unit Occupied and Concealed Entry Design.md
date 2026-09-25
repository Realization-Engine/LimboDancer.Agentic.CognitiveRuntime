# ASL Unit Occupied and Concealed Entry Design

**Status:** Proposed. Designed before code, on `feature/asl-unit-step8-design`; built in the order of section 13.

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 8, and acceptance scenarios U4 to U8 (section 14). Also ASL-MAP-081 in the [Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>).

**Related documents:**

- the [ASL Unit Governed Writes Design](<ASL Unit Governed Writes Design.md>), whose store, planner, gate wiring, and Play page this step extends;
- the [ASL Unit Read Contract Design](<ASL Unit Read Contract Design.md>) for `ReadCase` and the map read API;
- the [post-reveal forced-back execution boundary review](<Scenario A1 Post-Reveal Forced-Back Execution Boundary Review.md>), which admits the transition in section 8;
- the [post-reveal review](<Scenario A1 Concealment Post-Reveal Review.md>) and the occupied-case package, which are used as reviewed and not changed.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

Step 7 lets a squad enter an empty building. This step lets it try to enter a building that is not empty, and answers from the reviewed packages:

- **Known enemy.** An entry into a location holding a known, unconcealed enemy unit is refused, with the Occupied package's A4.14 conclusion as its reason (U8).
- **Concealed or hidden enemy.** An entry into a location holding one concealed or hidden enemy MMC reveals that unit, and the mover is forced back to where it started. The MF are counted there and its MPh ends (U7).
- **Every other occupied case** is refused with its reasons, and nothing is committed.

The step also delivers what these depend on:

- the terrain evidence behind the Scenario A1 packages, read through the map read API (ASL-MAP-081);
- live snapshot sources, so a package observes the live game;
- Inexperienced status and its MF allowance;
- per-side disclosure of what an entry reveals.

No die is rolled, and no new rule area is opened.

## 2. Principles

Everything in the Governed Writes Design, section 4, still holds: the planner plans from server state, replays before commit, commits only what is reviewed, and treats a repeated attempt as a Replay. This step adds three principles:

- **Packages observe through their providers.** Step 7 built the first case's observation inside the planner. Here the planner calls each package's own observation provider over a live snapshot source, so the reviewed provider checks (scope, version, terrain, exact case) run unchanged.
- **The adjudicator decides and each side is told only what it may know.** The planner reads the whole game (D3). What it returns is split into what the mover's side may see and what only the adjudicator may see (section 7).
- **Unknown is a refusal.** A fact the live game does not record stays unknown (ASL-UNIT-061). A refusal for a reason hidden from the mover's side does not name that reason to the mover's side.

## 3. Terrain evidence (ASL-MAP-081)

Today the five providers take the concrete `Board01TerrainCatalog`, and `Board01ValidatedSnapshotSource` wraps the occupied-case source with it. Each accepts a location only if it is one of the 63 building hexes whose type the pinned board 01 metadata states outright (version 6.9, metadata blob `e91b0d99...`), at level 0. The reviewed packages are bounded to that evidence, so this step keeps the same 63 hexes. What it changes is where the evidence is read from.

- **`IScenarioA1TerrainEvidence`.** A new interface in the Scenario A1 project with the one member the providers use: `IsSupportedGroundLevel(ScenarioA1TerrainBinding?)`. `Board01TerrainCatalog` implements it unchanged. The five providers and `Board01ValidatedSnapshotSource` take the interface instead of the class. Only the constructor parameter types change. The package manifests, matrices, and digests do not.
- **`BoardCatalogTerrainEvidence`.** A second implementation over `IBoardCatalog`, in the Play project. It is not in the Scenario A1 project: the runtime Host references the Execution adapter, which references Scenario A1, so a Maps reference there would bring the Maps project into the Host. It is true only when every one of these holds:
  - the binding names board 01 at the pinned metadata version and blob;
  - the handle's typed VASL source matches that pin (below);
  - the hex is one of the 63 override hexes;
  - the derived ground-level terrain of that hex, from the handle's Hex Facts, is the override's building type.

  That last check is the override consistency check of the Ingestion Design, section 6.4, made at read time. A board that disagrees is not evidence.
- **Typed board source.** `BoardHandle` gains an optional `VaslSource` record: board name, metadata version, metadata blob, LOSData blob, and commit. `StudioBoardCatalog` fills it from the board's `BoardProvenance`. The free-text `Provenance` stays for display. Authored and synthetic boards have none, so the Scenario A1 packages cannot be used on them.
- **Parity.** A test builds a handle from the committed board 01 Hex Fact oracle fixture (`bd01.hexfacts.json.gz`) with its recorded source identities. It checks that the two implementations agree on every hex of the board: 63 true, all others false. This test runs in CI without a VASL checkout. The existing `All63Board01BuildingOverridesAgreeWithTheGrid` test still covers the live VASL board in the run with `AslMaps__VaslRoot` set.
- **As built** (part 1). The parity and disagreement checks, and the Occupied provider over both implementations, are in `BoardCatalogTerrainEvidenceTests` in the Play tests. The existing Scenario A1 tests, including the second-defender gate binding, journal, and Host registration tests, pass unchanged.
- **Unchanged.** `ScenarioA1VerifiedReturnConclusionSource` in the Execution adapter keeps `Board01TerrainCatalog`. Step 9 decides that adapter's future.

## 4. Live snapshot sources

A new `LiveCaseSnapshots` in the Play project builds each package's snapshot from one `GameState` and the handle of the board the game is played on. The state is either the current state or, for the post-reveal case, the candidate state after the planned reveal (section 8). The snapshot's `Version` is `r{revision}` of that state, and its `SourceId` is `map-studio`. The providers compare both, so a snapshot of another revision is refused as stale.

**Occupied (`IScenarioA1BoardSnapshotSource`).** Only the known-enemy branch is built. Every other branch is left unfilled, so the provider's exact-case match refuses it.

| Snapshot field | Live derivation |
|---|---|
| `Occupancy` | `KnownUnconcealedEnemyMmc` when every occupant is an enemy MMC, known, unconcealed, and not hidden; otherwise `Other` or `ConcealedOrHidden` |
| `IsMovementPhase` | The phase is the MPh and the mover's side is phasing |
| `IsGoodOrderInfantrySquad` | As the first case derives it |
| `CanMove` | Not broken, TI, in Melee, or with its movement ended (section 8) |
| `IsKnownBuildingLocation` | `BoardCatalogTerrainEvidence` accepts the target |
| `HasNoSpecialModifier` | The game names no special rules |
| `HasNoA414Exception` | The mover is known not to be Berserk, Disrupted, or Unarmed. There is no Human Wave or OVR event (A4.14, p. 49). |
| `Terrain` | The binding for the target, from the typed board source |

**PostReveal (`IScenarioA1PostRevealSnapshotSource`).**

| Snapshot field | Live derivation |
|---|---|
| `PreviousLocationId` | The mover's location in the state before the attempt |
| `DefenderReveal` | `NonDummy` when the planned reveal shows a unit that is not a Dummy. The model has no Dummy kind yet (section 12). |
| `IsMovementPhase`, `IsOrdinaryInfantry` | Phase and phasing side; the mover is an Infantry MMC |
| `IsAttackerUnconcealedNonDummy` | The mover is known unconcealed and not hidden |
| `IsObstacleEntryNotBypass` | True for an entry, since Bypass is not an action of the live source |
| `HasNoA414EntryException` | As for Occupied |
| `HasNoOverrunElection` | True: the live source has no OVR election event, and the revealed unit is an MMC, so A12.15 offers no OVR option |
| `HasNoSpecialModifier` | The game names no special rules |
| `Terrain` | As for Occupied |

A case read (ASL-UNIT-060) is not used to build these. The planner already holds the whole-game state, and `ReadCase` answers for a perspective. The Read a case panel keeps showing what each perspective may read.

## 5. Inexperienced status

- **Derivation.** A unit is Inexperienced when its definition's class is Green or Conscript (A19.2, p. 86), except a Green MMC stacked with an unbroken leader (A19.3, p. 86). A Conscript is Inexperienced regardless of a leader. The class comes from the catalog definition, and a Replaced unit takes its new definition's class through lineage (A19.13, p. 86). A definition with no class, or a class the vocabulary does not know, makes the status unknown.
- **MF allowance.** A Good Order MMC has 4 MF, or 3 if Inexperienced (A4.11, p. 48; A19.31, p. 86). The remaining MF is the allowance less MF spent. The first case's `hasEnoughMovementFactors` becomes remaining MF of 2 or more, and is unknown only when the status is unknown. An entry after exactly 2 MF is then Definitive for a 1st Line squad.
- **Out of scope:** the leader's MF bonus (a stack moving with a leader) and conveyances. Movement is still one unit at a time.
- **Counters.** Every definition in the published catalog is 1st Line, so no live unit is Inexperienced today. The Green and Conscript paths are tested at the derivation level with synthetic definitions. A Green or Conscript counter is published only when the user supplies its printed values.
- **As built** (part 2). `Experience.Inexperienced` and `Experience.MfAllowance` in the Units project derive the status and allowance, resolving the definition through the unit's catalog reference. `GamePlanner.EntryFacts` uses the allowance, and `GamePlanner.ReviewEntryAsync` gives the facts and conclusion for one state. `ExperienceTests` cover every class, the leader exemption and its limits, and units that are not MMC; the Play tests show that a 1st Line squad after exactly 2 MF has enough MF and that its entry is Definitive.

## 6. The entry action

A mover's side cannot know whether a building it cannot see into is empty, so it cannot choose between an empty-building action and a concealed-entry action. There is therefore one entry action, and the planner decides which reviewed case applies from the whole game.

- **`asl.game.enter-building`.** A new registered action: Irreversible and Internal, with an idempotency key, the permission `asl.game.play`, and the same arguments as `asl.game.enter-empty-building`. The planner routes on the adjudicator's view of the target location:

| Target location | Package | Commits |
|---|---|---|
| Empty | The first case, as in step 7 | `instance-moved` with 2 MF, on a Definitive conclusion |
| Every occupant a known, unconcealed enemy MMC | Occupied, `A1-known-enemy-mmc-mph` | Nothing. The Definitive prohibition is the refusal's reason (U8). |
| Exactly one enemy unit, concealed or hidden, a non-Dummy MMC, and the return is clear | PostReveal, `A1-post-reveal-nondummy-forced-back` | The events of section 8, on a Definitive conclusion (U7) |
| Anything else | None | Nothing: a refusal |

  "Anything else" covers friendly occupants, an enemy SMC, several concealed units, a mix of known and concealed enemies, and a return hazard. The Occupied known-enemy case uses its board source only for the terrain and the exact-case facts; entry is into an adjacent building as in step 7.
- **`asl.game.enter-empty-building`.** Stays registered and plans only the empty case, as it does today, so existing tests and audit lines stay valid. The Play page uses the new action.

## 7. Disclosure

The planner's result gains a split view:

- `Reasons` and `Entry`, the facts and conclusion, are the adjudicator's view, as today.
- `SideView` is what the mover's side may see. It is built by projecting the plan's events and reasons for that side's perspective (ASL-UNIT-031).

Rules for the side view:

- **Proposal.** For an entry into a location the mover's side sees as empty or as a sealed presence, the proposal shows only "the entry will be resolved on confirmation". It does not show the planned events, the conclusion, or a refusal for a hidden reason. Otherwise proposing and cancelling would reveal hidden units without any commitment.
- **Confirmation.** The side view shows the committed events the side is entitled to: the attempt, the reveal, and the forced back. A refusal for a hidden reason is shown only as "the adjudicator cannot resolve this entry; nothing was committed" (for example, several concealed units).
- **Known limitation.** That refusal still tells the mover that something is there. The reviewed cases do not cover the location, so play stops, as it would in a face-to-face game where the players must consult the rules.

The Studio's single user acts for both sides and the adjudicator. The Play page shows the side view by default and the adjudicator view when the adjudicator perspective is chosen. Enforcing the split between users belongs to multi-user play, which is deferred.

## 8. The forced-back transition

The events are committed in one append, in this order. Each carries the PostReveal package identity as its rule package, and each event after the first names the attempt among its causes.

1. **`entry-attempted`** (new): `{ id, target, mf: 2 }`. It records the cost of the attempt. The mover's position and MF spent do not change, since A12.15 counts the MF in the previous location and the mover is not attackable in the target.
2. **`conditions-changed`**, only when the defender is hidden: `{ hidden: false, concealed: true }`. The unit is placed beneath a "?" (A12.15, p. 78), so this is a placement, not a reveal.
3. **`conditions-changed`**: `{ concealed: false }`. This is the reveal.
4. **`entry-forced-back`** (new): `{ id, attempt, returnedTo, mf: 2, followOnFireResolved: false }`. The projector keeps the mover in `returnedTo`, adds the 2 MF to its MF spent, and sets its movement as ended.

All four events are visible to every perspective: the attempt is declared, and the reveal is public. The hidden defender's earlier `instance-created` stays visible only to its side. The attacker learns of the unit from the reveal.

Supporting changes:

- **Projection.** `UnitInstance` gains `MovementEnded`, which is reset at every phase change, as MF spent already is (`GameProjector`). A unit whose movement has ended cannot move again in the phase (A4.1, p. 48), so `canMoveThisPhase` becomes false.
- **Replay checks.** `GameProjector` checks the new events:
  - `entry-attempted` needs an active unit in the MPh of its phasing side;
  - `entry-forced-back` needs a preceding `entry-attempted` for the same unit, and a `returnedTo` equal to the unit's position;
  - `mf` must match between the two.
- **Reveal detection.** `CaseReader.IsReveal` stops counting a change that sets `concealed` to true. A hidden unit placed beneath a "?" is not revealed.
- **Staleness.** `entry-attempted` and `entry-forced-back` affect cases at their target, previous location, and unit, so a snapshot read before them goes stale (U5).
- **Planning.** The planner builds the four events, replays the log with them to get the candidate state, and asks PostReveal over a snapshot of that candidate. It commits only on the Definitive conclusion; any other result refuses the whole plan. Before asking PostReveal, it checks the clear-return conditions of the execution boundary review against the current state: no Residual FP, FFE, minefield, Wire, entrenchment, or shellhole entity at the return location, and no depression terrain there.

## 9. Gate, confirmation, and audit

Unchanged from step 7:

- `GameConstraintEvaluator` plans the action, and a Ready or Replay plan is Satisfied.
- `ConfirmationPolicy` admits one confirmed correlation.
- `GameActionExecutor` re-plans at the validated revision, appends atomically, and reads the effect back.
- `AuditedActionExecutor` writes to `live/audit.jsonl`.

What changes:

- The executor's effect read-back checks all four events for a forced back, not only the last one.
- Audit lines for an entry record the adjudicator's reasons. The audit tail is shown only in the adjudicator view.

## 10. Map Studio

- **Play.**
  - The Enter control proposes `asl.game.enter-building`.
  - A perspective selector (each side, or the adjudicator) sets which view of proposals, results, units, and the audit tail the page shows.
  - After a forced back, the unit list shows the mover's MF spent and "movement ended". In the attacker's view, the revealed defender changes from a sealed presence to the unit.
- **Game states and the board viewer.** These need no change beyond the projection: they already filter live games by perspective.
- **Live testing.** Use the `map-studio-lf` launch configuration, because the Play entry tests and the Scenario A1 packages check embedded digests. Test on real VASL board 01:
  - a hidden defender in a building override hex;
  - a known-enemy refusal;
  - an entry after 2 MF.

## 11. Tests

**Scenario A1 tests**
- Terrain-evidence parity over the board 01 oracle fixture.
- The five providers accept the interface and still pass their existing tests.
- A board whose derived terrain disagrees with an override is refused.
- A handle with no VASL source is refused.

**Units tests**
- The replay checks for `entry-attempted` and `entry-forced-back`.
- `MovementEnded` is reset at a phase change.
- `IsReveal` ignores a placement beneath a "?".
- Staleness from the new events.
- Inexperienced derivation for 1st Line, Green (with and without a leader), Conscript, and an unknown class.

**Play tests**
- U7 over a live game on board 01 facts, with a hidden defender and with a concealed one:
  - the four (or three) events;
  - the mover's position and MF;
  - movement ended;
  - the attacker's view after the reveal;
  - a Replay.
- U8: a Definitive A4.14 refusal, with the revision unchanged.
- Each out-of-scope case refused with nothing committed, and with no hidden reason in the side view: several concealed units, an enemy SMC, a friendly occupant, a mixed location, a return hazard entity, a depression return, a Berserk mover, and an unknown condition.
- An entry after exactly 2 MF is Definitive.
- A proposal's side view hides the outcome.
- A stale confirm is refused.
- The action registrations.

**Acceptance over a live game**
- U4: the attacker sees a sealed presence before the attempt and the unit after it; the display for the attacker never receives the hidden unit before the reveal.
- U5: a case read before the attempt is stale after it.
- U6: a read of a location with a concealed occupant stays nondefinitive for the attacker.

**Map Studio tests**
- A forced back through the Play page.
- The perspective selector.
- A known-enemy refusal through the page.

The embedded-digest checks and the Play entry tests run in the LF clone, as the standing workflow requires.

## 12. Not in this step

- Dummies. The model has no Dummy kind, so every live unit is non-Dummy, and the dummies-only PostReveal branch is not reachable.
- Several concealed units and Random Selection, a revealed SMC and the OVR option, and every NTC. These belong to step 9.
- Occupied cases that are attempts or need state the model does not hold: stacking equivalents, a Breach, fortified buildings, and the APh.
- Follow-on fire after a forced back, and the TEM and placement effects of return hazards.
- Stack movement with a leader, and the leader MF bonus.
- Buildings outside the 63 board 01 override hexes, other boards, and composed maps. The reviewed packages are bounded to that evidence.

## 13. Build order

Each part goes on its own feature branch, and each branch compiles, passes all tests, and is verified in the LF clone and in Docker before merging.

1. `feature/asl-unit-terrain-evidence`: section 3.
2. `feature/asl-unit-inexperienced`: section 5.
3. `feature/asl-unit-concealed-entry`: sections 4 and 6 to 11.

This design and the execution boundary review are merged first, on their own branch, so each part starts from them.
