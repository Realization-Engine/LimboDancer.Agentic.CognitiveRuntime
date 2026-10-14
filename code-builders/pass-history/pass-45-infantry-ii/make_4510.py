"""Pass 45, task 45.10: the games. Board 3.

Usage: python make_4510.py <game id> <mode>
  ipc        a Russian squad with a MMG (4 PP) and a leader in [F10]. The Russian MPh: with Double Time the leader lends no IPC (A4.52)
  ti         two Russian squads in [F10], one TI with a LMG. The Russian RPh: the TI squad Transfers its LMG (A.7, A4.8)
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "ipc": "Pass 45 task 45.10: a leader who Double Times with a squad carrying 4 PP lends it no IPC (A4.42, A4.52; board 3)",
    "ti": "Pass 45 task 45.10: a TI squad, in Good Order, Transfers its LMG in the RPh (A.7, A4.8; board 3)",
}
game = Game(GAME, LABELS[MODE], phasing="russian")
game.unit("g-one", "squad", "attacker-squad", "german", "bd03:P10:0")
if MODE == "ipc":
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.unit("r-lead", "leader", "defender-leader", "russian", "bd03:F10:0")
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    game.phases("pfph", "mph")
else:
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0", ti=True)
    game.unit("r-two", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.weapon("r-lmg", "defender-lmg", "russian", "r-one")
game.write()
