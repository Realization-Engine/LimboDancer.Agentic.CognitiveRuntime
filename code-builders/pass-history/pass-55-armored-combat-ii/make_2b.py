"""Pass 55, increment 2b (task 55.4): fire at another level. Board 3: [E5] is a level 1 hill above the road in [E6] (LOS clear, range 1);
[N2] is a two-level stone building seen at level 1 from [N4] (range 2); [M2] is another."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""

g = Game("p55-ha-inf" + SUFFIX, "Pass 55 task 55.4: a German INF Gun in [E6] fires up at a squad on the hill [E5]: Case Q +1 for Height Advantage (B10.31, C2.6; board 3)")
g.gun("de-gun", "attacker-inf-gun", "german", "bd03:E6:0", "north-east")
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:E5:0")
g.phases("pfph")
g.write()

g = Game("p55-ha-veh" + SUFFIX, "Pass 55 task 55.4: a PzKpfw IIIH in [E6] fires up at a T-34 on the hill [E5]: Case Q +1 for Height Advantage (D4.22; board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:E6:0", "north-east")
g.vehicle("ru-tank", "defender-tank", "russian", "bd03:E5:0", "south-west")
g.phases("pfph")
g.write()

g = Game("p55-ha-down" + SUFFIX, "CONTROL of p55-ha-inf: a Russian AT Gun on the hill [E5] fires down at a squad in [E6]: no Height Advantage (B10.31; board 3)", phasing="russian")
g.gun("ru-gun", "defender-at-gun", "russian", "bd03:E5:0", "south-west")
g.unit("de-squad", "squad", "attacker-squad", "german", "bd03:E6:0")
g.phases("pfph", side="russian")
g.write()

g = Game("p55-cmg-up" + SUFFIX, "Pass 55 task 55.4: a PzKpfw IIIH in [N4] fires its CMG and then its MA at a squad on level 1 of the stone building [N2] (C2.6; board 3)")
g.vehicle("de-tank", "attacker-tank", "german", "bd03:N4:0", "north-east")
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:N2:1")
g.phases("pfph")
g.write()

g = Game("p55-own-hex" + SUFFIX, "Pass 55 task 55.4: a German INF Gun at ground level of [M2] is refused a shot at level 1 of its own hex (C2.6; board 3)")
g.gun("de-gun", "attacker-inf-gun", "german", "bd03:M2:0", "east")
g.unit("ru-squad", "squad", "defender-squad", "russian", "bd03:M2:1")
g.phases("pfph")
g.write()
