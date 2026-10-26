"""Writes only game (b) of games_check.py (a concealed target that keeps its "?"), so the played game (a) is never rewritten.

    python games_check_b.py < /dev/null
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import LIVE, GameFile  # noqa: E402

GAME = "p71-check-kept-concealed"
if os.path.exists(LIVE + GAME + ".game.json"):
    sys.exit(f"STOP: {GAME} exists; it may hold played events")
g = GameFile(GAME, "pass 71, whole-pass check: a concealed target keeps its \"?\" and the Sniper side reads one neutral line (A12.14)", phasing="german")
g.san("russian", 6)
# The first try put r1 in [E4], which sees [E6] (the board viewer's LOS tab: clear, range 2), so its PTC's "?" loss gave a seer. [E3] to [E6] is
# Blocked at [E4] (the LOS tab, 2026-10-26), so r1 in [E3] sees no [E6], concealed or not; g1 fires at it from the adjacent [E2].
g.unit("g1", "squad", "attacker-squad", "german", "bd03:E2:0")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:E6:0", concealed=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:E3:0", concealed=True)
g.sniper("rs", "russian", "bd03:E7:0")
g.phases("rph", "pfph", side="german")
g.write()
