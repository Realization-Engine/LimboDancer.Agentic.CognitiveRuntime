"""Pass 40 task 40.3: the design's section 8.2 and the time log's row. Appends; asserts the anchors."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"

SECTION = r'''
### 8.2 Task 40.3: Dash

Built on 2026-10-11 and waiting for the user's word before its commit.

**The rules, as read on their pages.**

- **A4.63 (pp. 51 to 52), with both examples:** Infantry declares a Dash to a particular Location before it moves, then moves from a non-Open Ground Location on one side of a road directly into the road and directly into a non-Open Ground Location on the other side, if the normal MF for the two-hex move is within its MF. A Location out of a firer's LOS counts as not Open Ground. The unit may have spent no MF before (but for a SMOKE grenade attempt), spends none in the road or the Location Dashed to beyond the cost of entry, and ends its MPh in the Location Dashed to, unless it becomes berserk, pinned, broken, or wounded in the road Location or cannot enter, when the Dash's benefits cease. All non-ordnance Defensive First Fire at it in the road Location is Area Fire, but for Fire Lanes and Residual FP already there; FFMO and FFNAM apply as usual. A weapon with a CA may not fire at it there if it must change its CA. Ordnance uses Case J, without Cases J3 and J4. Blocked from the Location by a concealed unit, it is back in the road Location with no Dash. "A declared Dash might not qualify as such vs one or more DEFENDING units." No other movement or MF expenditure is open to a Dashing unit, and Armored Assault is NA.
- **C6.1 (p. 173):** Case J, +2, against a Dashing target.
- **B3.43 (p. 114):** no Dash in a road hex covered by rubble or debris.

**What the code did:** nothing; the word was in no rule file.

**The change.** The rule is one Rules file, `ScenarioA1Dash`; Play hands it the facts.

1. **The declaration** is the move's new argument `dash`, the Location Dashed to, given with the stack's first step, which is the step into the road Location ("Dash to" on the Movement panel). `StepBar` refuses it unless: no mover has spent MF and it is the first step; it is not from off board, by a berserk unit, or with Assault Movement, a Minimum Move, Bypass, a pushed Gun, or the other terrain's cost; the Location entered has a road at its center; and the Location Dashed to is at ground level, beside the road Location, and neither the Location left nor beside it. The MF of both steps together must be within the movers' MF, by the check the step already makes.
2. **The step records it** (`MovementStepped.Dash`, the event's `dash`), and the movement state carries it while the stack is in the road Location.
3. **While it is pending** the only step is into the Location Dashed to, at its cost of entry: any other step, any option with it, and a SMOKE grenade or DC placed with MF there are refused, and so is ending the move in the road Location. A stack pinned, broken, or berserk there has no Dash any more, and may end its move.
4. **On arrival** the stack's MPh ends when the DEFENDER's window on that step closes (`EndingMembers`, a new fact). A stack forced back by a concealed unit stays in the road Location, its move ended as before, with no Dash.
5. **Fire.** The attack's new fact `Dash` is set for the DEFENDER's First Fire, Subsequent First Fire, and FPF at the stack in the road Location (not an ordnance hit, a Fire Lane, or Residual FP) when `QualifiesAgainst` says so: a firing Location to which neither the Location left nor the Location Dashed to is Open Ground. Open Ground to a firer is read as Interdiction reads it since pass 35 (the terrain, a clear LOS with no Hindrance, and no TEM against that firer). The package then halves the FP as "dash" (A4.63). The fire record's verifier takes the fact as recorded.
6. **Ordnance.** The shot's movement gains `Dashing`, set by the same read from the Gun's Location: Case J (+2) in place of Cases J3 and J4, and the shot is outside the package ("dash-covered-arc") when the Gun must turn.

**Rulings proposed.**

- **R40.7:** "on the other side of the road" is a ground-level Location beside the road Location that is neither the Location left nor beside the Location left. The page gives no geometry beyond its example ([CC8], [BB7], [AA7] of board 4, which this admits).
- **R40.8:** a declared Dash is not refused for its terrain. Whether it qualifies is decided against each firer, as the page says; a Dash between two Locations that are Open Ground to every firer is legal and changes nothing but the stack's freedom to do anything else.
- **R40.9:** a Fire Group whose Locations differ takes the Dash's halving if it holds against any one of them (A7.52).

**The games,** on board 4, the example's own hexes, written by `make_board_game.py`. Both are **recorded**; the lines are from the game files and the proposals' reviews.

| Game | What it holds |
|---|---|
| `p40-dash` | The squad in [CC8] steps into [BB7] for 2 MF (a hedge), "Dashing to [AA7]". First Fire from [Y9]: "4 FP x 0.5 (Area Fire at a Dash, A4.63) = 2", FFNAM -1, FFMO -1, as the example has the 4-4-7 halved. First Fire from [CC6], to which [AA7] is Open Ground: 4 FP, not halved, hedge +1, FFNAM -1. After the DEFENDER's pass, ending the move is refused ("ends its MPh there, not in the road Location") and so is a step to [BB6] (both read and cancelled, so not in the file). The step into [AA7] for 2 MF; a shot at it there is Subsequent First Fire's Area Fire, not the Dash's; after the pass no unit is offered to move. |
| `p40-dash-control` | The same squad enters [BB7] with no Dash: the fire from [Y9] is 4 FP, not halved. |

The frame's game `p40-bd04-dash` holds its setup alone and is not a fixture.

**Tests.**

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: the declaration's bars; the pending Dash's bars, the end bar, and when the benefits cease; against which firers it qualifies and what Open Ground to a firer is; Case J for ordnance without J3 and J4; the move's summary and the arrival's end. In `ScenarioA1Pass10Tests`: the halving on the IFT.
- **Play, new:** `BacklogPass10Tests.ADashIsAreaFireInTheRoadLocationAndEndsWhereItWasDeclaredTo`, with a move through a road and no Dash as its control. Its first run failed on the control alone: the same squad fired twice, and A8.3 rightly refused its Subsequent First Fire beyond the closest enemy; the control has its own firer now.
- **Run:** 71 Rules tests in four classes passed. 90 Play tests in five classes (movement, ordnance, fire, the rout): 89 passed and the new one failed as told; its class of 23 passed after the test's repair.

**Left out, each for a backlog row.**

- The SMOKE grenade attempt before a Dash (the rule's one exception to "no MF spent").
- "Wounded in the road Location": the game cannot tell a wound taken there from one carried in, so a wound alone does not end the Dash.
- A Dashing stack that has not the MF left to enter (the example's leader who breaks) may not end its move in the road Location unless it is pinned, broken, or berserk; Double Time, as the example has it, is open to it.
- Recovery, a Drop, and other actions with their own commands are not barred to a Dashing stack.
- The CA limit for a vehicle's MG and for a MG with a Covered Arc; only a Gun's shot is barred.
- Armored Assault (D9.31) is not built, so its bar has nothing to bar.
- B3.43: rubble and debris on a road. A rubble hex's center is not a road in the map's terms, so no Dash is offered there; debris has no counter.

**Not seen or not run.** No ordnance shot at a Dashing unit was staged, and the CA bar has no test: Case J rests on its Rules test, and the bar on reading. A Fire Group from two Locations, a Dash forced back by a concealed unit, a Dash by a stack with a leader, and the panel at narrow widths were not staged. The page's component tests, the replay digest, the sweep, and the text list were not run. Texts added on purpose: three refusals `play.move-dash`, `play.end-move-dash`, the move's Dash clause, the fire's "Area Fire at a Dash", the reason `dash-covered-arc`, and the panel's field.
'''

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "### 8.2 Task 40.3: Dash" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.2 committed at the user's word, with yes to rulings R40.4 to R40.6 (3946562e). Task 40.3, Dash: the multipliers, the ordnance mover's cases, and "
       "Interdiction's Open Ground by firer read in the code; the rule as one Rules file (ScenarioA1Dash) with the step's bars, the end bar, the qualifying "
       "read, and Open Ground to a firer; the move's dash argument and field, the step's record and the movement state, the arrival's end, fire's Area Fire, "
       "ordnance's Case J and CA bar; MapStudio built with warnings as errors at the first try; two games on board 4, the example's hexes, played with "
       "every commit read from the file; six Rules tests and one Play test; the design's section 8.2 "
       "| 20:47 | 21:03 | 16 min | Estimate 0:40. The Play test's first run failed on its own control (one squad firing twice, which A8.3 refuses), "
       "not on the rule. Not staged: an ordnance shot at a Dash, a group from two Locations, a Dash forced back. Not run: the page's component tests, "
       "the replay, the sweep, the text list. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
assert text.endswith("narrow widths. |\n"), text[-60:]
open(path, "w", encoding="utf-8", newline="").write(text + ROW)
print("ok")
