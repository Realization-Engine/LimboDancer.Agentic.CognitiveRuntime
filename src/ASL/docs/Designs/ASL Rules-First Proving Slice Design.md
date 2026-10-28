# ASL Rules-First Proving Slice Design

**Status:** Design and build of first task 4 of the [Rules-first four-track plan](<../Plans/ASL Rules-First Four-Track Plan.md>) (section 8), 2026-10-28, at the user's word ("execute task 4, now"; "do not stop to ask me unnecessary questions"). The rulings below were decided under that word and are open to the user's veto. Claim: `proving-slice` (the inventory's section 6), owning A10.6, E10.1, and A12.14.

## 1. What the slice proves

Three rules, end to end, before the claim board opens (the plan's section 8, question 4):

| Contract | Calculator (only read) | What it proves |
|---|---|---|
| `a10.6-rally` | `ScenarioA1RallyCalculator.Precheck` and `Resolve` | A contract can describe code that is built and played; a Play fact source gathers every fact |
| `e10.1-eligibility` | `TurnAmmoEligibilityRules.EvaluateRecipients` | Pending facts, entered by hand, through the JSON entry point; a calculator with no wiring |
| `a12.14-advance-concealment` | `ScenarioA1AdvanceCalculator.LosesConcealment` | Per-viewer disclosure: a fact a viewer may not know reaches the calculator as unknown |

## 2. Rulings

- **RPS.1 The contract record lives in Rules, in two new files.** `ScenarioA1RuleContractModels.cs` holds the records (`RuleContract`, `ContractRule`, `ContractFact`, `ContractEvent`, `ContractPlace`, `ContractVerdict`, `ContractReason`, `ContractRoll`) and the enums (`ContractFactType`: Flag, Number, Text, Choice, Hex, Unit; `ContractDisclosure`: All, Owner, Adjudicator). `ScenarioA1RuleContracts.cs` holds the registry (`All`, `Find`) and the three contracts. No calculator changes (CONVENTIONS.md, points 5, 6, 9). The record names nothing outside Rules.
- **RPS.2 The JSON entry point** is `ScenarioA1RuleContracts.Evaluate(string contractId, string factsJson)`: the facts are one JSON object, name to value, null for unknown; the result is the verdict as JSON. A missing fact that may not be unknown, or a calculator that throws on its input, gives the outcome "not-evaluated" with the fact named, never an exception.
- **RPS.3 A verdict** is an outcome (a word from the contract's list), reasons (each with its rule and physical page), and required rolls (kind, target, text). Nothing is rolled. The rally's target is read from the calculator's own arithmetic on a probe roll that is never drawn or written.
- **RPS.4 Disclosure is declared in Rules and applied outside it.** Each fact says who may know it (the plan's section 5): all, the owner of the unit it concerns, that unit's opponent (what the other side's own units see, which may be hidden from the owner), or the adjudicator. The workbench passes null for a fact the viewer may not know, and shows "not known to this viewer".
- **RPS.5 One Play fact source**, for the rally: `RuleContractFacts.Rally(GameState, unit, leader)` in Play, built on `LiveRally.FromState`, the path the Play page already uses, so the workbench and play read the same facts. The other two contracts' facts are entered by hand (E10.1) or read from the state where a reader exists (A12.14: night and the terrain gathered, the enemy's Good Order LOS within 16 gathered by the planner's existing read). Fact sources for every contract are track 3's.
- **RPS.6 The game record's `Minigame` property** is an init property of `GameRecord`, written as `"minigame"` after `"synthetic"` only when it is set, and read when present; every existing game file is unchanged. The store sets it at a game's first commit from the card its `game-started` names, through the minigame registry; `PlayOn` and every later commit carry it. It is never set by hand.
- **RPS.7 A card's `minigame`** is an optional property of `ScenarioCard` (null for every card today, so no card's text or SHA-256 changes).
- **RPS.8 The bundle's file names carry the id**: `src/ASL/units/minigames/<id>/<id>.minigame.json`, `<id>.scenario-card.json`, `<id>.setups.json`. The plan's `minigame.json` would collide as an embedded resource name; the id in the name keeps every resource unique. The cards are embedded beside the built-in ones (prefix `Scenarios.`), the manifests as `Minigames.<id>.minigame.json`.
- **RPS.9 The minigame registry** is `Minigames` in Play: the bundles read from their manifests, which card each owns, and which bundles name a contract or a rule. The manifest is the one source of the link (the plan's section 7.1).
- **RPS.10 The lists leave minigames out by default.** The Play page's game and card pickers, Replay, the Game inspector, and the Scenarios page take a "Show minigames" switch. A game's minigame is read from its file's header only, cached by the file's time and length (the memory and performance guidelines: no whole-record read per game per list).
- **RPS.11 The Rule Workbench** is a MapStudio page at `games/workbench` (in the Play group of the navigation), with links from Play and Replay that carry the game, revision, and viewer. Header: game (minigames first), revision, viewer, contract (the game's minigame contracts, or all). Map: the existing `BoardWorkspace`, read-only; a Hex or Unit fact is picked by clicking. Facts: one row each, with its source (Gathered, Pending, Hidden). Verdict and events: shown, marked "preview, not written". It writes nothing to a game.
- **RPS.12 The checks of this slice's bundles are hand-entered facts only.** A check evaluates its contract on its manifest's facts and compares the verdict. Checks on gathered facts at a position need a game, which track 3 adds to each bundle's `games/`; backlog row.
- **RPS.13 The registry test is in Rules.Tests** (every contract: unique id, rules that are rows of the inventory, every fact type round-trips through the JSON entry point, every outcome reachable or listed); the **shared minigame test class** is in Play.Tests (every manifest: its rules are inventory rows, its contracts exist, its card loads and validates against the catalog, its board is the card's, and each check's verdict matches).
- **RPS.14 No gate run.** The slice changes no rule decision and no record a game already has; CI's full suite and the narrow tests of the changed reader and writer stand in. Open to veto.

## 3. The three bundles

| Bundle | Board | Position | Checks |
|---|---|---|---|
| `a10-rally` | bd04 (the armor-test board) | A broken German squad with a leader in a building, in the RPh | may attempt with the leader's DRM; refused when the unit is not broken |
| `e10-eligibility` | bd04 | An ammo vehicle beside a Gun's crew | eligible; refused when not in the same or an Accessible Location |
| `a12-advance-concealment` | bd04 | A concealed squad advancing into Open Ground | loses "?" in view of Good Order enemy within 16 by day; keeps it at night; not known to the other side's viewer |

## 4. Memory and performance

No work per state, unit, hex, or step during play: the contracts are evaluated only on the workbench and in tests. The header cache of RPS.10 is one entry per game file, invalidated by the file's time and length. The workbench reads one snapshot at one revision. No test builds an expensive input more than once per class.

## 5. Build outputs outside the repository

Claude's Studio in `asl-gate-work/studio-6670` (registered already). No other.

## 6. As built (2026-10-28, branch `task4/proving-slice`)

**What the design placed and the build left out or changed, first:**

- **RPS.4 gained a fourth audience.** The Studio check showed that whether a Good Order enemy sees a Location is that enemy side's own knowledge, not only the adjudicator's: the plan's section 5 names owner, opponent, and adjudicator, and the first build had no "opponent". `ContractDisclosure.Opponent` was added, and the concealment contract's LOS fact is the opponent's; the advancer's identity may be unknown, since the calculator does not need what is under the "?". From the German view the verdict is not evaluated (a hidden Russian may see); from the Russian view and the adjudicator's it is "loses-concealment".
- **RPS.8: no setups file.** The three cards are minimal cards (ruling R22.3): units are set up by hand, and a setup plan needs an order of battle, so the bundles hold a card and a manifest only.
- **RPS.10: the scenario card list.** `ScenarioCards.Names` lists the scenario cards and leaves the minigames' out; `MinigameNames` lists those and `IsBuiltIn` answers for both, so the card library reads a minigame's card as built-in and no user card takes its name. Four tests that walk the built-in cards (passes 17, 21, 22, 30) hold unchanged.
- **RPS.11: the viewer is locked for a game still being played** that is not a minigame's: the workbench reads it only from the view its link carried (ruling R31b.1 kept), so it shows no side what Play would hide. Replay's link to it sits behind Replay's own guard of that ruling.
- **The rally contract** takes a `captors` fact its Precheck needs when a leader directs (A15, A20.4), and the concealment contract the Location advanced into (`to`). The rally's companions, No Quarter, a Commissar, and Extreme Winter are not facts of the contract (backlog section 78).
- **The Minigames column** is written by `code-builders/documents/minigames_column.py` from the manifests, and its `--check` runs in CI and the gate's strict stage beside the status-line and claim checks.

**Built:** the contract format and three contracts (Rules); `GamePlanner.ContractFacts` (Play); the `Minigame` property of game records and cards, its header read cached by file time and length, and its first-commit derivation from the card; the minigame registry and three bundles; the Rule Workbench page with links from Play and Replay; the "Show minigames" switch on Play, Replay, the Game inspector, and the Scenarios page; `ScenarioA1RuleContractTests` (5) and `MinigameBundleTests` (12, nine of them the bundles' checks).

**The Studio check** (Claude's Studio on 6670, games `ps-rally`, `ps-advance`, `ps-supply` written by `code-builders/pass-history/task4-proving-slice/games_workbench.py`, and `ps-start-rally` started from the a10-rally card): every rally fact gathered, the verdict "may attempt" with a Final DR of 7 or less (Morale Level 7; DRM -1 for the 8-1 leader, A10.7, and -1 for the woods, A10.61), and "refused" with no leader picked (Self-Rally barred with a leader present, A10.6); the concealment verdict from each of three viewers as above; E10 eligible from the adjudicator with the facts entered by hand, and not evaluated from the Russian view, which may not know the German supply vehicle's ammunition; the game started from the a10-rally card wrote `"minigame": "a10-rally"` at its first commit, and a normal game's file has none; the four lists hide the minigames until shown; 1600 and 320 pixels with no sideways scroll (the map wrapped as the board viewer's is). Not exercised: the keyboard (the clicks were dispatched by script), and the Game inspector's switch on an ended minigame game (none has ended).

**Tests run** (by method): Rules 5, Play 16 (the minigame class and the four card tests the change reaches), MapStudio 14 (the new-game card list and the navigation). The Rules and Play test projects build with warnings as errors. Full suites wait for CI (RPS.14).
