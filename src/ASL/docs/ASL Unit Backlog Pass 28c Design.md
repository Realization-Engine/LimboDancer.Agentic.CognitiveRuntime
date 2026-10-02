# ASL Unit Backlog Pass 28c Design

**Status:** Built. Pass 28c (The Play workspace and hardening) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, the fifth and last Studio pass.

**Date:** 2026-10-02

**Related documents:** the [Scenario A1 Backlog Pass 28c Review](<Scenario A1 Backlog Pass 28c Review 2026-10-02.md>); the plan's sections 11.1, 13.1, 13.4, 14, 15.1 to 15.10, 16.13, and 17.3; and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 37, 43, and 44.

A Studio pass adds no rulings. Its design decisions are recorded here and in the plan's section 15.10.

## 1. Outcome

Play is now a map-centered workspace. The user answered four questions on 2026-10-02:

- **R11 and the viewport.** The user went beyond the recommended markup move. R11 `PlayMapPanel` draws the map on the interactive B06 `BoardViewport`: pan, zoom, fit, and click a hex or a counter to pick it.
- **Backlog rows.** All four offered rows are built:
  - RuleHelp for the older panels' help paragraphs;
  - ready notes beside disabled Propose buttons;
  - the RPh headings split by task, and "Weapon" for the ordnance select;
  - the hand-over on load and an outcome hand-over button.
- **The activity strip.** The recommended form is built: the latest record in one line, with the existing lists below it, each grouped by turn and phase.
- **Hardening.** Play is fully hardened. The other pages were checked at the four widths and at 320px, and only what broke was fixed.

The pass stayed one pass. At the plan's rates the additions brought it to about 7:00, but recent passes ran at about half their estimates.

## 2. The workspace

| Width | Layout |
|---|---|
| 1440px and up | Actions left, map center, review right, activity strip below. The workspace fills the window under the sticky context, and each pane scrolls on its own. |
| 1024px to 1439px | Map, plus one side pane (the review over the actions), with the activity strip below. The navigation starts collapsed (section 11.1). |
| Under 1024px, and at 200% zoom | The review sits above labelled Map, Actions, and Activity tabs. The context scrolls away; its Review and hand-over buttons stay in a bar fixed to the window's foot. |

- **Context header** (`PlayContextHeader`). It holds:
  - the game and view picker;
  - "Turn N, <side> <phase>", with the revision apart;
  - a Live, Ended, or "Does not replay" badge;
  - the night and weather;
  - a link to the card and its Victory Conditions;
  - "Review: <action>" while a proposal waits;
  - a note on what the game waits for;
  - an announcement line.

  The header shows during a hand-over, since everything in it is known to both sides. The announcement and any proposal go with the last view.
- **Awaiting.** The page names the view whose turn it is:
  - the side with a pending choice;
  - the captors of a pending surrender;
  - the DEFENDER after a step or in the DFPh;
  - in the RPh, RtPh, and CCPh, where both sides act, the other side;
  - otherwise the phasing side.

  When that view is not the one shown, "Hand over to <side>" changes the view behind the hand-over screen. When it is, the header says "You act: ...".
- **Order in the actions pane.** A pending choice, a pending surrender, and an open declaration come first, then "Propose: end the <phase>", then the phase's panels.
- **Breakpoints.** The breakpoints that the script also reads are in pixels, so a larger default font cannot split them. The narrow layout follows the width in CSS before the script reports it; only the tabs need the script. The workspace's height comes from the context's measured height (`--play-context-height`, set by `playWorkspace.js`), not from a fixed offset.

## 3. The map (R11)

`GameMaps.Draw`, which inserted one SVG, is replaced by `GameMaps.Layers`. It returns:

- the board to load;
- the units the view may see (`layer-units`);
- the LOS line (`layer-los`);
- the marks: the Covered Arc and the Residual FP counters (`layer-marks`);
- the highlighted hex's points.

`PlayMapPanel` owns the viewport (section 15.5). It loads a board when the board changes, then sends each layer only when its text changes. A failed load shows its reason and a Try again button. The page caches the layers by game, revision, view, picked hex, LOS line, and the Gun whose arc is drawn.

A click picks the hex under it, and a click on a counter picks its hex. `PlaySelectedHex` lists what the view sees there; Locate in the units table does the same. Zoom in, Zoom out, and Fit are labelled buttons. The board host is a named group (`aria-roledescription="map"`) that takes the keyboard's + and - and 0.

The hand-over unmounts the panel, so a new view starts from no layers.

## 4. Review and confirmation (section 13.4)

- **Busy.** While the gate works, a second Propose or Confirm is dropped. Confirm and Cancel are disabled, the review pane is `aria-busy`, and the header shows "Working".
- **Stale.** A proposal records its revision. Confirm reads the game again first. A proposal made at another revision, whether after a reconnect, in another tab, or after another view's commit, is shown as Stale with both revisions and is never confirmed. The gate's expected revision still guards it too.
- **Focus.** A proposal takes the focus to the review. A confirmed hand-over takes it to the actions, opening the Actions tab when the window is narrow. `reveal` scrolls the pane into view below the sticky context even when it already had the focus.

## 5. The DEFENDER's pass (A8.1, A8.11)

Only the DEFENDER passes. In its own view, while the window on a step is open, a "Defensive First Fire" block above its fire panel says where the moving stack is, shows it on the map, and offers the pass. The phasing view's pass is the adjudicator's only.

The pass's reasons name the movers, so the DEFENDER's view reads only that it passed (ruling R23.1).

## 6. Backlog rows built

- **Section 37.** A page opened or reloaded on a game, or another game chosen in the picker, waits behind the hand-over. "Hand over to <side>" stands beside every outcome another view must answer.
- **Section 43.**
  - Ten help paragraphs are RuleHelp: Advance, Close Combat, BU, Mopping Up, Opportunity Fire, reaction CC, small arms fire, a thrown DC, vehicle CC, and vehicle movement. Their summaries were checked against the rules by the referee.
  - The RPh and ordnance panels' disabled Propose buttons carry a note.
  - The RPh has a heading per task: Rally, Repair, Deploy and Recombine, Shock and Unconfirmed Kills. The ordnance select reads "Weapon".
  - Records are grouped by turn and phase.

## 7. Hardening across the Studio

- **Contrast.** Text pairs measured 4.5:1 or better. The control border token measured 1.65:1, so it was darkened to 3.48:1 (`--control-line: #8f897a`), with a darker hover.
- **Reflow at 320px.** Controls never grow past their container. Toolbar and form children may shrink. Form grids stack under 40rem. Long tokens wrap. Wide tables scroll inside themselves under 40rem; the Play units table sits in a labelled scroll region. The Unit Lab stacks under 1024px.
- **Reduced motion.** Animation and transitions are cut, and the reveal scroll is instant.

## 8. Disclosure

- The map, the picked hex, and the records read the view, never the full state.
- `LatestRecord` reads only the per-view record lists.
- `WhenOf` reads the full history for turn and phase alone, which both sides know.
- The announcement, the proposal, and the drafts are cleared with the view.

## 9. Tests

- `PlayPagePass28cTests`: six page tests:
  - the hand-over on load;
  - a stale Confirm;
  - a step handed to the DEFENDER, who passes with no mover named, and the announcement cleared;
  - records grouped by turn and phase;
  - the narrow tabs, and a hand-over opening the Actions tab;
  - the wide regions.
- `PlayWorkspaceComponentTests`: six component tests:
  - the context header;
  - the picked hex;
  - record groups;
  - tabs with the caller's panels;
  - a ready note;
  - RuleHelp on Mopping Up.
- `PlayMaps`: the Play test contexts set up the viewport's script and read the layers the panel sent. The page tests that read the map's SVG now read those layers.
- The tests of the attacker passing now pass from the DEFENDER's view (`PassAs`). Games opened from the picker confirm the hand-over (`OpenGame`).
