# Scenario A1 Backlog Pass 22: The Card Editor

**Status:** Reviewed. Pass 22 of the [ASL Unit Scenario Card Games Plan](<ASL Unit Scenario Card Games Plan.md>), its last: a card editor in the Studio, the user's cards kept beside the saved maps, minimal cards for games with no scenario, and the Play page's new-game form replaced by a card choice. No package changes.

**Date:** 2026-09-30

**Plan:** the Scenario Card Games Plan, section 2 (pass 22), and the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), sections 1 and 5 (rulings R22.1 to R22.4). A referee (read-only) reviewed the rulings and the code; a table player made and played cards through the editor and the Play page in a clone, with bUnit tests.

**Design:** [ASL Unit Backlog Pass 22 Design](<ASL Unit Backlog Pass 22 Design.md>).

## Sources

A19.1 (a side's ELR), A26.4 (Balance), A16.1 (Battlefield Integrity), and E3.741 and E3.742 (Extreme Winter's date), all cited by the rulings of earlier passes; R0.3 for a named edge with no printed basis. The pass adds no rule text, counter, or package.

## Referee findings

| Finding | Disposition |
|---|---|
| The rulings against the plan and the user's ruling that every game is a card | Confirmed. |
| 12. A changed or deleted card half-applied: some checks refused it while others read the new text | Fixed: the planner reads a game's card only when its hash matches the one recorded. |
| 14. A copy of a built-in card lost its printed Battlefield Integrity totals and the playable hexrows' board | Fixed: both are edited and kept. |
| 3. Saving overwrote an existing user card under the same id (a second "-copy", the default id) | Fixed: a save replaces only the card being edited; the editor reports a taken id as it is typed. |
| 10, 11, 13. The card was hashed on every read, possibly from a different read than it was parsed from; the Victory validity cache ignored the hash | Fixed: text and hash are read together and cached by write time and length; built-in hashes once; validity keyed by hash. |
| 15. The picker added the catalog's first counter, not the one it showed | Fixed. |
| 2. The name pattern accepted a trailing line break | Fixed (`\z`). |
| 4. A reader could see a half-written card | Fixed: written to a temporary file and moved into place. |
| 8. R22.3 says a minimal card names no outcomes; validation did not check it | Fixed. |
| 16. A stale "moves first" note carried from one card to a new minimal card | Fixed. |
| 19. The Play page kept a card's read after the card changed | Fixed: its reads are keyed by the card's hash. |
| 20. The start summary said "ELR by OB group" for a minimal side with none | Fixed: "none". |
| 17. A named edge with basis `none` is rewritten to `manufactured` | Kept: a named edge no source prints is manufactured under R0.3; recorded in the design. |
| 18. Delete has no confirmation | Recorded (backlog section 32). |
| 21. The editor re-validates the whole card on every change | Recorded: fast enough for the three cards. |
| 24. No page test loads a game recorded without a card | Added. |
| 1. Out-of-scope items | Backlog section 32. |

## Table player findings

The table player tried 16 situations through bUnit: 11 passed and 5 failed. The file is kept as `TablePlayer22Tests`, its failures now fixed or turned into the expected refusals.

| Finding | Disposition |
|---|---|
| 1. Extreme Winter on a minimal card with no date: the editor accepted it, the game refused it | Fixed: the editor checks the SSR tokens against the date as the game sees it. |
| 2. A card deleted after a game started from it: the refusal said the card had changed | Fixed: it says the card is gone. Playing on without the card, and a warning before deleting a card in play, are backlog. |
| 3. A side ELR typed on a card with an OB was dropped silently | Fixed: refused with a message. |
| 4. An id with capitals showed as valid until Save | Fixed: checked as typed. |
| 5. Changing a side's nationality left the turn selects on the old side | Fixed: they follow the change. |
| 6. A stray comma for a card with no place | Fixed on the Play and Scenarios pages. |
| 14. The Scenarios page said "not evaluated yet" with empty citations | Fixed: it says whether the game evaluates the result. |
| 15. The picker on a minimal card gave no hint | Fixed: it says to add a group first. |
| 7 to 13, 15, 16. A quick game, a changed Guards copy, a copy moved to another board, two boards, night and mud, the old form's fields, renaming, the editor from a link | Passed. Group ELR by field, board checks at edit time, SSR citations, JSON messages, "bd01 bd02" side by side, renaming, and a warning when saving over a card in play are backlog (section 32). |
| The Guards card's SSR 3 note still says placing the OB "is pass 19" | Recorded: changing a built-in card's text changes its hash; backlog. |

## Live check

The Studio (`map-studio-scripted`): the editor copied The Guards Counterattack, added a counter with the picker, and saved it; the Play page listed it as "(yours)" with its start summary; a minimal card saved and deleted. The console errors were earlier reconnection attempts.

## Demonstration fixes

A demonstration to the user after the merge (2026-09-30) found five defects, fixed on `feature/asl-card-editor-polish` and retested in the Studio:

| Defect | Fix |
|---|---|
| The editor's fields ran together in one line: the page had no grid rule | A two-column grid for the editor, with the JSON areas at full width and the lists at their own width. |
| The first click on Save after correcting a field was lost: the button was still disabled when the field's change arrived | Save is never disabled; it checks the card when clicked and, when the card is invalid, says "Not saved" and why. |
| The Scenarios page did not show a minimal card's side ELR | Shown, with its ruling. |
| The Scenarios page's Victory line repeated a minimal card's text | It says whether the game evaluates the Victory Conditions. |
| The Play page showed an empty OB table for a minimal card | Replaced by a line saying that a minimal card's units set up by hand. |
