"""Pass 45, tasks 45.2 and 45.3: the games. Board 3.

Usage: python make_452.py <game id> <mode>
  mg         a Russian squad with a MMG and a LMG in [F10]; a German squad in [D9]. The German MPh: the squad enters [D10] and the Russian
             squad fires with its MMG. Played twice under two ids: with doubles (the squad cowers, A7.9) and without (the MMG keeps its ROF
             and the DEFENDER may give it up for Residual FP, A8.23)
  cx         the same squad with no SW, CX: its Residual FP is a column lower (A8.26)
  fresh      the control of cx: not CX
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "mg": "Pass 45 task 45.2: a Russian squad with a MMG and a LMG in [F10] fires at a squad that enters [D10]: Cowering's marks, and ROF or Residual FP (A7.9, A8.23; board 3)",
    "cx": "Pass 45 task 45.3: a CX Russian squad in [F10] fires at a squad that enters [D10]; its Residual FP is a column lower (A8.26; board 3)",
    "fresh": "Pass 45 task 45.3 control: a Russian squad in [F10] fires at a squad that enters [D10] and leaves 2 Residual FP (A8.2; board 3)",
}
game = Game(GAME, LABELS[MODE])
game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D9:0")
game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0", **({"cx": True} if MODE == "cx" else {}))
if MODE == "mg":
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    game.weapon("r-lmg", "defender-lmg", "russian", "r-one")
game.phases("pfph", "mph")
game.write()
