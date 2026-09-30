# ASL Card Play and Map Studio Redesign Plan

**Status:** Draft for the user's approval, 2026-09-30. It merges and replaces two documents: the ASL Unit Card Play, Deferred Rules, and DYO Plan (approved 2026-09-30, passes 23 to 30) and the ASL Map Studio UI Redesign 01 Design (branch `UI-Redesign-01`, commit `800416a`). Their content is carried over here in full, and both were then deleted; git history keeps them. New for approval: the Studio passes 22b, 22c, 22d, 28b, and 28c, the component tasks added to passes 23 to 29, and the merged order. No pass has started.

**Date:** 2026-09-30

**Scope:** thirteen passes in one order. Eight game passes, 23 to 30, close three groups of open backlog rows: card-driven play (backlog sections 27 to 32), rules deferred by earlier passes (entry and exit, the Heat of Battle, Leader Creation, and berserk gaps), and the Chapter H DYO purchase, which generates a scenario card. Five Studio passes, 22b to 22d, 28b, and 28c, carry out the Map Studio redesign: the supplied theme, grouped navigation, shared components, every page's markup extracted into Blazor components, and the map-centered Play workspace. The game passes also extract the Play components they change.

Section 1 of the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>) (how a pass is run, the standing rules, and the merge gate) applies unchanged to every pass here.

**Related documents:** the [ASL Unit Backlog](<ASL Unit Backlog.md>) (sections 27 to 32); the designs and reviews of passes 17 to 22; the [ASL Unit Deviations, Ordnance, and Vehicles Plan](<ASL Unit Deviations, Ordnance, and Vehicles Plan.md>), whose ordnance and vehicle passes the game passes build on; the [Map Studio Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Requirements.md>), the [Architecture and Rendering Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Studio Architecture and Rendering Design.md>), the [Unit Display Design](<ASL Unit Display Design.md>), the [Governed Writes Design](<ASL Unit Governed Writes Design.md>), and the [Unit Read Contract Design](<ASL Unit Read Contract Design.md>).

**Contents:** Part I, direction and decisions (sections 1 to 3). Part II, the passes (sections 4 to 8). Part III, the Studio design (sections 9 to 15). Part IV, the component inventory (section 16). Part V, acceptance (section 17) and rulings (section 18).

# Part I. Direction and decisions

## 1. Outcome

**The game.** Every game stays a card. The game passes make a card's play fuller (per-side views and hidden setup, Control of Locations, Gun and vehicle VP, vehicles and Guns from off board), close the rules earlier passes deferred (entry and exit, the Heat of Battle, Leader Creation, and berserk gaps), and add the Chapter H DYO purchase, which ends in a saved user card that the Play page starts like any other.

**The Studio.** The Studio makes the next useful task apparent while keeping the board, the current selection, and the result of an action understandable. It gets a consistent application shell with two page families: collection pages for finding things and workspaces for manipulating or inspecting them. Every page's markup becomes Blazor components with narrow contracts, and Play becomes a map-centered workspace. The supplied `ASL Map Studio UX Audit & Redesign PDF` (one page, 28 Sep 2026) and its stylesheet are design inputs supplied by the user, evaluated here rather than treated as instructions; the originals stay outside the repository.

## 2. Principles

**One path into a game.** The DYO purchase ends in a user card, so there is no second path into a game.

**Rules first, then tools.** The deferred rules (passes 25 and 27) come before the editor's forms (pass 28), so the forms write fields that play already reads.

**Only what the game plays is purchasable.** A DYO purchase of something the game has no rule for (OBA, Air Support, fortifications, boats, gliders, horses) is refused with its backlog row, never accepted and then ignored.

**Missing values are manufactured, never blocking.** Counter values no registered source prints (for example, Axis Minor or Japanese counters for their Heat of Battle exceptions) are manufactured under R0.3 on sheet MFG.

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

1. **Hidden setup in a hot-seat game (pass 23): a hand-over screen.** The Studio blanks the map and the side panels between sides until the next side confirms it is at the screen; there is no separate tab per side.
2. **Scope of group 2: the berserk gaps stay.** Tasks 27.2 and 27.3 remain in pass 27.
3. **DYO nationalities (passes 29 and 30): German and Russian first.** The DYO page offers the two nationalities the cards use; the other nationalities the catalog carries, with manufactured counters where needed, go to the backlog when pass 29 is built.
4. **Order: as numbered.** The game passes run 23 to 30, so each builds on the one before and the editor forms (pass 28) follow the rules they edit. This merged plan keeps that order and inserts the Studio passes around it (section 4); the insertion needs the user's approval.
5. **Autonomy: one pass at a time.** It applies to every pass here, the Studio passes included. Each pass starts only on the user's go-ahead and stops after its merge, with its times reported; a rule question that changes a pass's scope, or a failure that needs a design change, still stops the pass. Passes 26, 29, and 30 reach into areas not built before (vehicles from off board, Chapter H), which is why they are not run unattended.

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
| 22b | The theme and the shared foundation | Studio | 4 | 3:00 | 4:15 | 2:58 to 5:32 |
| 22c | The collection pages | Studio | 4 | 3:45 | 5:00 | 3:30 to 6:30 |
| 22d | The workspaces | Studio | 4 | 4:30 | 5:45 | 4:01 to 7:28 |
| 23 | Per-side views and hidden setup | Game | 5 | 3:55 | 5:10 | 3:37 to 6:43 |
| 24 | Control of Locations and more VP | Game | 5 | 2:10 | 3:25 | 2:24 to 4:26 |
| 25 | Entry and exit | Game | 6 | 3:50 | 5:05 | 3:34 to 6:36 |
| 26 | Vehicles and Guns on a card | Game | 5 | 3:15 | 4:30 | 3:09 to 5:51 |
| 27 | Heat of Battle, Leader Creation, and berserk gaps | Game | 5 | 4:00 | 5:15 | 3:40 to 6:50 |
| 28 | The card editor's forms and map picking | Game | 5 | 3:30 | 4:45 | 3:20 to 6:10 |
| 28b | The rest of Play | Studio | 4 | 2:45 | 4:00 | 2:48 to 5:12 |
| 28c | The Play workspace and hardening | Studio | 3 | 4:00 | 5:15 | 3:40 to 6:50 |
| 29 | DYO purchase I: Infantry, leaders, and SW | Game | 5 | 3:15 | 4:30 | 3:09 to 5:51 |
| 30 | DYO purchase II: ordnance, vehicles, and conditions | Game | 4 | 2:30 | 3:45 | 2:38 to 4:52 |
| | **All passes** | | **59** | **44:25** | **60:40** | **42:28 to 78:52** |

**Order.** The passes run in the order listed, one at a time on the user's go-ahead.

- **22b first.** The theme ends the split between main's app.css and the branch's site.css, which each game pass would otherwise widen, and the shared components (navigation, page header, field groups, findings, feedback, the perspective and card pickers) are reused by pass 23's side views and pass 28's forms.
- **22c and 22d next.** No game pass touches the collection pages or the workspaces, so they convert without conflicts. Both can move later if the game work is wanted sooner, but 22d must precede pass 28, whose map picking uses 22d's board viewport.
- **23 to 28 in the game plan's order,** each also extracting the components it changes (tasks 23.5, 24.5, 25.6, 26.5, 27.5, and 28.5; 29.5 builds the DYO page from components): the side's view is an input to every Play component from pass 23 on.
- **28b and 28c after pass 28,** when Play's content has settled: the Play panels no game pass touched, then the map-centered layout, responsive behavior, and hardening.
- **29 and 30 last,** the DYO page built from the shared components from the start.

**Separable groups.** The game passes alone are 23 to 30, without their component tasks; within them, card play alone is passes 23, 24, 26, and 28 (pass 26 then takes pass 25's entry tasks it needs), the deferred rules alone are passes 25 and 27, and DYO needs pass 28 only for opening a DYO card in forms, so it could follow pass 22 directly with the JSON editor. The Studio passes alone are 22b, 22c, 22d, 28b, and 28c; without the game passes, the card pages' components of task 28.5 move into 22c and the Play components of tasks 23.5 to 27.5 into 28b, which then follows 22d.

## 5. The passes

### Pass 22b: The theme and the shared foundation

**Purpose:** the Studio takes the theme, grouped navigation, and the shared components every later pass reuses. **After:** pass 22. **From:** sections 10, 11, and 15; the component candidates of section 16.3.

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 22b.1 The theme merged | Branch `UI-Redesign-01` merged: site.css with the supplied theme, the local fonts, and the viewport's data attributes; app.css retired, with any rule main has added since moved into site.css in the theme's tokens. | | 0:20 |
| 22b.2 Shell and navigation | S01 `StudioNavigation` (Boards, Maps, Units, Scenarios, Play, Verify, Settings; route-aware current page; collapsible; a drawer when narrow) and S02 `PageHeader` on every page; the source-derived draft restriction as a warning banner (section 11.2). | | 1:00 |
| 22b.3 Shared components | S03 `SourceRequiredNotice`, S04 `OperationFeedback`, S05 `StatusBadge`, S06 `FindingList`, S07 `FieldGroup`, S09 `PerspectivePicker`, and S10 `RevisionNavigator`, each with focused tests and the contracts of section 15. | | 1:10 |
| 22b.4 The card picker and the new game | K02 `CardPicker` on Play, Scenarios, and the card editor; K01 `NewGameFromCard` with K03 `CardStartSummary` and K04 `BalanceChoice`. | R22.4 | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 22b total** (build 3:00) | | **4:15** |

### Pass 22c: The collection pages

**Purpose:** the collection pages become components, and the Board library can be searched. **After:** pass 22b. **From:** sections 12.1 and 12.2; the candidates of sections 16.4, 16.5, 16.8, and 16.9. No game pass touches these pages.

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
| 23.3 HIP by SSR | An SSR token names the units that set up HIP; they are recorded hidden and revealed by the A12.3 triggers already used for concealment loss. | A12.3, A12.33, A12.34 | 0:40 |
| 23.4 The non-OB "?" | After both sides have set up, each side places "?" on units out of the enemy's LOS or at least 17 hexes away, checked against the board's LOS. | A12.12 | 0:25 |
| 23.5 Play context and review components | First, page tests that pin the Victory standing (`#play-victory`, `data-control`, `data-side`) in a side's and the adjudicator's view, which no test covers yet. Then P01 `ScriptedDicePanel`, P02 `LiveGameToolbar`, P07 `GameReplayFailure`, N02 `NightWeatherSummary`, R01 to R03 (the proposal review and its evidence), R12 `PlayUnitTable`, R13 `AdjudicatorAuditPanel`, K05 `SetupPoolsTable`, and K06 to K08 (the card panel, the game's end, the Victory standing), each receiving the side's view; the board link at Play:63 follows the view (section 15.4). | A26.15 | 1:25 |
| | Overhead | | 1:15 |
| | **Pass 23 total** (build 3:55) | | **5:10** |

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
| | Overhead | | 1:15 |
| | **Pass 25 total** (build 3:50) | | **5:05** |

### Pass 26: Vehicles and Guns on a card

**Purpose:** a card may field vehicles and Guns from setup to exit. **After:** pass 25 (entry and exit rules). **From:** backlog sections 29, 30, and 31.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 26.1 Vehicles entering | Vehicles enter from off board in Motion, loaded with their Passengers; Guns enter limbered and towed. | A2.52, D2.4, C10.1 | 0:50 |
| 26.2 Guns at setup | A Gun and its manning crew or HS set up together; the crew stacks as its own size. | A5.5, C10 | 0:25 |
| 26.3 Crews leaving with their Gun | A crew with its Gun leaves the map; the Exit VP count the Gun. | A2.6, C10.3, A26.23 | 0:20 |
| 26.4 A test card with armor | A manufactured test card (R0.3) fielding a halftrack, a tank, and a Gun, played end to end by the table player. | R0.3 | 0:40 |
| 26.5 Setup and vehicle components | P08 `SetupPlacementEditor` and P09 `SetupPlacementList` (Guns and crews, off-board placement, OB groups), A17 to A22 (vehicle movement, towing, Bounding Fire, CE/BU), C18 `GunArcAction`, and S13 `FacingPicker`. | | 1:00 |
| | Overhead | | 1:15 |
| | **Pass 26 total** (build 3:15) | | **4:30** |

### Pass 27: Heat of Battle, Leader Creation, and berserk gaps

**Purpose:** the recorded gaps in A15 close. **After:** pass 26 (charges at vehicles need vehicles on a card). **From:** the backlog rows on berserk charges, Heat of Battle, and owner's options (sections 1, 11, 20 to 22, and 25).

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 27.1 Nationality exceptions | The Axis Minor and Japanese Heat of Battle and Leader Creation exceptions, with manufactured counters where no source prints them (R0.3). | A15 table notes, p. 83; A25 | 0:40 |
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
| 28.4 Card management | Renaming a user card; a confirmation before deleting; a warning when a live game uses the card (a game-to-card index); the Guards card's stale SSR 3 note revised. | R22.2 | 0:30 |
| 28.5 The card pages' components | K10 to K12 (the card, read only) and K13 to K20 (the editor), the forms of 28.1 built from S07 `FieldGroup` and the map picking of 28.2 on B06 `BoardViewport`; the Source label's association is fixed (`for="edit-basis"` against the input `#edit-source-basis`). | | 0:45 |
| | Overhead | | 1:15 |
| | **Pass 28 total** (build 3:30) | | **4:45** |

### Pass 28b: The rest of Play

**Purpose:** the Play panels no game pass touched become components. **After:** pass 28, when Play's content has settled. **From:** section 13; the remaining candidates of sections 16.10 to 16.13 and 16.15.

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 28b.1 Rally, Rout, and support weapons | A02 to A10: Rout and its obligations, support weapons, Rally, repair, Deployment, Recombination, DM retention, and Shock recovery, with their resets (section 15.3). | | 1:15 |
| 28b.2 Ordnance and Starshells | C16 `OrdnanceFirePanel`, C17 `OrdnanceTargetFields`, and N13 `StarshellActionPanel`. | | 0:30 |
| 28b.3 Activity records | R04 to R10: dice, the action records (night, Sniper, ordnance, Close Combat, Rally), the fire history and its cards, the arithmetic, and the effects tables, each keeping its disclosure filter. | | 0:45 |
| 28b.4 Rule help | S08 `RuleHelp` for the long rule paragraphs across Play. | | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 28b total** (build 2:45) | | **4:00** |

### Pass 28c: The Play workspace and hardening

**Purpose:** Play becomes the map-centered workspace, and every page meets the accessibility and responsive targets. **After:** pass 28b. **From:** sections 11.1, 13.1, and 14; R11.

| Task | What it changes | Rules | Estimate |
|---|---|---|---|
| 28c.1 The map-centered workspace | R11 `PlayMapPanel` in the middle; the context header (game, view, turn and phase, revision, live or replay); actions by phase; the task pane; the activity strip; a pending proposal always reachable (section 13.1). | | 1:30 |
| 28c.2 Responsive layouts | The page families and breakpoints of section 11.1: 22b.2's navigation drawer checked at each breakpoint, workspace panes, and labeled Map, Action, and Activity views below 1024px; dynamic viewport height instead of the fixed chrome height. | | 1:00 |
| 28c.3 Hardening | Keyboard use, focus and drawers, 200% zoom and 320px reflow, the contrast targets, reduced motion, and reconnection with a pending proposal (section 14); the browser matrix at 1920x1080, 1366x768, 1024x768, and narrow widths (section 17.3). | | 1:30 |
| | Overhead | | 1:15 |
| | **Pass 28c total** (build 4:00) | | **5:15** |

### Pass 29: DYO purchase I: Infantry, leaders, and SW

**Purpose:** Chapter H makes a card. **After:** pass 28 (a DYO card opens in the editor's forms). **From:** backlog section 27.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 29.1 The DYO setup and roster | A DYO page: German and Russian (user answer, 2026-09-30), the date, boards, and the points each side spends; the Roster, with the counter limits and purchase mechanics. | H1.1 to H1.14 | 0:45 |
| 29.2 Infantry purchase | Squads and crews by BPV and MPV, with Assault Engineers, Sappers, Commandos, and MOL capabilities where the game plays them; the ELR Chart and SAN. | H1.2 to H1.29 | 0:50 |
| 29.3 Bonus Infantry and leaders | The second Infantry purchase and bonus Infantry; leader quality and the Leader Exchange DR. | H1.7 to H1.74, H1.8 to H1.82 | 0:40 |
| 29.4 SW allotment and the card | SW allotted by ratio; the purchase written as a user card (sides, ELR, SAN, OB groups) and opened in the editor. | H1.83, H1.84 | 0:45 |
| 29.5 The DYO page from components | Built from S02, S04, S06, S07, and K02 from the start; no raw form markup to extract later. | | 0:15 |
| | Overhead | | 1:15 |
| | **Pass 29 total** (build 3:15) | | **4:30** |

### Pass 30: DYO purchase II: ordnance, vehicles, and conditions

**Purpose:** the DYO purchase covers what the game plays beyond Infantry. **After:** pass 29. **From:** backlog section 27. Its purchase forms reuse pass 29's components.

| Task | What it changes in play | Rules | Estimate |
|---|---|---|---|
| 30.1 Ordnance and vehicles | Guns and vehicles by BPV with the Availability DR and RF; optional armament and Armor Leaders where the game plays them. | H1.3, H1.4 to H1.43 | 1:00 |
| 30.2 DYO conditions | The DYO Weather, EC, and NVR tables written to the card's SSR tokens. | E1.11, E3 | 0:40 |
| 30.3 What is not played | OBA, Air Support, fortifications, boats, gliders, horses, and OP tanks refused with their backlog rows. | H1.44 to H1.6 | 0:20 |
| 30.4 End to end | A DYO game bought, saved, started, and played to its end by the table player. | | 0:30 |
| | Overhead | | 1:15 |
| | **Pass 30 total** (build 2:30) | | **3:45** |

## 6. Duration report

The game passes keep the Scenario Card Games Plan's basis: 1:15 of overhead per pass (reading and rulings, the two reviews and their fixes, the documents, the full local suite, and the merge gate). The Studio passes and the component tasks use the same overhead. Their builds assume about 8 to 10 minutes per extracted component, including its tests, resets, and disclosure checks, with Play panels taking the most: most extractions move markup behind a narrow contract without changing behavior, and recent passes have run at about 65 % of their estimates. P1 candidates are the default and a pass may leave one inline with a reason; P2 and P3 candidates are extracted only when needed (section 16.17). The largest single risks to the estimates are 22d.1 (the viewport) and 27.5 (Close Combat and its disclosure). The game passes were first estimated at 30:00 (33 tasks, build 20:00); their component tasks and the tests before them bring them to 36:25. Studio passes total 24:15; all passes 60:40, about 8.1 working days of 7.5 hours. Actuals have run well under plan (pass 20 took 2:17 against 3:00, pass 21 2:22 against 3:45, pass 22 2:18 against 3:30); at about 65 % of the estimate the whole would be nearer 39:30. The estimates are not cut for that, because passes 22d, 26, 28c, 29, and 30 reach into areas the recent passes did not.

## 7. Risks

- **Pass 23** changes what every Play panel shows; the page tests read the adjudicator's view and may need a side's view throughout. Its component task (23.5) makes the side's view an explicit input from the start.
- **Pass 22d's board viewport** is the riskiest extraction: the page and a component must never both install listeners on one element, and reconnection, route changes, repeated mounting, and disposal all need tests (section 15.5).
- **Pass 26** depends on vehicle movement from off board, which the ordnance and vehicle passes did not build; if Motion and Passengers at entry need more of Chapter D, the pass grows.
- **Pass 29** needs the catalog's BPV for every purchasable counter; counters the catalog lacks are manufactured (R0.3), which may add catalog versions and package re-pins as pass 17 did.
- **Test hooks.** The 156 MapStudio tests address the pages' element ids and data attributes; every extraction keeps them (section 17.1), adding focused component tests rather than rewriting page tests.
- **Disclosure.** A component must never receive hidden data and hide it with CSS (section 15.4); the review of each Play extraction checks the DOM and accessible text in both a side's and the adjudicator's view.

## 8. Left out

These stay in the backlog: Fire spread and Kindling (so Control forfeited to a Kindled Fire, and EC and wind as an SSR the game reads), Sewer Movement and foxholes, Battlefield Integrity, the Factory and Fanaticism SSRs, OBA and Air Support, fortifications, the national rules of A25 for nationalities without counters, Chapter G (the Pacific), and more built-in cards.

For the Studio, what section 2 places outside the redesign stays out; narrow-screen access and keyboard operation are in it (pass 28c). P2 and P3 candidates a pass leaves inline are recorded in its review, not in the backlog, unless a later pass needs them. The Board library's SVG previews (section 12.1) are not in these passes; they get a backlog row when this plan is approved.

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

- Play:63 builds the board link with `Perspective.Adjudicator`, even though the toolbar has a selected perspective. Treat this as a separate explicit behavior decision and test it during the redesign; do not bury a perspective change in a component refactor. **2026-09-30:** still present on main. Pass 23 (per-side views with a hand-over screen) cannot hide a side's setup while this link opens the adjudicator view, so the decision is due before that pass.
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

# Part IV. The component inventory

## 16. Component extraction inventory

### 16.1 Findings and source baseline

The existing pages contain **170 active candidate component boundaries** after the 2026-09-30 review (155 on 2026-09-29, less 5 retired, plus 20 new; section 16.16). They include shared components, page-specific task panels, and optional nested children. This is a broad inventory for selection, not a target of 170 new Razor files. A parent and its listed children are separate possible boundaries within overlapping source, not independent chunks to extract twice.

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

The active inventory contains **113 P1**, **53 P2**, and **4 P3** candidates (2026-09-29: 102, 49, and 4). Retired candidates keep their rows, marked retired, so their IDs are not reused. Names are proposed `.razor` filenames, not existing classes. Contracts are suggested inputs and outputs, not claims that these view models already exist. Callback names describe intent; use typed `EventCallback<T>` contracts during implementation.

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
| R11 / P1 | `PlayMapPanel` | [Play:1443-1466](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/2fa3f564056049d222f722e4b5bc925d51c3abd3/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor#L1443) | Perspective-safe drawing SVG/source/problems and current selection; LocationSelected if supported. | Own drawing success/error presentation. Reusing interactive BoardViewport is later behavior work, not a markup-only extraction. |
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

These 20 additions contain 14 P1 and 6 P2 candidates. Links pin main `c13e9b7`.

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
| K13 / P1 | `CardEditorForm` | [CardEditor:44-138](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L44-L138) | A card draft, the catalog's choices, and the diagnostics; DraftChanged. | The editor's form root; the page keeps the library, validation, save, and delete. Pass 28's forms grow inside it. |
| K14 / P1 | `CardIdentityFields` | [CardEditor:45-56](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L45-L56); [CardEditor:133-137](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L133-L137) | Id, title, place, date, introduction, and source draft; IdentityChanged. | The id is checked as typed (form, built-in name, another user card); a minimal card may leave the date 0. The Source label's `for="edit-basis"` does not match the input `#edit-source-basis`; pass 28.5 fixes it. |
| K15 / P1 | `CardMapFields` | [CardEditor:57-72](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L57-L72) | Boards text, north, playable area and hexrows with their board; MapChanged. | The future host of map picking and saved-map choice (pass 28); today typed. |
| K16 / P1 | `CardTurnRecordFields` | [CardEditor:74-82](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L74-L82) | Turns, half turn, sets-up and moves-first sides, Scenario Defender, side choices; TurnsChanged. | Its side references follow a side's nationality change (section 15.3). |
| K17 / P1 | `CardSideFields` | [CardEditor:83-114](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L83-L114) | One side's nationality, SAN, ELR, edge and basis, Integrity BPV, Balance, and OB groups; keyed by index; SideChanged. | The two sides are one repeated block; a side ELR on a card with an OB is refused, not dropped. |
| K18 / P1 | `ObCounterPicker` | [CardEditor:115-128](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L115-L128) | Side and group, the side's counters in the order shown, count and area; AddRequested. | Adds the counter it shows (the demonstration found it did not); resets to the side's first counter when the side changes. |
| K19 / P2 | `CardJsonField` | [CardEditor:112-113](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L112-L113); [CardEditor:129-132](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L129-L132) | Label, JSON text, and parse error; TextChanged. | Editable JSON for groups, SSRs, and Victory Conditions until pass 28's forms replace it; distinct from the read-only S14 `JsonDisclosure`. |
| K20 / P2 | `CardSaveActions` | [CardEditor:38-41](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L38-L41); [CardEditor:154-158](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime/blob/c13e9b7596cb90c49ab2a38dd4c6060ec0b6669a/src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/CardEditor.razor#L154-L158) | Can-delete state, the save note; Save/Delete. | Composes S04 `OperationFeedback`. Save stays enabled and says why a card is not saved, so a click that arrives with a field's last change is not lost. |

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

Every active candidate belongs to exactly one pass; the five retired ones (P03 to P06 and N01) to none. A pass extracts its P1 candidates by default and may leave one inline with a reason; its P2 and P3 candidates are extracted only when the parent stays hard to understand, is rendered on its own, or is reused (section 16.2). The pass's review records every candidate left inline. Pass 24 extends K08 rather than adding one, and passes 29 and 30 build a new page from the shared components rather than extracting any.

| Pass | Candidates | P1 | P2 and P3 | Candidates |
|---|---:|---:|---:|---|
| 22b | 13 | 12 | 1 | S01 `StudioNavigation`, S02 `PageHeader`, S03 `SourceRequiredNotice`, S04 `OperationFeedback`, S05 `StatusBadge`, S06 `FindingList`, S07 `FieldGroup`, S09 `PerspectivePicker`, S10 `RevisionNavigator`, K01 `NewGameFromCard`, K02 `CardPicker`, K03 `CardStartSummary`, K04 `BalanceChoice` |
| 22c | 31 | 19 | 12 | H01 `AuthoredBoardList`, H02 `MapSummaryList`, H03 `BoardScopeSummary`, H04 `LibraryReportContext`, H05 `VaslBoardTable`, H06 `BoardVerificationRow`, M01 `MapPlacementEditor`, M02 `MapPlacementRow`, M03 `ScenarioRuleEditor`, M04 `PlacementTextEditor`, M05 `MapBuildActions`, M06 `NewBoardForm`, M07 `BoardDimensionsFields`, M08 `SourceDraftNotice`, G01 `GameReplayToolbar`, G02 `GameReadFindings`, G03 `GameContextSummary`, G04 `ProjectedGameUnitTable`, G05 `GameEquipmentTable`, G06 `ReadCaseForm`, G07 `ReadCaseResult`, G08 `PerspectiveEventList`, F01 `FidelityRunControls`, F02 `LosFidelityPanel`, F03 `LosFidelityResultsTable`, F04 `FidelityReportPicker`, F05 `FidelityReportMetadata`, F06 `FidelityResultsTable`, F07 `FidelityDifferences`, F08 `FidelityCheckBadges`, F09 `StudioConfigurationReport` |
| 22d | 34 | 20 | 14 | B01 `BoardViewerToolbar`, B02 `BoardViewPicker`, B03 `BoardComparisonControls`, B04 `BoardLayerToggles`, B05 `UnitOverlayControls`, B06 `BoardViewport`, B07 `SelectedUnitInspector`, B08 `UnitGameFacts`, B09 `HexUnitStackList`, B10 `LosPanel`, B11 `LosResult`, B12 `BoardProvenancePanel`, B13 `BoardEditorHeader`, B14 `EditorToolOptions`, B15 `InspectorTabs`, U01 `UnitTemplatePicker`, U02 `AttachedEquipmentEditor`, U03 `CounterTierPreviewGrid`, U04 `CounterFacePreviewGallery`, U05 `UnitAccessibleDetails`, U06 `UnitLabFindings`, U07 `UnitDocumentOutput`, U08 `UnitStyleSheetEditor`, U09 `UnitPlacementSetEditor`, U10 `PlacedUnitList`, E01 `VocabularyAttributeInput`, E02 `UnitIdentityFields`, E03 `UnitFaceEditor`, E04 `UnitAttributeFields`, E05 `UnitStateChecks`, E06 `HexGridSamples`, E07 `HexFeatureList`, E08 `TerrainPicker`, S14 `JsonDisclosure` |
| 23 | 13 | 12 | 1 | P01 `ScriptedDicePanel`, P02 `LiveGameToolbar`, P07 `GameReplayFailure`, N02 `NightWeatherSummary`, R01 `ProposalReviewPanel`, R02 `EntryReviewFacts`, R03 `FireReviewFacts`, R12 `PlayUnitTable`, R13 `AdjudicatorAuditPanel`, K05 `SetupPoolsTable`, K06 `PlayCardPanel`, K07 `GameEndNotice`, K08 `VictoryStandingTable` |
| 25 | 13 | 8 | 5 | A01 `BuildingEntryAction`, A11 `MovementStatus`, A13 `InfantryMovementAction`, A14 `SmokeGrenadeAction`, A15 `MovementWindowActions`, A16 `ReactionFireAction`, A23 `AdvanceActionPanel`, C19 `OpenEntryDeclaration`, K09 `MapExitAction`, N03 `PlaceDemolitionChargeAction`, N04 `BerserkRetainedWeaponsField`, S11 `LocationField`, S12 `UnitSelectionList` |
| 26 | 10 | 7 | 3 | P08 `SetupPlacementEditor`, P09 `SetupPlacementList`, A17 `VehicleMovementPanel`, A18 `VehicleMovementStatus`, A19 `VehicleStepChoices`, A20 `VehicleTowingActions`, A21 `BoundingFireAction`, A22 `CrewExposureActions`, C18 `GunArcAction`, S13 `FacingPicker` |
| 27 | 24 | 14 | 10 | A12 `BerserkChargeNotices`, C01 `VehicleCloseCombatPanel`, C02 `CloseCombatPanel`, C03 `CloseCombatLocationControl`, C04 `CloseCombatStacking`, C05 `CloseCombatWithdrawals`, C06 `CloseCombatAttackBuilder`, C07 `CloseCombatAttackQueue`, C08 `PendingChoicePanel`, C09 `PendingSurrenderPanel`, C10 `PrisonerActionPanel`, C11 `FireMarkerSummary`, C12 `OpportunityFireAction`, C13 `SmallArmsFirePanel`, C14 `FireGroupSelector`, C15 `FireTargetOptions`, N05 `CloseCombatRoundOptions`, N06 `AmbushWithdrawalActions`, N07 `CaptureAttemptFields`, N08 `CloseCombatInfiltrationChoices`, N09 `PrisonerCustodyActions`, N10 `MolFirerChoice`, N11 `ThrowDemolitionChargeAction`, N12 `PlacedChargeDetonationActions` |
| 28 | 11 | 7 | 4 | K10 `ScenarioCardView`, K11 `CardSideSection`, K12 `CardSpecialRulesList`, K13 `CardEditorForm`, K14 `CardIdentityFields`, K15 `CardMapFields`, K16 `CardTurnRecordFields`, K17 `CardSideFields`, K18 `ObCounterPicker`, K19 `CardJsonField`, K20 `CardSaveActions` |
| 28b | 20 | 13 | 7 | A02 `RoutActionPanel`, A03 `RoutObligations`, A04 `SupportWeaponActionPanel`, A05 `RallyActionPanel`, A06 `RepairActionPanel`, A07 `DeployActionPanel`, A08 `RecombineActionPanel`, A09 `DmRetentionChoices`, A10 `ShockRecoveryAction`, C16 `OrdnanceFirePanel`, C17 `OrdnanceTargetFields`, R04 `DiceRollHistory`, R05 `ActionRecordList`, R06 `FireHistory`, R07 `FireResolutionCard`, R08 `FireArithmeticBreakdown`, R09 `InfantryFireEffectsTable`, R10 `VehicleFireEffectsTable`, N13 `StarshellActionPanel`, S08 `RuleHelp` |
| 28c | 1 | 1 | 0 | R11 `PlayMapPanel` |
| **All** | **170** | **113** | **57** | |

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

### 17.2 Game passes

A game pass is complete when its tasks' rules play as its rulings say, its referee and table player reviews are answered, and the merge gate of the Backlog Passes Plan's section 1 passes: the full local suite with the ScenarioA1 tests, the Docker Linux check, the chart supplement regeneration where authoring changed, and GitHub Actions after the push. Its component task (23.5, 24.5, 25.6, 26.5, 27.5, 28.5, or 29.5) also meets section 17.1.

### 17.3 Browser evidence

Completion evidence by pass, carried from the redesign's delivery stages:

| Passes | Completion evidence |
|---|---|
| 22b | Every route reachable; the current navigation group correct; keyboard and zoom inspection. |
| 22c | Matching, empty, loading, and failure states and back navigation in the Board library; rule order and placement editing preserved in Maps. |
| 22d | Existing edit, save, undo, and redo, the counter tiers, and the comparison modes preserved; the viewport's browser checks recorded. |
| 23.5 to 28.5, 28b | Representative actions from each extracted family keep their proposal, confirmation, refusal, and replay behavior, in a side's and the adjudicator's view. |
| 28c | Overflow, focus, reconnection, performance, copy, and evidence review; the browser walkthrough matrix and the relevant automated checks recorded. |

Use representative data: empty/configuration failure, a full board library, a verified board and a failing board, a source-derived draft, a composed map with ordered SSR rules, multiple unit kinds, and live/synthetic games. Inspect at 1920x1080, 1366x768, 1024x768, and narrow/zoomed layouts. Include long names, diagnostics, and action labels.

Run targeted MapStudio component tests for changed flows and existing renderer/viewport checks when those boundaries are touched. For Play, verify visible versus adjudicator projections, proposal refusal, stale confirmation, duplicate confirmation, and record attribution. Build with existing warning policy; broaden testing when a change crosses service or rendering boundaries. Documentation-only changes require link, diff, and scope checks, not an application test run.

Record browser evidence separately from automated results. A compilation or selector check is not visual acceptance. No screen is complete while ordinary scrolling or pane collapse can make Confirm, Save, validation, or draft restrictions unreachable.

### Records of completed checks

The records below come from the redesign branch; they describe the theme and the card pages as checked on 2026-09-28 to 2026-09-30.

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
