"""Pass 55, the Studio check of the whole pass: games for the widths, a walk of a Player Turn, and the reviews' fixes. Board 3."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""

g = Game("p55-w-can" + SUFFIX, "Pass 55 Studio check: the ordnance panel with Canister, its facing and aims, at the five widths (board 3)",
         phasing="american", other="american", year=1943, month=8)
g.gun("a-gun", "american-at-gun", "american", "bd03:F10:0", "west")
g.unit("de-d10", "squad", "attacker-squad", "german", "bd03:D10:0")
g.unit("de-d9", "squad", "attacker-squad", "german", "bd03:D9:0", concealed=True)
g.unit("de-e10", "squad", "attacker-squad", "german", "bd03:E10:0")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:C10:0", "east")
g.phases("pfph", side="american")
g.write()

g = Game("p55-w-apds" + SUFFIX, "Pass 55 Studio check: the 6-Pounder's ammunition list and note at the five widths; a shot with no round (board 3)",
         phasing="british", other="british", year=1944, month=6)
g.gun("b-gun", "british-at-gun", "british", "bd03:D10:0", "east")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:G10:0")
g.phases("pfph", side="british")
g.write()

for game_id, label, hidden, facing in (
        ("p55-w-mot", "Pass 55 Studio check: the Motion attempt block at the five widths, then a walk of the Player Turn (board 3)", False, "east"),
        ("p55-w-duel", "Pass 55 Studio check: the Gun Duel's pending choice at the five widths (board 3)", None, "north-east"),
        ("p55-fix-hidden", "Pass 55, the reviews' fixes: a hidden T-34 may not attempt Motion (A12.32; ruling R55.32; board 3)", True, "east")):
    g = Game(game_id + SUFFIX, label)
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:D9:0", "south-east")
    if hidden is None:
        g.gun("ru-gun", "defender-at-gun", "russian", "bd03:H10:0", facing)
    else:
        g.vehicle("ru-tank", "defender-tank", "russian", "bd03:H10:0", facing, **({"hidden": True} if hidden else {}))
    g.phases("pfph", "mph")
    g.write()
