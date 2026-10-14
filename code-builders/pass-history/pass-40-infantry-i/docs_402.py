"""Pass 40 task 40.2: the design's section 8 (first part) and the time log's row. Appends; asserts the anchors."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"

SECTION = r'''
## 8. Roads and movement in the open: 40.2, 40.3, 40.8 (increment 2b)

### 8.1 Task 40.2: the road

Built on 2026-10-11 and waiting for the user's word before its commit.

**The rules, as read on their pages.**

- **A4.132 (p. 48), with both examples:** Infantry entering a hex across a road hexside pays the road's cost or the cost of the hex's other terrain. Paying the other terrain's cost it is in that terrain. Paying the road's, it is subject to FFMO if the LOS can be traced to either of two target points without crossing an obstruction in that hex: the hex center dot, or the point where the hexside crossed meets the road. A Hindrance or Artificial Terrain there negates the FFMO.
- **B3.3 (pp. 113 to 114):** the other terrain of a road hex gives its TEM; a unit that moved in at the road rate is subject to Interdiction and FFMO in its place, unless the LOS is traced through other terrain between the firer and the target points.
- **B3.4, B3.43 (p. 114):** the Road Bonus, for a unit that crosses only road hexsides in its MPh; not for one that pays extra MF for the cover of woods.
- **B13.31 (p. 128):** a mover in a woods-road hex has no woods TEM in Defensive First Fire, and takes FFMO, if the LOS does not cross a woods symbol and it entered at the road rate; "otherwise, the normal woods TEM is in effect", and a vehicle there is always on the road.
- **B13.32, B13.41 (p. 128):** a vehicular OVR in a woods-road hex takes the woods' +1 but against a vehicle on the road; a vehicle may leave the road into the woods of its hex at the woods' cost.

**What the code did** (read on its lines, and seen in the Studio on board 5, section 5.5): a hex whose center dot is on a road read as Open Ground for everything; a road hexside was always crossed at the road's cost; fire never read how a hex was entered.

**The change.**

1. **A woods-road hex is woods** (`ScenarioA1Definitions.IsWoodsRoadHex`, read by Play's `TerrainKey`): a road at the center dot and Woods on one of the hex's hexside Locations. That is VASL's own test (`VASLBoard.fillWoodsRoadHexes`, which the map builder already ports for the NoWoodsRoads SSR). By it boards 1 to 4 have no such hex and board 5 has eleven ([I4], [J3], [K3], [L2], [M3], [N3], [O4], [P4], [P5], [Q6], [R6]; `pass32-tools/pass40/woods_road.py`), so no game played before the pass changes. Such a hex now costs two MF across a hexside without a road, gives its TEM to a unit that is not a mover at the road rate, and is woods for a rout's cover and for concealment.
2. **The mover's choice** (`GroundStep`, a new argument; the move's new argument `terrainRate`; a checkbox on the Movement panel): across a road hexside into a hex that has other terrain the unit pays the road's cost unless it chooses the other terrain's. The choice is refused where there is nothing to choose: no road hexside, a hex of Open Ground, an entry from off board, a Bypass, an obstacle occupied. Paying the other terrain's cost gives up the Road Bonus (B3.43).
3. **The step records that it was at the road rate** (`MovementStepped.RoadRate`, the event's `roadRate`, carried in the movement state): the unit is on the road and not in the other terrain. SMOKE in the hex ends the Road Bonus and not this.
4. **The two target points, in Maps** (`LosTargetPoints.CrossesTerrainInTargetHex`, a new file of 45 lines beside the LOS calculator): whether the straight line from a firing hex's LOS point to the target hex's center dot, or to the edge point of the hexside crossed, passes inside the target hex over a cell that is neither open nor hexside terrain. VASL's walk is not touched: its LOS to a center Location never asks about the target hex's own terrain. The LOS up to the hex stays the calculator's.
5. **Fire reads it** (`FireMapFacts`, with `ScenarioA1Definitions.RoadRateTargetTerrain`): for direct fire in the MPh at a stack that entered its Location at the road rate, the target terrain is Open Ground when every firing Location reaches one of the two points clear (A7.52: the worst case of a group's Locations is the group's), and the Location's terrain otherwise. Open Ground then brings FFMO by the rule already built, with its own bars (a Hindrance, an AFV or wreck, Height Advantage, a wall or hedge).
6. **A vehicle in a woods-road hex is on the road** (B13.31 EXC): fire at vehicles alone there reads Open Ground.

**Rulings proposed.**

- **R40.4:** a woods-road hex is named as VASL names it. Orchard, brush, and grain beside a road at the center dot are not read this way (none is on boards 1 to 5); a backlog row.
- **R40.5 (amends R10.1):** the road's cost is the default across a road hexside and the other terrain's cost the mover's choice, declared with the step.
- **R40.6:** the point "where the hexside crossed intersects the road" is the edge point of that hexside, which VASL keeps for every hexside. On the example it gives the page's answer for all twelve cases.

**What the run settled (question F4).** No LOS to a point had to be added to the VASL walk, and the Maps change is far under the hour set as its limit. The test `LosTargetPointTests` runs the page's example on the real board 5: [G2], [J3], [K3], and [L2] reach [I4]'s center dot clear; [I5] and [J4] reach neither point clear; [H4] and [I3] reach only the H3-I4 hexside; [I8] reaches neither point of [I9], and [I10] reaches its road hexside.

**The games,** on board 5, written by `make_board_game.py`. All three are **recorded**; the lines are from the game files.

| Game | What it holds |
|---|---|
| `p40-road` | The squad enters [I4] from [H3]: "(woods) for 1 MF, at the road rate: on the road, not in the hex's other terrain (A4.132)". First Fire from [I5]: woods +1, FFNAM -1, no FFMO. From [H4]: FFNAM -1, FFMO -1. From [G2], at range 2: FFNAM -1, FFMO -1. |
| `p40-road-control` | The same squad enters [I4] with the other terrain's cost chosen: "(woods) for 2 MF". First Fire from [H4]: woods +1, FFNAM -1. |
| `p40-road-building` | The squad enters the building [I9] by the road of [I10] for one MF, at the road rate. First Fire from [I8], behind the building: wooden building +2, FFNAM -1. From [H9] and from [J9]: FFNAM -1, FFMO -1. |

The frame's game `p40-bd05-road` holds one step recorded by the build before this task; it is left as the record of the fault and is not a fixture.

**Tests.**

- **Maps, new:** `LosTargetPointTests` (two tests on board 5; they run where a VASL checkout is configured, as the other board tests do, so not on CI).
- **Rules, new,** in `ScenarioA1Pass40RulesTests`: a woods-road hex by its facts; the road's cost against the other terrain's, with SMOKE, Open Ground, and a hexside without a road; the terrain a firer reads from its two points, for one Location and for a group; the move's summary.
- **Play, new:** `BacklogPass10Tests.AMoverAtTheRoadRateIsOnTheRoadForAFirerThatSeesItAndInTheWoodsForOneThatDoesNot`, with the terrain cost chosen as its control. Its LOS stub answers the target points by the firer's hex.
- **Run:** 57 Rules tests in three classes and 99 Play tests in five classes (movement, entry, vehicles, fire, the rout), all passed; the two Maps tests passed.

**Left out, each for a backlog row.**

- On a map of several boards the two target points are not read, so a mover at the road rate keeps the Location's TEM and takes no FFMO there, as before the pass. Gambit and the Armor Test stand on such maps; they have buildings beside roads and no woods-road hex.
- Interdiction of a rout at the road rate (B3.3) and Residual FP against a mover at the road rate still read the Location's terrain.
- A vehicle that leaves the road inside a woods-road hex (B13.41) is not tracked: it pays the woods' cost across a hexside without a road and is then read as on the road.
- Orchard, brush, and grain beside a road at the center dot (ruling R40.4).
- A step that spends MF inside the Location after a road-rate entry (a SMOKE attempt) writes a new step, and the unit is then read as in the terrain.

**Not seen or not run.** A vehicle in a woods-road hex, a Fire Group from two Locations, a Bypass of a woods-road hex, a rout into one, and the Movement panel at narrow widths were not staged. The page's component tests (`MovementComponentTests`), the replay digest, the sweep, and the text list were not run. Texts added on purpose: the refusal `play.move-terrain-rate` (two wordings), the move's road-rate clause, and the checkbox's label and title.
'''

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "## 8. Roads and movement in the open" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 2a committed at the user's word, with yes to the rulings, the reading of A5.13, and A5.132 to pass 50 (79c1e663). Increment 2b, task 40.2, the road: "
       "how Maps reads a LOS and what it keeps for a hexside (the edge point; the hex of a cell); the two target points of A4.132 as a read beside the calculator "
       "(LosTargetPoints), run on the page's example on board 5 with all twelve cases right at the first run; VASL's own test for a woods-road hex and its count "
       "on boards 1 to 5 (none, none, none, none, eleven); the woods-road terrain fact, the mover's choice with its argument and checkbox, the step's roadRate "
       "record, and fire's read of the two points; MapStudio built with warnings as errors (three tries: a missing using, a name used twice); three games on "
       "board 5 played with every commit read from the file; five Rules tests, one Play test, two Maps tests; the design's section 8.1 "
       "| 20:17 | 20:42 | 25 min | Estimate 0:40 in the plan, 1:15 in the frame. The user asked in the middle that the new code be tested in games on board 5; "
       "the three games stand on the example's own hexes. Question F4 settled without a change to the VASL walk. Not run: the page's component tests, "
       "the replay, the sweep, the text list. Not staged: a vehicle in a woods-road hex, a group from two Locations, narrow widths. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
assert text.endswith("after the repair. |\n"), text[-60:]
open(path, "w", encoding="utf-8", newline="").write(text + ROW)
print("ok")
