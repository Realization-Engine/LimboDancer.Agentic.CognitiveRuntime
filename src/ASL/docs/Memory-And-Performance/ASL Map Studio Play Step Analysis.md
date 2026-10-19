# ASL Map Studio Play Step Analysis

**Written:** 2026-10-19, at the user's word: a full analysis of the design and implementation of the fixes for what the SVG Memory Analysis left open (its section 14, "What is left"), and their cost.
**Source:** the SVG Memory Analysis, sections 13 and 14; the Counter Layer Design, sections 7.3 and 16; the backlog, section 62. Code read on `main` at 9ff197dd: `MapStudio` (`Play.razor`, `StudioBoardCatalog`, `CampaignPlayableFootprints`, `BoardProvider`, `LivePlay`), `Play` (`GamePlanner` and its `Rout`, `Encirclement`, `Map`, `Terrain`, `CloseCombat` parts), `Rules` (`ScenarioA1RoutCalculator`, `ScenarioA1UpperLevelEncirclement`), `Units` (`GameState`), `Maps` (`InMemoryBoardCatalog`, `BoardRef`).
**Status:** analysis only; no code is changed. P1 approved by the user 2026-10-19 ("re-measure Play in your Studio now, split by phase, and look at the 1.3 GB step in the same run"); measured the same morning, section 7. Sections 3 to 5 were written before it; where section 7 changes them, it says so, and section 8 revises the proposals.
**Scope:** the server's work on a Play step. Every ASL rule stays in the Rules project (the ruling of 2026-10-05); what changes here is how often the planner and the page ask Rules the same question, and what a board lookup allocates.

## 1. What is open

Of the four items in section 14's "What is left", one is closed:

| Item | State |
|---|---|
| Replay: the counter layer built every step, sent and redrawn whole | **Closed** by PR 22 (68447a05): build 48 to 8.5 ms, a step sends only changed hexes, allocation 13.1 to 4.3 MB a step |
| Play: the counter layer (about 46 ms) | **Closed** by PR 22: build 55 to 5 to 8 ms |
| Play: rout obligations and advice (about 41 ms) | Open |
| Play: upper-level encirclement (about 40 ms) | Open |
| Play: the footprint lookup's dictionary work (about 31 ms) | Open |
| Play: the unit table's rows (7.7 s over 51 steps in section 13, about 150 ms a step then, much of it the file check since fixed) | Open, not named in section 14 |
| Play: one step near revision 137 of cd-guards-5 that allocates 1.3 to 1.6 GB (Counter Layer Design, 7.3) | Open, cause unknown |

After E, Play's follow loop does about 103 to 138 ms of server work a step (two runs). The three open figures were measured before E (section 14) and overlap: the lookup's 31 ms is spent inside the rout and encirclement searches, so the three do not add up to a step.

## 2. Measure again first

**Proposal: yes, measure again in my Studio before any fix**, as the first task of whatever plan follows. Reasons:

- **The figures are a day old and pre-E.** E moved the step from about 157 to 103 to 138 ms; the shares of what is left have moved with it.
- **They overlap.** A trace read by inclusive time per method counts the lookup inside the rout and inside the encirclement. The plan needs exclusive figures for each of the three, so each fix has an expectation to meet.
- **The rout figure is an average across phases.** The page asks the rout obligations only in the Rout Phase (`state.Phase == "rtph"`, `Play.razor:491`). An average over 70 steps hides what a Rout Phase step costs, which is the step a player waits on. The measurement should split steps by phase.
- **It gives the "before" for the fixes,** measured the same way as the "after" will be.

How, as in section 13 and the design's 7.3: my Studio (port 6670, Debug), the follower of cd-guards-5 as the adjudicator, the page visible; `dotnet-trace` (`dotnet-sampled-thread-time`) for times by method, read exclusive and inclusive; `dotnet-counters` alone, in a second run, for allocation and CPU (a trace at the same time spoils the allocation counter). Steps grouped by phase. Cost: about 30 to 45 minutes (the design's E4 took 27).

The same run can take the 1.3 GB step: hold the follower at revision 136, start `dotnet-counters` and a `dotnet-trace` with the `gc-verbose` profile, and step once (section 7).

## 3. Where the time goes, from the code

### 3.1 The board lookup (about 31 ms, pre-E)

Every rules question that touches the map goes through `GamePlanner` to `IBoardCatalog.TryGetBoard`, many thousands of times a revision. On the profiled game (one VASL board, `bd01`, at a pinned version, no footprint) a lookup that hits every cache still does:

| Where | What it does on every call |
|---|---|
| `StudioBoardCatalog.TryGetBoard` (`StudioBoardCatalog.cs:22-37`) | Allocates the closure of the footprint lambda (line 35) even when that lambda never runs; builds a throwaway `InMemoryBoardCatalog` around the one handle (line 36): an array, the catalog, a `Dictionary` from `ToDictionary`, a second lookup of the same board, a second comparison of the 40-character version, and a new `BoardReadResult` |
| `VaslBoardProvider.Load` (`BoardProvider.cs:182-186`) | `GetOrAdd` with a lambda that captures `board` and `this`: a closure and a delegate per call, then a string-keyed lookup |
| `CampaignPlayableFootprints.Read` (`CampaignPlayableFootprints.cs:24-48`) | Two dictionary lookups keyed by (board, version), each hashing the 40-character version; a clock read through `TimeProvider` |
| The call site, `state.Map.Board(board)` (`GameTypes.cs:124`) | A LINQ `FirstOrDefault` with a capturing lambda |

About nine allocations, five or six hashes of the board key, about four hashes or comparisons of the version, and a clock read, per lookup. On a placed map (several boards) it is worse: `GamePlanner.Composed` (`GamePlanner.cs:2054-2073`) looks up every board and builds a new `ComposedMapRead` and `MapLayout` on every call, from 14 call sites, with no cache.

The handle itself is cached (one per loaded board, `ConditionalWeakTable`), which matters: the planner keeps its line-of-sight caches by handle.

### 3.2 Rout obligations and advice (about 41 ms, pre-E, Rout Phase only)

The page reads the obligations once per state and view (`routCache`, `Play.razor:6567-6593`), so the cost is not repeated renders. It is repeated searches inside one read:

- `ReadRoutObligations` (`Play.razor:6595-6614`), for each broken unit of mine, calls `MayRout` (which calls `MustRout`), then `RoutSurrenderAdvice` (which calls `MustRout` again, and may search for an Interdiction trap), then `MustRout` (line 6606), then `MustRout` again (line 6612, the same question as line 6606), then `RoutAdviceText`, which runs `RoutAdvice`. `ReadRoutChoices` (line 6616) asks `MayRout` and `RoutSurrenderAdvice` again for the same units.
- **Every `MustRout` builds a new `RoutScan`** (`GamePlanner.Rout.cs:32`). A scan reads the enemy list once (`Enemies`, line 136), but each new scan reads it again, and **for every Known enemy it runs `planner.Encircled`** (line 145), which is the A7.72 search of section 3.3 for each enemy standing upstairs, plus `InterdictionRange`, which calls `state.At`.
- So one read costs about (broken units) x (7 to 10 scans) x (enemies x (an encirclement search + a scan of all objects)), plus the rout searches themselves (`RoutReach`, a Dijkstra search over location and "seen by" state, which reads adjacency, line of sight, and Infantry entry per edge).

Nothing in the planner keeps `MustRout`, `RoutAdvice`, a scan's enemy list, or `Encircled` across calls; only `SurrenderedBeforeRouts` is kept per state (`Rout.cs:53`). The same calls run during a proposal (`PlanRout`, `Rout.cs:408-482`; the phase-end check in `GamePlanner.Proposer.cs:49-50`), so the waste is not only the page's.

### 3.3 Upper-level encirclement (about 40 ms, pre-E)

`UpperLevelEncircled` (`GamePlanner.Encirclement.cs:24-38`) hands Rules a reader for a breadth-first search of up to 96 Locations (`ScenarioA1UpperLevelEncirclement.cs:54-90`), which stops when it reaches ground level; a cut-off unit is the worst case, since it searches everything reachable. Per Location it reads:

- the steps: `ChargeNeighbors` (six neighbours, the levels in the hex), then `InfantryStep` for each, which looks the board up, resolves both ends, reads the hexside, scans units and entities for smoke (`HasSmoke`), and asks the weather rules;
- the units there: `state.At(location)`, which runs `Location()` and `Aboard()` for every object in the game, each a linear `Find` (`GameState.cs:401-461`). That is quadratic in the number of objects, per Location visited.

The page asks it once per revision and view for each visible unit upstairs (`CutOffUpstairs`, cached in `cutOffCache`, `Play.razor:5687-5710`). The rout scans ask it again for every enemy, in every scan (section 3.2), and fire, movement, and advance ask it for firers, targets, and movers. Nothing keeps the answer per state.

### 3.4 The unit table's rows

`UnitRows` (`Play.razor:1287-1294`) is a property with no cache, read on every render of the Play page. Wide layouts keep the Units tab mounted while hidden (`KeepMounted`, `Play.razor:7219-7239`), so it runs on every render whether the tab is shown or not. Each row calls `state.Location`, `Custody` (a scan of all units), `CargoNotes`, `VehicleNotes`, and `GamePlanner.IsBurning` (a scan of entities): quadratic in the number of objects, again per render. A confirm renders the page twice; any click renders it again.

### 3.5 What the four have in common

- **`GameState` has no index.** `Find`, `Unit`, `Location`, `Aboard`, and `At` are linear scans (`GameState.cs:401-461`), and the rules readers call them inside loops. A `GameState` is a `sealed record`, a snapshot of one revision, so an index built once per state is safe.
- **The planner forgets between calls.** It is a singleton shared by every Blazor circuit (`LivePlay`, `Program.cs:37`), and the same `GameState` object is reused across refreshes when the events have not changed (`GamePlanner.Replay`, `GamePlanner.cs:162-181`). A memo per state, in a `ConditionalWeakTable<GameState, ...>` as `surrenderedFirst` already does, lives exactly as long as the state and is shared by the page, the proposal, and every circuit.

## 4. The fixes

Each with its design, its expected gain (an estimate, to be checked against section 2's new baseline), its cost to build, its runtime cost, its risk, and the tests it reaches.

### A. The board lookup

| | A1: an allocation-free hit | A2: the result cached per (board, version) | A3: the map resolved once per state |
|---|---|---|---|
| Design | Return the handle's `BoardReadResult` directly, without the throwaway catalog (check the version inline); make the lambdas non-capturing (`GetOrAdd(key, static (k, arg) => ..., arg)`); keep the footprint read | A `ConcurrentDictionary<(BoardRef, string), Entry>` in `StudioBoardCatalog` holding the finished result, the footprint generation it was made for, and when an absence is next checked; `Publish` bumps the generation | A per-state `MapContext` in the planner (`ConditionalWeakTable<GameState, MapContext>`): each board's handle, the composed read of a placed map, `Map.Board` by key. `ReadLocation`, `Step`, and `Composed` read from it |
| Gain (estimate) | Most of the 31 ms's allocation; perhaps half its time | Most of what A1 leaves: one lookup and two number compares a call | The catalog hit once per board per state; on a placed map, the layout no longer rebuilt per call |
| Cost to build | About 20 minutes | About 45 minutes, with a test of the generation | About 45 to 60 minutes: several call sites in the planner |
| Runtime cost | None | One entry per pinned board version; must be bounded (as `AuthoredBoardService`, 64 versions) or it holds every version's facts | One small object per live state, collected with it |
| Risk | Low: the same handle, the same result | A footprint published by the other Studio must still be seen within the 5-second window; the absence check stays per entry | A footprint published mid-revision is seen at the next state, not the current one (acceptable: a state is a snapshot) |
| Tests reached | `StudioBoardCatalogTests`, `CampaignPlayableFootprintsTests` | the same | the planner tests (`LimboDancer.Domains.Asl.Play.Tests`), the Play page tests that use a placed map |

**Proposal: A1 now; A3 with fix C; A2 not built.** A1 is small and safe. A3 does what A2 does and more, for the planner's calls, without a cross-process cache to keep correct. A2 is held back unless the new baseline shows other callers (the `CaseReader`, `GameLibrary.cs:150`) still spending on the lookup.

### B. Rout obligations and advice

| | B1: the page asks each question once | B2: the planner keeps rout answers per state |
|---|---|---|
| Design | One pass over the broken units that reads, once per unit, `MustRout`, the surrender advice, and the advice, and builds both the obligations and the choices from it (removes the duplicate at line 6612 and the second pass in `ReadRoutChoices`) | A per-state rout memo: the enemy list per side (`RoutScan.Enemies`), `MustRout` and `RoutSurrender` per unit, `RoutAdvice` per unit; one shared, thread-safe store per state |
| Gain (estimate) | From 7 to 10 scans per broken unit to about 3 | From about 3 to 1 scan per unit, and the enemy list (with its encirclement searches) read once per side per state instead of once per scan; the proposal and the phase-end check gain too |
| Cost to build | About 15 to 20 minutes | About 60 minutes: the memo, its thread safety, tests of reuse |
| Runtime cost | None | A few dictionaries per state in the Rout Phase |
| Risk | Low: the same answers, fewer reads | `SurrenderedBeforeRouts` reads routes while its own set is still empty (`Rout.cs:56-58`): a memo filled during that read would keep answers made "before the surrenders". The memo must not keep what is read inside that window, or must key on it |
| Tests reached | The Play page's rout tests (`PlayPageDemoFixesTests`, `PlayPagePass28bTests`, `PlayPagePass31Tests`, `PlayPagePass31dTests`) | Those, and the planner's rout tests (`BacklogPass10Tests`, `BacklogPass13Tests`, `BacklogPass20TablePlayerTests`, `BacklogPass31dTests`, `BacklogPass35Tests`) |

**Proposal: B1 and B2, in that order.** B1 is a page-only change that pays at once; B2 is where the multiplication is, and it serves the proposal too. B2's risk is the one real design point in this analysis and gets its own test: a Rout Phase where one unit surrenders before the other side routs (the A20.21 example on p. 69), compared with and without the memo.

### C. Upper-level encirclement

| | C1: `Encircled` kept per state | C2: an index of a state's objects |
|---|---|---|
| Design | `UpperLevelEncircled(state, unit)` memoized per (state, unit id) in the planner. The rule counts unconcealed enemies only, so the answer does not depend on the view | Per state, built once: objects by id, each object's resolved location, the objects in each Location. `state.At`, `Location`, and `Find` as the readers use them read from it. Either a planner-side memo, or a lazy index inside `GameState` |
| Gain (estimate) | The search runs once per upstairs unit per state, instead of once per rout scan per enemy, per fire and movement check, and per page read | `state.At` from quadratic to a lookup; every search and scan that asks "who is here" gains, and the unit table (fix D) too |
| Cost to build | About 20 minutes | About 45 minutes as a planner memo; more inside `GameState`, whose record equality and the many projects that make states would have to be checked |
| Runtime cost | One set per state | One dictionary per state, a few hundred entries |
| Risk | Low: the answer is a pure function of the state | Low as a planner memo. Inside `GameState`, a lazy field on a record changes nothing a caller sees only if equality and `with` copies ignore it: to be checked, so the planner memo is the safer start |
| Tests reached | The A7.72 rules tests (`ScenarioA1Pass45RulesTests`), the planner's encirclement tests (`BacklogPass12Tests`, `BacklogPass26Tests`, `BacklogPass35Tests`); no planner-level test of `UpperLevelEncircled` exists, so one is added | The planner tests broadly; the Play page tests that read units |

**Proposal: C1, then C2 as a planner memo.** C1 removes the repetition; C2 makes each remaining search cheaper. Both sit in the planner, next to A3, as one per-state context.

### D. The unit table's rows

- **Design:** cache `UnitRows` per (revision, view), as `routCache` and `cutOffCache` do. `Words` (the cascaded "Say") is fixed for a page's life; if not, it joins the key. While there, `cutOffCache` keys on `viewAs` and `routCache` on `Viewer.Name`: one key for both.
- **Gain:** the rows built once per revision instead of on every render (a confirm alone renders twice; every click renders again).
- **Cost:** about 15 minutes. **Runtime cost:** one list per page. **Risk:** low. **Tests:** the Play page's unit table tests (`PlayPageTests` and the pass tests that read `#play-units tr`).

**Not proposed: rendering hidden tabs only when shown.** Dropping `KeepMounted` for the Units tab would stop the work when the tab is closed, but many page tests read the hidden table, and one asserts it stays in the page (`PlayPagePass31cTests.cs:403`). With D's cache the hidden table costs one build a revision, so the gain left would not repay rewriting those tests.

### E. The 1.3 GB step

Not designed: the cause is not known. Investigating it is section 2's second run: hold the follower at revision 136, step once under `dotnet-counters` and a `dotnet-trace` with `gc-verbose`, read the allocating types and stacks. Cost: about 30 minutes to find; a fix is sized once the cause is known. It may share a cause with B or C (a search over a crowded step), which is why it is looked at before the fixes.

## 5. The cost in all

### 5.1 Building it

Estimates from the time log of the last two days (the footprint and set-id fixes with their tests took 26 minutes; E's tasks 7 to 23 minutes each; a gate with the strict build about 15 minutes; a before-and-after measurement about 30 minutes):

| Task | Estimate |
|---|---|
| 1. Measure again (section 2), and the 1.3 GB step (E) | 60 to 75 min |
| 2. A1, the allocation-free hit | 20 min |
| 3. B1 and D, the page's fixes | 30 to 35 min |
| 4. The per-state context in the planner: A3, B2, C1, C2 | 2 h 30 to 3 h |
| 5. Measure after, the Studio check of Play (the Rout Phase panel, the unit table, cut-off units), the results written | 45 min |
| 6. The gate (the strict build, the tests the change reaches, then the projects at the final gate) and the pull request | 30 to 45 min |
| **In all** | **about 5 to 6 hours**, in one branch and one pull request, or two (tasks 2 and 3 first, as they are small and independent) |

### 5.2 At runtime

- **Memory:** per live state, a context of a few dictionaries (boards, objects by id and by Location, encirclement answers, rout answers). Small beside a state, and collected with it. A2, if ever built, is the only cache that would need a bound.
- **Correctness:** every memo is keyed by a `GameState` object, an immutable snapshot, so no memo can go stale within a revision. The one exception to watch is B2 inside `SurrenderedBeforeRouts` (section 4, B).
- **Concurrency:** the planner is shared across circuits; each per-state store is filled under a lock or with concurrent collections, and a duplicate fill is harmless (the answers are the same).
- **The rules:** unchanged. Rules is asked the same questions with the same facts, fewer times.

### 5.3 What it should buy

Estimates only, to be replaced by task 1's baseline:

| | After E (measured) | After the fixes (estimate) |
|---|---|---|
| Play: the follow loop's server work, a step outside the Rout Phase | about 103 to 138 ms | about 50 to 80 ms |
| Play: a Rout Phase step with broken units | not measured apart | well under half of what task 1 finds |
| Proposals in the Rout Phase (`PlanRout`, the phase-end check) | not measured | faster by the same memos; not measured today |

## 6. For the user to rule on

| # | Question | Proposal |
|---|---|---|
| P1 | Measure again before any fix (section 2) | Yes, task 1, with the 1.3 GB step |
| P2 | Which fixes | A1, B1, D, then the per-state context (A3, B2, C1, C2); not A2; not lazy hidden tabs |
| P3 | Branches | One branch, one pull request; tasks 2 and 3 may go first as their own if the user wants an early gain |
| P4 | Where `GameState`'s index lives (C2) | In the planner's per-state context, not in `GameState` |

P1 was approved and done (section 7). P2 to P4 are restated in section 8.

## 7. Measured again (2026-10-19, after PR 22)

### 7.1 How

My Studio (port 6670, Debug, built from `main` at 645f18c3), the Play page's follower of cd-guards-5 as the adjudicator, the page visible, a step a second paced on the server. No product code was changed for the measurement.

- **Run 1, untraced:** all 898 revisions, 258 steps, with `dotnet-counters` (System.Runtime, every second). In the page, a `MutationObserver` noted the time of each change of the follower's "revision N of 898"; a step's interval less the follow loop's one-second pause is **the step's work**: the server's work after the step (the store's append, the page's refresh and render) and the page's update, together. Each step is given the phase of the revision it ends at, read from the record's `phase-changed` events.
- **Run 2, traced:** a second follower, revisions 1 to 621, under `dotnet-trace` (`dotnet-sampled-thread-time`), split into steps by the opens of the page's `FollowNext` frame; inclusive time of named frames per step, all threads.
- **Run 3, the allocation trace:** the second follower held at revision 621, then two steps by hand (621 to 622, 622 to 623) under `dotnet-trace` with `gc-verbose` and sampled thread time; the allocation ticks read with TraceEvent by type and by the nearest Studio frames.

Scripts in my scratchpad (`split.py`, `trace_split.py`, `trace_children.py`, `allocread.cs`); the two follower games deleted after.

### 7.2 A step, by phase (run 1)

| Phase | Steps | Work a step: median | mean | 90th percentile | most | Process CPU a step, mean |
|---|---|---|---|---|---|---|
| All play | 256 | 129 ms | 185 ms | 300 ms | 1,206 ms | 207 ms |
| **Rout Phase (rtph)** | **33** | **567 ms** | **567 ms** | **907 ms** | **1,206 ms** | **597 ms** |
| Every other phase | 223 | 122 ms | 129 ms | 163 ms | 485 ms | 149 ms |
| of which: Rally; Prep Fire; Movement; Defensive Fire; Advancing Fire; Advance; Close Combat | 24; 47; 19; 58; 23; 39; 13 | 116; 123; 116; 124; 127; 113; 138 ms | | | | |

- **The Rout Phase is the cost.** Its steps are 13% of the game's steps and about 40% of its server time (33 x 567 ms against 223 x 129 ms). A Rout Phase step costs about 4.6 times any other; outside it, the phases differ little.
- **Every other step is about 120 ms,** against 103 to 138 ms measured after E (Counter Layer Design, 7.3) over a mix of phases: E's figure stands.
- **The step that closes setup** (revision 1 to 27) took 13.1 s in run 1 and 10.2 s in run 2, while the server's `FollowNext` spent about 0.35 s and the process about 0.44 s of CPU: the rest is the browser's first draw after setup closes, not the server. Outside this analysis; in the backlog (section 9).

### 7.3 Where a step's time goes (run 2, inclusive, mean a step)

| Frame | Rout Phase (13 steps) | Other phases (155 steps) |
|---|---|---|
| The page's rout obligations (`RoutObligations`) | 366 ms | (not read) |
| The page's rout choices (`RoutChoices`) | 91 ms | (not read) |
| of these, overlapping: the enemy list (`RoutScan.Enemies`) | 293 ms | |
| `MustRout` | 248 ms | |
| `UpperLevelEncircled` (all callers) | 230 ms | 13 ms |
| `RoutAdvice` | 202 ms | |
| `InfantryStep` | 182 ms | 4 ms |
| `GameState.At`; `GameState.Location` | 138; 108 ms | 6; 5 ms |
| The board lookup (`TryGetBoard`) | 262 ms (see below) | 7 ms |
| `FollowNext` (the store's append and read, then `Refresh`) | 51 ms | 51 ms |
| of which: `LivePlay.FollowStep` (`FileGameStore.Append` and `Read`) | 48 ms | 48 ms |
| The Play page's render (`BuildRenderTree`) | 17 ms | 17 ms |
| The counter layer (`UnitOverlayBuilder.Build`) | 14 ms | 14 ms |
| The unit table's rows (`UnitRows`), with `CutOffUpstairs` inside | 14 ms | 13 ms |

What it shows:

- **The rout read is about 450 ms of a Rout Phase step** (366 + 91), which is the difference between a Rout Phase step and any other (567 against 122 ms).
- **Two thirds of it is the enemy list, read again in every scan** (293 ms), and most of that is the encirclement search run for each enemy (`Encircled` inside `Enemies`, about 220 ms). Section 3.2's reading of the code holds: the multiplier is the scans, and inside each scan the A7.72 search per enemy.
- **Outside the Rout Phase, encirclement costs 13 ms a step** (the page's `CutOffUpstairs`, once per revision), not section 14's 40 ms: that figure was mostly the rout's work, averaged over all steps.
- **The board lookup's figure is not to be trusted at this depth.** In the trace, 260 of its 262 ms sit under `TimeProvider.GetUtcNow`; a benchmark on this machine puts that call at 32 ns (`GetTimestamp` 19 ns, `Environment.TickCount64` 4 ns), so the trace would need about 140 million calls. The sampler stops a thread at its next safe point, and a P/Invoke such as the clock's is one, so the time of the rules' own loops before the call is charged to it. The outer frames (the rout read, the enemy list, encirclement) are sound at the scale of milliseconds; the lookup's share, here and in section 14 (where `File.Exists` drew the samples the same way), is an upper bound. Its real cost is to be measured by count and stopwatch, not by this sampler.
- **The store's append is the largest item outside the Rout Phase: 48 ms a step.** `FileGameStore.Commit` reads the game's file (`File.ReadAllBytes`, `GameStore.cs:93`), writes all of it again with the new events to a temporary file, and moves it into place (`GameStore.cs:193-194`). cd-guards-5's file is 1.2 MB at its end, so the cost grows with the game. A confirm in Play pays the same; the follower only makes it visible.

### 7.4 The "1.3 GB step": a counter artifact

Run 1's counters showed the same thing as the design's 7.3, at another step: the second ending at the step from 621 to 622 (entering the German Rout Phase of turn 4) gave **4,267 MB** allocated, after four seconds of 0 MB while 10 gen 0 collections ran. In that same second:

- the process's working set fell from 269 to 257 MB and the GC's committed memory from 135 to 123 MB, which a 4 GB burst could not do;
- the allocation trace of the same step (run 3, the allocation ticks, about one a 100 KB, all counted) gives **about 315 MB** for the step and its render, and 42 MB for the next step, as the counters give for an ordinary step (median about 30 MB).

So the counter's total (`dotnet.gc.heap.total_allocated`, a running total the counter differences each second) is not credible at those seconds: it stalls and then jumps. The cause is not established (the Server GC's changing heap count is a candidate); the backlog entry is corrected (section 9). **What is real:** the Rout Phase steps allocate several times what others do (run 1, a mean of 238 MB against 28 MB, with the caveat above), and the step from 621 to 622 is the heaviest seen, about 315 MB and 702 ms of work.

What run 3 found in those 315 MB, by the nearest Studio frames:

| Source | MB | What it is |
|---|---|---|
| `ScenarioCards.EmbeddedText` from `GamePlanner.CardOf` | 87 | `ScenarioCardLibrary.Current` (`ScenarioCardLibrary.cs:59-65`) reads a built-in card's text from the assembly's resource on **every** call (`ScenarioCards.cs:211-222`: a stream, `ReadToEnd`, `ReplaceLineEndings`, two strings), though its comment says "a built-in card's once": the planner's card cache (`CachedCard`, `GamePlanner.CardSetup.cs:35`) is keyed after the read. Only the SHA-256 is kept (`EmbeddedSha256`) |
| `GameState.Find`, `Objects`, `Location`, `At` | about 60 | the linear scans of section 3.5: a `Concat` iterator and a predicate closure per `Find` |
| `FileAuditSink.Tail` from the Play page's render | 23 | the adjudicator's audit panel (`Play.razor:1143`) reads the whole audit log (`File.ReadLines(path).TakeLast(12)`, `LivePlay.cs:244-250`) on every render; the file is 5.5 MB and only grows |
| The board lookup and `BoardHandle.Resolve` | about 40 | section 3.1's per-call allocations, and the location reads |
| The store's write and read | about 17 | section 7.3 |

## 8. The proposals, revised by the measurement

Section 7 changes the order. The rout read is most of what a player waits for; two cheap finds (the card text, the audit tail) join; the board lookup and the 1.3 GB step drop in rank.

| Fix | What | Expected gain | Cost to build |
|---|---|---|---|
| **B2 + C1** | The per-state rout and encirclement memo (section 4, B and C): the enemy list once per side per state, `MustRout`, the surrender and the advice once per unit, `Encircled` once per unit | Most of the 450 ms of a Rout Phase step: estimate a Rout Phase step from about 570 to 150 to 250 ms | About 1 h 30 with tests (the surrender window, section 4 B, is the design point) |
| **B1** | The page asks each rout question once (section 4, B1) | Part of the same 450 ms; with B2 it is the part B2 does not reach (the page's own repeats) | About 20 min |
| **H** (new) | Cache a built-in card's text, as its SHA-256 already is (`ScenarioCards.EmbeddedText`) | About 87 MB of a heavy Rout Phase step's 315 MB, and the time to read and copy it; also every other `CardOf` caller | About 10 min, one test |
| **I** (new) | The audit panel's tail without reading the whole log: kept in memory as lines are written, or read back from the file's end | About 11 MB and a 5.5 MB file read per render of the adjudicator's view, growing with the log | About 20 min |
| **D** | The unit table's rows once per revision and view (section 4, D) | About 13 ms a render | About 15 min |
| **A1** | The allocation-free board lookup (section 4, A) | Unknown: the sampler cannot say (section 7.3); the allocation trace puts it near 40 MB in a heavy step | About 20 min, with a count of calls and a stopwatch to measure it first |
| C2, A3 | The per-state index of objects and the map context | Inside the rout's work; measured after B2 and C1 | Held until then |
| E | The 1.3 GB step | Dropped: a counter artifact (section 7.4) | None |
| J (backlog) | The store rewriting the whole game file at each append (48 ms a step, growing) | Every confirm and every step of a long game | A storage design decision: section 9 |

**In all:** B2, C1, B1, H, I, D, and A1 with its measurement, then the after-measurement (the same three runs) and the gate: **about 4 to 5 hours**, one branch and one pull request.

**What it should buy** (estimate, against section 7's figures): a Rout Phase step from about 570 to about 200 ms; other steps from about 120 to about 100 ms (D, I, and H where `CardOf` is read); the heaviest step's allocation from about 315 MB to under 150 MB.

| # | Question | Proposal |
|---|---|---|
| P2' | Which fixes | B2 and C1, B1, H, I, D, A1 with its measurement first; C2 and A3 only if the after-measurement asks; not A2; not lazy hidden tabs |
| P3 | Branches | One branch, one pull request |
| P4 | Where `GameState`'s index lives (C2, if built) | In the planner's per-state context |
| P5 | The store's whole-file rewrite (J) | To the backlog now, for its own design; not in this pass |

## 9. To the backlog

Section 65 of the backlog, added with this section:

- The step that closes setup takes 10 to 13 s in the browser (section 7.2).
- The store rewrites the whole game file at every append (section 7.3, J).
- The 1.3 GB step of section 62 is a counter artifact (section 7.4); that entry is corrected.
- `dotnet-counters`' allocation total stalls and jumps on this Studio: allocation per step is read from allocation ticks, not from that counter, until the cause is known.
