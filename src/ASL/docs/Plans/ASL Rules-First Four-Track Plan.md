# ASL Rules-First Four-Track Plan

**Status:** First tasks started, 2026-10-28. The user's word of 2026-10-28 starts the first tasks of section 15, beginning with Step 0; the rest of the plan stays a proposal, and questions 1 to 7 and 10 of section 14 are open. Nothing in it is built yet. It records the direction the user gave in the discussion of 2026-10-28 and turns it into tracks, tasks, and open questions. Until it is approved, the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>) and the [Turn Guide later Rules plan](<../Turn.Guide/ASL Turn Guide Later Rules Implementation Plan.md>) stand as written, except that pass 73 does not start without the user's word.

**Kept by:** Claude. Codex sessions read it through `AGENTS.md` once it is approved.

## 1. Why a new plan

Two tracks have been building ASL rules at the same time with no shared record of who owns which rule:

- **The Passes track** (Claude) builds rules by section of the rulebook, in the numbered order of the redesign plan, one pass at a time.
- **The Turns track** (Codex) builds rules as the ASL Sequence of Play reaches them: what each phase and step touches. Its nine packages (D1 to D9) implement rules that the redesign plan gives to passes 73, 74, 85, 95, 105, 150, 165, 170, and 175.

Three findings came out of comparing them:

1. **Rules do not need to be built in sequence.** A calculator takes facts and returns a verdict. Its unit tests can supply any facts, whether or not the code that will produce them exists. Order matters only when a rule is wired into Play, because Play can only gather facts that the game state and the map hold.
2. **The redesign plan's order is mostly a grouping, not a dependency.** Of the 25 passes left, 11 (73, 74, 75, 80, 85, 90, 100, 110, 115, 125, 150) have every pass they name under "Needs first" already merged. Many other edges hold for a few rows of a pass only (pass 175 needs pass 125 "for D16", not for E10). The hard chains are few: wind and EC (85) before Drift and weather (95) and Fire (185); terrain 125 to 145 in order; 100 before 105; 115 before 120; 150 before 155.
3. **Rules is less than half the work.** In passes 65, 70, 71, and 72, lines added split as Rules 39% (calculators and tests), Play 43% (gathering facts, commands, events), Units 5%, and MapStudio 13%. Finishing Rules leaves the Play wiring as the largest share, not the UI.

The rule inventory has about 1,330 rows today that are not built, partly built, or refused (899, 355, and 74 by its rows), out of 2,127.

## 2. The direction (the user, 2026-10-28)

- Four tracks, accepted "generally, but let's keep working it": Units, Rules, Play, MapStudio.
- Rules are built by a first-come, first-served task list that agents claim from until the library is complete.
- Wiring contracts come first, so the mechanism can be tested in the Studio before most calculators depend on its format.
- Each rule is verified in the Studio on a **minigame**: an existing board appropriate to the rule (not a custom board) and the bare-minimum counters the rule needs.
- Each minigame is used twice: as a position checked on the workbench while its rule is written (track 2), and as the game played to accept the rule once it is wired (track 3).

## 3. The four tracks

| Track | Owner | Scope | Ends when |
|---|---|---|---|
| 1. Units catalog | Codex | Counter definitions, catalog values (manufactured under R0.3 where no source prints them), vocabulary, documents, plausibility checks, counter art; the VASL boards and overlays the minigames need (section 7.3) | Every counter a claimed rule needs is in the catalog, and every terrain a claimed rule needs is on an imported board or recorded as missing |
| 2. Rules | Claude and Codex, from one claim board | Every inventory row: a calculator, its tests, its wiring contract, its minigame position | Every row is built, built with a deviation, or left out by a ruling |
| 3. Play | Claude and Codex, by capability | Play's fact gathering, commands, and events; Units `State/` (events, writer and reader, projector); ASL.Turns; Maps facts; the game logic now in MapStudio `Services/` | Every phase passes its acceptance (section 9) |
| 4. MapStudio | By page | The real controls that replace the workbench's generic form, rule by rule | Every rule wired in track 3 has its controls |

Tracks 1 and 2 run at once. Track 3 starts by capability as soon as that capability's rules are built (section 9), not after the whole library. Track 4 follows track 3 page by page.

**Units is split.** Its catalog half is static and finite and goes with track 1. Its `State/` half is not: every wired rule adds events and projector cases, and the projector calls the same calculators Play does. `State/` goes with track 3.

## 4. The Rules claim board

### 4.1 The unit of a claim

A claim is a rule section, or a subsection group, whose rules share one calculator. The 105 numbered sections are the starting cut; a large section (A7, B23, C8, D3) is split by subsection. A claim names its rows and the calculator files it will change. Two agents never hold the same calculator file at once, with one exception (2026-10-29): a large file many rules use, which a claim changes only at its edges (a call into its own calculator, a field on a record), is held "shared", and several claims may hold it so at once. Files outside Rules are not held; the claim check warns when two open claims' branches change the same one (section 10).

The board covers the rulebook beyond Chapter E as question 8 rules: the H Notes, the ASOP, and G10 with A to E, and F, G, and W in a second wave once their boards and counters are imported.

**ASOP rows (2026-10-29).** An ASOP row is a step of the sequence, not a rule, and most steps cite many sections (2.22A designates a Spotter, C9.3, and runs the whole of Prep Fire). A claim takes in its claim commit the ASOP rows whose steps cite only its own rules (3.11A cites C9.3 alone); `code-builders/documents/asop_steps.py` lists them from the PDF. A step that cites other sections too is taken by track 3 when it accepts that phase (section 9).

### 4.2 The ledger and the lock

The [rule inventory](<../ASL Rule Inventory A to E.md>) is the one ledger. Each row gains two columns, **Owner** (the claim's id and the agent) and **Claim state** (open, claimed, built, reviewed, wired, accepted).

A claim is made by a commit to main that fills the Owner column for the claim's rows and nothing else. That is an edit to the text of an existing document, so it goes straight to main under the standing rules. The push is the lock: if two agents claim at once, the second push is rejected as not a fast-forward, and that agent fetches, sees the claim, and takes another. The work itself is done on a branch named for the claim and reaches main by pull request and a green CI run, as all code does.

Alternative considered: GitHub issues with an assignee. They are atomic too, but they put the ledger outside the repository, where neither agent's checks can read it.

### 4.3 What "built" means for a claim

1. **The rulebook read and the code mapped.** Every rule of the claim read on its PDF page, cited by rule and physical page, and the whole rulebook searched for its cross-references. Every place the code decides, gathers, or records the rule today listed in the design, from `code-builders/documents/rule_sites.py` and a reading of each site (the Rules project's conventions, section 2, added 2026-10-29 after the C9.3 claim found about twelve places where the plan named two). A claim on a rule already built is its audit by the same steps.
2. **The calculator,** in `LimboDancer.Domains.Asl.Rules` under its conventions (section 2 for a claim), taking facts and returning a verdict. The claim changes no caller outside Rules (the user, 2026-10-29: the Rules library is built rapidly, with no dependency but its unit tests, and with minimal impact on the rest of the code): each site outside Rules that decides the rule is listed for track 3, and an old Rules member keeps its callers and its answer until track 3 moves them.
3. **The wiring contract** (section 5).
4. **The minigame bundle** (section 7.1): its card, setups, and manifest with its checks, all passing on the workbench in Claude's Studio (port 6670) or Codex's equivalent.
5. **Tests,** written after the Studio check under the standing rule that the Studio comes first: the calculator's boundaries, refusals, and hidden-information cases. The bundle's checks run through the shared minigame test class. Only the claim's own test methods and its bundle's checks run.
6. **A review by a different agent,** read-only: the referee's reading of the rules against the PDF, and the facts a side may know. This review replaces the play-time checking a calculator cannot get until track 3.
7. **The rows updated** in the inventory and the coverage document, and anything left out given a backlog row.
8. **The pass's Rules status refreshed** in the redesign plan (section 11.1).

**No gate per rule claim; a batch gate every five** (the user, 2026-10-29, to speed up the Rules library). A rule claim changes the Rules library and its tests and leaves every old member's answer as it was (step 2), so no recorded game replays or sweeps differently, and the gate's replay and sweep, about 25 to 35 minutes a claim, would prove nothing its tests and CI do not. A rule claim therefore merges on its unit tests and a green CI run. `claim_check.py` makes that safe and regular: a rule claim's branch that changes code outside its scope is a fault, and after five rule claims merged since the last batch gate (recorded in the inventory's section 6) the sixth is held until the gate runs once on main against that commit (`gate.sh rules-batch`, with `CODE_MOVE=1`: no game may differ). Its `carry` stage files the baseline under the proved commit, so each batch after the first costs the build and the proofs alone, about 15 minutes. A difference is found after merge, among at most five claims each merged as its own commit, and is fixed forward. Wiring claims keep a gate each, since that is where behavior changes (section 9.1).

**The prompt for Codex at each batch gate** (the user, 2026-10-29). With the batch gate's record, Claude writes Codex's prompt for the wiring the batch opened, by `code-builders/documents/wiring_prompt.py`, from the board: every open wiring row grouped by capability, the rows whose rule claims merged in this batch marked new, the wiring claims in progress, and the rule claims waiting for review, with Claude's note where one is needed. Each prompt is the next numbered file in `src/ASL/docs/Prompts/Codex Wiring/`, so the folder is the history of what Codex was handed; the user approved that these new files go straight to main with the record. The first, Wiring Prompt 001, covers the batch gate `f525ea78`.

### 4.4 What goes on the board first

- The 143 faults the audits found in rules already built, as claims of their own.
- The Turns track's `Turn*` contracts, whose tasks are merged into the pass sections (section 11.2). Each becomes the claim of the rows it decides, reconciled with any calculator a pass already built for the same rule, so that one rule has one calculator (C9.3 has both `ScenarioA1AreaCalculator` and four `TurnSpotter*Rules`; A24.5 has `TurnWhiteSmokeRules`; E10 has `TurnAmmoEligibilityRules`).
- The hard chains of section 1, finding 2, are claimed in order: a claim may not start before the claims it reads verdicts from are built.

## 5. Wiring contracts

A wiring contract is a data record kept in Rules beside its calculator. It refers to nothing outside Rules, so the Rules boundary holds. It names:

| Part | Content |
|---|---|
| Rules | The rule numbers it decides, each with its physical PDF page |
| Facts | The calculator's input record: each fact's name, type (flag, number, hex, unit, choice), whether it may be unknown, and who may see it (owner, opponent, adjudicator) |
| Verdict | The outcomes, reasons, and required rolls it returns |
| Events | The events a command built on it would write: names and contents |
| Place | The phase and ASOP step where it applies, linking it to the Turns checklist catalogs |

The contract also carries one entry point that takes the facts as JSON and returns the verdict as JSON. The workbench uses it to drive any calculator without code of its own, and the registry test uses it to round-trip every fact type.

**Unknown is for hidden information.** A fact may be declared as possibly unknown only where a side may lack it under the rules (a concealed unit's identity, an unobserved load). It is not used to stand in for code that does not exist yet; a test or the workbench supplies such a fact.

**The registry test** runs at the gate and in CI: every calculator has a contract, every rule a contract cites has an inventory row, every fact type round-trips through JSON, and every minigame manifest is sound: each rule it names has an inventory row, each contract it names exists, its board is imported, and its card loads. A contract does not list its minigames; the registry finds them from the manifests (section 7.1).

## 6. The Rule Workbench

### 6.1 What it is

The Rule Workbench is a page of the MapStudio UI. It lets a person (or an agent driving the Studio) ask one rule a question about a real position and see the answer, before the rule has any controls of its own on the Play page. It stands in for every rule's future controls until track 4 builds them.

It does one thing: it evaluates a calculator through its wiring contract on a game at a revision, as seen by one viewer, and shows the result. It does not play the game.

### 6.2 Where it appears

- **In the navigation,** as its own page, "Rule Workbench", beside Play and Replay.
- **From the Play and Replay pages,** a "Workbench" link opens it on the game, revision, and viewer being shown, so a rule can be asked about the position on screen.

### 6.3 The page

| Area | Content |
|---|---|
| Header | Game picker (minigames first, section 7.1), revision (the latest by default, or any earlier one), viewer (each side, or the adjudicator), and contract picker (searchable by rule number or title, filtered to the contracts the game's minigame names unless "All contracts" is chosen) |
| Map | The game's board at that revision, read-only, through the existing board viewport and counter layer. A fact of type hex or unit is filled by clicking the map, as the Play page picks a Location today |
| Facts | One row per fact of the contract: its name, its value, and its source. **Gathered:** read by a Play fact source from the game state and the map. **Pending:** no fact source yet, so an input box of the fact's type (a check box, a number, a choice list, or a hex or unit picked on the map). **Hidden:** the viewer may not know it; the row shows "not known to this viewer" and the calculator receives it as unknown |
| Verdict | The outcome, each reason with its rule and page, and any required roll with its target number. Nothing is rolled |
| Events | The events a command built on this contract would write, with their contents, marked "preview, not written" |

### 6.4 How it works

1. The page asks the contract for its fact list.
2. For each fact, it asks Play for a fact source. A gathered fact is read from one snapshot of the game at the chosen revision through the viewer's perspective. A pending fact takes the value the person entered.
3. The page assembles the facts as JSON and calls the contract's entry point (section 5). The calculator runs exactly as it will in play.
4. The page shows the verdict and the event preview.

**An example.** The E10 eligibility contract (section 8) on its minigame: pick the German viewer and the contract. The supply vehicle and the recipient are picked on the map, so their identities and Locations are gathered. "Recipient is CE" has a fact source, so it is gathered. "Ammunition authorized by SSR" has none yet, so it is pending and gets a check box. Tick it, run, and the verdict reads "eligible", or "refused: the recipient is not in the same or an Accessible Location (E10.2, p. 244)". Switch to the Russian viewer and the German vehicle's ammunition facts read "not known to this viewer".

### 6.5 What it is for

- **Track 2.** The Studio check of every claim (section 4.3, step 4): the calculator on its minigame position, before its tests are written.
- **Track 3.** As fact sources are written, pending rows turn into gathered ones. A contract whose facts are all gathered is ready to wire into a command.
- **Later.** A referee's tool: ask any rule about any position of any game, without changing it.

### 6.6 Fact sources and guardrails

Fact sources in Play follow the pattern of `CaseRead` in Units: one consistent snapshot at an expected revision, with a status (Definitive, Stale, Unverified, and the rest). They reuse the state's map context and `PlayStepMemo`.

**Guardrails.**

- The workbench never writes to a game. It previews events and stops. It is not an adjudication route, and nothing it shows is evidence in a game's history.
- A value typed into a pending fact is labeled as entered by hand wherever it is shown, and is never saved with a game.
- An unwired contract changes no replay and no sweep, so a Rules claim's gate stays light.

The Turn Guide panel can show, for the current phase, which contracts apply there and how many of their facts are gathered, building on the Turns capability inventory.

## 7. Minigames

### 7.1 What a minigame is

A scenario card on an existing, imported board, chosen because it holds the terrain the rule needs, with the fewest counters the rule needs and an SSR where the rule needs one. The card limits play with its playable area (cards already support an enforced band of hexrows, as Tractor Works does).

**The bundle.** Each minigame is one folder that carries the rules it proves and the checks that prove them:

```
src/ASL/units/minigames/a10-rally/
  minigame.json                  the manifest
  a10-rally.scenario-card.json   the card
  a10-rally.setups.json          its setups
  games/                         the played acceptance game, added in track 3
```

| Manifest field | Content |
|---|---|
| `id` | The bundle id, named for its claim: `a10-rally` |
| `rules` | The rule numbers it proves, for example `A10.6`, `A10.61` |
| `contracts` | The wiring contracts it exercises |
| `example` | The rulebook or Training Manual example it reproduces, with its physical page, if any |
| `checks` | The proofs. Each names a position (a setup or a revision), a viewer, a contract, any hand-entered facts, and the expected verdict (outcome and reasons). A printed example's result is its expected verdict |

**The manifest is the one source of the link** between a rule and its minigames. Nothing else keeps its own list:

- A registry built from the manifests answers which minigames prove a contract or a rule.
- The inventory's Minigames column and each pass's Rules status line (section 11.1) are written from the registry by script.
- The card and every game started from it carry the bundle id (below).

The cards are embedded resources today; the `minigames` folder is added to the project file the same way, with the culture setting dotted resource names need.

Where the rulebook prints an example on a named board and hexes, the minigame reproduces it, and the printed result is its expected outcome.

**The Minigame property** (the user, 2026-10-28). A game record gains a property `Minigame`, beside the `Synthetic` property it already has: `GameRecord(Label, Synthetic, Minigame, Events)`. It holds the bundle id, written in the game's JSON as `"minigame": "a10-rally"`; empty means a normal game. A record without it reads as empty, so every existing game keeps its meaning. A minigame card carries the same property, and a game started from the card takes it from the card; it is not set by hand. The id both filters the lists and leads from any game back to its bundle and its rules.

- The normal game lists (the Games page, the Play page's game picker, Replay) leave minigames out. Since the Minigames page design (2026-10-31) there is no "Show minigames" filter: the Minigames page lists them, and no list holds both kinds.
- The Scenarios page leaves minigame cards out the same way.
- The Rule Workbench (section 6) lists minigames first.

### 7.2 Used twice

- **Track 2.** The minigame's setup is a fixed position. Opening the bundle on the workbench lists its checks; "Run checks" evaluates each on its position, with gathered facts where Play can already read them and the check's hand-entered facts where it cannot, and marks it pass or fail against its expected verdict. The same checks run as tests: one data-driven test class goes through every bundle, so a claim writes no test code for its position checks, only its calculator's own unit tests.
- **Track 3.** Once the rule is wired, the same minigame is played through in the Studio and its recorded game goes into the bundle's `games/` folder as the rule's acceptance game. The registry tells the gate which bundles a change's contracts reach, so the gate replays and sweeps those and not every minigame on every change.

Minigames are small, so they are kept as fixtures. The standing rule on proof games (keep the recent full games and the current and previous change's) applies to which of them the gate sweeps, not to whether they are kept.

### 7.3 Boards

- **The terrain data exists already.** VASL's `BoardMetadata.xml` is thin (geometry, building-type overrides, slopes, railroad embankments, partial orchards, color and SSR rules), but each board's LOSData raster holds its terrain. The VASL import decodes it (`LosDataCodec`) and derives a `HexFacts` record for every hex: each Location's level and terrain, depression terrain, hexside terrain, cliffs, slopes, railroad embankments, bridges, and stairways, checked against the VASL fixtures (F1, F2). The Studio already shows these facts in its board and hex views.
- **What is missing is only a search across boards,** and it cannot be live. The last fidelity batch (2026-10-03) holds 297 VASL boards: 235 verified, 61 out of scope, 1 failed. Boards load on demand at about 2.2 seconds each (up to 12), about 11 minutes for all of them, and only the fidelity batch already loads every board.
- **The board finder is a saved index of map hexes by terrain, the `MapHexIndex`** (the user, 2026-10-28), a track 1 task:
  1. **The `MapHexIndex` logic lives in Maps:** a pure function from a board's `HexFactSet` to every terrain type and feature it holds (building levels, gullies, streams, orchards, bridges, cliffs, slopes, and the rest) and the hexes where each occurs. It is tested alone.
  2. **The fidelity batch writes it,** beside its report in the Studio's cache, stamped with the VASL commit and the derivation version. The batch already derives every board, so the index adds little; a new import regenerates it, and a stale index shows by its stamp.
  3. **The board library page gains a "Has terrain" filter** that reads the `MapHexIndex`. Opening a result highlights the matching hexes in the board viewer.
  4. **Agents read the same `MapHexIndex`** through a small script under `code-builders/`, for questions such as "boards with a gully and a two-level building, and their hexes". For a question about neighbouring hexes, the index narrows the list to a few boards, and the agent opens those in the Studio to confirm.
  An agent picks a minigame's board this way, never by eye.
- **Terrain the finder does not find.** The importer's version 1 scope declines some boards: legacy V5 boards without `BoardMetadata.xml`, HASL maps that VASL builds with board-specific code, alternate hex grain, custom geometry, and boards without LOSData. The inventory's notes that "no in-scope board prints" Narrow Streets, Steeples, railroads, Towers, or sewers are checked against the `MapHexIndex` in Step 0: terrain it finds is no longer missing, and terrain found only on one of the 61 out-of-scope boards is reported as such. Terrain that exists only on a declined board needs the importer's scope widened for that board, a track 1 task. A claim never uses a hand-made board: hand-made terrain reads as Unverified and checks the rule against terrain nobody has confirmed.

## 8. The proving slice

Before the claim board opens, three rules prove the mechanism end to end, each with a contract, a fact source or pending facts, a minigame, and a workbench check:

| Rule | Why this one | What it proves |
|---|---|---|
| A rally rule already wired (from `ScenarioA1RallyCalculator`) | Built and played today | A contract can describe existing code; every fact shows as gathered |
| E10 eligibility (`TurnAmmoEligibilityRules`, Turns D4.1) | A calculator with no wiring | Pending facts, hand entry, and the JSON entry point |
| A concealment rule (from `ScenarioA1Concealment`) | Depends on what a side may see | Per-viewer disclosure on the workbench |

If the three work in the Studio, the contract format is frozen and the board opens. If one does not, the format changes once, here, before any claim depends on it.

## 9. Track 3: Play by capability, accepted by phase

Work is split by capability, because the phases share code: fire runs in Prep Fire, in Defensive First Fire during the Movement phase, in Defensive Fire, and in Advancing Fire. The capabilities are fire, movement, rout, rally, close combat, setup and work (including packing and ammunition supply), and the turn's boundaries and cleanup. A capability starts when the claims it reads are built.

Each phase is accepted against the Turns track's catalogs (its walkthrough, its checklist catalog, and the cross-phase dependency catalog) once the capabilities it uses are wired, by playing the minigames of its rules and one full scenario card through that phase.

Two preparatory tasks open the track:

- **Split `GameProjector.cs`** (5,468 lines) into partial files by capability, so that several agents can wire at once without editing the same file.
- **Move the game logic out of MapStudio `Services/`** into Play: live play, replay steps and differences, immediate proposals, movement plans, close combat declarations, scripted dice, and the like. MapStudio then holds only UI.

### 9.1 Wiring on the claim board (the user, 2026-10-29)

**The division of labor.** Claude takes the rule claims (track 2), which change the Rules library and its tests only (the Rules conventions, section 2); Codex takes the wiring (track 3), which moves Play, the Units projector, ASL.Turns, and the Studio's services onto the calculators. It is a default, not a lock: when one queue is empty, either agent takes from the other. Each reviews the other's work: Codex, who will call a calculator, reviews its facts and verdicts; Claude reviews the wiring against the rules.

**The wiring table** (the inventory's section 7). A rule claim that reaches "built" adds one wiring row, `W-<claim>`, from its design's map: the sites outside Rules that decide the rule and the calculator method that replaces each; the old Rules members to remove once their callers have moved; and the behavior changes the wiring will bring (where the claim's reading differs from what play does today). A row is open, claimed, wired, reviewed, or accepted.

**Wiring claims** (the inventory's section 8) are taken by capability (fire, movement, rout, rally, close combat, setup and work, the turn's boundaries), as above: one wiring claim takes the open rows of its capability whose rule claims are merged into main (the user, 2026-10-31; built was enough before, and it let wiring stack on unmerged Rules branches; every branch now starts from main and every pull request targets main, AGENTS.md step 3). It is made like a rule claim, by one commit to main, and it holds the files outside Rules it will change, by path under `src/ASL/`, whole or `(shared)`: no two wiring claims hold one file whole. Its branch changes only the files it holds, its tests, and the Rules members its rows name for removal. Behavior changes land here, so a wiring claim runs the merge gate, and each replay or sweep difference is one of its rows' named changes; older recorded games keep their recorded reading.

**States.** A wiring claim and its rows go claimed, wired, reviewed, accepted (when track 3 accepts the phases its capability serves). When every wiring row of a rule claim is wired, that rule claim and its inventory rows move to "wired" too, and to "accepted" with the phase.

**The check.** `claim_check.py` reads both tables: one holder per file across the wiring claims; each wiring row's rule claim exists and is built or later before the row is claimed; a wiring claim's branch changes only what it holds; and a rule claim at "wired" has every wiring row wired.

**Two working folders.** A wiring claim runs the merge gate, so Codex keeps its own working folder (`ASL_GATE_WORK`, which every tool under `code-builders` reads) and its own Studio port; AGENTS.md names them.

## 10. Coordination between Claude and Codex

- **Separate checkouts.** The main checkout is Claude's; Codex works only in its own worktree. Codex's uncommitted Turns documents now in the main checkout (the plan's review update and the D4.2 design) are committed or moved to its worktree first.
- **Shared numbers are settled at rebase.** The vocabulary version, event type names, backlog section numbers, and gate configurations are taken when a branch is rebased on main before its merge, not when it is cut. Since 2026-10-29 the branch merges main rather than rebasing (no force push), and `code-builders/documents/settle_documents.py` settles the documents two claims both edit: rows changed on different sides, lines both appended, the generated parts rewritten from the rows, and a colliding backlog section renumbered. Only a cell or a sentence both sides changed is left to a person.
- **Shared code outside Rules.** Play, Units, ASL.Turns, and MapStudio are not held. `claim_check.py` warns when this branch changes a file another open claim's branch changes too, and `--touched` lists what each open claim's branch changes, so the agents agree who goes first before the conflict is large.
- **One gate.** Each branch's gate baseline is cut from current main. Unwired claims change no replay, so most claims' gates show no differences; a wiring change in track 3 is gated as a pass is today.
- **`AGENTS.md`** gains the claim board and the one-rule-one-calculator rule once this plan is approved.
- **The branch `asl-narrative-extensions`** (named counters and name pools) is independent of this plan and is left alone.

## 11. What changes in the existing plans

- **The redesign plan.** Its pass order (73 to 185) is replaced by the claim board. Its pass groupings remain as a guide to what is claimed together, and its "Needs first" edges become the claim board's ordering where they are real (section 4.4). The pass documents stay as they are (the user, 2026-10-28), and each pass's section carries its Rules status (section 11.1).
- **The Turn Guide later Rules plan.** Its 36 tasks are merged into the pass sections of the redesign plan (the user, 2026-10-28; section 11.2). The Turns plan then keeps its history and its acceptance records, and points to the pass sections for what remains.
- **The standing rules.** "The Studio is the only test until the code is complete" holds for a claim: the workbench check of the minigame position comes before its tests (section 4.3, steps 4 and 5).

### 11.1 Each pass's Rules status

The pass documents stay as they are: the redesign plan's pass sections, and the designs and reviews under `Passes/`. Because the rules are now built ahead of their passes, each pass section in the redesign plan gains one status line, kept current as claims land:

> **Rules status (from the inventory, 2026-11-02):** 14 of 18 rows built; claims A8-residual (built, reviewed), A9-spraying (claimed by Codex). Wiring: 0 of 18. Minigames: 2.

The line is written by a script under `code-builders/`, not by hand. The script reads the inventory's Pass, Status, Owner, and Claim state columns, counts each pass's rows, and rewrites the line in each pass section. A claim runs it as its last step (section 4.3, step 8), and the gate's strict stage fails when a pass's line differs from what the inventory says, so the plan cannot drift from the ledger.

A pass whose rows are all built is marked **"Rules part built"**. What is left of that pass is its wiring and its controls, done in tracks 3 and 4.

### 11.2 The Turns tasks merged into the pass sections

Each of the Turns plan's packages decides rules that the inventory already gives to a pass. Each task is merged into that pass's section as two parts: its **Rules part** (a claim on the board) and its **wiring part** (track 3). A Turns task already done keeps its record and counts toward its pass's Rules status.

| Turns package | Its rules | Pass sections it merges into | State today |
|---|---|---|---|
| D1 Spotter, Opportunity Fire, artillery, vehicle and PRC | C9.3, C9.31 Spotters; A7.25 Opportunity Fire; OBA and vehicle fire dependencies | 110 (C9.3 and A7.25 are its rows); 150 and 155 for artillery | D1.1 done (Rules part); D1.2 in progress, mostly wiring (designation command, deployment successor) |
| D2 Atmosphere | A24 white SMOKE, A24.61 Drift, A24.62 Gust, B25.6 wind; WP; vehicle, FFE, and ambient dust | 85; 155 for FFE smoke; 95 for weather | D2.1 in progress (`TurnWhiteSmokeRules`, Rules part) |
| D3 Packing | G10 Animal-Packing; Horse and Mule identities (A13 Cavalry) | 165, with G10 rows added to the inventory (section 4.1) | D3.1 done; D3.2 in progress (Rules contracts, journals, Horse setup identity) |
| D4 Ammunition | E10 Ammo Vehicles and Dumps; D3.71 circled B# and Low Ammo | 175 for E10; 105 for D3.71 | D4.1 done (Rules part); D4.2 designed, not approved; its design attaches to pass 175 |
| D5 Tunnels and airborne | B8.6 tunnels; E9 paratroops, E8 gliders | 145 for tunnels; 170 for airborne | Not started |
| D6 Close Combat and heroes | The A11 and A15 rows still open | By row: 73, 80, 105, 110, 115, 135, 165, 180 | Not started |
| D7 Profiles | Water and amphibious; aerial; night; weather; modules | 125, 170, 175 for water; 160 for aerial; 90 for night; 95 for weather | Not started |
| D8 Trip Flares and Clearance | E1.95 Trip Flares; B24.7 and A23.72 Clearance | 115 for B24.7; 75 for A23.72; 90 for E1.95 to E1.953 (brought into scope, question 9) | Not started |
| D9 Core topics | Rally, movement, Rout, terrain hazards, status, possession, prisoners, turn boundaries | By row, mostly 73 and 74; D9.1's matrix becomes the inventory's Owner column | Not started |

Each package's fourth task (its acceptance) becomes the phase acceptance of track 3 (section 9) for the phases its rules touch. The Turns catalogs, walkthroughs, and source register stay in `Turn.Guide/` and are cited from the pass sections.

## 12. Memory and performance

Answers to the [guidelines' checklist](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>):

- **Work added per state, unit, hex, or step.** None during play. The workbench evaluates one contract on request. Fact sources read through the state's map context and `PlayStepMemo`, the existing paths.
- **Ownership, lifetime, invalidation.** The contract registry is built once per process from immutable records. The workbench keeps no cache; each run reads one snapshot at one revision.
- **Disclosure and snapshots.** Facts are read through the viewer's perspective, and the workbench shows a viewer only what the contract's disclosure allows (section 8 proves it).
- **Rendering.** The workbench does not touch the counter layer; the board shows the minigame through the existing incremental path.
- **Regression checks.** The registry test (section 5), the claim's own test methods, and in track 3 the minigames' replay and sweep at the gate.
- **Test cost.** The registry test builds the registry once per run. A new test project, if one is made, is named in a CI share.
- **Measurement.** No speed is claimed. If track 3 wiring adds measurable read cost to play, it is measured as the guidelines require before it is accepted.
- **Results and cleanup.** Each claim updates the inventory, the coverage document, and the backlog before it merges.

## 13. Build outputs outside the repository

The proving slice and the claims use Claude's existing Studio build under `asl-gate-work/studio-6670` and the gate's own outputs, which are registered already. The `MapHexIndex` is written by the fidelity batch into the Studio's existing cache folder, beside the batch's own reports, and makes no new outside folder. A task that does make an outside build registers it with `outside_outputs.sh` at once and removes it at the latest by the gate's `ci` stage. Codex's builds follow the same rule from its worktree.

## 14. Open questions

Each carries a proposal.

| # | Question | Proposal |
|---|---|---|
| 1 | Is the claim unit a numbered section, split where large? | **Ruled (the user, 2026-10-28): yes.**  Finer units multiply claims and reviews; coarser ones block other agents on a shared calculator. |
| 2 | Claims in the inventory by commit to main, or as GitHub issues? | **Ruled (the user, 2026-10-28):** the inventory, by commit to main (section 4.2): one ledger that both agents' checks can read. |
| 3 | Who reviews a claim? | **Ruled (the user, 2026-10-28):** the agent that did not build it: Codex reviews Claude's claims and the reverse, and a sub-agent where only one is working. |
| 4 | Does the proving slice (section 8) come before anything else? | Yes. It fixes the contract format, the workbench, and the minigame form before any claim depends on them. |
| 5 | Which Turns contracts are reconciled first? | **Ruled (the user, 2026-10-28): as proposed.** C9.3 Spotters first, because two calculators already decide it. Then A24.5 white SMOKE and E10 eligibility. |
| 6 | Does track 3 wait for the whole Rules library? | No. A capability starts when the claims it reads are built (section 9). |
| 7 | Do the `Turn*` file names stay? | They follow the Rules project's conventions when their claim is reconciled; the D4.2 design already proposes the `ScenarioA1` prefix. |
| 8 | Rows for chapters outside A to E? | **Ruled (the user, 2026-10-28): the Rules work goes beyond Chapter E.** By the PDF's outline, about 1,100 entries follow Chapter E (F 186, G 554, H 161, W 200). Every row is added in Step 0 with the PDF edition it was read from. **Claimed now:** the H Vehicle and Ordnance Notes (pp. 336 to 598), added as track 1 brings each counter that carries a note into the catalog; Appendix 2, the ASOP (pp. 678 to 681); G10 Animal Pack; and the Trip Flares (question 9). **Claimed in a second wave:** F North Africa, the rest of G Pacific Theater, and W Korean War, each once track 1 has imported that theater's boards and counters, since a claim needs its minigame. **Stays with passes 190 and 195:** H1, the DYO purchase rules. **Out:** J Deluxe ASL (miniatures and conversion) and K Training Manual (it teaches A to E and adds no rules; its eight Days serve as sources for A to E minigames). The other appendices (QRDC, OBA aid, Overrun flowchart, charts) summarize rules already in A to H and serve as cross-checks for minigame checks. |
| 9 | Trip Flares (E1.95 to E1.953): the inventory leaves them out by ruling, as a Pacific-only rule waiting with Chapter G, while Turns package D8 has them in scope. | **Ruled (the user, 2026-10-28): brought into scope.** The four rows join pass 90 (Night), since E1.95 sits under E1. Their minigame names them by SSR on an existing board. The redesign plan's left-out table loses its Trip Flare entry (5 rows become 1, A25.934) and the inventory rows move to pass 90; both edits are made with first task 3. |
| 10 | Is each pass's Rules status line (section 11.1) written by a script and checked at the gate? | **Ruled (the user, 2026-10-28): yes.** `code-builders/documents/pass_rules_status.py` writes the lines, and the merge gate's strict stage runs its `--check`. A hand-kept line would drift from the inventory within a week of parallel claims. |

## 15. First tasks once approved

0. **Step 0: bring the rule inventory up to date as the ledger** (the user, 2026-10-28). The [rule inventory](<../ASL Rule Inventory A to E.md>) already lists every numbered rule of A to E with its status, code, tests, and pass, but it records whether a rule is in the code, not whether it is in the Rules library. Of its 1,009 implemented rows (built, built with a deviation, partly, refused), 253 cite Rules code only, 590 cite Rules and Play or Units, 103 cite Play or Units only, and 63 cite neither. It also predates the Turns work (Horse identity, E10 eligibility, white SMOKE strength, the Spotter contracts), and its counts table no longer matches its rows. Step 0:
   - Adds a **Rules calculator** column: the calculator and method that decides the rule, or "none". Each of the 103 Play-or-Units-only rows is read: a rule decided outside Rules is a fault under the standing rules and becomes a claim.
   - Adds the **Owner**, **Claim state**, and **Minigames** columns (sections 4.2 and 7).
   - Records the Turns work against its rows.
   - Adds rows beyond Chapter E (question 8): F, G, W, the H Notes, and the ASOP, each with the PDF edition it was read from and its claim wave. The inventory's title ("A to E") no longer fits; renaming the file is a separate change that goes by pull request.
   - Regenerates the counts table by script from the rows.

   **Progress (2026-10-28, branch `step0/inventory-ledger`).** Done: the Rules calculator column (121 rows read, 38 claims found, 23 more in passing), the Turns work on 23 rows of A to E and on G10, the Owner, Claim state, and Minigames columns (every row with a rule open), 1,093 rows beyond E (F, G, H1, W, the ASOP; "not audited"), and both count tables written and checked by script. Two figures above were wrong: by the Where cells there are 108 rows that cite Play or Units and no Rules code, not 103, and 49 that cite Maps alone; and the counts table did match its rows. Left: the check of the "no in-scope board prints" notes, which waits for the `MapHexIndex` (first task 5).

   **Progress (2026-10-28, first task 5, branch `task5/map-hex-index`).** The `MapHexIndex` is built ([its design](<../Designs/ASL MapHexIndex Design.md>), rulings RMH.1 to RMH.7) and the check above is done. The notes were in the redesign plan's Pass 145 lines, not in the inventory. Railroads (`terrain:Railroad, Ground Level`, bdBFPA and bdBFPB) and a Tower (bdBFPA [K6]) are on in-scope boards; Narrow Streets, Steeples, Rail Cars, and Manholes have no VASL terrain type; no in-scope board holds a sewer or tunnel. What the 61 out-of-scope boards print the index cannot say. The Pass 145 lines are corrected. The inventory's own section 1 (point 5) and section 3 say how each part was made.
1. Commit or move Codex's open Turns documents out of the main checkout (section 10).
2. Add the Owner and Claim state columns to the inventory, and the claim board and the one-rule-one-calculator rule to `AGENTS.md`.

   **Progress (2026-10-28, branch `task2/claim-board`).** Done, with questions 1 to 3 ruled by the user that day. The columns came with Step 0. The inventory's section 6 is the claims table (claim, agent, reviewer, branch, held Rules files, state). `AGENTS.md` says how a claim is made (one commit to main that fills the table and the rows), built, reviewed, and checked, that the main checkout is Claude's and Codex works in its own worktree, and that the board opens with task 6. `code-builders/documents/claim_check.py` checks the ledger (owners, states, no Rules file held twice) and a branch's changed Rules files against its claims; CI's scope job runs it with the status-line check on every pull request and push, and the merge gate's strict stage runs both.
3. Merge the Turns tasks into the redesign plan's pass sections (section 11.2), move the Trip Flare rows to pass 90 (question 9), write the Rules status script, and add each pass's first status line (section 11.1).

   **Progress (2026-10-28, branch `task3/turns-into-passes`, on top of Step 0's).** Done. Each of the 36 Turns tasks has a home pass section (73, 85, 90, 110, 115, 125, 145, 165, 170, 175) with its Rules part, wiring part, and state, and 15 other passes carry a pointer. The later Rules plan keeps its acceptance contract and history and maps each task to its pass. The Trip Flare rows are in pass 90 and the 16 G10 rows in pass 165. The 25 passes with rows carry a status line written by `pass_rules_status.py`, which the gate's strict stage checks (question 10). This settles first task 1 as well: Codex's D4.2 design is committed with this task and linked from pass 175, its review note is carried into the later Rules plan's head, and `.codex/` stays untracked. The claims follow question 5's proposed order (C9.3, then A24.5, then E10); the question stays open.
4. Build the proving slice (section 8): the contract record, the registry test, the JSON entry point, one Play fact source, the workbench page, the `Minigame` property on game records and cards with the list filters, the bundle manifest and its registry, the shared minigame test class, and three minigame bundles.

   **Progress (2026-10-28, branch `task4/proving-slice`, claim `proving-slice` built, its review Codex's).** Built at the user's word, with question 4 answered by that word: the slice comes first. All three rules work in the Studio: the rally with every fact gathered, E10 with facts entered by hand, and the advance's concealment with what each viewer may know, which added an "opponent" audience to the contract format (section 5 names it). The format is frozen as built ([its design](<../Designs/ASL Rules-First Proving Slice Design.md>), section 6). The inventory's Minigames column is written by `minigames_column.py` and checked in CI and the gate. What it left out is backlog section 78.
5. Track 1 builds the `MapHexIndex`, its write in the fidelity batch, the library page's "Has terrain" filter, and the agents' index script (section 7.3). Step 0's check of the "no in-scope board prints" notes uses it, so this task runs alongside Step 0.
6. Open the claim board.

   **Progress (2026-10-28).** Opened at the user's word: "Before we bring codex into it. Open the claim board and do your first claim as proposed. Then let's evaluate how that went. Work independently up to the merge." `AGENTS.md` says the board is open. Claude takes the first claim, C9.3 Spotters (question 5), alone, with a sub-agent as reviewer, and stops before its merge so the user can judge how the board worked before Codex joins. **The C9.3 claim built (2026-10-29, branch `claim/c9.3-spotters`):** [its design](<../Designs/ASL C9.3 Spotters Claim Design.md>) has the rulings, the as-built account, and the review; it found two shared files every claim would edit (the contract registry and the bundle test's count) and changed both so that a claim adds files only.

   **The evaluation's five issues addressed (2026-10-29, branch `board/claim-fixes`, merged as PR 55, 13874daf, at the user's word "Address all these issues, now").** The documents two claims both edit are settled by `settle_documents.py` when a branch merges main (section 10). Files outside Rules are watched by `claim_check.py`'s overlap warning and `--touched` (section 10). Large shared calculators are held "shared" (section 4.1), and C9.3's three ordnance files are marked so. ASOP rows go to the claim whose rules alone a step cites, and to track 3 otherwise (section 4.1), so C9.3 takes ASOP 3.11A. The Rule Workbench lets a side pick its own unit for an owner's fact that names itself (the proving slice's design, section 6). The C9.3 branch then merged main, and `settle_documents.py` settled the inventory, the backlog, and the time log; only this paragraph was merged by hand.
