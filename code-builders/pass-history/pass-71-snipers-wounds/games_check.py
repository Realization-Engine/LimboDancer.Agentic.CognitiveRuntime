"""The Studio games of pass 71's whole-pass check, on board 3: the two owed by 4f (the wound pin), and a concealed target that keeps its "?" (Round A),
read from the side that must not see.

    python games_check.py < /dev/null

Terrain read with code-builders/boards/hex_facts.py (2026-10-26): [A9], [A10], [B10], [C10], [D10], [E10], [F10] Open Ground; [C9] Woods. [F10] sees
[D10] at range 2 (pass 35's read of board 3, the board viewer). The route [A9], [A10], [B10], [C10], [D10] steps between adjacent hexes (one column,
then one row of the next columns).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# (a) R71.14 (A17.2): the German 8-1 moves alone [A9], [A10], [B10], [C10], [D10], 1 MF each, the Russian passing after each step but the last; at [D10],
# after 4 MF, the Russian 4-4-7 in [F10] First Fires at him (range 2, -1 FFMO, -1 FFNAM). Dice, every one a 2: the IFT DR 2, 2, a Final 2, K/2 on the
# 4 FP column; then the Wound Severity dr and the MC. He is wounded and, after more than 3 MF, pinned.
g = GameFile("p71-check-wound-pin", "pass 71, whole-pass check: a leader wounded by Defensive First Fire after 4 MF is pinned (A17.2)", phasing="german")
g.unit("de-l", "leader", "attacker-leader-8-1", "german", "bd03:A9:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.phases("rph", "pfph", "mph", side="german")
g.write()

# (b) R71.21, R71.22 (A12.14, A14.23): the German PFPh; the German 4-6-7 g1 in [E3] fires at the concealed Russian 4-4-7 r1 in [E4]; its IFT DR 3 + 3
# equals the Russian SAN 6 and calls the Russian Sniper in [E7]; its dr 2 and a Random Location DR of 1, 1 take it one hex north to [E6], where the
# concealed German 4-6-7 g2 stands (terrain read: [E3], [E4] Open Ground, [E6] Dirt Road, [E7] Woods; the "?" placed by the file). The only Russian
# unit, r1, is itself concealed, so g2 keeps its "?" (R71.21). The Russian view, which must not see, reads one neutral line; the German view its twin.
g = GameFile("p71-check-kept-concealed", "pass 71, whole-pass check: a concealed target keeps its \"?\" and the Sniper side reads one neutral line (A12.14)",
             phasing="german")
g.san("russian", 6)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E3:0")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:E6:0", concealed=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:E4:0", concealed=True)
g.sniper("rs", "russian", "bd03:E7:0")
g.phases("rph", "pfph", side="german")
g.write()
