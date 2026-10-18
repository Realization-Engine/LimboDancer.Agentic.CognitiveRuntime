# ASL Replay Re-enactment Scope

Analysis only, 2026-10-18. No product code. Branch `claude/replay-reenact-scope-9c9f9a`.

The feature: from the Replay page, start a new live game that is played forward on the Play page from a recorded game, one recorded step at a time, with the recorded actions and the recorded dice, so the new game takes the same steps. The user may stop at any step and take over. Where today's rules give a different outcome from the record, the page says where. It is meant as a permanent option on the Replay page.

This is not the development-only "follow a recorded game" control the parent session is building on the Play page: that control appends a recorded step's events to a live copy and re-enacts nothing. Nor is it "Play on from here" (`LivePlay.PlayOn`), which copies the events through a step and leaves the rest to the user with new dice; the user has said that one already covers diverging at a chosen point.

## 1. Summary and recommendation

Re-enactment is feasible without the Play page's UI state. The governed path takes a registered action and a JSON argument object, nothing else (`GamePlay.ProposeAsync` and `ConfirmAsync`, [GameGate.cs:378](../../LimboDancer.Domains.Asl.Play/GameGate.cs)). The record does not keep those arguments, but for every kind of step in the two recorded games they can be rebuilt from the step's own events, most of them directly, some with a read of the state before the step.

Two facts make the comparison clean:

- An event's id is the attempt id plus an index (`EventId`, [GamePlanner.cs:2102](../../LimboDancer.Domains.Asl.Play/GamePlanner.cs)), and units a step creates take ids from the attempt too (`fire-0174e552453c-g-squad-7`). Proposing a re-enacted step with the recorded attempt id gives the same event ids and the same new unit ids, so later recorded steps still name the right units, and the two logs line up by id.
- Each recorded step is exactly one attempt: the Replay page already groups steps that way (`ReplaySteps.AttemptOf`, [ReplaySteps.cs:108](../../LimboDancer.Domains.Asl.MapStudio/Services/ReplaySteps.cs)), and in both games no attempt's events are interleaved with another's.

The record's dice are kept per attempt, in draw order, as `dice-rolled` events. A roller that serves those values in order, and fails when they run out, reproduces the draws exactly while the rules ask for the same rolls, and stops cleanly when they do not: the store draws inside its commit and writes nothing when the build throws ([GameStore.cs:131](../../LimboDancer.Domains.Asl.Play/GameStore.cs)).

**Recommendation: option A** (section 7): a re-enactor service in the Studio that rebuilds each step's arguments, proposes and confirms it through `GamePlay` with a recorded-dice roller, and compares the result with the record; a panel on the Play page drives it; the Replay page starts it. Build it in two slices: first the engine and a headless run of both recorded games to the end (it tells us, before any UI, where today's rules part from the record), then the Play and Replay pages. Setup is copied, not re-enacted. Kinds of step absent from the recorded games are deferred, and the re-enactment stops before one with "take over here".

## 2. What a recorded game holds

A record (`src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/cd-guards-5.game.json` in the main checkout, untracked live data) is `{schemaVersion, tenant, game, label, synthetic, events}`; each event has `eventId, revision, time, source, type, visibility, payload`, and optionally `rulePackage` and `causes`. Outcomes only: no proposals, no refused attempts, no record of the view that proposed.

"Roots" (events without `causes`) are not steps: one fire attempt has two roots (`dice-rolled`, then `fire-resolved`), and a capture attempt (`cap-...`) has none, since all its events point back to the advance-phase event that left the surrender pending. The unit is the attempt:

| Game | Events | Roots | Steps (attempts) | Interleaved attempts |
|---|---|---|---|---|
| cd-guards-5 | 898 | 555 | 259 | 0 |
| guards-06-dlandi | 659 | n/a | 197 | 0 |

The attempt id's prefix names the Play page control that made it (`Attempt(kind)`, [Play.razor:7061](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)): a kind and 12 hex digits. Steps by prefix:

| Prefix | Action | cd-guards-5 | guards-06 | Dice per step |
|---|---|---|---|---|
| `fire` | `asl.game.fire` | 98 | 65 | 1 to 10 |
| `advance` | `asl.game.advance-phase` | 82 | 63 | 0 to 3 |
| `adv` | `asl.game.advance` | 29 | 25 | 0 |
| `rout` | `asl.game.rout` | 16 | 7 | 0 or 1 |
| `rally` | `asl.game.rally` | 15 | 15 | 1 |
| `move` | `asl.game.move` | 6 | 6 | 0 |
| `cap` | `asl.game.take-prisoner` | 4 | 2 | 0 |
| `setup` | `asl.game.setup` | 3 | 3 | 0 |
| `cc` | `asl.game.close-combat` | 2 | 4 | 0 or 1 |
| `enter` | `asl.game.enter-building` | 2 | 0 | 0 |
| `recover` | `asl.game.recover` | 1 | 0 | 1 |
| `reject` | `asl.game.take-prisoner` (reject) | 1 | 0 | 0 |
| `massacre` | `asl.game.massacre` | 0 | 3 | 0 |
| `non-ob` | `asl.game.setup` (conceal) | 0 | 1 | 0 |
| `grd` | `asl.game.guard-prisoners` | 0 | 1 | 0 |
| `amb` | `asl.game.ambush` | 0 | 1 | 2 |
| `choose` | `asl.game.choose` | 0 | 1 | 0 |

Every recorded roll has source `system` (217 and 152 rolls). The rule packages stamped on the events (`scenario-a1-fire`, `-rally`, `-close-combat`, `-declared-first-case`) all name manifests older than today's (for example the fire package's recorded `6f53...` against today's `116f...`, [ScenarioA1FirePackage.cs:15](../../LimboDancer.Domains.Asl.Rules/ScenarioA1FirePackage.cs)). So the package stamp differs on every stamped event and cannot by itself count as a divergence.

## 3. Feasibility per kind of step

"Rebuild" says where each argument comes from. The schemas are in [GameActions.cs](../../LimboDancer.Domains.Asl.Play/GameActions.cs). Every action also takes `gameId` (the new game), `attemptId` (the recorded attempt id), and `expectedRevision` (the new game's event count), and the re-enactor proposes from the adjudicator's view (`proposedBy` = `adjudicator`, [GamePlanner.Proposer.cs:20](../../LimboDancer.Domains.Asl.Play/GamePlanner.Proposer.cs)).

| Root kind | Action | Rebuild | Effort |
|---|---|---|---|
| `game-started`, `instance-created` | `setup` (start, placements) | `start` and `placements` are in the payloads (sides, map, catalog, card; each instance). Copy instead: see section 7, first slice | Copy: none. Rebuild: medium |
| `setup-concealed` | `setup` with `conceal` | Unit ids per event; the `conceal` object's shape needs reading from `PlanNonObConcealment` | Copy with setup |
| `phase-changed` (with `conditions-changed`, `concealment-gained`, `instance-eliminated`, `surrender-pending`, `dice-rolled` in the same step) | `advance-phase` | No arguments, except `retainDm` at the end of the RPh: the broken units under DM before the step that still have DM after it in the record (no `asl:dm: false` for them). The other events are the planner's own consequences | Easy; `retainDm` medium (reads the state before) |
| `dice-rolled` + `fire-resolved` | `fire` | `firers` and `director` on the payload; `target` = `targetLocation`; `weapons` from `facts.firers[].weapons[].equipmentId`; `withoutInherent` from `facts.firers[].usesInherentFp: false`; `directors` (more than one) from the facts. `partners`, `snapShot`, `sprayTarget`, `mol`, `vehicle`, `fireLane` do not occur in either game; each must be found in the facts when built | Medium (98 + 65 steps; the largest group) |
| `advanced` | `advance` | `unitIds` = `units`, `to`; an exit (`adv-exit`) names `exit` | Easy |
| `rout-stepped` (+ `rout-interdicted`, `equipment-transferred`) | `rout` | `unitId` = `unit`; `route` = the `to` of each `rout-stepped` in order; `lowCrawl` on the payload. A dropped SW (`equipment-transferred` with a position) was a separate drop or part of the rout; to be checked against `PlanRout` | Easy |
| `dice-rolled` + `rally-attempted` | `rally` | `unitId` = `unit`, `leader` | Easy |
| `movement-step` (+ `concealment-lost`) | `move` | `unitIds` = `movers`, `to`, `minimumMove`. `assault`, `doubleTime` are not on the payload: read from the movers' conditions after the step (CX for Double Time) or the step's MF; to be confirmed against `PlanMoveOrEntryAsync` | Medium |
| none (all caused by the advance-phase step) | `take-prisoner` | `unitId` = `instance-captured.id`, `captorId` = `custodian` | Easy |
| `surrender-rejected` (caused) | `take-prisoner` with `reject` | `unitId` = `unit` | Easy |
| `instance-moved` (rule package `declared-first-case`) | `enter-building` | `unitId` = `id`, `location` = `position.at` | Easy |
| `dice-rolled` + `close-combat-resolved` | `close-combat` | `location`, `round`; `attacks` from `facts.attacks[]` (attackers, defenders, director, capture, yield, guard); `stacking`, `withdrawals`, `infiltrations`, `handToHand` to be found in the facts when built | Medium |
| `dice-rolled` + `recovery-attempted` | `recover` | `unitId` = `unit`, `equipmentId` = `weapon` | Easy |
| `prisoners-massacred` | `massacre` | `unitId` = `units[0]`, `prisonerId` = `prisoners[0]` | Easy |
| `instance-captured` (root) | `guard-prisoners` | `guardId` = the old custodian (state before), `to` = the new `custodian`; `abandon` when the prisoners are freed instead | Easy (reads the state before) |
| `dice-rolled` + `ambush-rolled` | `ambush` | `location` | Easy |
| `choice-made` (caused) + `fire-resolved` | `choose` | `key`, `option` on `choice-made` | Easy |
| `concealment-gained`, `conditions-changed`, `instance-eliminated`, `equipment-transferred`, `surrender-pending`, `concealment-lost` as roots | none of their own | Consequences inside an `advance-phase`, `adv`, `rout`, or `move` step | n/a |
| `game-ended` | none | The last `advance-phase` (or an immediate victory) writes it | n/a |

Kinds with an action but no step in either game (vehicles, ordnance, DCs, starshells, smoke, OVR, deployment, transfers, mopping up, hidden placement, pass and end of a move) are listed in section 7 as deferred. Two are trivial and worth including early, since any game with movement may have them: `pass` (`asl.game.pass-fire`, no arguments) and `end` (`asl.game.end-move`, optional `unitIds`).

Older games may carry prefixes no longer used; the re-enactor maps a prefix to an action through a table and, failing that, reads the step's first event type. A step it cannot map stops the re-enactment there (section 5).

## 4. The engine path

### 4.1 How the Play page proposes and confirms

`Propose(action, arguments)` ([Play.razor:6744](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)) adds the view's `proposedBy` and calls `Live.Play.ProposeAsync(action, arguments, Live.Principal)`. `Confirm()` ([Play.razor:6823](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)) checks the game is still at the proposal's revision and calls `ConfirmAsync` with the proposal's correlation id. `Refresh()` ([Play.razor:6074](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)) reads the game again from the store.

`GamePlay.RunAsync` ([GameGate.cs:392](../../LimboDancer.Domains.Asl.Play/GameGate.cs)) plans the action (`GamePlanner.PlanAsync`, [GamePlanner.cs:215](../../LimboDancer.Domains.Asl.Play/GamePlanner.cs)), runs the Execution Gate (the constraint evaluator plans it again), and, once confirmed, the executor plans a third time and appends (`GameActionExecutor`, [GameGate.cs:57](../../LimboDancer.Domains.Asl.Play/GameGate.cs)). A rolled plan is appended with `AppendRolled`, which draws inside the store's lock and builds the events from the draw.

### 4.2 An action without UI state

Nothing in that path reads the page: the inputs are the descriptor, the argument JSON, and the principal. The page's state (pickers, selections, `retainDm`) only builds the argument object. So a service can propose and confirm a rebuilt argument object, then ask the Play page to `Refresh()`. One case needs care: a withheld entry returns `NeedsConfirmation` from Propose by design ([GameGate.cs:402](../../LimboDancer.Domains.Asl.Play/GameGate.cs)); the re-enactor confirms it like any other proposal.

### 4.3 Dice

- `DiceRoller` ([DiceRoller.cs](../../LimboDancer.Dice/DiceRoller.cs)) is sealed; its public constructor uses the cryptographic generator, and an internal one takes a per-die source `Func<int, int>` (the sides in, a zero-based value out). The Studio project can reach the internal one (`InternalsVisibleTo`, [LimboDancer.Dice.csproj](../../LimboDancer.Dice/LimboDancer.Dice.csproj)); the Play project cannot.
- `ScriptedDice` ([ScriptedDice.cs](../../LimboDancer.Domains.Asl.MapStudio/Services/ScriptedDice.cs)) queues dice for UI tests, replaces the Studio's one roller for all games, falls back to random dice silently when the queue is empty, and exists only in Development with `Play:ScriptedDice`. It is not fit for a permanent feature.
- The roller is fixed when `GamePlay` is built ([GameGate.cs:360](../../LimboDancer.Domains.Asl.Play/GameGate.cs)); `LivePlay` builds one for the Studio ([LivePlay.cs:40](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs)).

The proposal: a recorded-dice roller per re-enacted step, built from the step's recorded `dice-rolled` values in order, served die by die, with a check that each die's sides match the recorded roll's, and an exception when the values run out. The re-enactor runs the step through a second `GamePlay` built over the same planner, store, and audit with that roller (two `GamePlay` objects over one store are safe: the store locks per file). After the commit, values left unused are a divergence too.

### 4.4 Same state, action, and dice: same events?

- **Ids**: yes. Ids are `{attemptId}-{n}`, and new units' ids derive from the attempt id; with the recorded attempt id they match.
- **Order**: the planner builds the events from the state, the arguments, and the draws, with no randomness other than the dice; projected collections iterate in insertion order. This is expected to be deterministic, and the headless run (section 7, slice 1) proves it on 456 steps.
- **What differs even when nothing changed**: `time` (the planner's clock, [GamePlanner.cs:2044](../../LimboDancer.Domains.Asl.Play/GamePlanner.cs)), and the `rulePackage` stamp whenever a package has a new manifest, which today is all of them. The `facts` and `resolution` blobs on fire, rally, close combat, and ambush records also grow when Rules adds a fact or a DRM, without the outcome changing.

## 5. Divergence

### 5.1 What counts as the same

Three levels, compared after each step against the recorded game replayed to the same step (the record replays by its outcomes, with no rule deciding anything, so the recorded state at every step is always at hand):

1. **Outcome** (a divergence): the dice asked for differ (more than recorded, fewer, or other sides), or the state after the step differs. State: each entity's position, status, conditions, holding, custodian, and definition; the turn, phase, and phasing side; any pending choice or surrender; the open movement window; the game's end and result. This is what decides whether the next recorded step can still follow.
2. **Events** (a divergence, shown with the outcome): the step's events by id, type, causes, and visibility, and their payloads without `time`, `rulePackage`, `facts`, and `resolution`.
3. **Detail** (a note, not a divergence): the `facts` and `resolution` blobs and the package stamp. Shown on request, as "recorded under an earlier version of the fire package" and a list of the fields that differ.

A step refused today (for example a rule now bars the recorded fire group) is an outcome divergence with the refusal's reasons. A step the re-enactor cannot rebuild is not a divergence: it says the kind of step is not re-enacted yet.

A canonical state comparison does not exist today; it is new code in the Studio (a projection of `GameState` to the fields above, compared field by field). It decides nothing about the rules, so it does not belong in Rules.

### 5.2 What happens at a divergence

The re-enactment stops after the step that diverged (the new game keeps today's outcome; nothing is undone, since writes are irreversible). The page says:

- "Step 87 of 259 (fire, [G4] at [F5]) came out differently from the record:" then the differences in words, for example "r-squad-21 is broken here, Good Order in the record", "the rules now ask for 2 dice; the record has 1".
- With the choice: take over from here (the game is now an ordinary live game), or go on re-enacting where the next recorded step still applies (offered only when the state matches at level 1; otherwise it would follow a script that no longer fits).

A run to a chosen step stops at the first divergence.

## 6. The user experience

What exists on Replay: "Open on Play" (the game as it stands now, [Replay.razor:43](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Replay.razor)) and the collapsed "Play on from here" ([Replay.razor:79](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Replay.razor)), offered only where the whole game may be known (an ended game, or the adjudicator's view; ruling R31b.1).

Proposed:

- **Replay**: a second collapsed section beside "Play on from here", "Re-enact on Play", under the same condition. It names the new game (default `<game>-re`), says that setup is copied and each step after it is played again with the recorded action and dice, and opens Play on the new game with the re-enactment panel. Its first step is the step after setup, not the step shown; the shown step is the default stopping point ("Re-enact to step 87"), editable.
- **Play**: a panel at the head of the activity area while a re-enactment is attached to the game: "Re-enacting cd-guards-5: step 41 of 259 (rally, r-squad-3 by g-leader-9-1-1)". Controls: Next step (one step), Run to step N (with pace, stopping at a key or click, as Replay's Play does), and Stop and take over. Each step shows in the page's ordinary step list and dice history. A divergence shows as in section 5.2. Taking over removes the panel; the game is then an ordinary live game.
- The re-enactment runs in the adjudicator's view only, since the steps are both sides' and their outcomes may reveal concealed units.
- The attachment (source game, next step) lives in the Studio session, not in the record; reopening the game later offers "Resume re-enacting cd-guards-5 from step 42" when the new game's events still match the source through that step, which is checkable from the two records alone.

## 7. Options

| Option | What | Effort | Risk |
|---|---|---|---|
| **A. Re-enactor service, recorded dice, Play panel** (recommended) | Section 3's builders, section 4.3's roller, section 5's comparison, section 6's panels | Engine and harness: about a day of sessions. Pages and Studio check: about a day. Remaining kinds: in groups, half a day each | Argument rebuilds that guess wrong on a field the record does not keep (Double Time, `retainDm`); the harness shows them as divergences on the very first run |
| B. Drive the Play page's own controls with ScriptedDice | Script the pickers and Confirm, queue the dice | Larger: each control's state is different | Dev-only roller, silent random fallback, one queue for all games, brittle to every UI change. Not recommended |
| C. Record each step's arguments from now on | Write the confirmed argument object beside the events (a field on the step's first event, or a side record), so later games re-enact exactly | Small in Play; a record format change in the Units reader and writer | Helps only games played after it; the existing games still need A. Recommended later, once A shows how often a rebuild is ambiguous |

Recommendation: A now; C as a follow-up decision after slice 1's results.

### 7.1 First slice (engine and evidence, no page)

1. A recorded-dice roller (Studio services) and a re-enactor that, for one step, rebuilds the arguments, proposes and confirms through a `GamePlay` with that roller, and compares at the three levels.
2. Builders for the kinds in both recorded games: `advance-phase` (with `retainDm`), `fire` (firers, director(s), target, weapons, withoutInherent), `advance`, `rout`, `rally`, `move`, `take-prisoner` (capture and reject), `enter-building`, `close-combat`, `ambush`, `recover`, `massacre`, `guard-prisoners`, `choose`; plus `pass` and `end`.
3. Setup copied, as `PlayOn` copies events ([LivePlay.cs:104](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs)), through the last setup step (including the non-OB "?" step): it draws no dice and its arguments are the bulkiest to rebuild.
4. A headless run of cd-guards-5 and guards-06-dlandi from setup to the end, as a test or a code-builders tool, reporting the first divergence of each and every note. This is the evidence that decides what slice 2 must handle and whether option C is worth it.

### 7.2 Second slice (pages)

The Replay section and the Play panel of section 6, with the five-width Studio check scoped to the two changed pages.

### 7.3 Deferred

Kinds absent from the recorded games, each stopping the re-enactment with "this kind of step is not re-enacted yet; take over here" until built: vehicles (`move-vehicle`, `overrun`, `vehicle-close-combat`, `motion-attempt`, `button-up`, `hook-gun`), ordnance (`fire-ordnance`, `recover-shock`, `turn-gun`), DCs (`throw-dc`, `detonate-dc`, Placement through `move`), `fire-starshell`, SMOKE through `move`, Infantry OVR (`declare-overrun`, `infantry-overrun-ntc`, `overrun-evade`), `opportunity-fire`, `forfeit-rof`, `deploy`, `recombine`, `transfer`, `drop`, `dismantle`, `repair`, `ambush-withdraw`, `mop-up`, `place-hidden`, and setup re-enactment (as opposed to copying). Each goes to the backlog when the plan is approved.

## 8. Open questions, each with a proposal

- **U1. The dice's provenance in the new game.** Its `dice-rolled` events are the source game's draws, served again; they keep source `system`, as "Play on from here" keeps the copied rolls. Proposal: keep `system`, and say it in the game's label ("cd-guards-5 (re-enacted from step 1)"); no record format change in this feature.
- **U2. After a divergence.** Proposal: stop; offer "go on re-enacting" only when the state still matches (section 5.2).
- **U3. Where re-enactment starts.** Proposal: always at the first step after setup, with the shown Replay step as the default stopping point. "Play on from here" already covers starting the new game at a later step.
- **U4. Recording arguments from now on (option C).** Proposal: decide after slice 1's headless run.
- **U5. Pace and performance.** Each step plans three times (preview at Propose, the gate, the executor), as a played step does. A run of 259 steps has not been timed; the parent session's Play profiling bears on it. Proposal: time it in slice 1's headless run and set the default pace from that.
