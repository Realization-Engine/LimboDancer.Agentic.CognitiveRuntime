"""Pass 55, increment 2g (task 55.7): Gun Duels. Board 3: a PzKpfw IIIH starts in [D9], out of LOS of a Russian AT Gun in the woods of [H10],
and enters [D10] with its turret turned east. The Gun must turn to fire: two hexspines (Case A +4, doubled in woods: +8) or one (+6)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
GAMES = (
    ("p55-duel-decline", "Pass 55 task 55.7: the AT Gun in [H10] declares a shot at the moving tank in [D10]; the tank declines the Gun Duel and the shot resolves (C2.2401; board 3)", "north-east"),
    ("p55-duel-miss", "Pass 55 task 55.7: the tank fires first in a Gun Duel and misses; the Gun's declared shot then resolves (C2.2401; board 3)", "north-east"),
    ("p55-duel-void", "Pass 55 task 55.7: the tank fires first in a Gun Duel and breaks the crew: the return shot is void (C2.2401; board 3)", "north-east"),
    ("p55-duel-none", "CONTROL: the Gun's DRM is not higher than the tank's: no Gun Duel, the shot resolves at once (C2.2401; board 3)", "west"),
)
for game_id, label, facing in GAMES:
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:D9:0", "south-east")
    g.gun("ru-gun", "defender-at-gun", "russian", "bd03:H10:0", facing)
    g.phases("pfph", "mph")
    g.write()
