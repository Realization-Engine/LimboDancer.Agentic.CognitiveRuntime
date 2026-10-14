"""Pass 45, task 45.8: the games. Board 3.

Usage: python make_458.py <game id> <mode>
  mixed      two Russian squads in the woods of [C9], one marked First Fire; a berserk German squad in [D9]. The German MPh: the charge
             into [C9] and one Mandatory FG of the two (A8.312, A8.31; ruling R45.1)
  plain      the control: neither Russian squad is marked
  bore       a Russian squad with a MMG in [F10], Bore Sighted on [D10]; a German squad in [D9]. The German MPh: the squad enters [D10]
             and the MMG fires alone (C6.44)
  unsighted  the control: the same with no Bore Sighting
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "mixed": "Pass 45 task 45.8: two Russian squads in [C9], one marked First Fire, make one Mandatory FG on a berserk squad's entry (A8.312, A8.31; board 3)",
    "plain": "Pass 45 task 45.8 control: two Russian squads with no counter in [C9] and a berserk squad's entry (A8.312; board 3)",
    "bore": "Pass 45 task 45.8: a Russian MMG in [F10] Bore Sighted on [D10] fires alone at a squad that enters it (C6.44; board 3)",
    "unsighted": "Pass 45 task 45.8 control: a Russian MMG in [F10] with no Bore Sighting fires at a squad that enters [D10] (board 3)",
}
game = Game(GAME, LABELS[MODE], defender="russian" if MODE in ("bore", "unsighted") else None)
if MODE in ("mixed", "plain"):
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D9:0", berserk=True)
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:C9:0")
    game.unit("r-two", "squad", "defender-squad", "russian", "bd03:C9:0", **({"first_fire": True} if MODE == "mixed" else {}))
else:
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D9:0")
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    if MODE == "bore":
        game.add("bore-sighted", {"gun": "r-mmg", "location": "bd03:D10:0", "crew": "r-one", "setupLocation": "bd03:F10:0"})
game.phases("pfph", "mph")
game.write()
