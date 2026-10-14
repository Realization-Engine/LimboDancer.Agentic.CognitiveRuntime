"""Pass 45, task 45.7: the games. Board 3.

Usage: python make_457.py <game id> <mode>
  dm         a German squad with a dismantled HMG in [F10]; a Russian squad in [E10]. The German PFPh: the HMG fires as a LMG (A9.8)
  whole      the control: the HMG assembled
  xnumber    a Russian squad and its LMG in [F10], both marked First Fire; a German squad in [D10]. The German MPh: it enters [E10] and the
             squad fires again with the LMG in Sustained Fire, DR 11 (A.11)
  mortar     a Russian squad with a light mortar and a German squad with its HMG, each alone. The Russian PFPh: the mortar is dismantled (A9.8)
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "dm": "Pass 45 task 45.7: a German squad's dismantled HMG in [F10] fires as a LMG at [E10] (A9.8; board 3)",
    "whole": "Pass 45 task 45.7 control: a German squad's assembled HMG in [F10] fires at [E10] (board 3)",
    "xnumber": "Pass 45 task 45.7: a LMG in Sustained Fire rolls its printed B# and is removed (A.11; board 3)",
    "mortar": "Pass 45 task 45.7: a Russian squad dismantles its light mortar in the PFPh (A9.8; board 3)",
}
phasing = "russian" if MODE == "mortar" else "german"
game = Game(GAME, LABELS[MODE], phasing=phasing)
if MODE in ("dm", "whole"):
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:F10:0")
    game.weapon("g-hmg", "attacker-hmg", "german", "g-one", **({"dismantled": True} if MODE == "dm" else {}))
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:E10:0")
    game.phases("pfph")
elif MODE == "xnumber":
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0", first_fire=True)
    game.weapon("r-lmg", "defender-lmg", "russian", "r-one", first_fire=True)
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D10:0")
    game.phases("pfph", "mph")
else:
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.weapon("r-mtr", "defender-light-mortar", "russian", "r-one", kind="light-mortar")
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D10:0")
    game.phases("pfph")
game.write()
