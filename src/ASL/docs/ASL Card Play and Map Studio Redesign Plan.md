# ASL Card Play and Map Studio Redesign Plan

**Status:** Approved by the user on 2026-09-30. It merges and replaces two documents: the ASL Unit Card Play, Deferred Rules, and DYO Plan (approved 2026-09-30, passes 23 to 30) and the ASL Map Studio UI Redesign 01 Design (branch `UI-Redesign-01`, commit `800416a`); both were deleted after their content was carried over here, and git history keeps them. Passes 22b, 22c, and 22d are built (2026-09-30; see their designs, [22b](<ASL Unit Backlog Pass 22b Design.md>), [22c](<ASL Unit Backlog Pass 22c Design.md>), and [22d](<ASL Unit Backlog Pass 22d Design.md>), and reviews, [22b](<Scenario A1 Backlog Pass 22b Review 2026-09-30.md>), [22c](<Scenario A1 Backlog Pass 22c Review 2026-09-30.md>), and [22d](<Scenario A1 Backlog Pass 22d Review 2026-09-30.md>)); pass 23, the first game pass, is built too (2026-09-30; its [design](<ASL Unit Backlog Pass 23 Design.md>) and [review](<Scenario A1 Backlog Pass 23 Review 2026-09-30.md>)), pass 24 (2026-09-30; its [design](<ASL Unit Backlog Pass 24 Design.md>) and [review](<Scenario A1 Backlog Pass 24 Review 2026-09-30.md>)), pass 25 (2026-10-01; its [design](<ASL Unit Backlog Pass 25 Design.md>) and [review](<Scenario A1 Backlog Pass 25 Review 2026-10-01.md>)), pass 26 (2026-10-01; its [design](<ASL Unit Backlog Pass 26 Design.md>) and [review](<Scenario A1 Backlog Pass 26 Review 2026-10-01.md>)), pass 27 (2026-10-01; its [design](<ASL Unit Backlog Pass 27 Design.md>) and [review](<Scenario A1 Backlog Pass 27 Review 2026-10-01.md>)), pass 28 (2026-10-01; its [design](<ASL Unit Backlog Pass 28 Design.md>) and [review](<Scenario A1 Backlog Pass 28 Review 2026-10-01.md>)), pass 28b, a Studio pass (2026-10-02; its [design](<ASL Unit Backlog Pass 28b Design.md>) and [review](<Scenario A1 Backlog Pass 28b Review 2026-10-02.md>)), and pass 28c, the last Studio pass (2026-10-02; its [design](<ASL Unit Backlog Pass 28c Design.md>) and [review](<Scenario A1 Backlog Pass 28c Review 2026-10-02.md>)). The passes run one at a time on the user's go-ahead. Passes 32 and 33, the DYO purchase, are deferred (2026-10-02, the user's word) in favor of UI and playability; they were passes 29 and 30, and every planned pass after them moved up by one when the user added pass 29, the shared board workspace, on 2026-10-02, and again when the user added pass 30, prepared setups, on 2026-10-03, and a third time when the user added pass 31, the play-test UI, on 2026-10-04 (section 4 has the old and the new numbers). Pass 29 is built (2026-10-03; its [design](<ASL Unit Backlog Pass 29 Design.md>) and [review](<Scenario A1 Backlog Pass 29 Review 2026-10-03.md>)); pass 30 follows it.

**Date:** 2026-09-30

**Scope:** seventeen passes in one order, followed by the legacy scenario migration (Part VI: display batches D1 to D18 and rule passes 34 to 43, with 35b). Ten game passes, 23 to 28, 30, 30b, 32, and 33, close three groups of open backlog rows: card-driven play (backlog sections 27 to 32), rules deferred by earlier passes (entry and exit, the Heat of Battle, Leader Creation, and berserk gaps), and the Chapter H DYO purchase, which generates a scenario card. Pass 31, the play-test UI, changes the Play page and the game together, from the findings of a played game. Six Studio passes, 22b to 22d, 28b, 28c, and 29, carry out the Map Studio redesign: the supplied theme, grouped navigation, shared components, every page's markup extracted into Blazor components, and the map-centered Play workspace. The game passes also extract the Play components they change.

Section 1 of the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>) (how a pass is run, the standing rules, and the merge gate) applies unchanged to every pass here, including its visual check in the Studio before the costly tests (section 17.2).

**Related documents:** the [ASL Unit Backlog](<ASL Unit Backlog.md>) (sections 27 to 32); the designs and reviews of passes 17 to 22; the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), whose ordnance and vehicle passes the game passes build on; the [Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), the [Architecture and Rendering Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design.md>), the [Unit Display Design](<ASL Unit Display Design.md>), the [Governed Writes Design](<ASL Unit Governed Writes Design.md>), and the [Unit Read Contract Design](<ASL Unit Read Contract Design.md>).

**Contents:** Part I, direction and decisions (sections 1 to 3). Part II, the passes (sections 4 to 8). Part III, the Studio design (sections 9 to 15). Part IV, the component inventory (section 16). Part V, acceptance (section 17) and rulings (section 18). Part VI, the legacy scenario migration (sections 19 to 23). Appendix A, the legacy card catalog.

# Part I. Direction and decisions

## 1. Outcome

**The game.** Every game stays a card. The game passes make a card's play fuller (per-side views and hidden setup, Control of Locations, Gun and vehicle VP, vehicles and Guns from off board), close the rules earlier passes deferred (entry and exit, the Heat of Battle, Leader Creation, and berserk gaps), and add the Chapter H DYO purchase, which ends in a saved user card that the Play page starts like any other.

**The Studio.** The Studio makes the next useful task apparent while keeping the board, the current selection, and the result of an action understandable. It gets a consistent application shell with two page families: collection pages for finding things and workspaces for manipulating or inspecting them. Every page's markup becomes Blazor components with narrow contracts, and Play becomes a map-centered workspace. The supplied `ASL Map Studio UX Audit & Redesign PDF` (one page, 28 Sep 2026) and its stylesheet are design inputs supplied by the user, evaluated here rather than treated as instructions; the originals stay outside the repository.

## 2. Principles

**One path into a game.** The DYO purchase ends in a user card, so there is no second path into a game.

**Rules first, then tools.** The deferred rules (passes 25 and 27) come before the editor's forms (pass 28), so the forms write fields that play already reads.

**Only what the game plays is purchasable.** A DYO purchase of something the game has no rule for (OBA, Air Support, fortifications, boats, gliders, horses) is refused with its backlog row, never accepted and then ignored.

**Missing values are manufactured, never blocking.** Counter values no registered source prints (for example, Axis Minor counters for their Heat of Battle exceptions) are manufactured under R0.3 on sheet MFG.

**Components follow the work.** Shared components come first because later passes reuse them. Pages no game pass touches are converted in dedicated Studio passes before the game work. Play, which almost every game pass changes, is converted by each pass for the panels it changes, and the rest of Play once its content has settled. Converting Play in one sweep while the game passes change it would mean constant conflicts in a 4,371-line page.

**The redesign changes presentation and interaction, not the game.** It preserves:

- Deterministic server-generated SVG, existing terrain rendering, board labels, counter styles, and far/mid/near counter tiers.
- Browser-local pan and zoom, canonical board and Location identities, and existing rendering and read boundaries.
- Game projections, disclosure rules, replay, revision checks, and propose-then-confirm writes through the Execution Gate.
- Fidelity evidence, provenance, and restrictions on saving source-derived drafts.
- Existing routes and meaningful deep-link parameters, including board, game, perspective, and revision.

New rules adjudication, changes to game persistence, VASL artwork, a new component-library dependency, and a general mobile gameplay product are outside this redesign. Narrow-screen access and keyboard operation remain required.

## 3. Decisions

**The user's answers (2026-09-30):**

1. **Hidden setup in a hot-seat game (pass 23): a hand-over screen.** The Studio blanks the map and the side panels between sides until the next side confirms it is at the screen; there is no separate tab per side. A later answer the same day, when pass 23 was built: in a sequential setup the second side sees the first side's finished setup as on the board (the rulebook, A12.12 and A2.9), not nothing until both have set up.
2. **Scope of group 2: the berserk gaps stay.** Tasks 27.2 and 27.3 remain in pass 27.
3. **DYO nationalities (passes 32 and 33): German and Russian first.** The DYO page offers the two nationalities the cards use; the other nationalities the catalog carries, with manufactured counters where needed, go to the backlog when pass 32 is built.
4. **Order: as numbered.** The game passes run 23 to 28, then 30, 30b, 32, and 33, so each builds on the one before and the editor forms (pass 28) follow the rules they edit. This merged plan keeps that order and inserts the Studio passes around it (section 4); the insertion needs the user's approval.
5. **Autonomy: one pass at a time.** It applies to every pass here, the Studio passes included. Each pass starts only on the user's go-ahead and stops after its merge, with its times reported; a rule question that changes a pass's scope, or a failure that needs a design change, still stops the pass. Passes 26, 32, and 33 reach into areas not built before (vehicles from off board, Chapter H), which is why they are not run unattended.

**Studio design decisions** (from the redesign; validated during the passes named):

| Decision | Initial proposal | Validate during implementation |
|---|---|---|
| Navigation | Grouped sidebar, collapsible; current routes retained | Canvas width and route ancestry |
| Visual direction | Warm neutral surfaces and olive controls | Contrast and terrain separation |
| Fonts | Bundled IBM Plex Sans/Mono and Barlow Condensed, with system fallbacks | Long labels, readability and loading |
| Library | Compact list with search and filters (pass 22c); SVG previews in the backlog | Browsing density |
| Play | Map-centered workspace with phase task groups (pass 28c) | Passes 14-22 contracts and disclosure; the card panel and Victory standing under per-side views (pass 23) |
| Card workflow | Scenario cards and the card editor grouped under Scenarios; new games from a card on Play | Editor forms (pass 28) built on the shared components (pass 22b) |
| Proposal review | Persistent pane; explicit confirmation | Stale/reconnect and withheld-information behavior |
| Mobile | Reflow and task access, not a separate gameplay product | Keyboard, zoom, drawers, long labels |

# Part II. The passes

## 4. The schedule

| Pass | Title | Kind | Tasks | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---:|---|---|---|
| 22b | The theme and the shared foundation | Studio | 5 | 4:15 | 5:30 | 3:51 to 7:09 |
| 22c | The collection pages | Studio | 4 | 3:45 | 5:00 | 3:30 to 6:30 |
| 22d | The workspaces | Studio | 4 | 4:30 | 5:45 | 4:01 to 7:28 |
| 23 | Per-side views and hidden setup | Game | 5 | 4:10 | 5:25 | 3:47 to 7:02 |
| 24 | Control of Locations and more VP | Game | 5 | 2:10 | 3:25 | 2:24 to 4:26 |
| 25 | Entry and exit | Game | 7 | 4:50 | 6:05 | 4:15 to 7:54 |
| 26 | Vehicles and Guns on a card | Game | 7 | 4:45 | 6:00 | 4:12 to 7:48 |
| 27 | Heat of Battle, Leader Creation, and berserk gaps | Game | 5 | 4:00 | 5:15 | 3:40 to 6:50 |
| 28 | The card editor's forms and map picking | Game | 5 | 3:45 | 5:00 | 3:30 to 6:30 |
| 28b | The rest of Play | Studio | 4 | 3:10 | 4:25 | 3:06 to 5:44 |
| 28c | The Play workspace and hardening | Studio | 3 | 4:00 | 5:15 | 3:40 to 6:50 |
| 29 | The shared board workspace | Studio | 5 | 3:15 | 4:30 | 3:09 to 5:51 |
| 30 | Prepared setups | Game | 6 | 8:30 | 9:45 | 6:50 to 12:41 |
| 30b | Setup plans for the side that sets up second | Game | 8 | 7:30 | 9:00 | 6:18 to 11:42 |
| 31 | Play-test UI | Studio and game | 17 | 22:45 | 24:45 | 17:20 to 32:11 |
| 32 | DYO purchase I: Infantry, leaders, and SW | Game | 5 | 3:15 | 4:30 | 3:09 to 5:51 |
| 33 | DYO purchase II: ordnance, vehicles, and conditions | Game | 4 | 2:30 | 3:45 | 2:38 to 4:52 |
| | **All passes** | | **99** | **91:05** | **113:20** | **79:20 to 147:20** |

**Order.** The passes run in the order listed, one at a time on the user's go-ahead.

- **22b first.** The theme ends the split between main's app.css and the branch's site.css, which each game pass would otherwise widen, and the shared components (navigation, page header, field groups, findings, feedback, the perspective and card pickers) are reused by pass 23's side views and pass 28's forms.
- **22c and 22d next.** No game pass touches the collection pages or the workspaces, so they convert without conflicts. Both can move later if the game work is wanted sooner, but 22d must precede pass 28, whose map picking uses 22d's board viewport.
- **23 to 28 in the game plan's order,** each also extracting the components it changes (tasks 23.5, 24.5, 25.6, 26.5, 27.5, and 28.5; 32.5 builds the DYO page from components): the side's view is an input to every Play component from pass 23 on.
- **28b and 28c after pass 28,** when Play's content has settled: the Play panels no game pass touched, then the map-centered layout, responsive behavior, and hardening.
- **29 after 28c,** the board viewer's inspector shared with Play (added 2026-10-02).
- **30 after 29,** prepared setups for a card's first side (added 2026-10-03).
- **31 after 30b,** the play-test UI: the problems a played game of The Guards Counterattack found (added 2026-10-04; its design asks whether it runs as passes 31 and 31b).
- **32 and 33 last,** the DYO page built from the shared components from the start.

**Separable groups.** The game passes alone are 23 to 28, 30, 30b, 32, and 33, without their component tasks; within them, card play alone is passes 23, 24, 26, and 28 (pass 26 then takes pass 25's entry tasks it needs), the deferred rules alone are passes 25 and 27, and DYO needs pass 28 only for opening a DYO card in forms, so it could follow pass 22 directly with the JSON editor. The Studio passes alone are 22b, 22c, 22d, 28b, 28c, and 29; without the game passes, the card pages' components of task 28.5 move into 22c and the Play components of tasks 23.5 to 27.5 into 28b, which then follows 22d.

**Renumbering of 2026-10-04.** The user added pass 31, the play-test UI, and every pass not yet started moved up by one. Documents written before that date (time log rows, reviews, designs of finished passes, commit messages) keep the numbers they were written with; read them with this table.

| Until 2026-10-03 | From 2026-10-04 | Pass |
|---|---|---|
| (none) | 31 | Play-test UI |
| 31 | 32 | DYO purchase I |
| 32 | 33 | DYO purchase II |
| 33 | 34 | Armored combat I |
| 34 | 35 | Armored combat II |
| 34b | 35b | Night and winter |
| 35 | 36 | Fortifications I |
| 36 | 37 | Fortifications II |
| 37 | 38 | Offboard artillery |
| 38 | 39 | Air support |
| 39 | 40 | Terrain I |
| 40 | 41 | Terrain II |
| 41 | 42 | Special units |
| 42 | 43 | Fire |

Task numbers moved with their passes (31.1 became 32.1, and so on). Passes 22b to 30b and the display batches D1 to D18 keep their numbers. Two earlier renumberings moved the same passes: on 2026-10-02 (pass 29 added: DYO 29 and 30 became 30 and 31, the rule packages 31 to 40 with 32b became 32 to 41 with 33b) and on 2026-10-03 (pass 30 added: DYO became 31 and 32, the rule packages 33 to 42 with 34b).

## 5. The passes

### Pass 22b: The theme and the shared foundation

**Purpose:** the Studio takes the theme, grouped navigation, and the shared components every later pass reuses, and shows a scenario card's full provenance. **After:** pass 22. **From:** sections 10, 11, and 15; the component candidates of section 16.3.

**Status:** Built 2026-09-30, all five tasks, with two reviews and two visual checks; the Unit Lab starting-template overlap of section 11 (planned for 22d.4) was fixed here. Items left out are in section 34 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 22b.1 The theme merged | Branch `UI-Redesign-01` merged: site.css with the supplied theme, the local fonts, and the viewport's data attributes; app.css retired, with any rule main has added since moved into site.css in the theme's tokens. | | 0:20 |
| 22b.2 Shell and navigation | S01 `StudioNavigation` (Boards, Maps, Units, Scenarios, Play, Verify, Settings; route-aware current page; collapsible; a drawer when narrow) and S02 `PageHeader` on every page; the source-derived draft restriction as a warning banner (section 11.2). | | 1:00 |
| 22b.3 Shared components | S03 `SourceRequiredNotice`, S04 `OperationFeedback`, S05 `StatusBadge`, S06 `FindingList`, S07 `FieldGroup`, S09 `PerspectivePicker`, and S10 `RevisionNavigator`, each with focused tests and the contracts of section 15. | | 1:10 |
| 22b.4 The card picker and the new game | K02 `CardPicker` on Play, Scenarios, and the card editor; K01 `NewGameFromCard` with K03 `CardStartSummary` and K04 `BalanceChoice`. | R22.4 | 0:30 |
| 22b.5 Card provenance | K21 `CardProvenancePanel` on the Scenarios page and in Play's card panel, showing a card's full provenance (section 13.6): its identity (id, built-in or user file, SHA-256, format, pinned catalog), and for a game the id, SHA-256, and catalog it started from; its source, legacy card, and adaptation notes; for each counter line, the catalog source record, its status, transcriber, and reviewer, and each printed value's sheet, counter, face, and row, with manufactured values marked; and for each rule citation, the registered fragment's rulebook PDF page, fragment id, and comparison result, or the ruling's id linked to its row in the Backlog Passes Plan, section 5 (no parsing of the rulings' text). The pass 17 comparison registry is embedded in the Play library, which reads it; the component receives a prepared model. | R0.3, R17.2 | 1:15 |
| | Overhead | | 1:15 |
| | **Pass 22b total** (build 4:15) | | **5:30** |

### Pass 22c: The collection pages

**Purpose:** the collection pages become components, and the Board library can be searched. **After:** pass 22b. **From:** sections 12.1 and 12.2; the candidates of sections 16.4, 16.5, 16.8, and 16.9. No game pass touches these pages.

**Status:** Built 2026-09-30, all four tasks: the 19 P1 candidates and M05 extracted, the other P2 and P3 candidates left inline (the review lists why), with two reviews and two visual checks. Items left out are in section 35 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 22c.1 Board library | H01 to H06; search by name or reference, filters (source type, verification status, out of scope), the result count with Clear, and separate loading, empty, no-match, and failure states; filters and scroll kept on return from a viewer. | | 1:15 |
| 22c.2 Maps and New board | M01 to M08: the placement editor keyed by stable draft ids, the SSR rule editor keeping rule order, compact placement text in an Advanced section, and a stale preview labeled as such. Pickers stay native selects (section 14). | | 1:00 |
| 22c.3 Game states | G01 to G08, composing S09 and S10; G03 `GameContextSummary` is reused by Play's context header in pass 28c. | | 0:45 |
| 22c.4 Fidelity and Settings | F01 to F09. | | 0:45 |
| | Overhead | | 1:15 |
| | **Pass 22c total** (build 3:45) | | **5:00** |

### Pass 22d: The workspaces

**Purpose:** the board viewer, the board editor, and the Unit Lab become components, with the board viewport's lifecycle in one place. **After:** pass 22c. **From:** sections 12.3, 12.4, and 15.5; the candidates of sections 16.6, 16.7, and 16.14. Pass 28's map picking needs 22d.1.

**Status:** Built 2026-09-30, all four tasks: 28 of the 34 candidates extracted (all 20 P1 and 8 of the P2 and P3), the rest left inline (the review lists why), with two reviews and two visual checks. Items left out are in section 36 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 22d.1 The board viewport | B06 `BoardViewport` owns the element reference, the JavaScript module, the .NET object reference, its callbacks, and disposal; bUnit tests for mounting, callbacks, and disposal, and a browser check for reconnection, route changes, comparison modes, and duplicate listeners, which bUnit's mocked JavaScript cannot show. | | 1:15 |
| 22d.2 The board viewer | B01 to B05 and B07 to B12: the view picker and Fit, a Layers panel, a game and replay strip, and inspector tabs for Selection, LOS, and Evidence. | | 1:15 |
| 22d.3 The board editor | B13 to B15 and E06 to E08: a document header with the dirty state, Undo, Redo, and Save; the tool rail and its options; inspector tabs; a confirmation before leaving an unsaved draft. | | 1:00 |
| 22d.4 Unit Lab | U01 to U10, E01 to E05, and S14 `JsonDisclosure`: grouped fields, the kind picker (a native select; section 14), the preview beside the active fields, and the starting-template list no longer overlapping the preview. | | 1:00 |
| | Overhead | | 1:15 |
| | **Pass 22d total** (build 4:30) | | **5:45** |

### Pass 23: Per-side views and hidden setup

**Purpose:** a side sees only what it may see. **After:** pass 22b (it runs after 22d in the order of section 4). **From:** backlog sections 27 (hidden setup per side), 29 (HIP by SSR, the non-OB "?"), and 31 (Control left undeclared).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 23.1 A side's view | The Play page's perspective (S09 `PerspectivePicker`) becomes a view: a side's view hides the enemy's concealed and HIP units' identities, the contents of "?" stacks, and undeclared Control; the adjudicator view shows all. A hand-over screen between sides (user answer, 2026-09-30) blanks the map and panels until the next side confirms. | A12.1, A12.3, A26.15 | 0:50 |
| 23.2 Hidden sequential setup | In a sequential setup, the side setting up second does not see the first side's placements until both have set up, except what the card places in view. | A2.9 | 0:35 |
| 23.3 HIP by SSR | An SSR token names the units that set up HIP; they are recorded hidden and revealed by the A12.3 triggers already used for concealment loss, and a hidden unit that moves is revealed (backlog section 20). | A12.3, A12.33, A12.34 | 0:55 |
| 23.4 The non-OB "?" | After both sides have set up, each side places "?" on units out of the enemy's LOS or at least 17 hexes away, checked against the board's LOS. | A12.12 | 0:25 |
| 23.5 Play context and review components | First, page tests that pin the Victory standing (`#play-victory`, `data-control`, `data-side`) in a side's and the adjudicator's view, which no test covers yet. Then P01 `ScriptedDicePanel`, P02 `LiveGameToolbar`, P07 `GameReplayFailure`, N02 `NightWeatherSummary`, R01 to R03 (the proposal review and its evidence), R12 `PlayUnitTable`, R13 `AdjudicatorAuditPanel`, K05 `SetupPoolsTable`, and K06 to K08 (the card panel, the game's end, the Victory standing), each receiving the side's view; the board link at Play:63 follows the view (section 15.4). | A26.15 | 1:25 |
| | Overhead | | 1:15 |
| | **Pass 23 total** (build 4:10) | | **5:25** |

**Built 2026-09-30** (rulings R23.1 to R23.6). Task 23.2 follows the rulebook, by the user's answer of 2026-09-30, not its first text: the side setting up is out of the other's sight, and once it has finished the other side sees its setup as on the board, each stack's top counter until play starts (A12.12, A2.9). A hidden unit that moves is first placed beneath "?" (A12.32), not revealed. All thirteen candidates of task 23.5 are extracted, with the hand-over screen as a fourteenth component.

### Pass 24: Control of Locations and more VP

**Purpose:** Victory Conditions count everything A26 counts. **After:** pass 23 (undeclared Control needs the side's view). **From:** backlog section 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 24.1 Control of Locations | Control kept per Location (upper levels, cellars), a building's Control from its Locations; Control by Mopping Up. | A26.1, A26.12, A12.153 | 0:50 |
| 24.2 Gun and vehicle VP | A Gun 2 VP; a vehicle 1 plus its MA and AF values; their CVP when eliminated or captured. | A26.212, A26.22 | 0:30 |
| 24.3 Start Control edge cases | A building partly in one side's area and partly on a board only that side sets up on; areas with no board on a multi-board card. | A26.11 | 0:15 |
| 24.4 Vehicles' temporary Control and the Control cache | A vehicle's temporary Control of its hex; the Control fold cached by revision. | A26.12 | 0:25 |
| 24.5 The Victory standing | K08 `VictoryStandingTable` gains Location rows and Gun and vehicle VP. | A26.212 | 0:10 |
| | Overhead | | 1:15 |
| | **Pass 24 total** (build 2:10) | | **3:25** |

Control forfeited to a Kindled Fire (A26.16) waits for Fire spread (backlog section 27) and stays in the backlog.

**Built 2026-09-30** (rulings R24.1 to R24.7). The user chose the rulebook on the three Control questions: an upper level or cellar no one enters keeps its own Control, a building's Control does not need every Location (A26.14), and Mopping Up comes with its Search casualties (A12.154). Cellars are left out of Location Control, since they have no use in play (B23.41). Mopping Up is a new action with a Play panel (`MoppingUpAction`), so the UI and Blazor reviewer ran too. No card fields a Gun or a vehicle yet, so task 24.2 is tested with constructed games. Task 24.3 found both edge cases unreachable from a valid card and resolves each hex on its own. Items left out are in section 38 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

### Pass 25: Entry and exit

**Purpose:** units enter and leave the map as A2.5 and A2.6 allow. **After:** pass 24 (exits by captors count double CVP). **From:** backlog sections 30 and 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 25.1 Entry by advance | An offboard unit capable of movement may enter by advance in its APh. | A2.5, A4.7 | 0:25 |
| 25.2 Blocked and delayed entry | The four-hex radius a Game Turn later; rubble or Blaze cutting an entry hex off; never across a river. | A2.5 | 0:35 |
| 25.3 Entry through the full movement step | Entering a hex with concealed enemy units, Residual FP, or a Fire Lane; Bypass, Minimum Move, SMOKE, or a DC at entry. | A2.51, A12.15, A8.22, A4.3, A4.134 | 0:40 |
| 25.4 Offboard actions | An offboard squad's Deployment attempt in its RPh with a leader; several entry areas for one OB line; a Balance counter in an entering group. | A2.52, A2.5 | 0:20 |
| 25.5 Exits | Leaving in the APh, by Bypass with its extra MF, and at the road rate; a Guard leaving with prisoners, and captured units exited for double Exit VP. | A2.6, A20.53, A26.23, A26.222 | 0:30 |
| 25.6 Movement components | First, a page test for leaving the map (`#move-exit`, `#propose-exit`), which no test covers yet. Then A01, A11, A13 to A16, A23, C19, K09, N03, N04, S11, and S12: the movement, entry, advance, and exit panels, with their resets (section 15.3). | | 1:20 |
| 25.7 Messages and small fixes | The SMOKE panel's eligibility (backlog section 19); clearer answers once Defensive First Fire has eliminated every mover, and the end-move refusal after a DC placer was Replaced (section 25); the projector's raw message for a setup naming another side's OB group, and the offboard Deployment message (section 30); an exit refusal from one row inside the edge naming the edge hex (section 31). | A24.1, A2.52, A2.6 | 1:00 |
| | Overhead | | 1:15 |
| | **Pass 25 total** (build 4:50) | | **6:05** |

**Built 2026-10-01** (rulings R25.1 to R25.7). The user chose the rulebook on the three entry questions: an entry area may name its entry hexes, with the four-hex radius a Game Turn later when they are all held (never past a river or canal); an entering stack that meets concealed units is forced back off board (A12.15); and the APh, not the MPh, holds units that must enter. Entry is now a step like any other, and SMOKE or a DC from off board is refused by A2.52 rather than left for later. A Guard leaves by any edge with its prisoners, spared from CVP only off its Friendly Board Edge or its exit condition's edge (the referee, on A20.53). All thirteen candidates of task 25.6 are extracted; K08 shows its Location note only with a Location row. Items left out are in section 39 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

### Pass 26: Vehicles and Guns on a card

**Purpose:** a card may field vehicles and Guns from setup to exit. **After:** pass 25 (entry and exit rules). **From:** backlog sections 29, 30, and 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 26.1 Vehicles entering | Vehicles enter from off board in Motion, loaded with their Passengers; Guns enter limbered and towed, with hook-up and en portee (backlog sections 12 and 18). | A2.52, D2.4, C10.1 | 0:50 |
| 26.2 Guns at setup | A Gun and its manning crew or HS set up together; the crew stacks as its own size. | A5.5, C10 | 0:25 |
| 26.3 Crews leaving with their Gun | A crew with its Gun leaves the map; the Exit VP count the Gun. | A2.6, C10.3, A26.23 | 0:20 |
| 26.4 A test card with armor | A manufactured test card (R0.3) fielding a halftrack, a tank, and a Gun, played end to end by the table player. | R0.3 | 0:40 |
| 26.5 Setup and vehicle components | P08 `SetupPlacementEditor` and P09 `SetupPlacementList` (Guns and crews, off-board placement, OB groups), A17 to A22 (vehicle movement, towing, Bounding Fire, CE/BU), C18 `GunArcAction`, and S13 `FacingPicker`; the vehicle panel's "wished to enter next" field for Motion (backlog section 21). | | 1:15 |
| 26.6 Covered Arcs across boards | A Gun's Covered Arc across composed boards and on a reversed board, checked first against what happens today (backlog section 12); pass 36 reuses it for pillboxes. | C3.2, C3.21 | 0:45 |
| 26.7 Concealed and hidden Guns | Concealed crews and Guns, a firing crew's loss of concealment, and an Emplaced Gun in Concealment Terrain setting up HIP where A12.34 allows it, confirmed against the PDF (backlog sections 12 and 22). | A12.34, C11 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 26 total** (build 4:45) | | **6:00** |

**Built 2026-10-01** (rulings R26.1 to R26.8). The user answered the five questions on the rulebook's side or the narrower build: Passengers load and unload; limbering and en portee wait, since both catalog Guns are QSU; A12.34's HIP is built for Concealment Terrain only; the MPh holds vehicles until they enter, since they cannot advance; the pass ran whole. The three known bugs are fixed: a vehicle's exit now records its `UnitExit` with its Passengers' and towed Gun's, a manned Gun's hidden flag is checked, and a crew manning a Gun stacks as a squad. Passengers are reached through their vehicle: they take no fire, gain no Control, and act only by unloading, until plan passes 34 and 35 build their fire and survival. The built-in card Armor Test is manufactured under R0.3. All ten candidates of task 26.5 are extracted. Items left out are in section 40 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

### Pass 27: Heat of Battle, Leader Creation, and berserk gaps

**Purpose:** the recorded gaps in A15 close. **After:** pass 26 (charges at vehicles need vehicles on a card). **From:** the backlog rows on berserk charges, Heat of Battle, and owner's options (sections 1, 11, 20 to 22, and 25).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 27.1 Nationality exceptions | The Axis Minor Heat of Battle and Leader Creation exceptions, with manufactured counters where no source prints them (R0.3); the Japanese ones wait with Chapter G (backlog section 33). | A15 table notes, p. 83; A25 | 0:40 |
| 27.2 The berserk route | A berserk charge's route counting Bypass, stairwells and upper levels, and a charge at a vehicle. | A15.431, A4.3, B23.4, A15.43 | 0:50 |
| 27.3 Berserk contact | A charge into concealed units draws Dummies by Random Selection (A.9); a berserk OVR's CC against a lone SMC resolved in the MPh; the berserk leader's companions' TCs in the CC record. | A.9, A12.15, A4.152, A15.432, A20.24 | 0:40 |
| 27.4 Owner's options mid-attack | A DC's Battle Hardening and Unlikely Kill options, and Spraying Fire's second Location or a Fire Lane after a choice, resumed. | A15.3, A7.309, A9.5, A9.22 | 0:20 |
| 27.5 Close Combat and fire components | A12 `BerserkChargeNotices`, C01 to C15 (Close Combat by Location, the attack builder and queue, withdrawals, pending choices and surrender, prisoners, opportunity and small-arms fire), and N05 to N12 (prisoner rounds, Ambush Withdrawal, capture, infiltration, custody, MOL, DC actions). The P1 roots are extracted; their P2 children stay inline unless a root is still hard to follow. | | 1:30 |
| | Overhead | | 1:15 |
| | **Pass 27 total** (build 4:00) | | **5:15** |

### Pass 28: The card editor's forms and map picking

**Purpose:** a card is made without editing JSON. **After:** pass 27, so the forms write every field play reads. **From:** backlog section 32.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 28.1 Forms for the OB, SSRs, and Victory Conditions | A group's name, ELR, areas, and counter lines; an SSR's text, status, and tokens; an outcome's winner and conditions; each as a form, the JSON kept as a view. | A19.1, A26, R17.1 to R17.13 | 1:00 |
| 28.2 Picking on the map | Boards composed or taken from a saved map; setup, entry, and exit areas picked on the map. | R17.8, R20.5, R21.5 | 0:50 |
| 28.3 Checks at edit time | Areas and hexrows checked against the chosen boards as the card is edited; plain messages for bad JSON and board text. | R19.3 | 0:25 |
| 28.4 Card management | Renaming a user card; a confirmation before deleting; a warning when a live game uses the card (a game-to-card index); the Guards card's stale SSR 3 note revised; a test of a game whose card changed or is gone (backlog section 29). | R22.2 | 0:45 |
| 28.5 The card pages' components | K10 to K12 (the card, read only) and K13 to K20 (the editor), the forms of 28.1 built from S07 `FieldGroup` and the map picking of 28.2 on B06 `BoardViewport`; the Source label's association is fixed (`for="edit-basis"` against the input `#edit-source-basis`). | | 0:45 |
| | Overhead | | 1:15 |
| | **Pass 28 total** (build 3:45) | | **5:00** |

### Pass 28b: The rest of Play

**Purpose:** the Play panels no game pass touched become components. **After:** pass 28, when Play's content has settled. **From:** section 13; the remaining candidates of sections 16.10 to 16.13 and 16.15.

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 28b.1 Rally, Rout, and support weapons | A02 to A10: Rout and its obligations, support weapons, Rally, repair, Deployment, Recombination, DM retention, and Shock recovery, with their resets (section 15.3); the Deploy control splitting several SW between the two HS (backlog section 23). | | 1:25 |
| 28b.2 Ordnance and Starshells | C16 `OrdnanceFirePanel`, C17 `OrdnanceTargetFields`, and N13 `StarshellActionPanel`. | | 0:30 |
| 28b.3 Activity records | R04 to R10: dice, the action records (night, Sniper, ordnance, Close Combat, Rally), the fire history and its cards, the arithmetic, and the effects tables, each keeping its disclosure filter; records of DM gained, Failure to Rout eliminations, and SW transfers (backlog section 23). | | 1:00 |
| 28b.4 Rule help | S08 `RuleHelp` for the long rule paragraphs across Play. | | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 28b total** (build 3:10) | | **4:25** |

**Built 2026-10-02**, all four tasks, with three reviews and two visual checks. The user took every recommendation: both backlog section 23 additions are built (the Deploy control gives several SW to the second HS; the records list shows DM gained and kept, Failure to Rout eliminations, and SW transfers); A09, C17, and R08 to R10 are extracted and A03 stays inline in A02; RuleHelp covers the Play page and this pass's panels, a summary with the full text collapsed beneath it. Nineteen of the twenty candidates are components. Items left out are in section 43 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

### Pass 28c: The Play workspace and hardening

**Purpose:** Play becomes the map-centered workspace, and every page meets the accessibility and responsive targets. **After:** pass 28b. **From:** sections 11.1, 13.1, and 14; R11.

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 28c.1 The map-centered workspace | R11 `PlayMapPanel` in the middle; the context header (game, view, turn and phase, revision, live or replay); actions by phase; the task pane; the activity strip; a pending proposal always reachable (section 13.1). | | 1:30 |
| 28c.2 Responsive layouts | The page families and breakpoints of section 11.1: 22b.2's navigation drawer checked at each breakpoint, workspace panes, and labeled Map, Action, and Activity views below 1024px; dynamic viewport height instead of the fixed chrome height. | | 1:00 |
| 28c.3 Hardening | Keyboard use, focus and drawers, 200% zoom and 320px reflow, the contrast targets, reduced motion, and reconnection with a pending proposal (section 14); the browser matrix at 1920x1080, 1366x768, 1024x768, and narrow widths (section 17.3). | | 1:30 |
| | Overhead | | 1:15 |
| | **Pass 28c total** (build 4:00) | | **5:15** |

**Built 2026-10-02**, all three tasks, with three reviews and two visual checks. The user chose the interactive BoardViewport for R11, beyond the recommended markup move. All four offered backlog rows are built: RuleHelp for the older panels, the ready notes, the RPh headings and "Weapon", and the hand-over on load with an outcome hand-over button. The user also took the recommended activity strip and the recommended reach of the hardening: Play fully, the other pages checked and fixed where they broke. R11 is extracted. Section 15.10 holds the decisions; items left out are in section 44 of the [ASL Unit Backlog](<ASL Unit Backlog.md>).

### Pass 29: The shared board workspace

**Added 2026-10-02** at the user's word, as the first task of the UI and playability work on branch `ui-improvements`. The board viewer's inspector (Selection, LOS, Evidence) is far richer than Play's picked-hex list and its collapsed LOS form, and the two drift apart as separate code. The pass extracts one map-and-inspector component from the board viewer and has Play host it, with the proposal review as an extra tab. Each task is checked in the Studio before the next; the unit tests, reviews, and merge gate run once the batch of UI work is done.

**Purpose:** one map workspace for every page that shows a board. **After:** pass 28c. **From:** the user's review of the board viewer against Play.

**Built 2026-10-03** (its [design](<ASL Unit Backlog Pass 29 Design.md>) and [review](<Scenario A1 Backlog Pass 29 Review 2026-10-03.md>)), with the UI batch from Claude Design's analysis and three reviews' fixes. Tasks 29.1 to 29.4 were built in one go at the user's word and checked in the Studio, then a review's fixes and the turned map from backlog section 44; 29.5 is built with the board viewer's game banner. The user chose the Proposal tab over a pinned review. Section 15.11 holds the decisions; the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>) the detail.

| Task | What it changes | Estimate |
|---|---|---|
| 29.1 `BoardInspector` | The board viewer's tabbed pane (Selection, LOS, Evidence) as a presentational component: the board, the selected and hovered hex, the selected unit, the units in the hex, the LOS draft and result, and extra tabs the host supplies (a tab and its markup). A move with no change in behavior. | 0:30 |
| 29.2 `BoardWorkspace` | `BoardViewport` and `BoardInspector` with the selection, hover, Escape, unit click, highlight, and LOS logic lifted out of the board viewer page. It takes a board, a unit overlay already read for the view, an optional marks layer, and a toolbar slot, and binds the selection and the open tab. The board viewer becomes a host: its route, its board load, and its study controls in the toolbar. | 0:50 |
| 29.3 The game's overlay | `GameMaps.Layers` returns the projected overlay with its SVG, so the Selection tab reads only what the view sees; the marks layer (the Covered Arc, Residual FP) and the extra tabs reach the workspace. "?" counters show as "?" only. | 0:40 |
| 29.4 Play on the workspace | Play hosts `BoardWorkspace` with a Proposal tab holding the existing review: chosen and marked when a proposal arrives, and reached from the header's Review button. `PlayMapPanel`, `PlaySelectedHex`, and the LOS form under the map retire. The hand-over unmounts the workspace and clears the selection, the LOS line, and the proposal. The page tests that read the old ids follow. | 1:00 |
| 29.5 The selection in the board link | `Games.BoardLink` carries the picked hex (`hex=`), so the selection survives "View on". | 0:15 |
| | Overhead | 1:15 |
| | **Pass 29 total** (build 3:15) | **4:30** |

### Pass 30: Prepared setups

**Added 2026-10-03** at the user's word, as the next pass before DYO. A scenario card gains up to three prepared setups, called setup plans, for the side that sets up first, made offline with AI from the card, its boards' terrain and LOS, its Victory Conditions, and its SSRs, then reviewed by the user. When a game starts from the card, that side may choose a setup, adjust it on the map, or set up by hand as before. A setup places the card's fixed OB; it never changes the OB.

**Purpose:** setting up from a card without retyping every counter. **After:** pass 29. **From:** the user's request of 2026-10-02.

**Built 2026-10-03** on branch `feature/asl-backlog-pass-30`: tasks 30.1 to 30.6, ten setup plans on the four built-in cards, three reviews, and the Studio check of the whole pass; see the [pass 30 design](<ASL Unit Backlog Pass 30 Design.md>) and the [review document](<Scenario A1 Backlog Pass 30 Review 2026-10-03.md>). What it leaves out is in backlog section 46.

| Task | What it changes | Estimate |
|---|---|---|
| 30.1 The setup list from the card | "Set up this group" fills the setup list with one row per counter the group still needs: its counter, a generated id, its group, and its Location when the area is one hex; an entering group goes off board in one click, with its entry area chosen when there are several. The gate's checks are unchanged. | 1:00 |
| 30.2 Picking setup hexes on the map | "Pick on the map" beside a row's Location, as in the card editor: the next click on the map fills it, and the hex is outlined. The hexes that may take a non-OB "?" are outlined, and a click ticks or unticks one. | 1:00 |
| 30.3 Setup plans as data | A file beside the card (`<card id>.setups.json`, embedded for built-in cards), keyed by the card's id and hash, so adding plans never changes the card or its saved games. Each plan has a name, its idea, what it gives up, a few terrain facts, and its placements. A plan for an earlier revision of the card is offered marked as such. | 0:45 |
| 30.4 Choosing a plan at setup | In the first side's view only (ruling R23.3), the setup offers that side's plans; one chosen fills the setup list, to adjust on the map and propose. The gate checks it like any setup, so a stale or illegal plan is refused with reasons. | 0:45 |
| 30.5 Plans for the built-in cards | Up to three plans for the first side of each built-in card, made offline, each grounded in the Studio's LOS and the card's Victory Conditions, and reviewed by the user before it is kept. | 1:00 |
| | Overhead | 1:15 |
| 30.6 The workflow rebuilt | Added 2026-10-03 after Claude Design's review of the first build (the pass 30 design, sections 10 and 11): setup as a mode of the actions pane (a bar, and Plans, Counters, and Card OB tabs), a plan shown on the map as draft counters without filling the list, the list grouped by stack, refusals linked to their rows, a side's own Dummies drawn, and the reload and hand-over bugs fixed. | 4:00 |
| | **Pass 30 total** (build 8:30, revised 2026-10-03) | **9:45** |

### Pass 30b: Setup plans for the side that sets up second

**Added 2026-10-03** at the user's word, after pass 30 was merged. A card gains setup plans for the side that sets up second: one response for each plan of the first side, and one for any setup. The second side is never told which plan the first side used; the game does not store it. Its plans are ordered by how closely each first-side plan matches the stacks it can see, computed from its own view. The workflow follows Claude Design's report as checked in the [pass 30b design](<ASL Unit Backlog Pass 30b Design.md>), section 4.

**Purpose:** the second side sets up from a card without placing every stack by hand. **After:** pass 30. **From:** the user's request of 2026-10-03.

**Built 2026-10-03** on branch `feature/asl-backlog-pass-30b`: tasks 30b.1 to 30b.8, sixteen setup plans made or remade on the four built-in cards, three reviews, and the Studio check of the whole pass; see the [pass 30b design](<ASL Unit Backlog Pass 30b Design.md>) and the [review document](<Scenario A1 Backlog Pass 30b Review 2026-10-03.md>). What it leaves out is in backlog section 47.

| Task | What it changes | Estimate |
|---|---|---|
| 30b.1 Plans for any side that is setting up | Plans are offered to the side whose groups are setting up now; the file's cap is per side; the "no setup plans" line and the new-game form's count speak of the right side. | 0:30 |
| 30b.2 The format `asl-setup-plans/2` | A plan's setup order, `answers` (the plan answered and a hash of its placements), the caps, `/1` files still read, and "Made for an earlier version of" a plan. The card's text and hash are untouched. | 0:45 |
| 30b.3 The Plans tab in groups | The second side's plans grouped under the first-side plan each answers, with that plan's public text, and "For any setup" last. | 0:45 |
| 30b.4 The comparison | Each first-side plan's footprint, as the other side would see it, compared with the stacks in the second side's view; the score, the sort, and the help line. It reads the view only. | 1:00 |
| 30b.5 The answered plan on the map | The answered plan's footprint drawn under the enemy's stacks, with a toggle; the zoom takes in the enemy stacks. | 1:00 |
| 30b.6 The words | The bar, the review, and the first side's note that setup plans are public. | 0:30 |
| 30b.7 Plans for the built-in cards | Up to thirteen plans for the second side of the four built-in cards, one card at a time, each accepted by the gate in the Studio and reviewed by the user. | 2:00 |
| 30b.8 Hidden and concealed setup | Added 2026-10-03 at the user's condition (the design's section 13): the non-OB "?" for every stack that may take one in one step, what an unchanged plan gives away told to the side that uses it, "Swap" for two stacks of the list, and each plan using every "?" and hidden setup its card allows. | 1:00 |
| | Overhead | 1:30 |
| | **Pass 30b total** (build 7:30, revised 2026-10-03) | **9:00** |

### Pass 31: Play-test UI

**Added 2026-10-04** at the user's word. On 2026-10-03 and 2026-10-04 Claude in Chrome played a whole game of The Guards Counterattack through the Play page (`guards-dl-01`, revisions 56 to 613) and reported 31 problems and 8 possible rules errors. The pass makes the page fit to play such a game: no legal action is blocked or lost, a proposal says what it will do, the game says when it ends and who won, and the page holds still and speaks in the player's words. It works from four sources: the report, its screenshots, the game's event log, and the gate's audit log. See the [pass 31 design](<ASL Unit Backlog Pass 31 Design.md>).

**Purpose:** a game played to its end through the page without a blocked or lost action. **After:** pass 30b. **From:** the play test's report of 2026-10-04; backlog sections 44 and 45.

**Status:** designed 2026-10-04; waits for the user's approval and answers (the design's section 12). Its first question is whether it runs as two passes, 31 (part A) and 31b (part B).

| Task | What it changes | Estimate |
|---|---|---|
| 31.1 Who may act | A proposal carries the side that proposes it and the planner checks it; the pickers list the viewing side's units; the phase end is offered to the view that may end it; a waiting choice comes first. | 1:30 |
| 31.2 A move that fire stops | The DEFENDER's pass for a moving stack with no member left, and a move ended by the fire that eliminates it, so the Movement Phase no longer locks. | 0:30 |
| 31.3 The last turn and the result | "Turn N of M" and "last turn" in the header; the game's result in every view once it has ended. | 0:45 |
| 31.4 Consequences | A proposal lists what it will eliminate, capture, waste, or end, apart from its checks, with a warning heading and a Confirm that says so. | 1:00 |
| 31.5 Fire corrections | The Defensive First Fire MF limit counted for this stack, Location, and phase; fire at a Location with two leaders; the Effect column showing what changed; a created unit's conditions; a pinned firer's SW. | 1:45 |
| 31.6 Advance and Close Combat | The Advance list by building level, with concealed units; prisoners out of the Close Combat form, a stacked SMC ticked with its MMC, a text for each refusal, an ordered capture list. | 1:30 |
| 31.7 The Rally Phase | Ralliers and Dismantle offered only where they can act; a text for each Self-Rally refusal; the Self-Rally allowance checked. | 0:45 |
| 31.8 The referee's check | The possible rules errors R-01 and R-03 to R-08 checked against the rulebook; the confirmed ones fixed under rulings. | 1:00 |
| 31.9 Names and words | Every unit named by its printed values, kind, and a fixed tag; modifier names in words; reasons without codes; Locations in words. | 2:30 |
| 31.10 Picking on the map | Every typed Location takes the hex picked on the map; routes and paths by clicks; the fire panel's From and Target. | 1:15 |
| 31.11 The fire proposal | FP, column, and DRM before the dice; the headline with its weapons; rate of fire kept; fire groups across ADJACENT Locations. | 2:15 |
| 31.12 A workspace that holds still | No window scroll at 1024 pixels and wider; a header of fixed height; short windows; fixed places for Propose and Confirm. | 2:00 |
| 31.13 The map | The view kept across a hand-over and a phase change; a loading line; the Residual FP marker off the counter; a Melee mark. | 0:45 |
| 31.14 Records and the latest line | The side in a record's heading; every event in "Latest", with a fire's result; "Since you last looked"; Records over the strip. | 1:00 |
| 31.15 Fewer hand-overs | A pass in one step; the DEFENDER's standing pass for the Movement Phase. | 1:30 |
| 31.16 Rout speed | The rout search kept for a unit at a revision; a "Working" sign. | 1:15 |
| 31.17 The second play test | The Guards Counterattack played again through the page; each problem tried again by the report's steps. | 1:30 |
| | Overhead | 2:00 |
| | **Pass 31 total** (build 22:45; part A 8:45, part B 14:00) | **24:45** |

### Pass 32: DYO purchase I: Infantry, leaders, and SW

**Deferred 2026-10-02** at the user's word: the DYO purchase is a whole new feature of ASL that can wait; the work turns to the Studio's UI and playability. The pass stays planned as written, and its backlog rows stay where they are.

**Purpose:** Chapter H makes a card. **After:** pass 28 (a DYO card opens in the editor's forms). **From:** backlog section 27.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 32.1 The DYO setup and roster | A DYO page: German and Russian (user answer, 2026-09-30), the date, boards, and the points each side spends; the Roster, with the counter limits and purchase mechanics. | H1.1 to H1.14 | 0:45 |
| 32.2 Infantry purchase | Squads and crews by BPV and MPV, with Assault Engineers, Sappers, Commandos, and MOL capabilities where the game plays them; the ELR Chart and SAN. | H1.2 to H1.29 | 0:50 |
| 32.3 Bonus Infantry and leaders | The second Infantry purchase and bonus Infantry; leader quality and the Leader Exchange DR. | H1.7 to H1.74, H1.8 to H1.82 | 0:40 |
| 32.4 SW allotment and the card | SW allotted by ratio; the purchase written as a user card (sides, ELR, SAN, OB groups) and opened in the editor. | H1.83, H1.84 | 0:45 |
| 32.5 The DYO page from components | Built from S02, S04, S06, S07, and K02 from the start; no raw form markup to extract later. | | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 32 total** (build 3:15) | | **4:30** |

### Pass 33: DYO purchase II: ordnance, vehicles, and conditions

**Deferred 2026-10-02** at the user's word: the DYO purchase is a whole new feature of ASL that can wait; the work turns to the Studio's UI and playability. The pass stays planned as written, and its backlog rows stay where they are.

**Purpose:** the DYO purchase covers what the game plays beyond Infantry. **After:** pass 32. **From:** backlog section 27. Its purchase forms reuse pass 32's components.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 33.1 Ordnance and vehicles | Guns and vehicles by BPV with the Availability DR and RF; optional armament and Armor Leaders where the game plays them. | H1.3, H1.4 to H1.43 | 1:00 |
| 33.2 DYO conditions | The DYO Weather, EC, and NVR tables written to the card's SSR tokens. | E1.11, E3 | 0:40 |
| 33.3 What is not played | OBA, Air Support, fortifications, boats, gliders, horses, and OP tanks refused with their backlog rows. | H1.44 to H1.6 | 0:20 |
| 33.4 End to end | A DYO game bought, saved, started, and played to its end by the table player. | | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 33 total** (build 2:30) | | **3:45** |

## 6. Duration report

The game passes keep the Scenario Card Games Plan's basis: 1:15 of overhead per pass (reading and rulings, the two reviews and their fixes, the documents, the full local suite, and the merge gate). The Studio passes and the component tasks use the same overhead. Their builds assume about 8 to 10 minutes per extracted component, including its tests, resets, and disclosure checks, with Play panels taking the most: most extractions move markup behind a narrow contract without changing behavior, and recent passes have run at about 65 % of their estimates. P1 candidates are the default and a pass may leave one inline with a reason; P2 and P3 candidates are extracted only when needed (section 16.17). The largest single risks to the estimates are 22d.1 (the viewport) and 27.5 (Close Combat and its disclosure). The game passes were first estimated at 30:00 (33 tasks, build 20:00); their component tasks and the tests before them (6:40), and the backlog survey's additions (tasks 25.7, 26.6, and 26.7, and the wider 23.3 and 28.4, 2:45), bring them to 39:25 in 43 tasks. Studio passes total 30:25 with pass 29 (added 2026-10-02); all passes 75:35 with pass 30 (added 2026-10-03), about 10.1 working days of 7.5 hours. Actuals have run well under plan (pass 20 took 2:17 against 3:00, pass 21 2:22 against 3:45, pass 22 2:18 against 3:30); at about 65 % of the estimate the whole would be nearer 49:08. The estimates are not cut for that, because passes 22d, 26, 28c, 30, 32, and 33 reach into areas the recent passes did not.

## 7. Risks

- **Pass 23** changes what every Play panel shows; the page tests read the adjudicator's view and may need a side's view throughout. Its component task (23.5) makes the side's view an explicit input from the start.
- **Pass 22d's board viewport** is the riskiest extraction: the page and a component must never both install listeners on one element, and reconnection, route changes, repeated mounting, and disposal all need tests (section 15.5).
- **Pass 26** depends on vehicle movement from off board, which the ordnance and vehicle passes did not build; if Motion and Passengers at entry need more of Chapter D, the pass grows.
- **Pass 32** needs the catalog's BPV for every purchasable counter; counters the catalog lacks are manufactured (R0.3), which may add catalog versions and package re-pins as pass 17 did.
- **Test hooks.** The 156 MapStudio tests address the pages' element ids and data attributes; every extraction keeps them (section 17.1), adding focused component tests rather than rewriting page tests.
- **Disclosure.** A component must never receive hidden data and hide it with CSS (section 15.4); the review of each Play extraction checks the DOM and accessible text in both a side's and the adjudicator's view.

## 8. Left out

These stay in the backlog: Fire spread and Kindling (so Control forfeited to a Kindled Fire, and EC and wind as an SSR the game reads), Sewer Movement and foxholes, Battlefield Integrity, the Factory and Fanaticism SSRs, OBA and Air Support, fortifications, the national rules of A25 for nationalities without counters (those the display batches add are built by batch D4 and passes 37 to 42), and Chapters F and G (the desert and the Pacific; their legacy cards stay display only). Fire, Sewer Movement and foxholes, the Factory, OBA and Air Support, fortifications, and more built-in cards are planned in Part VI.

For the Studio, what section 2 places outside the redesign stays out; narrow-screen access and keyboard operation are in it (pass 28c). P2 and P3 candidates a pass leaves inline are recorded in its review, not in the backlog, unless a later pass needs them. A survey of the backlog on 2026-09-30 found 83 rows the passes already build and moved 34 more into passes 23 to 42 and batch D1; on approval they were listed with their planned passes in section 33 of the backlog. The Board library's SVG previews (section 12.1) are not in these passes; they are a row in section 33 of the backlog.

# Part III. The Studio design

The sections below carry over the redesign's design in full, renumbered. Where they say "proposed", the pass named in section 16.17 builds it.

## 9. Audit findings translated into decisions

| Audit finding | Design response | CSS alone? |
|---|---|---|
| Flat visual hierarchy | Shared tokens, page headers, labeled primary actions, consistent controls | Partly; actions need markup |
| Ungrouped navigation | Collapsible grouped navigation with current destination and breadcrumbs | No |
| Long selects and sentence-flow forms | Searchable categorized pickers, field groups, separate help text | No |
| Library hard to browse | Search, status/type filters, SVG previews, explicit result states | No |
| Dense viewer toolbar | Separate view, layers, game/replay, and inspection controls | No |
| Unbounded page layout | Collection width limits and viewport-sized workspaces | Partly |
| Weak feedback | Explicit loading, empty, failed, busy, disabled, and completed states | No |
| Internal references in primary copy | Plain task descriptions with expandable technical evidence | No |

The audit's reported 236 library rows, approximately 190 SSR rules, and 35 unit kinds are observations of its session, not fixed application limits or acceptance fixtures. Current code already supplies some colored statuses and loading messages. Make them consistent rather than assuming none exist.

## 10. Application shell and navigation

Use a compact top bar for application identity and a collapsible left navigation area. Put the page title, breadcrumb, short description, and page-level action in the content header. Avoid placing object-editing tools in global navigation.

| Group | Destinations and existing routes | Page-level actions |
|---|---|---|
| Boards | Board library `/`; viewer `/boards/{BoardName}`; editor `/author/{BoardName}` | New board `/author/new`, Edit |
| Maps | Maps `/maps` | New map, Preview, Save |
| Units | Unit Lab `/units/lab` | Existing unit and placement save/export actions |
| Play | Live play `/games/play`; Game states `/units/games` | New game (from a card), Open game |
| Scenarios | Scenario cards `/units/scenarios`; Card editor `/units/cards/edit` | New card, Edit this card, Save, Delete |
| Verify | Fidelity `/fidelity` | Run verification |
| Settings | Settings `/settings` | Existing configuration tasks |

Scenario cards is currently a top-level link between Game states and Play; grouping it with the card editor under Scenarios keeps the card workflow together while New game stays under Play. Game states remains distinct from live play and retains synthetic/live source labels. Do not rename it to History, because it also exposes fixtures and sources. Board viewer breadcrumbs reflect the opened object: a composed map belongs under Maps even though it shares a viewer route.

Use `NavLink` or equivalent route-aware state, including an exact root match and explicit parent-group selection for nested routes. Expose `aria-current="page"` on the current destination. A collapsed sidebar retains accessible destination names; narrow layouts use a labeled navigation button and drawer. Settings stays reachable in all modes.

Keep route changes separate from sidebar expansion. Collapsing navigation must not discard a draft, reset a viewport, or navigate away.

## 11. Layout and visual system

### 11.1 Page families

Collection pages use a centered content area with a proposed maximum width of 90rem; explanatory text is limited to about 70 to 80 characters per line. Tables may use the full content width. Workspaces use available width and height with explicit scroll ownership: the canvas stays visible while tools and inspector contents scroll independently.

Use a shell grid or flex layout based on actual header size, with `min-height: 0` and `min-width: 0` on shrinking children. Avoid a global `100vh - 5.5rem` assumption. Use dynamic viewport height where supported, with a fallback. Do not apply a content max-width to every `main` or every workspace.

Proposed responsive behavior, to validate against content:

| Available viewport width | Navigation | Workspace |
|---|---|---|
| At least 1440px | Expanded by default, collapsible | Tools, canvas, inspector; inspector initially about 22rem |
| 1024px to 1439px | Collapsed by default | Canvas plus one side pane; tools may open as a drawer |
| Below 1024px | Navigation drawer | Map and task/inspector views switch through labeled tabs |

Forms and library search reflow down to 320 CSS pixels. Wide data tables use a labeled horizontal scroll region. Keep the canvas usable rather than squeezing it between fixed-width sidebars. Browser zoom must trigger the same responsive behavior.

### 11.2 Palette and typography

Adopt the proposal's warm neutral background, white surfaces, dark header, and restrained olive accent as the initial direction. Keep magenta board selection, amber counter focus, and navy drawing gestures distinct from terrain. Interface tokens must not recolor terrain or counter SVG internals.

Use semantic tokens for text, muted text, surfaces, borders, accent, success, warning, error, and information. Warning banners retain warning meaning; the existing source-derived draft restriction must not become an informational blue banner simply because it shares `.banner`.

The applied theme uses locally bundled IBM Plex Sans/Mono and Barlow Condensed, distributed with their SIL Open Font License notices. System fonts remain fallbacks. The supplied Google Fonts import is replaced with local @font-face declarations; there is no runtime font service dependency. Use condensed uppercase type sparingly for page identity, not dense form labels or instructions.

### 11.3 Controls

Use one visually dominant action per active task. Play can offer multiple action families, but only the active form's Review action or pending proposal's Confirm action is dominant. Avoid promoting every submit button globally.

Buttons use minimum height and allow meaningful labels to wrap when space requires it. Disabled controls remain legible and have a visible nearby reason when the reason is actionable. Preserve focus indicators on links, buttons, inputs, tabs, and canvas controls, including disabled link-style button overrides.

Statuses combine words and an optional symbol with color. Distinguish Not checked, Checking, Verified, Failed, Out of scope, and Authored valid. Never present approximate vectorization as exact or interpret missing verification as failure.

### 11.4 Required stylesheet ownership

On branch `UI-Redesign-01`, which pass 22b.1 merges, all Map Studio CSS lives in `wwwroot/site.css`, including tokens, base elements, shared controls, page layouts, component classes, responsive rules, and interaction states. The application loads it through `Assets["site.css"]`.

Do not use HTML/SVG `style` attributes, embedded `<style>` elements, JavaScript `.style` writes or CSS injection, Blazor CSS isolation, or `.razor.css` files. Components express presentation state through semantic classes and data attributes; the corresponding CSS is defined in site.css. Scope page/component rules with explicit root classes rather than Blazor-generated selectors.

On that branch the existing app.css has been migrated in full, so there is no second stylesheet or duplicate cascade to maintain. Although retaining app.css for basics was permitted, it is unnecessary for this foundation. The supplied redesign app.css now provides the theme rules and values in site.css. Its :root block and following rules are preserved, with local font declarations before them and the viewport state rules after them.

SVG geometry and renderer-owned presentation attributes such as viewBox, coordinates, fill, stroke, and clip-path remain part of the existing deterministic vector output. They are not CSS declarations or style attributes. Dynamic Studio layer visibility and comparison opacity are controlled by data attributes whose rules live in site.css, including the comparison slider's 0 through 100 percent values.

Acceptance includes checking rendered DOM after interactions, not just searching Razor markup: the previous viewport JavaScript created style attributes even though Razor had none.

### 11.5 Applied theme values

The following values are implemented on that branch (merged by 22b.1) in [site.css](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/800416a/src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/site.css). They describe the current baseline, while sections 11.1 and 11.3 describe further layout and control refinements. All root custom properties are listed here; site.css remains the canonical source for individual selector declarations.

| Token | Applied value |
|---|---|
| `--ink` | `#1b2025` |
| `--muted` | `#5e656c` |
| `--line` | `#dad5c8` |
| `--panel` | `#e6e2d8` |
| `--accent` | `#4a6629` |
| `--pass` | `#23592f` |
| `--fail` | `#8e2e1f` |
| `--ground` | `#f3f1ea` |
| `--surface` | `#ffffff` |
| `--surface-2` | `#faf9f5` |
| `--line-soft` | `#efece4` |
| `--control-line` | `#cfc9ba` |
| `--command` | `#1e2419` |
| `--command-ink` | `#e8e6dd` |
| `--accent-ink` | `#3f5a22` |
| `--accent-tint` | `#dce5c4` |
| `--pass-bg` | `#ddeedf` |
| `--fail-bg` | `#f6e1dc` |
| `--warn` | `#7a4f0b` |
| `--warn-bg` | `#f6ecd6` |
| `--info` | `#244f77` |
| `--info-bg` | `#dce7f2` |
| `--radius` | `6px` |
| `--font-sans` | `"IBM Plex Sans", "Segoe UI", system-ui, sans-serif` |
| `--font-mono` | `"IBM Plex Mono", ui-monospace, Consolas, monospace` |
| `--font-display` | `"Barlow Condensed", "Segoe UI", sans-serif` |
| `--chrome` | `5.5rem` |

| Treatment | Applied value |
|---|---|
| Base typography | 15px; line-height 1.45; IBM Plex Sans |
| Main page headings | Barlow Condensed, 1.6rem, weight 600, uppercase, line-height 1.2 |
| Header | 3.5rem height; dark command surface; title 1.35rem, weight 700 |
| Main spacing | 1rem 1.25rem padding |
| Buttons | 2.1rem height; .9rem text, weight 500 |
| Text controls | 2.1rem minimum height; .9rem text |
| Tables | .875rem text; .55rem .9rem cell padding; .7rem uppercase headings |
| Focus indicator | 2px solid accent outline; 2px offset |
| Viewer | minmax(0, 1fr) 22rem columns; calc(100vh - var(--chrome)) height |
| Editor | 11rem tool rail, flexible canvas, 22rem inspector |
| Unit Lab | minmax(320px, 420px) 1fr columns; 1.25rem gap |
| Viewport comparison | Data attributes select 101 opacity values, 0 through 100 percent, in site.css |

Since the 2026-09-30 rebase, site.css also carries main's scenario card rules (`.scenario-card`, in the theme's `--line`, `--warn`, and `--muted` tokens) and the card editor's two-column grid (`.card-editor`, with `--font-mono` for its JSON areas). Neither uses a style attribute.

Local font weights are IBM Plex Sans 400/500/600, IBM Plex Mono 400/500, and Barlow Condensed 600/700, each with font-display: swap. [Font sources and licenses](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/800416a/src/ASL/LimboDancer.Domains.Asl.MapStudio/wwwroot/fonts/README.md) record the seven bundled files and redistribution notices.

The supplied theme is an applied visual baseline, not completion of the redesign. It still uses fixed chrome and pane sizes, lacks responsive breakpoints and general Play form layouts, and includes active/primary selectors that need corresponding markup. The generic .banner is currently informational blue; a dedicated warning variant is still required for source-derived draft restrictions. Unit Lab's wide starting-template select also needs a layout correction: it extends into the preview column at the inspected desktop width. These remaining refinements belong to passes 22b to 28c (section 4).


## 12. Collection and authoring screens

### 12.1 Board library

Keep authored boards and VASL boards discoverable through type filters. Preserve the existing Maps section as a compact link or summary leading to the Maps collection; avoid two competing map-management interfaces.

Place search and filters above results: board name/reference, source type, verification status, and out-of-scope inclusion. Show the result count and active filters together, with Clear filters. Search typed text locally where the existing catalog supports it; do not ingest every board on each keystroke. Preserve filters and scroll position when returning from a viewer.

Default to a compact list; small SVG previews are optional and wait in the backlog (section 8). A preview uses the existing renderer and generated terrain, never VASL board artwork. Load visible previews on demand; cache by board identity/version and rendering options. Missing or failed previews show a labeled placeholder without making the board disappear. Do not trigger full fidelity verification to display a thumbnail.

Separate loading, no configured source, no authored boards, no matching results, and catalog failure. While loading, show a progress message instead of zero boards. Retain the full diagnostic detail through an expandable row or details panel.

### 12.2 Maps

Use a map collection followed by a composition workspace. The workspace has an ordered placement list, canvas preview, and selected placement/rule properties. Board pickers search reference and display name. Keep rotation, row, and column editable numerically; pointer rearrangement is optional later work, not a prerequisite.

SSR selection supports search and categories based on actual catalog metadata. Preserve the exact selected rule identifiers and their order. Explain scope and show unsupported or diagnostic results without inventing rule behavior.

Move compact placement syntax into an Advanced section, retaining full edit and round-trip functionality. Editing text or placements marks the preview stale until rebuilding finishes. An unsuccessful rebuild preserves the last valid preview, clearly labeled as out of date. Save must retain existing validation behavior.

**Pass 22c:** the composer has no drawn preview of an unsaved map, so the last Check result stands for it: after an edit it stays, labeled out of date, with its findings hidden, until the next check. A drawn preview is backlog section 35. The board and rule pickers stay native selects (section 14); the board picker shows each board's title.

### 12.3 Board viewer and editor

The viewer toolbar exposes view mode and Fit directly. Put layer toggles in a labeled Layers panel, and game source, perspective, revision, and counter style in a separate game/replay strip. Keep the current perspective and revision visible even when its controls collapse.

Inspector tabs are Selection, LOS, and Evidence. LOS is a first-class tab rather than a buried disclosure. Show source, target, result, and terrain contributions together; retain manual Location entry and selected-hex shortcuts. Evidence contains provenance, F1/F2 results, and diagnostic details.

The editor adds a tool rail and a document header with name, dirty state, Undo, Redo, and Save/Save draft. Preserve all tool options, layers, validation, gestures, keyboard shortcuts, and comparison modes. A warning on source-derived drafts remains visible whenever it constrains saving. Confirm navigation away from an unsaved draft using existing dirty-state information.

### 12.4 Unit Lab

Group fields into identity, combat/movement characteristics, capabilities, appearance, and placement. Hide irrelevant fields only when the underlying unit kind makes them inapplicable; changing kind must not silently destroy edited values without a clear reset policy.

Use a searchable kind picker and keep the preview adjacent to the active fields. Preserve side-by-side far/mid/near and classic/digital comparisons. Place raw JSON and style-sheet editing in Advanced sections without removing their existing functions. Validation points to the relevant group and field.

## 13. Play workspace

### 13.1 Persistent context

Promote the existing map from below the action forms into the central workspace. Keep game, side/perspective, turn, phase, and revision visible. Show live versus historical state explicitly. Keep the current NVR, weather, precipitation and illumination summary visible when relevant, supplied by the authoritative state adapter. For a game from a card, keep the card's turn count, the game-end notice and result, and the Victory Conditions standing reachable from the context header; they currently sit in the collapsible card panel above the page. The selected unit or stack drives the task pane, but manual unit and Location entry remain available.

```text
+-----------------------------------------------------------------------+
| Game | View as | Turn / phase | Revision | Replay / live indicator      |
+----------+-----------------------------------+------------------------+
| Actions  |                                   | Selected unit / stack  |
| by phase |            Board / map            | Active task fields     |
|          |                                   | Review action          |
+----------+-----------------------------------+------------------------+
| Activity: latest result, rolls, changes; expandable history            |
+-----------------------------------------------------------------------+
```

This is a layout sketch, not a promise that all controls fit at all widths. On narrow screens, labeled Map, Action, and Activity views share the context header. A pending proposal is always reachable from that header.

### 13.2 Setup

**Revised 2026-09-30.** Every game starts from a scenario card (ruling R22.4): the boards, sides, ELR, SAN, Friendly Board Edges, date, Scenario Defender, and SSR tokens come from the card, and are edited in the card editor, not on the Play page. New-game setup therefore has three sections: the card and the game's id and label; the start the card gives (a read-only summary, the first-move note, and the Balance choice); and unit placement, led by the OB setup table that shows each group's order, status, remaining counters, "?" left, and setup areas. A minimal card shows a one-line note instead of an empty OB table. Preserve Sniper and Dummy placement, the OB group choice, and off-board placement for entering groups. Put rule explanations in help text beneath fields, with expandable citations. Do not force experienced users through a multi-page wizard; allow reviewing the complete setup before proposal.

The side, date, and SSR fields the audit described now live in the card editor (section 16.16), where the same guidance applies: two coherent side groups, a structured SSR picker that round-trips tokens, and field-level validation.

### 13.3 Phase and action organization

| Context | Task groups, reflecting currently implemented capabilities |
|---|---|
| Rally | Rally, repair, Deploy/Recombine, eligible support-weapon handling, recovery choices |
| Movement | Infantry/vehicle movement, SMOKE, movement costs, DC placement, berserk retained SW, defender response window, overrun/reaction |
| Fire phases | Small arms including FT/MOL, thrown/placed DCs, Starshell attempts, ordnance, targets and modifiers |
| Rout | Rout requirement, destination/cover, route, Low Crawl, Interdiction and surrender results |
| Advance | Unit selection, destination, movement implications |
| Close Combat | Location, participants, Ambush/withdrawal, attacks/capture, prisoner escape round, infiltration, Ambush Withdrawal, Hand-to-Hand option, vehicle sequence |

This table organizes tasks; it is not a new eligibility specification. Existing planners and phase rules remain authoritative. An action chooser can show an unavailable action with a public reason, but must not infer permission or disclose hidden state. Keep End phase separate from an active unit task so it cannot be confused with completing that task.

### 13.4 Review and confirmation

Selecting a unit, clicking a target, or editing a field only prepares an action. The UI's Review action invokes the existing proposal path. A successful proposal opens a persistent review pane with action, participants, declared destination/target, disclosed consequences, costs, and current revision. Confirm invokes the existing confirmation path. Cancel dismisses the proposal without changing game state.

Show refused, unsupported, indeterminate, stale, already committed, and committed results distinctly, preserving their actual service meaning. Never translate an unknown result into success. While planning, show a busy state and prevent duplicate submissions. Do not promise a completion percentage when none exists.

Changing action arguments, game, perspective, or revision invalidates the displayed proposal for confirmation. A stale result requires a fresh proposal and review; it must not silently confirm a revised plan. Retain existing attempt/idempotency behavior for repeated confirmation and retries. On reconnect, refresh authoritative state and reconcile any pending attempt before enabling confirmation.

The review pane must not reveal undisclosed defenders or consequences that the existing flow withholds until confirmation. Any unit search, tooltip, target list, activity entry, and accessibility label must use the appropriate projection. Clear selection and cached presentation data when perspective changes. Terrain-only LOS retains its existing availability.

### 13.5 Activity and evidence

Keep a compact latest-result summary visible. Expand activity into grouped records for movement, fire, ordnance, Close Combat, Rally/Rout/support weapons, Sniper attacks, night/weather events, and dice. Retain Fire Lane MG attribution and other explanatory facts added by recent commits. Rule citations are relevant evidence, not clutter to remove indiscriminately. Technical package identities and internal design references belong in a separate evidence disclosure.

### 13.6 Scenario card provenance

**Added 2026-09-30 at the user's request.** A scenario card's full provenance is visible wherever the card is shown, so a player or reviewer can trace every fact on it back to its source without reading JSON or the repository. The same panel serves the Scenarios page and Play's card panel; on Play it also shows what the game started from and whether the card still matches.

| Layer | What the panel shows | Where the data is |
|---|---|---|
| Card | Id; built-in (embedded) or the user's (its file under the boards folder); SHA-256 of its text; format; the catalog it pins | The card and the card library |
| Game | The card id, SHA-256, and catalog the game recorded at its start; whether the current card matches, changed, or is gone | `GameStarted` and the library |
| Source | The basis, the legacy card cited, and the adaptation notes | The card's `source` |
| Counters | For each counter line, the catalog source record, its status, transcriber, and reviewer; each printed value's sheet, counter, face, and row; values manufactured under R0.3 on sheet MFG marked as such | The catalog's `CatalogSource`, `CounterReference`, and `ValueSource` |
| Citations | Each rule a card cites: for a registered fragment, its rulebook PDF page, fragment id, and the pass 17 comparison result; for a ruling, its id with a link to its row | The pass 17 comparison registry (`asl-scenario-a1.pass17-pdf-comparison.json`), embedded in the Play library; the rulings of the Backlog Passes Plan, section 5 |

The panel is read only and collapsed by default beneath the card; the counter layer expands per line, since a card can field fifty counters. Unknown or unregistered provenance is shown as such, never as blank. The registry is read once by the Play library and passed to the component as a prepared model (section 15.2); rulings are shown by id and link, since their text lives in a Markdown table.

## 14. Interaction and accessibility acceptance targets

- Every workflow can be reached and completed with a keyboard. Canvas actions have equivalent labeled controls or Location inputs.
- Searchable pickers support typing, arrow navigation, selection, Escape, and an announced result count; retain native selects until a replacement supports these behaviors.
- Tabs expose selected state and predictable keyboard navigation. Opening a drawer moves focus appropriately; closing restores focus to its opener. Persistent desktop panes do not trap focus.
- Loading and action completion are announced without repeatedly reading large logs. Validation summaries link to fields; urgent errors remain visible until addressed.
- Use proposed contrast targets of 4.5:1 for normal text and 3:1 for large text and meaningful control boundaries/focus indicators, verified during implementation rather than assumed from token names.
- Target at least 32px control height in dense desktop toolbars and 44px for primary touch interactions. Treat these as design targets, not a claim of accessibility certification.
- Avoid mandatory animation, respect reduced-motion preferences, and keep hover-only content accessible by focus or explicit activation.
- At 200% zoom, no primary action or confirmation is clipped. At 320 CSS pixels, forms reflow; canvas and wide tables have explicit viewing/scrolling controls.

## 15. Blazor component architecture

Keep `MainLayout.razor` responsible for the shell. Introduce small shared components for page headers, status badges, field/help/error groups, searchable pickers, empty/loading states, workbench panes, and proposal review. Define all styles in site.css, using explicit page and component root classes where behavior differs. Do not create component stylesheets or inline styles.

Extract Play presentation in small steps: context header, action groups, review pane, and activity. Keep the existing services and action arguments intact. Do not reimplement eligibility in visual components or couple the renderer to Blazor.

Before extracting a form, inventory its bindings, conditional controls, action identifiers, test selectors, and diagnostic outputs. Preserve stable IDs where practical. Component tests should assert behavior rather than the old markup tree. Reuse `boardViewport.js` interactions where appropriate, checking mounting and disposal when panels or routes change. An expanded Play canvas must use the same perspective-safe drawing source as the existing Play view.

Separate ephemeral UI state (pane widths, filters, active inspector tab) from game state. Pane preferences may persist locally; hidden unit data and proposal authority must not. Coalesce or cancel obsolete search/preview requests and discard responses for a superseded selection or perspective.

### 15.1 Reuse existing components

The application already has useful component boundaries:

| Existing component | Decision |
|---|---|
| `Diagnostics` | Retain its map-diagnostic grouping adapter. Improve keyboard-accessible detail disclosure; a title tooltip alone is insufficient. Compose shared FindingList/StatusBadge where useful rather than replacing its domain contract with object. |
| `ToolPalette` | Retain. It already has a tool value and ToolChanged callback. Restyle within this boundary. |
| `LayerList` | Retain. It represents authored features in paint order, not renderer layer visibility. Do not merge it with BoardLayerToggles. |
| `ValidationPanel` | Retain its report and finding-selection contract; share findings presentation underneath. |
| `HexInspector` | Retain as a task-level component. Extract grid evidence or feature lists only when tabs/reuse need them. The small Locations and Hexsides tables can remain inline initially. |
| `FeatureProperties` | Retain. Its replacement-feature callback is a good domain boundary. Share TerrainPicker; do not split every conditional property input. |
| `UnitEditor` | Retain as the public vocabulary-driven editor. VocabularyAttributeInput is the strongest internal extraction; face/identity/state children are subsequent choices. |

Do not add wrappers around App's document structure, Routes' router, a single heading, every status-bearing span, every table cell, or an empty toolbar. The empty toolbar at Play:1238-1239 (at `2fa3f56`; Play:1499-1500 on main) is a cleanup candidate, not a component. CSS classes are sufficient for repeated visual treatment that has no behavioral or semantic contract.

### 15.2 Page/container responsibilities

Keep route/query handling, dependency injection for application services, data loading, projection, action proposal/confirmation, persistence, and navigation in the page or a dedicated page-level coordinator. Extracting a panel must not grant it authority to mutate a live game or call a planner from its markup.

Children should receive narrow task-specific data, not a reference to Play, a broad cascading page object, or the entire unrestricted GameState. A form may receive an editable local draft and permitted choices, but its action callback returns declared intent. The page invokes the same existing action handler or adapter.

Use coherent drafts such as RoutDraft, MovementDraft, FireDraft, and CloseCombatDraft. These are proposed UI data structures. Do not turn them into a second rules engine. When a draft is passed by reference during an incremental extraction, document its ownership and notify changes explicitly. Prefer value replacement for new contracts so parameter updates and invalidation are visible.

For example, RoutActionPanel should receive a RoutDraft, prepared obligations, authorized unit choices, and busy state. It raises DraftChanged and ReviewRequested. It does not receive LivePlay, calculate a mandatory route, or confirm a proposal.

ProposalReviewPanel receives a presentation model that has already removed withheld facts, plus confirm availability. ConfirmRequested and CancelRequested identify the displayed proposal attempt. The page checks that the attempt still matches the current proposal before executing the existing confirmation path.

### 15.3 Draft reset rules are part of extraction

Existing code contains important dependent changes and reset helpers. Preserve or deliberately improve them with a focused test:

| Trigger | Coupled state to preserve |
|---|---|
| Game, phase or perspective change | Selection, eligible choices, current proposal and any stale form values; re-run the appropriate reset/pruning path |
| Manual boards text changes | Retired 2026-09-30: the boards come from the card |
| Scenario card choice changes | Keep the card's hash as read; clear each placement's OB group the new card does not have; refill the start summary; clear the Balance choice |
| Card editor: source card changes | Reload every field; a built-in card becomes a copy under a new id; reset the counter picker to the side's first counter |
| Card editor: a side's nationality changes | The Turn Record Chart's sets-up and moves-first sides and the Scenario Defender follow the change; the picker resets |
| Card editor: picker side changes | Select that side's first counter in the order shown, so Add adds what is shown |
| Card editor: save | The saved id becomes the edited card, so the next save replaces it; another user card's id is refused |
| Selected support weapon changes | Clear receiving/recovering unit |
| Rally unit changes | Clear rally leader |
| Deployment squad changes | Clear leader and SW assignment |
| First recombining HS changes | Clear partner and leader |
| Fire origin changes | Apply PruneFire; remove invalid firer, weapon and director selections |
| Ordnance weapon changes | Reset ammunition default, spotter and director |
| Ordnance target changes | Clear selected target vehicle |
| Close Combat Location changes | Clear attacks/round-related draft and restore default stacking as existing helpers require |
| Repeated attachment or placement removed | Preserve remaining objects' identity; do not transfer draft/focus to the next index |

Pass view context keys such as game, revision, perspective, Location, and stable object ID explicitly where state can survive rerenders. Use `@key` for attachment editors, placement rows, repeated Location panels, and event cards. Do not key on list index or regenerate keys on every render.

### 15.4 Projection and disclosure

Several existing Play selectors query full `state`; extracting them does not by itself prove they are safe for every perspective. Audit the projection boundary for each new contract. Unknown/withheld is not an empty list, false, or zero.

Specific hotspots in the source:

- Play:63 builds the board link with `Perspective.Adjudicator`, even though the toolbar has a selected perspective. Treat this as a separate explicit behavior decision and test it during the redesign; do not bury a perspective change in a component refactor. **2026-09-30:** still present on main. Pass 23 (per-side views with a hand-over screen) cannot hide a side's setup while this link opens the adjudicator view, so the decision is due before that pass. **Decided in pass 23 (ruling R23.3):** the link follows the page's view, and is left out while a hand-over waits.
- The Victory Conditions standing (`#play-victory`) shows Control to both sides, which A26.15 withholds for concealed and HIP units (backlog section 31); a `VictoryStandingTable` child must receive the side's view once pass 23 provides it.
- Play:246-250 (at `2fa3f56`) builds building-entry choices from active state units. Other task lists also consult state and planners. Pass only the disclosure appropriate to the current workflow; preserve adjudication authority on the server.
- Required choice, surrender, audit, fire-effects and sealed-presence blocks have different disclosure rules. A generic card must not receive their secret data and merely hide it with CSS.
- Changing perspective must clear unauthorized details from child state, caches, tooltips, and accessible names. A reused component instance can otherwise retain content that its new parameters no longer authorize.

These are review targets based on source shape, not a claim that an exploit or runtime leak was demonstrated.

### 15.5 Viewport ownership

BoardViewport is a high-value but higher-risk extraction. BoardViewer and Author currently own the host ElementReference, JS module/viewport references, DotNetObjectReference, event callbacks, and disposal. Move that lifecycle as one unit or leave the host inline until it can move safely.

Use child-owned lifecycle with a narrow callback/API contract for Fit, Highlight, render updates, and gesture mode. Do not leave both page and component installing listeners on the same element. Verify reconnect, route changes, tab collapse, repeated mount/unmount, comparison mode, focus, and disposing the last JS/DotNet reference.

Play currently inserts rendered SVG directly. Converting that output to the interactive viewport changes behavior and requires separate verification. It is not implied by extracting PlayMapPanel.

### 15.6 Forms, DOM, and styling

Keep the existing binding event timing unless a task explicitly changes it. Several fields use oninput, while others use onchange. Replacing all of them with one generalized control can change preview timing and draft validation.

Do not impose EditForm on every panel or create nested forms. Native buttons, inputs, labels and selects remain useful. Introduce validation contracts deliberately rather than wrapping controls so deeply that IDs, focus, and errors become difficult to inspect.

Components used inside tables must render valid table elements. A row component emits tr; it does not put a div between tbody and tr. Preserve label/for pairs and unique IDs, especially attachment prefixes and repeated vehicle towing controls.

Apply the stylesheet ownership and rendered-DOM checks in section 11.4 to every extraction. Use explicit component/page root classes and data attributes across nested Razor components.

### 15.7 Suggested organization

Keep domain-related names close together without introducing a new project:

```text
Components/
  Layout/       StudioNavigation, PageHeader, InspectorTabs
  Shared/       OperationFeedback, StatusBadge, FindingList, FieldGroup, JsonDisclosure
  Board/        existing components, BoardViewport, viewer/editor controls, provenance
  Maps/         MapPlacementEditor, ScenarioRuleEditor, PlacementTextEditor
  Units/        UnitEditor and its children, Lab previews/style/placement panels
  Games/        PerspectivePicker, RevisionNavigator, replay tables, ReadCase panels
  Play/         context/setup, action families, proposal review, activity, map/table panels
  Fidelity/     runner controls, report picker/metadata/results, differences
  Pages/        route-aware composition and orchestration
```

These folders express ownership, not a requirement to create every directory immediately. Promote a component to Shared only after its semantics fit multiple domains. A generic SearchablePicker is a redesign addition, not an existing extracted HTML block in this inventory; use it later beneath domain pickers after keyboard and accessibility behavior is defined. Likewise, new thumbnails and a new phase-action chooser should not inflate the extraction count.

### 15.8 Extraction practice

- Dependency order comes before priority: a P1 candidate waits for the shared component or the pass it depends on.
- Prove a pattern on a few representative forms before applying it everywhere: in pass 22c the Maps placement and rule editors, in pass 25 the movement panels, then the other task families, keeping each family working before extracting the next.
- Establish the proposal review's narrow disclosed-data contract (R01 to R03, pass 23) before extracting the panels that feed it.
- Commit presentation extraction separately from changes to action semantics or services, so reviewers can tell them apart.
- Let the shell and one representative page per family settle spacing and pane behavior before restyling every form.
- A change to a design decision is recorded in section 3 or in the Part III section it belongs to, so later passes share the same rationale.

### 15.9 Decisions of pass 28b

**Added 2026-10-02.**

- **Rule help.** S08 `RuleHelp` shows a one-line summary and keeps the full paragraph, with its citations, in a closed disclosure that keeps the paragraph's id. The summary carries the id plus `-summary`, which describes the disclosure's label, and is what a field's `aria-describedby` names. A summary is new text and is checked against its paragraph and the rules, as a rule paragraph is.
- **Records.** A records list shows the latest attempt first, and an attempt's records in the order they happened, so a cause reads before its effect. A record names a unit to a side only when the side's view could name it just before the event.
- **Choices.** A panel's select options are `PlayChoice` values read for the view and passed through `SeenOnly`. A Propose button is ready only when its choice is one the panel offers. A value a panel hides for the current choice is never sent.
- **Resets.** One page method holds the resets of the Rally, Rout, SW, Starshell, and ordnance panels for another game, phase, or view; the resets within a panel stay in its handlers on the page.
- **Shared wording.** Text formatting that several components and the page share lives in an internal static class beside them (`FireText`), not in a component.

### 15.10 Decisions of pass 28c

**Added 2026-10-02.**

- **Breakpoints.** The workspace follows section 11.1:
  - From 1440px: actions, map, and review side by side over the activity strip.
  - From 1024px to 1439px: the map and one side pane, with the navigation starting collapsed.
  - Under 1024px: labelled Map, Actions, and Activity tabs under the review.

  Breakpoints that the script also reads are in pixels. The narrow layout holds in CSS before the script reports the width; only the tabs need the script.
- **Context.** The context header stays in sight above the workspace, which sizes itself from the header's measured height. Under 1024px the header scrolls away, and its Review and hand-over buttons stay in a bar fixed to the window's foot. Everything in it is known to both sides, so it shows during a hand-over; its announcement goes with the view.
- **Whose turn.** The page names the view the game waits for:
  - a choice's side;
  - a surrender's captors;
  - the DEFENDER after a step or in the DFPh;
  - the other side in the RPh, RtPh, and CCPh;
  - otherwise the phasing side.

  It offers the hand-over to that view, or says "You act". A page opened on a game, or another game chosen, waits behind the hand-over.
- **The map.** R11 `PlayMapPanel` owns B06 `BoardViewport` and receives layers already read for the view (`GameMaps.Layers`). It loads the board when the board changes and sends a layer only when its text changes. The page caches the layers by game, revision, view, picked hex, LOS line, and Gun. A click picks a hex for the review pane's list; it does not fill the action drafts.
- **Confirmation.** One gate request at a time; Confirm and Cancel wait while it runs. A proposal records its revision, and a Confirm at another revision is shown as Stale and never sent.
- **The DEFENDER's pass.** Only the DEFENDER passes, from its own view beside its fire panel. Its review reads only that it passed.
- **Theme.** The control border token meets 3:1 against the surfaces. Shared reflow rules keep every page within 320 CSS pixels.

### 15.11 Decisions of pass 29

**Added 2026-10-02.**

- **One workspace.** `BoardWorkspace` (Components/Board) owns B06 `BoardViewport` and `BoardInspector` and the map's interaction: click, hover, Escape, a counter click, the highlight, the pixel sample, and the LOS check. The board viewer and Play host it; each gives the board, the units already read for the view, and its own toolbar, and binds the hex kept and the open tab.
- **Siblings, not a wrapper.** The stage and the inspector are sibling elements whose classes and attributes the host names, so Play keeps its pass 28c grid, breakpoints, and pane ids. Play puts the inspector first and its narrow tabs and actions between the inspector and the map.
- **The proposal is a tab.** On Play the inspector's first tab is Proposal, holding the existing review. A new proposal chooses the tab, marks it "Proposal (1)", and takes the focus there; the header's Review button does the same. The user chose this over a review pinned above the tabs.
- **The view's overlay.** `GameMaps.Layers` returns the unit overlay and the view beside the SVG, so the Selection tab reads what the view may see and nothing more (section 15.4). Terrain facts, Evidence, and LOS are terrain only and serve every view.
- **The workspace draws the picked hex and the LOS line.** The page's map cache keys only on the game, revision, view, and Gun.
- **Retired.** `PlayMapPanel`, `PlaySelectedHex`, and Play's LOS form under the map.
- **The turned map** (backlog section 44, the user's row). "Rotate map" turns the whole map a quarter clockwise as one, counters included, by a CSS transform; the board's coordinates and the pointer mapping are unchanged. The browser remembers the choice for each shape of pane, tall or wide, and Fit on a turned map fills the pane's width.
- **Narrow windows.** Under 1024px the inspector sits below the map in the Map tab; a proposal opens the Map tab on its Proposal tab.
- **The Proposal tab holds** while a proposal waits for Confirm, and is counted only then. The game and view pickers are disabled while the gate works, and the workspace is keyed by the game and the view.

# Part IV. The component inventory

## 16. Component extraction inventory

### 16.1 Findings and source baseline

The existing pages contain **171 active candidate component boundaries** after the 2026-09-30 review (155 on 2026-09-29, less 5 retired, plus 21 new; section 16.16). They include shared components, page-specific task panels, and optional nested children. This is a broad inventory for selection, not a target of 171 new Razor files. A parent and its listed children are separate possible boundaries within overlapping source, not independent chunks to extract twice.

The review covers all ten routed pages, MainLayout, the application/router shells, and the seven existing content components. It examines Razor markup, bindings, callbacks, relevant state-reset and viewport lifecycle code, and existing component-test selectors. This is source analysis, not a fresh browser usability or performance measurement.

Sections 16.3 through 16.14 retain their source references at `2fa3f564056049d222f722e4b5bc925d51c3abd3`, the original implementation baseline of this redesign branch; since the 2026-09-30 rebase they are immutable GitHub links to that commit, so they stay accurate while the local files move on. Section 16.15 pins main at `ab32b53` and section 16.16 pins main at `c13e9b7`; both add contract amendments that supersede the older descriptions where noted. Every source link is an immutable GitHub link, so line numbers stay correct for their commit while the files move on.

Original branch baseline measurements:

| Page | Total source lines | Lines before @code |
|---|---:|---:|
| Author | 591 | 152 |
| BoardViewer | 759 | 309 |
| Fidelity | 309 | 217 |
| Games | 342 | 179 |
| Home | 215 | 150 |
| Maps | 321 | 123 |
| NewBoard | 159 | 61 |
| Play | 3,755 | 1,532 |
| Settings | 47 | 31 |
| UnitLab | 478 | 227 |
| **Total** | **6,976** | **2,981** |

The pre-`@code` count includes directives, whitespace and Razor control flow, not just HTML. Play accounts for roughly 54% of page source and 51% of these markup-region lines. Its state and service coupling matter more than line count: moving markup alone will not resolve that coupling.

The best extraction units are complete tasks or representations: a rout form, an ordered placement editor, a proposal review, a counter preview matrix, or a provenance panel. Shared controls should follow actual duplicated behavior, not visual similarity alone.

### 16.2 Priorities and contracts

- **P1: strong candidate.** A coherent task, meaningful duplicated behavior, accessibility boundary, or lifecycle boundary. Priority is benefit, not a promise that the change is easy.
- **P2: useful second pass.** Extract with its parent or when the redesign independently places/reuses the block.
- **P3: conditional.** A sensible possible boundary, but leave inline unless reuse or complexity justifies it.

The active inventory contains **114 P1**, **53 P2**, and **4 P3** candidates (2026-09-29: 102, 49, and 4). Retired candidates keep their rows, marked retired, so their IDs are not reused. Names are proposed `.razor` filenames, not existing classes. Contracts are suggested inputs and outputs, not claims that these view models already exist. Callback names describe intent; use typed `EventCallback<T>` contracts during implementation.

Each table gives source location, proposed contract, and the reason for extraction. Linked ranges are source evidence, not code changes. A single line or small range listed for a shared primitive is one concrete usage, not a recommendation to make every similarly shaped element a component.

### 16.3 Shared layout and interaction blocks

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| S01 / P1 | `StudioNavigation` | [Layout/MainLayout:3-14](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Layout/MainLayout.razor#L3) | Destination definitions, current route, collapsed state; Navigate/Collapse callbacks. | Own grouped navigation, active-route behavior, and keyboard access in one place. |
| S02 / P1 | `PageHeader` | [Maps:10-15](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L10); [Games:11-15](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L11); [UnitLab:12-16](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L12); [Play:19-23](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L19) | Title, description and action slots; no service access. | Repeated title/intro blocks become a consistent heading hierarchy without embedding page logic. |
| S03 / P1 | `SourceRequiredNotice` | [Home:10-18](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L10); [Maps:17-20](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L17); [NewBoard:10-13](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L10) | Missing prerequisites, explanation, settings link, optional setup instructions. | Unify configuration failures while retaining page-specific requirements. |
| S04 / P1 | `OperationFeedback` | [Maps:106-121](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L106); [NewBoard:53-59](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L53); [UnitLab:131-137](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L131); [Author:46-49](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L46) | Busy state, severity, message and optional diagnostics; retry callback if offered. | Consistent busy, failure and completion announcements; never infer success from an empty message. |
| S05 / P1 | `StatusBadge` | [Home:118-121](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L118); [BoardViewer:27-28](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L27); [Fidelity:170-175](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L170) | Explicit label, tone, optional explanation and symbol; no callbacks. | Reuse presentation while each caller maps its domain statuses, including unknown and not checked. |
| S06 / P1 | `FindingList` | [UnitLab:111-129](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L111); [Games:53-62](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L53); [Maps:118-121](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L118); [Board/ValidationPanel:21-31](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Board/ValidationPanel.razor#L21) | Normalized display items with category/code/message/severity, optional selection callback. | Share accessible findings presentation; keep map, unit, plausibility and style diagnostic adapters separate. |
| S07 / P1 | `FieldGroup` | [Play:100-123](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L100); [Maps:46-49](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L46); [NewBoard:16-46](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L16) | Label, input ID, help/error text and child control slot. | Keep label, help and error associations together; preserve native input binding and validation. |
| S08 / P2 | `RuleHelp` | [Play:263-270](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L263); [Play:303-309](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L303); [Play:605-614](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L605) | Short help, detailed explanation, citations; expansion state only. | Long rule paragraphs can move out of the task flow without losing actionable explanations. |
| S09 / P1 | `PerspectivePicker` | [Games:35-43](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L35); [BoardViewer:113-118](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L113); [Play:54-62](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L54) | Allowed perspective choices, value, ID; ValueChanged. | A genuinely repeated domain control. Parent performs reprojection and proposal invalidation. |
| S10 / P1 | `RevisionNavigator` | [Games:44-46](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L44); [BoardViewer:119-121](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L119) | Current/min/max revision, busy state, optional slider; RevisionChanged. | Share boundary handling and accessible previous/next controls while parents own history loading. |
| S11 / P2 | `LocationField` | [Play:200-201](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L200); [Play:541-543](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L541); [BoardViewer:222-228](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L222); [Games:137-138](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L137) | Raw text, label, examples, error, optional selected-location action; TextChanged/UseSelection. | A reusable canonical Location entry, distinct from the optional holder field or bypass-route syntax beside it. |
| S12 / P2 | `UnitSelectionList` | [Play:537-540](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L537); [Play:738-741](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L738); [Play:1005-1008](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1005); [Play:1032-1039](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1032) | Projected choice rows, selected IDs, per-row disabled reason, ID/class hooks; SelectionChanged. | Repeated checkbox lists gain consistent labels and selection behavior. Caller supplies eligibility. |
| S13 / P2 | `FacingPicker` | [Units/UnitEditor:22-32](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L22); [Units/UnitEditor:48-58](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L48); [Play:205-213](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L205); [Play:1220-1228](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1220) | Hexspine choices, nullable value and null label, control ID; ValueChanged. | Share facing controls without conflating hexspines with the different Hexside vocabulary. |
| S14 / P2 | `JsonDisclosure` | [UnitLab:138-141](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L138); [Play:1524-1529](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1524) | Title and already-authorized text; expansion state. | Share expandable code presentation. Adjudicator authorization stays outside this generic component. |

### 16.4 Board library

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| H01 / P1 | `AuthoredBoardList` | [Home:21-45](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L21) | Authored listing rows and create/edit/view links. | Independent collection with an empty state; keeps draft labels together with board actions. |
| H02 / P1 | `MapSummaryList` | [Home:47-69](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L47); [Maps:23-43](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L23) | Map summaries, optional edit/delete callbacks, view links. | Reuse the overlapping map/name/placement listing, exposing actions only where the caller supports them. |
| H03 / P1 | `BoardScopeSummary` | [Home:71-81](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L71); [Fidelity:103-111](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L103) | Explicit scope/outcome counts and optional not-checked count. | Shared totals display, with no hidden assumption that library and batch totals have identical categories. |
| H04 / P2 | `LibraryReportContext` | [Home:82-92](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L82) | Last fresh report timestamp, out-of-scope count/value; IncludeOutOfScopeChanged. | Own the report explanation and existing filter before adding search. |
| H05 / P1 | `VaslBoardTable` | [Home:93-148](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L93) | Resolved display rows, view links; optional row-selection callback. | Move the five conditional result paths out of route markup; resolve cached versus fresh report data upstream. |
| H06 / P2 | `BoardVerificationRow` | [Home:107-145](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Home.razor#L107) | One normalized board result with F1/F2, scope reason and diagnostics. | Complex repeated row merits its own tests. Render a tr, not a div inside tbody; extract with H05 if useful. |

### 16.5 Map composition and new-board forms

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| M01 / P1 | `MapPlacementEditor` | [Maps:51-75](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L51) | Placement drafts, board choices, bounds; Add/Change/Remove callbacks. | Own editable placement table and add action; page retains map building and persistence. |
| M02 / P2 | `MapPlacementRow` | [Maps:58-71](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L58) | Stable row key, selected board/column/row/reversal; RowChanged/Remove. | Repeated multi-field row has identity and validation. Key by a stable draft ID, not row index. |
| M03 / P1 | `ScenarioRuleEditor` | [Maps:77-99](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L77) | Ordered selected rules, catalog choices and descriptions; Add/Remove callbacks. | One coherent rule-selection task that can gain search while preserving rule order. |
| M04 / P1 | `PlacementTextEditor` | [Maps:101-104](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L101) | Draft text, help and parse feedback; TextChanged/ApplyText. | Separates advanced compact syntax from ordinary placement editing and preserves apply semantics. |
| M05 / P2 | `MapBuildActions` | [Maps:106-121](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Maps.razor#L106) | Working state, result/diagnostics; Check/SaveAndView. | A page-specific command group composed with OperationFeedback, not a new map service. |
| M06 / P1 | `NewBoardForm` | [NewBoard:16-59](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L16) | Creation draft, board choices, busy/result state; DraftChanged/Create. | A complete creation task with conditional blank/vectorized modes; page owns slug validation and creation initially. |
| M07 / P3 | `BoardDimensionsFields` | [NewBoard:29-35](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L29) | Width/height and their distinct bounds; DimensionsChanged. | Optional child if dimensions become reusable or per-field validation grows; otherwise keep in NewBoardForm. |
| M08 / P1 | `SourceDraftNotice` | [NewBoard:48-51](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/NewBoard.razor#L48); [Author:21-27](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L21) | Context-specific draft restriction text and evidence links. | Shared warning semantics; never downgrade the authoring restriction to an informational notice. |

### 16.6 Board viewer and editor

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| B01 / P1 | `BoardViewerToolbar` | [BoardViewer:26-137](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L26) | Title/status, supported views, navigation links, toolbar child slots. | A composition boundary for the existing crowded toolbar, not one component with all game/editor state. |
| B02 / P1 | `BoardViewPicker` | [BoardViewer:33-47](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L33); [Author:36-43](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L36) | Supported BoardView choices and value; ViewChanged. | Shared capability-aware view selection; editor does not automatically gain Comparison. |
| B03 / P1 | `BoardComparisonControls` | [BoardViewer:56-71](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L56) | Mode, opacity/divider value; ModeChanged/ValueChanged. | Separate tightly coupled comparison mode and slider from unrelated controls. |
| B04 / P1 | `BoardLayerToggles` | [BoardViewer:72-85](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L72) | Available layers, visibility set, optional derivation trace; LayerChanged/TraceChanged. | Group rendering toggles. This differs from existing LayerList, which selects authored features. |
| B05 / P1 | `UnitOverlayControls` | [BoardViewer:86-130](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L86) | Show state, game/set choices, sheet names, projected perspective/revision; typed change callbacks. | Keep synthetic placement sets and games distinct while composing shared perspective/revision controls. |
| B06 / P1 | `BoardViewport` | [BoardViewer:139-140](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L139); [Author:108-111](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L108) | Render source/options and interaction mode; HexHovered/HexSelected/UnitSelected/GestureCompleted; limited Fit/Highlight commands. | Own ElementReference, JS initialization, callbacks and disposal together. HTML-only extraction would leave lifecycle split. |
| B07 / P1 | `SelectedUnitInspector` | [BoardViewer:143-198](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L143) | Projected unit display details, allowed game facts and source summary. | Unit identity and game facts form a complete inspector; never accept unrestricted state for convenience. |
| B08 / P2 | `UnitGameFacts` | [BoardViewer:154-193](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L154) | Projected unit/equipment/presence facts with explicit withheld state. | Optional child of SelectedUnitInspector; centralizes custody, containment and withheld rendering. |
| B09 / P1 | `HexUnitStackList` | [BoardViewer:205-214](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L205) | Hex label and projected stack members with drawn/not-drawn flag; SelectUnit. | Repeated selectable stack entries belong together; preserve the beyond-six explanation. |
| B10 / P1 | `LosPanel` | [BoardViewer:218-236](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L218); [Play:1468-1489](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1468) | Source/target draft, busy/result; Check/Clear; optional selected-hex and visible-unit slots. | Substantial overlap, with capability-specific children for auxiliary vertex and Play unit selection. |
| B11 / P2 | `LosResult` | [BoardViewer:232-235](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L232); [Play:1485-1488](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1485) | Explicit answered/blocked/refused status, summary and optional details. | Share result semantics; clear LOS and unanswered LOS must never look identical. |
| B12 / P1 | `BoardProvenancePanel` | [BoardViewer:258-306](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L258) | Read-only provenance, composition, fidelity, validation and renderer metadata. | Independent evidence panel suitable for a tab without exposing the entire BoardViewer. |
| B13 / P1 | `BoardEditorHeader` | [Author:28-50](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L28) | Name/reference/version/status, dirty/undo/redo state; Rename/Undo/Redo/Save; view controls slot. | Cohesive document commands and save feedback, separate from drawing tools. |
| B14 / P1 | `EditorToolOptions` | [Author:55-106](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L55) | Tool-specific option draft and catalog choices; OptionsChanged. | Move the terrain/elevation/linear/building/annotation switch into one focused component, not five tiny controls. |
| B15 / P1 | `InspectorTabs` | [Author:115-147](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L115) | Tab IDs/titles/counts, selected ID and panel fragments; SelectedChanged. | Own tab behavior and focus while retaining existing FeatureProperties, LayerList, ValidationPanel and HexInspector. |

### 16.7 Unit Lab

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| U01 / P1 | `UnitTemplatePicker` | [UnitLab:20-50](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L20) | Synthetic examples, catalog definitions grouped by publication, selection; SelectedChanged. | Keep the grouped picker and catalog provenance notice together, preserving catalog-versus-Lab semantics. |
| U02 / P1 | `AttachedEquipmentEditor` | [UnitLab:54-67](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L54) | Attachment drafts, vocabulary and stable IDs; Add/Remove/AttachmentChanged. | Composes existing UnitEditor; fixes ownership and identity for repeated attachment forms. |
| U03 / P1 | `CounterTierPreviewGrid` | [UnitLab:71-88](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L71) | Labeled rows of trusted rendered SVG for far/mid/near; refusal state. | Independent comparison view; rendering service and source validation remain outside. |
| U04 / P2 | `CounterFacePreviewGallery` | [UnitLab:90-96](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L90) | Sheet label and labeled face/perspective SVG previews. | Distinct axis from detail-tier comparison; reuse a small SVG preview tile only if needed. |
| U05 / P2 | `UnitAccessibleDetails` | [UnitLab:98-108](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L98); [BoardViewer:145-153](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/BoardViewer.razor#L145) | Accessible name, detail pairs and optional Location. | Reuse read-only unit labeling presentation, preserving exact accessible-name content. |
| U06 / P1 | `UnitLabFindings` | [UnitLab:111-129](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L111) | Validation, plausibility and style findings; no callbacks unless navigation added. | Domain adapter around FindingList, preserving categories and explicit No findings. |
| U07 / P2 | `UnitDocumentOutput` | [UnitLab:131-141](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L131) | Can-save state, JSON and save feedback; Save. | Document output is a complete task. Composes JsonDisclosure and OperationFeedback. |
| U08 / P1 | `UnitStyleSheetEditor` | [UnitLab:143-179](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L143) | Sheet/palette choices, raw text, parsed status, diagnostics and save-name draft; Select/Edit/SaveAs. | A substantial independent editor; preserve raw invalid text and existing oninput parsing timing. |
| U09 / P1 | `UnitPlacementSetEditor` | [UnitLab:181-224](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L181) | Board/hex/level/set draft, placed units, save result; Place/Load/Save/Remove. | A complete synthetic placement workflow; must never call live-game writes. |
| U10 / P2 | `PlacedUnitList` | [UnitLab:209-220](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/UnitLab.razor#L209) | Projected display rows with stable IDs/Location/name; Remove. | Optional child reusable in read-only previews; do not confuse synthetic rows with live setup commands. |

### 16.8 Game states and case reading

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| G01 / P1 | `GameReplayToolbar` | [Games:23-49](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L23) | Game choices, perspective/revision state and board target; ChangeGame/ShowOnBoard. | Compose shared PerspectivePicker and RevisionNavigator, with parent-owned replay loading. |
| G02 / P2 | `GameReadFindings` | [Games:51-63](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L51) | Diagnostics and positions-checked evidence. | Read-only result adapter; successful position validation is evidence, not just another warning. |
| G03 / P1 | `GameContextSummary` | [Games:67-71](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L67); [Play:175-179](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L175) | Revision, turn, phase, phasing side, perspective, synthetic/setup labels. | Shared context strip with explicit optional fields; do not pretend Play supports revision scrubbing here. |
| G04 / P1 | `ProjectedGameUnitTable` | [Games:73-102](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L73) | Visible unit and sealed-presence row models, trusted preview SVG. | Own display-only table semantics; do not merge with Play's different movement/action table prematurely. |
| G05 / P2 | `GameEquipmentTable` | [Games:104-120](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L104) | Projected equipment/entity rows and formatted custody/Location. | Independent non-unit inventory with a different schema from personnel. |
| G06 / P1 | `ReadCaseForm` | [Games:122-140](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L122) | Projected attacker choices, Location and expected revision draft; Read. | Separate request preparation from result inspection; parent owns ReadCase service call. |
| G07 / P1 | `ReadCaseResult` | [Games:141-168](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L141) | Already-authorized status/reason and optional snapshot display model. | Cohesive evidence table, including incomplete occupancy and nondefinitive results. |
| G08 / P2 | `PerspectiveEventList` | [Games:170-176](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Games.razor#L170) | Only entitled event rows and selected revision. | Safe, read-only timeline with revision numbering; no raw event-log injection. |

### 16.9 Fidelity and Settings

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| F01 / P1 | `FidelityRunControls` | [Fidelity:14-42](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L14) | Availability, IncludeF3, runner state/progress/error; Start/Cancel/IncludeF3Changed. | Own the batch lifecycle display; page retains runner subscription and disposal initially. |
| F02 / P1 | `LosFidelityPanel` | [Fidelity:44-75](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L44) | Fixture availability/count, running/progress and results; Run. | Separate LOS verification job from board fidelity without duplicating service orchestration. |
| F03 / P2 | `LosFidelityResultsTable` | [Fidelity:55-70](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L55) | Fixture result rows with unsupported counts and disagreements. | Optional child of LosFidelityPanel for independent sorting/details later. |
| F04 / P1 | `FidelityReportPicker` | [Fidelity:77-99](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L77) | Saved report summaries, selection and download URL; SelectedChanged. | One task with empty state and download, not a generic select wrapper. |
| F05 / P1 | `FidelityReportMetadata` | [Fidelity:112-131](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L112) | Timestamps, source IDs, versions, timings and diagnostics. | Evidence presentation independent of job controls and filtering. |
| F06 / P1 | `FidelityResultsTable` | [Fidelity:133-215](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L133) | Board result rows and filter; FilterChanged. | Own filter/table composition. Service lookups and report selection stay above it. |
| F07 / P2 | `FidelityDifferences` | [Fidelity:172-194](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L172) | F2 status, differences, diagnostics, display limit and report link. | Extract the nested disclosure, preserving truncation and the full-report escape path. |
| F08 / P2 | `FidelityCheckBadges` | [Fidelity:195-200](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Fidelity.razor#L195) | Named checks with passed/gating/detail fields. | Differentiate failed gating checks from informational F3 results; compose accessible badges. |
| F09 / P3 | `StudioConfigurationReport` | [Settings:10-30](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Settings.razor#L10) | Resolved paths, catalog/version metadata and precomputed cache size. | Low urgency on a small read-only page; filesystem enumeration should not become a child's render-time job. |

### 16.10 Play: context and setup

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| P01 / P1 | `ScriptedDicePanel` | [Play:25-39](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L25) | Queue values, input/error and enabled flag; Enqueue/Clear. | Isolate development-only controls and their conspicuous test-data warning. |
| P02 / P1 | `LiveGameToolbar` | [Play:41-65](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L41) | Game choices, selected game, permitted perspectives and board URL; GameChanged/PerspectiveChanged. | Shared context boundary; explicitly review the existing adjudicator board link rather than silently preserving or changing it. |
| P03 / retired (was P1) | ~~`NewGameForm`~~ | [Play:67-156](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L67) | New-game draft and choice catalogs; DraftChanged. | One setup root with the following field-group children; parent retains proposal construction. |
| P04 / retired (was P2) | ~~`GameMapChoice`~~ | [Play:73-85](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L73) | Board placement text, saved-map choices and selection; MapChoiceChanged. | Enforce mutual exclusivity in one place: typing board text clears saved-map selection. |
| P05 / retired (was P1) | ~~`ScenarioSidesFields`~~ | [Play:86-123](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L86) | Two side drafts, ELR and edge choices; SidesChanged. | Reorganize interleaved side labels into two coherent groups without losing null/default distinctions. |
| P06 / retired (was P2) | ~~`ScenarioConditionsFields`~~ | [Play:124-154](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L124) | Month/year/defender draft and side choices; ConditionsChanged. | Scenario conditions and their help form a reusable setup section. |
| P07 / P1 | `GameReplayFailure` | [Play:157-172](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L157) | Filtered replay diagnostics and catalog-availability explanation. | Independent recovery/error block; preserves the five-item limit and missing-catalog explanation. |
| P08 / P1 | `SetupPlacementEditor` | [Play:184-224](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L184) | Definition choices, placement draft and applicable options; DraftChanged/Add. | Complete placement task with Gun/vehicle facing, Bore Sight, concealment and holder alternatives. |
| P09 / P1 | `SetupPlacementList` | [Play:225-234](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L225) | Pending setup placements and busy state; Remove/ReviewSetup. | Reviewable setup queue; stable placement keys and explicit proposal action. |

### 16.11 Play: Rally, Rout, movement and vehicles

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| A01 / P1 | `BuildingEntryAction` | [Play:243-259](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L243) | Authorized unit choices, destination and help; DraftChanged/ReviewEntry. | Small complete action; retain withheld-information explanation. |
| A02 / P1 | `RoutActionPanel` | [Play:260-299](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L260) | Rout obligations/advice, eligible choices and route/LowCrawl draft; ReviewRout. | Own coherent rout task; planners compute obligations upstream. |
| A03 / P2 | `RoutObligations` | [Play:271-279](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L271) | Precomputed unit/side/requirement/cover/routed rows. | Optional child separates advice from route editing and removes repeated planner work from markup. |
| A04 / P1 | `SupportWeaponActionPanel` | [Play:300-336](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L300) | Weapon/receiver choices, selection and action availability; ReviewTransfer/Drop/Recover/Dismantle. | Keep dependent weapon/receiver selection together; changing weapon resets receiver. |
| A05 / P1 | `RallyActionPanel` | [Play:339-368](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L339) | Broken-unit and leader choices, rally draft; DraftChanged/Review. | One action with self-rally option; changing unit resets leader. |
| A06 / P1 | `RepairActionPanel` | [Play:369-387](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L369) | Eligible SW/AAMG choices and selection; ReviewRepair. | Separate repair from rally while preserving the different weapon and vehicle cases. |
| A07 / P1 | `DeployActionPanel` | [Play:388-425](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L388) | Squad, leader and SW choices/draft; ReviewDeploy. | Selection dependencies and second-HS equipment assignment form one task. |
| A08 / P1 | `RecombineActionPanel` | [Play:426-458](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L426) | First HS, compatible partner and leaders; ReviewRecombine. | Keep resets of partner/leader with first-HS changes. |
| A09 / P2 | `DmRetentionChoices` | [Play:459-470](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L459) | Eligible projected units and selected IDs; SelectionChanged. | Phase-end choices, not a standalone committed action. Parent includes them in phase advancement. |
| A10 / P1 | `ShockRecoveryAction` | [Play:471-490](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L471) | Shocked/UK vehicle choices and selection; ReviewRecovery. | Independent required recovery step with clear phase-end consequence. |
| A11 / P1 | `MovementStatus` | [Play:500-519](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L500) | Movement window, movers, remaining members, bypass/minimum/end facts. | A persistent status strip shared by movement-related tasks; no service calls. |
| A12 / P2 | `BerserkChargeNotices` | [Play:520-535](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L520) | Authorized charge target/next-step/undecided advice. | Dedicated obligation list; distinguish undecided from a mandatory known route. |
| A13 / P1 | `InfantryMovementAction` | [Play:536-550](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L536) | Move draft, projected candidates, pushable-Gun facts; ReviewMove. | Cohesive movement fields; compose UnitSelectionList and LocationField. |
| A14 / P1 | `SmokeGrenadeAction` | [Play:551-565](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L551) | Eligible placers, target draft and move-selection context; ReviewSmoke. | Separate action currently embedded inside the movement toolbar; retain its moveUnits dependency. |
| A15 / P2 | `MovementWindowActions` | [Play:566-567](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L566) | Defender-window/end availability, selected ending members; ReviewPass/ReviewEndMove. | Two related phase-window commands whose labels and enabled states must remain synchronized. |
| A16 / P1 | `ReactionFireAction` | [Play:569-601](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L569) | Reaction Location/vehicle, authorized attackers/SMCs, draft; ReviewReaction. | A complete defender response, distinct from the ordinary Close Combat phase. |
| A17 / P1 | `VehicleMovementPanel` | [Play:602-713](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L602) | Selected vehicle, movement options/status and draft; typed review callbacks. | Large task root. Build the following children only as needed; do not pass the whole Play page. |
| A18 / P2 | `VehicleMovementStatus` | [Play:626-634](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L626); [Play:707-710](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L707) | VCA, MP spent/remaining, movement/bog/reverse/bypass/recall display facts. | Complex state explanation deserves independent presentation tests. |
| A19 / P2 | `VehicleStepChoices` | [Play:635-666](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L635); [Play:685-690](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L685) | Precomputed legal/blocked steps with costs/reasons, movement-option draft; StepRequested. | Group start/turn/entry/stop/exit/OVR/ESB choices; parent serializes existing action arguments. |
| A20 / P1 | `VehicleTowingActions` | [Play:667-684](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L667) | Towable Gun choices, current towing state and facing; ReviewHook/ReviewUnhook. | A distinct attachment operation; unique IDs for repeated unhook-facing controls. |
| A21 / P1 | `BoundingFireAction` | [Play:692-706](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L692) | Projected target choices, selected vehicle/target; ReviewBoundingFire. | Fire from vehicle movement is a separate task with its own eligibility context. |
| A22 / P1 | `CrewExposureActions` | [Play:715-729](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L715) | Eligible vehicles and current CE/BU state; ReviewExposureChange. | Independent action group shared across Movement and Advance contexts. |
| A23 / P1 | `AdvanceActionPanel` | [Play:730-754](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L730) | Advance candidates, selected IDs and destinations; ReviewAdvance. | Separate movement-phase stepping from Advance rules; share presentation controls only. |

### 16.12 Play: combat and required choices

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| C01 / P1 | `VehicleCloseCombatPanel` | [Play:755-810](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L755) | Per-Location projected participants, next side, selected attack draft; ReviewAttack/ReviewPass. | Use one keyed Location child for each repeated block; prevent selection leaking between Locations. |
| C02 / P1 | `CloseCombatPanel` | [Play:811-940](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L811) | Selected Location, due/status/round facts and CC draft; reviewed action callbacks. | Task root for the following independently meaningful blocks; include the pass 14 extensions in section 16.15. |
| C03 / P2 | `CloseCombatLocationControl` | [Play:822-860](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L822) | Due Locations, selected Location, Ambush/round status; LocationChanged/RoundChanged/ReviewAmbush. | Own Location changes and the explicit draft-reset boundary. |
| C04 / P1 | `CloseCombatStacking` | [Play:863-877](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L863) | Authorized SMC/MMC choices and stacking map; StackingChanged. | Repeated relational editor with unit IDs, separate from attack participant selection. |
| C05 / P1 | `CloseCombatWithdrawals` | [Play:878-895](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L878) | Authorized unit destinations and mandatory flags; WithdrawalsChanged. | Repeated per-unit choices; planner supplies destinations and must-withdraw facts. |
| C06 / P1 | `CloseCombatAttackBuilder` | [Play:900-930](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L900) | Projected sides/participants/directors and draft; SelectionChanged/AddAttack. | Own coordinated attacker/defender/director input; do not give each checkbox rule knowledge. |
| C07 / P2 | `CloseCombatAttackQueue` | [Play:931-937](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L931) | Declared attacks and review availability; Remove/ReviewRound. | A reviewable local queue, including explicit resolution with no attacks. |
| C08 / P1 | `PendingChoicePanel` | [Play:941-957](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L941) | Authorized choice description/options or waiting state; ReviewChoice. | Independent blocking interaction; unauthorized viewers must not receive hidden option data. |
| C09 / P1 | `PendingSurrenderPanel` | [Play:958-972](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L958) | Authorized surrender/Guard choices and waiting state; ReviewAccept/ReviewReject. | Repeated prompt keyed by surrender identity; retain No Quarter implications. |
| C10 / P2 | `PrisonerActionPanel` | [Play:973-982](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L973) | Permitted unit/prisoner action rows; ReviewAction. | Baseline block contains massacre choices; keep that contract distinct from N09 prisoner custody in section 16.15. |
| C11 / P2 | `FireMarkerSummary` | [Play:983-995](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L983); [Play:1444-1450](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1444) | Authorized Fire Lane, Encirclement and Residual FP summaries. | One map-context summary with meaningful categories, preserving MG/operator attribution. |
| C12 / P1 | `OpportunityFireAction` | [Play:996-1011](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L996) | Eligible units and selected IDs; ReviewOpportunityFire. | Distinct preparatory action, not another option on immediate fire. |
| C13 / P1 | `SmallArmsFirePanel` | [Play:1012-1100](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1012) | Fire draft plus precomputed firer/weapon/director/target options; Review/Clear. | Complete task; group selection dependencies belong together. |
| C14 / P2 | `FireGroupSelector` | [Play:1022-1053](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1022); [Play:1086-1097](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1086) | Location, unit/weapon/MG-alone/leader/partner choices; GroupChanged. | Optional child for the most interdependent fire-selection block; preserve PruneFire behavior. |
| C15 / P2 | `FireTargetOptions` | [Play:1054-1085](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1054) | Target/free-Location draft, phase options and selected weapons; TargetOptionsChanged. | Keeps Snap Shot, Spraying Fire and Fire Lane fields with their target semantics. |
| C16 / P1 | `OrdnanceFirePanel` | [Play:1101-1216](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1101) | Gun/MA/LATW choices, draft, target/ammunition/spotter/director options; ReviewFire. | Independent action family; preserve gun-change and target-change resets. |
| C17 / P2 | `OrdnanceTargetFields` | [Play:1161-1206](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1161) | Target and vehicle choices, ammunition capability/draft; TargetChanged/VehicleChanged/AmmoChanged. | Consolidate duplicated vehicle selects while retaining fixed AP/HEAT versus selectable ammunition. |
| C18 / P1 | `GunArcAction` | [Play:1217-1237](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1217) | Selected Gun, facing and precomputed target-arc readings; FacingChanged/ReviewTurn. | A separate non-firing action with evidence; parent keeps GunTargetStatus calls. |
| C19 / P1 | `OpenEntryDeclaration` | [Play:1316-1323](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1316) | Authorized pending entry attempts; ReviewElect/ReviewDecline. | Blocking Infantry OVR declaration is separate from general vehicle overrun controls. |

### 16.13 Play: review, activity and map

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| R01 / P1 | `ProposalReviewPanel` | [Play:1244-1312](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1244) | Already-disclosed proposal summary, reasons, optional evidence, can-confirm/busy state; Confirm/Cancel. | Highest-value extraction for consistent confirmation. Parent owns attempt ID, revision, withholding and execution. |
| R02 / P1 | `EntryReviewFacts` | [Play:1257-1272](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1257) | Disclosed tri-state facts and conclusion/provenance. | Independent evidence block; unknown stays distinct from false. |
| R03 / P1 | `FireReviewFacts` | [Play:1273-1305](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1273) | Authorized attack facts including range, LOS, levels, terrain and scenario month. | Preserves recent pass 10 details in a testable evidence table. |
| R04 / P1 | `DiceRollHistory` | [Play:1325-1344](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1325) | Entitled rolls/subjects and scripted-mode notice. | Distinct record type with roll IDs, provenance and test-data warning. |
| R05 / P1 | `ActionRecordList` | [Play:1346-1355](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1346); [Play:1421-1441](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1421) | Heading, list ID and already-formatted event ID/kind/text rows. | One reusable list for ordnance, Close Combat and rally records; do not build three identical components. |
| R06 / P1 | `FireHistory` | [Play:1357-1419](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1357) | Authorized fire records; no callbacks required initially. | Specialized records are richer than ActionRecordList; compose the following children. |
| R07 / P1 | `FireResolutionCard` | [Play:1363-1416](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1363) | One projected fire record with explicit withheld effects. | Natural repeated boundary keyed by event ID; keep withheld state explicit. |
| R08 / P2 | `FireArithmeticBreakdown` | [Play:1367-1379](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1367) | Authorized firer arithmetic, multipliers, column shifts, dice and DRM. | Independently verifiable explanation; presentation must not recalculate the adjudication. |
| R09 / P2 | `InfantryFireEffectsTable` | [Play:1380-1395](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1380) | Disclosed target effects, definition changes and checks. | A coherent nested table; null/withheld effects must not be treated as an empty success. |
| R10 / P2 | `VehicleFireEffectsTable` | [Play:1396-1411](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1396) | Disclosed vehicle-hit, vehicle-line and crew results. | Different data schema from Infantry effects warrants its own component. |
| R11 / P1 (built in pass 28c, on B06) | `PlayMapPanel` | [Play:1443-1466](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1443) | Perspective-safe drawing SVG/source/problems and current selection; LocationSelected if supported. | Own drawing success/error presentation. Reusing interactive BoardViewport is later behavior work, not a markup-only extraction. |
| R12 / P1 | `PlayUnitTable` | [Play:1491-1523](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1491) | Projected visible-unit/sealed rows, formatted costs/conditions; Locate. | Separate live movement-aware table; never pass full state just to compute a row label. |
| R13 / P2 | `AdjudicatorAuditPanel` | [Play:1524-1529](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1524) | Explicitly authorized audit lines; no service access in generic child. | Authorization wrapper around JsonDisclosure; hiding with CSS does not protect the audit. |

### 16.14 Further boundaries inside existing components

| ID / priority | Proposed component | Existing source block | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| E01 / P1 | `VocabularyAttributeInput` | [Units/UnitEditor:175-188](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L175) | Attribute definition, raw string value, ID; ValueChanged. | Extract the existing RenderFragment switch into a testable input component, preserving enum blanks and number/list parsing semantics. |
| E02 / P2 | `UnitIdentityFields` | [Units/UnitEditor:4-73](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L4) | Identity draft, vocabulary choices and attached/concealed capabilities; IdentityChanged. | Focused identity/orientation group; use FacingPicker while keeping Hexside distinct. |
| E03 / P2 | `UnitFaceEditor` | [Units/UnitEditor:82-105](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L82) | Face label, accepted attribute/trait definitions and values, stable prefix; ValuesChanged/TraitsChanged. | Repeated face fieldset is a natural child, keyed by face. |
| E04 / P2 | `UnitAttributeFields` | [Units/UnitEditor:86-93](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L86); [Units/UnitEditor:111-119](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L111) | Ordered accepted definitions and value access/change adapter. | Reuse face/unit attribute layout around VocabularyAttributeInput; scope stays explicit. |
| E05 / P3 | `UnitStateChecks` | [Units/UnitEditor:122-134](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Units/UnitEditor.razor#L122) | State definitions, selected names and prefix; SelectionChanged. | Optional semantic group for independent state validation; otherwise leave in UnitEditor. |
| E06 / P2 | `HexGridSamples` | [Board/HexInspector:53-72](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Board/HexInspector.razor#L53) | Prepared sample label/pixel/terrain/elevation/off-grid rows. | Move dense evidence table to the Evidence tab without making the child read or derive the grid. |
| E07 / P3 | `HexFeatureList` | [Board/HexInspector:74-90](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Board/HexInspector.razor#L74) | Covering feature display rows; SelectFeature. | Optional only if feature evidence is reused outside HexInspector. |
| E08 / P2 | `TerrainPicker` | [Board/FeatureProperties:12-21](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Board/FeatureProperties.razor#L12); [Author:59-68](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Author.razor#L59) | Already-filtered terrain codes/names, selected code, ID; CodeChanged. | A genuinely shared terrain selection; feature-kind filtering remains with the authoring adapter. |

### 16.15 Main review: passes 14 through 16 (2026-09-29)

The reviewed remote main is `ab32b53a8740f45dc47f8d349b0dcc5076d89f81`. Local main additionally contains `fd1e71b`, a Docker merge-check script and plan update with no Studio markup changes. This review inspected commit diffs, current Play markup and action/reset helpers, the pass review reports, and the added MapStudio tests. It did not execute their tests or repeat their browser sessions.

| Commits | Changes relevant to this redesign | Component response |
|---|---|---|
| `6cf7ea1`, merged by `5e58210`; review record `2fd2c24` | CC capture/Guard/yield, prisoner escape and custody, infiltration, Ambush Withdrawal and Hand-to-Hand | Extend C02-C09; add N05-N09; retain explicit proposal and disclosure boundaries |
| `0af4dde`, merged by `91a18a4`; review record `ec7bf80` | SAN/Sniper placement/history, FT/MOL, thrown/placed DCs, berserk retained SW; expanded nationality/catalog rules | Extend P05/P08, A13, C13/C14 and R05/R06; add N03/N04/N10-N12 |
| `eec0739`, merged by `ab32b53`; review record `68ca5e2` | Setup SSR text, night/weather summary, Starshell actions and event history | Extend P06 and R05; add N01/N02/N13 |
| `e2ac914` | Scenario cards moved to pass 17 | No scenario-card component is justified by current markup |
| `fd1e71b` (local main only) | Linux/Docker merge-check script and plan maintenance | No new UI boundary |

Only Play.razor changed among Studio pages/components: 493 lines added and 20 removed. It now has 4,228 lines, including 1,740 before @code, up 473 and 208 respectively. Across the ten pages this is 7,449 total lines and 3,189 before @code. Play now accounts for approximately 57% and 55%. Other pages may display richer vocabulary/catalog data, but this does not justify nationality-specific editor components or duplicating UnitEditor's vocabulary-driven controls.

#### Additional candidate boundaries

These 13 additions contain nine P1 and four P2 candidates. Optional children overlap their named parents in the same way as the original inventory. No new component files are implemented by this review.

| ID / priority | Proposed component | Existing source block on reviewed main | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| N01 / retired (was P1) | ~~`ScenarioSpecialRulesField`~~ | [Play:137-144](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L137-L144) | SSR draft text, help and validation; RulesChanged. | Child of P06. Keep exact tokens and advanced text round-trip; the server validates combinations. Do not conflate game SSRs with Maps terrain rules. |
| N02 / P1 | `NightWeatherSummary` | [Play:189-192](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L189-L192) | Prepared NVR/weather/precipitation/illumination text; no service access. | Persistent context independent of the action pane. Distinguish no applicable conditions from withheld facts. |
| N03 / P1 | `PlaceDemolitionChargeAction` | [Play:580-594](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L580-L594) | Mover IDs, permitted charge/holder choices, target draft and busy state; DraftChanged/ReviewPlace. | Complete movement subtask. The page retains GameActions.Move and its placeDc/placeDcAt arguments, not a new independent mutation. |
| N04 / P2 | `BerserkRetainedWeaponsField` | [Play:595-598](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L595-L598) | Authorized berserk-mover context and retained SW draft; KeepChanged. | Optional A13 child for the conditional Keep SW field; preserve comma-separated IDs and server portage validation. |
| N05 / P2 | `CloseCombatRoundOptions` | [Play:896-905](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L896-L905) | Prisoner-round selection, Hand-to-Hand capability/value; RoundChanged/HandToHandChanged. | Optional C03 child. Round changes reset declarations; Hand-to-Hand is capability-gated, not a global preference. |
| N06 / P1 | `AmbushWithdrawalActions` | [Play:907-921](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L907-L921) | Authorized ambusher units and precomputed destinations; ReviewWithdraw(unit, destination). | A separate immediate proposal from C05 withdrawal declarations; preserve before-round/closed-round timing. |
| N07 / P2 | `CaptureAttemptFields` | [Play:988-1003](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L988-L1003) | Capture flag, attacker-derived Guard choices and ordered defender yield draft; DraftChanged. | Optional C06 child; capture belongs to a queued attack and must not execute when toggled. |
| N08 / P1 | `CloseCombatInfiltrationChoices` | [Play:1005-1028](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1005-L1028) | Authorized unit destinations, selected infiltration map; InfiltrationsChanged. | Distinct conditional destination declarations; planner supplies eligibility and destinations, page submits them with the round. |
| N09 / P1 | `PrisonerCustodyActions` | [Play:1072-1086](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1072-L1086) | Projected Guards, permitted recipients, transfer/abandon availability; ReviewTransfer/ReviewAbandon. | Rally/Advance custody operations differ from C10 massacre actions and C09 pending surrender; preserve explicit abandonment intent. |
| N10 / P2 | `MolFirerChoice` | [Play:1167-1179](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1167-L1179) | SSR-enabled state, selected firer IDs and optional MOL user; FirerChanged. | Optional C14 child. Preserve none, SSR gating and the selected-firer dependency; do not duplicate the fire planner. |
| N11 / P1 | `ThrowDemolitionChargeAction` | [Play:1226-1246](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1226-L1246) | Projected holder/charge pairs, target draft and busy state; DraftChanged/ReviewThrow. | Independent fire task. Use a typed pair in a new contract; the page adapts the current holder|charge encoding and two-Location result. |
| N12 / P1 | `PlacedChargeDetonationActions` | [Play:1277-1287](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1277-L1287) | Authorized operable charge rows, disclosed target/concealment/CX text; ReviewDetonate(chargeId). | AFPh task with phase-end implications; buttons propose rather than detonate directly. |
| N13 / P1 | `StarshellActionPanel` | [Play:1247-1276](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1247-L1276) | Eligible projected firers, method/target draft and busy state; DraftChanged/ReviewStarshell. | Separate night task: own-hex, at-target and three-hexes have different target requirements; preserve server timing, LOS and NVR checks. |

#### Amendments to existing candidates

These amendments expand existing boundaries and do not increase the count.

| Candidate | Revised contract and preservation requirement |
|---|---|
| P03/P05, NewGameForm / ScenarioSidesFields | Retired (section 16.16). Each side draft now includes optional SAN alongside ELR and edge; preserve unset versus zero and the relation to a placed Sniper counter. Keep both side labels explicitly associated with their fields. |
| P06, ScenarioConditionsFields | Retired (section 16.16). Compose N01 with month/year/defender inputs. Expose rule validation without turning the component into a rules engine. |
| P08, SetupPlacementEditor | Include the per-side Sniper choice beside Dummy choices. Preserve the special Sniper placement/serialization path rather than requiring a catalog unit definition. |
| A13, InfantryMovementAction | Include N03 and optional N04 with shared mover context, Assault Movement and Double Time. Review place versus ordinary move through their existing handlers. |
| C02/C03, CloseCombatPanel / CloseCombatLocationControl | Include prisoner-round and Hand-to-Hand options, infiltration declarations and separate Ambush Withdrawal. A Location or round change is an explicit draft boundary. |
| C06/C07, CloseCombatAttackBuilder / CloseCombatAttackQueue | DeclaredAttack now includes Capture, ordered Yield and optional Guard. Show capture versus attack, preserve yield order, and carry these values into review; adding an attack resets its fields. |
| C09, PendingSurrenderPanel | Add ReviewFree as Unarmed beside accept/reject. Preserve the existing service meaning and availability; it is not equivalent to rejecting surrender with No Quarter. |
| C10, PrisonerActionPanel | Retain the baseline massacre-specific actions. N09 separately owns transfer/abandon controls; a shared visual wrapper must not merge their command semantics. |
| C13/C14, SmallArmsFirePanel / FireGroupSelector | Include FT among weapon choices and optional MOL user. Parent serialization preserves a FT holder's withoutInherent treatment and validates the MOL user against selected firers. |
| R05, ActionRecordList | Reuse for NightRecords and SniperRecords, retaining play-night/night-record and play-snipers/sniper-record hooks and data-event IDs. No separate near-identical history components are needed. |
| R06/R07, FireHistory / FireResolutionCard | Preserve DC identity, user and mode, including the thrown charge's attack at its own Location; retain FT/MOL and low-visibility arithmetic supplied by existing records. |
| R05 Close Combat adapter | Keep capture/Guard, freed-as-Unarmed, escape NTC, rearming, infiltration, concealment loss and Hand-to-Hand evidence in formatted records. Generic list rendering must not discard these facts. |

New reusable history blocks: [Play:1618-1638](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1618-L1638). State and formatting evidence: [Play:2159-2199](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2159-L2199), [Play:2261-2289](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2261-L2289), [Play:2918-3037](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L2918-L3037).

#### State, disclosure and verification additions

- Adding a CC attack resets capture, Guard and yield along with participants/director. ClearCloseCombat also clears infiltration and Hand-to-Hand. ChooseRound currently clears participants/director/queued attacks, but not every new CC field; specify and test which fields should survive rather than assuming all resets are identical.
- Revalidate charge/holder choices when movers, game, perspective or revision change. Revalidate the retained SW list, MOL user and Starshell firer/target when their dependent selections change. These are extraction requirements, not a claim that the present page already resets every field.
- Keep IsVisibleTo filtering in the NightRecords/SniperRecords adapters. Night/weather summaries currently read state, and custody/detonation choices also inspect state; audit their presentation contracts for each perspective before passing data to children. Concealed target flags and illumination locations require the same scrutiny as ordinary fire facts. These are review targets, not demonstrated leaks.
- Preserve the existing capture, two-Location thrown-DC, and night/Starshell page-test flows. Extend extraction coverage to freeing surrender, transferring/abandoning prisoners, capture/yield queue reset, prisoner rounds, infiltration and Ambush Withdrawal, FT/MOL dependencies, SAN/Sniper setup, and all Starshell methods. Check refused/stale/confirmed outcomes through the parent gate path.
- Keep the pass reports' known rule limitations distinct from UI capabilities. This design does not promise additional night routing, Gunflash, terrain/weather, or Sniper adjudication beyond the server's implemented scope. Scenario-card workflow remained later work at that review; section 16.16 covers it.

[Pass 14 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2014%20Review%202026-09-28.md); [Pass 15 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2015%20Review%202026-09-29.md); [Pass 16 review](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/ab32b53a8740f45dc47f8d349b0dcc5076d89f81/src/ASL/docs/Scenario%20A1%20Backlog%20Pass%2016%20Review%202026-09-29.md).

### 16.16 Main review: passes 17 through 22 (2026-09-30)

The branch was rebased onto main `c13e9b7`, which includes the scenario card games plan: passes 17 and 17b (the card format and the Scenario cards page), 18 (a game starts from a card), 19 (setup from the OB), 20 (turns, reinforcements, and the start options), 21 (Victory Conditions), and 22 (the card editor, the user's cards, and the new-game form's removal), with the demonstration fixes of 2026-09-30. The review read the commit diffs and the current markup of Play, Scenarios, and CardEditor, and the MapStudio tests that exercise them. It did not run a fresh browser session.

| Commits | Changes relevant to this redesign | Component response |
|---|---|---|
| `55d522a`, `1a748e9`, `1347115` | The Scenario cards page (`/units/scenarios`): a card picker and the whole card read-only | K02, K10 to K12 |
| `9fce110` | The Play card panel, the game's card id and hash | K06 |
| `7132323` | The OB setup table and the OB group choice at placement | K05; P08 amended |
| `13495a9` | Game end, the first-move note, the Balance choice, off-board placement for entering groups | K03, K04, K07; P08 amended |
| `4bd323d` | The Victory Conditions standing, the result, leaving the map | K08, K09; A13 amended |
| `fa365b7`, `db812c9` | The card editor (`/units/cards/edit`); the new-game form removed; the start summary; Save never disabled | K01, K13 to K20; P03 to P06 and N01 retired |

Current page measurements (lines before `@code`):

| Page | Total source lines | Lines before @code |
|---|---:|---:|
| Author | 591 | 152 |
| BoardViewer | 759 | 309 |
| CardEditor (new) | 531 | 160 |
| Fidelity | 309 | 217 |
| Games | 342 | 179 |
| Home | 215 | 150 |
| Maps | 321 | 123 |
| NewBoard | 159 | 61 |
| Play | 4,371 | 1,815 |
| Scenarios (new) | 250 | 178 |
| Settings | 47 | 31 |
| UnitLab | 478 | 227 |
| **Total** | **8,373** | **3,602** |

Play is now about 52% of page source and 50% of the markup region. Its growth since section 16.15 (143 lines) is the card panel, the OB setup table, and the card choice; the removed new-game form offset part of it.

#### Retired candidates

P03 `NewGameForm`, P04 `GameMapChoice`, P05 `ScenarioSidesFields`, P06 `ScenarioConditionsFields`, and N01 `ScenarioSpecialRulesField` are retired: pass 22 removed the new-game form (ruling R22.4). Their concerns move to K01 (the card choice on Play) and K14 to K17 (the same fields in the card editor), where the guidance of sections 13.2 and 16.10 now applies.

#### New candidate boundaries

These 21 additions contain 15 P1 and 6 P2 candidates; K21 was added on 2026-09-30 for card provenance (section 13.6). Links pin main `c13e9b7`.

| ID / priority | Proposed component | Existing source block on main | Inputs and outputs | Why this boundary helps |
|---|---|---|---|---|
| K01 / P1 | `NewGameFromCard` | [Play:132-185](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L132-L185) | Card choices, the chosen card's start summary model, game id/label draft, Balance draft; CardChanged/DraftChanged. | The whole new-game task in one root; the page keeps the card's hash, placement group pruning, and proposal construction. |
| K02 / P1 | `CardPicker` | [Play:140-147](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L140-L147); [Scenarios:23-31](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Scenarios.razor#L23-L31); [CardEditor:28-37](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L28-L37) | Card names with title and built-in/user flag, the first option's label ("choose a card", none, "a minimal card"), selection; SelectedChanged. | The same card list is built three ways on three pages ("(yours)", "your card", "a copy of"); one picker keeps labels and ordering consistent. |
| K03 / P1 | `CardStartSummary` | [Play:150-163](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L150-L163) | Prepared boards, side lines (ELR, SAN, edge, who moves first), date, SSR tokens, Scenario Defender, first-move note. | Read-only presentation of what the card gives; a minimal side says "none" rather than "by OB group". |
| K04 / P2 | `BalanceChoice` | [Play:164-182](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L164-L182) | The card's sides, the agree/wish draft, player names; BalanceChanged. | A26.4's two forms and the player names belong together; hidden for a minimal card. |
| K05 / P1 | `SetupPoolsTable` | [Play:218-243](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L218-L243) | Prepared setup report rows (order, side, group, status, remaining, "?" left, areas) or the minimal-card note. | A complete read-only view with its own tests (`#setup-pools`, `data-group`); the planner's report is computed by the page. |
| K06 / P1 | `PlayCardPanel` | [Play:67-130](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L67-L130) | The game's card summary, changed or missing flags, Balance and players, groups; child slots for K07 and K08. | The card context of a live game; changed and missing cards stay explicit warnings. |
| K07 / P1 | `GameEndNotice` | [Play:81-89](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L81-L89) | Turn, reason, and the recorded result (winner or draw, and why). | The game's end is a state the context header should always show; `#play-ended` and `#play-result` keep their hooks. |
| K08 / P1 | `VictoryStandingTable` | [Play:100-116](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L100-L116) | Prepared Control rows and side totals (CVP, Exit VP, squad-equivalents) and the would-win line. | An evidence table with `data-control` and `data-side` hooks; it must take the side's view once per-side views exist (section 15.4). |
| K09 / P2 | `MapExitAction` | [Play:629-639](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L629-L639) | Edge choices and the selected movers' context; ReviewExit. | A distinct movement task (A2.6) sharing A13's mover selection; the page keeps `ProposeExit`. |
| K10 / P1 | `ScenarioCardView` | [Scenarios:49-175](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Scenarios.razor#L49-L175) | A read card and display helpers (side names, compass, basis text, citations); no library access. | The whole card, read-only; the same view could show a card in a Play side panel instead of the current partial copy. |
| K11 / P2 | `CardSideSection` | [Scenarios:81-133](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Scenarios.razor#L81-L133) | One side's ELR, SAN, Integrity, edge, Balance, and OB groups, keyed by side. | Repeated per side; its facts also appear in K03 and K17, so one side model serves all three. |
| K12 / P2 | `CardSpecialRulesList` | [Play:117-122](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L117-L122); [Scenarios:135-156](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Scenarios.razor#L135-L156) | SSR rows with an option to show status and citations. | Play shows the SSR text only; Scenarios adds status and citations. One list with a detail switch avoids two drifting lists. |
| K13 / P1 | `CardEditorForm` | [CardEditor:44-138](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L44-L138) | A card draft, the catalog's choices, and the diagnostics; DraftChanged. | The editor's form root; the page keeps the library, validation, save, and delete. Pass 28's forms grow inside it. Built in pass 28 with the draft, the catalog, the side choices, the board names, the saved maps, the drawn map and why it cannot be drawn, and the card's JSON; Changed; it owns which area or condition is being picked. |
| K14 / P1 | `CardIdentityFields` | [CardEditor:45-56](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L45-L56); [CardEditor:133-137](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L133-L137) | Id, title, place, date, introduction, and source draft; IdentityChanged. | The id is checked as typed (form, built-in name, another user card); a minimal card may leave the date 0. The Source label's `for="edit-basis"` does not match the input `#edit-source-basis`; pass 28.5 fixes it. |
| K15 / P1 | `CardMapFields` | [CardEditor:57-72](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L57-L72) | Boards text, north, playable area and hexrows with their board; MapChanged. | The future host of map picking and saved-map choice (pass 28); today typed. Built in pass 28 with board rows, a saved map's boards, and the new `CardMapPicker` on B06. |
| K16 / P1 | `CardTurnRecordFields` | [CardEditor:74-82](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L74-L82) | Turns, half turn, sets-up and moves-first sides, Scenario Defender, side choices; TurnsChanged. | Its side references follow a side's nationality change (section 15.3). |
| K17 / P1 | `CardSideFields` | [CardEditor:83-114](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L83-L114) | One side's nationality, SAN, ELR, edge and basis, Integrity BPV, Balance, and OB groups; keyed by index; SideChanged. | The two sides are one repeated block; a side ELR on a card with an OB is refused, not dropped. |
| K18 / P1 | `ObCounterPicker` | [CardEditor:115-128](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L115-L128) | Side and group, the side's counters in the order shown, count and area; AddRequested. | Adds the counter it shows (the demonstration found it did not); resets to the side's first counter when the side changes. |
| K19 / P2 | `CardJsonField` | [CardEditor:112-113](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L112-L113); [CardEditor:129-132](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L129-L132) | Label, JSON text, and parse error; TextChanged. | Editable JSON for groups, SSRs, and Victory Conditions until pass 28's forms replace it; distinct from the read-only S14 `JsonDisclosure`. Retired in pass 28: the forms replace it, and S14 shows the card read only. |
| K20 / P2 | `CardSaveActions` | [CardEditor:38-41](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L38-L41); [CardEditor:154-158](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L154-L158) | Can-delete state, the save note; Save/Delete. | Composes S04 `OperationFeedback`. Save stays enabled and says why a card is not saved, so a click that arrives with a field's last change is not lost. |
| K21 / P1 | `CardProvenancePanel` | New (no existing block): beside [Scenarios:50-60](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Scenarios.razor#L50-L60) and [Play:67-130](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L67-L130) | A prepared provenance model (card identity, the game's recorded start, source, counter value sources, citations with pages or ruling text); no library or registry access. | A card's full provenance on both pages (section 13.6); the only candidate that is new behavior rather than extracted markup, added at the user's request. |

#### Amendments to existing candidates

| Candidate | Revised contract and preservation requirement |
|---|---|
| S04 `OperationFeedback` | Add the card editor's save note and "Not saved" refusal as a use. |
| S06 `FindingList` | Add the card diagnostics of Scenarios (`#card-diagnostics`) and the card editor (`#edit-diagnostics`, `#edit-valid`) as adapters; keep the explicit valid state. |
| P08 `SetupPlacementEditor` | Include the OB group choice (`#place-group`, shown only when the card has groups) and off-board placement (`#place-offboard`) for entering groups. |
| A13 `InfantryMovementAction` | Compose K09 for leaving the map; the movers' selection is shared. |
| R11 `PlayMapPanel` | Setup and entry areas and the playable area are candidates for map highlighting (pass 28 picks them on the map). |

#### Shared logic that is not a component

The card-to-boards text (`bd04@0,0 bd02@0,1/r`) is formatted in both Play and the card editor, and card titles are read in both Play and Scenarios. These belong in the Play library beside `ScenarioCards.PlaceAndDate`, not in a component.

#### Coordination with the passes

Section 16.17 assigns every candidate to a pass. The shared components come first (pass 22b); the card panels and the Victory standing are extracted with pass 23's side view as an input; pass 28 builds its forms on S07 `FieldGroup`, S14 `JsonDisclosure`, S06 `FindingList`, and K02 `CardPicker`, and its map picking on B06 `BoardViewport`.

### 16.17 Candidates by pass

Every active candidate belongs to exactly one pass; the five retired ones (P03 to P06 and N01) to none. A pass extracts its P1 candidates by default and may leave one inline with a reason; its P2 and P3 candidates are extracted only when the parent stays hard to understand, is rendered on its own, or is reused (section 16.2). The pass's review records every candidate left inline. Pass 24 extends K08 rather than adding one, and passes 32 and 33 build a new page from the shared components rather than extracting any.

| Pass | Candidates | P1 | P2 and P3 | Candidates |
|---|---:|---:|---:|---|
| 22b | 14 | 13 | 1 | S01 `StudioNavigation`, S02 `PageHeader`, S03 `SourceRequiredNotice`, S04 `OperationFeedback`, S05 `StatusBadge`, S06 `FindingList`, S07 `FieldGroup`, S09 `PerspectivePicker`, S10 `RevisionNavigator`, K01 `NewGameFromCard`, K02 `CardPicker`, K03 `CardStartSummary`, K04 `BalanceChoice`, K21 `CardProvenancePanel` |
| 22c | 31 | 19 | 12 | H01 `AuthoredBoardList`, H02 `MapSummaryList`, H03 `BoardScopeSummary`, H04 `LibraryReportContext`, H05 `VaslBoardTable`, H06 `BoardVerificationRow`, M01 `MapPlacementEditor`, M02 `MapPlacementRow`, M03 `ScenarioRuleEditor`, M04 `PlacementTextEditor`, M05 `MapBuildActions`, M06 `NewBoardForm`, M07 `BoardDimensionsFields`, M08 `SourceDraftNotice`, G01 `GameReplayToolbar`, G02 `GameReadFindings`, G03 `GameContextSummary`, G04 `ProjectedGameUnitTable`, G05 `GameEquipmentTable`, G06 `ReadCaseForm`, G07 `ReadCaseResult`, G08 `PerspectiveEventList`, F01 `FidelityRunControls`, F02 `LosFidelityPanel`, F03 `LosFidelityResultsTable`, F04 `FidelityReportPicker`, F05 `FidelityReportMetadata`, F06 `FidelityResultsTable`, F07 `FidelityDifferences`, F08 `FidelityCheckBadges`, F09 `StudioConfigurationReport` |
| 22d | 34 | 20 | 14 | B01 `BoardViewerToolbar`, B02 `BoardViewPicker`, B03 `BoardComparisonControls`, B04 `BoardLayerToggles`, B05 `UnitOverlayControls`, B06 `BoardViewport`, B07 `SelectedUnitInspector`, B08 `UnitGameFacts`, B09 `HexUnitStackList`, B10 `LosPanel`, B11 `LosResult`, B12 `BoardProvenancePanel`, B13 `BoardEditorHeader`, B14 `EditorToolOptions`, B15 `InspectorTabs`, U01 `UnitTemplatePicker`, U02 `AttachedEquipmentEditor`, U03 `CounterTierPreviewGrid`, U04 `CounterFacePreviewGallery`, U05 `UnitAccessibleDetails`, U06 `UnitLabFindings`, U07 `UnitDocumentOutput`, U08 `UnitStyleSheetEditor`, U09 `UnitPlacementSetEditor`, U10 `PlacedUnitList`, E01 `VocabularyAttributeInput`, E02 `UnitIdentityFields`, E03 `UnitFaceEditor`, E04 `UnitAttributeFields`, E05 `UnitStateChecks`, E06 `HexGridSamples`, E07 `HexFeatureList`, E08 `TerrainPicker`, S14 `JsonDisclosure` |
| 23 | 13 | 12 | 1 | P01 `ScriptedDicePanel`, P02 `LiveGameToolbar`, P07 `GameReplayFailure`, N02 `NightWeatherSummary`, R01 `ProposalReviewPanel`, R02 `EntryReviewFacts`, R03 `FireReviewFacts`, R12 `PlayUnitTable`, R13 `AdjudicatorAuditPanel`, K05 `SetupPoolsTable`, K06 `PlayCardPanel`, K07 `GameEndNotice`, K08 `VictoryStandingTable` |
| 25 | 13 | 8 | 5 | A01 `BuildingEntryAction`, A11 `MovementStatus`, A13 `InfantryMovementAction`, A14 `SmokeGrenadeAction`, A15 `MovementWindowActions`, A16 `ReactionFireAction`, A23 `AdvanceActionPanel`, C19 `OpenEntryDeclaration`, K09 `MapExitAction`, N03 `PlaceDemolitionChargeAction`, N04 `BerserkRetainedWeaponsField`, S11 `LocationField`, S12 `UnitSelectionList` |
| 26 | 10 | 7 | 3 | P08 `SetupPlacementEditor`, P09 `SetupPlacementList`, A17 `VehicleMovementPanel`, A18 `VehicleMovementStatus`, A19 `VehicleStepChoices`, A20 `VehicleTowingActions`, A21 `BoundingFireAction`, A22 `CrewExposureActions`, C18 `GunArcAction`, S13 `FacingPicker` |
| 27 | 24 | 14 | 10 | A12 `BerserkChargeNotices`, C01 `VehicleCloseCombatPanel`, C02 `CloseCombatPanel`, C03 `CloseCombatLocationControl`, C04 `CloseCombatStacking`, C05 `CloseCombatWithdrawals`, C06 `CloseCombatAttackBuilder`, C07 `CloseCombatAttackQueue`, C08 `PendingChoicePanel`, C09 `PendingSurrenderPanel`, C10 `PrisonerActionPanel`, C11 `FireMarkerSummary`, C12 `OpportunityFireAction`, C13 `SmallArmsFirePanel`, C14 `FireGroupSelector`, C15 `FireTargetOptions`, N05 `CloseCombatRoundOptions`, N06 `AmbushWithdrawalActions`, N07 `CaptureAttemptFields`, N08 `CloseCombatInfiltrationChoices`, N09 `PrisonerCustodyActions`, N10 `MolFirerChoice`, N11 `ThrowDemolitionChargeAction`, N12 `PlacedChargeDetonationActions` |
| 28 | 11 | 7 | 4 | (K19 retired in pass 28) K10 `ScenarioCardView`, K11 `CardSideSection`, K12 `CardSpecialRulesList`, K13 `CardEditorForm`, K14 `CardIdentityFields`, K15 `CardMapFields`, K16 `CardTurnRecordFields`, K17 `CardSideFields`, K18 `ObCounterPicker`, K19 `CardJsonField`, K20 `CardSaveActions` |
| 28b (built; A03 left inline in A02) | 20 | 13 | 7 | A02 `RoutActionPanel`, A03 `RoutObligations`, A04 `SupportWeaponActionPanel`, A05 `RallyActionPanel`, A06 `RepairActionPanel`, A07 `DeployActionPanel`, A08 `RecombineActionPanel`, A09 `DmRetentionChoices`, A10 `ShockRecoveryAction`, C16 `OrdnanceFirePanel`, C17 `OrdnanceTargetFields`, R04 `DiceRollHistory`, R05 `ActionRecordList`, R06 `FireHistory`, R07 `FireResolutionCard`, R08 `FireArithmeticBreakdown`, R09 `InfantryFireEffectsTable`, R10 `VehicleFireEffectsTable`, N13 `StarshellActionPanel`, S08 `RuleHelp` |
| 28c (built; R11 on B06 BoardViewport) | 1 | 1 | 0 | R11 `PlayMapPanel` |
| **All** | **171** | **114** | **57** | |

# Part V. Acceptance and rulings

## 17. Acceptance and verification

### 17.1 Component extraction acceptance

An extraction is complete when the user can still perform the same task and the ownership is clearer, not merely when the page is shorter.

- Preserve existing routes, query behavior, element IDs/data attributes used by tests, labels, disabled conditions, conditional options, and displayed evidence unless the change explicitly revises them.
- Existing bUnit tests exercise IDs such as play-confirm, play-outcome, fire-facts, lab-name, game-summary and data-unit/data-event rows. Retain these hooks initially and add focused child tests for meaningful contracts.
- Verify enum/null/unknown handling in VocabularyAttributeInput, selection reset in action forms, multiple Location panels, and attachment deletion with stable keys.
- Test parent integration for proposal then confirm, cancel, refusal, stale state and idempotent replay. A child callback test cannot replace gate-path coverage.
- Verify both player and adjudicator views, including switching between them with a pending proposal or selected unit. Check the DOM and accessible text for withheld information.
- Keep the existing renderer's SVG output unchanged for presentation-only extraction. Verify counter tier switching, keyboard focus and viewport lifecycle in a browser when touched.
- Do not call planners or perform recursive filesystem enumeration from a frequently rerendered child. Prepare expensive display facts outside render loops; measure before introducing caching that might become stale.
- Run the relevant MapStudio tests and build for implementation changes. For inventory-only edits, validate source references, links, priorities/counts, and the diff.

The most useful initial outcome is a set of focused page coordinators composing understandable task panels. The inventory is intentionally broader than the first implementation pass so redesign decisions can choose boundaries with evidence rather than inventing them during markup changes.

### 17.2 Every pass: the Studio visual check first

**User rule, 2026-09-30.** Before the full local suite, the Docker check, or any other long test run, every pass runs the Studio (`map-studio-scripted`) and checks in the browser every page it changed: the new behavior played, the new or extracted components looked at, in a side's and the adjudicator's view where disclosure applies. Every issue found is fixed at once and the visual check run again, until it is clean; only then do the costly tests run. A pass or task the Studio cannot show (a rules change with no page effect, a test, a document) records that it needed none. The browser evidence of section 17.3 comes from this check.

**Game passes.** A game pass is complete when its tasks' rules play as its rulings say, its referee and table player reviews are answered, and the merge gate of the Backlog Passes Plan's section 1 passes: the full local suite, the rules tests among it, the Docker Linux check, the chart supplement regeneration where authoring changed, and GitHub Actions after the push. Its component task (23.5, 24.5, 25.6, 26.5, 27.5, 28.5, or 29.5) also meets section 17.1.

### 17.3 Browser evidence

Completion evidence by pass, carried from the redesign's delivery stages:

| Passes | Completion evidence |
|---|---|
| 22b | Every route reachable; the current navigation group correct; keyboard and zoom inspection; the full provenance of the three built-in cards, on the Scenarios page and in a game from each, with every citation resolved to a page or a ruling; a user card's provenance shown, with any unregistered citation marked as such. |
| 22c | Matching, empty, loading, and failure states and back navigation in the Board library; rule order and placement editing preserved in Maps. |
| 22d | Existing edit, save, undo, and redo, the counter tiers, and the comparison modes preserved; the viewport's browser checks recorded. |
| 23.5 to 28.5, 28b | Representative actions from each extracted family keep their proposal, confirmation, refusal, and replay behavior, in a side's and the adjudicator's view. |
| 28c | Overflow, focus, reconnection, performance, copy, and evidence review; the browser walkthrough matrix and the relevant automated checks recorded. |

Use representative data: empty/configuration failure, a full board library, a verified board and a failing board, a source-derived draft, a composed map with ordered SSR rules, multiple unit kinds, and live/synthetic games. Inspect at 1920x1080, 1366x768, 1024x768, and narrow/zoomed layouts. Include long names, diagnostics, and action labels.

Run targeted MapStudio component tests for changed flows and existing renderer/viewport checks when those boundaries are touched. For Play, verify visible versus adjudicator projections, proposal refusal, stale confirmation, duplicate confirmation, and record attribution. Build with existing warning policy; broaden testing when a change crosses service or rendering boundaries. Documentation-only changes require link, diff, and scope checks, not an application test run.

Record browser evidence separately from automated results. A compilation or selector check is not visual acceptance. No screen is complete while ordinary scrolling or pane collapse can make Confirm, Save, validation, or draft restrictions unreachable.

**Records of completed checks.** The records below come from the redesign branch; they describe the theme and the card pages as checked on 2026-09-28 to 2026-09-30.

### 17.4 Completed stylesheet foundation checks

The existing stylesheet was moved from app.css to site.css, retaining its original rules. App.razor now loads site.css through the static asset map. The viewport uses data attributes for layer visibility and comparison state; all display, visibility, and opacity declarations live in site.css.

Validation: Release build succeeded with the existing warning policy; 23 targeted MapStudio render-endpoint, map-page, and component tests passed; all five existing viewport pointer/tier tests passed. A headless Edge check using the real viewport module and stylesheet verified all 101 opacity values, layer hiding/showing across reload, side-by-side reload, swipe clipping, mode transitions, and disposal, with zero style attributes or embedded style elements in the resulting DOM.

Initial page tests exposed local Git CRLF conversion of checksum-pinned JSON resources in the isolated checkout. Restoring their exact committed bytes resolved those failures; no rule-package content or checksum was changed.

### 17.5 Supplied theme and consolidated document checks

The theme build succeeded. A headless Edge smoke check at 1366x900 reached nine routes (library, Maps, Unit Lab, Play, Settings, New board, Fidelity, Game states, and board viewer). All used site.css, the expected warm background and dark header, and 15px base text, with no horizontal page overflow or inline styles in those sampled states. All seven local font faces loaded, with zero external requests. Library and Unit Lab screenshots were inspected; the Unit Lab select overlap is recorded in section 11.5. The viewer was sampled in its loading state, so this does not establish loaded-board visual acceptance or completion of every workflow.

The viewport browser check was repeated against the supplied theme: all 101 opacity values, layer visibility/reload, side-by-side/reload, swipe and mode transitions, and disposal passed, with no style attributes or embedded styles.

The supplied :root block and following theme rules are preserved exactly after newline normalization. The initial consolidation retained all 142 baseline candidates and their source references; the subsequent main review adds 13 candidates (section 16.15), for 155 total. Responsive, interactive, populated-game, and accessibility acceptance remains subject to the delivery matrix above.

### 17.6 Main-review document verification

Checked the 2026-09-29 additions against the reviewed Git source, including their line ranges, total counts and priorities. The inventory now has 155 unique candidate IDs: 102 P1, 49 P2 and four P3. Existing source links remain at the branch baseline; new references pin main at ab32b53. CSS values and the site.css-only rule are unchanged. This is a documentation update, so no application build, test run or fresh visual acceptance is claimed.

### 17.7 Rebase and passes 17 to 22 review (2026-09-30)

The branch was rebased onto main `c13e9b7`. The only conflict was app.css: main had added the scenario card rules there, which this branch deletes. They were moved into site.css in the theme's tokens; the card editor rules main added later came across with Git's rename detection and were set to `--font-mono`. The review added 20 candidates and retired 5 (section 16.16): 170 active, 113 P1, 53 P2, and four P3. The old inventory's 188 local source links now pin `2fa3f56`. On the rebased branch the MapStudio project built in Release with warnings as errors, and the 156 MapStudio tests passed. A browser check of the card pages under the theme followed the same day (section 17.8).

### 17.8 Card pages under the theme: browser check (2026-09-30)

The Studio ran from this branch against the user's live data at the Browser pane's width (about 2,000 CSS pixels). The check covered the Scenario cards page, the card editor (a copy of The Guards Counterattack with a validation error, and a user card), a new game on Play from a card, and a finished game from a card (`p21-visual`, read only). Every page loaded only site.css and the seven local fonts, with no style attributes, no embedded styles, and no horizontal page overflow.

It found one defect and four layout gaps, fixed on this branch and checked again after a relaunch:

| Finding | Fix |
|---|---|
| The theme's `.diagnostic` is a badge (fixed height, no wrap, 0.75rem), but passes 18 to 22 use it on whole messages: the game result overflowed its 555px badge by 151px, and the editor's error list sat beside Save | `p.diagnostic` and `ul.diagnostic` are block messages that wrap, keep their severity colours, and use 0.875rem text; a result with no severity uses the surface colour. Spans stay badges. |
| The new-game fields on Play had no grid | A `new-game-fields` class: a label column and a field column, capped at 60rem. |
| The card editor spanned the whole pane (JSON lines about 1,900px) | Capped at 90rem, the collection width of section 11.1. |
| A card's facts on the Scenarios page were 12px mono (`dl.report dd`) | Scenario cards use the sans face at 0.875rem; provenance reports elsewhere keep mono. |
| Save was a plain button; Delete was not marked | Save is `primary` and Delete `danger` (section 11.3). |

The 156 MapStudio tests passed after the changes. Not covered: narrow widths, 200% zoom, keyboard-only use, and the board viewer under the new rules (the rules are scoped, so it should be unchanged).

## 18. Rulings

Each game pass adds its rulings to the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5, R23.1 onward, subject to the referee's review. The Studio passes add no rulings; their design decisions are recorded in section 3 and, when one changes, in the section of Part III it belongs to.

# Part VI. Legacy scenario migration

## 19. Outcome and decisions

**Added 2026-09-30 at the user's request.** Every portable ASL scenario card *The General* published is ported into the game as a scenario card, brought up to the registered rulebook. Pass 17 catalogued the source and ported two cards, and pass 17b a third; the other 83 portable cards are planned here.

**The source.** Avalon Hill's *The General*, Vol. 22 No. 6 (1986) to Vol. 32 No. 3 (1994), at `E:\AvalonHill\The_General\General Avalon Hill`, with the 1988 Special Issue. The insert cards are the last pages of most issues; the magazine's own list, "GENERAL Scenarios for ASL" (Vol. 31 No. 2, p. 52), was the checklist. The catalog of 2026-09-30 found 89 distinct cards: 33 in Vol. 22 to 26 and 56 in Vol. 27 to 32, every listed scenario found, and 14 printed after the list (G35 to G46, V, and W). The Special Issue's scenario H reprints Vol. 24 No. 1's. Appendix A lists all 89.

**The user's decisions (2026-09-30):**

1. **Cards whose printed facts cannot be settled are not portable,** and are not migrated. Three are marked so in Appendix A:

| Card | Title | Why it is not portable |
|---|---|---|
| G28 | Ramsey's Charge | names board 25, which is not in its board layout |
| U | Chance d'Une Affaire | its board number is printed rotated and cannot be read with certainty |
| W | The Defense of Luga | its Turn Record Chart shows both END after Turn 4 and a restart box |

   Two cards first judged so are settled by one-line rulings instead, each recorded as its card's ruling when it is ported:

| Card | Title | Ruling |
|---|---|---|
| C | The Streets of Stalingrad | The date is 6 October 1942, as scenarios A and B that it combines; the card's 1944 is a misprint. |
| H | Escape from Velikiye Luki | The boards are as the original printing lays them out (Vol. 24 No. 1: boards 4, 2, and 3, side by side, east to west); the 1988 reprint's order is not used. |

2. **Display first.** Every portable card is ported first as display only: read, adapted, its counters in the catalog, its provenance complete, and shown on the Scenarios page, but not offered for play. It becomes playable when the rule packages it needs are built (section 22).
3. **Order.** The migration follows pass 33.
4. **Chapters F and G are not built,** and the cards that need them stay display only, with the chapters in the backlog (section 33 of the ASL Unit Backlog). With one card whose map is not among the VASL boards, 12 portable cards stay display only:

| Card | Title | Why it stays display only |
|---|---|---|
| G9 | Sunday of the Dead | Chapters F and G |
| G13 | A View from the Top | desert boards (Chapter F) |
| G16 | Alligator Creek | Chapters F and G |
| G19 | A Tough Nut to Crack | Chapters F and G |
| G20 | Camp Nibeiwa | Chapters F and G |
| G22 | A Day by the Shore | beaches (Chapter G, G13) |
| G23 | Habbaniya Heights | desert boards (Chapter F) |
| G24 | Mountain Comes to Mohammed | Chapters F and G |
| G27 | Vaagso Venture | a seaborne assault with landing craft (Chapter G, G14) |
| G38 | Castello Fatato | desert boards (Chapter F) |
| G41 | JABO! | the KGP historical map, not among the VASL boards in scope |
| G45 | Halha River Bridge | Chapters F and G |

**The method, per card** (rulings R17.2, R17.9, R17.13, and R0.3 as for the first three):

1. Read the card at 300 dpi; record facts only, paraphrasing the text; never commit a card image or its wording.
2. Bring every entry to the registered rulebook: each term, rule, and reference is checked against the PDF and cited to it; what the rulebook cannot support is rewritten, manufactured under R0.3, or marked not enforced with a backlog row, never carried over as printed.
3. Add the card's counters to the catalog from registered sources (the counter sheets, the National Capabilities Chart, the ordnance and vehicle listings), manufacturing under R0.3 on sheet MFG what no source prints; a catalog version per batch, pinned as batch D1's ruling decides (section 21).
4. Register a PDF comparison for every rule fragment the card newly cites (the pass 17 registry).
5. Write the card (asl-scenario-card/1) marked display only with the packages it waits for, its ruling (one R number per card for its adaptations), and its tests: the card validates and its provenance is complete (section 13.6). Recheck the card's packages against its image: its boards' terrain (a board holding terrain the movement rules refuse waits for pass 40), its nationalities' A25 rules, night and winter conditions, special units, and whether an SSR's "Kindling NA" still leaves a Fire need (DASL-A, T5, and G15 are flagged for this check). A changed need moves the card's playable pass, recorded in the batch's review.
6. When the card's packages are built, the rule pass that completes them makes it playable: tests that it sets up from its OB and plays to its end, its not-enforced entries enforced where the new rules allow, and its display-only mark removed.
7. A referee reviews each display batch's adaptations against the rulebook; each rule pass's table player plays one of the cards it makes playable.

## 20. What the cards need

The catalog read each card's units, terrain, and rules, corrected by the reviews of 2026-09-30. Of the 83 portable cards, 12 stay display only (decision 4) and 71 become playable: 4 need nothing the game lacks once passes 23 to 33 are done, and the rest wait for rule packages the game has not built. Each display batch rechecks its cards (section 19, step 5).

| Package | Cards needing it (of the 71) | What is missing today |
|---|---:|---|
| Armored combat | 49 | Closed-topped AFVs, main armament, AP and HEAT, special ammunition, To Kill, and Armor Leaders (backlog sections 12 and 14). Vehicles move, fire MGs, and Infantry attacks unarmored vehicles today. |
| Night and winter | 9 | Night rules beyond pass 16's (the Scenario Defender at night, Cloaking, Straying) and Fog, Ice, Drifts, and Winter Camouflage (backlog section 26). |
| Fortifications | 30 | Foxholes, trenches, wire, mines, pillboxes, and roadblocks (backlog sections 2, 16, 21, 23, 24, 26, and 27). |
| Offboard artillery and air support | 24 | Neither is built. |
| Terrain | 22 | Board overlays, streams, rivers, canals, bridges, bocage, cellars, Factories, sewers, and board terrain the movement rules still refuse (gullies, shellholes, graveyards, crags, cliffs, sunken roads). The 235 VASL boards in scope pass both fidelity checks, so the boards themselves are not a limit. |
| Special units | 12 | Cavalry, skis, air drops and gliders, boats, and partisans. |
| Fire | 4 | Kindling, spread, and Blazes (backlog section 27). |

Playable cards grow as the packages are built, in the order that plays the most cards soonest: after the display batches, 4 playable; with armored combat, 15; with night and winter, 17; with fortifications, 25; with offboard artillery and air support, 39; with terrain, 56; with special units, 67; with fire, 71.

## 21. The display batches: D1 to D18

Each display batch ports cards by steps 1 to 5 of section 19. Batch D1 first extends the card format and the pages (1:20): a card's `play` status (`display-only` with the packages it waits for, or `playable`); the Scenarios page and the card editor showing it; the Play page listing a display-only card but refusing to start it, with the packages named; a user card no longer shadowed by a built-in card of the same name, since the batches add 83 built-in cards (backlog section 32); and a ruling on how a catalog version bump affects games started from a built-in card, so that a batch's new catalog does not mark every earlier game's card changed (for example, a card's catalog pin moves only when its own counters change). Batch D4 also builds the American national rules (0:15), since T1 is the first American card made playable. The 4 cards that need no package are made playable in their display batch, with step 6's tests counted in it. Estimates per card: 15 minutes of build for a small card, 20 for a small-medium one, 25 for a medium one, 33 for a medium-large one, and 40 for a large one, with 10 more for a card that needs armored combat (its vehicles enter the catalog), from pass 17b's actual (The Tractor Works, a medium card with new counters, 0:24 of build including its play tests); 1:15 of overhead per batch.

| Batch | Cards | Build | Total |
|---|---|---|---|
| D1 | C The Streets of Stalingrad; D The Hedgehog of Piepsk | 2:35 | 3:50 |
| D2 | E Hill 621; G1 Timoshenko's Attack; F The Paw of the Tiger; G Hube's Pocket | 2:45 | 4:00 |
| D3 | G2 Last Act in Lorraine; G3 The Forgotten Front; DASL-A To the Last Man; H Escape from Velikiye Luki | 2:25 | 3:40 |
| D4 | T1 Gavin Take; T2 The Puma Prowls; T3 Ranger Stronghold; T4 Shklov's Labors Lost; G4 First Action | 2:30 | 3:45 |
| D5 | G5 Six Came Back; I Buchholz Station; J The Bitche Salient; G6 Rocket's Red Glare; K The Cannes Strongpoint | 3:00 | 4:15 |
| D6 | L Hitdorf on the Rhine; G7 Bring Up the Guns; M First Crisis at Army Group North; G8 Recon in Force; G9 Sunday of the Dead; N Soldiers of Destruction | 2:55 | 4:10 |
| D7 | O The St. Goar Assault; P The Road to Wiltz; G10 Grab at Gribovo; Q Land Leviathans; G11 Pegasus Bridge; HASL-A Ghosts in the Rubble | 3:00 | 4:15 |
| D8 | G12 Avalanche!; T6 The Dead of Winter; T5 The Pouppeville Exit; T7 Hill 253.5; T8 Aachen's Pall | 2:45 | 4:00 |
| D9 | G13 A View from the Top; T9 The Niscemi-Biscari Highway; T10 Devil's Hill; T11 The Attempt to Relieve Peiper; T12 Hunters from the Sky; G14 Tiger, Tiger | 3:00 | 4:15 |
| D10 | R Burzevo; G15 Bone of Contention; S The Whirlwind; G16 Alligator Creek; T13 Commando Raid at Dieppe; T15 The Akrotiri Peninsula | 2:45 | 4:00 |
| D11 | T16 Strayer's Strays; G17 Hakkaa Paalle; G18 Goya; G19 A Tough Nut to Crack | 2:13 | 3:28 |
| D12 | G20 Camp Nibeiwa; G21 Cat's Kill; G22 A Day by the Shore | 2:20 | 3:35 |
| D13 | G23 Habbaniya Heights; G24 Mountain Comes to Mohammed; T Pavlov's House; DASL-B The Kiwis Attack | 2:55 | 4:10 |
| D14 | G25 The T-Patchers; G26 Parker's Crossroads; G27 Vaagso Venture | 2:15 | 3:30 |
| D15 | G29 Shoot-N-Scoot; G30 Morgan's Stand; DASL-C Smoke the Kents!; G31 Point of the Sword; G32 A Helping Hand | 2:45 | 4:00 |
| D16 | G33 The Awakening of Spring; G34 The Liberators; G35 Going to Church; G36 Hill of Death | 2:45 | 4:00 |
| D17 | G37 Forth Bridge; V Auld Lang Syne; G38 Castello Fatato; G39 A Desperate Affair; G40 Will to Fight...Eradicated; G41 JABO! | 2:51 | 4:06 |
| D18 | G42 The Youth's First Blood; G43 Kangaroo Hop; G44 Abandon Ship!; G45 Halha River Bridge; G46 Triumph atop Taraldsvikfjell | 2:45 | 4:00 |

## 22. The rule packages: passes 34 to 43

Each package is a game pass run as in section 1 of the Backlog Passes Plan: rulings, a referee and a table player, the Studio visual check before the costly tests, the merge gate. Its last task makes playable the cards it completes (step 6 of section 19: 10 minutes for a small card, 15 for a larger one). Its other tasks are planned in detail, in the format of section 5, when the pass before it merges; the package estimates are its scale.

| Pass | Package | What it builds | Rules | Package build | Cards made playable | Build | Total |
|---|---|---|---|---|---|---|---|
| 34 | Armored combat I | Closed-topped AFVs and their main armament: AFVs as targets of ordnance and of Infantry AT weapons, the To Hit and To Kill process with AP and HEAT, Bounding First Fire with the MA and the Gun Duels it meets, the tank's BMG and CMG with one fire marker for all its weapons, and crews Bailing Out with their own inherent fire (backlog sections 1, 12, 14, 17, and 18). | C2.2401, C3, C7, C8.1 to C8.3, D1.8, D3, D5, A7.35 | 5:40 | none (0:00) | 5:40 | 6:55 |
| 35 | Armored combat II | Special ammunition (APCR, APDS, Canister, Smoke) and ordnance SMOKE and WP; Motion and Non-Stopped targets and Motion attempts; Immobilization and Shock from To Kill; burning wrecks as terrain (not fire spread); Recall, to a Friendly Board Edge of more than one edge; Passengers and Riders, their fire and rout; ordnance and vehicle fire at another level; Armor Leaders (the catalog shows them on G, G2, T2, T4, M, and Q) (backlog sections 1, 6, 12, 14, 15, 18 to 21, and 23). | C8.4 to C8.9, C2.6, D2, D5.3, D6, D3.4 | 5:25 | G, T2, T3, T4, I, G6, M, N, Q, G14, S (2:05) | 7:30 | 8:45 |
| 35b | Night and winter | The night rules the cards use beyond pass 16's: the Scenario Defender at night (HIP and "?" allotments, Freedom of Movement, the lower ELR, Recon), Cloaking and Straying, and Lax and Stealthy units at night; and the winter conditions the cards set: Fog, Ice, Drifts, and Winter Camouflage (backlog section 26). | E1.2, E1.4 to E1.6, E3.3, E3.7 | 2:45 | R, G33 (0:25) | 3:10 | 4:25 |
| 36 | Fortifications I | Foxholes, trenches, and Entrenching, with Manhandling and Entrenching in Mud, snow, and Extreme Winter; the tasks that place TI; pillboxes with their Covered Arc (from task 26.6), Rally terrain, and DM; roadblocks (backlog sections 2, 23, 24, and 26). | B27, B29, B30, A4.8, A10.61, A10.62, E3.61, E3.722 | 3:35 | none (0:00) | 3:35 | 4:50 |
| 37 | Fortifications II | Wire, A-P and A-T mines, known and hidden minefields, and their attacks on Infantry and vehicles; the French and Norwegian national rules. | B26, B28, A25 | 3:15 | G3, T6, T9, T10, T13, G31, G44, G46 (1:40) | 4:55 | 6:10 |
| 38 | Offboard artillery | OBA: radio and field phone, battery access, Spotting Rounds and Fire for Effect, accuracy and extent of error, the blast area, and its attacks. | C1 | 3:30 | none (0:00) | 3:30 | 4:45 |
| 39 | Air support | Aircraft arrival, sighting, bombs and strafing, and AA fire against them. | E7 | 3:00 | D, E, F, G4, J, K, L, P, T7, T11, G29, V, G42, G43 (3:20) | 6:20 | 7:35 |
| 40 | Terrain I | Board overlays in the map composer and the Hex Facts; streams, rivers, fords, and canals; bridges; the board terrain the movement rules still refuse (gullies, crags, shellholes, graveyards, lumberyards, sunken and elevated roads, cliffs, and the like); marsh and brush as Open Ground in snow, frozen streams, and minefields in Deep Snow; paved road hexes named as such (backlog sections 20, 26, 29, and 30). | B6, B20, B21, B3, B19, E3.722, E3.73, overlays | 5:30 | none (0:00) | 5:30 | 6:45 |
| 41 | Terrain II | Bocage and hedges; cellars; Factories; sewers and Sewer Movement; an AFV entering a building; the Italian, Finnish, Iraqi, and New Zealand national rules, ANZAC Stealth included (backlog sections 15, 20, 21, and 27). | B9, B23.41, B23.74, B8, A25 | 3:05 | C, G1, G5, G8, HASL-A, G18, G21, T, DASL-B, G25, G30, DASL-C, G32, G34, G35, G36, G37 (4:00) | 7:05 | 8:20 |
| 42 | Special units | Cavalry and horses, skis, air drops and gliders, boats, and partisans as the cards field them; the Dutch and Polish national rules (backlog section 15). | A13, E4, E5, E8, E9, A25 | 3:40 | G7, O, G10, G11, G12, T12, T15, G17, G26, G39, G40 (2:30) | 6:10 | 7:25 |
| 43 | Fire | Kindling, spread, Blazes, and the smoke of fire; Control forfeited to a Kindled Fire; EC and wind as an SSR the game reads. | B24, B25, A26.16 | 3:00 | G2, DASL-A, T5, G15 (0:50) | 3:50 | 5:05 |

## 23. Schedule and duration

The migration follows pass 33, in this order, one pass at a time on the user's go-ahead:

| Pass | Title | Kind | Build | Total | Range (-30 % to +30 %) |
|---|---|---|---|---|---|
| D1 | Display: C, D | Display | 2:35 | 3:50 | 2:41 to 4:59 |
| D2 | Display: E, G1, F, G | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D3 | Display: G2, G3, DASL-A, H | Display | 2:25 | 3:40 | 2:34 to 4:46 |
| D4 | Display: T1, T2, T3, T4, G4 | Display | 2:30 | 3:45 | 2:38 to 4:52 |
| D5 | Display: G5, I, J, G6, K | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D6 | Display: L, G7, M, G8, G9, N | Display | 2:55 | 4:10 | 2:55 to 5:25 |
| D7 | Display: O, P, G10, Q, G11, HASL-A | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D8 | Display: G12, T6, T5, T7, T8 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D9 | Display: G13, T9, T10, T11, T12, G14 | Display | 3:00 | 4:15 | 2:58 to 5:32 |
| D10 | Display: R, G15, S, G16, T13, T15 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D11 | Display: T16, G17, G18, G19 | Display | 2:13 | 3:28 | 2:26 to 4:30 |
| D12 | Display: G20, G21, G22 | Display | 2:20 | 3:35 | 2:30 to 4:40 |
| D13 | Display: G23, G24, T, DASL-B | Display | 2:55 | 4:10 | 2:55 to 5:25 |
| D14 | Display: G25, G26, G27 | Display | 2:15 | 3:30 | 2:27 to 4:33 |
| D15 | Display: G29, G30, DASL-C, G31, G32 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D16 | Display: G33, G34, G35, G36 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| D17 | Display: G37, V, G38, G39, G40, G41 | Display | 2:51 | 4:06 | 2:52 to 5:20 |
| D18 | Display: G42, G43, G44, G45, G46 | Display | 2:45 | 4:00 | 2:48 to 5:12 |
| 34 | Armored combat I | Rules | 5:40 | 6:55 | 4:50 to 9:00 |
| 35 | Armored combat II | Rules | 7:30 | 8:45 | 6:08 to 11:22 |
| 35b | Night and winter | Rules | 3:10 | 4:25 | 3:06 to 5:44 |
| 36 | Fortifications I | Rules | 3:35 | 4:50 | 3:23 to 6:17 |
| 37 | Fortifications II | Rules | 4:55 | 6:10 | 4:19 to 8:01 |
| 38 | Offboard artillery | Rules | 3:30 | 4:45 | 3:20 to 6:10 |
| 39 | Air support | Rules | 6:20 | 7:35 | 5:18 to 9:52 |
| 40 | Terrain I | Rules | 5:30 | 6:45 | 4:44 to 8:46 |
| 41 | Terrain II | Rules | 7:05 | 8:20 | 5:50 to 10:50 |
| 42 | Special units | Rules | 6:10 | 7:25 | 5:12 to 9:38 |
| 43 | Fire | Rules | 3:50 | 5:05 | 3:34 to 6:36 |
| | **All migration passes** | | **105:44** | **141:59** | **99:23 to 184:35** |

The display batches total 70:59 and the rule passes 71:00. With passes 22b to 33 (113:20, after pass 30's task 30.6 and pass 30b were added on 2026-10-03 and pass 31 on 2026-10-04), the whole plan is 255:19, about 34 working days of 7.5 hours; at the recent pace (about 65 % of estimates) nearer 165:57.

**Order.** The display batches come first, so the whole portable corpus is readable early and the catalog work is done in one sweep. Armored combat is the first rule package because 49 of the playable cards need it; night and winter follows it, since several armor cards are night or winter cards; offboard artillery and terrain make the most cards playable once fortifications are in.

**Risks.** The package estimates are scale only; armored combat and offboard artillery are the largest rules areas the game has not touched, and either may need a third pass. The catalog was read from page images and its needs corrected by review, but a card may still need a package the catalog missed; its display batch records the change. That includes T1, T8, T16, and H, playable in their display batch only if their boards hold no terrain the movement rules refuse. Each display batch adds counters, so each carries a catalog version, with its effect on earlier games settled by D1's ruling.

# Appendix A. The legacy card catalog

The 89 distinct ASL scenario cards of *The General*, Vol. 22 to 32 (the Special Issue's reprint of H omitted), catalogued on 2026-09-30 from the magazine's pages and corrected by the reviews the same day. Facts only; titles as printed. "Needs" lists the rule packages of section 20 the card waits for; "Status" is where it is ported and when it becomes playable, why it stays display only, or why it is not portable.

| Id | Title | Issue | Date | Boards | Sides | Turns | Size | Needs | Status |
|---|---|---|---|---|---|---|---|---|---|
| A | The Guards Counterattack | Vol22i6 | 6 Oct 1942 | one board, hexrows A-P only (board number not legible in scan; likely board 1) | German vs Russian (Guards) | 5 | small | none | Ported (pass 17), playable |
| B | The Tractor Works | Vol22i6 | 6 Oct 1942 | one board, hexrows O-GG only (board number not legible in scan; likely board 1) | German (incl. assault engineers) vs Russian | 8 | medium | none | Ported (pass 17b), playable |
| C | The Streets of Stalingrad | Vol22i6 | 6 Oct 1942 (card header misprints 1944) | Combined layout of scenarios A and B | German vs Russian | as A and B (7 turns shown) | large | Armor, OBA-Air, Terrain | Display D1; playable after pass 41 (settled by its ruling, section 19) |
| D | The Hedgehog of Piepsk | Vol23i2 | 14 Nov 1941 | 4, 3, 2 (stacked N-S) | German vs Russian | 10 | medium | OBA-Air | Display D1; playable after pass 39 |
| E | Hill 621 | Vol23i2 | 1 Jul 1944 | 2, 4, 3 (side by side E-W) | German vs Russian (Guards) | 10 | large | Armor, OBA-Air | Display D2; playable after pass 39 |
| G1 | Timoshenko's Attack | Vol23i3 | 12 Jul 1941 | 22, 11, 10 (side by side, separate lanes) | German vs Russian | 9 | large | Armor, OBA-Air, Terrain | Display D2; playable after pass 41 |
| F | The Paw of the Tiger | Vol23i5 | 12 Jan 1943 | 2, 4, 5 (side by side E-W) | German vs Russian | 10 | small-medium | Armor, OBA-Air, Fortifications | Display D2; playable after pass 39 |
| G | Hube's Pocket | Vol23i5 | 6 Apr 1944 | 4, 2, 5 (stacked N-S) | German (SS) vs Russian | 10 (END mark at 4 with restart option to turn 11) | medium | Armor | Display D2; playable after pass 35 |
| G2 | Last Act in Lorraine | Vol23i6 | 6 Dec 1944 | 12, 17 (stacked N-S) | German vs American | 10 | medium | Armor, OBA-Air, Fire | Display D3; playable after pass 43 |
| G3 | The Forgotten Front | Vol23i6 | 9 Feb 1945 | 19, 12 (stacked N-S) | German vs American | 8 | medium | Armor, Fortifications | Display D3; playable after pass 37 |
| DASL-A | To the Last Man | Vol24i1 | 13 Jan 1945 | Deluxe boards a, b, c, d | German (SS cavalry) vs Russian (Guards) | 8 | large | Armor, OBA-Air, Fortifications, Fire | Display D3; playable after pass 43 |
| H | Escape from Velikiye Luki | Vol24i1 | 12 Jan 1943 | 4, 2, 3 (side by side E-W) | German vs Russian | 10 (last turn conditional) | small | none | Display D3; playable in D3 (settled by its ruling, section 19) |
| T1 | Gavin Take | Vol24i2 | 6 Jun 1944 | 3 | German vs American (82nd Airborne) | 6 | small | none | Display D4; playable in D4 |
| T2 | The Puma Prowls | Vol24i2 | 28 Jun 1944 | 22, 4 (stacked N-S) | German vs Russian | 6 | small | Armor | Display D4; playable after pass 35 |
| T3 | Ranger Stronghold | Vol24i2 | 14 Sep 1943 | 2 | German vs American (Rangers) | 6 | small | Armor | Display D4; playable after pass 35 |
| T4 | Shklov's Labors Lost | Vol24i2 | 11 Jul 1941 | 1 | German (Grossdeutschland) vs Russian (officer cadets) | 6 | small | Armor | Display D4; playable after pass 35 |
| G4 | First Action | Vol24i3 | 8 Nov 1944 | 12, 18, 19 (side by side E-W) | German vs American (761st Tank Bn) | 11 (last turn conditional) | medium | Armor, OBA-Air, Fortifications | Display D4; playable after pass 39 |
| G5 | Six Came Back | Vol24i3 | 30 Jan 1944 | 12, 17, 16 (stacked N-S) | German (Hermann Goering, 26th Pz) vs American (Rangers) | 9 | medium | Armor, Terrain, Night and winter | Display D5; playable after pass 41 |
| I | Buchholz Station | Vol24i4 | 16 Dec 1944 | 4, 3 (stacked N-S) | German (Volksgrenadier) vs American | 10 | medium | Armor | Display D5; playable after pass 35 |
| J | The Bitche Salient | Vol24i4 | 14 Jan 1945 | 4, 2 (side by side E-W) | German (Gebirgsjaeger) vs American | 10 | large | Armor, OBA-Air, Fortifications | Display D5; playable after pass 39 |
| G6 | Rocket's Red Glare | Vol24i6 | 22 Dec 1944 | 3 | German (SS) vs American (504th Parachute) | 6 | small | Armor | Display D5; playable after pass 35 |
| K | The Cannes Strongpoint | Vol25i2 | 23 Aug 1944 | 4, 2, 3 (side by side E-W) | German vs American (509th Parachute) | 5 | medium | Armor, OBA-Air, Fortifications | Display D5; playable after pass 39 |
| L | Hitdorf on the Rhine | Vol25i2 | 6 Apr 1945 | 4, 3 (side by side E-W) | German (Volksgrenadier, 11th Pz) vs American (504th Parachute) | 9 | medium | Armor, OBA-Air, Fortifications | Display D6; playable after pass 39 |
| G7 | Bring Up the Guns | Vol25i3 | 10 May 1940 | 4, 33 (stacked N-S) | German (cavalry) vs Dutch | 7 | small | Fortifications, Special units | Display D6; playable after pass 42 |
| M | First Crisis at Army Group North | Vol25i3 | 25 Jun 1941 | 6, 4 (stacked N-S) | German vs Russian | 7 | small | Armor | Display D6; playable after pass 35 |
| G8 | Recon in Force | Vol25i5 | 16 Jul 1943 | 22, 21 (stacked N-S) | Italian and German (Axis) vs American (Rangers) | 7 | medium | Terrain | Display D6; playable after pass 41 |
| G9 | Sunday of the Dead | Vol25i6 | 23 Nov 1941 | 27, 28, 26, 29 (desert boards, side by side) | German vs British (South African) | 9 | large | Armor, OBA-Air, Chapter F or G | Display D6; display only: Chapters F and G |
| N | Soldiers of Destruction | Vol25i6 | 10 Oct 1944 | 6, 4 (side by side E-W) | German (SS Totenkopf) vs Russian (Guards) | 7 | small | Armor | Display D6; playable after pass 35 |
| O | The St. Goar Assault | Vol26i1 | 24 Mar 1945 | 2, 1, 4, 3 (side by side E-W) | German (Wehrkreis XIII) vs American (87th Inf Div) | 8 (last turn conditional) | large | Armor, Terrain, Special units, Night and winter | Display D7; playable after pass 42 |
| P | The Road to Wiltz | Vol26i1 | 18 Dec 1944 | 3, 2 (side by side E-W) | German (Volksgrenadier, Panzer Lehr) vs American (engineers, 707th Tank Bn) | 5, with restart to 10 (restart on turn 11 option) | large | Armor, OBA-Air, Fortifications | Display D7; playable after pass 39 |
| G10 | Grab at Gribovo | Vol26i2 | 3 Jan 1942 | 18, 12, 4 (stacked N-S) | German vs Russian (paratroops) | 8 | small | Special units, Night and winter | Display D7; playable after pass 42 |
| Q | Land Leviathans | Vol26i2 | 3 Jul 1941 | 2, 3, 4 (stacked N-S) | German vs Russian | 7 | small | Armor | Display D7; playable after pass 35 |
| G11 | Pegasus Bridge | Vol26i5 | 6 Jun 1944 | 23 | German vs British (6th Airborne, Ox and Bucks) | 5 | small | Fortifications, Terrain, Special units | Display D7; playable after pass 42 |
| HASL-A | Ghosts in the Rubble | Vol27i1 | 31 October 1942 | RB map (only hexes numbered >= 38 on/east of hexrow U) | German (Pz Div 14) vs Russian (45th Rifle Div) | 7 | medium | OBA-Air, Fortifications, Terrain | Display D7; playable after pass 41 |
| G12 | Avalanche! | Vol27i1 | 6 February 1943 | 9, 15 | German (Gebirgsjaeger Div 1) vs Russian (318th Mountain Rifle Div) | 10 | large | Fortifications, Special units, Night and winter | Display D8; playable after pass 42 |
| T6 | The Dead of Winter | Vol27i2 | 29 December 1941 | 4 (hexrows R-GG only) | German (IR 18) vs Russian (Siberians, 31st Army) | 5 | small | Armor, Fortifications | Display D8; playable after pass 37 |
| T5 | The Pouppeville Exit | Vol27i2 | 6 June 1944 | 3, 5 | German (IR 1058) vs American (501st Parachute Rgt) | 8 | small | Armor, Terrain, Fire | Display D8; playable after pass 43 |
| T7 | Hill 253.5 | Vol27i3 | 9 July 1943 | 2 | German (Pz Div 18, Pzjg Abt 653) vs Russian (307th Rifle Div, mech brigade) | 8 | large | Armor, OBA-Air, Fortifications | Display D8; playable after pass 39 |
| T8 | Aachen's Pall | Vol27i3 | 15 October 1944 | 1 (hexrows A-P only) | German (Aachen HQ) vs American (26th Inf Rgt) | 4 | small | none | Display D8; playable in D8 |
| G13 | A View from the Top | Vol27i5 | 23 February 1945 | 25, 15 (hexrows R-GG on 15) | German (ID 334) vs American (86th Mountain Inf Rgt) | 8 | medium | OBA-Air, Fortifications, Special units, Night and winter | Display D9; display only: desert boards (Chapter F) |
| T9 | The Niscemi-Biscari Highway | Vol28i1 | 10 July 1943 | 4, 5 (hexrows Q-GG) | American (504th Parachute Rgt) vs German (Hermann Goering Div recon) | 8 | small | Armor, Fortifications | Display D9; playable after pass 37 |
| T10 | Devil's Hill | Vol28i1 | 19 September 1944 | 2, 5 | German (Landesschuetzen Div 406) vs American (508th Parachute Rgt) | 7 | small | Armor, Fortifications | Display D9; playable after pass 37 |
| T11 | The Attempt to Relieve Peiper | Vol28i2 | 21 December 1944 | 2, 5 | American (505th Parachute Rgt) vs German (1st SS Pz Div) | 10 | medium | Armor, OBA-Air, Fortifications | Display D9; playable after pass 39 |
| T12 | Hunters from the Sky | Vol28i2 | 24 March 1945 | 5, 4, 2 | German (ID 84) vs American (513th Parachute Rgt) | 10 | medium | Armor, Special units | Display D9; playable after pass 42 |
| G14 | Tiger, Tiger | Vol28i3 | 11 February 1943 | 11, 18, 33, 17 | Russian (46th Tank Bde) vs German (s.Pz.Abt 502) | 7 | medium | Armor | Display D9; playable after pass 35 |
| R | Burzevo | Vol28i3 | 2 December 1941 | 3 | German (IR 478) vs Russian (20th Tank Bde) | 5 | small | Armor, Night and winter | Display D10; playable after pass 35b |
| G15 | Bone of Contention | Vol28i4 | 31 August 1944 | 21, 20 | Free French partisans (Maquis) vs German (SS-Pz Abt 102 tank crews) | 7 | small | Armor, Fire, Special units | Display D10; playable after pass 43 |
| S | The Whirlwind | Vol28i4 | 18 April 1945 | 5, 10 | American (26th Inf Div) vs German (Ersatz Div 471) | 7 | small | Armor | Display D10; playable after pass 35 |
| G16 | Alligator Creek | Vol28i5 | 21 August 1942 | 32, 34 (R-GG) plus overlays Be4, Be6, Ef1, Oc1, Oc2 | American (1st Marine Rgt) vs Japanese (28th Inf Rgt, Ichiki detachment) | 7 | large | OBA-Air, Fortifications, Terrain, Chapter F or G | Display D10; display only: Chapters F and G |
| T13 | Commando Raid at Dieppe | Vol28i6 | 18 August 1942 | 4, 5 | German (302nd ID, Battery Hess) vs British (No. 4 Commando) with US Rangers | 10 | medium | Fortifications | Display D10; playable after pass 37 |
| T14 | Gambit | Vol28i6 | 21 May 1941 | 4, 2 | New Zealand (22nd Bn) vs German (Sturm Rgt 1) | 8 | small | none | Ported (pass 17), playable |
| T15 | The Akrotiri Peninsula | Vol29i1 | 20 May 1941 | 3, 6, 4 | British (102nd AT Rgt, 151st AA Bty) vs German (Sturm Rgt 1, glider) | 9 | medium | Special units | Display D10; playable after pass 42 |
| T16 | Strayer's Strays | Vol29i1 | 6 June 1944 | 6 | German (IR 919) vs American (mixed 502/506/508 PIR) | 4 | small | none | Display D11; playable in D11 |
| G17 | Hakkaa Paalle | Vol29i2 | 12 January 1940 | 16, 19, 32, 34 (R-GG on 32/34 not playable), overlay O5 | Russian (2nd Ski Bde) vs Finnish (9th Sissi Co) | 10 | medium | Terrain, Special units, Night and winter | Display D11; playable after pass 42 |
| G18 | Goya | Vol29i2 | 7 January 1945 | 39, 16, 23 | German (VGR 183) vs American (551st PIB) | 11 | large | Terrain, Night and winter | Display D11; playable after pass 41 |
| G19 | A Tough Nut to Crack | Vol29i3 | 1 January 1943 | 37, 35 plus overlays X13, X14 | Japanese (19th Army remnants) vs Australian (2/9th Bn, 2/6th Armoured Rgt) | 9 | medium-large | Armor, OBA-Air, Fortifications, Terrain, Chapter F or G | Display D11; display only: Chapters F and G |
| G20 | Camp Nibeiwa | Vol29i3 | 9 December 1940 | 30, 31 (Q-GG not playable), 29, 28, overlays D1, D5, D6, H3, H6, S2, S6 | Italian (Gruppo Maletti) vs British (4th Indian Div, 7th RTR) | 10 | large | Armor, Fortifications, Terrain, Chapter F or G | Display D12; display only: Chapters F and G |
| G21 | Cat's Kill | Vol29i4 | 8 June 1944 | 20, 10 plus overlays O1-O5 | Canadian (Regina Rifles) vs German (12. SS Pz Div Hitlerjugend) | 10 | large | Armor, Terrain, Night and winter | Display D12; playable after pass 41 |
| G22 | A Day by the Shore | Vol29i4 | 7 June 1944 | 8, 20, 4 | German (GR 736) vs British (45 Royal Marine Commando) | 10 | large | OBA-Air, Fortifications, Terrain | Display D12; display only: beaches (Chapter G, G13) |
| G23 | Habbaniya Heights | Vol29i5 | 5 May 1941 | 29, 25 plus overlay E1 | Iraqi (2nd Iraqi Legion) vs British (King's Own Royal Rgt) | 7 | large | Armor, Fortifications, Terrain, Night and winter | Display D13; display only: desert boards (Chapter F) |
| G24 | Mountain Comes to Mohammed | Vol29i5 | 25 August 1941 | 28, 26 plus overlays S3, S5, S8, X4, D6, H6 | Iranian (Pahlavi Guards) vs British (2/11th Sikh, 13th Lancers) | 8 | large | Armor, Fortifications, Terrain, Chapter F or G | Display D13; display only: Chapters F and G |
| T | Pavlov's House | Vol29i6 | 20 October 1942 | 1 plus overlays OG1, OG4, OG5 | Russian (42nd Guards Rifle Rgt) vs German (6. Armee Sturmgruppe) | 7 | small | Armor, OBA-Air, Terrain | Display D13; playable after pass 41 |
| DASL-B | The Kiwis Attack | Vol29i6 | 15 March 1944 | Deluxe boards q, p | German (FJR 3) vs New Zealand (25th Bn, 19th Armd Rgt) | 7 | large | Armor, Fortifications, Terrain | Display D13; playable after pass 41 |
| G25 | The T-Patchers | Vol30i1 | 15 December 1943 | 41, 11 (R-GG) plus overlays X12, X13, X14 | German (PzGr Div 29) vs American (143rd Inf Rgt, 753rd Tank Bn) | 7 | medium | Armor, Fortifications, Terrain | Display D14; playable after pass 41 |
| G26 | Parker's Crossroads | Vol30i1 | 23 December 1944 | 42, 19, 33, 43, 35, 16 (parts unplayable) | American (mixed: 589th FA Bn, 3rd Armd Div, 325th GIR) vs German (2nd SS Pz Div Das Reich) | 10 | large | Armor, OBA-Air, Fortifications, Special units | Display D14; playable after pass 42 |
| G27 | Vaagso Venture | Vol30i2 | 27 December 1941 | 41 (R-GG), 8 (A-P) | German (ID 181) vs British (2, 3 and 6 Commandos) | 9 | large | Armor, OBA-Air, Fortifications, Terrain, Fire | Display D14; display only: a seaborne assault with landing craft (Chapter G, G14) |
| G28 | Ramsey's Charge | Vol30i3 | 16 January 1942 | 40, 35 plus overlays 1, X6, OG1, OG2, OG3, OG5 | Japanese (14th Army) vs American (26th Cavalry, Philippine Scouts) | 7 | small-medium | Terrain, Special units, Chapter F or G | Not portable: names board 25, which is not in its board layout |
| G29 | Shoot-N-Scoot | Vol30i4 | 18 December 1944 | 12, 42 | American (38th Inf Rgt, 644th TD Bn, 741st Tank Bn) vs German (12th SS Pz Div) | 9 | large | Armor, OBA-Air, Fortifications | Display D15; playable after pass 39 |
| G30 | Morgan's Stand | Vol30i4 | 11 September 1944 | 10, 7 (R-GG) | American (53rd Armd Inf Bn) vs German (Fusilier Rgt 312) | 7 | small-medium | Armor, Terrain | Display D15; playable after pass 41 |
| DASL-C | Smoke the Kents! | Vol30i5 | 20 May 1940 | Deluxe boards g, d, h, e plus overlays dx2, dx4, dx6, dx7 | British (7th Royal West Kent) vs German (Pz Div 1) | 8 | medium | Armor, Terrain | Display D15; playable after pass 41 |
| U | Chance d'Une Affaire | Vol30i5 | 14 May 1940 | 9 (rotated label; possibly 6) | German (IR 1, Pz Div 1, Sturmpionier Bn 43) vs French (213e RI, 7e BCC) | 9 | medium | Armor | Not portable: its board number is printed rotated and cannot be read with certainty |
| G31 | Point of the Sword | Vol30i6 | 6 June 1944 | 10 | German (Pz Div 21) vs British (4th Brigade Commandos) and Canadian (Regiment de la Chaudiere) | 8 | medium | Fortifications | Display D15; playable after pass 37 |
| G32 | A Helping Hand | Vol30i6 | 13 June 1944 | 11, 2 (A-P) | German (VGR 914, FJR 8) vs American (38th Inf Rgt) | 10 | medium | OBA-Air, Fortifications, Terrain | Display D15; playable after pass 41 |
| G33 | The Awakening of Spring | Vol31i1 | 14 March 1945 | 11, 16, 17, 4 | Russian (35th Guards Rifle Corps, 23rd Tank Corps) vs German (SS Pz Div 6) | 10 | large | Armor, Night and winter | Display D16; playable after pass 35b |
| G34 | The Liberators | Vol31i1 | 2 April 1945 | 22, 3 plus overlays X13, X16, X17, St3 | German (SS-Pz Korps 1) vs Russian (IX Guards Mech Corps) | 9 | large | Armor, OBA-Air, Terrain | Display D16; playable after pass 41 |
| G35 | Going to Church | Vol31i2 | 1 August 1944 | 23 (A-P) | German (SS Pz Div 9) vs Canadian (Les Fusiliers Mont-Royal) | 6 | small | Terrain | Display D16; playable after pass 41 |
| G36 | Hill of Death | Vol31i2 | 11 July 1944 | 33, 9, 16 | German (SS Pz Divs 9 and 10, s.SS-Pz Abt 102) vs British (5th DCLI, 7th RTR) | 8 | large | Armor, OBA-Air, Fortifications, Terrain | Display D16; playable after pass 41 |
| G37 | Forth Bridge | Vol31i3 | 11 April 1945 | 17, 18 plus overlays OG5, Wd4, B2, B3, St1, St3 | German (FJ Div 7) vs British (2nd Gordon Highlanders, 3rd Scots Guards) | 9 | medium-large | Armor, Fortifications, Terrain | Display D17; playable after pass 41 |
| V | Auld Lang Syne | Vol31i3 | 1 January 1945 | 10 (R-GG), 1 (A-P) | American (345th Inf Rgt) vs German (PzGr Div 15) | 6 | small-medium | OBA-Air | Display D17; playable after pass 39 |
| G38 | Castello Fatato | Vol31i4 | 20 December 1942 | 27, 28 (R-GG) plus overlays X7-X15, X18 | Russian (1st Guards Army) vs Italian (3rd Bersaglieri Rgt) | 7 | medium | Terrain, Fire, Night and winter | Display D17; display only: desert boards (Chapter F) |
| G39 | A Desperate Affair | Vol31i4 | 20 May 1941 | 9, 33, 16, 18 | British (2nd Black Watch) vs German (FJR 1) | 8 | medium-large | Fortifications, Special units | Display D17; playable after pass 42 |
| G40 | Will to Fight...Eradicated | Vol31i5 | 7 September 1939 | 12 (hexrows K-Y) plus overlays G1, G2, G4, Wd5, X7, X9 | German (LSSAH) vs Polish (2nd Kaniov Rifle Rgt, dismounted cavalry) | 7 | medium | Terrain, Special units | Display D17; playable after pass 42 |
| G41 | JABO! | Vol31i5 | 18 December 1944 | KGP map (hexes <= 14 in rows AA-TT) | German (Kampfgruppe Peiper) vs American (IX Tactical Air Command aircraft) | 3 | small | Armor, OBA-Air | Display D17; display only: the KGP historical map, not among the VASL boards in scope |
| G42 | The Youth's First Blood | Vol31i6 | 7 June 1944 | 16, 10, 33 | German (ID 716, 12. SS Pz Div) vs Canadian (North Nova Scotia Highlanders, Sherbrooke Fusiliers) | 9 | large | Armor, OBA-Air | Display D18; playable after pass 39 |
| G43 | Kangaroo Hop | Vol31i6 | 17 September 1944 | 2 | German (Bodenstaendige Div 326) vs Canadian (North Nova Scotia Highlanders, Fort Garry Horse, RE) | 6 | medium | Armor, OBA-Air, Fortifications | Display D18; playable after pass 39 |
| G44 | Abandon Ship! | Vol32i2 | 19 December 1944 | 24 | American (501st PIR) vs German (Pz Lehr, PGR 902) | 7 | small-medium | Armor, Fortifications | Display D18; playable after pass 37 |
| G45 | Halha River Bridge | Vol32i2 | 8 July 1939 | 26, 27 (A-P) plus overlays St1, St3, SD2, SD6 | Russian (149th Motorized Rifle Rgt) vs Japanese (4th Tank Rgt, 72nd Inf Rgt) | 6 | small | Armor, Fortifications, Terrain, Chapter F or G | Display D18; display only: Chapters F and G |
| G46 | Triumph atop Taraldsvikfjell | Vol32i3 | 28 May 1940 | 2 | German (Gebirgsjaeger Rgt 139 and naval personnel) vs Allied (French Foreign Legion, Norwegian 2/15th Bn) | 7 | medium | Fortifications | Display D18; playable after pass 37 |
| W | The Defense of Luga | Vol32i3 | 19 July 1941 | 4, 3, 5, 1 | German (IR 469, ID 269) vs Russian (Operational Group Luga) | 10 (turn chart also marks END at 4 and a restart; unclear) | large | Armor | Not portable: its Turn Record Chart shows both END after Turn 4 and a restart box |
