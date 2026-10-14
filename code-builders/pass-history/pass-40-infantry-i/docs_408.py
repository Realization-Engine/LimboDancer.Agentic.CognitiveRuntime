"""Pass 40 task 40.8: the design's section 8.3 and the time log's row. Appends; asserts the anchors."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"

SECTION = r'''
### 8.3 Task 40.8: Hazardous Movement in every fire phase

Built on 2026-10-11 and waiting for the user's word before its commit.

**The rule, as read on its page.** A4.62 (p. 51): certain activities "automatically incur a -2 IFT DRM to any attacks against units so engaged regardless of fire phase until they are pinned"; neither FFMO nor FFNAM applies to such a shot; it is cumulative with all other terrain DRM. The acts are pushing a Gun; Flame, roadblock, or rubble Clearance; descending paratroops; Fording; preparing a Set DC; Climbing; and sewer movement; never a vehicle, but PRC Survival. C6.6 (p. 175) gives ordnance Case O for it. C10.3 (p. 181) was read for the push: it says nothing of how long the state lasts.

**What the code did:** the fact was read only in the MPh, from the stack then moving with a pushed Gun, and the Fire package put an attack that carried it in any other phase outside itself.

**The change.**

1. **The state keeps who pushed** (`GameState.HazardousMovers`, set by `ScenarioA1MovementCalculator.HazardousMovers` from a step that pushes a Gun, and emptied as a new Player Turn begins).
2. **`ScenarioA1FireMapRules.HazardousMovement` reads that list in any phase:** every target is a unit on it and none is broken. The pinned are left out by the package, as before, target by target.
3. **The Fire package no longer refuses the fact outside the MPh.** The -2 stands in place of FFNAM and FFMO where those would apply, and beside every TEM.
4. **Ordnance:** in the MPh Case O comes from the same read as before; outside it a new fact of the shot, `HazardousTarget`, gives Case O.

**Ruling R40.10 (question F9, answered yes on 2026-10-11).** Hazardous Movement holds from the push until the unit is pinned or broken, or the Player Turn ends, in every fire phase between. The page gives the pin alone; the break and the turn's end are the ruling's, since a broken crew is no longer "so engaged" and nothing on the page carries the act into the next Player Turn. Only pushing a Gun exists in the game; each other act arrives with its own pass and joins the same list.

**The games,** on board 3, written by `make_hazard.py`. Both are **recorded**; the lines are from the game files.

| Game | What it holds |
|---|---|
| `p40-hazard` | A Russian crew pushes its AT Gun from [D10] to [D9] in its MPh ("for 2 MF, pushing gun R1: a Manhandling DR +2 against M11"). The German squad in [F10] passes; in the DFPh its fire at [D9] has the DRM gunshield +2 and Hazardous Movement -2. |
| `p40-hazard-control` | The Gun is not pushed. The same squad's DFPh fire at [D10] has the Emplacement's +2 and no Hazardous Movement. |

Seen in passing and not part of this task: the control's record names the Gun "r gun" where the other names it "gun R1" (the Emplacement's DRM label against the gunshield's).

**Tests.**

- **Rules, new,** in `ScenarioA1Pass10Tests`: `HazardousMovementIsReadInEveryFirePhaseForTheUnitsThatPushed` (the read by the list, a broken target, the -2 outside the MPh with no refusal, the list after a step).
- **Play, new:** `BacklogPass8Tests.ACrewThatPushedItsGunIsUnderHazardousMovementInTheDfphToo`, with a squad that pushed nothing as its control and the list empty in the next Player Turn. Its first run failed on the control alone: a lone crew with no Gun is a target the Fire package does not review (ruling R24.3); the control is a squad now.
- **Run:** 99 Rules tests in five classes passed. 59 Play tests in three classes (pushing and ordnance, movement, the mortar): 58 passed and the new one failed as told; its class of 19 passed after the test's repair.

**Left out, each for a backlog row.**

- The -2 for a stack in which only some targets pushed: the fact is read for the attack as a whole, as ruling R10.8 had it, so a crew fired on together with a unit that did not push takes none.
- Case O outside the MPh for the Area Target Type.
- The other Hazardous acts (Clearance, Fording, a Set DC, Climbing, sewer movement, descending paratroops, PRC Survival): passes 115 to 175.

**Not seen or not run.** No ordnance shot at the crew outside the MPh was staged and `HazardousTarget` has no test; it rests on reading. A pinned crew in the DFPh was not staged. The replay digest, the sweep, and the text list were not run. No text was added or changed.
'''

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "### 8.3 Task 40.8" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.3 committed at the user's word, with yes to rulings R40.7 to R40.9 (17bac58b). Task 40.8, Hazardous Movement in every fire phase: C10.3 read on "
       "p. 181 (silent on the state's length); the state's list of the units that pushed, the read by that list in any phase, the package's refusal outside "
       "the MPh removed, Case O outside the MPh; MapStudio built with warnings as errors at the first try; two games on board 3 with every commit read from "
       "the file; one Rules test and one Play test; the design's section 8.3 "
       "| 21:03 | 21:13 | 10 min | Estimate 0:20. The Play test's first run failed on its control (a lone crew is not a reviewed target), not on the rule. "
       "Increment 2b by the clock: 0:25, 0:15, and 0:10, in all 0:50 against 1:40 in the plan. Not staged: an ordnance shot outside the MPh, a pinned crew. "
       "Not run: the replay, the sweep, the text list. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
assert text.rstrip("\n").endswith("the text list. |"), text[-80:]
open(path, "w", encoding="utf-8", newline="").write(text + ROW)
print("ok")
