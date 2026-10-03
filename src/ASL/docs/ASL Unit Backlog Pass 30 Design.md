# ASL Unit Backlog Pass 30 Design

**Status:** In progress 2026-10-03 on branch `feature/asl-backlog-pass-30`: the setup workflow is being rebuilt after Claude Design's review (sections 10 and 11). The user answered the four questions of section 9 on 2026-10-03, each as recommended. Section 8 records what is built and checked in the Studio. Pass 30 (Prepared setups) of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 5, a Studio pass.

**Date:** 2026-10-03

**Related documents:** the plan's section 15.11; the [pass 29 design](<ASL Unit Backlog Pass 29 Design.md>); the [ASL Unit Backlog](<ASL Unit Backlog.md>), sections 37 and 45; the [pass 30 handover prompt](<ASL Pass 30 Handover Prompt.md>).

**The name.** The thing is a "setup plan" everywhere: in the page, the code (`SetupPlan`), the file (`<card id>.setups.json`), and the documents; "plan" alone is its short form. The user settled this on 2026-10-03. "Deployment plan" is not used: to Deploy is the rules' word for splitting a squad into two half-squads (A1.31), and A2.9 Deployment is itself checked at setup. The pass keeps its title, "Prepared setups", as the plan and the handover prompt name it.

A Studio pass adds no rulings. The gate's setup checks (rulings R19.1 to R19.6, R20.4 to R20.7, R23.3, R23.5, R23.6, R25.4, R26.1 to R26.5) are unchanged; the pass only fills the setup list faster and offers prepared lists.

## 1. Outcome

Setting up from a card today means typing every counter: its definition, an id, its OB group, and its Location. The Guards Counterattack's Germans are 26 counters.

After the pass:

- "Set up this group" fills the setup list with one row per counter the group still owes.
- A row's Location is picked on the map.
- A card may carry up to three setup plans for the side that sets up first. That side chooses one, adjusts it on the map, and proposes it. The gate checks it like any setup.
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

- A file holds at most three plans, all for the side that sets up first; ids are unique in the file.
- A placement is what the page's row is: an id, a definition or a Dummy, its group, a Location or off board with an entry area, a holder, a facing, "?" or hidden, a Bore Sighted Location.
- Reading checks the form only: the format, the card's id, the side being the card's first side, each definition being that side's in the catalog, each group being a group of that side, each counter having a Location on the card's boards, a holder among the plan's counters, or an off-board entry. A Dummy names its group, as a card's setup needs (ruling R19.5); the one-counter form now gives a Dummy the group chosen in it too. Whether the placements are legal is the gate's question, asked when the plan is proposed.
- A file that cannot be read gives no plans and a reason, shown where the plans would be; setup by hand goes on as before.
- `cardSha256` is the card text the plan was made for. A plan whose hash is not the hash the game recorded is offered marked "made for an earlier text of this card".
- A user's setups file is written by hand in this pass. An editor for plans, and a setups file following its card on Rename and Delete, go to the backlog (section 46).

**D6. Choosing a plan (30.4).** A new component, `SetupPlanPicker`, above the list. It shows when the game is in setup, the viewer is the side that sets up first (not the other side, not the adjudicator; ruling R23.3), a group of that side is in the order setting up now, and the card has plans. Each plan shows its name, idea, what it gives up, and its terrain facts, with "Use this plan". Choosing one replaces the list with the plan's rows; the rows are then adjusted like any others and proposed. The chosen plan is marked "In the list" until the list is proposed or cleared. A plan is offered only while every group it places is setting up now, so The Tractor Works' plans for the 308th are not offered again when the remnants set up.

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

Filled in task 30.5, one card at a time, each card's plans reviewed by the user before the next card is started. A plan's screenshot goes to the user in the chat, not into the repository.

### 7.1 The Guards Counterattack: the Germans

Made 2026-10-03 from the Studio's terrain and LOS on board 01 (Verified, LOS definitive), read with the scratchpad script, with fifteen of the cited lines checked again in my Studio's LOS tab: all agreed. Each plan was chosen in a new game in my Studio (`p30-gc-1`, `p30-gc-2`, `p30-gc-3`), and the gate accepted each (26 events). The user approved all three on 2026-10-03, with no remarks.

What the plans answer. The Russians move first. The 12 Guards squads (6-2-8) and the 10-2 start in building F3, two hexes from building F5; the 308th starts in N4, J2, M2, and N2. The Russians win by Controlling two more of the German buildings than the Germans Control of theirs, or by a three to one ratio of unbroken squads, so the Germans must keep their buildings and their squads through five turns.

Facts every plan rests on:

- I7 and M9 are one-hex stone buildings with a ground level only, so their groups have no choice of hex: every plan puts them there.
- F5, K5, and M7 are two-storey stone buildings (levels 0, 1, and 2). Their stairwells are in F6, J5 and K4, and L6.
- On this board the Studio's LOS from level 1 or 2 of a building hex is, with few exceptions, the LOS from its ground level.

**Plan 1: Forward line** (`forward-line`)

| | |
|---|---|
| The idea | Every platoon stands in the hexes that face the Russians, one squad to a hex on level 1, so each street the Russians must cross is under fire from two buildings from the first turn. |
| What it gives up | The F5 platoon starts in the LOS of the Guards at range 2 to 3 and takes their Prep Fire; squads alone in their hexes are attacked one at a time, and no squad is held back. |
| Terrain facts | F5 sees the street hexes E5, F4, and G5 at range 1, and is seen from E4, F3, and G4 at range 2, from J2 at 5, and from M2 at 7.<br>G6 sees G5 at range 1 and F4 and H4 at 2; H5 sees G5, H4, I5, and I6 at range 1.<br>J4 flanks the Guards' street: it sees H4 at range 2, G5 at 3, and F4 at 4, and J3, I4, and I5 at 1.<br>K4 sees J3, K3, L3, and L4 at range 1, and is seen from J2 and M5 at range 2 and M2 at 3.<br>L6 sees L5, K6, and M6 at range 1 and L4 and N6 at 2; M7 sees M6, N6, and N7 at range 1. Both are seen from M5 and N5 at range 2. |

| Building | Placements |
|---|---|
| F5 | F5, level 1: 4-6-7 squad with the LMG, 9-1 leader; G6, level 1: 4-6-7 squad; H5, level 1: 4-6-7 squad |
| K5 | J4, level 1: 4-6-7 squad with the LMG; K4, level 1: 4-6-7 squad with the LMG, 8-0 leader; K5, level 1: 4-6-7 squad |
| I7 | I7, ground level: 2 x 4-6-7 squad with the LMG, 4-6-7 squad, 9-2 leader |
| M7 | L6, level 1: 4-6-7 squad with the MMG, 8-1 leader; M7, level 1: 4-6-7 squad with the LMG; M7, ground level: 4-6-7 squad |
| M9 | M9, ground level: 4-6-7 squad with the HMG, 8-1 leader |

**Plan 2: Out of sight** (`out-of-sight`)

| | |
|---|---|
| The idea | The two wing platoons start where no Russian setup Location has a LOS to them, F6 and L7, so they are whole after the first Russian Prep Fire and meet the Russians inside their own buildings; the K5 platoon fires across the Guards' street from J4 and J5. |
| What it gives up | The Russians cross to F5, G6, H5, L6, and M7 without fire from the platoons that own them; three squads in one hex are one target once they are seen. |
| Terrain facts | F6 has no LOS to or from any Russian setup Location, at any level; it sees E6, F7, and G7 at range 1 and D5, D6, E5, and H6 at 2.<br>L7 at ground level has no LOS to or from any Russian setup Location; it sees L8 and M8 at range 1 and K6 and N7 at 2.<br>J4 sees H4 at range 2, G5 at 3, and F4 at 4. J5 sees H4 at range 2 and F4 at 4, and is seen from G4 at range 3, from the upper levels of G3 and from N5 at 4, and from E4 at 5.<br>K5 sees L4, L5, and K6 at range 1 and M6 at 2; it is seen from M5 at range 2, the upper levels of N3 at 3, and M2 at 4. |

| Building | Placements |
|---|---|
| F5 | F6, level 1: 4-6-7 squad with the LMG, 4-6-7 squad, 9-1 leader; F6, ground level: 4-6-7 squad |
| K5 | J4, level 1: 4-6-7 squad with the LMG; J5, level 1: 4-6-7 squad with the LMG, 8-0 leader; K5, level 1: 4-6-7 squad |
| I7 | I7, ground level: 2 x 4-6-7 squad with the LMG, 4-6-7 squad, 9-2 leader |
| M7 | L7, ground level: 4-6-7 squad with the MMG, 4-6-7 squad with the LMG, 4-6-7 squad, 8-1 leader |
| M9 | M9, ground level: 4-6-7 squad with the HMG, 8-1 leader |

**Plan 3: Tripwire and reserve** (`tripwire-and-reserve`)

| | |
|---|---|
| The idea | In each large building one squad with a machine gun watches the street from level 1, and the rest of the platoon waits by the stairwell, out of sight or nearly so, to fire at the Russians who get in. |
| What it gives up | Only one squad of each platoon fires as the Russians cross; the forward squads fight alone until the reserve comes up. |
| Terrain facts | H5 sees G5, H4, I5, and I6 at range 1 and F4 and I4 at 2; of the Russian setup Locations only E4 (range 3), G4 (range 2), and J2 (range 4) see it.<br>F6, the F5 building's stairwell hex, has no LOS to or from any Russian setup Location.<br>K4 sees J3, K3, L3, and L4 at range 1; J4 sees H4 at range 2, G5 at 3, and F4 at 4; J5, the K5 building's other stairwell hex, is seen from G4 at range 3, from level 2 of G3 and from N5 at 4, and from E4 at 5.<br>L6 sees L5, K6, and M6 at range 1 and L4 and N6 at 2; L7 at ground level has no LOS to or from any Russian setup Location. |

| Building | Placements |
|---|---|
| F5 | F6, level 1: 4-6-7 squad, 9-1 leader; F6, ground level: 4-6-7 squad; H5, level 1: 4-6-7 squad with the LMG |
| K5 | J4, level 1: 4-6-7 squad with the LMG; J5, ground level: 4-6-7 squad, 8-0 leader; K4, level 1: 4-6-7 squad with the LMG |
| I7 | I7, ground level: 2 x 4-6-7 squad with the LMG, 4-6-7 squad, 9-2 leader |
| M7 | L6, level 1: 4-6-7 squad with the MMG, 8-1 leader; L7, ground level: 4-6-7 squad; M7, level 1: 4-6-7 squad with the LMG |
| M9 | M9, ground level: 4-6-7 squad with the HMG, 8-1 leader |

### 7.2 Gambit: the British

Made 2026-10-03 from the Studio's terrain and LOS on the card's map (board 4 over board 2 reversed; LOS definitive), read with the scratchpad script, with eleven of the cited lines checked again in my Studio's LOS tab: all agreed. Each plan was chosen in a new game in my Studio (`p30-gb-1`, `p30-gb-2`, `p30-gb-3`), and the gate accepted each (20 events: five counters on board, fifteen off board to enter). The user approved all three on 2026-10-03, with the wall added to the facts of plans 1 and 3.

What the plans answer. SSR 2 lets the British set up only five counters, none under "?" and at least two of them MMC, in the hexes numbered 5 to 7 on board 4. The Germans (eight 5-4-8 squads and three leaders) then set up in the hexes numbered 8 to 10, seeing the five, and move first. The other fifteen British counters enter along the north edge in the British half of Turn 1. The British win by exiting 20 Exit VP off the south edge near 2I1, 2Q1, or 2Y1 within eight turns, so the five counters are there to see the Germans, to hold a foothold on a route south, and to live through one German turn alone.

Facts every plan rests on:

- Board 4 is level ground: woods, grain, a few wooden buildings, and dirt roads.
- The exit hexes lie ten hexes south of board 4's last hexrow. Board 2 is reversed, so 2Y1 is below 4I10 (the west), 2Q1 below 4Q10 (the centre), and 2I1 below 4Y10 (the east).
- The farmhouse O6-P6 is the only building with an upper level in the British area. A wall runs along the west and south sides of the yard hexes O7 and P7, directly south of it: the Studio's hex facts give wall hexsides O7-N6, O7-N7, O7-O8, P7-O8, and P7-P8, and none on O6 or P6 themselves. The LOS tab blocks P6's ground level from A9 at O7 ("Intervening hexside terrain"), and clears it from level 1. Added to the terrain facts of plans 1 and 3 at the user's word.
- The Studio's LOS counts grain as a hindrance. The card's date is May, when grain is not in season, so the hindered lines are at least as open as stated.
- A SW counts among the five counters, as the gate counts them: two squads, their two SW, and a leader make five.

**Plan 1: The farmhouse** (`farmhouse`)

| | |
|---|---|
| The idea | Two squads with both LMG and the 9-1 hold the upper floor of the farmhouse O6-P6 in the centre, on the road to the middle exit: a fire base that sees most of the German setup area and draws the Germans toward it while the battalion enters behind. |
| What it gives up | All five counters and the best leader stand in one wooden building two hexes from the German setup area, and the Germans move first with eight squads; the flanks are not watched from the ground. |
| Terrain facts | O6 and P6 are wooden building hexes with a level 1 and a stairwell each.<br>From level 1, P6 sees 47 of the 83 hexes numbered 8 to 10 on board 4, from A9 to GG10, and O6 sees 33; from the ground P6 sees 12.<br>O8, P8, and Q8 are at range 2 of P6, and Q9, O9, and N8 at range 3.<br>A wall runs along the west and south sides of the yard hexes O7 and P7, directly south of the farmhouse (hexsides O7-N6, O7-N7, O7-O8, P7-O8, and P7-P8). From P6's ground level it blocks the LOS to the south-west, to A9 for one; from level 1 the LOS passes over it, which is why the plan stands upstairs. |
| On board (5 counters) | P6, level 1: 4-5-8 squad with the LMG, 9-1 leader; O6, level 1: 4-5-8 squad with the LMG |
| Off board, to enter on Turn 1 | 2 x 4-5-8 squad with the 2-in. mortar, 4-5-8 squad with the ATR, 7 x 4-5-8 squad, 2 x 8-0 leader |

**Plan 2: West woods** (`west-woods`)

| | |
|---|---|
| The idea | A squad with an LMG at the south edge of the woods H5-H6-I5-I6 and a squad with a 2-in. mortar and an 8-0 deeper in them cover the western route, by the dirt road I1-I2-I3-H3 toward the exit below 4I10; the mortar may fire Smoke for the crossing (SSR 3). |
| What it gives up | Nothing east of hexrow Q is seen, so the Germans may shift there unseen; the 9-1 and both LMG but one stay with the main body, and the woods give less cover than a building. |
| Terrain facts | H5, H6, I5, and I6 are woods; I1, I2, I3, and H3 are dirt road hexes leading to them from the north edge.<br>H6 sees 32 of the 83 hexes numbered 8 to 10 on board 4, all from A9 to Q10: G8, H8, and I8 at range 2, E8 and G9 at 3.<br>I5 sees only 9 of them, K8 at range 4 the nearest, each through a hindrance of 2 to 4.<br>The Studio's LOS counts grain as a hindrance; the card's date is May, when grain is not in season, so the hindered lines here are at least this open. |
| On board (5 counters) | H6, ground level: 4-5-8 squad with the LMG; I5, ground level: 4-5-8 squad with the 2-in. mortar, 8-0 leader |
| Off board, to enter on Turn 1 | 4-5-8 squad with the LMG, 4-5-8 squad with the 2-in. mortar, 4-5-8 squad with the ATR, 7 x 4-5-8 squad, 9-1 leader, 8-0 leader |

**Plan 3: Two posts** (`two-posts`)

| | |
|---|---|
| The idea | One squad with an LMG watches from the upper floor of the farmhouse and one with an LMG and an 8-0 from the woods at H6, eight hexes apart: between them they see the German setup area from A9 to GG10, and the Germans cannot tell which route the battalion will take. |
| What it gives up | Each post is one squad that cannot help the other, and the farmhouse squad has no leader to rally it. |
| Terrain facts | From level 1, P6 sees 47 of the 83 hexes numbered 8 to 10 on board 4, from A9 to GG10; O8, P8, and Q8 are at range 2.<br>H6 is woods and sees 32 of them, all from A9 to Q10: G8, H8, and I8 at range 2.<br>H6 and P6 are eight hexes apart.<br>A wall runs along the west and south sides of the yard hexes O7 and P7, directly south of the farmhouse (hexsides O7-N6, O7-N7, O7-O8, P7-O8, and P7-P8). From P6's ground level it blocks the LOS to the south-west, to A9 for one; from level 1 the LOS passes over it, which is why the plan stands upstairs.<br>The Studio's LOS counts grain as a hindrance; the card's date is May, when grain is not in season, so the hindered lines here are at least this open. |
| On board (5 counters) | P6, level 1: 4-5-8 squad with the LMG; H6, ground level: 4-5-8 squad with the LMG, 8-0 leader |
| Off board, to enter on Turn 1 | 2 x 4-5-8 squad with the 2-in. mortar, 4-5-8 squad with the ATR, 7 x 4-5-8 squad, 9-1 leader, 8-0 leader |

### 7.3 The Tractor Works: the Russians (the 308th Rifle Division)

Made 2026-10-03 from the Studio's terrain and LOS on board 01, read with the scratchpad script, with fourteen of the cited lines checked again in my Studio's LOS tab: all agreed. Each plan was chosen in a new game in my Studio (`p30-tw-1`, `p30-tw-2`, `p30-tw-3`), and the gate accepted each (28, 30, and 31 events). The user approved all three on 2026-10-03, with every counter at ground level as the card's SSR 5 says.

What the plans answer. The 308th (12 squads, the 9-2, the HMG, two MMG, three LMG, and 18 "?") sets up first, alone in building X3. The Germans then set up around it: the assault engineers with two flamethrowers and four demolition charges in AA4, CC3, or Y8, Kampfgruppe Tienham in Y8, CC7, or AA4, and Kampfgruppe Stahler in U3, T4, R7, or T7. The Russian remnants set up last, to the west. The side that Controls six of X3's nine hexes at game end wins, and the Russians Control all nine at the start, so the 308th must still hold six after eight turns.

Facts every plan rests on:

- The card makes X3 a Factory (SSR 5), which has no upper levels. The game does not enforce that SSR and the board's data gives the building levels 1 and 2, so the gate would accept an upper level; the plans keep every counter at ground level, as the card says.
- The east face (Y3, Y4, Y5) is two hexes from AA4 and AA5; Y5 and X5 are two hexes from Y7 and Z6. The west face (W4, X2, X3) is two hexes from V2, and W4 two from U3.
- X4, the centre hex, has no LOS to or from any German setup Location, nor to any of the 24 hexes around the building that were checked.
- Every stack of real units is set up under "?", which the stone building allows (Concealment Terrain). Each concealed stack uses one of the 18 "?", and each Dummy uses one.
- The Studio's map does not draw a side's own Dummies: they are in the game and in the units table, but the screenshots show only the real counters. This was so before pass 30; it goes to the backlog (section 46).

**Plan 1: All round** (`all-round`)

| | |
|---|---|
| The idea | Every hex of the Works is held and looks the same from outside: a concealed stack and one Dummy in each. The HMG and the 9-2 stand at Y5, which sees more German setup Locations than any other hex, and two squads wait in the centre. |
| What it gives up | Most hexes hold a single squad, so the Germans may mass against any one face; the reserve is two squads. |
| Terrain facts | The east face looks at the assault engineers' buildings: Y3, Y4, and Y5 see AA4 at range 2, Y4 and Y5 see AA5 at range 2, and Y5 and X5 see Y7 and Z6 at range 2.<br>The west face looks at Kampfgruppe Stahler's building U3: W4, X2, and X3 see V2 at range 2, W4 sees U3 at range 2, and X2, X3, and W5 see U3 at range 3.<br>X4, the centre hex, has no LOS to or from any German setup Location, nor to any hex next to the building.<br>Y5 sees thirteen German setup Locations (AA4, AA5, Y7, and Z6 at every level, and CC7), more than any other hex of the building. |

| Hex | Units, each stack under "?" | Dummies |
|---|---|---|
| W4 | 4-4-7 squad with the LMG | 1 |
| W5 | 4-4-7 squad with the LMG | 1 |
| X2 | 4-4-7 squad | 1 |
| X3 | 4-4-7 squad | 1 |
| X4 | 2 x 4-4-7 squad | 1 |
| X5 | 4-4-7 squad with the MMG, 4-4-7 squad | 1 |
| Y3 | 4-4-7 squad with the LMG | 1 |
| Y4 | 4-4-7 squad with the MMG | 1 |
| Y5 | 4-4-7 squad with the HMG, 4-4-7 squad, 9-2 leader | 1 |

The plan uses all 18 "?": 9 for its concealed stacks and 9 Dummies.

**Plan 2: East front** (`east-front`)

| | |
|---|---|
| The idea | The weight faces the assault engineers and Kampfgruppe Tienham: two squads in each hex of the east face with the HMG, both MMG, and the 9-2, two more at X5, and two in the centre. The west face is one squad with an LMG at W4 and at X2, with Dummies at W5 and X3. |
| What it gives up | The west face is thin and half of it is Dummies; it leans on the remnants, who set up after the Germans, to keep Kampfgruppe Stahler busy. |
| Terrain facts | The east face looks at the assault engineers' buildings: Y3, Y4, and Y5 see AA4 at range 2, Y4 and Y5 see AA5 at range 2, and Y5 and X5 see Y7 and Z6 at range 2.<br>The west face looks at Kampfgruppe Stahler's building U3: W4, X2, and X3 see V2 at range 2, W4 sees U3 at range 2, and X2, X3, and W5 see U3 at range 3.<br>X4, the centre hex, has no LOS to or from any German setup Location, nor to any hex next to the building.<br>Of the two hexes held only by Dummies, W5 is seen from U3 at range 3 and from S5 and U8 at 4, and X3 from V2 at range 2 and U3 at 3. |

| Hex | Units, each stack under "?" | Dummies |
|---|---|---|
| W4 | 4-4-7 squad with the LMG | 1 |
| W5 | no unit | 3 |
| X2 | 4-4-7 squad with the LMG | - |
| X3 | no unit | 3 |
| X4 | 2 x 4-4-7 squad | - |
| X5 | 4-4-7 squad with the LMG, 4-4-7 squad | 1 |
| Y3 | 4-4-7 squad with the MMG, 4-4-7 squad | 1 |
| Y4 | 4-4-7 squad with the MMG, 4-4-7 squad, 9-2 leader | 1 |
| Y5 | 4-4-7 squad with the HMG, 4-4-7 squad | 1 |

The plan uses all 18 "?": 7 for its concealed stacks and 11 Dummies.

**Plan 3: Hidden core** (`hidden-core`)

| | |
|---|---|
| The idea | The east face, two hexes from the flamethrowers and demolition charges, holds only Dummies, three to a hex. The real force stands one hex back: three squads and the 9-2 in the centre, where nothing sees them, and the machine guns at X5, X3, W4, W5, and X2. |
| What it gives up | Y3, Y4, and Y5 are not defended: a German squad that enters them takes their Control, and the Russians must take back at least one to keep six hexes. |
| Terrain facts | The east face looks at the assault engineers' buildings: Y3, Y4, and Y5 see AA4 at range 2, Y4 and Y5 see AA5 at range 2, and Y5 and X5 see Y7 and Z6 at range 2.<br>X4, the centre hex, has no LOS to or from any German setup Location, nor to any hex next to the building.<br>X5 sees Y6, X6, and W6 at range 1 and Z5, W7, and X7 at 2, the ground south of the building, and Y7 and Z6 at range 2.<br>X3 and W4 are seen only from U3 and V2, and X2 from U3, V2, and T4; none of them is seen from the engineers' buildings. |

| Hex | Units, each stack under "?" | Dummies |
|---|---|---|
| W4 | 4-4-7 squad with the MMG, 4-4-7 squad | 1 |
| W5 | 2 x 4-4-7 squad with the LMG | 1 |
| X2 | 4-4-7 squad with the LMG | 1 |
| X3 | 4-4-7 squad with the MMG, 4-4-7 squad | - |
| X4 | 3 x 4-4-7 squad, 9-2 leader | - |
| X5 | 4-4-7 squad with the HMG, 4-4-7 squad | - |
| Y3 | no unit | 3 |
| Y4 | no unit | 3 |
| Y5 | no unit | 3 |

The plan uses all 18 "?": 6 for its concealed stacks and 12 Dummies.

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

**30.3 and 30.4, 2026-10-03**, at 1920x1080, by script, on a user card `p30-guards-copy` (a copy of The Guards Counterattack in `src/ASL/boards/cards`, with its own `p30-guards-copy.setups.json` of two plans), new game `p30-copy`:

- The German view offered both plans; the one whose `cardSha256` is not the game's read "Made for an earlier text of this card".
- That plan, which puts one squad in a Russian building and places a fourteenth squad, filled 27 rows and was refused by the gate with two reasons (`play.setup-pool`, `play.setup-area`).
- The Russian view and the adjudicator's view showed no plans. Each hand-over emptied the list; back in the German view no plan was marked as chosen.
- The other plan filled 26 rows and was marked "In the list". One squad was moved from F6 to H5 by "Pick on the map" and a click. The gate accepted the setup (26 events); after Confirm the list was empty and the plans were no longer offered.

## 9. Questions for the user, and the answers

Asked 2026-10-03. The user took the recommended answer to each.

1. **The new-game flow (D1).** Recommended: start the game from the card with no counters, then set up on the game's map in the first side's view. The other way is to draw the card's map on the new-game form before the game exists; that needs a second map host on the page and has no view to respect, so the plans would show to whoever is at the screen.
2. **The Tractor Works.** Recommended: plans for the 308th Rifle Division in X3 only (order 1, with its 18 "?"); the remnants, order 3, set up by hand after the Germans.
3. **LOS offline (section 6).** Recommended: the scratchpad console script on the Studio's own assemblies, with every cited line checked again in the Studio's LOS tab. The other way is the LOS tab alone.
4. **Upper levels.** Recommended: a plan may place units on a building's upper levels where the board has them and the gate accepts them, since the Guards Counterattack and The Tractor Works are fought for buildings. The other way is ground level only.

## 10. The workflow under review

**2026-10-03.** After the plans of three cards, the user asked how a plan is viewed, selected, seen on the map, and exchanged for another. A walk-through in my Studio (game `p30-demo`, The Guards Counterattack, 1366x768) showed that the flow works but the page buries it. The user paused the pass to think the workflow through with Claude Design. Tasks 30.1 to 30.4 stay as built until that review; The Tractor Works' plans (section 7.3) were approved and committed later the same day; Armor Test's plans are not started.

**The flow as built:**

1. A new game: the card, an id, "Start the game, then set up on the map", Confirm, then the hand-over to the side that sets up first.
2. In that side's view, the Setup block of the actions pane shows "Setup plans": one card per plan, with its name, idea, what it gives up, its terrain facts (collapsed), and "Use this plan".
3. "Use this plan" fills the setup list with the plan's rows and marks the plan "In the list". Nothing is in the game yet.
4. The map outlines each hex the list names, with the number of counters the list puts there.
5. "Use this plan" on another card replaces the list, and the outlines move.
6. A row is adjusted in the list or by "Pick on the map"; "Propose setup" at the foot of the list asks the gate; the Proposal tab shows its answer.
7. Confirm commits the setup. Only then are the counters drawn; the plans are no longer offered, and the choice cannot be undone.

**What the walk-through found:**

| Problem | Measure at 1366x768 |
|---|---|
| The plans are hard to find | The picker sits about 1,400 pixels down the actions pane (390 by 465 pixels), under the card's OB table, which wraps into a column 1,396 pixels tall |
| The plan, the list, and the button are far apart | The list starts about 1,450 pixels below the picker; "Propose setup" is below all 26 rows |
| The map preview is weak | Before Confirm the map shows outlines and counts, not counters; which squad, which level, and who holds a SW are only in the list |
| Plans cannot be compared as counters | Seeing a plan as counters means confirming it, which is final; each plan's screenshot for the user needed its own game |
| A side's own Dummies are not drawn on the map | In the game and the units table, but not on the map (so before pass 30); a plan that leans on Dummies looks empty where they stand |

**The second walk-through, 2026-10-03.** Claude Design cannot reach the Studio, so Claude Code walked the flow again and took 29 screenshots for it (S1 to S28 and S20b), each sent to the user with what it shows: The Guards Counterattack at 1366x768 from the new game to the committed setup and the other views (S1 to S16), a reload in the middle of setup (S17, S18), Gambit (S19, S20, S20b), The Tractor Works' Dummies (S21), the same flow at 1920x1080, 800x900, and 320x640 (S22 to S27), and the keyboard (S28). The images are not in the repository. It found, beyond the table above:

| Problem | What was seen |
|---|---|
| A reload forgets the game | A game started with "Start the game, then set up on the map" does not put its id in the address, so a reload opens the empty new-game form (S17) |
| A game reopened in setup opens on the wrong side | Chosen again in "Game", it waits behind the hand-over of the side that moves first, not the side setting up; that view has no plans (S18) |
| A map pick drops the level | "Pick on the map" fills ground level, so a row at `bd01:G6:1` became `bd01:F6:0` without saying so (S9) |
| Another plan discards the adjustments | "Use this plan" replaces the list without a question (S10) |
| A refusal does not lead to its row | The reasons name a counter id and a Location; the row is not marked or linked, and is up to 2,000 pixels away (S13) |
| The review says little | "play.setup: 26 event(s)"; it does not name the plan (S12) |
| Nothing says the setup is final | After Confirm the plans and the list are simply gone (S14) |
| The setup's button sits among the next actions | "Propose setup" is followed at once by "Propose: end the Rally Phase" while setup is open (S8) |
| Narrow windows split the task over tabs | Choosing a plan leaves the page on the Actions tab; the outlines are on the Map tab (S24, S25) |
| The outline's number leaves out SW | Gambit's five counters on the map show as 1 and 2 (S19) |
| A row offers "?" where the card forbids it | Gambit's five on-board counters (S20) |
| Rows read as ids | "g-squad-1: attacker-squad (german-1)", not the counter's printed values (S7) |

Two things read better than expected: at 800 pixels the three plans fit one screen, because the actions take the page's width (S24); and by keyboard the first "Use this plan" is 13 Tab presses from the top of the page, since Tab skips the OB table (S28).

**Claude's proposals, not built, for the review to weigh:**

- A. The picker first in the Setup block, above the OB table, with the OB table collapsed while plans are offered.
- B. Compact plan cards: the name, one line of the idea, and "Use this plan"; the rest behind a disclosure.
- C. The list's rows drawn on the map as counters marked as a draft, in place of the outlines, so each plan is seen as it will look before anything is proposed. Only the viewer's own draft is drawn.
- D. "Propose setup" and "Clear the list" repeated above the list, beside the chosen plan's name.

**Limits any redesign keeps:** a plan is offered only in the view of the side that sets up first (ruling R23.3); the hand-over clears the chosen plan, the list, and what the map drew of them; a plan places the card's fixed OB and never changes it; a plan reaches the game only as placements proposed through the gate, so adding plans never changes a card's hash or its saved games.

## 11. The workflow rebuilt

**2026-10-03.** Claude Design reviewed the workflow from the 29 screenshots, section 10, and the code (its report, "Choosing a setup plan", is the user's PDF, not in the repository). The user accepted its model and took, on Claude Code's recommendations: its steps 1 to 20 in this pass; refusals matched by the page, with no change to the gate; and Armor Test's plans after the rebuild. Its steps 21 to 23 go to the backlog (section 46): the comparison table, hex names in terrain facts as links, and a draft kept across a reload.

**The model.** Two changes; the rest is layout.

- **Setup is a mode of the actions pane.** While a card's setup is not complete, the pane shows only setup: a bar at the top and three tabs, Plans, Counters, and Card OB. The Play actions are not shown, since the gate refuses every one of them until setup is complete (`play.setup-incomplete`, ruling R19.2). They return once every group has set up, when the first of them starts play.
- **Showing a plan is not choosing it.** "Show on map" draws a plan as draft counters and leaves the list alone. "Use this plan" fills the list, and asks first when the list has changes. The map shows one source at a time: a plan, or "My list".

**The setup bar.** The side setting up, the group, how many counters are in the list, the plan the list came from and how many changes it has, "Propose setup", and "Clear". Propose and Clear live only there. In a view that may not set up now, the bar says who is setting up.

**The tabs.**

| Tab | What it holds |
|---|---|
| Plans | Compact cards: the name, the first sentence of the idea, "Gives up", "Show on map", "Use this plan", and "More" (the whole idea and the terrain facts). The plan in the list is marked, with "Reset to <plan>" when it has changes. Offered only in the first side's view (ruling R23.3). With no plans the tab is absent and a line says so. |
| Counters | "Set up this group" for each group the viewer may set up. The list grouped by stack: a header with the Location in words, its level (from the hex's own levels), and "Move"; under it one line per counter in printed values, its id after it in small type; a SW under its holder. Sections "On the map" and "Off board, to enter" (collapsed). A limited area's header counts as the gate does ("5 of 5 allowed, at least 2 MMC"), and "?" is left out where the card's area forbids it. The non-OB "?" once both sides have set up. "Add a counter by hand" at the foot, closed. |
| Card OB | The OB table, without its "Set up" column. |

**The map.**

- The list's rows, or the plan shown, are drawn as draft counters with the counters' own art: stacks, level tabs, SW under their holders, "?" on concealed stacks, and Dummies. A draft is marked as one, and its hex keeps the gold outline. The draft is the page's own and goes with the view (ruling R23.3).
- A switcher in the map's toolbar: each plan and "My list"; `[` and `]` step through it when the map has the focus.
- A side's own Dummies are drawn in its own view, in the draft and in the game. The other side sees a sealed "?" as before.
- A click on a hex the list uses selects that stack in the list and does not change the inspector's tab. "Move" on a stack or a counter, then a hex click, moves it and keeps its level when the new hex has that level; a banner on the map says what is being moved.
- The map zooms to the setup areas when setup opens and when a plan is shown.

**Propose, refusal, and the end.**

- The review names the plan and its changes, counts counters and hexes, and says "Confirm is final: the setup cannot be changed afterwards." The stray "Picked" line is gone from it.
- A refusal: each reason that names a row's id links to that row and its hex; those rows and the bar are marked ("2 problems"). A SW refused only with its holder is folded under the holder's reason. The page matches the ids it owns; the gate's reasons stay plain text.
- After Confirm, a notice replaces the bar: what was set up and from which plan, who sets up next, and "Hand the screen to the <side> side".

**The bugs fixed first.**

- A game started from the new-game form puts its id in the address, so a reload opens it.
- A game opened during setup waits behind the hand-over of the side setting up now, not the side that moves first. A new game opens the same way, so the pass's `openAs` field goes.

**Under 1024 pixels.** "Show on map" and "Use this plan" open the Map tab; the switcher sits on the map, and the setup bar stays in view on both the Map and the Actions tabs.

**Build order** (Claude Design's numbers in brackets), each group checked in the Studio and committed:

1. The fixes [1 to 7]: the address, the hand-over's side, the level kept on a pick, asking before a list is replaced or cleared, printed values, the review, the notice after Confirm.
2. The setup mode [8, 9, 10, 13]: the bar, the tabs, compact plan cards, no Play actions during setup.
3. The Counters tab [14, 15, 11]: stacks, sections, the limited area's count, the list and the map tied together.
4. Refusals [12].
5. Draft counters, own Dummies, "Show on map" and the switcher, the zoom [16 to 19].
6. Under 1024 pixels [20].

**Estimate.** About 4:00 for the rebuild, so pass 30 grows from 5:45 to 9:45 (build 8:30).
