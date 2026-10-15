"""The Studio games of increment 2a (HE Equivalency), on board 3."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# AP at Infantry: the 6-Pounder in July 1942 has no HE yet (HE7 from February 1943), so AP by its HE Equivalency (2 FP).
g = GameFile("p60-ap", "pass 60, 2a: a 6-Pounder's AP at a squad in the open (C8.31)", phasing="british", other="british", year=1942, month=7)
g.gun("gb", "british-at-gun", "british", "bd03:F10:0", "west")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
g.phases("rph", "pfph")
g.write()

# A PSK at a squad in a stone building (12 FP), and the same squad in the open, refused (C8.31's HEAT bar).
for game, at, label in (("p60-psk", "bd03:O5:0", "a PSK at a squad in a stone building, 12 FP (C8.31, C13.9)"),
                        ("p60-psk-open", "bd03:D10:0", "CONTROL: a PSK at a squad in the open, refused (C8.31)")):
    g = GameFile(game, "pass 60, 2a: " + label, phasing="german", year=1944, month=7)
    g.unit("g1", "squad", "attacker-squad", "german", "bd03:O3:0" if at == "bd03:O5:0" else "bd03:F10:0")
    g.weapon("psk", "attacker-psk", "german", "g1", kind="latw")
    g.unit("r1", "squad", "defender-squad", "russian", at)
    g.phases("rph", "pfph")
    g.write()

# A PF at a squad in a stone building: the PF Check +1 at a target that is not an AFV, one unit by Random Selection, 16 FP.
g = GameFile("p60-pf", "pass 60, 2a: a PF at two squads in a stone building (C13.31, C8.31)", phasing="german", year=1944, month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:O3:0")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:O5:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:O5:0")
g.phases("rph", "pfph")
g.write()

# An ATR at a Gun: 1 FP on the Gun and its crew only (C13.23, C11.52).
g = GameFile("p60-atr-gun", "pass 60, 2a: an ATR at a German INF Gun and its crew (C13.23)", phasing="russian", year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:F10:0")
g.weapon("atr", "defender-atr", "russian", "r1", kind="latw")
g.gun("gg", "attacker-inf-gun", "german", "bd03:D10:0", "east")
g.phases("rph", "pfph")
g.write()
