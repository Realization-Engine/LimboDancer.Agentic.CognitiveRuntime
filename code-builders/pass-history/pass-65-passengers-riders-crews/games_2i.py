"""The Studio games of increment 2i (captured vehicles; A21.2, A21.21, A21.22, D5.343, A26.12; ruling R65.22), on board 3, in 1943."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# A21.2, A21.22: a German PzKpfw IIIH Abandoned at F8, a Russian 4-4-7 and a Russian infantry crew at F9, in the Russian MPh. Both advance into F8 in the APh;
# as the CCPh ends the tank is captured (no German Personnel there). In the Russian MPh of Turn 2 the crew re-crews it as a Temporary crew (MP halved),
# and in Turn 3 its start takes the start dr (+1 Russian).
g = GameFile("p65-capture-afv", "pass 65, 2i: an Abandoned PzKpfw IIIH captured as the CCPh ends and crewed by a Russian crew (A21.2, A21.22)",
             phasing="russian", year=1943, month=7)
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east", abandoned=True)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.unit("rc", "crew", "defender-crew", "russian", "bd03:F9:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B2:0")
g.phases("rph", "pfph", "mph")
g.write()

# A21.2: the same tank with a German HS beside it, so no automatic capture; the Russian 4-4-7 advances in and, in the CCPh, attempts the capture in CC
# (the Vehicle CC panel's capture button).
g = GameFile("p65-capture-cc", "pass 65, 2i: a capture attempt in CC on an escorted Abandoned PzKpfw IIIH (A21.2)",
             phasing="russian", year=1943, month=7)
g.vehicle("dt", "attacker-tank", "german", "bd03:F8:0", "east", abandoned=True)
g.unit("gh", "half-squad", "attacker-half-squad", "german", "bd03:F8:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")
g.write()

# A11.52, A21.21: a stopped German truck alone at F8 and a Russian 4-4-7 at F9 in the Russian APh; the squad advances in and, as the CCPh begins, the truck
# passes to the Russians with a Temporary Driver.
g = GameFile("p65-capture-truck", "pass 65, 2i: an unarmed truck captured with a Temporary Driver (A11.52, A21.21)",
             phasing="russian", year=1943, month=7)
g.vehicle("dtr", "attacker-truck", "german", "bd03:F8:0", "east")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B2:0")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")
g.write()
