"""The Studio games of increment 2k (a crew that mans no Gun as a target and in CC, A1.123, rulings R65.28, R65.29; A7.308's Random Selection of vehicles
and D5.311's Unprotected crew, ruling R65.26), on board 3, in 1943."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# R65.28: a German vehicle crew in the open at F8 beside a German 4-6-7, under the Russian 4-4-7's fire from F10 in the Russian PFPh. Ruling R8.3 refused
# this attack before; now both are targets. A German crew manning its 75mm infantry Gun at F6 beside a second 4-6-7 is still refused (C11, R8.3).
g = GameFile("p65-crew-target", "pass 65, 2k: a German vehicle crew beside a squad under a Russian 4-4-7's fire (A1.123, R65.28)",
             phasing="russian", year=1943, month=7)
g.unit("dc", "vehicle-crew", "german-vehicle-crew", "german", "bd03:F8:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F8:0")
g.gun("dg", "attacker-inf-gun", "german", "bd03:F6:0", "east")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:F6:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:F4:0")
g.phases("rph", "pfph")
g.write()

# R65.29: the same German vehicle crew alone at F8 and a Russian 4-4-7 at F9, in the Russian APh: the squad advances into F8 and the CCPh resolves the CC.
g = GameFile("p65-crew-cc", "pass 65, 2k: a Russian 4-4-7 advances onto a German vehicle crew and fights it in CC (A1.123, R65.29)",
             phasing="russian", year=1943, month=7)
g.unit("dc", "vehicle-crew", "german-vehicle-crew", "german", "bd03:F8:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F9:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B2:0")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")
g.write()

# A7.308: three German trucks at F8 under the Russian LMG at F10, in the Russian PFPh. The LMG fired alone (2 FP), or the HS (2 FP), meets three trucks on a
# column whose highest KIA# is 2: each truck makes a Random Selection dr and the two highest take the DR. The whole group's 6 FP (KIA# 3) needs none.
g = GameFile("p65-three-trucks", "pass 65, 2k: three German trucks under a Russian LMG's fire; Random Selection (A7.308, R65.26)",
             phasing="russian", year=1943, month=7)
g.vehicle("t1", "attacker-truck", "german", "bd03:F8:0", "east")
g.vehicle("t2", "attacker-truck", "german", "bd03:F8:0", "east")
g.vehicle("t3", "attacker-truck", "german", "bd03:F8:0", "east")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.weapon("rl", "defender-lmg", "russian", "r1")
g.unit("rh", "half-squad", "defender-half-squad", "russian", "bd03:F10:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:B2:0")
g.phases("rph", "pfph")
g.write()

# D5.311: a German SPW 251/1 (OT) in the woods of D8, under the Russian 50mm with its squad at H8, in the Russian PFPh. A hit by Air Bursts attacks the
# halftrack on the Vehicle line and its crew as Infantry with the -1: a crew that breaks is placed beneath, broken, and the halftrack is Abandoned.
g = GameFile("p65-air-burst-crew", "pass 65, 2k: a 50mm mortar's Air Burst on an OT SPW 251/1 in woods; its crew attacked (D5.311, R65.26)",
             phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:H8:0")
g.weapon("mtr", "defender-light-mortar", "russian", "r1", kind="light-mortar")
g.vehicle("dh", "attacker-halftrack", "german", "bd03:D8:0", "east")
g.phases("rph", "pfph")
g.write()
