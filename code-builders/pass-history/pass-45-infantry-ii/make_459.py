"""Pass 45, task 45.9: the games. Board 3.

Usage: python make_459.py <game id> <mode>
  crew       a Russian crew at its AT Gun in [D10]; two German squads and a leader in [D9]. The German PFPh: a K result on the crew (A7.302)
  held       a Russian crew at its AT Gun in [D10] with a German squad in the same Location; another German squad in [F10].
             The Russian PFPh: the Gun's shot at [F10] is refused (A7.212)
  free       the control: no German squad in [D10]; the shot is proposed
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "crew": "Pass 45 task 45.9: a K result on a Russian crew alone at its AT Gun in [D10] eliminates it (A7.302; board 3)",
    "held": "Pass 45 task 45.9: a Gun whose Location [D10] holds a Known enemy squad may not fire at [F10] (A7.212; board 3)",
    "free": "Pass 45 task 45.9 control: a Gun with no enemy in its Location [D10] fires at [F10] (A7.212; board 3)",
}
game = Game(GAME, LABELS[MODE], phasing="german" if MODE == "crew" else "russian")
game.unit("r-crew", "crew", "defender-crew", "russian", "bd03:D10:0")
game.add("instance-created", {"instance": {"id": "r-gun", "kind": "asl:gun", "definition": "defender-at-gun", "side": "russian",
                                            "position": {"at": "bd03:D10:0", "facing": "east"}, "holding": {"holder": "r-crew", "role": "manned"},
                                            "conditions": {"asl:malfunctioned": False}}})
if MODE == "crew":
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D9:0")
    game.unit("g-two", "squad", "attacker-squad", "german", "bd03:D9:0")
    game.unit("g-lead", "leader", "attacker-leader-9-1", "german", "bd03:D9:0")
else:
    game.unit("g-far", "squad", "attacker-squad", "german", "bd03:F10:0")
    if MODE == "held":
        game.unit("g-near", "squad", "attacker-squad", "german", "bd03:D10:0")
game.phases("pfph")
game.write()
