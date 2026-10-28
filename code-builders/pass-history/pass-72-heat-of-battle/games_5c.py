"""The Studio games of increment 5c (A19.132's SSR ELR for underscored units and A19.32's MG B# in Inexperienced hands; rulings R72.9, R72.10, R72.13).

Both games are on board 3 (code-builders/boards/los-survey "terrain" mode, bd03, 2026-10-27: [A1], [A2], and [A3] are Open Ground at level 0). LOS and ranges,
read with the LosCalculator through code-builders/boards/los-survey ("pair" mode, built outside the repository, 2026-10-27):
- [A3] -> [A2]: clear, range 1 (game a, PBF);
- [A3] -> [A1]: clear, range 2 (game b).

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# (a) A19.132, ruling R72.13: the SSR underscored-elr:german:3 (a card token, R72.10). Russian PFPh: the two Russian 4-4-7 r-1 and r-2 in [A3] fire PBF
# at the German 8-3-8 g-1 in [A2]: 8 FP doubled, 16 FP. Dice: 3, 4 (a 2MC on the 16 column); the MC 5, 5 (12, +2) fails Morale Level 8 by four. Beyond the
# SSR's ELR of 3 (R15.9's 5 would only break it), so the 8-3-8 is Replaced by a broken 4-3-6 Conscript ("replaced-elr"). Read back: the fire's "facts" carry
# "unitSubstitution": true and, on its target g-1, "underscoredElr": 3; a "lineage" event makes g-1 an attacker-conscript-squad, broken, in [A2]. Without the
# token (delete the special rule) the same dice only break the 8-3-8, and its target carries no "underscoredElr".
g = GameFile("p72-underscored-elr", "pass 72, 5c: under an SSR ELR of 3 a failed MC Replaces the German 8-3-8 by a 4-3-6 Conscript (A19.132)",
             phasing="russian", special=["underscored-elr:german:3"])
g.unit("r-1", "squad", "defender-squad", "russian", "bd03:A3:0")
g.unit("r-2", "squad", "defender-squad", "russian", "bd03:A3:0")
g.unit("g-1", "squad", "attacker-elite-squad-8-3-8", "german", "bd03:A2:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A19.32, ruling R72.9 (R9.10 lifted): Russian PFPh: the Russian 4-2-6 Conscript r-c in [A3] fires its LMG r-lmg (B11) with its inherent FP at the
# German 4-6-7 g-2 in [A1], two hexes off: 4 + 2 FP. Dice: 4, 6, an Original DR of 10, one below the LMG's printed B11: in a Conscript's hands its B# is
# 10, so it malfunctions (its X# is the printed 11 by A.11, which a 10 does not reach, so it is not removed). Read back: the fire's "facts" carry
# "unitSubstitution": true; its "resolution" "weaponEffects" r-lmg "breakdownNumber": 10, "malfunctioned": true; the LMG is marked malfunctioned. A 4, 5
# (9) does not malfunction it. (dl-tractor-works-01 revision 1267, recorded before the pass, keeps B11 for its Conscript LMG and its DR 10.)
g = GameFile("p72-inexperienced-mg", "pass 72, 5c: a Russian Conscript's LMG malfunctions at its lowered B# of 10 (A19.32)", phasing="russian")
g.unit("r-c", "squad", "defender-conscript-squad", "russian", "bd03:A3:0")
g.weapon("r-lmg", "defender-lmg", "russian", "r-c")
g.unit("g-2", "squad", "attacker-squad", "german", "bd03:A1:0")
g.phases("rph", "pfph", side="russian")
g.write()
