"""Pass 55, increment 2f (task 55.3): Motion attempts. Board 3: a tank that starts in [D9] facing south-east, out of LOS of [H10], enters
[D10], which [H10] sees at range 4."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
GAMES = (
    ("p55-mot-pass", "Pass 55 task 55.3: a T-34 in [H10] gains Motion against a PzKpfw IIIH that enters its LOS in [D10], turns its VCA, and fires from Motion (D2.401, C5.35; board 3)", "bd03:D9:0"),
    ("p55-mot-fail", "Pass 55 task 55.3: the T-34's Motion attempt fails, and it has no second (D2.401; board 3)", "bd03:D9:0"),
    ("p55-mot-immob", "Pass 55 task 55.3: the T-34's Motion attempt passes and its Mechanical Reliability DR is a 12 (D2.51; board 3)", "bd03:D9:0"),
    ("p55-mot-seen", "CONTROL: the PzKpfw IIIH begins its move in [D10], in the T-34's LOS: no Motion attempt (D2.401; board 3)", "bd03:D10:0"),
)
for game_id, label, start in GAMES:
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", start, "south-east")
    g.vehicle("ru-tank", "defender-tank", "russian", "bd03:H10:0", "east")
    g.phases("pfph", "mph")
    g.write()
