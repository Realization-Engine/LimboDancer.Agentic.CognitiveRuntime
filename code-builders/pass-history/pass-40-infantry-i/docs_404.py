"""Pass 40 task 40.4: the design's section 10 (with 10.1) and the time log's row. Usage: python docs_404.py <start HH:MM> <end HH:MM>"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = (int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))

SECTION = """
## 10. Entry upon the enemy and forced fire: 40.4, 40.5 (increment 2d)

Built on 2026-10-11 under the user's word of that evening: "Complete Pass 40 without my intervention." Each task is committed without a stop, and each ruling below is decided by the proposal that would have been put, marked **decided under the user's word of 2026-10-11** so that it can be struck later.

### 10.1 Task 40.4: Infantry OVR, and recapture by entry

**The rules, as read on their pages.**

- **A4.14 (p. 49):** Infantry may not enter a Location that holds a Known enemy unit in the MPh, but by an Infantry OVR or a berserk charge.
- **A4.15 (p. 49):** an Infantry MMC may enter a Location that holds only one Known enemy SMC (not one in an AFV) at double the MF cost of entry, once it has passed a NTC whose DRM is the TEM of the enemy's Location; a leader may take the NTC for his stack, and if he fails "no unit in his stack may ... move or take any other action during that phase".
- **A4.151 (p. 49):** before any Defensive First Fire at the units that entered, the SMC may move away into an adjacent Accessible Location of the ATTACKER's choice, free of all fire; the ATTACKER may not force it into Open Ground if another choice is open.
- **A4.152 (p. 49):** if the SMC stays, CC is resolved at once, before anything else; survivors are held in Melee.
- **A20.54 (p. 88):** "Unarmed units are not an obstacle to movement"; a Location that holds only Unarmed enemy units is entered in the MPh, and they are attacked in CC at once.

**What is built.** One Rules file, `ScenarioA1InfantryOverrun`: who takes the NTC, the target's bar, the DRM, pass or fail, whether a failure ends the stack's MPh, the bar on a second NTC, the entry's bars and its doubled MF, when the CC is owed, the SMC's bar on moving away, and its destinations. Play hands it facts in `GamePlanner.InfantryOverrun.cs`:

- `asl.game.infantry-overrun-ntc` (stack, Location): one DR, events `dice-rolled` and `infantry-overrun-ntc`. A leader's failure ends the MPh of every unit named; a MMC's own failure bars only its OVR of that Location.
- The move's `overrun` argument: the step is refused without a passed NTC, in Bypass, on a Minimum Move, and for a berserk unit (its charge is ruling R27.3's case); the MF of entry are doubled; the step records `infantryOverrun`, and the movement state holds the Location.
- `asl.game.overrun-evade` (the SMC): refused once any fire has been resolved since the step, for a unit not Good Order, pinned, TI, or in Melee, and when no Location is open. It pends a choice of kind `overrun-evade` for the ATTACKER; the answer moves the SMC.
- The CC at once: the berserk charge's own gate (`BerserkOverrunPending`, `OverrunAttacks`, the bar `play.cc-overrun-first`) now reads either kind of entry. The words of the bar no longer say "berserk".
- A Location that holds only Unarmed enemy units is entered by an ordinary step at its ordinary cost, with no NTC; the same CC is owed at once.
- The page: an "Infantry OVR" tick on the move panel, the NTC's button, the SMC's "moves away" button in a block of its own for the DEFENDER's view and the adjudicator's, and the ATTACKER's choice shown as "into [C8]". The record and the Replay page each have a sentence for the NTC.

**Rulings, decided under the user's word of 2026-10-11.**

- **R40.12:** the NTC is an act of its own, taken before the step it allows, for one stack against one Location; it is taken once in a phase by those units against that Location. Its DRM is the TEM of the Location and of a hexside the stack would cross (the page says "the TEM of the enemy's Location"; a wall between the two is that Location's TEM to this stack). Hindrance within the Location is not counted.
- **R40.13:** a MMC that fails its own NTC loses only the OVR; it may still move elsewhere. The page gives the heavier penalty to the leader's stack alone.
- **R40.14:** the SMC's destinations are the adjacent ground-level Locations it could enter by an ordinary step, in the playable area, that hold no unit of the other side. Minefields, FFE, and Wire are not read. The hex the stack came from is among them when it stands empty.
- **R40.15:** the prefilled CC attack of an Infantry OVR is by the stack's MMC; a leader who came with them is not joined to it by the page (the player may add him). The recapture's prefilled attack is a plain CC attack, not a capture attempt.
- **R40.16:** a unit's Good Order, where the record does not state every condition, is read as it is elsewhere in the move: barred only when a condition says no.

**Left out, each for a backlog row:** a second OVR against a SMC already in Melee; a SMC in an AFV; Human Wave's entry; the Disrupted exception; Hindrance in the NTC's DRM; minefield, FFE, and Wire among the destinations; the leader in the prefilled attack; the recapture as a capture attempt; the note of `BerserkOverrunCloseCombat.razor`, which says "overran the lone ..." for a recapture.

**The games,** on board 3, written by `make_ovr.py`: a German squad in the Open Ground of [D9], a Russian unit in the woods of [C9].

| Game | What it holds |
|---|---|
| `p40-ovr-cc` | The NTC passed; the entry for eight half MF (woods, doubled); the DEFENDER passes; the CC at once; the leader eliminated; the move ends. |
| `p40-ovr-evade` | The SMC moves away: the ATTACKER is offered the woods of [C8] and [D8], the Open Ground hexes left out, and chooses [C8]. No CC. |
| `p40-ovr-fail` | The squad's NTC failed: the OVR refused, a second NTC refused, another move allowed. |
| `p40-ovr-leader-fail` | A leader's NTC for his stack failed (DR 12 +1 against morale 8): the stack's MPh is over. |
| `p40-ovr-melee` | A stack with its leader: both attacks miss, and all are held in Melee. |
| `p40-recapture` | An Unarmed squad entered for two MF with no NTC, and the CC at once. |
| `p40-ovr-control` | An armed squad in [C9]: the NTC and the move are both refused. |

`p40-ovr-stack` was played with dice left over from a refused proposal and under the build before the prefilled attack was mended; it is not a fixture.

**Tests.**

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: the NTC's taker and its bars, the DRM, pass and fail, the stack's end; the target's bar, the entry's bars, the doubled MF, and `EnemiesAtTarget` with the Known SMC entered and a concealed unit beside it still met; Unarmed units, when the CC is owed, and the entry's two attacks; the SMC's bar on moving away and its destinations.
- **Rules, changed:** `ScenarioA1SequenceRulesTests`, for the bar's words.
- **Play, new,** in `BacklogPass27Tests`: the NTC, the refusals without it, the doubled MF, the CC first, and the end of the move; the SMC's move away with the ATTACKER's choice; a leader's failure and a MMC's own.
- **Play, changed:** `PlayTests`, for the two new actions in the registered list.
- **Run:** 50 Rules tests in two classes, 76 Play tests in the classes of entry, concealment, the vehicle OVR, Close Combat, and movement, and 40 Studio tests in the page's component and record classes, all passed. MapStudio built with warnings as errors.
- **No Play test** for the recapture or for the stack held in Melee: both were played in the Studio.

**The tests found** three faults of their own (a hex that was not adjacent, an Original DR 2 that draws a further roll, an empty hex wrongly expected to be barred) and one of the build: the NTC and the SMC's move away both asked for Good Order as a stated fact, which a record that states only some conditions never gives (ruling R40.16).

**Recorded games.** Every new fact is absent from an older record and reads as it did: `infantryOverrun` on a step, the state's list of NTCs. The one changed text is the bar `play.cc-overrun-first`.

**Not run:** the replay digest, the sweep, the text list.
"""

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "## 10. Entry upon the enemy" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.7's games committed with it (275b64fc). Then the user's word: \"Complete Pass 40 without my intervention\", with a sub-agent's analysis before any question. "
       "Increment 2d, task 40.4, Infantry OVR and recapture by entry: A4.14 to A4.152 and A20.54 read on pp. 49 and 88; one Rules file (ScenarioA1InfantryOverrun), "
       "one Play file, the NTC's and the move-away's actions, the move's overrun argument, the CC gate of the berserk charge widened to both kinds of entry; "
       "MapStudio built with warnings as errors; eight games on board 3 played with scripted dice and read back, seven of them fixtures; four Rules tests and three Play tests; "
       f"the design's section 10.1 with rulings R40.12 to R40.16 decided under the user's word | {start} | {end} | {minutes} min | "
       "The games found two faults (the move-away button sat in the DEFENDER-only section; the CC was not offered to a stack with a leader). "
       "The tests found three faults of their own and one of the build (Good Order asked as a stated fact). One game was spoiled by dice left over from a refused proposal. "
       "Not run: the digest, the sweep, the text list. One slip: an inline python command, against the standing rule. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + ROW)
print("ok", minutes)
