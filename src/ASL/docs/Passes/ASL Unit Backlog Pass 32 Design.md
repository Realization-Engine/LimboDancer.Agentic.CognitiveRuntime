# ASL Unit Backlog Pass 33 Design

**Status:** Preparation finished 2026-10-05, read-only, and **answered by the user the same day (section 15)**: the migration is pass 33, carved into sub-passes 33.a, 32.b, and on, and all of it comes before the rule passes. Not started. This design was written as "pass 32.a" with the pass numbers of that hour; the file was renamed and the numbers of later passes moved up by one at the user's answer. Sections 1, 10, 11, and 13 are the proposal as it was put; section 15 is what was decided.

**Date:** 2026-10-05

**Related documents:** the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 19, decision 7, and section 22.1; the [rule inventory](<../ASL Rule Inventory A to E.md>); the [rule coverage](<../ASL Rule Coverage.md>), section 8.3; the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>); this design's [appendix](<ASL Unit Backlog Pass 33 Design Appendix.md>), which lists every member that holds rule logic outside the Rules project.

**The rulebook.** This design states no rule. Every rule number in it is the code's own citation, copied from a comment or a refusal text, and is used only to name the code that moves. No page of the PDF was read for it, as the task allows for a rule that only moves.

## 1. Outcome

1. **The move is far larger than one pass, and larger than two.** Outside the Rules project, 1,006 members and blocks hold rule logic, in 521 members of Play and Units that would have to be split or moved. Sized by the plan's task sizes they come to 223:10 of build. With the overhead of the passes that work would fill, that is about 247 hours on the plan's basis and about 123 hours likely at the week review's pace. All of section 22.1, the 31 rule passes, is 195:45 on the same basis. Section 10 gives the figures and what they rest on.
2. **Almost nothing moves as it is.** 67 members are pure functions. The rest read the state or the map and decide in one body: 798 blocks can be split into "gather the facts, then decide", and 132 decide inside a search or a scan.
3. **The move can be made without changing behavior,** but only under four constraints the reading found (section 5.3): the four recorded fact records keep their exact shape, the order of checks is kept, refusal texts are kept to the character, and where two copies of a rule disagree both are kept.
4. **The two questions.** LOS stays where it is, as you said during this session (decision D8). For the projector I recommend that Units gain a reference to Rules (decision D7).
5. **My recommendation on the schedule** (question 2): keep decision 7 whole, make pass 32.a the foundation (the reference, the conventions, the proof, the pure layer), and move each subsystem in its own migration pass just before the block of rule passes that first changes it. This departs from "one migration pass before pass 45", so it is yours to decide; the alternative, all of it first, is laid out beside it.

## 2. The checks

- `main` at aba0210, level with origin; only `src/ASL/boards/` untracked.
- CI run 37313790418 for aba0210 was in progress at the session's start and ended **green** (8m43s).
- My script over the inventory's Where column gives the figures of 2026-10-05 again: 926 rows with logic; 217 name only Rules files; 222 name Rules and another project (192 Play, 18 Play and Units, 7 Units, 5 Maps and Play); 487 name no Rules file (325 Play, 84 Play and Units, 48 Maps, 21 Maps and Play, 8 Units, 1 Maps and Units).
- Line counts and references are as the task gave them: Play 24,288 lines in 53 files, Rules 16,008 in 49, Units 12,797 in 38, Maps 11,398 in 48. Rules references Abstractions only.

## 3. The line

**A fact** is read, not decided. **A rule** is what the rulebook decides from facts. **Plumbing** is neither.

| | What it is | Examples from the code |
|---|---|---|
| Fact | A unit's Location, side, kind, and conditions; what it holds; a hex's terrain name, base level, a hexside's terrain; the distance in hexes; a LOS result; a printed value from the catalog; a recorded dr; an event's fields. | `GamePlanner.Map.cs`: `ReadLocation` (the read of one Location), `Step` (two reads, whether the hexes are adjacent, the hexside crossed), `Los` (the LOS result, with its cache). `GameState.Location`, `Aboard`, `Passengers`. `LosResult.Range`. |
| Rule | A cost (MF, MP); a DRM; a legality ("may not move, it Prep Fired"); a result read from a DR; the choices a rule gives a player; an effect a rule orders; a definition or threshold; a table of rule values. | `GamePlanner.Terrain.cs`: `InfantryStep` and `GroundStep` (the cost of a step, and why a step is refused). `GamePlanner.Map.cs`: `IsAdjacent` (ADJACENT as the code reads A.8), the "within 16 hexes" of `EnemyGoodOrderInLosWithin16`. `GameProjector.cs`: `ChangePhase`, lines 457 to 469 (which markers a phase's end removes), `StepMovement`, lines 1852 to 1856 (A3.3). `VehicleCheckRolled.For` (a result table that sits on a Units event record). |
| Plumbing | Building an event, event ids and step numbers, that a named roll exists and has the right shape, JSON, caches, the store, UI words that state no rule. | `GameGate.cs`, `GameStore.cs`, `GameActions.cs`; the projector's `CheckEnvelope`, `Replace`, `Fail`. |

Three points settle the hard cases.

- **A definition is a rule.** "Good Order", "ADJACENT", "Known enemy", "Inexperienced", "the unit's US#" are decided by the rulebook, so they move, even where the code is one line.
- **A derived fact is a rule.** `LiveFire.FromState` hands the Fire calculator a record of facts, but about twenty of its "facts" are decisions: the fire kind from the firers' markers, who is attacked, whether a SW-only use counts as having fired, when a director counts as having fired. Each is decided in Play before the calculator is called. They move.
- **A reading of the board is a fact, even when it took work.** The LOS between two Locations, the range, the Locations a building hex has, the terrain a board prints in a hex: Maps reads them, and Rules takes them as given (decisions D8 and D9).

Everything on the rule side moves, except what D8 and D9 keep in Maps.

## 4. How the inventory was made

Fourteen read-only readers worked in the main checkout, one for each group of files: nine for Play (all 53 files), three for Units (`GameProjector.cs` in two halves, and the state model), two for Maps (the LOS code, and the rest). Each read its files in full and wrote one row for each member, or each block of a long member, with its class, the rules it cites, what it decides, the facts it needs, what it calls, its dice, its refusal texts, how it could move, and a receiver. A script checked that every row has its 14 fields and that the rows cover each file: 1,897 rows, none malformed, every one of Play's 53 files assigned.

**What the harness did.** It refused thirteen of the fourteen readers the writing of their findings files. Each returned its findings in its closing message instead, and I keep a digest of them. The rows files were written as asked.

**What I checked myself.** I read in the code: all of `GamePlanner.Map.cs`; `GamePlanner.Terrain.cs`, lines 1 to 140; `GameProjector.cs`, lines 403 to 470 and 1843 to 1960, and its member list; `LiveFire.cs`, lines 738 to 903 (the fire record's verifier); `ScenarioA1StackingCost.cs` and `FireRange.cs`; the public entry points of the seven Rules calculators; the four project files and who references Units and Rules; the lock-file and restore settings; that no test project has access to the internals of Play, Units, Rules, or Maps. Everything the decisions of section 6 rest on is in that list. **Every other row is a reader's, taken as reported.** The appendix's rule numbers and "facts it needs" are the readers' words.

**The appendix** lists the 1,006 rows that hold rule logic, file by file, and for each of the inventory's 222 split rows what sits outside Rules.

## 5. What the reading found

### 5.1 The size, by project

| Project | Files with rule logic | Rule-bearing rows | Lines in those rows | Pure (WHOLE) | Split | Search | Refusal and diagnostic texts |
|---|---:|---:|---:|---:|---:|---:|---:|
| Play | 44 of 53 | 731 | 15,859 of 24,288 | 45 | 603 | 83 | 1,224 |
| Units | 9 of 17 read | 160 | 3,555 (3,193 in `GameProjector.cs`) | 11 | 144 | 5 | 257 |
| Maps | 8 of 16 read | 116 | 2,496 (1,630 in `LosCalculator.cs`) | 21 | 51 | 44 | 137 |

The lines include each member's comment. Nine files of Play hold no rule logic: `GameActions.cs`, `GameGate.cs`, `GameStore.cs`, `RefusalReasons.cs`, `CardProvenance.cs`, `ScenarioSetupPlans.cs`, `ScenarioCardLibrary.cs`, `LiveCaseSnapshots.cs`, and `BoardCatalogTerrainEvidence.cs` (a fact adapter). `GameGate.cs` enforces no sequence of play: it plans again through the planner.

**Against the Where column.** The code outside Rules cites 496 of the inventory's rules by number (310 in Play alone, 154 in Play and Units, 16 in Units alone, 14 in Maps alone, 2 in Maps and Play). The Where column names an outside file for 709 rows. 450 rows are in both lists; 46 are cited by the code and not named by the column (26 built, 19 partly, 1 with a deviation); 259 are named by the column and not cited by number in the code, most of them LOS rows and rows whose code cites the parent rule. The column is a guide, as the task said.

### 5.2 Where the rule logic sits

- **The planner decides before it asks.** `LiveFire.FromState` makes about 20 rule decisions with 17 ordered refusals before `ScenarioA1FireCalculator` runs; `LiveOrdnance.FromState` and `Panzerfaust` about 20 refusals and a dozen derived values; `LiveRally` and `LiveCloseCombat` fewer. A rule such as the TI bar on fire (A4.8) exists only in Play, because the calculator's record has no TI fact.
- **The planner decides after it rolls.** Results are decided inside the roll closures: the Random Selection reveal, the OVR NTC with its TEM, a failed hedge Bog DR that changes the step, the Sniper's results (built in `GamePlanner.Snipers.cs`, not in the Fire calculator), the Casualty dr of Mopping Up.
- **Whole subsystems have no calculator at all:** Infantry movement and terrain costs, the rout, vehicle movement and its costs, Overrun declaration and PAATC, Passengers, Recall, night and weather, Starshells, setup and entry, Victory, concealment gain and loss.
- **The projector re-checks and applies.** Of its 172 rows, 131 hold rules: legality re-checks (phase, side, the barring conditions) and effects (which markers a phase's end removes, Melee, Guards, placed charges). It never re-checks a cost, an MF or MP allowance, or ADJACENCY: those are the planner's alone.
- **Rule tables sit on Units event records,** pure and called by both planner and projector: `VehicleCheckRolled.For`, `RoutInterdicted.For`, `ShockRecoveryRolled.For`, `ManhandlingRolled.For`, `SmokeAttempt.Placed`. The Repair result has no such function and is written inline in the projector.
- **The same rule is written many times.** The readers named well over a hundred pairs, some of them the same pair seen from both sides. The ones that matter to the move: "Good Order" has three definitions that differ (`GameState.GoodOrder`, `GamePlanner.GoodOrder`, the Close Combat calculator's); the US# by kind is written five times; overstacking three times with three counts of SMC, and a fourth at setup; Guard capacity five times and who may guard five times, differently; the grain season is June to September at twelve places and April to September at three; Target Facing three times and the Covered Arc wedge five; Inexperience twice; the planner and the projector both hold most step legality, and differ on a TI vehicle, a bogged vehicle, the rout MF of a wounded SMC, and the CE test of a vehicle MG repair.
- **Maps outside LOS cites no rule.** None of its files names an ASL rule number, and it holds no TEM, no entry cost, and no playable-hex test. What looks like rule there is the reproduction of VASL's board reading: the Locations a hex has, how boards join, what an SSR's terrain change does to the grid.
- **The LOS code is a port of VASL's `Map.LOS`.** Its rules run once for each cell of the line, read the pixel grid as they decide, and rewrite the facts later rules read. A LOS result's equality includes the reason text and the blocking cell. Rules already takes LOS as a fact: `FireLos` (blocked, the hindrance DRM, grain in the LOS), filled by Play.

### 5.3 What constrains a move that changes nothing

1. **The recorded fact records are frozen.** `FireRecordVerifier.Verify` rebuilds the `FireAttack` from the state, serializes it, and compares the text with the record (`LiveFire.cs`, line 883); the Rally, Close Combat, and Ordnance verifiers do the like. A field added to, taken from, or renamed in `FireAttack`, `RallyAttempt`, `CloseCombatFacts`, `VehicleCloseCombatFacts`, `AmbushFacts`, or `OrdnanceShot` makes every recorded game fail. So the decisions of the `FromState` readers move into Rules as builders that take new, unrecorded fact records and return these records unchanged.
2. **The order of checks is behavior.** `PlanMove` runs some thirty checks, `PlanFire` and `FireMapFacts` about forty, `PlanMoveVehicle` about thirty-eight texts, `PlanAdvance` about twenty; the first that fails is the refusal a player sees. In the projector the order decides which UNIT-STATE text is reported, and several unrelated conditions share one text.
3. **Refusal texts carry values of Maps and Units types:** a `BoardLocation`, a hexside direction, a unit id, a terrain name. A calculator that words a refusal takes those as strings.
4. **Where two copies disagree, both are behavior.** The three Good Orders, the two grain seasons, the planner's and the projector's differing checks: each stays as it is, under its own name in Rules, until pass 45 or a later pass decides.
5. **Some decisions read a state that the plan itself makes.** `PlanMove`, `PlanRout`, `VehicleStepPlan`, `PlanCloseCombat`, `Snipe`, and others replay the game with the events built so far and decide the next thing on that state; some build an altered state to ask a question. The replay stays in Play; what is decided on its result moves.
6. **Searches read as they go.** The rout search keys each node on a Location and a mask of the enemies that have seen the unit, so each step's verdict depends on LOS reads made during the search, held in five caches. The berserk charge and the Recall route are searches of the same kind; off-board entry scans every hex; the concealment and Starshell rules scan every enemy with a LOS read for each.
7. **Rules decided by asking the package.** `CloseCombatRequired`, `MandatoryAttackDecidable`, `BerserkOverrunPending`, and the Overrun declaration decide by building facts and running a Rules `Precheck`. These are already Rules decisions with a planner loop around them.

## 6. Decisions

**D1. One calculator an action, in the shape Rules already has.** For each thing the game decides (a move step, a rout step, a vehicle step, a setup, a phase's end) Rules gets a facts record, a calculator, and a verdict record, as `FireAttack`, `ScenarioA1FireCalculator.Resolve` and `Precheck`, and `FireResolution` are today. The verdict is the first refusal, or the values decided (costs, markers, effects in Rules' own words). Play gathers the facts, calls, and turns the verdict into events.

**D2. The facts are plain.** Ints, bools, strings, and Rules' own records. A three-valued fact (true, false, unknown) is a `bool?`, as Rules has it now; Units' `ConditionState` does not cross. A Location, a unit, a hexside is named by the string Play gives it, and the calculator uses that string in a refusal text as it stands.

**D3. The order is kept by moving the checks in their order.** A calculator returns the first refusal its checks give, in the order the member had them. Where a member gathered a fact only after earlier checks passed, the fact becomes nullable and the check that needs it comes where it came. A check's place never changes in this pass.

**D4. A fact reader for a search, and only for a search** (question 3). Where a decision is made inside a search or a scan (132 rows, in about 25 members), the calculator takes a small interface declared in Rules, which Play implements over the state and the map and which returns facts only: for the rout, the neighbors of a Location, whether one is playable, the facts of a step, the enemies that see a Location. The search, its node, its mask, and its order move into Rules; the reads and their caches stay in Play. Locations are strings to Rules. No interface gives Rules the state or the map as a whole.

**D5. The recorded fact records keep their shape.** No field of the six records of section 5.3 is added, removed, renamed, or reordered. Decisions that `FromState` makes move into builders in Rules that take new fact records (never serialized) and return these.

**D6. Nothing is unified.** Two copies of a rule that give the same result for every input may become one function; I will say so in the slice's commit and the proof must show it. Two copies that differ in any input stay two functions in Rules, each named for where it is used, and go on the list for pass 45.

**D7. Units references Rules** (question 4; recommended). The projector calls the same calculators as the planner. See section 7.

**D8. LOS and range stay where they are** (the user, 2026-10-05, in this session: "I do agree that LOS and Range calculations should remain in Play"). The LOS calculation sits in the Maps project (`Los/LosCalculator.cs`) and Play is its only caller in the game (`GamePlanner.Map.cs`, lines 72 to 115); I take the user's word to mean it stays there and Play hands Rules the result as a fact (question 5 asks to confirm that reading). What moves is what Play decides from a LOS result: "within 16 hexes", the recount of hindrances by terrain and season in `FireMapFacts`, the low-visibility DRM and its block at 6, who counts as seeing.

**D9. Board reading stays in Maps** (question 6; recommended). The Locations of a hex, how boards join, the SSR terrain changes, and the terrain type's LOS flags reproduce VASL's reading of a board and cite no rule. They are facts by the line of section 3. The inventory's rows that name them keep their Maps files.

**D10. Old public names stay as forwards.** A public member that tests or the Studio call (`VehicleCheckRolled.For`, `GameState.GoodOrder`, `Experience.Inexperienced`, `ScenarioSetup.Check`, `GamePlanner.FireBar`, and the like) stays where it is as one line that calls Rules. So **no test file is edited by the move**: not an assertion, and not a namespace either. If a slice finds a test that cannot stay untouched, the slice stops and I say so.

**D11. Card records stay in Play.** `ScenarioCard` and its parts are the JSON format of a card. The setup, entry, and Victory calculators take their own fact records, built from a card by Play.

**D12. What does not move, and why.** `RefusalReasons.Sentences` and the action descriptions restate rules as prose for the page and hold no logic. `GameActionExecutor.EffectHolds` reads back what a plan did. `UnitPlausibility.Check` warns about a counter's drawing in the Units Lab and serves no play (question 7).

**D13. Faults are written down, not fixed.** The readers reported about 170 things that look wrong against the code's own comments. Section 12 lists the ones that bear on the move. None is fixed in this pass.

**D14. File names.** New Rules files take the `ScenarioA1` prefix, as the task says, unless the user says otherwise.

## 7. The two questions

### 7.1 LOS

Answered by the user in this session: it stays (D8). The reading agrees. The LOS code is VASL's algorithm ported cell by cell; its tests compare its results with VASL's fixtures, reason text and blocking cell included; to move it Rules would need the pixel grid, the terrain catalog, and the hex geometry, which are the map. Rules and Units name no LOS type today, and Rules already takes `FireLos` as a fact.

One thing follows for a later pass, not this one: the LOS code values a hindrance as VASL does (1, 2, or one half, the sum rounded down, six blocks), and `FireMapFacts` discards those values and counts again by terrain name and season, while the rout uses VASL's total as it stands. The recount is a rule and moves to Rules in the Fire slice. That two counts exist is on the list for pass 45.

### 7.2 The projector's checks

**Recommended: Units gains a project reference to Rules.** Rules references Abstractions only, so there is no cycle; Units keeps its reference to Maps.

- The projector's 131 rule rows then call the calculators the planner calls. Where planner and projector hold the same check, one function serves both; where they differ, each calls its own (D6).
- The pure tables on Units records (`VehicleCheckRolled.For` and the four others) move to Rules and the records forward to them (D10).
- The other way is the one the code already uses for the four record verifiers: Units declares an interface, Play implements it over Rules, and the projector is handed it. I do not recommend it for legality. A verifier may be absent (a caller of `GameProjector.Project` may pass none), and a projector that skips its rule checks when nothing was handed to it would change what a replay accepts.
- **What it costs.** `RestorePackagesWithLockFile` is on and CI restores in locked mode, so the lock files of Units and of every project that references it (Units.Rendering, Units.CounterSheets, Play, MapStudio, the CLI, and their tests) are regenerated and committed in the slice that adds the reference. Units, Units.Rendering, and Units.CounterSheets then carry the Rules assembly and its embedded sources.
- The projector stays the reader of events and the writer of states. It asks Rules "is this step legal on these facts" and "what does this phase's end remove", and applies the answer.

## 8. How "no behavior changed" is proved

**P1. The suite.** Every test project passes with no test file changed (D10). By my count of `[Fact]` and `[Theory]` attributes: Rules 315, Play 599, Units 146, Maps 166, MapStudio 319, Authoring 151, Maps.Vasl 39, Maps.Rendering 22, Units.Rendering 38, Units.CounterSheets 19. After each slice: the solution build, then the test projects the slice touches, one at a time with `--no-build`. `git diff --stat` over `src/ASL/tests` must be empty at every commit.

**P2. The recorded games replay to the same states.** A small tool, built in the scratchpad against the branch and committed nowhere, replays each game through `GameProjector` and writes, for each event, a digest of the state after it (every public value, by reflection, in a fixed order) and every diagnostic with its code and text. It is run on `main` before the first slice and after every slice; the outputs must be equal byte for byte. The games: the two fixtures (`guards-dl-01`, `p31c-tw`), the one saved game under `src/ASL/units/games` (`a1-village.synthetic`; the folder holds one game, not several), and, read only and with your leave (question 8), the 160 live games under `src/ASL/boards/units/live`. Because each fire, Rally, Close Combat, and ordnance record is verified against the state as it replays, this also proves the builders of D5 give the same records.

**P3. The planner answers the same.** The second tool asks the planner. At the last state of each game, and at each phase change of the two fixtures, it sends a fixed set of requests through `PlanAsync` for every unit of the side to act (a move to each adjacent Location, fire at each enemy Location in range, a rout, an advance, the phase's end, and each action that fits the phase) and calls the planner's public reads (`Range`, `Adjacent`, `CoverAt`, `MustRout`, `MayRout`, `KnownEnemyInLos`, and the rest, about fifty). It records the status, the refusal text, the events of a plan that needs no roll, and for a plan that rolls the roll's key and dice count. Before and after must be equal. I have not yet confirmed how the tool fixes the dice (the roller's seeded constructor is internal); it compares up to the first roll until that is settled, and I will say what it reached.

**P4. Texts and keys.** A script lists every string literal that begins `play.`, `asl.`, or `UNIT-STATE-`, every `CASE-` and `MAP-` code, and every roll purpose and key fragment, over Play, Units, Rules, and Maps, as a sorted list with counts. Before and after a slice the lists must be equal: a text may change its file, never its characters. The same holds for package ids and reason codes.

**P5. The gate.** Once for the whole pass: the Studio check (a turn played in each view), the full local suite, the Node viewport test, the chart supplement regenerated with no difference, and the Docker Linux check.

**What the tests do not reach, and the guard for each.**

| Not reached | Guard |
|---|---|
| Refusals in branches no test and no recorded game enters (many of the 1,481 texts) | P3's sweep of requests; D3 (checks moved in their order); a read-only review of each slice that compares the old member's order of checks with the calculator's, block by block. |
| Planner and projector differing on purpose or by fault | D6: two functions; the list of section 12. |
| A fact gathered earlier than before, where the read can fail or is costly | D3: nullable facts, read where they were read; for a search, D4. |
| Speed (the phase's end, the rout search, LOS caches) | The largest saved game's phase end and a rout search timed before and after, at the gate, as the standing rules ask. |
| The Studio's pages, which read the planner's public methods | D10 (the names stay); P5's Studio check. |
| Linux | Docker at the gate. |

## 9. The slices

Each slice builds and passes on its own and is one commit, or a few where the slice is long; `main`'s behavior holds at every commit because each commit moves a member's decision and leaves a forward or a caller in its place.

| Slice | What moves | From | To (Rules files) | Members | Of them searches |
|---|---|---|---|---:|---:|
| S0 Foundation | Nothing. The Units to Rules reference and the lock files; the two proof tools and the text list, run on `main`; a short conventions note in the Rules project's folder. | | | | |
| S1 The pure layer | The 46 pure members of Play and Units: the result tables on Units records, the US#, IPC, squad-equivalents, the angle, VCA, Target Facing, and Covered Arc arithmetic (as ints), the grain seasons (both, named), terrain cost tables. | Play, Units | new `ScenarioA1Definitions.cs`, `ScenarioA1Geometry.cs`, `ScenarioA1ResultTables.cs` | 46 | 0 |
| S11 The state's own definitions | Good Order (the state's), Encircled, the setup period, Inexperience and the MF allotment, what a side may see (`GameView.Of`, as a verdict for each counter that Units applies). | Units | new `ScenarioA1Experience.cs`, `ScenarioA1Visibility.cs`; `ScenarioA1Definitions.cs` grows | 6 | 1 |
| S3 Infantry movement and terrain | A step's cost and refusals, Bypass, Double Time, SMOKE grenades, ADJACENT, hexside TEM and Wall Advantage, the "seen within 16" scans; the projector's movement step, window, and forced back. | `GamePlanner.Movement.cs`, `.Terrain.cs`, `.Smoke.cs`, `.Map.cs`; projector, 10 members | new `ScenarioA1MovementCalculator.cs`, `ScenarioA1MovementModels.cs`, `ScenarioA1TerrainCosts.cs` | 35 | 4 |
| S4 Fire | What `LiveFire.FromState` decides, `FireMapFacts`, follow-ups, Encirclement, Fire Lanes, concealment gain, Acquisition, the roll loop's dice table; the projector's fire, Residual FP, and SW records. | `GamePlanner.Fire.cs`, `.FireExtensions.cs`, `.Acquisition.cs`, `.Consequences.cs`, `LiveFire.cs`; projector, 12 | new `ScenarioA1FireEligibility.cs`, `ScenarioA1FireMapRules.cs`, `ScenarioA1FireFollowUps.cs`; `ScenarioA1FireModels.cs` gains unrecorded fact records only | 50 | 3 |
| S7 Ordnance, Guns, and SW | What `LiveOrdnance.FromState` and `Panzerfaust` decide, the Gun's map facts, effects, CA change, Manhandling, hook-up, DC placement, Deployment, Recovery, Transfer. | `GamePlanner.Ordnance.cs`, `.Guns.cs`, `.DemolitionCharges.cs`, `.SupportWeapons.cs`, `LiveOrdnance.cs`; projector, 8 | new `ScenarioA1OrdnanceEligibility.cs`, `ScenarioA1GunCalculator.cs`, `ScenarioA1SupportWeaponCalculator.cs` | 51 | 1 |
| S8 Vehicles | The vehicle step and its costs, checks, Motion, Overrun and PAATC, Passengers, wrecks and vehicle hindrance, vehicle concealment, Recall and its route. | `GamePlanner.Vehicles.cs`, `.VehicleTerrain.cs`, `.VehicleConcealment.cs`, `.Wrecks.cs`, `.Overrun.cs`, `.Passengers.cs`, `.Recall.cs`; projector, 6 | new `ScenarioA1VehicleMovementCalculator.cs`, `ScenarioA1VehicleModels.cs`, `ScenarioA1VehicleTerrainCosts.cs`, `ScenarioA1OverrunCalculator.cs`, `ScenarioA1PassengerCalculator.cs`, `ScenarioA1RecallCalculator.cs` | 87 | 8 |
| S5 Rout, Rally, and Repair | The rout search and its bars, Interdiction, DM, surrender and Massacre, what `LiveRally` decides, Rally effects, Repair, Shock, the freed SMC. | `GamePlanner.Rout.cs`, `.Rally.cs`, `.FreedSmc.cs`, `.Shock.cs`, `LiveRally.cs`; projector, 9 | new `ScenarioA1RoutCalculator.cs`, `ScenarioA1RoutModels.cs`; `ScenarioA1RallyCalculator.cs` and `ScenarioA1RallyModels.cs` grow | 53 | 12 |
| S6 Close Combat, the advance, and prisoners | The advance, Ambush and round order, obligations, withdrawal, the berserk charge, capture and Guards, Mopping Up, a vehicle's CC sequence. | `GamePlanner.CloseCombat.cs`, `.VehicleCloseCombat.cs`, `.MoppingUp.cs`, `.Prisoners.cs`, `LiveCloseCombat.cs`; projector, 14 | new `ScenarioA1AdvanceCalculator.cs`, `ScenarioA1ChargeCalculator.cs`, `ScenarioA1PrisonerCalculator.cs`, `ScenarioA1MoppingUp.cs`; `ScenarioA1CloseCombatCalculator.cs` and `ScenarioA1VehicleCloseCombat.cs` grow | 71 | 16 |
| S9 Night, weather, Starshells, and Snipers | NVR and Illumination, weather costs, the low-visibility DRM, Wind Change, Starshells, the Sniper's attack and results. | `GamePlanner.Night.cs`, `.Starshells.cs`, `.Snipers.cs`; projector, 3 | new `ScenarioA1NightAndWeather.cs`, `ScenarioA1Starshells.cs`, `ScenarioA1Sniper.cs` | 21 | 1 |
| S2 Sequence of play and the A1 entry | The phase's end and what it removes and requires, setup steps, Balance, Bore Sighting, the literal facts and wording around the A1 entry packages, choices, lineage, and creation in play. | `GamePlanner.cs`, `.Proposer.cs`, `.Choices.cs`; projector, 18 | new `ScenarioA1SequenceCalculator.cs`, `ScenarioA1SequenceModels.cs`; the entry packages' providers grow | 44 | 6 |
| S10 Setup, entry, Victory, and concealment at setup | Setup legality and fill, entry from off board, card rules (Battlefield Integrity, the playable area), exit, Control, CVP, the conditions of Victory, non-OB concealment. | `ScenarioSetup.cs`, `ScenarioCards.cs`, `SetupPlanMatch.cs`, `ScenarioVictory.cs`, `GamePlanner.CardSetup.cs`, `.Victory.cs`, `.Disclosure.cs`; projector, 1 | new `ScenarioA1SetupCalculator.cs`, `ScenarioA1EntryCalculator.cs`, `ScenarioA1VictoryCalculator.cs`, `ScenarioA1Concealment.cs` | 57 | 8 |

**The order.** S0 first: the proof must exist before anything moves. S1 and S11 next: every later slice calls them. Then by what the rule passes need (section 10), with S3 before S5, S6, and S8, which call the Infantry step. The existing calculators `ScenarioA1FireCalculator.cs` (3,444 lines), `ScenarioA1OrdnanceCalculator.cs`, and `ScenarioA1ArmorCalculator.cs` do not grow; what joins them goes in files beside them.

**Tests.** None moves and none is edited (D10). The move adds no test of its own: a calculator's tests are the ones that reach it through the planner today. Tests written straight against the new calculators belong to the rule passes that change them.

## 10. The estimate

**The basis.** The plan's task sizes (tiny 0:05, small 0:20, medium 0:40, large 1:00), applied by script to each of the 521 members: a pure member is tiny; a member of under 60 rule-bearing lines small, of 60 to 200 medium, of more large, and of more than 400 two large; a member with a search one size more. S0 is my own figure. Overhead is 1:15 a pass.

| Slice | Members | Rule-bearing lines | Build |
|---|---:|---:|---:|
| S0 Foundation | | | 2:00 |
| S1 The pure layer | 46 | 393 | 3:50 |
| S11 The state's own definitions | 6 | 335 | 3:40 |
| S3 Infantry movement and terrain | 35 | 1,969 | 17:40 |
| S4 Fire | 50 | 2,368 | 22:20 |
| S7 Ordnance, Guns, and SW | 51 | 2,336 | 22:40 |
| S8 Vehicles | 87 | 2,854 | 37:00 |
| S5 Rout, Rally, and Repair | 53 | 1,597 | 25:20 |
| S6 Close Combat, the advance, and prisoners | 71 | 2,562 | 33:40 |
| S9 Night, weather, Starshells, and Snipers | 21 | 754 | 8:40 |
| S2 Sequence of play and the A1 entry | 44 | 2,543 | 22:00 |
| S10 Setup, entry, Victory, and concealment | 57 | 1,703 | 24:20 |
| **All** | **521** | **19,414** | **223:10** |

**Plainly: this is not one pass, and not two.** A pass of this plan runs 4 to 9 hours of build. At about 12 hours of build a pass the move is about 19 passes: 223:10 of build and 23:45 of overhead, **246:55 on the plan's basis, about 123 hours likely** at the week review's half. Section 22.1 in full is 195:45.

**How far to trust it.** The plan's sizes were set for building a rule, with its reading and its tests. A move reads no rulebook and writes no test, so it may run faster than half; it also carries a proof run after every slice. There is no measured pace for a move in this project. The floor, if every member were a tiny task, is 43 hours. The first slices will measure it, and I will give the measured pace with the first pass's review.

**Three ways to schedule it** (question 2).

| | A. All of it before pass 45 | B. The foundation, then each subsystem ahead of the block that first changes it | C. The foundation, then on touch |
|---|---|---|---|
| What happens | Pass 32.a is S0, S1, S11, and one worked action. Then about 18 migration passes, S3 to S10, before any rule pass. | Pass 32.a the same. Then the slices a block of section 22.1 needs, as migration passes of their own, just before that block. | Pass 32.a the same. A rule pass moves each member it is about to change, as its first commit; a sweep pass after pass 64 moves what is left. |
| First new rule after | about 123 likely hours | about 28 likely hours (31e, S3, S4) | about 6 likely hours (31e) |
| For | It is the decision as made. The rule passes start from a clean base. | The same end state and the same total. Repairs and new rules do not wait a month. A member that pass 45 or a later pass rewrites is moved just before it is rewritten, with the proof still fresh. The pace is measured before most of the cost is committed. | The least delay. |
| Against | The longest wait before a rule is added or a wrong result repaired. Twenty passes of one kind of work with nothing a player sees. | Until the last slice, some rule logic is still in Play. Each block's start waits on its slices. | Rule logic stays in Play for the life of the plan; the "move" commits and the "change" commits of one pass must be kept apart by discipline alone; what no pass touches waits for the sweep. |

**I recommend B.** In all three, decision 7 holds from the first day: no pass writes new rule logic outside Rules. Under B a first cut of what each block needs:

| Block of section 22.1 (f) | Passes | Slices to move first | Build of those slices |
|---|---|---|---:|
| 1. Repairs and the ground under everything | 45, 46, 47 | S3, S4 (pass 45's rows elsewhere are moved on touch) | 40:00 |
| 2. Armor | 33, 34, 50, 51 | S7, S8 | 59:40 |
| 3. The rest of Chapter A | 48, 49, 63 | S5, S6, S2 | 81:00 |
| 4. SMOKE, night, and weather | 56, 34b, 57 | S9 | 8:40 |
| 5 to 10 | the rest | S10, where a pass needs setup, entry, or Victory | 24:20 |

Each block's own design confirms its slices.

**Pass 32.a under any of the three:** S0, S1, S11, and one worked action taken whole through planner and projector as the pattern for the rest: the SMOKE grenade attempt (`PlanSmoke`, with the projector's check in `StepMovement`), 0:40. Build 10:10, overhead 1:15, **total 11:25, likely about 5:45**.

## 11. The pass's number

The last session suggested 31e. I confirm it for the foundation pass. The migration passes after it would run 31f, 31g, and on, whichever of A, B, or C is chosen; under B and C they sit between the rule passes in time, and the plan's section 4 gets a dated table saying so. Question 1.

## 12. Faults seen, for pass 45

None is fixed in this pass. These are the ones that bear on the move, because they are places where two copies of a rule differ and D6 keeps both; the readers' full lists (about 170 lines, by file and line) are in my session notes and go into the pass's review at stage 2.

| Where | What differs |
|---|---|
| `GameState.GoodOrder` (`GameState.cs` 457 to 470); `GamePlanner.GoodOrder` (`GamePlanner.SupportWeapons.cs` 16 to 18); `ScenarioA1CloseCombatCalculator.cs` 153 | Three definitions of Good Order: the first tests Melee and berserk but not Disrupted or TI; the second Disrupted, Melee, and TI but not berserk; the third neither Melee nor Disrupted. |
| `GamePlanner.Map.cs` 160 to 164, 180 to 185, 204 to 208 | Three scans for "a Good Order enemy with a LOS", each with its own filter; two test only "not broken". |
| `GamePlanner.Movement.cs` 85, `GamePlanner.VehicleTerrain.cs` 160 to 168, `GamePlanner.Recall.cs` 98 to 106, against twelve other places | Grain in season April to September for a cost, June to September elsewhere, under one citation (B15.6). |
| `ScenarioSetup.cs` 380 to 384; `GamePlanner.CardSetup.cs` 272 to 273; `GamePlanner.CloseCombat.cs` 306 to 313 and `ScenarioA1CloseCombatCalculator.cs` 292 to 301; `LiveOrdnance.cs` 523 to 529 | Overstacking counted four ways: SMC as tenths above four, a fifth SMC refused, SMC by fives, and no SMC term. |
| `GamePlanner.Vehicles.cs` 340 to 349 against `GameProjector.cs` 2050 to 2055 | The planner bars a TI vehicle from moving; the projector does not. |
| `GamePlanner.Vehicles.cs` 382 to 387 against `GameProjector.cs` 2078 to 2081 | What a bogged vehicle may do differs. |
| `GamePlanner.Rout.cs` 332 against `GameProjector.cs` 2380 to 2381 | The rout MF of a wounded SMC: any SMC in the planner, a leader or hero in the projector. |
| `GamePlanner.Rally.cs` 472 against `GameProjector.cs` 1777 to 1781; `GamePlanner.Rally.cs` 393 and 403 against `GameProjector.cs` 1804 to 1835 | The CE test of a vehicle MG repair differs; the planner asks more of a SW repair than the projector checks. |
| `LiveFire.cs` 333 to 345 against `Experience.cs` 22 to 64 | Inexperience decided twice, with different leader tests and results. |
| `GamePlanner.Fire.cs` 1133 to 1166 against `ScenarioA1FireCalculator.cs` 1664 and 1823 to 1826 | Cowering in "could cause a NMC" has fewer exemptions than the calculator's, so DM can disagree with the attack. |
| `GamePlanner.Fire.cs` 1604 to 1618 against `LosCalculator.cs` 2248 to 2306, and `GamePlanner.Rout.cs` 93 and 118 | Hindrances are valued twice: Fire recounts by terrain and season; the rout uses VASL's total. |
| `GamePlanner.CloseCombat.cs` 1105 to 1138, 1215 to 1233; `GamePlanner.MoppingUp.cs` 156 to 172 | The effects of capture in three copies that clear different conditions. |
| `GamePlanner.CloseCombat.cs` 1284 to 1288; `GamePlanner.Prisoners.cs` 113 to 114; `GamePlanner.MoppingUp.cs` 111 to 113; `ScenarioA1CloseCombatCalculator.cs` 1137 to 1139; `GameProjector.cs` 3785 to 3788 | Who may guard prisoners, in five tests that differ. |
| `LiveFire.cs` 461 and 475; `LiveCloseCombat.cs` 224 to 225 | "Stunned" groups Recalled, Shocked, and Unconfirmed Kill three ways. |
| `GamePlanner.Recall.cs` 113 to 115 against `GamePlanner.VehicleTerrain.cs` 188 | The road rate's copy in Recall lacks the snow clause; the Recall exit cost adds no night or weather MP. |
| `GameProjector.cs` 3490 to 3492 | A failed creation in play is run twice, so its diagnostic is written twice. The move keeps it. |

## 13. Questions for the user (answered: section 15)

Each with my recommendation.

1. **The number.** Is the foundation pass 32.a, with the migration passes after it 31f and on? *Recommended: yes.*
2. **The schedule.** The move is about 247 hours on the plan's basis, about 123 likely, not one pass. A (all of it before pass 45), B (the foundation, then each subsystem ahead of the block that first changes it), or C (on touch, with a sweep at the end)? *Recommended: B.* It keeps decision 7 and changes only "one migration pass before pass 45".
3. **A fact reader for a search** (D4). May a calculator whose decision is a search take an interface, declared in Rules and implemented in Play, that returns facts only (neighbors, a step's facts, who sees a Location)? Without it the rout, the charge, and the Recall route cannot move and keep their results. *Recommended: yes, for searches and scans only.*
4. **The projector** (D7). Does Units gain a project reference to Rules? *Recommended: yes.*
5. **LOS** (D8). You said LOS and range stay in Play. The calculation is in Maps and Play calls it. Do I read you rightly that it stays in Maps, and that Play hands Rules the result as a fact? *Recommended: yes.*
6. **Board reading** (D9). Do the Locations of a hex, the joining of boards, and the SSR terrain changes stay in Maps as readings of the board? *Recommended: yes.* They reproduce VASL and cite no rule.
7. **Left where they are** (D11, D12). Card records stay in Play as the card's format; the refusal sentences and action descriptions stay as prose; the counter plausibility warnings of the Units Lab stay in Units. *Recommended: yes to all three.*
8. **The live games.** May the proof replay the 160 games under `src/ASL/boards/units/live`, read only, writing its output to the scratchpad? *Recommended: yes.* Without them the replay proof rests on three games.
9. **Old names as forwards** (D10). Public members that tests and the Studio call stay as one-line forwards to Rules, so that no test file is edited. A later pass may remove a forward when it changes the rule. *Recommended: yes.*
10. **The `ScenarioA1` prefix** stays on new Rules files unless you say otherwise. *Recommended: it stays for now;* a rename of the whole project's files is a small pass of its own, better made once the move is done.
11. **Section 22.1 and the plan.** At stage 2 I amend section 22.1 to list the migration passes and to say its passes write their rules in Rules, and I change decision 7's "one migration pass" to whatever you answer to question 2. *Recommended: yes.*

## 14. For the build session

- The branch: `feature/asl-backlog-pass-32a`, and so for each sub-pass.
- Before S0: one small edit to confirm that writes pass; the clock read.
- S0 runs the two proof tools and the text list on `main` first and keeps their outputs in the scratchpad as the "before".
- After each slice: the solution build with `--warnaserror`, the touched test projects one at a time with `--no-build`, the two tools and the text list compared with "before", `git diff --stat` over the tests empty, then the commit by explicit paths.
- The one departure from the standing rules holds: here the tests are the proof, not the Studio; the Studio check comes once, at the gate.
- A move that cannot be made without changing a result stops the slice.
- The documents at the end: the inventory's Where column for every row moved, the coverage document, the plan (decision 7, section 4, section 22.1), the backlog, the time log, the review with the readers' fault lists.

## 15. The user's answers (2026-10-05)

Given in the chat to the six questions I put there, by their numbers in that message; the design's question numbers are in brackets.

| | The question | The answer | What follows |
|---|---|---|---|
| 1 [1] | Is the foundation pass 31e? | "NO. The next pass is 32. You will need to move down the numbers again." | The migration is **pass 32**. Every planned pass from 32 moved up by one (the plan's section 4 has the dated table): armored combat 33 and 34, night 34b, DYO 35 and 36, the other planned packages 37 to 44, the twenty new ones 45 to 64. This design's file and its later pass numbers were changed to match. |
| 2 [2] | The schedule: A, B, or C? | "I want all rules logic to be in the rule library. We can carve up that work to do that with 32.a, 32.b and so on." | **A:** all of the move before the rule passes, as sub-passes of pass 32. The slices of section 9 become 32.a (S0, S1, S11, and the worked action), 32.b (S3), 32.c (S4), 32.d (S7), 32.e (S8), 32.f (S5), 32.g (S6), 32.h (S9), 32.i (S2), 32.j (S10); the plan's section 22.2 has the table. Each sub-pass has its own branch, proof, gate, and merge. |
| 3 [3] | A fact reader for a search? | "Yes" | D4 stands. |
| 4 [4] | Does Units reference Rules? | "Yes." | D7 stands. |
| 5 [5] | LOS stays in Maps, read by Play, handed to Rules as a fact? | "It stays were it is, and so does the Range projector." | D8 stands: LOS and range are not moved. |
| 6 [8] | May the proof replay the live games? | "There seems to be a versioning snafu with 'older' games not able to be read at runtime. Fix that." | Fixed before the pass begins, on its own branch (`fix/asl-older-games-replay`), by dropping the catalog version lock at the user's later word: see below. The proof then replays the live games. |

**The questions not answered one by one** (the design's 6, 7, 9, 10, and 11: board reading stays in Maps; card records, the refusal sentences, and the plausibility warnings stay where they are; old public names stay as forwards; the `ScenarioA1` prefix stays; section 22.1 and decision 7 amended). I put them to the user as recommendations in one line and the user's answers did not change them. I go on with each as recommended and will say so again at the first sub-pass's stop; any of them can still be turned.

**The older games.** 60 of the 160 live games name a catalog version from 1.1.0 to 1.10.0. The Studio carried only 1.13.0 and an archived 1.12.0, and looked a game's catalog up by its exact version, so those games failed at their first event ("The catalog ... is not loaded"); a game whose version was found still failed at its first fire, Rally, Close Combat, or ordnance record, because the packages read 1.13.0 alone.

I first restored every earlier version from Git history and let an older game's records stand unchecked. The user then asked "Do we need Catalog versions? The seem in the way", and I measured: across the 13 published versions no definition was ever removed, and only versions 1.4.0 to 1.6.0 differ from today's catalog at all (2 to 7 definitions, corrected by 1.7.0); with the 60 older games relabelled as the current version and every record checked, 157 of the 160 replay. The user's ruling (2026-10-05): **"B. Drop the lock, and delete those 3 games that don't work."**

What was built, on branch `fix/asl-older-games-replay`:

1. **The version locks nothing.** `UnitCatalogs.For` gives the loaded catalog of the name a game or a card records, whatever version the record names. The projector, the planner's game start and Victory read, the Play and Replay pages, and a card's validation all use it. The record keeps the version it was made under, as a label; the state reads the catalog that is loaded.
2. **Every record is checked in every game.** The record verifiers are as they were.
3. **The archive is gone:** `src/ASL/units/catalog/archive`, its embedding in the Units project, and `UnitCatalogs.ReplayNames`.
4. **A catalog whose name is not loaded** still refuses the game (UNIT-STATE-008), and the page says which.
5. **The three games that do not replay were deleted at the user's word** from `src/ASL/boards/units/live`: `pass1-demo` (a unit moves in the MPh after firing in the PFPh, which the projector refuses under A3.3 as the code has it), `pass1-demo-2` (a fire record today's Fire package does not resolve), and `steps-demo` (a Rally record whose result differs from what today's calculator gives). The other 157 replay.

What this rests on, for later passes: a catalog's definitions are added to and corrected, never removed or renamed. A pass that corrects a printed value may make an older game's record fail to reproduce; the replay then says so, as it did for these three.

**The size under answer 2.** 223:10 of build in ten sub-passes, 12:30 of overhead: 235:40 on the plan's basis, about 118 hours likely. The first sub-pass measures the pace.

## 16. Pass 32.a as built (2026-10-05 to 2026-10-06)

Branch `feature/asl-backlog-pass-32a`, on main at 82d4381. The user's go-ahead came at 22:10 on 2026-10-05 after the preparation's stop, with the proposal below accepted.

**What the preparation found.** The Appendix marks 55 rows WHOLE in Play and Units (44 and 11), not 46; the figure of 46 could not be reconstructed from the documents. The members section 9 names for S1 as "US#, IPC, squad-equivalents, Target Facing, and the Covered Arc wedge" are not WHOLE rows but parts of SPLIT members (`UnitSize`, `FireBar`, `Laden`, `MfAllotment`, `OverstackExcess`, `SquadEquivalents`, `OrdnanceMapFacts`, `SupportWeaponMapFacts`, `BypassTargetFacing`) and stay for S3, S4, S6, S7, and S8. `AngleBetween` is in no row and was moved with the geometry. Five WHOLE rows were left in place: `UnitPlausibility.ArmorFactors` (D12), `HexsideTemAt#4` and `PlanExit#7` (blocks of S3 and S10 members), `MoverFacts` and `MoveOptions` (the A1 entry cases, S2). So S1 moved 50 members and `AngleBetween`.

**The commits.**

| Commit | Slice | What |
|---|---|---|
| c967a19 | S0 | Units references Rules; the eleven lock files that carry Units (Units, Play, Units.Rendering, Units.CounterSheets, MapStudio, and the tests of Units, Play, Rules, MapStudio, Units.Rendering, Units.CounterSheets; the CLI and Authoring carry no Units entry, against section 7.2); `CONVENTIONS.md` in the Rules folder |
| 767261a | S1 | 50 members and `AngleBetween` into `ScenarioA1Definitions.cs`, `ScenarioA1Geometry.cs`, `ScenarioA1ResultTables.cs`, with one-line forwards; facings and hexsides cross as ints; the two identical Concealment Terrain copies forward to one function (D6's allowance); the card-typed members take ints and bools |
| 8101141 | S11 | Good Order, Encircled, and the setup events in `ScenarioA1Definitions.cs`; `ScenarioA1Experience.cs`; `ScenarioA1Visibility.cs`, where `GameView.Of` hands over each counter's container and holder chain as facts, so no fact reader was needed; `RuleState` carries a condition's five states |
| (next) | fix | The Play forward of the vehicle result conditions inserted its dictionary keys in a changed order, which the record writer would have serialized in that order; found by the read-only review, the verdict is now an ordered list |
| (next) | action | `ScenarioA1SmokeCalculator`: `Plan` with the planner's seven checks in their order, `Verify` with the projector's one combined check (two functions, D6) |

**The proofs, as run.** P1: no test file changed (the lock files under `tests/` changed with S0). P2: the replay digest over the 157 live games, the synthetic game, the two fixtures, and 48 cuts of `p31c-tw`, one digest a state; equal after every commit (the baseline's only diagnostics difference was the tool's own map configuration, fixed after the first run). P3: at the user's word the sweep runs over one game, `p31c-tw` (Tractor Works, 716 events), at its last state, at every fourth phase change (32 cuts), and at every entry into the MPh (16 cuts), with one request of each kind per unit and about thirty public reads per unit, 3:30 a run; its baseline is taken from a build of main's own sources in the scratchpad. P4: the text list, with interpolation holes normalized, since a hole's expression is not a character a player sees; one literal (the SMOKE proposal's sentence) holds a nested hole with quotes that the script cuts short, and that literal was compared by eye. A read-only review compared all 59 moved members' old and new bodies and found the one difference above.

**Measured pace.** S0 2:00 estimated; S1 3:50; S11 3:40; the action 0:40: 10:10 of build. The time log has the hours; most of the session went to the proof tools, not the moves.
