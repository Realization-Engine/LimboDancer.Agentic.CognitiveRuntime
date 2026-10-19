# ASL Map Studio Play Step Analysis

**Written:** 2026-10-19, at the user's word: a full analysis of the design and implementation of the fixes for what the SVG Memory Analysis left open (its section 14, "What is left"), and their cost. Revised the same day after the measurement the user approved (P1), so that every section agrees with it.
**Source:** the SVG Memory Analysis, sections 13 and 14; the Counter Layer Design, sections 7.3 and 16; the backlog, section 62; the measurement of section 2. Code read on `main` at 9ff197dd: `MapStudio` (`Play.razor`, `StudioBoardCatalog`, `CampaignPlayableFootprints`, `BoardProvider`, `LivePlay`), `Play` (`GamePlanner` and its `Rout`, `Encirclement`, `Map`, `Terrain`, `CloseCombat`, `CardSetup` parts, `ScenarioCards`, `ScenarioCardLibrary`, `GameStore`), `Rules` (`ScenarioA1RoutCalculator`, `ScenarioA1UpperLevelEncirclement`), `Units` (`GameState`), `Maps` (`InMemoryBoardCatalog`, `BoardRef`).
**Status:** analysis and measurement. P1 (measure again) approved and done 2026-10-19; P2 to P5 approved the same day. The fixes are built on branch `play-step` (the Play Step Plan, tasks 1 to 7) and measured again in section 8.
**Scope:** the server's work on a Play step. Every ASL rule stays in the Rules project (the ruling of 2026-10-05); what changes here is how often the planner and the page ask Rules the same question, and what a step reads, writes, and allocates around them.

## 1. What is open

Section 14's "What is left", as it stands after PR 22 and the measurement of section 2:

| Item, as section 14 or the design named it | Now |
|---|---|
| Replay: the counter layer built every step, sent and redrawn whole | **Closed** by PR 22 (68447a05): build 48 to 8.5 ms, a step sends only changed hexes, allocation 13.1 to 4.3 MB a step |
| Play: the counter layer (about 46 ms) | **Closed** by PR 22: about 14 ms a step in section 2's trace (build and the rest of `UnitOverlayBuilder.Build`) |
| Play: rout obligations and advice (about 41 ms) | **Open, and larger than it looked:** the 41 ms was an average over all steps. The page reads it only in the Rout Phase, where it is about 450 ms of a step of about 567 ms (section 2.3) |
| Play: upper-level encirclement (about 40 ms) | **Open, mostly inside the rout:** about 230 ms of a Rout Phase step, three quarters of it the rout's enemy lists (section 2.3); outside the Rout Phase, about 13 ms a step |
| Play: the footprint lookup's dictionary work (about 31 ms) | **Open, size unknown:** the sampler charges it time that belongs to its callers (section 2.4); the allocation trace puts the lookup's allocation near 40 MB in a heavy Rout Phase step |
| Play: the unit table's rows (7.7 s over 51 steps in section 13, before the footprint fix) | **Open, small:** about 13 ms a step |
| Play: a step near revision 137 that allocates 1.3 to 1.6 GB (Counter Layer Design, 7.3) | **Closed as a measurement artifact:** the runtime's allocation counter stalls and then reports the backlog in one second (section 2.5) |

The measurement found three costs section 14 did not name: a built-in scenario card's text read again on every `CardOf` call, the adjudicator's audit panel reading the whole audit log on every render, and the game store rewriting the whole game file on every append (sections 2.3 and 2.5).

## 2. Measured again (2026-10-19, after PR 22)

### 2.1 How

My Studio (port 6670, Debug, built from `main` at 645f18c3), the Play page's follower of cd-guards-5 as the adjudicator, the page visible, a step a second paced on the server. No product code was changed for the measurement.

- **Run 1, untraced:** all 898 revisions, 258 steps, with `dotnet-counters` (System.Runtime, every second). In the page, a `MutationObserver` noted the time of each change of the follower's "revision N of 898"; a step's interval less the follow loop's one-second pause is **the step's work**: the server's work after the step (the store's append, the page's refresh and render) and the page's update, together. Each step is given the phase of the revision it ends at, read from the record's `phase-changed` events.
- **Run 2, traced:** a second follower, revisions 1 to 621, under `dotnet-trace` (`dotnet-sampled-thread-time`), split into steps by the opens of the page's `FollowNext` frame; inclusive time of named frames per step, all threads.
- **Run 3, the allocation trace:** the second follower held at revision 621, then two steps by hand (621 to 622, 622 to 623) under `dotnet-trace` with `gc-verbose` and sampled thread time; the allocation ticks read with TraceEvent by type and by the nearest Studio frames.

Scripts in my scratchpad (`split.py`, `trace_split.py`, `trace_children.py`, `trace_nested.py`, `stalls.py`, `allocread.cs`); the two follower games deleted after. Time: 25 minutes.

### 2.2 A step, by phase (run 1)

| Phase | Steps | Work a step: median | mean | 90th percentile | most | Process CPU a step, mean |
|---|---|---|---|---|---|---|
| All play | 256 | 129 ms | 185 ms | 300 ms | 1,206 ms | 207 ms |
| **Rout Phase (rtph)** | **33** | **567 ms** | **567 ms** | **907 ms** | **1,206 ms** | **597 ms** |
| Every other phase | 223 | 122 ms | 129 ms | 163 ms | 485 ms | 149 ms |
| of which: Rally; Prep Fire; Movement; Defensive Fire; Advancing Fire; Advance; Close Combat | 24; 47; 19; 58; 23; 39; 13 | 116; 123; 116; 124; 127; 113; 138 ms | | | | |

- **The Rout Phase is the cost.** Its steps are 13% of the game's steps and about 40% of its server time (33 x 567 ms against 223 x 129 ms). A Rout Phase step costs about 4.6 times any other; outside it, the phases differ little.
- **Every other step is about 120 ms,** in line with the 103 to 138 ms measured after E (Counter Layer Design, 7.3) over a mix of phases.
- **The step that closes setup** (revision 1 to 27) took 13.1 s in run 1 and 10.2 s in run 2. In run 2's trace the server's `FollowNext` spent about 0.35 s of it, and in run 1 the process used about 0.44 s of CPU over the whole interval. So the server was mostly idle; the likeliest cost is the browser's first draw after setup closes, not measured here. In the backlog (section 7).

### 2.3 Where a step's time goes (run 2, inclusive, mean a step)

| Frame | Rout Phase (13 steps) | Other phases (155 steps) |
|---|---|---|
| The page's rout obligations (`RoutObligations`) | 366 ms | (not read) |
| The page's rout choices (`RoutChoices`) | 91 ms | (not read) |
| of these, overlapping: the enemy lists (`RoutScan.Enemies`) | 293 ms | |
| `MustRout` | 248 ms | |
| `UpperLevelEncircled`, all callers | 230 ms | 13 ms |
| of which: inside the enemy lists | 216 ms | |
| of which: the page's `CutOffUpstairs` | 13 ms | 13 ms |
| `RoutAdvice` | 202 ms | |
| `InfantryStep` | 182 ms | 4 ms |
| `GameState.At`; `GameState.Location` | 138; 108 ms | 6; 5 ms |
| The board lookup (`TryGetBoard`) | 262 ms (not to be trusted, section 2.4) | 7 ms |
| `FollowNext` (the store's append and read, then `Refresh`) | 51 ms | 51 ms |
| of which: `LivePlay.FollowStep` (`FileGameStore.Append` and `Read`) | 48 ms | 48 ms |
| The Play page's render (`BuildRenderTree`) | 17 ms | 17 ms |
| The counter layer (`UnitOverlayBuilder.Build`) | 14 ms | 14 ms |
| The unit table's rows (`UnitRows`), with `CutOffUpstairs` inside | 14 ms | 13 ms |

What it shows:

- **The rout read is about 450 ms of a Rout Phase step** (366 + 91), close to the difference between a Rout Phase step and any other (567 against 122 ms).
- **Two thirds of it is the enemy lists, read again in every scan** (293 ms), and three quarters of those (216 ms) are the A7.72 encirclement search, run for every Known enemy in every scan (section 3.2). Most of the rest of the enemy lists is `InterdictionRange` (19%).
- **Outside the Rout Phase, encirclement costs 13 ms a step:** the page's `CutOffUpstairs`, once per revision. Section 14's 40 ms was mostly the rout's work, averaged over all steps.
- **The store's append is the largest single item measured outside the Rout Phase: 48 ms a step.** `FileGameStore.Commit` reads the game's file (`File.ReadAllBytes`, `GameStore.cs:93`), writes all of it again with the new events to a temporary file, and moves it into place (`GameStore.cs:193-194`). cd-guards-5's file is 1.2 MB at its end, so the cost grows with the game. A confirm in Play appends through the same store (`LivePlay.cs:122`, `174`); the follower only makes it visible. The rest of an ordinary step's 122 ms (about 40 ms not in this table) is Blazor's diff, the send, and the page's update.

### 2.4 What the sampler cannot say

In the trace, 260 of the board lookup's 262 ms sit under `TimeProvider.GetUtcNow`. A benchmark on this machine puts that call at 32 ns (`GetTimestamp` 19 ns, `Environment.TickCount64` 4 ns), so the trace would need about 140 million calls in 13 steps. The sampler stops a thread at its next safe point, and a P/Invoke such as the clock's is one, so the time of the rules' own loops before the call is charged to it. Consequences:

- The outer frames (the rout read, the enemy lists, encirclement, `FollowNext`) are sound at the scale of milliseconds.
- The lookup's share is an upper bound, here and in section 14, where `File.Exists` drew the samples in the same way. Its real cost is to be measured by a count of calls and a stopwatch before it is fixed (fix A1, section 4).

### 2.5 The "1.3 GB step" and the heavy steps' allocation

**The cause is known: the counter, not the Studio.** The runtime's allocation counter (`dotnet.gc.heap.total_allocated`, read each second by `dotnet-counters`) stalls: it reads 0 MB for stretches of 4 to 163 seconds while gen 0 collections go on (8 such stretches in runs 1 and 2). Some stretches end with a jump that reports the backlog in one second: 4,267 MB at the step from 621 to 622 in run 1, 1,469 MB and 4,861 MB in run 2. In the second of the 4,267 MB, the process's working set fell from 269 to 257 MB and the GC's committed memory from 135 to 123 MB, which a 4 GB burst could not do; the stalled stretches since the last jump, at the rate the counter read just before each, add up to about 3.2 GB, the same order as the jump. The design's 1.3 to 1.6 GB near revision 137 was very likely the same kind of jump; its counters were not kept, so that cannot be checked. **Why the counter stalls** is not established (the Server GC's changing heap count is a candidate); it does not affect the Studio, only this way of measuring it. Run 1's allocation per step is therefore not used anywhere in this analysis; allocation is read from allocation ticks.

**What a heavy step really allocates** (run 3, the allocation ticks, one about every 100 KB, all counted): **about 315 MB** for the step from 621 to 622 (a Rout Phase step) and its render, and about 42 MB for the next step (an Advance Phase step). By the nearest Studio frames:

| Source | MB | What it is |
|---|---|---|
| `ScenarioCards.EmbeddedText` from `GamePlanner.CardOf` | 87 | `ScenarioCardLibrary.Current` (`ScenarioCardLibrary.cs:59-65`) reads a built-in card's text from the assembly's resource on **every** call (`ScenarioCards.cs:211-222`: a stream, `ReadToEnd`, `ReplaceLineEndings`, two strings), though its comment says "a built-in card's once"; the planner's card cache (`CachedCard`, `GamePlanner.CardSetup.cs:35`) is keyed after the read, and only the SHA-256 is kept (`EmbeddedSha256`) |
| `GameState.Find`, `Objects`, `Location`, `At` | about 60 | the linear scans of section 3.5: a `Concat` iterator and a predicate closure per `Find` |
| The board lookup and `BoardHandle.Resolve` | about 40 | section 3.1's per-call allocations, and the location reads |
| `FileAuditSink.Tail` from the Play page's render | 23 | the adjudicator's audit panel (`Play.razor:1140-1145`, rendered for the adjudicator even while its tab is hidden) reads the whole audit log (`File.ReadLines(path).TakeLast(12)`, `LivePlay.cs:244-250`) on every render; the file is 5.5 MB and only grows |
| The store's write and read | about 17 | section 2.3 |

How heavy the other Rout Phase steps are is not measured: only this one was traced for allocation.

## 3. Where the time goes, from the code

### 3.1 The board lookup

Every rules question that touches the map goes through `GamePlanner` to `IBoardCatalog.TryGetBoard`, many thousands of times a revision. On the profiled game (one VASL board, `bd01`, at a pinned version, no footprint) a lookup that hits every cache still does:

| Where | What it does on every call |
|---|---|
| `StudioBoardCatalog.TryGetBoard` (`StudioBoardCatalog.cs:22-37`) | Allocates the closure of the footprint lambda (line 35) even when that lambda never runs; builds a throwaway `InMemoryBoardCatalog` around the one handle (line 36): an array, the catalog, a `Dictionary` from `ToDictionary`, a second lookup of the same board, a second comparison of the 40-character version, and a new `BoardReadResult` |
| `VaslBoardProvider.Load` (`BoardProvider.cs:182-186`) | `GetOrAdd` with a lambda that captures `board` and `this`: a closure and a delegate per call, then a string-keyed lookup |
| `CampaignPlayableFootprints.Read` (`CampaignPlayableFootprints.cs:24-48`) | Two dictionary lookups keyed by (board, version), each hashing the 40-character version; a clock read through `TimeProvider` (32 ns, section 2.4) |
| The call site, `state.Map.Board(board)` (`GameTypes.cs:124`) | A LINQ `FirstOrDefault` with a capturing lambda |

About nine allocations, five or six hashes of the board key, about four hashes or comparisons of the version, and a clock read, per lookup. On a placed map (several boards) it is worse: `GamePlanner.Composed` (`GamePlanner.cs:2054-2073`) looks up every board and builds a new `ComposedMapRead` and `MapLayout` on every call, from 14 call sites, with no cache. cd-guards-5 is on one board, so the measurement does not cover a placed map.

The handle itself is cached (one per loaded board, `ConditionalWeakTable`), which matters: the planner keeps its line-of-sight caches by handle.

### 3.2 Rout obligations and advice (about 450 ms of a Rout Phase step)

The page reads the obligations once per state and view (`routCache`, `Play.razor:6567-6593`), so the cost is not repeated renders. It is repeated searches inside one read:

- `ReadRoutObligations` (`Play.razor:6595-6614`), for each broken unit of mine, calls `MayRout` (which calls `MustRout`), then `RoutSurrenderAdvice` (which calls `MustRout` again, and may search for an Interdiction trap), then `MustRout` (line 6606), then `MustRout` again (line 6612, the same question as line 6606), then `RoutAdviceText`, which runs `RoutAdvice`. `ReadRoutChoices` (line 6616) asks `MayRout` and `RoutSurrenderAdvice` again for the same units.
- **Every `MustRout` builds a new `RoutScan`** (`GamePlanner.Rout.cs:32`). A scan reads the enemy list once (`Enemies`, line 136), but each new scan reads it again, and **for every Known enemy it runs `planner.Encircled`** (line 145), which is the A7.72 search of section 3.3 for each enemy standing upstairs, plus `InterdictionRange`, which calls `state.At`.
- So one read costs about (broken units) x (7 to 10 scans) x (enemies x (an encirclement search + a scan of all objects)), plus the rout searches themselves (`RoutReach`, a Dijkstra search over location and "seen by" state, which reads adjacency, line of sight, and Infantry entry per edge).

Nothing in the planner keeps `MustRout`, `RoutAdvice`, a scan's enemy list, or `Encircled` across calls; only `SurrenderedBeforeRouts` is kept per state (`Rout.cs:53`). The same calls run during a proposal (`PlanRout`, `Rout.cs:408-482`; the phase-end check in `GamePlanner.Proposer.cs:49-50`), so the waste is not only the page's; the follower makes no proposals, so section 2 does not measure that side.

### 3.3 Upper-level encirclement (about 230 ms of a Rout Phase step, 13 ms of any other)

`UpperLevelEncircled` (`GamePlanner.Encirclement.cs:24-38`) hands Rules a reader for a breadth-first search of up to 96 Locations (`ScenarioA1UpperLevelEncirclement.cs:54-90`), which stops when it reaches ground level; a cut-off unit is the worst case, since it searches everything reachable. Per Location it reads:

- the steps: `ChargeNeighbors` (six neighbours, the levels in the hex), then `InfantryStep` for each, which looks the board up, resolves both ends, reads the hexside, scans units and entities for smoke (`HasSmoke`), and asks the weather rules;
- the units there: `state.At(location)`, which runs `Location()` and `Aboard()` for every object in the game, each a linear `Find` (`GameState.cs:401-461`). That is quadratic in the number of objects, per Location visited.

The page asks it once per revision and view for each visible unit upstairs (`CutOffUpstairs`, cached in `cutOffCache`, `Play.razor:5687-5710`): the 13 ms. The rout scans ask it again for every enemy, in every scan (section 3.2): the 216 ms. Fire, movement, and advance ask it for firers, targets, and movers during proposals. Nothing keeps the answer per state.

### 3.4 The unit table's rows (about 13 ms a step)

`UnitRows` (`Play.razor:1287-1294`) is a property with no cache, read on every render of the Play page. Wide layouts keep the Units tab mounted while hidden (`KeepMounted`, `Play.razor:7219-7239`), so it runs on every render whether the tab is shown or not. Each row calls `state.Location`, `Custody` (a scan of all units), `CargoNotes`, `VehicleNotes`, and `GamePlanner.IsBurning` (a scan of entities): quadratic in the number of objects, again per render. A confirm renders the page twice; any click renders it again.

### 3.5 What they have in common

- **`GameState` has no index.** `Find`, `Unit`, `Location`, `Aboard`, and `At` are linear scans (`GameState.cs:401-461`), and the rules readers call them inside loops. A `GameState` is a `sealed record`, a snapshot of one revision, so an index built once per state is safe.
- **The planner forgets between calls.** It is a singleton shared by every Blazor circuit (`LivePlay`, `Program.cs:37`), and the same `GameState` object is reused across refreshes when the events have not changed (`GamePlanner.Replay`, `GamePlanner.cs:162-181`). A memo per state, in a `ConditionalWeakTable<GameState, ...>` as `surrenderedFirst` already does, lives exactly as long as the state and is shared by the page, the proposal, and every circuit.

## 4. The fixes

Each with its design, its expected gain (an estimate against section 2's figures), its cost to build, its runtime cost, its risk, and the tests it reaches. Ordered by what a player waits for.

### B. Rout obligations and advice

| | B1: the page asks each question once | B2: the planner keeps rout answers per state |
|---|---|---|
| Design | One pass over the broken units that reads, once per unit, `MustRout`, the surrender advice, and the advice, and builds both the obligations and the choices from it (removes the duplicate at line 6612 and the second pass in `ReadRoutChoices`) | A per-state rout memo: the enemy list per side (`RoutScan.Enemies`), `MustRout` and `RoutSurrender` per unit, `RoutAdvice` per unit; one shared, thread-safe store per state |
| Gain (estimate) | From 7 to 10 scans per broken unit to about 3 | From about 3 to 1 scan per unit, and the enemy list (with its encirclement searches, 216 ms) read once per side per state instead of once per scan; the proposal and the phase-end check gain too |
| Cost to build | About 20 minutes | About 60 minutes: the memo, its thread safety, tests of reuse |
| Runtime cost | None | A few dictionaries per state in the Rout Phase |
| Risk | Low: the same answers, fewer reads | `SurrenderedBeforeRouts` reads routes while its own set is still empty (`Rout.cs:56-58`): a memo filled during that read would keep answers made "before the surrenders". The memo must not keep what is read inside that window, or must key on it |
| Tests reached | The Play page's rout tests (`PlayPageDemoFixesTests`, `PlayPagePass28bTests`, `PlayPagePass31Tests`, `PlayPagePass31dTests`) | Those, and the planner's rout tests (`BacklogPass10Tests`, `BacklogPass13Tests`, `BacklogPass20TablePlayerTests`, `BacklogPass31dTests`, `BacklogPass35Tests`) |

B2's risk is the one real design point in this analysis and gets its own test: a Rout Phase where one unit surrenders before the other side routs (the A20.21 example on p. 69), compared with and without the memo.

### C. Upper-level encirclement

| | C1: `Encircled` kept per state | C2: an index of a state's objects |
|---|---|---|
| Design | `UpperLevelEncircled(state, unit)` memoized per (state, unit id) in the planner. The rule counts unconcealed enemies only, so the answer does not depend on the view | Per state, built once: objects by id, each object's resolved location, the objects in each Location. `state.At`, `Location`, and `Find` as the readers use them read from it, as a planner-side memo |
| Gain (estimate) | The search runs once per upstairs unit per state, instead of once per rout scan per enemy: most of the 216 ms inside the enemy lists, with B2 or without it | `state.At` from quadratic to a lookup (138 ms of a Rout Phase step under `At`, before C1 and B2 cut the calls), and about 60 MB of a heavy step's allocation |
| Cost to build | About 20 minutes | About 45 minutes |
| Runtime cost | One set per state | One dictionary per state, a few hundred entries |
| Risk | Low: the answer is a pure function of the state | Low as a planner memo. Inside `GameState` (not proposed), a lazy field on a record would have to be kept out of equality and `with` copies |
| Tests reached | The A7.72 rules tests (`ScenarioA1Pass45RulesTests`), the planner's encirclement tests (`BacklogPass12Tests`, `BacklogPass26Tests`, `BacklogPass35Tests`); no planner-level test of `UpperLevelEncircled` exists, so one is added | The planner tests broadly; the Play page tests that read units |

### H. A built-in card's text, read once

- **Design:** keep each built-in card's text in a static dictionary, as its SHA-256 already is (`ScenarioCards.EmbeddedSha256`), so `EmbeddedText` reads the resource once per name. The text is immutable within a build.
- **Gain:** about 87 MB of the heavy Rout Phase step's 315 MB, with the time to read and copy it; every other `CardOf` caller gains as well (how often they run outside the Rout Phase is not measured).
- **Cost:** about 10 minutes and one test. **Runtime cost:** one string per built-in card read. **Risk:** low. **Tests:** the scenario card tests in `LimboDancer.Domains.Asl.Play.Tests`.

### I. The audit panel without reading the whole log

- **Design:** `FileAuditSink` keeps its last lines in memory as it writes them (a small ring, filled from the file's end once at start), and `Tail` reads from it; or `Tail` reads back from the file's end.
- **Gain:** a 5.5 MB file read and about 11 MB of strings per render of the adjudicator's view, growing with the log.
- **Cost:** about 20 minutes. **Runtime cost:** a dozen lines. **Risk:** low; the other Studio's lines (the two Studios share the folder) are seen by the read-from-the-end variant, not by the ring, which is the choice to make. **Tests:** the audit sink's tests and the adjudicator panel's page tests.

### D. The unit table's rows

- **Design:** cache `UnitRows` per (revision, view), as `routCache` and `cutOffCache` do. `Words` (the cascaded "Say") is fixed for a page's life; if not, it joins the key. While there, `cutOffCache` keys on `viewAs` and `routCache` on `Viewer.Name`: one key for both.
- **Gain:** about 13 ms a render, built once per revision instead (a confirm alone renders twice; every click renders again).
- **Cost:** about 15 minutes. **Runtime cost:** one list per page. **Risk:** low. **Tests:** the Play page's unit table tests (`PlayPageTests` and the pass tests that read `#play-units tr`).

**Not proposed: rendering hidden tabs only when shown.** Dropping `KeepMounted` for the Units tab would stop the work when the tab is closed, but many page tests read the hidden table, and one asserts it stays in the page (`PlayPagePass31cTests.cs:403`). With D's cache the hidden table costs one build a revision, so the gain left would not repay rewriting those tests.

### A. The board lookup

| | A1: an allocation-free hit | A2: the result cached per (board, version) | A3: the map resolved once per state |
|---|---|---|---|
| Design | Return the handle's `BoardReadResult` directly, without the throwaway catalog (check the version inline); make the lambdas non-capturing (`GetOrAdd(key, static (k, arg) => ..., arg)`); keep the footprint read | A `ConcurrentDictionary<(BoardRef, string), Entry>` in `StudioBoardCatalog` holding the finished result, the footprint generation it was made for, and when an absence is next checked; `Publish` bumps the generation | A per-state `MapContext` in the planner (`ConditionalWeakTable<GameState, MapContext>`): each board's handle, the composed read of a placed map, `Map.Board` by key. `ReadLocation`, `Step`, and `Composed` read from it |
| Gain | Most of the lookup's allocation (near 40 MB in a heavy step); its time is unknown (section 2.4), so a count of calls and a stopwatch come first | Most of what A1 leaves: one lookup and two number compares a call | The catalog hit once per board per state; on a placed map, the layout no longer rebuilt per call |
| Cost to build | About 20 minutes, with the measurement | About 45 minutes, with a test of the generation | About 45 to 60 minutes: several call sites in the planner |
| Runtime cost | None | One entry per pinned board version; must be bounded (as `AuthoredBoardService`, 64 versions) or it holds every version's facts | One small object per live state, collected with it |
| Risk | Low: the same handle, the same result | A footprint published by the other Studio must still be seen within the 5-second window; the absence check stays per entry | A footprint published mid-revision is seen at the next state, not the current one (acceptable: a state is a snapshot) |
| Tests reached | `StudioBoardCatalogTests`, `CampaignPlayableFootprintsTests` | the same | the planner tests (`LimboDancer.Domains.Asl.Play.Tests`), the Play page tests that use a placed map |

A2 is not proposed: A3 does what it does and more for the planner's calls, without a cross-process cache to keep correct. A3 and C2 are held until the measurement after B2 and C1 shows what is left.

### J. The store's whole-file rewrite (backlog)

48 ms a step at cd-guards-5's end, growing with the game, on every confirm. Changing it (an append-only record, or the parsed game kept in memory with a write behind) is a storage design with the store's atomicity and the shared boards folder to keep; it belongs in its own design, not in this pass. In the backlog (section 7).

## 5. The cost in all

### 5.1 Building it

Estimates from the time log of the last two days (the footprint and set-id fixes with their tests took 26 minutes; E's tasks 7 to 23 minutes each; a gate with the strict build about 15 minutes; this measurement 25 minutes):

| Task | Estimate |
|---|---|
| 1. B2 and C1, the per-state rout and encirclement memo, with the surrender test | 1 h 30 |
| 2. B1, the page's single pass | 20 min |
| 3. H, the card text; I, the audit tail; D, the unit rows | 45 min |
| 4. A1, with its count and stopwatch first | 20 min |
| 5. Measure after (the same three runs), the Studio check of Play (the Rout Phase panel, the unit table, cut-off units, the adjudicator's audit panel), the results written | 45 min |
| 6. The gate (the strict build, the tests the change reaches, then the projects at the final gate) and the pull request | 30 to 45 min |
| **In all** | **about 4 to 5 hours**, one branch and one pull request |

### 5.2 At runtime

- **Memory:** per live state, a context of a few dictionaries (rout answers, encirclement answers, and with C2 objects by id and by Location). Small beside a state, and collected with it. H keeps a few card texts; I a dozen lines.
- **Correctness:** every memo is keyed by a `GameState` object, an immutable snapshot, so no memo can go stale within a revision. The one exception to watch is B2 inside `SurrenderedBeforeRouts` (section 4, B).
- **Concurrency:** the planner is shared across circuits; each per-state store is filled under a lock or with concurrent collections, and a duplicate fill is harmless (the answers are the same).
- **The rules:** unchanged. Rules is asked the same questions with the same facts, fewer times.

### 5.3 What it should buy

Estimates against section 2's figures, to be replaced by task 5's measurement:

| | Measured (section 2) | After the fixes (estimate) |
|---|---|---|
| A Rout Phase step's work | about 567 ms (median) | about 150 to 250 ms |
| Any other step's work | about 122 ms (median) | about 100 ms (D, I, and H where `CardOf` is read) |
| The heaviest traced step's allocation (621 to 622) | about 315 MB | under 150 MB (H, C2's share with C1, A1) |
| Proposals in the Rout Phase (`PlanRout`, the phase-end check) | not measured (the follower makes none) | faster by the same memos |

## 6. For the user to rule on

| # | Question | Proposal |
|---|---|---|
| P1 | Measure again before any fix | **Approved and done** 2026-10-19 (section 2) |
| P2 | Which fixes | B2 and C1, B1, H, I, D, and A1 with its measurement first; A3 and C2 only if the measurement after asks; not A2; not lazy hidden tabs. **Approved** 2026-10-19 |
| P3 | Branches | One branch, one pull request. **Approved** 2026-10-19 |
| P4 | Where the index of a state's objects lives (C2, if built) | In the planner's per-state context, not in `GameState`. **Approved** 2026-10-19 |
| P5 | The store's whole-file rewrite (J) | To the backlog now, for its own design; not in this pass. **Approved** 2026-10-19 |

The user's word on 2026-10-19: "P2 to P5 approved, write the plan". The plan is `ASL Map Studio Play Step Plan.md`, beside this analysis.

## 7. To the backlog

Section 65 of the backlog:

- The step that closes setup takes 10 to 13 s with the server mostly idle (section 2.2).
- The store rewrites the whole game file at every append (section 2.3, J).
- `dotnet-counters`' allocation total stalls and jumps on the Studio (section 2.5): allocation per step is read from allocation ticks until the cause is known.

Section 62's 1.3 GB entry is corrected: a counter artifact.

## 8. Measured after the fixes (2026-10-19, branch `play-step`)

The Play Step Plan's tasks 1 to 7 (C1, B2, B1, H, I, D, A1), measured as section 2 was: my Studio on port 6670, Debug, built from `play-step` at f95674a6, the follower of cd-guards-5 as the adjudicator, the page in front. The scripts are in `code-builders/diagnostics/play-step/`. Each task's own Studio test is in the plan's time log; this section is the whole measurement of task 8.

**Behaviour:** the last pass of the plan's task 8, through the German Rout Phase of turn 4 step by step (729 to 741) as the German side and as the Russian side, and each task's own pass as the adjudicator at 621, 622, and 729 to 744: the Rout panel's obligations, choices, and routes, and the unit table's rows, read the same in my Studio and in the "before" Studio built from `main` (port 6671), by a hash of each list's text.

### 8.1 A step, by phase (run 1, all 898 revisions, 256 steps of play)

| Phase | Steps | Work a step: median | mean | 90th percentile | most | Process CPU a step, mean |
|---|---|---|---|---|---|---|
| All play | 256 | 108 ms (129) | 128 ms (185) | 176 ms (300) | 646 ms (1,206) | 137 ms (207) |
| **Rout Phase (rtph)** | **33** | **258 ms (567)** | **262 ms (567)** | **387 ms (907)** | **646 ms (1,206)** | **276 ms (597)** |
| Every other phase | 223 | 104 ms (122) | 108 ms (129) | 139 ms (163) | 232 ms (485) | 117 ms (149) |

Section 2.2's figures are in brackets. A Rout Phase step costs about 2.5 times any other, where it cost 4.6 times; the plan's estimate was 150 to 250 ms (section 5.3), so the median sits at the estimate's upper end. Every other step is about 18 ms cheaper, more than D and I were expected to give alone; the two short runs per task could not see the difference (their run-to-run noise was ±40 ms).

### 8.2 Where a Rout Phase step's time goes (run 2, traced to 621; 13 Rout Phase steps, 155 others)

| Frame, inclusive, mean a step | Rout Phase: before (section 2.3) | after |
|---|---|---|
| The page's rout read (`RoutObligations`, and `RoutChoices` folded into it by B1) | 366 + 91 ms | 125 ms |
| the enemy lists (`RoutScan.Enemies`) | 293 ms | 3.6 ms |
| `MustRout` | 248 ms | 6.6 ms |
| `UpperLevelEncircled`, all callers | 230 ms | 11.7 ms (the page's `CutOffUpstairs`, once a revision) |
| `RoutAdvice` | 202 ms | 117 ms |
| `InfantryStep` | 182 ms | 82 ms |
| `GameState.At` | 138 ms | 7.5 ms |
| `FollowNext` (the store's append and read, then `Refresh`) | 51 ms | 47 ms |
| The unit table's rows | 14 ms | 12 ms (once a revision now; before, once a render) |

**What is left in the Rout Phase is `RoutAdvice`'s own searches:** about 117 ms a step, nearly all of the page's rout read, each a `RoutReach` over the map whose steps ask `InfantryStep`. The searches are not repeated any more (each runs once a unit and state); they are the work itself. The board lookup is charged 116 ms in this trace, all of it under `CampaignPlayableFootprints.Read`'s clock read, which section 2.4 showed the sampler cannot be trusted on; task 7's count (section 8.4) is the figure to use.

**A stall outside this work:** in run 2 one step's `LivePlay.FollowStep` (the store's append, revision 420 to 426) took 23 s; every other step's took about 45 ms, and run 1's step there took 1.1 s in all. The cause is not established; a file the store reads or writes held by another process (the case of backlog 64, S2) is the likeliest. In the backlog (section 65).

### 8.3 The heavy step's allocation (run 3, the step from 621 to 622)

| | MB |
|---|---|
| Section 2.5, before the fixes | about 315 |
| After tasks 1 to 5 (H and I) | 135 (no `EmbeddedText`, no `FileAuditSink.Tail`) |
| After task 7 (A1) | **101** |

The plan's estimate was under 150 MB. What is left, by the nearest Studio frames: `BoardHandle.Resolve` under `GamePlanner.Step` (23 MB), `InfantryStep`'s own facts and strings (about 20 MB), `MapInPlay.Board`'s capturing lambda at the call site in Units (about 7 MB), and the store's write and read (about 10 MB).

### 8.4 The board lookup, counted (task 7)

The harness the plan named (a Release console that builds the planner as the Studio does) did not build within its 30-minute box: its build on its own output path printed nothing for 35 minutes (as the first test run on a new output path did in task 3). The cause was found in task 9: such a build copies every other output folder under MapStudio's `bin` into its own output, and old passes' folders had nested into each other there (backlog section 65; the standing rules' Studio lessons). The plan's fallback was used: a counting, timing decorator around `StudioBoardCatalog.TryGetBoard` in my Studio's build only, never committed, read through the follower after tasks 1 to 6.

| Step (the page's work at the state it reaches) | Lookups | Their time | Share of the step's work |
|---|---|---|---|
| 728 to 729 | 105,958 | 113 ms | about 10% |
| 729 to 731 | 94,736 | 131 ms | about 17% |
| 731 to 732 | 72,450 | 81 ms | about 15% |
| 621 and 622 | 82,461 and 66,728 | 80 and 62 ms | about 13% |
| 809 | 76,658 | 105 ms | about 13% |
| Any step outside the Rout Phase | about 480 | under 1.5 ms | under 1% |

About 1.1 µs a call, the decorator's two clock reads included. More than a few per cent, so A1 was built; its gain shows in allocation (section 8.3), not separately in time, since the short runs' noise is larger than the 10 to 20 ms it can save.

### 8.5 C2 and A3 (the plan's task 8 question)

The Rout Phase step is at the estimate's upper end, not well above it, so neither is needed for the plan's goal. If a later pass wants the Rout Phase cheaper still, the trace points at **A3** (the map context per state) before C2: what is left is `RoutAdvice`'s searches, whose cost per step is `InfantryStep` (82 ms), with `BoardHandle.Resolve` and the board lookup inside each step; `GameState.At`, which C2 would index, is down to 7.5 ms. Neither is built in this branch (ruling P2); A3 is proposed to the user in the pull request, not decided here.

**Since: A3 built (the A3 Map Context Plan, branch `map-context`, 2026-10-19).** The map context per state (each board's handle, a placed map's composed read) and, at the user's ruling, A3b (each Location resolved once a state). A Rout Phase step's median fell from 258 to 229 ms in run 1, and from 485 to 365 ms against `main` under the same machine load; its board lookups from about 28,000 to one, its `Resolve` calls from 34,472 to 180, the step from 621 to 622 from 101 to 68 MB; on a placed map (the Gambit card) a Rout Phase step from 22 to 49 s to under a second. The plan's section 10 has the figures. What is left of a Rout Phase step is `RoutAdvice`'s searches themselves; C2 stays the next candidate, not built.
