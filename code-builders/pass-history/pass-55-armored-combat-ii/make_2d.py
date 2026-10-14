"""Pass 55, increment 2d (task 55.2): Canister. Board 3: an American 37mm AT Gun in [F10] facing west; aim 4 attacks [D9], [D10], [C10],
and [E10] at half FP; aim 3 attacks [D10], [C10], and [E10] at a quarter."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass55")
from p55lib import Game

SUFFIX = sys.argv[1] if len(sys.argv) > 1 else ""
GAMES = (
    ("p55-can-used", "Pass 55 task 55.2: August 1943, a 37mm AT Gun in [F10] fires Canister (C7) at aim 4: [D9], [D10], [C10], and [E10] at half FP; then aim 3 (C8.4, C8.41; board 3)", 1943, 8),
    ("p55-can-none", "Pass 55 task 55.2: Canister on an Original DR of 8: the Gun had none and has not fired; HE follows (C8.9; board 3)", 1943, 8),
    ("p55-can-malf", "Pass 55 task 55.2: Canister on an Original DR of 12: the Gun malfunctions (C8.4; board 3)", 1943, 8),
    ("p55-can-turn", "Pass 55 task 55.2: Canister after a change of CA: Case A on each IFT DR and the ROF lowered (D3.52, C2.5; board 3)", 1943, 8),
    ("p55-can-1942", "CONTROL: July 1942, a 37mm AT Gun has no Canister yet (C7 from August 1942; board 3)", 1942, 7),
)
for game_id, label, year, month in GAMES:
    g = Game(game_id + SUFFIX, label, phasing="american", other="american", year=year, month=month)
    g.gun("a-gun", "american-at-gun", "american", "bd03:F10:0", "west")
    g.unit("de-d10", "squad", "attacker-squad", "german", "bd03:D10:0")
    g.unit("de-d9", "squad", "attacker-squad", "german", "bd03:D9:0", concealed=True)
    g.unit("de-e10", "squad", "attacker-squad", "german", "bd03:E10:0")
    g.vehicle("de-tank", "attacker-tank", "german", "bd03:C10:0", "east")
    g.phases("pfph", side="american")
    g.write()
