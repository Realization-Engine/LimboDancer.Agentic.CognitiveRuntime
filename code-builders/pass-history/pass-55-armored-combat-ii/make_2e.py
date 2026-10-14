"""Pass 55, increment 2e (task 55.5): Armor Leaders and Inexperienced Crews. Board 3: [D10] and [F10] see each other at range 2."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""


def tank(g, identifier, definition, side, at, facing, leader=None, **conditions):
    g.vehicle(identifier, definition, side, at, facing, **conditions)
    if leader:
        g.events[-1]["payload"]["instance"]["armorLeader"] = {"morale": leader[0], "leadership": leader[1]}


def both(game_id, label, phasing, german_leader, russian_inexperienced=False, german=None, phase="pfph"):
    g = Game(game_id + SUFFIX, label, phasing=phasing)
    tank(g, "de-tank", "attacker-tank", "german", "bd03:D10:0", "east", leader=german_leader, **(german or {}))
    tank(g, "ru-tank", "defender-tank", "russian", "bd03:F10:0", "west", **({"inexperienced_crew": True} if russian_inexperienced else {}))
    g.phases(*(("pfph",) if phase == "pfph" else ("pfph", "mph")), side=phasing)
    g.write()


both("p55-al-fire", "Pass 55 task 55.5: a PzKpfw IIIH with a 9-1 Armor Leader in [D10] fires its MA at a T-34 in [F10]: -1 on the To Hit DR (D3.44; board 3)", "german", (9, -1))
both("p55-al-ctl", "CONTROL of p55-al-fire: the same shot with no Armor Leader (board 3)", "german", None)
both("p55-al-shock", "Pass 55 task 55.5: a T-34 hits a PzKpfw IIIH with a 10-2 Armor Leader for a possible Shock: the NTC against morale 10 (D3.42; board 3)", "russian", (10, -2))
both("p55-al-shock2", "Pass 55 task 55.5: as p55-al-shock, the second copy, played once the first has shown the Final TK# (board 3)", "russian", (10, -2))
both("p55-al-inexp", "Pass 55 task 55.5: a T-34 with an Inexperienced Crew fires its MA: +1 on the To Hit DR (D3.45; board 3)", "russian", None, russian_inexperienced=True)
both("p55-al-bog", "Pass 55 task 55.5: a bogged PzKpfw IIIH with a 9-1 Armor Leader starts: -1 on the Bog Removal dr (D8.3; board 3)", "german", (9, -1), german={"bogged": True}, phase="mph")
