# ASL Card Editor Redesign Design

**Status:** Built on branch `claude/card-editor-redesign`, 2026-10-31. Approved by the user the same day ("Approve, build it"); the user asked for a demo before the merge.

**Scope:** the Studio's Card editor page (`/units/cards/edit`) and its components under `MapStudio/Components/Cards`. Presentation only: no rule, no check, no card format, and no saved card changes. `CardDraft`, the checks, save, rename, and delete keep their behavior.

## 1. What was wrong

1. One long scroll of eight boxes and about 90 inputs; Save and the problem list at the very bottom.
2. The map sat inside the Boards box, while the "Pick on the map" buttons sat in the sides and Victory, several screens lower.
3. Fields read as sentences ("day [0] month [0] year [0]", "hexrows [ ] to [ ] enforced on board [every]"), the 9rem label column wasted width, and the boxes ran past the window's right edge.
4. Problems were listed in one place, far from the fields they name.
5. A long introduction and ruling numbers stayed on screen all the time.

## 2. The design as built

- **The bar.** It stays at the top of the window while the page scrolls. It shows the card's title, its id, and its state ("a new card, not saved", "a copy of a built-in card, not saved", "your card, saved", "your card, changed and not saved"). Then come the problem count ("Valid" or "N problems"), which opens the full list with a button per section; **Open** (the card picker that "Start from" was); **More** (Rename, Delete, the games started from the card, View the card as JSON); and **Save the card**, the bar's only main button.
- **The section list.** The sections are Card, Map, Turns, one entry per side named by its nationality, Special rules, Victory, and JSON. Each shows a tick or its problem count. One section is shown at a time, and the address carries it (`?section=side-1`), so a reload keeps it.
- **Every section stays rendered; the others are hidden.** This keeps the page's elements and ids as before. A field's value, an open group, and the page tests all survive a switch. A pick in progress ends when another section is shown.
- **The map above the form.** The map is shown for Map, the sides, and Victory, and is hidden elsewhere. It stands above the section at every width (the user, after the demo; it first stood beside the form at 1280 pixels and wider). The whole board shrinks to fit within 40% of the window's height, so the form starts on the first screen; its summary folds it away. A browser that turns tall maps (pass 29) turns it on a phone, and the turned map then gets a box of its own height (a fault the demo found: the turned map drew nothing). On a phone the bar scrolls away with the page.
- **Problems where they belong.** `CardSections.Route` sends each problem to the section its field key names (`card.boards:` to Map, `card.ssr:` to Special rules). A side's problem goes to the side whose group, counter, or name it mentions. Each section lists its own problems at its top, without the key. A group's problems sit under that group, and its header shows their count. A problem no section can be named for is still in the bar's full list.
- **Panels and width.** The shown section is one padded panel, framed like the map above it. The editor keeps a working width of 96rem on a very wide screen, and a text field grows to 40rem at most.
- **Fields.** Labels sit above the fields. Widths are short for numbers and wide for text. The Card section puts the title first, and a new card's or a copy's id follows the title until the id is typed by hand; a saved card's id never follows, since its rename touches the games started from it. The date gets labelled day, month, and year. Source, legacy card, and adaptation notes are folded under "Provenance".
- **Map:** North is four buttons, and the playable area is a labelled row.
- **Turns:** Game Turns and the half turn, then Sets up first, Moves first, and Scenario Defender side by side.
- **A side:** Nationality, SAN, ELR, and BPV in one row; the Friendly Board Edge, its basis, and its note in the next. Balance is folded with its text in the summary.
- **An OB group:** a box that folds, its header showing name, ELR, setup order, "?" count, line count, and problems. Its areas and its counter lines sit side by side where the panel has room for both (about 1280 pixels and wider) and stack below that; the counter table takes only the width it needs, and the row that adds a counter line keeps its controls on one line. Each area is one line ("N4 building [M5] [N3] [N4] [N5]") that opens to edit and is outlined while its hexes are being picked. A new area starts open.
- **Special rules:** each SSR is a card with its number, status, Up, Remove, and text. Its tokens, cited rules, and note are folded under "Details", open by default for a rule the game reads.
- **Victory:** each outcome reads as a sentence: "[Russian] wins [at once, after any action,] if any of these holds:".

## 3. Choices made while building

- **Selects kept** for Sets up first, Moves first, Scenario Defender, and the Friendly Board Edge, where the proposal showed buttons. Each is a short native control that the keyboard already handles, and the page tests drive them by value. Only North became buttons; no test drives it.
- **Hidden, not dropped.** Rendering only the shown section would have meant rewriting the page tests and losing a section's local state. The page renders what it rendered before, so there is no cost under the performance guidelines.

## 4. The performance checklist

- **Added work:** each check routes its problems once (a few string tests per problem), and the bar's count reads the same list. Nothing is added per state, unit, or hex, and no map, game, or counter path changes.
- **Ownership and caches:** none added. The map viewport keeps its own load and marks; hiding its panel does not reload it.
- **Disclosure, snapshots, counters:** not touched.
- **Checks:** `CardEditorRedesignTests` (routing, the id from the title, one section at a time, the address); the existing editor page tests, unchanged, pass through the hidden sections.
- **Tests and CI:** no new test project; the new class joins the MapStudio share.
- **Outside outputs:** my Studio's build (`asl-gate-work/studio-6670`), the filtered test build, and the strict test build, all registered by their scripts and removed by the clean-up once CI passes.

## 5. Left out

Recorded in the [backlog](<../ASL Unit Backlog.md>), section 93: a problem shown under the very field it names; reordering special rules by dragging; and the full five-width walk by keyboard, which waits for the demo.
