# ASL Unit Backlog Pass 30 Design

**Status:** In progress 2026-10-03 on branch `feature/asl-backlog-pass-30`. The user answered the four questions of section 9 on 2026-10-03, each as recommended. Section 8 records what is built and checked in the Studio. Pass 30 (Prepared setups) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

**Date:** 2026-10-03

**Related documents:** the plan's section 15.11; the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>); the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 37 and 45; the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>).

A Studio pass adds no rulings. The gate's setup checks (rulings R19.1 to R19.6, R20.4 to R20.7, R23.3, R23.5, R23.6, R25.4, R26.1 to R26.5) are unchanged; the pass only fills the setup list faster and offers prepared lists.

## 1. Outcome

Setting up from a card today means typing every counter: its definition, an id, its OB group, and its Location. The Guards Counterattack's Germans are 26 counters.

After the pass:

- "Set up this group" fills the setup list with one row per counter the group still owes.
- A row's Location is picked on the map.
- A card may carry up to three prepared setups (plans) for the side that sets up first. That side chooses one, adjusts it on the map, and proposes it. The gate checks it like any setup.
- Each of the four built-in cards has up to three plans, reviewed by the user one card at a time.

A plan places the card's fixed OB and never changes it. Adding plans never changes a card's hash or its saved games.

## 2. What the code has now

- **The card** (`ScenarioCards`, `ScenarioCardLibrary`, Play library). Built-in cards are embedded from `src/ASL/units/scenarios/*.scenario-card.json`; a user's cards are files in `src/ASL/boards/cards`. A card's hash is the SHA-256 of its own text, and a game records it at its start (`ScenarioCards.SameCard` accepts the Guards Counterattack's earlier text too, `EarlierRevisions`).
- **The setup's state** (`ScenarioSetup.Check`, `GamePlanner.CardSetup`). A `SetupReport` lists each OB group with its order, what it still owes on board (`Remaining`), what it still owes off board (`OffBoard`), its "?" left, and its areas, and the order setting up now.
- **The page** (`Play.razor`). The setup block holds `SetupPoolsTable` (the report), the non-OB "?" checkboxes, `SetupPlacementEditor` (one counter's form), and `SetupPlacementList` (the rows waiting, as text with "remove", and "Propose setup"). The rows are the page's `placements` list, cleared by a hand-over, a game change, and a committed setup.
- **A new game.** The first setup proposal carries the start and the first placements, so the first side's first counters are typed before the game exists, with no map and no view. The gate already accepts a start with no placements.
- **The map.** Play hosts `BoardWorkspace`; the page keeps the picked hex (`highlight`) and cascades it as `PickedLocation`, and `LocationField` offers "Use E4 on board 01" (pass 29). `GameMaps.Layers` gives the `layer-marks` group. The card editor's `CardMapPicker` outlines picked hexes with `CardMaps.Marks`.
- **Sight at setup** (`GamePlanner.OutOfSight`, ruling R23.3). The groups of the order setting up now are out of the other side's sight.

## 3. Who sets up first, card by card

| Card | Sets up first | What it sets up | Areas | "?" |
|---|---|---|---|---|
| The Guards Counterattack (bd01, hexrows A to P) | German | One group, 26 counters: 13 squads, five leaders, six LMG, one MMG, one HMG | Five buildings (F5, K5, I7, M7, M9); every OB line names its building; I7 and M9 are one hex each | None |
| Gambit (bd04 over bd02 reversed) | British | One group, 20 counters: five set up on board (none a "?", at least two MMC), the other fifteen wait off board and enter on Turn 1 along the north edge | Hexes numbered 5 to 7 on board 4; one entry area | None |
| The Tractor Works (bd01, hexrows O to GG) | Russian | Sequential (A12.12): the 308th Rifle Division is order 1 (19 counters: 12 squads, the 9-2, the HMG, two MMG, three LMG); the Germans are order 2; the Russian remnants are order 3 | Building X3, nine hexes | 18 for the 308th |
| Armor Test (bd04) | Russian | One group, 7 counters: three squads, the 8-1, a crew, the 45mm Gun, the T-34 | Hexes numbered 5 to 10 on board 4 | 2 |

For The Tractor Works the plans are for the 308th only (the user's answer to question 2): the remnants set up after the Germans, so a plan made beforehand could not answer what the Germans did.

No plan takes the Balance. If the side has the Balance, its extra counter shows as still owed after the plan is chosen, and is added by hand.

## 4. Draft decisions

**D1. The game starts first, then the side sets up on the map.** A new game from a card with an OB gets a second button, "Start the game, then set up on the map": the setup action with the card's start and no placements. The game then exists, so the workspace draws its map and the page has views. The view opens as the side that sets up first, behind the hand-over screen, and "Set up this group", map picking, and the plans are offered there. "Propose setup" with counters typed before the start stays as it is.

**D2. One list, editable rows.** `SetupPlacementList` keeps one row per waiting counter. A row gains what a prepared row needs changed: its Location (typed, "Use E4", or "Pick on the map"), its holder (a SW's possessor, a Gun's crew or towing vehicle, a Passenger's vehicle) from the units in the list and on the map, a Gun's or vehicle's facing, and its "?". Hidden, broken, and Bore Sighting stay in the one-counter form; a row shows them as text. The one-counter form stays below for anything else.

**D3. "Set up this group" (30.1).** A button for each group the viewer may set up now: a group of the order setting up, of the viewer's side (any side for the adjudicator), and a group with counters still owed off board. It adds one row per counter still owed, and replaces rows it added before for that group.

- The id is generated from the side's letter, a short kind, and a number not yet used by the game or the list, such as `g-squad-1`.
- The Location is filled when the line's area is one hex (I7, M9); otherwise it is empty and the row says "pick a hex".
- A SW is given to the first unfilled squad of its own line's area, one SW per squad, then round again; the holder is changed in the row.
- An entering counter is set off board with its line's entry area. A counter that should enter by another of its group's entry areas is set up with the one-counter form, as before.
- A Gun that sets up on board is manned by the group's first free crew; a Gun that enters is in tow of the group's first vehicle. Either is changed in the row.
- A unit of a side that has vehicles may be put aboard one in its row, as a Passenger (ruling R26.2).
- A limited area (Gambit's five counters) owes "counters", not definitions: the button sets the group's whole OB off board, and the player unticks "off board" on five rows and gives them a Location.
- "Clear the list" empties the list.

**D4. Picking on the map (30.2).** "Pick on the map" beside a row marks that row as the one being picked; the next click on the map fills its Location at ground level, and the next row still without a Location is picked next, so a group is placed click by click. A level above ground is typed (`bd01:F5:1`). Every row's hex is outlined on the map with the number of counters the list puts there, the row being picked more strongly, so a plan is seen on the map before it is proposed. Once both sides have set up, the hexes that may take the viewer's non-OB "?" are outlined with a dashed line in another color, a ticked one filled and marked "?", and a click on one ticks or unticks its checkbox. The outlines are appended to the workspace's marks layer; they are the page's own drafts, so they follow the view like the list does.

**D5. Setup plans as data (30.3).** One file beside the card, `<card id>.setups.json`: embedded from `src/ASL/units/scenarios` for a built-in card (logical name `Scenarios.<id>.setups.json`), in `src/ASL/boards/cards` for a user's card. The card's own text, and so its hash, is untouched. A new static class `ScenarioSetupPlans` in the Play library reads and validates it; `ScenarioCardLibrary.Plans(name)` returns a card's plans.

```json
{
  "format": "asl-setup-plans/1",
  "card": "guards-counterattack",
  "plans": [
    {
      "id": "hold-the-street",
      "cardSha256": "ad43d29d...",
      "side": "german",
      "name": "Hold the street",
      "idea": "One or two sentences: what the plan is for.",
      "givesUp": "One sentence: what it leaves weak.",
      "terrain": ["A terrain or LOS fact the plan rests on, as the Studio reads it."],
      "placements": [
        { "id": "g-squad-1", "definition": "attacker-squad", "group": "german-1", "at": "bd01:F5:1" },
        { "id": "g-lmg-1", "definition": "attacker-lmg", "group": "german-1", "holder": "g-squad-1" },
        { "id": "b-squad-6", "definition": "british-elite-squad", "group": "british-1", "offBoard": true, "entry": "entry" },
        { "id": "r-gun-1", "definition": "defender-at-gun", "group": "russian-1", "at": "bd04:Q7:0", "holder": "r-crew-1", "facing": "north-east", "hidden": true },
        { "id": "r-dummy-1", "dummy": true, "at": "bd01:X4:0" }
      ]
    }
  ]
}
```

- A file holds at most three plans for a side; ids are unique in the file.
- A placement is what the page's row is: an id, a definition or a Dummy, its group, a Location or off board with an entry area, a holder, a facing, "?" or hidden, a Bore Sighted Location.
- Reading checks the form only: the format, the card's id, the side being the card's first side, each definition being in the catalog, each group being a group of that side. Whether the placements are legal is the gate's question, asked when the plan is proposed.
- A file that cannot be read gives no plans and a reason, shown where the plans would be; setup by hand goes on as before.
- `cardSha256` is the card text the plan was made for. A plan whose hash is not the hash the game recorded is offered marked "made for an earlier text of this card".
- A user's setups file is written by hand in this pass. An editor for plans, and a setups file following its card on Rename and Delete, go to the backlog (section 46).

**D6. Choosing a plan (30.4).** A new component, `SetupPlanPicker`, above the list. It shows when the game is in setup, the viewer is the side that sets up first (not the other side, not the adjudicator; ruling R23.3), a group of that side is in the order setting up now, and the card has plans. Each plan shows its name, idea, what it gives up, and its terrain facts, with "Use this plan". Choosing one replaces the list with the plan's rows; the rows are then adjusted like any others and proposed. The chosen plan's name stays above the list until the list is proposed or cleared.

**D7. The gate decides.** A plan is proposed through the same setup action. A stale plan (for another text of the card) or an illegal one is refused with the gate's reasons, as a typed setup is. Nothing of a plan is stored in the game: only its placements, as the events any setup makes. So a game saved before the plans were added opens unchanged, and a card's hash is the same with and without its setups file.

## 5. Disclosure

- The plans of a card are public, like the card: both players may read the file. What is secret is which plan was chosen and how it was adjusted.
- The picker shows only in the first side's view, while that side sets up. The other side's view and the adjudicator's never show it.
- The chosen plan, the rows, the row being picked, and the outlines live in the page, not in the game. `ResetForView` clears them, so the hand-over clears everything of the last view; a game change clears them too.
- The outlines are drawn only from the viewer's own draft rows and the viewer's own non-OB choices.
- Once proposed and confirmed, the placements are events with the visibility the gate gives any setup; the other side sees nothing of a group set up out of its sight (ruling R23.3).

## 6. Reading LOS and terrain offline for task 30.5

The plans rest on what the Studio reads, not on the printed boards from memory.

- **Terrain.** For each setup hex and the hexes around it: the hex's terrain and levels from the board's hex facts (`StudioBoard.Facts`), the same facts the inspector's Selection tab shows.
- **LOS.** `StudioLos.Check`, the read behind the inspector's LOS tab, between each candidate setup Location (ground and upper levels) and the Locations that matter: the enemy's setup hexes, the streets and open ground it must cross, the entry and exit hexes, and the victory buildings.
- **How it is run.** A small console script in the session's scratchpad loads the Studio's built assemblies and prints, for a card, a terrain table and an LOS table. It is not committed and it builds nothing in the repository. Every LOS fact a plan cites is then checked again in my Studio's LOS tab before the plan's screenshot is taken. If the script cannot load the Studio's services, the LOS tab is driven by script instead, which is slower but needs no code.
- **Victory Conditions.** Each plan says how it serves the card's conditions: building Control and the squad ratio (Guards), the exit hexes 2I1, 2Q1, and 2Y1 (Gambit), six hexes of X3 (Tractor Works), the exits near 4G10, 4Q10, and 4Y10 (Armor Test).
- **The gate first.** Each plan is chosen in my Studio and proposed in a test game, so the gate has accepted it before the user sees it.

## 7. The plans

Filled in task 30.5, one card at a time, each card's plans reviewed by the user before the next card is started.

## 8. Studio checks

Each task is checked in my Studio on port 6670 before it is committed; step 5 checks the whole pass.

**30.1, 2026-10-03**, at 1920x1080, by script:

- The Guards Counterattack, new game `p30-guards`: "Start the game, then set up on the map" proposed a start with no counter, and the gate accepted it. After Confirm the page waited behind "Hand the screen to the German side", then showed the map, the German view, and one "Set up this group" button, for the German group.
- The button filled 26 rows: the I7 and M9 rows with their Location (one-hex areas), the F5, K5, and M7 rows empty, each SW held by a squad of its own building. With the twelve empty Locations typed and one LMG given to another squad, the gate accepted the setup (26 events); after Confirm the list was empty, the group read "set up", and the Russian groups read "out of your sight".
- Armor Test, new game `p30-armor`, the Russian view: seven rows, the Gun manned by the crew with a facing, the tank with a facing. A tank placed in a building hex was refused with the gate's reason (`play.setup-vehicle`), and the rows stayed in the list.
- The hand-over to the German view emptied the list. "Set up this group" set the whole column off board in one click, the Gun in tow of the first vehicle. With the Gun moved to the truck, the crew aboard the truck, and the squad and leader aboard the halftrack, the gate accepted the setup (7 events).

**30.2, 2026-10-03**, at 1920x1080, on `p30-guards` in the Russian view, by script:

- "Set up this group" for the 308th outlined J2, M2, and N2 with 2, 3, and 1 counters. "Pick on the map" on the first empty row marked it; a click on N4 filled it and moved the picking to the next empty row; clicks on N3 and N5 filled the next two.
- With the second group added (12 Guards squads over E4, F3, G3, and G4, the 10-2 typed at `bd01:F3:1`), the map outlined eleven hexes with their counts, and the gate accepted the setup (25 events), the upper level included.
- After Confirm both sides had set up: the Russian non-OB "?" fieldset listed G3, N2, N3, and N4, and the map outlined the same four with a dashed line. A click on G3 ticked its checkbox and filled its outline with a "?"; a second click unticked it.

## 9. Questions for the user, and the answers

Asked 2026-10-03. The user took the recommended answer to each.

1. **The new-game flow (D1).** Recommended: start the game from the card with no counters, then set up on the game's map in the first side's view. The other way is to draw the card's map on the new-game form before the game exists; that needs a second map host on the page and has no view to respect, so the plans would show to whoever is at the screen.
2. **The Tractor Works.** Recommended: plans for the 308th Rifle Division in X3 only (order 1, with its 18 "?"); the remnants, order 3, set up by hand after the Germans.
3. **LOS offline (section 6).** Recommended: the scratchpad console script on the Studio's own assemblies, with every cited line checked again in the Studio's LOS tab. The other way is the LOS tab alone.
4. **Upper levels.** Recommended: a plan may place units on a building's upper levels where the board has them and the gate accepts them, since the Guards Counterattack and The Tractor Works are fought for buildings. The other way is ground level only.
