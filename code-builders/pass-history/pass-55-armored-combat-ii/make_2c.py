"""Pass 55, increment 2c (task 55.1): special ammunition. Board 3: a British 6-Pounder in [D10] sees a PzKpfw IIIH in [F10] and a squad in [G10]."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
GAMES = (
    ("p55-apds-used", "Pass 55 task 55.1: June 1944, a 6-Pounder fires APDS (D6) with an Original DR of 3, then 6: used, then used up (C8.9; board 3)", 1944, 6, None),
    ("p55-apds-none", "Pass 55 task 55.1: June 1944, a 6-Pounder's APDS on an Original DR of 7: it had none and has not fired; AP follows (C8.9; board 3)", 1944, 6, None),
    ("p55-apds-malf", "Pass 55 task 55.1: June 1944, a 6-Pounder's APDS on an Original DR of 12: no APDS, and the Gun malfunctions (C8.9; board 3)", 1944, 6, None),
    ("p55-apds-may", "CONTROL: May 1944, a 6-Pounder has no APDS yet (D6 from June 1944; C8.12; board 3)", 1944, 5, None),
    ("p55-apds-elite", "Pass 55 task 55.1: June 1944 with elite-ammunition:british: APDS on an Original DR of 7 is the last round (C8.2; board 3)", 1944, 6, ["elite-ammunition:british"]),
    ("p55-he-number", "Pass 55 task 55.1: June 1944, a 6-Pounder's HE (HE8) at a squad in [G10]: used on a 6, the last round on an 8 (C8.8, C8.9; board 3)", 1944, 6, None),
    ("p55-he-1942", "CONTROL: July 1942, a 6-Pounder has no HE (HE7 from February 1943) and is refused at Infantry (C2.21, C8.8; board 3)", 1942, 7, None),
)
for game_id, label, year, month, special in GAMES:
    g = Game(game_id + SUFFIX, label, phasing="british", other="british", year=year, month=month, special=special)
    g.gun("b-gun", "british-at-gun", "british", "bd03:D10:0", "east")
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
    g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:G10:0")
    g.phases("pfph", side="british")
    g.write()
