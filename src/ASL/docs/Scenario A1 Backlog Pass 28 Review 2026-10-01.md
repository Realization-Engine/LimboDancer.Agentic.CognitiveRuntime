# Scenario A1 Backlog Pass 28: The Card Editor's Forms and Map Picking

**Status:** Reviewed and fixed. Pass 28 of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>).

**Date:** 2026-10-01

**Related documents:** the [ASL Unit Backlog Pass 28 Design](<ASL Unit Backlog Pass 28 Design.md>); rulings R28.1 to R28.5 in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5; the [ASL Unit Backlog](<ASL Unit Backlog.md>), section 42.

## The user's rulings

Four questions, answered on 2026-10-01: every card field as a form, the JSON shown read only (R28.1); boards composed slot by slot or taken from a saved map, and areas picked on the map (R28.2); rename and delete warn and name the games that use the card, with a built-in card's earlier hashes accepted, and the game-to-card index read from the live games on request (R28.4); a side id apart from its nationality left in the backlog. One pass, no split.

## Scope settled while building

- The editor dropped an Axis Minor side's nation when it rebuilt the card from its fields (pass 27 added the field after the editor was written). The forms keep it.
- The Studio's six games from The Guards Counterattack had recorded the card's hash from before the catalog 1.13.0 re-pin, so they were already refused; the SSR 3 revision changes nothing for them. The earlier hash accepted is the pass 27 text's, paired with the pass 28 text.
- The render endpoint could not draw a map built from placements (only saved maps), so the map service now remembers the recent ones.
- The user's own Studio ran under Visual Studio's debugger and held the main checkout's MapStudio output, so the build, tests, and Studio checks ran in a local clone on port 5179.

## Visual checks

In the clone's Studio (port 5179): The Guards Counterattack's copy on board 01 (every building area passes the whole-building check; a click on K5 picked K5 J4 J5 K4, which showed that counter lines kept the old area id: fixed with `RenameArea`); Gambit on board 4 with board 2 turned (exit hexes outlined on the south edge, a click removed one); a saved map of boards 3 and 4 copied into a minimal card (its LOS rule left out, with the note), and a building on board 3 picked whole with its board set; the Scenarios page through K10 to K12. After the fixes: the same pages again, the marks in their own layer, picking ending when the Victory Conditions are switched off, the side lists without the other side's nationality, and all four built-in cards valid against their boards (Tractor Works' nine-hex Factory joined across its interior walls). Clean.

## Referee findings

| # | Finding | Fix |
|---|---|---|
| 1 | The entry-edge check counted top-row hexes as on the left and right edges, unlike the game's entry rule, and ignored the playable area. | `OnEdge` uses the planner's rule (the edge the missing neighbour lies past); entry hexes outside an enforced playable area are named (R20.6). |
| 2 | Rowhouses and Factories butted across their outside walls joined into one building (B23.71), blocking a correct card's save. | The join stops at Rowhouse walls, Breaches, and outside Factory walls; interior Factory walls stay within (B23.74). |
| 3 | A building's half hex on a board seam belongs to the board placed later, so the check called a correct area wrong. | The check accepts a seam hex named under the area's board; picking still leaves it out (backlog section 42). |
| 4 | An earlier hash was accepted against any later text. | `EarlierRevisions` pairs each earlier hash with the text it was revised into. |
| 5 | A named edge with basis "none" was saved as "manufactured" unseen. | Naming an edge sets the basis to `manufactured` in the form, where the author sees it; the draft writes the basis as chosen. |
| 6 | Tokens on a non-token SSR were dropped silently. | Tokens are kept as typed and the R17.10 check reports them. |
| 7 | Round-trip losses: hexrows shown but not enforced, a note beside a named first side, "?" on an entry, an explicit false. | Each kept as read; a test covers them. |
| 8 | `RenameArea` moved Victory Conditions though another group's area kept the old id. | The conditions follow only when no area keeps the id. |
| 9 | Citation A2.5 for an entry hex off its edge; an empty edge flagged every hex. | A2.51; no check until the edge is chosen. |
| 10 | SSR 3 as `game-default`, R28.4, the index, and the saved-map LOS rules left out are sound. | None needed. |

## Table player (card author) findings

| # | Finding | Fix |
|---|---|---|
| 1 | Choosing the other side's nationality merged the two sides' references. | Each side's list leaves out the other side's nationality. |
| 2 | A hex-numbers area on a one-board card had no board list. | The board list always shows for hex-numbers. |
| 3 | The form said the first hex is a building's anchor; the id is. | The id is labeled "anchor hex" for a building, the hexes "the anchor among them". |
| 4 | A second board left one-board areas with no board. | `AddBoard` gives them the old board; replacing boards by a saved map, and the messages naming the missing board, are backlog. |
| 5 | Picking carried on after its target was removed, hidden, or no longer took hexes. | `CardDraft.Shows` ends it. |
| 6 | A removed board left an area's board list showing "choose". | The list adds "(not on the card)". |
| 7 | An entering group needs a setup order once any group has one, and nothing said so. | The field's title says so; a message naming the group is backlog. |
| 8 | Entry hexes were checked before the edge was chosen. | Fixed with referee 9. |
| 9 | Exit hexes are only checked for being on the map, not near their edge. | Backlog. |
| 10, 11 | Victory messages too general; mistyped SSR tokens accepted. | Backlog (section 32 already holds unknown tokens' cause, the game's parser). |
| 12 | Save under a new id made a copy without saying so. | Save reads "Save as a new card (keeps 'old')" when the id differs. |
| 13 | The confirmation understated what a game loses; saving a card in use asked nothing. | The confirmation names the turns, Victory Conditions, and SSRs; a note under Save names the games. |
| 14 | Edge basis "none" saved as "manufactured". | As referee 5. |
| 15 | SSR text after Remove or Up. | As UI 1 and 5. |
| 16 | A number typo hid the map's update; a condition's type change kept "at least"; the last group removed kept the side's ELR away. | The map follows the board fields; `ChangeType` clears the fields; the side gets the ELR back. |

## UI and Blazor findings

| # | Finding | Fix |
|---|---|---|
| 1 | Textareas showed stale text after a row shifted or a card changed. | Bound through `value`. |
| 2 | The picking target outlived its item. | As table player 5. |
| 3 | The outlines could be lost when a render overlapped the map's load. | One load at a time; the marks are sent after it. |
| 4 | Every change re-read every card file for the picker. | The choices are listed when the library changes. |
| 5 | No `@key` on shifting lists; removal by index. | `@key` on boards, groups, areas, lines, SSRs, outcomes, conditions, and Balance lines; removal by item. |
| 6 | Unbounded cache of placed maps. | At most 32 kept. |
| 7 | The index reads whole game logs. | Kept (on request only); backlog. |
| 8 | The confirmation did not take focus. | The confirm button takes it when the question opens; returning focus on Cancel is backlog. |
| 9 | Help text not associated. | `aria-describedby` on each control with help. |
| 10 | Repeated or contradictory accessible names. | Names carry the side, group, area, line, outcome, and condition; the pick buttons keep one label with `aria-pressed`. |
| 11 | A stale map while the card could not be built. | The map follows the board fields, and goes when a slot is not a number. |
| 12 | Contracts against the plan; the LOS layer reused for marks. | Plan section 16.16 and 16.17 updated (K19 retired, K13 and K15 contracts); B06 gains `SetMarks` with its own `layer-marks`. |

## Components

Extracted: K10 to K18 and K20 (all seven P1 candidates and K11, K12, K20 of the P2), with the new `CardMapPicker`, `CardGroupFields`, `CardAreaFields`, `CardRulesFields`, and `CardVictoryFields`, and the display phrases in `CardText`. K19 is retired: the forms replace the editable JSON, and S14 `JsonDisclosure` shows the card.

## Tests

MapStudio: `CardEditorPass28Tests` (the draft round trip of all four built-in cards, an Axis Minor nation, SSR tokens kept, lesser fields kept as read, the map's checks on a composed synthetic board, the earlier hash, the game-to-card index and the delete confirmation, an OB built from the forms, picking and rows following their items, the side lists, K12, the Source label); `TablePlayer22Tests` and `CardEditorPageTests` driven through the forms (`CardEditorDriver`), with the delete confirmation and Rename. Play: a deleted user card refused, and a game from the Guards card's earlier text reading the current one. The synthetic board has no building, so the whole-building join is checked in the Studio on board 01 and in the built-in cards' validity there (backlog section 42 holds a unit test on real board data).
