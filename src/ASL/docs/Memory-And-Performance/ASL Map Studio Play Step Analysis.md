# ASL Map Studio Play Step Analysis

**Written:** 2026-10-19, at the user's word: a full analysis of the design and implementation of the fixes for what the SVG Memory Analysis left open (its section 14, "What is left"), and their cost.
**Source:** the SVG Memory Analysis, sections 13 and 14; the Counter Layer Design, sections 7.3 and 16; the backlog, section 62. Code read on `main` at 9ff197dd: `MapStudio` (`Play.razor`, `StudioBoardCatalog`, `CampaignPlayableFootprints`, `BoardProvider`, `LivePlay`), `Play` (`GamePlanner` and its `Rout`, `Encirclement`, `Map`, `Terrain`, `CloseCombat` parts), `Rules` (`ScenarioA1RoutCalculator`, `ScenarioA1UpperLevelEncirclement`), `Units` (`GameState`), `Maps` (`InMemoryBoardCatalog`, `BoardRef`).
**Status:** analysis only; no code is changed. Every figure below that predates option E is marked so; section 2 proposes measuring again first.
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
