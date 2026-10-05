# Scenario A1 Backlog Pass 28c: The Play Workspace and Hardening

**Date:** 2026-10-02

**Branch:** `feature/asl-backlog-pass-28c`

**Related documents:** the [pass 28c design](<ASL Unit Backlog Pass 28c Design.md>), the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>) (pass 28c, sections 11.1, 13, 14, 15.10, and 17.3), and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 37, 43, and 44.

## The user's answers

Before the build, the user answered four questions:

1. **R11.** Adopt the interactive BoardViewport, not only move the map. The recommendation was the markup move.
2. **Backlog rows.** All four offered rows: RuleHelp for the older panels, ready notes, the RPh headings and "Weapon", and the hand-over on load with an outcome hand-over button.
3. **The activity strip.** The latest record, with the lists grouped by turn and phase (recommended).
4. **Hardening.** Play fully; the other pages checked and fixed only where they broke (recommended).

During the build, the user said the pass needs rigorous UI testing in the running Studio. The visual checks below are that testing. The user also asked whether the map should turn 90 degrees at 320px; that is backlog section 44.

## Visual checks

The Studio ran on port 6670. Two test games, `p28c-walk` and `p28c-walk2`, were built on the minimal user card from `p28b-visual2`'s setup.

**First check.** The page was checked at 1920x1080, 1366x768, 1024x768, 800x900, 683x384 (1366x768 at 200% zoom), and 320x640. It covered:

- the hand-over on load;
- a proposal moving focus to the review;
- a stale Confirm after a second tab committed;
- a Prep Fire attack and its grouped records;
- a step, the outcome hand-over to the DEFENDER, and its pass;
- the tabs' arrow keys and End.

It found eight issues, all fixed and checked again:

1. Under 1024px the sticky context took over a third of the window. It now scrolls away, and its Review and hand-over buttons sit in a fixed bar.
2. At 1024px the open navigation left the map 375px wide. The navigation now starts collapsed from 1024px to 1439px.
3. The activity strip ended 13px below the fold. The workspace now sizes from the context's measured height.
4. Review did not scroll to the review pane when that pane already had the focus. `playWorkspace.js` `reveal` fixes it.
5. The DEFENDER's view had no pass button.
6. The control borders measured 1.65:1. They are now 3.48:1.
7. At 320px, nine pages scrolled sideways. The causes were selects in toolbars, form grids, the Unit Lab's grid, tables clipped by `overflow: hidden`, a hidden span escaping its table, and a long commit hash. Every page now fits.
8. The map cache missed the chosen Gun, so its Covered Arc was not drawn. This was found by the tests.

**Second check, after the review fixes.** The page was checked at 1920x1080, 1366x768, 683x384, and 320x640. It played `p28c-walk2` through a whole turn:

- the RPh;
- a Prep Fire attack;
- in the MPh, a step, the hand-over, the DEFENDER's "Defensive First Fire" block, "Show it on the map", and its pass, whose review names no mover;
- the hand-over back, and the end of the move;
- the DFPh and the AFPh;
- in the RtPh, a surrender answered by its captor's side;
- the APh and the CCPh, into turn 2.

It found two issues, both fixed:

- The surrender panel's long button labels did not wrap.
- The both-sides note named the phasing side as "too".

At 200% zoom the hand-over opened the Actions tab with focus. At 320px no page scrolled sideways, and the map kept 465px of height.

## Referee findings

All fixed but one:

1. **The DEFENDER's pass review named the movers by id.** A concealed stack could keep its "?" while moving (A12.14), so this broke ruling R23.1. The DEFENDER's view now reads only that it passed.
2. **The ATTACKER could pass for the DEFENDER.** Only the DEFENDER passes, from its own view; the phasing view's pass is the adjudicator's only (A8.1, A8.11).
3. **Changing the game showed the new game's view without a hand-over.** It now hands over.
4. **The announcement line survived a view change.** It is now cleared with the view.
5. **The Covered Arc layer was stale.** Fixed by keying the map cache on the Gun.
6. **The revision count during hidden setup was already public** in the unit table's note and the board link. Left as it was; backlog section 44.
7. **Summaries.**
   - Mopping Up: the no-unconcealed-enemy condition is restored.
   - The thrown DC: "at the thrower's level" is removed. The full paragraph and the planner's level rule are backlog section 44.
   - Close Combat: now reads "none attacking or attacked more than once".
   - Reaction CC: adds "armed", "not in Melee", and "unless exempt".
   - BU: adds the Prep Fire and Stun limits.
   - Advance and small arms fire keep their paragraphs' scope (berserk advance, other firers) as before.

## Table player findings

Fixed:

- The DEFENDER sees where the moving stack is, beside its fire panel, and may show it on the map.
- A narrow hand-over opens the Actions tab.
- In the RPh, RtPh, and CCPh both sides are offered.
- The DFPh note says who ends the phase.
- What blocks play (a choice, a surrender, an open declaration) comes first in the actions.
- The advance button names the phase it ends.
- The context reads "Turn N, <side> <phase>", with the revision apart.
- The context says "You act" when the shown view acts.

Left to backlog section 44:

- The DEFENDER's fire panel showing for the whole MPh. The planner refuses fire with no stack moving, and the tests pin that.
- The advance button for every view.
- A picked hex filling the action drafts.
- The latest-record line leaving out moves, passes, and surrenders.
- A DEFENDER standing order to pass.

## UI and Blazor findings

Fixed:

- The announcement line across a hand-over.
- The Covered Arc cache.
- Focus into a hidden pane.
- A visible busy state: Confirm and Cancel disabled, `aria-busy`, "Working". Cancel during a Confirm is ignored.
- The workspace height from the measured context, not a fixed offset.
- The LOS form no longer clipped in the map pane.
- A failed map load caught, shown, and retryable.
- The board host given a role, and the duplicate "Map" landmark removed.
- The units table in a labelled scroll region.
- The narrow layout in CSS before the script reports the width.
- Pixel breakpoints.
- A missing revision never taken as "no check".
- Narrower exception filters.
- The review moved above the tabs.
- The disclosure test waits on r1's absence.

Left to backlog section 44:

- The navigation collapsing after the first paint.
- Strict JSInterop in the Play tests, and per-instance layer reads.
- The other pages' tables, which become blocks under 40rem rather than sitting in labelled regions.

## Components

- R11 `PlayMapPanel` is extracted, on B06 `BoardViewport`.
- `PlayContextHeader` and `PlaySelectedHex` are new; neither is a section 16 candidate.
- `InspectorTabs` can leave its panels to the caller.
- `ActionRecordList` and `FireHistory` take a turn and phase.
- `ProposalReviewPanel` takes Busy.
- `MovementWindowActions` takes MayPass.

## Tests

- MapStudio: 264 pass.
- The full local suite and the Docker check are recorded in the time log.
