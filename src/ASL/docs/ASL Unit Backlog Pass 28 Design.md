# ASL Unit Backlog Pass 28 Design

**Status:** Built. Pass 28 (The card editor's forms and map picking) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, the sixth game pass.

**Date:** 2026-10-01

**Related documents:** the [Scenario A1 Backlog Pass 28 Review](<Scenario A1 Backlog Pass 28 Review 2026-10-01.md>), the [ASL Unit Backlog Pass 22 Design](<ASL Unit Backlog Pass 22 Design.md>) (the card editor and the user's cards), the [ASL Unit Backlog Pass 22d Design](<ASL Unit Backlog Pass 22d Design.md>) (B06 `BoardViewport`), and the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 29, 32, and 42.

Rulings R28.1 to R28.5 are in the [ASL Unit Backlog Passes Plan](<ASL Unit Backlog Passes Plan.md>), section 5.

## 1. Outcome

A card is made without editing JSON. The user answered the four questions on 2026-10-01: every field as a form with the JSON read only; boards composed or taken from a saved map; rename and delete warn and name the games that use the card, with a built-in card's earlier hashes accepted; and a side id apart from its nationality left in the backlog. The pass ran as one pass.

- **Forms (R28.1).** Every field of `asl-scenario-card/1` is a form. The card is shown as read-only JSON beneath them.
- **Boards and picking (R28.2).** Board rows (board, column, row, turned) or a saved map's boards; the card's boards drawn as one map, where a click picks a whole building, or adds or removes an entry or exit hex.
- **Checks (R28.3).** Areas and exit hexes are checked against the drawn boards as the card is edited; a field that is not a number is named in plain words.
- **Management (R28.4).** Rename, and confirmations before rename and delete naming the games from the card; the Guards Counterattack's SSR 3 revised.
- **Components (R28.5).** K10 to K18 and K20 extracted, with six new card components; K19 retired.

## 2. The draft

`CardDraft` (MapStudio, `Components/Cards`) holds a card as the forms edit it: strings for every number, so a half-typed value is reported rather than lost, and mutable lists for boards, sides, groups, areas, lines, SSRs, outcomes, and conditions. `CardDraft.From` reads a card (a built-in card as a copy under `-copy`), and `Build` writes it back with a list of problems, each a code and a plain message ("card.turns: the Game Turns is 'ten', which is not a whole number"). Only the fields of an area's kind and a condition's type are written, and an SSR's tokens only while its status is `token`. `ChangeSide` carries a side's new nationality to the Turn Record Chart, the Scenario Defender, and the Victory Conditions, and clears a nation that no longer applies; `RenameArea` carries an area's new id to its group's lines and the conditions naming it. A test reads each built-in card into the draft and checks that it builds back to the same text.

## 3. The map

`CardMaps` (MapStudio, `Services`) loads the card's boards through `MapService.LoadPlacements`, as a game's map is drawn, and the map service now remembers a map built from placements, so the render endpoint can serve it to the viewport. `HexAt` names the hex under a point with its board (`VaslMap.OwnerOf`); `Building` joins hexes across hexsides of building terrain on one board, the clicked hex first; `OnEdge` tells whether no hex lies beyond a hex on a side of the map; `Check` runs the edit-time checks; `Marks` outlines hexes for the viewport's overlay layer.

`CardMapPicker` draws the map on B06 `BoardViewport` and passes clicks to `CardEditorForm`, which owns the picking target (an area or an exit condition) and ends it when another card is loaded. Picking a building ends at once; entry and exit hexes are picked until Done. On board 01 every building area of the built-in cards passes the whole-building check, and Gambit's two-board map (board 2 turned) passes its exit-hex check.

## 4. Card management

`LivePlay.GamesFrom(card)` reads each live game's record and returns those whose `game-started` names the card. `CardSaveActions` shows Rename when a user card's id field differs from its file name, and both Rename and Delete open a confirmation that names those games. Rename saves first and deletes the old file only when the save succeeds.

`ScenarioCards.EarlierRevisions` lists, per built-in card, the SHA-256 of earlier texts that differ only in a note or status, and `ScenarioCards.SameCard` and `ScenarioCardLibrary.Matches` replace the five hash comparisons in the planner, the provenance, and the Play page. Starting a new game still requires the current text (R18.1). The Guards Counterattack's SSR 3 is now `game-default`; the Studio's six games from that card had recorded hashes from before the catalog 1.13.0 re-pin, so they stay refused as before.

## 5. Components

| Candidate | Component | Notes |
|---|---|---|
| K10 | `ScenarioCardView` | The whole read card for the Scenarios page; display phrases moved to `CardText`. |
| K11 | `CardSideSection` | Names an Axis Minor side's nation. |
| K12 | `CardSpecialRulesList` | Detailed on the Scenarios page, plain on Play's card panel. |
| K13 | `CardEditorForm` | The form root and the picking target. |
| K14 | `CardIdentityFields` | Identity, source, adaptation notes, aftermath; the Source label names `#edit-source-basis`. |
| K15 | `CardMapFields` | Board rows, saved maps, North, playable area; hosts `CardMapPicker`. |
| K16 | `CardTurnRecordFields` | With the first-move note when a die roll decides it. |
| K17 | `CardSideFields` | With `CardGroupFields` and `CardAreaFields` (new). |
| K18 | `ObCounterPicker` | One per group, offering the group's areas; one for a side's Balance counters. |
| K19 | retired | The forms replace the editable JSON; S14 `JsonDisclosure` shows the card. |
| K20 | `CardSaveActions` | Save, Rename, Delete, and the confirmation. |
| new | `CardRulesFields`, `CardVictoryFields`, `CardMapPicker` | SSRs; Victory Conditions with typed conditions; the map. |
