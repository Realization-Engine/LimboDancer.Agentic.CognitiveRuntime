# ASL Unit Backlog Pass 29 Design

**Status:** Built 2026-10-03. Tasks 29.1 to 29.5 and the UI batch on branch `ui-improvements` are built, reviewed by a table player, a referee, and a UI and Blazor review (section 15), checked in the Studio, and passed through the merge gate (section 16). Pass 29 (The shared board workspace) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

**Date:** 2026-10-02

**Related documents:** the plan's sections 13.1, 13.4, 15.5, 15.10, and 15.11; the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>); and the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 44.

A Studio pass adds no rulings. Its design decisions are recorded here and in the plan's section 15.11.

## 1. Outcome

The user compared the board viewer's inspector with Play's review pane. The board viewer shows a picked unit's details, the units in a hex, the hex's terrain facts, LOS as a tab, and Evidence (grid samples and provenance). Play showed a list of unit ids in the picked hex, and its LOS form sat collapsed under the map. The two were separate code and would keep drifting apart.

The pass extracts one map-and-inspector component from the board viewer, and both pages now host it. On Play, the proposal review is the inspector's first tab.

The user's answers on 2026-10-02:

- **Proposal as a tab.** The review is a Proposal tab, chosen and marked when a proposal arrives, rather than a pane pinned above the tabs.
- **Order.** The four tasks were built in one go, on branch `ui-improvements`, each checked in the Studio.
- **Numbering.** This is pass 29. Every planned pass from the old 29 moved up by one: DYO is now passes 30 and 31 (still deferred), and the rule packages are 32 to 41 with 33b.

## 2. The components

| Component | Folder | What it is |
|---|---|---|
| `BoardInspector` | Components/Board | The tabbed pane alone, presentational: Selection, LOS, and Evidence, with any tabs the host adds placed first. It takes the board, the hex kept and the hex hovered, the units already read for the view, the picked unit, the view, the pixel clicked, the LOS draft and result, and the open tab. It shows only what it is given. |
| `BoardWorkspace` | Components/Board | B06 `BoardViewport` and `BoardInspector`, with the logic lifted out of the board viewer page: click, hover, Escape, a counter click, the highlight, the pixel sample, and the LOS check. It loads the board when the board, its view, or its trace changes, and sends each layer (units, LOS, marks, highlight, visible layers, comparison) only when it changes. A failed load shows its reason and a Try again button. |

`BoardWorkspace`'s contract:

- **The host gives:** the board; the view, trace, visible layers, and comparison (the board viewer only); the unit overlay already read for the view, and the view; a marks layer (Play only); a toolbar and a footer.
- **The host binds:** the hex kept (`Selected`) and the open tab (`Panel`).
- **The workspace keeps:** the hex hovered, the picked unit, the pixel sample, and the LOS draft and check. A unit the overlay no longer draws, or one outside the hex kept, is dropped.
- **Layout.** The stage (toolbar, map, footer) and the inspector are sibling elements, not one wrapper, so each host places them in its own grid. The host names their classes and attributes, may put the inspector first, and may put markup between the two (`Between`).
- **Methods.** `Zoom`, `Fit`, and `Inspector` (the inspector's element, for a host that brings it into view).

## 3. The board viewer (29.1, 29.2)

The page keeps its route and query parameters, the board's load, the unit sources (placement sets and games, perspective, revision, counter sheet), and its study controls: the view picker, comparison, layers, and trace, given to the workspace as its toolbar. The Styled view's vectorization runs before the view that needs it is drawn. Nothing it shows or does changed; the tab, panel, and LOS ids are the same (`viewer-tab-*`, `los-*`).

## 4. The game's overlay (29.3)

`GameMaps.Layers` returns, beside the SVG layers, the `UnitOverlay` the units were drawn from and the `GameView` they were read for (`GameMapLayers.Overlay` and `View`). The inspector's Selection tab reads only these, so it shows what the view may see:

- a concealed unit is "<side> concealed unit", with its identity and conditions withheld from the other side;
- a unit's facts in the game come from the view, never the full state.

Two helpers convert between the page's picked Location and the map's hex: `GameMaps.LocationOf(board, hex)` (the board and hex that own a hex of a composed map) and `GameMaps.FactsAt(board, location)`.

## 5. Play on the workspace (29.4)

| Before | Now |
|---|---|
| The review pane: the proposal, then the picked hex's list (`PlaySelectedHex`) | The inspector: Proposal, Selection, LOS, and Evidence tabs |
| `PlayMapPanel` on B06 | `BoardWorkspace`'s stage, with Play's zoom buttons and the Residual FP line as its toolbar |
| The LOS form under the map, with a "From unit" select | The LOS tab, with "selected hex" for either end; a counter click picks its hex |
| The page drew the picked hex and the LOS line through `GameMaps.Layers` | The workspace draws both; the page's map cache keys only on the game, revision, view, and Gun |

- **The Proposal tab.** It holds the existing review: the "Nothing to review" note, or the proposal with Confirm and Cancel, busy and Stale as in pass 28c. While a proposal waits, the tab reads "Proposal (1)". A new proposal chooses the tab and takes the focus there; the header's "Review: <action>" button does the same from any tab.
- **Selection.** A click on the map, a click on a counter, "Locate" in the units table, and the DEFENDER's "Show it on the map" all pick a hex. The tab shows the picked unit's details, the units in the hex, and the hex's terrain facts.
- **The hand-over** still unmounts the workspace, so the next view starts with no hex kept, no LOS line, and the Selection tab; the proposal is cleared with the view, as before.
- **The layout** keeps pass 28c's breakpoints and ids (`play-panel-review`, `play-panel-map`, `play-panel-actions`, `play-panel-activity`, `play-map-zoom-in`, `play-map-fit`). The inspector comes first in the page, then the narrow tabs and the actions (as the workspace's `Between`), then the map. Under 1024px the inspector sits above the Map, Actions, and Activity tabs and scrolls within 55% of the window's height; it always shows, since its tabs are always useful.
- **Retired:** `PlayMapPanel`, `PlaySelectedHex`, the `play-los` form, and their styles.

## 6. Disclosure

- The workspace receives the overlay already read for the view; it never sees the game's state.
- Hex facts, Evidence, and LOS are terrain only, so every view may read them (as on the board viewer).
- The hand-over clears everything of the last view: the workspace is unmounted, and the page clears the picked hex, the proposal, and the open tab.

## 7. Studio checks

Checked in the Studio on 2026-10-02.

- **29.1**, in the user's Studio on port 5178, on `/boards/bd01` with the synthetic game: a hex click, a counter click, the tabs by keyboard (Right and End), LOS from the selected hex with its line drawn and cleared, Evidence, Escape, and hover.
- **29.2 to 29.4**, in a Studio on port 6670:
  - the board viewer: a counter click with the unit's details, LOS kept across a change to the Hex Facts view, a layer toggled, and the Russian perspective;
  - Play on `p28c-walk2` in the German view: the Russian "?" shown only as a concealed unit; a proposal choosing and focusing the Proposal tab; Review from the LOS tab; Cancel; Locate; LOS; the hand-over to the Russian view clearing the hex, the line, and the tab;
  - the widths 1920x1080, 1366x768, 1024x768, 683x384, and 320x640: no page scrolls sideways, and at 320px the map keeps 258 by 346 pixels.

One issue was found and fixed: a counter click kept the hex but lost the unit's details, because the unit was set after the hex's change had rendered.

## 8. Open

- **Tests, at the end of the batch.** The component tests of `PlayMapPanel` and `PlaySelectedHex` go, and the Play page tests that read the old map panel, `play-selected`, or `play-los-*` move to the workspace. New component tests for `BoardInspector` and `BoardWorkspace`.

## 9. The review's fixes and the turned map

The user had Claude Design review the first build (commit 747ad2c). It found no case of a view seeing more than it may, but four ways state could outlive a change of view or game, and several usability problems. On 2026-10-02 the user accepted every fix below, chose to put the inspector under the map in narrow windows, and pointed to the map rotation of backlog section 44 as what makes that work: a board is about three times wider than tall, so in a portrait window most of the Map tab was empty.

**The turned map.**

- "Rotate map" in the map's toolbar, on Play and the board viewer, turns the whole map a quarter clockwise as one: terrain, counters, labels, LOS, marks, and the highlight. The user ruled that counters turn with the board; nothing is redrawn upright.
- The turn is a CSS transform on the drawing (`.board-host.rotated`, sized with container units), so the board's coordinates do not change. The viewport reads the pointer through the screen matrix, which includes the transform, so clicks, hover, and wheel zoom stay right; dragging to pan maps the screen's movement onto the turned axes.
- The browser remembers the choice (local storage); it is off until first chosen. Section 10 makes it one choice per shape of pane.

**Narrow windows.** Under 1024px the inspector sits below the map, inside the Map tab, so what a click picks shows right under the map. A new proposal and the Review button open the Map tab with the Proposal tab chosen. Wider, the inspector stays the right-hand pane.

**The Proposal tab.**

- While a proposal waits for Confirm, a click on a hex or a counter keeps the Proposal tab open (`HoldPanel`). Otherwise a counter click opens Selection, and a hex click opens Selection from the Proposal tab; LOS stays open, so its ends can be picked.
- The tab reads "Proposal (1)" only while a proposal needs confirmation, not after a commit, refusal, or Stale result.

**State across a change of view or game.**

- The game and view pickers are disabled while the gate works, and a proposal or confirmation that returns for another game or view is dropped.
- Changing the game clears the picked hex and the open tab (the Gun was already cleared with the panel choices).
- The workspace carries a `@key` of the game and the view, so a new view always starts a new workspace, whether or not a hand-over unmounted it.
- A picked "?" is let go when the view is read again, since its placement id is numbered afresh at each revision.

**Smaller fixes.**

- A hover draws the inspector again, not the workspace, so the actions pane is not redrawn as the pointer moves.
- The units in a hex show their ids, and the unit shown above is marked ("shown above").
- The LOS tab opened with an empty From takes the picked hex.
- Good Order reads Yes or No; a side reads as the counter names it ("German"), in the game facts too.
- The duplicate `play-proposal` id is gone (the tab's wrapper is `play-proposal-tab`); Play's unused injects are removed; `GameMaps.Layers` loses its highlight and LOS parameters, and `GameMapLayers` its Los and Highlight fields.
- A layer the browser refuses is reported like a failed load, with Try again.
- The Styled view is set only once its vectorization is ready, also when a board opens in it.
- With no drawable map, a new proposal and Review focus the review pane.

**Studio checks**, on port 6670 with `p28c-walk2`:

- the turned map at 1920x1080: Fit, a counter click (the screen point maps to the counter's own board coordinates, and the hit is the counter), a drag that keeps the point under the pointer, hover across hexes;
- the Proposal tab: a counter and a hex click kept it open while Confirm waited; after Cancel the count went and a hex click opened Selection;
- 683x384: a proposal opened the Map tab, with the inspector below the map and the focus on it; 320x640: the turned map fits the column (section 10 makes Fit fill its width), and nothing scrolls sideways;
- the LOS tab filled From with the picked hex; the hand-over started the Russian view on Selection with the map still turned;
- the board viewer: the turn remembered across pages, the Styled view drawn after its vectorization, and the turn switched off.

## 10. The second review

The user had Claude in Chrome check section 9 in the running Studio (commit 69eb89e). Six of seven checks passed; the disabled pickers could not be seen, and five problems were found. The user accepted every fix below on 2026-10-02, chose to remember the turn by the pane's shape, and added that a turned map should fit the pane's width.

- **Fit on a turned map** in a tall pane fits the board's turned width to the pane's width, from its turned top; the length is panned. In a wide pane, or when the turned board fits whole, the whole board shows. Turning the map fits it at once, and a view still at its fit is fitted again when its pane changes size (a third check found the map shrinking when the context grew).
- **One turn per pane shape.** The choice is remembered for a pane taller than wide (`studio.map.rotated.portrait`) and for one that is not (`studio.map.rotated.landscape`). A pane resized into the other shape takes that shape's choice, and the toolbar's button follows it. A pane that is hidden (a narrow tab not shown) has no size and so no shape; its choice waits until it shows. Turning the map on a phone, or in Play's tall map pane, never turns the board viewer's wide pane.
- **The pickers draw as disabled.** The page draws its busy state before it asks the gate, so the Game and View pickers show as disabled while a proposal or a confirmation is checked.
- **Cancel clears the status line**, with the proposal.
- **The context no longer covers the panes after a proposal.** The reveal waits two frames, so the context's new height is measured first and the pane stops below it.
- **A reload keeps the game.** Choosing a game writes `?game=` to the address, so a reload opens it again, behind the hand-over.
- **Good Order unknown says why.** The rule (ASL-UNIT-023) leaves Good Order unknown while broken, berserk, captured, or Melee is not recorded, and the games made in the Studio leave some of them unrecorded. The inspector reads, for example, "unknown: not recorded whether berserk, captured, melee". Recording all four at a unit's creation changes saved games, so it is a backlog row (section 45).
- **The units table** writes a side as the counters and the inspector do ("Russian").
- **The old storage key** (`studio.map.rotated`) is removed when the map opens.
- **Left as they are, at the user's word:** the legend turns with the map, and hexside names stay board directions.

**Studio checks**, on port 6670 with `p28c-walk2`:

- the pickers' fieldset went disabled and back during a proposal; Cancel cleared the status; choosing the game wrote `?game=p28c-walk2`, and a reload opened it behind the hand-over;
- the units table read "German" and "Russian"; g1's Good Order read "unknown: not recorded whether berserk, captured, melee";
- at 320x640, "Rotate map" in the Map tab turned the map and fitted its width (view box 963 by 717 board units across a 258 by 346 pane), and stored the portrait choice only;
- at 1480x900, Play's map pane (434 by 556) is tall, so it stayed turned; the board viewer's pane (901 by 586) is wide, so it was not;
- at 1480x900, with the workspace just below the context, a proposal grew the context to 205 pixels and the panes started at 209.
- after the third check (commit b8806a5): at 1480x900 a proposal shrank the turned map's pane from 556 to 495 pixels, and the view box followed it (919 to 818 units long, 717 across), then back after Cancel; the board viewer's wide pane (901 by 586) turned shows the whole board; a resize from 1480 to 700 pixels switched Play to the narrow tabs with no sideways scroll (the reviewer's background tab did not).

## 11. Task 29.5 and the board viewer's game banner

Built 2026-10-02 from Claude Design's UX analysis (the user's PDF), with task 29.5.

- **The selection in the board link (29.5).** `Games.BoardLink` takes the picked hex; Play's "View on" link carries it as `hex=`, and the board viewer opens with that hex picked and highlighted.
- **The game banner.** When the board viewer shows a game, a line above its toolbar says which game, whose view, and which revision: "Viewing live game p28c-walk2 as the German side, revision 36 of 36." A live game links "Back to Play" (`games/play?game=...`).
- **Historical.** Below the game's latest revision the banner adds a "Historical" badge: an earlier revision is not the game's current state.

Checked in the Studio on port 6670: a hex located on Play put `hex=bd01:E5:0` in the link; the viewer opened with E5 picked; Previous showed "revision 35 of 36" with Historical, Next took it away.

## 12. From the UX analysis: words, the card, and verification counts

Built 2026-10-02 from Claude Design's UX analysis (the user's PDF), in the order the user approved, each checked in the Studio on port 6670.

- **Words for identifiers** (`DisplayText`, Services). A side reads as its counters name it ("German"); Good Order as Yes, No, or unknown; a kind without `asl:`; a board's status in words ("Authored, valid"); a Location as "F6 on bd01", with its id beside it in the non-OB lists. Play's context, notes, hand-over, and view picker, the Game states page, the board headers, and the inspector use it. The identifiers themselves are unchanged.
- **The scenario card on Play starts closed.** The context's "Card and Victory Conditions" opens it below the sticky context and focuses its title, so the workspace starts near the top of the page (168 pixels at 1920x1080, from about 830). The card's own link to the whole card has its own id now (`play-card-whole`).
- **Verification counts.** The library and the Fidelity page disagreed (236 against 158 boards in scope, 4 against 156 verified) because the latest report, of 24 Sep, was made with importer 1.0.0 and the importer has been 1.1.0 since commit e6bc0fb the same evening; the library rightly refused its results, and the Fidelity page showed them without saying so. Now:
  - `FidelityJobRunner.StaleReason` words why a report is out of date (a tool's version or the shared board metadata changed); the library's freshness check uses it.
  - The Fidelity page says a report is out of date and that the library does not use it; the library says why it shows "Not checked".
  - The batch was run again (1.9 minutes): 297 boards, 235 verified, 1 failed (bd79: its `BoardMetadata.xml` is not well-formed), 61 out of scope. Both pages now show 236 in scope, 235 verified, and 1 failed.
  - The library still counts 5 fewer boards out of scope (56 against 61): it lists only boards whose names make a board reference, and the five with an underscore (bdAF_BiazzaRidge and bdFB_NE, NW, SE, SW) do not; all five are out of scope.

## 13. Claude Design's review of sections 11 and 12

Claude Design reviewed sections 11 and 12 against its analysis (2026-10-02). The user took its eight recommended items, added plain names for the fidelity checks, and deferred one decision. Built 2026-10-02 and checked in the Studio on port 6670:

1. **Sides and views everywhere.** The Play card ("German sets up first; Russian moves first", the Balance, the players), the board viewer's and Game states' view pickers ("German", "Russian", "the adjudicator"), Game states' context line ("German phasing; seen by the adjudicator"), its headings, and "withheld from the German side".
2. **"Card changed"** shows as a badge in Play's context while the card is closed.
3. **One statement of the game in the board viewer.** The banner keeps it; the strip's own sentence is gone, and the note says only whether the game is live or synthetic.
4. **One status vocabulary.** Ingested reads "Not verified" everywhere (the library's totals and filter, the batch outcome); Pass is spelled one way; Yes, No, Unknown, and "Does not apply" share one case; `DisplayText.Condition` and `ConditionsOf` replace the copied conditions text.
5. **Plain names for the fidelity checks.** F1 is "Terrain data read exactly", F2 "Hex facts match VASL", and F3 "the Styled drawing" check, with the codes kept in brackets; the named checks read "Terrain outlines", "Exact drawing", "Styled drawing: pixels", and so on; the run option reads "Also check the Styled drawing (F3, slower)".
6. **Locations as players say them**: "E4 on board 01", "on board 01, cellar", "level 1".
7. **Fidelity finds failures.** A report with boards not verified opens on them; failed boards sort first, with their reason beside the outcome; check chips are green when passed, red when failed, amber when only informational, with a legend.
8. **The five boards with an underscore** are counted out of scope in the library (61, as on the Fidelity page) and named beneath the totals (`IBoardProvider.Unlisted`).

**Deferred by the user:** whether the board viewer, opened from a side's view of a live game, should lock to that view or allow other views only behind a hand-over (backlog section 45).

## 14. The rest of Claude Design's list

Built 2026-10-02 at the user's word, each checked in the Studio on port 6670.

- **Disabled Propose buttons say why** on Play's Setup: "Choose a scenario card first.", "Add at least one counter above.", and beside a side's non-OB "?", "Tick at least one Location."
- **The board viewer's Layers tab.** The rendered layers and the trace moved from a row above the map into a Layers tab after Selection, LOS, and Evidence (`BoardInspector.ExtraTab` takes `Last`); "Rotate map" joined the view picker in the toolbar. The rows above the map are now the game banner, the board's toolbar, and the units strip.
- **The card editor's labels** sit at the top of a tall field, not at its last line.
- **The Unit Lab's preview** stays in view while the fields beside it scroll (from 1024px up).
- **The library's authored boards.** An untitled board reads "Untitled (its reference)"; "Kept in" shows only when the boards are kept in different places, and a line says where otherwise.
- **Game states in compact rows** (51 pixels, from about 85), with "23 / 23" beside the revision slider.
- **Two-layer introductions** on Play and Game states: one plain sentence, with how it works in a "How this works" disclosure (`RuleHelp.DetailLabel`).
- **From the review's smaller points:** the Play card closes from its foot ("Close the card"), returning the focus to the context's link; "Back to Play" carries the picked hex, and Play picks it once the hand-over on load is confirmed; the library's out-of-date note is plain, with a link to the Fidelity page; a comment says why the compiler and vectorizer do not decide a report's freshness.

**Not built here**, each larger than an hour or waiting on a decision: the board viewer's view on a live game (deferred, backlog section 45); one context bar for the viewer and Play; picking setup hexes on the map; a map-slot preview on Maps; a shared table component; per-row reasons when a board's source changed since the report; bringing a picked hex into view on a zoomed or composed map; unit names for "Produced from" and "Prisoner of".

## 15. The three reviews of the batch

Run on 2026-10-03 against `git diff main...ui-improvements` at 5978e4e: a table player, a referee (rulings R23.1 to R23.4), and a UI and Blazor review. The [review](<Scenario A1 Backlog Pass 29 Review 2026-10-03.md>) lists every finding; this section records what was decided and built.

**Play (`Play.razor`).**

- `ShowActions(scroll)`: the actions take the focus after a commit, after Cancel, and after a confirmed hand-over; under 1024px their tab opens. From 1024px up a commit and a Cancel move the focus without scrolling, since every pane is already in view (`reveal(element, scroll)` in `playWorkspace.js`); a hand-over still scrolls.
- `LocateHex(at)`: the units table's Locate and "Show it on the map" pick the hex, open the Map tab under 1024px, open Selection unless a proposal waits, and bring the map's pane into view with the focus (`BoardWorkspace.Stage`).
- The picked hex is cascaded to the actions as `PickedLocation`, a ground-level Location; `LocationField` offers "Use E4 on board 01" when it differs from its value. No action panel was changed to pass it along.
- `PickedNote`: while Confirm waits, the Proposal tab says which hex a click picked and how many counters the view draws there.
- The hand-over: `unconfirmed` marks the hand-over of a load or a game change, which choosing the same view again does not drop; a change of view clears the hex waiting from "Back to Play"; `hex=` leaves the address once read (after the first render) and on a game change.
- `Custody` and `SelectedUnitInspector.Named`: a side's view reads "a unit not in view" for a Guard, a parent, or a carrier its view does not hold.
- The under-1024px map pane is 55vh.

**The workspace and the viewer.**

- `BoardWorkspace` remembers a failed load or layer (`failedKey`) and tries again only on "Try again"; `Select` leaves the redraw to a host that binds the hex.
- The board viewer stacks under 1024px (the map at 60vh, the inspector below). "Rotate map" is disabled in the side-by-side comparison and a turned map is turned back when that mode opens. A view change that a later one overtook is dropped. The map has an accessible name.
- `MapSlotPreview` stops at 12 columns or rows, keeps its own size, and scrolls inside its figure.

**Words.** `DisplayText` now words the DEFENDER's note, the moving stack's status, Residual FP, Fire Lanes, encirclements, the card's OB groups, the Location options of the fire, ordnance, Bounding Fire, and Close Combat panels, the fire and move help, the setup pools, the victory standing, and the read case's Good Order.

**Decisions.**

- The fire panels keep their selects and free-text targets; taking them from the map is a backlog row. `LocationField` covered the typed Locations for the cost of one cascading value.
- Only Fidelity chips that failed or differ take the keyboard focus: with every board listed, focus on all chips made 940 tab stops.
- Arrow-key panning and the Firefox check of a turned map are backlog rows: the first is a new feature, the second was not reproduced.
- The board viewer's view on a live game stays as it is until the user decides (section 14, backlog section 45).

## 16. Verification

**The tests.** No unit test had run on the branch since main. On the committed batch 33 of the Studio's 264 tests failed; all were answered in the tests, none in the product's behavior:

- The retired `PlaySelectedHex` test became `ATypedLocationOffersTheHexPickedOnTheMap`; the "LOS from a unit" test became `TheLosTabStartsFromThePickedHex`; the LOS and picked-hex tests read the inspector's ids (`play-inspector-tab-los`, `los-*`, `hex-units`); `#play-map` became `#play-panel-map`.
- The narrow-width test expects the Actions tab after a commit.
- Wording: sides as on the counters, Locations in words, "Verified (report)", the Fidelity page opening on the boards not verified, "Also check the Styled drawing (F3, slower)".
- A proposal draws its busy state before the gate is asked (section 10). In a component test that wait moved the rest of the proposal to another thread, so a test's next click could be queued behind it and return before it was handled. On Windows the tests then passed once they waited for the outcome; in the Linux container 9 still failed. The Play page tests now register `ImmediateProposals` (in `PlayMaps.UseViewport`), a marker in Services that the Studio never registers: with it the page asks the gate without the wait (`DrawBusy` in Play). The wait itself is therefore checked in the Studio, not by a unit test.

**The local suite**, after one solution build with warnings as errors (0 warnings): Play 615, Rules 484, the Studio 264, Units 408, Units.Rendering 352, Maps 244, Authoring 167, Maps.Vasl 121 (28 skipped), Maps.Rendering 34 (2 skipped), Dice 24, Units.CounterSheets 19; no failure.

**The Docker Linux check** (`docker_check.sh ui-improvements`): on commit 3c79885 the restore, the build, and both regenerations passed and 9 Studio tests failed, as above; on commit 46a15a6 every step reports exit 0, with the same counts as the local suite.

**The chart supplement** regenerates identical to the committed `asl-scenario-a1.supplementary-source-registry.json` (compared as sorted JSON). Authoring did not change in this pass.
