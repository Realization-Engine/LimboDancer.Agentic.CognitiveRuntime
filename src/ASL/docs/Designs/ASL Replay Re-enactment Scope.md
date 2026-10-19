# ASL Replay Re-enactment Scope

Analysis only, 2026-10-18, rewritten the same day after the user's corrections. No product code. Branch `claude/replay-reenact-scope-9c9f9a`, up to date with main at e6f407b6 (#20).

Status: kept for later at the user's word (2026-10-18); no proposal is approved. Followed up as backlog section 63 (`ASL Unit Backlog.md`).

## 1. The feature

As the user stated it:

1. **Re-play mode on the Play page.** From the Replay page, the user opens a recorded game on Play from its first event. Each step applies the recorded events as they are, the recorded dice with them. Nothing is decided again, so nothing can come out differently.
2. **"Play on from here"** switches the game from re-play mode to live play from that point on: the user's own proposals, and new dice drawn by the system.
3. **Divergence** can occur only once the game has turned live: it is the live game departing from the record. The page says where, that is, the step at which play went on.

Re-play starts at the beginning of the recorded game and goes on until, and only if, "Play on from here" is invoked.

The first version of this document scoped something else: re-deciding each recorded step under today's rules with the recorded actions and dice, and reporting where the outcome differed. That is not the feature, and its sections (argument rebuilding, the recorded-dice roller, outcome comparison, questions U1 to U5) are removed. It is not proposed for the backlog unless the user asks.

## 2. What main has today

Checked on main at e6f407b6.

### 2.1 Replay page

- **"Open on Play"** ([Replay.razor:43](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Replay.razor)): the game on Play as it stands now.
- **"Play on from here"** ([Replay.razor:73](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Replay.razor), `PlayOn` at line 534): makes a new game holding the events through the step shown and opens it on Play. Its note says "The dice from there on are new. This game is not changed." It is offered only where the whole game may be known, an ended game or the adjudicator's view, and never from a step at which the game had ended (`MayPlayOn`, line 524; ruling R31b.1). The new game's label records its origin: "from cd-guards-5 at step 41, revision 160".
- A step is one attempt with everything it caused (`ReplaySteps.AttemptOf`, [ReplaySteps.cs:108](../../LimboDancer.Domains.Asl.MapStudio/Services/ReplaySteps.cs)). cd-guards-5 has 898 events in 259 steps; guards-06-dlandi has 659 in 197.

### 2.2 `LivePlay.PlayOn`

[LivePlay.cs:104](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs): copies the source's events through a revision into a new game, under a new name and a label naming the origin. The source is only read. The copied `dice-rolled` events are the source's own draws; every draw after that is the new game's. This is the switch from re-play to live, already built.

### 2.3 The follow control (merged with #20)

A development aid for profiling, shown only with scripted dice (Development and `Play:ScriptedDice`):

- `LivePlay.StartFollowing` ([LivePlay.cs:130](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs)) makes a new game `<source>-follow-<n>` holding the record's first step, through `PlayOn`.
- `LivePlay.FollowStep` ([LivePlay.cs:147](../../LimboDancer.Domains.Asl.MapStudio/Services/LivePlay.cs)) appends the record's next step, its events copied as they are. It refuses a follower whose events no longer match the record's (`FOLLOW-002`, "played on by hand now") and one that has every step (`FOLLOW-003`). `StepEnd` (line 178) finds where a step ends.
- On Play: `StartFollowing`, `FollowNext`, and `FollowRun` ([Play.razor:1170 to 1230](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)); a step a second, stopped by a cancellation token and on dispose. `FollowRecordPanel.razor` shows progress, Next step, Run and Stop, and a problem line.
- Tests: [FollowRecordTests.cs](../../tests/LimboDancer.Domains.Asl.MapStudio.Tests/FollowRecordTests.cs) shows that a follower ends with the record's event ids and types in its order, one append per attempt, and that a follower that left the record is refused.

So the follow control is already a re-play mode of a kind, with three differences from the feature:

1. It is development only.
2. It writes the game as it goes: every step is a commit to a new game file, so a re-play leaves a game behind, and a step cannot be taken back.
3. Going live is implicit: the proposal controls stay open while following, and the first confirmed proposal makes the follower leave the record. Nothing says where it left except the next `FOLLOW-002`. There is no "Play on from here" in Play.

The source a follower follows is held only in the page's fields (`followGame`, `followSource`), so it is lost when the page is left.

### 2.4 What makes a read-only re-play cheap

`GameHistory` keeps the state after every event: `States[n]` is the state at revision n + 1, and `At(revision)` reads it ([GameState.cs:543](../../LimboDancer.Domains.Asl.Units/State/GameState.cs)). Replay's views at each step come from it (`GameLibrary.ViewOf`, [GameLibrary.cs:129](../../LimboDancer.Domains.Asl.MapStudio/Services/GameLibrary.cs)). The Play page takes its `history` and `state` from one place, `Refresh()` ([Play.razor:6149](../../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor)). A history cut at revision N is `new GameHistory(Events.Take(N), States.Take(N), [])`, built from the record's replay with no replay of its own. Given that, Play shows the game as it stood at N, exactly as if it had N events.

## 3. Options

### Option R: re-play reads, "Play on from here" writes (recommended)

- Re-play mode is a way of showing a recorded game on Play, at a step: `games/play?game=cd-guards-5&replay=41`. `Refresh()` cuts the history at the step's last revision. Nothing is written.
- In re-play mode the page shows the game as Play shows it, in every view and panel, but proposes nothing: the proposal controls are hidden or disabled, and a panel at the head of the page says "Re-playing cd-guards-5: step 41 of 259, (step title)". Controls: Back, Next step, Run at a pace (as Replay's Play: stopped by a key or a click), and **Play on from here**.
- **Play on from here** calls `LivePlay.PlayOn(source, revision, name, origin)`, exactly as Replay's does, and opens the new game on Play, live. Its label names the source and the step. The source game is unchanged.
- **Divergence** is where the new game's events stop matching the source's: the step at which play went on. It is computable from the two records by event id, the same comparison as `FOLLOW-002`. Once live, the Play page says it in the game's context ("Played on from cd-guards-5 after step 41"), from the label's origin and that comparison. Replay of the new game marks the same step.

Strengths: nothing written until the user decides, so no leftover games; Back is free; going live is one explicit act; the switch is code that exists and is tested. Cost: the Play page must hold a "re-play at revision N" mode in which it proposes nothing, and every place that reads `history` or `state` must read the cut history (they all come from `Refresh()`, which keeps this contained).

### Option W: promote the follow control

- Remove the Development gate; re-play makes a new game at once and appends each recorded step to it (`FollowStep`).
- Add an explicit "Play on from here" that ends following for that game; close the proposal controls while following, so going live happens only through it.
- Keep the follow link (source, next step) somewhere durable: a side file next to the game, or a field in the record (a format change).

Strengths: the per-step work is the real commit path, which is what the profiling wanted. Costs: every re-play leaves a game file behind, Back is impossible (events are irreversible), and the follow link needs storage the feature otherwise does not.

### Recommendation

Option R. It matches the feature: re-play is a reading of the record, and the game becomes a live game at one explicit moment. It reuses `PlayOn`, `ReplaySteps`, and the follow control's run loop and panel shape, and it needs no change to the record format. The follow control stays the development aid it is, for measuring the commit path, unless the user prefers to retire it once option R exists (section 6, U2).

## 4. The user experience under option R

- **Replay**: a link beside "Open on Play", "Re-play on Play", under the same condition as "Play on from here" (an ended game, or the adjudicator's view; ruling R31b.1). It opens Play in re-play mode at step 1. The existing "Play on from here" on Replay stays: it is the same switch, made from Replay.
- **Play, re-play mode**: the panel of section 3 at the head of the activity area; the map, the inspector, the step lists, and the dice history all at the step. The step lists' links to Replay go on working. The URL carries the step, so a reload or a shared link comes back to it.
- **Play, at the last step of an ended record**: "Play on from here" is not offered, as on Replay; the panel says the game ended.
- **Play on from here**: asks for the new game's name (default as Replay's, `<game>-r<revision>`), makes it, and opens it live. From then on it is an ordinary live game.
- **After the switch**: the game's context says "Played on from cd-guards-5 after step 41 (revision 160)". The new game's Replay shows the same and marks the step.
- **Views**: re-play is offered only where the whole game may be known, so a side's view never steps through the other side's hidden actions. A live game made by Play on opens behind Play's own hand-over, as Replay's Play on does today.

## 5. Effort, risk, and a first slice

| Piece | Effort | Risk |
|---|---|---|
| Re-play mode in Play: the `replay` query, the cut history in `Refresh()`, the proposal controls closed, the panel (Back, Next, Run, Play on) | About a day of sessions | A panel that reads the head of the game instead of the cut history would show the future. Tests at a step before a known later event in each panel's data catch it |
| "Re-play on Play" on Replay | Small | None beyond R31b.1's condition |
| The "played on from" line after the switch, on Play and on Replay | Small | Older Play-on games' labels: read the origin from the label when it has the "from <game> at step <n>" form, and from the event comparison otherwise |
| Studio check (five widths on Play and Replay, side views) | Half a day | |

**First slice**: re-play mode in Play with Next, Back, and Play on from here, reached by the URL, and its tests (a cut history shows no later event in any panel; Play on makes the game through `PlayOn` and opens it live; nothing is written before). **Second slice**: Run at a pace, the Replay link, the "played on from" line, and the Studio check.

Nothing is deferred for rules: re-play applies recorded events, so every kind of step a record holds re-plays, vehicles and ordnance included.

## 6. Open questions, each with a proposal

- **U1. Where "Play on from here" lives in Play.** Proposal: in the re-play panel, next to Next step; and still on Replay as today.
- **U2. The follow control.** Proposal: keep it as the development aid for the commit path's cost; retire it later if the user prefers one control.
- **U3. The new game's name.** Proposal: Replay's default, `<game>-r<revision>`, editable before it is made.
