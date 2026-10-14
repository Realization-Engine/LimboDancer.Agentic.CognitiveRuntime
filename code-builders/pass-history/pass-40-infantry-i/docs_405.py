"""Pass 40 task 40.5: the design's section 10.2 and the time log's row. Usage: python docs_405.py <start HH:MM> <end HH:MM>"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = (int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))

SECTION = """
### 10.2 Task 40.5: the attack owed in the DEFENDER's own Location, and a crew's FP in FPF

**The rules, as read on their page.**

- **A8.312 (p. 61):** "An armed, unbroken Infantry DEFENDER not in Melee must after all Residual-FP/minefield/OBA attacks then immediately attack any Infantry/Cavalry MMC unit that enters its Location during the MPh whether it uses Defensive First Fire, Subsequent First Fire, or FPF [EXC: A SMC with a MG/IFE already marked with a First Fire counter may not use Subsequent First Fire or FPF because it cannot use Sustained Fire; 9.3]. ... All such FPF and any Defensive/Subsequent First Fire must be combined into one Mandatory FG (7.55)."
- **A8.311 (p. 61):** "Infantry manning ordnance cannot fire it during FPF [EXC: OVR Prevention; C5.64] but must add their inherent FP to the FPF attack."
- **A7.352 (p. 56):** a crew, HS, or SMC that fires any SW or Gun loses its inherent FP until CC or the Player Turn's end, but as A7.353 has it.

**What is built.** One Rules file, `ScenarioA1MandatoryFire`: who owes the attack, the bar on the pass, the bar on any other fire, the page's note, and when a crew that fired its Gun still adds its FP. Play hands it facts in `GamePlanner.MandatoryFire.cs`:

- The attack is owed on the step that changed the stack's Location, while the DEFENDER's window is open, when an Infantry MMC of the stack stands in the Location. The units that owe it are the DEFENDER's armed, unbroken Infantry there, not in Melee, not prisoners, that have not fired at that Location since the step.
- As question F6 was answered, the game fires nothing on its own. `asl.game.pass-fire` is refused (`play.pass-mandatory`) while the attack is owed, and `asl.game.fire` by the DEFENDER is refused (`play.fire-mandatory`) unless it is at that Location and names every unit that owes the attack. Other firers may join it, and the DEFENDER keeps its choice of MG and of a leader's direction.
- A unit the fire rules give no attack now is not held to one: Play asks the fire plan whether an attack by the unit alone at its own Location would be allowed, and Rules leaves out a unit for which it would not. So the pass is never refused for an attack that cannot be made (a Guard, whose Location's fire is refused whole, is the case question 5 of the frame named).
- The page: in the DEFENDER's window, the note names the units and the pass button gives way to "Propose: the Mandatory FG attacks", which proposes each unit's inherent FP (a unit with none, its first MG). The window's "fire at it below, or pass" is not shown while the attack is owed.
- A8.311: in an attack of the kind FPF, a crew that fired its Gun adds its inherent FP. Outside FPF ruling R8.4 stands.
- `EnemyUnitFacts.Broken`, which nothing has read since pass 35, is removed.

**Rulings, decided under the user's word of 2026-10-11.**

- **R40.17:** the units that owe the attack fire as one group where the fire rules form one. The fire package forms no group of a unit with no counter and a unit marked First Fire (ruling R12.3: one kind of Defensive fire to an attack), and it lets a Location fire once at a MF expenditure (A7.55). In that case one of the units that owe the attack makes it, alone or with others that owe it, and the rest are released; the page says so. A group of mixed kinds is a backlog row.
- **R40.18 (amends R8.4):** a crew that fired its Gun this Player Turn has no inherent FP, but in an attack of the kind FPF, where it adds it, whatever its own counter. A7.353's halved FP in Subsequent First Fire and Final Fire stays a backlog row (pass 45).
- **R40.19:** the bar is on the DEFENDER's small-arms and MG fire (`asl.game.fire`). A Gun's shot, a thrown DC, and a Starshell are not held behind the attack owed.
- **R40.20:** the step that owes the attack is one that changed the stack's Location. Cavalry is not in the catalog. Residual FP, minefield, and OBA attacks, which the rule puts first, are resolved with the step as before.

**Left out, each for a backlog row:** the Mandatory FG of mixed kinds; OVR Prevention (C5.64); a Guard's attack on a unit that enters its Location (pass 75); the page offering a crew as a firer (ruling R24.8 keeps it out of the fire panel, so a crew's FPF is reached only by the action); a berserk charge into a crew's Location (rulings R24.3, R30.5); the other acts of the DEFENDER held behind the attack; a fault met in a test and not followed: an FPF whose NMC replaces a firer by ELR was refused at the commit (UNIT-STATE-007, the replaced unit "is consumed and cannot act").

**The games,** on board 3, written by `make_tpbf.py`: a berserk German squad in [D9] charges into the woods of [C9].

| Game | What it holds |
|---|---|
| `p40-tpbf` | A Russian squad in [C9]: the note, no pass button, the Mandatory FG's attack (4 FP tripled to 12), then the pass. |
| `p40-tpbf-mixed` | Two Russian squads, one marked First Fire: the group is refused by the fire package, each squad has its own button, one attacks, and the pass follows (ruling R40.17). |
| `p40-tpbf-control` | The Russian squad is broken: no note, and the pass at once. |

**Not staged in the Studio:** a crew's FP in FPF. The page offers no crew as a firer, and a charge into a crew's Location is refused; the game was staged twice and removed. A Play test holds the case.

**Tests.**

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: who owes the attack, with each excepted unit; the two bars and the note, with and without a group; a crew's FP by the kind of the attack.
- **Rules, changed:** `ScenarioA1Pass35RulesTests`, for the fact removed.
- **Play, new:** `BacklogPass8Tests.ACrewThatFiredItsGunAddsItsOwnFpToFpf` (the crew refused alone, then committed in the squad's FPF).
- **Play, changed:** `CloseCombatStepsTests.U34ABerserkResultMakesTheSquadChargeTheNearestKnownEnemyInItsNextMph`, the one test of 730 that the new bar stopped: it now reads the attack owed, has the pass and two other attacks refused, and makes the Mandatory FG's.
- **Run:** the whole Play project before the tests were changed (729 of 730, the one named above) and the whole Rules project (777); then 83 Rules tests in four classes, 52 Play tests in four classes, and 106 Studio tests of the page's components, all passed. MapStudio built with warnings as errors.

**Recorded games.** No record changes: the bars refuse proposals, and a record that holds a pass where the attack was owed replays as it was played. The table player of the sweep passes on every step; where it meets the bar its game will differ, which the gate's sweep will show.

**Not run:** the replay digest, the sweep, the text list.
"""

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "### 10.2 Task 40.5" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.4 committed (d91cec7c). Task 40.5, the attack owed under A8.312 and a crew's FP in FPF (A8.311): the page read; the fire package's kinds and its marks read in the code; "
       "one Rules file (ScenarioA1MandatoryFire) and one Play file, the pass and the fire refused while the attack is owed, the page's note and button; EnemyUnitFacts.Broken removed; "
       "MapStudio built with warnings as errors; three games on board 3 played and read back; three Rules tests, one Play test new and one changed; "
       f"the design's section 10.2 with rulings R40.17 to R40.20 decided under the user's word | {start} | {end} | {minutes} min | "
       "The whole Play project was run once, since the bar reaches every class that passes: one test of 730 stopped, as intended. "
       "The games found three things: the page named units by id, kept \"or pass\" in its note, and offered one button for a group the fire package refuses (rulings R12.3, A7.55), which became ruling R40.17. "
       "A crew's FPF could not be staged in the Studio (no crew in the fire panel; a charge into a crew's Location refused), so a Play test holds it; that test met an older fault (an FPF's NMC that replaces a firer is refused at the commit), which is a backlog row. "
       "Two slips against the standing rules: a sed -i on the page, and a temporary line in the gate to read a refusal, taken out again. Not run: the digest, the sweep, the text list. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + ROW)
print("ok", minutes)
