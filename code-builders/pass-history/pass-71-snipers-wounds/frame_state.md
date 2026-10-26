# Frame agent B: the Sniper counter, placement, the SAN as state, the Sniper player's choices, Sniper Check

(The agent's closing report, kept as it came, 2026-10-26 about 08:3x.)

Read-only; nothing edited, built, run, or started. Rules read on their pages: p082 (A14 to A14.4), p083 (A14.4's DRM table, its EX, the SAN reduction), p225 (E1.76); every page searched for "SAN" and "1.76" (p253 night chart and p249 note repeat E1.76).

## 1. The counter today
- A Sniper is an entity, kind `asl:sniper`, faces front and pinned (EntityTests.cs:10,38,47; GameTypes.cs:649). It is not a unit.
- It enters a game only as an `instance-created` placement inside `asl.game.setup`, by hand: the setup page offers "the Sniper counter (side)" (Play.razor:1719,3754,7231); tests write it directly (BacklogPass15TablePlayerTests.cs:148).
- The card checks ignore it: `ScenarioA1SetupCalculator.Check` drops `asl:sniper` before checking groups (:539), so nothing checks where it is placed. `SideDiagnostics` (:190) checks only that the SAN is 0 to 7.
- The SAN lives on the side: `SideState.San` (GameTypes.cs:35), written into `game-started` (GameEventWriter.cs:96, GameEventReader.cs:151); for card games `ScenarioCards.Start` copies `card.san` (ScenarioCards.cs:260; a minimal card omits a SAN of 0). `UnitPlausibility.cs:67` checks an `asl:san` attribute on a counter document; game state never reads it.
- The planner reads the SAN from `game-started` through the side, `ScenarioA1Sniper.Triggers(sum, printed, night)` adding E1.76's +2 (GamePlanner.Snipers.cs:48), and finds the counter by `Entities.FirstOrDefault(kind asl:sniper, side, Active)` (:54). No counter, no attack, silently.

Proposal: the counter given in a setup step after both sides set up and before play: an `asl.game.setup` argument `sniper: {side, at}`, built like the non-OB "?" step (`PlanSetup`'s `conceal` branch, GamePlanner.cs:463, R23.6; page block `nonObBlock`, Play.razor:112). Card games with a side at SAN >= 2: `CardSetupIncomplete` (GamePlanner.CardSetup.cs:287) refuses the start of play until each such side's counter is placed; DEFENDER first (`ScenarioDefender`, else the side that does not move first). Script games: hand placement stays, no gate (many tests set a SAN without a counter, `ASideWithNoSniperCounter`). Recorded games in play: replay reads only records; never add a counter to a game whose setup is closed (`SetupClosed`); the page says once "this game started without the X side's Sniper counter (A14.01)". Games still in setup get the new step.

## 2. Setup placement (A14.2)
- `ScenarioA1Sniper.PlacementTier(counts)` and `PlacementBar(count, best)`; Play hands, for each unoccupied hex, the number of enemy-occupied hexes within 6 (Maps distance).
- Enemy-occupied counts enemy units and "?" as the placing side's view shows them; hidden units not counted; the enemy Sniper counter is no unit.
- The owner proposes the hex; the counter is public. Repositioning uses the same check against the state at that moment.

## 3. The Sniper player's choices (replacing R15.5)
Pass 70's pattern: `ChoicePending(Key, Kind, Side, Options, Resume)` (ChoiceRecords.cs:13), options any strings (`OverrunEvade` uses Locations); `PlanChoose` resumes a stopped resolution from the recorded rolls and draws new dice in the answer's commit (GamePlanner.Choices.cs:110-260, the `record` switch); `ChoicePendingBar` refuses everything but `choose` and `setup` while a choice waits (GamePlanner.cs:381); `PendingLossBar` only rewords that refusal; visibility by the event's audience (`LossChoiceEvent` to `[viewing]`, GamePlanner.ConcealmentChoices.cs:137; `ChoiceShown` hides the panel, Play.razor:8132); `MovementPlans.Review` treats only `ConcealmentLoss` as the DEFENDER's turn (MovementPlans.cs:369); the projector accepts only kinds listed in `PendChoice` (GameProjector.cs:1477); `GameGate` checks results by the last payload (GameGate.cs:207,296).

A Sniper attack can pause mid-commit, and the hook exists: `AddSniperAttacks` returns early when the commit holds a `ChoicePending` (Snipers.cs:23); Fire's resume path calls it after the answer (Choices.cs:216-227); its dice dictionary already reads `existing` "for an attack resumed after an owner's choice" (Snipers.cs:28).

Proposed: one new kind `sniper`, for the Sniper's side. `Snipe` resumable at named steps; `Resume` holds `record:"sniper"`, the sniper id, the trigger, roll ids, the step, the facts so far, and the triggers still to resolve in DR order.

| Step | Pauses when | Options |
|---|---|---|
| forfeit (A14.2) | each effective dr, before the Random Location DR, Sniper unpinned | attack / reposition (then the legal hexes) |
| equidistant hex (A14.21) | lowest TEM still ties | the tied hexes |
| Location (A14.2) | more than one Location with an eligible target | the Locations |
| prior target (A14.2 EXC) | enemy Sniper, Vulnerable Inherent Crew, or unarmored vehicle present | each, or "random selection" |
| first tied target (A14.2) | Random Selection ties | the tied stacks |

Pause only where there is more than one real option; keep no part of R15.5 (each changes the result; effective attacks are rare). While a choice waits nothing else may happen, which gives the A12.14 exception for free (needs a test). Options in the Sniper side's view words. Supporting: `PendChoice` whitelist and `DescribeChoice`; a `GameGate` case; `MovementPlans` must stop on any pending choice; the page's existing `PendingChoicePanel`. The choice is public.

## 4. The SAN as recorded state
A new event `san-reduced` (`SanReduced(Side, Cause)`); `SideState.SanReductions` (default 0), the printed San kept; Rules `ScenarioA1Sniper.ActualSan(printed, night, reductions)`: by day printed minus reductions; at night min(printed+2, 7) minus reductions, never below printed (E1.76, p. 225). Below 2 the same commit writes `instance-eliminated` for the counter (never at night). Causes: a Sniper Check <= 2, a Sniper attack dr 1 against a Sniper. Old records: 0 reductions. The trigger read at Snipers.cs:48 switches to ActualSan.

## 5. Sniper Check (A14.4)
Eligible: Good Order, unpinned, not TI (`asl:ti`, GameProjector.cs:744), armed Infantry in the target Location, no Prep/First/Final/Bounding Fire marker, not moved this Player Turn. Gap: no "moved this Player Turn" set for Infantry (`NoMoveThisPlayerTurn`, `MovedVehicles`, `MovedWeapons` do not cover it).

Shape: after an effective attack with eligible units, the target side gets `choice-pending` kind `sniper-check` (take/decline), possibly in the other side's Player Turn. Ruling proposal: "take" commits every eligible unit ("maximize benefit"); a subset to the backlog. 2 dice; Final DR <= 2 `san-reduced` (and removal below 2); 3 pins the Sniper; participants `asl:ti`; then the remaining triggers.

DRM (`ScenarioA1SniperCheck.Drm`): one leader's leadership directing others, none alone (EX); -1 each hero; -1 each crew/HS equivalent (squad -2). EX's three results as tests. Open: a heroic leader's hero DRM with leadership (A15.21); a wounded leader's modifier worse.

Pinned Sniper (A14.31): `Attacks(pinned)` skips it; the forfeit not offered while pinned; check that `ClearedMarkers("ccph")` reaches entities.

A Sniper Check DR does not call the enemy Sniper (A14.1's list).

## 6. Hidden information
Counter and moves public; SAN public. Choice options in the Sniper side's words. The placement set from the placing side's view (never counts HIP units). A pending placement shows the other side only "waiting". The Sniper Check choice names only the target side's own units.

## 7. Memory and performance
`PlayStepMemo` per state: `SniperOf(side)`, `ActualSan(side)`, `SniperPlacement(side, view)` (about 330 hexes x 30 enemy hexes), eligible targets by Location from `Objects(state)` (today `Snipe` calls `state.Location(unit.Id)` per unit several times and replays `[existing, events]` three or four times, Snipers.cs:123-142,209,219), `SniperCheckEligible(location)`. Measure the hand-over on `p70-perf-tw` with a pending Sniper choice.

## Estimates
| Part | H:MM |
|---|---|
| 1. Setup step, card gate, page block, old-game note | 2:30 |
| 2. Placement calculator, view count, repositioning | 1:30 |
| 3. Resumable Snipe, sniper choice kind, remaining triggers, PendChoice, GameGate, MovementPlans, page | 5:00 |
| 4. SAN state, event, ActualSan, removal, night floor | 1:30 |
| 5. Sniper Check | 3:00 |
| 6. View wording | 0:30 |
| 7. Memo entries | 0:40 |
| Total | 14:40 |

## Moves out proposed
SMOKE in the TEM comparison and the HA/Factory exclusions to pass 85; hidden Fortifications revealed to pass 115; a Sniper Check by a subset to the backlog; Cavalry to pass 165; Interior Building Locations to pass 135 unless A or C takes it.

## Not read
Backlog 26 and 55 in full; the pass 70 design and frame reports; `ScenarioA1ConcealmentChoices` in full, `AddLeaderLossChecks`, `Play.razor`'s `Awaiting`, MovementPlans.cs:473; whether the CCPh pin removal covers entities; A14.22, A14.33, D5.34, the TH, PAATC, and Entrenching triggers.
