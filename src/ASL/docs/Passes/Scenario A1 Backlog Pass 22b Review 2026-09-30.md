# Scenario A1 Backlog Pass 22b: The Theme and the Shared Foundation

**Status:** Reviewed. Pass 22b of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>): the supplied theme, the grouped navigation and drawer, page headers, the shared components, the card picker and new-game components, and card provenance. No package changes.

**Date:** 2026-09-30

**Plan:** the Redesign Plan, pass 22b (tasks 22b.1 to 22b.5) and section 17.2 (the visual check first). A UI and Blazor reviewer read the change; a table player read it and the rendered pages as a player would use them. Both were read-only.

**Design:** [ASL Unit Backlog Pass 22b Design](<ASL Unit Backlog Pass 22b Design.md>).

## Visual checks

Before the reviews, in the Studio (`map-studio-scripted`): the ten page headers; Play's new-game fields and start summary; the card picker on Play, Scenarios, and the card editor; the editor's findings and save feedback; the provenance panel on Scenarios for the three built-in cards and a user card, and in a game started from a card; Play at phone width. Fixed: the Unit Lab's starting-template list ran into the preview column, and its previews past the page edge; no space between the Scenarios picker and its link; the scripted-dice note split around the queue field; the start summary's "Month 5, year 1941" (now the card's own date); the provenance table's centered cells; an empty Counters table for a card with none. After the review fixes, checked again (Play's start and Balance, the provenance of all cards, the drawer by keyboard at 800px); fixed a repeated "moves first" and a CSS rule that lost to the table's own. Clean.

## UI and Blazor findings

| Finding | Disposition |
|---|---|
| 1. Play rebuilt the provenance model on every render | Fixed: built again only when the card, its hash, or the game changes. |
| 2. Escape worked only inside the navigation; a route change left focus on a hidden link | Fixed: Escape anywhere in the shell; the route change returns focus to the toggle. Focus into the opened drawer is backlog. |
| 3. No tests for the shell | Fixed: a test of collapse, the drawer, Escape, and a route change. |
| 4. A game whose card is gone showed nothing of what it started from | Fixed: Play lists the recorded card id, hash, and catalog. |
| 5. A blank Basis or Legacy showed as an empty field | Fixed: "not recorded". |
| 6. Play did not give a user card's file | Fixed: the card library gives the file to both pages. |
| 7. `RevisionNavigator` keeps showing a refused typed value | Backlog, before its first use (22c.3). |
| 8. The feedback's live region was inserted already filled | Fixed: the region is always present. |
| 9. The editor guessed the note's outcome from its first words | Fixed: set with each note. |
| 10. No page header on the board editor and viewer | Planned: tasks 22d.2 and 22d.3. |
| 11. Play's perspective select is not S09 yet | Planned: task 23.1. |
| 12. The shared `.toolbar` rule changes every page's toolbars | Checked in the Studio on Play, Games, the board editor, Unit Lab, Scenarios, and the card editor. |

## Table player findings

| Finding | Disposition |
|---|---|
| 1. Balance counters were counted into the OB | Fixed: on rows of their own, marked "Balance only (A26.4)". |
| 2. No side in the counters table | Fixed: a Side column; each side's counters apart. |
| 3. The start left out the Game Turns and who sets up first | Fixed: a line with both, and who moves first or that a dr decides. |
| 4. "SSR tokens: none" read as a card with no SSRs | Fixed: "SSRs the game enforces", with the number the card prints. |
| 5. The Balance choice mixed two meanings and hid the Balance text | Fixed: the dr option names the side the loser plays with its Balance, and each side's Balance is shown. |
| 6. A manufactured counter's record read as printed | Fixed: "sheet MFG entry ... values manufactured under R0.3". |
| 7. Values shown by ontology id | Fixed: plain names, the id as the title. |
| 8. The comparison shown as a token with a truncated hash | Fixed: the "physical" page, the comparison in words, the fragment id as the title. |
| 9. The ruling link did not name the ruling | Fixed: "Ruling R16.9, Backlog Passes Plan, section 5". |
| 10. "edge" instead of the Friendly Board Edge | Fixed: "Friendly Board Edges (FBE)" in the help, "FBE" in the start. |
| 11. The match badge ignores a catalog that differs | Backlog. |
