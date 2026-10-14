"""Pass 55, increment 2a: the new Guns. Board 3; [D10] sees [F10], [G10] and [H10] (range 2 to 4)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""

g = Game("p55-6pdr" + SUFFIX, "Pass 55 task 55.6: a British 6-Pounder in [D10] fires AP at a PzKpfw IIIH in [F10], June 1944: black TH numbers (C3.3; board 3)",
         phasing="british", other="british", year=1944, month=6)
g.gun("b-gun", "british-at-gun", "british", "bd03:D10:0", "east")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:G10:0")
g.phases("pfph", side="british")
g.write()

for year in (1943, 1944):
    g = Game("p55-37mm-%d%s" % (year, SUFFIX),
             "Pass 55 task 55.6: an American 37mm AT Gun in [D10] fires HE at a squad in [H10] in %d: %s TH numbers (C3.3, the chart of p. 695; board 3)"
             % (year, "red" if year < 1944 else "black"), phasing="american", other="american", year=year, month=7)
    g.gun("a-gun", "american-at-gun", "american", "bd03:D10:0", "east")
    g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:H10:0")
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:F10:0", "west")
    g.phases("pfph", side="american")
    g.write()
