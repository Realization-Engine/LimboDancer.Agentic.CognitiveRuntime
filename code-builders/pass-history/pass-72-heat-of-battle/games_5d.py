"""The Studio games of increment 5d (A19.131's Ammunition Shortage; rulings R72.11, R72.12).

All three games are on board 3 with the SSR ammo-shortage:russian (a card token, R72.10). [A1], [A2], and [A3] are Open Ground at level 0 (5c's terrain read,
code-builders/boards/los-survey "terrain" mode, bd03, 2026-10-27). LOS and ranges, read with the LosCalculator through code-builders/boards/los-survey ("pair"
mode, 2026-10-27, for 5c; the same pairs are used here):
- [A3] -> [A2]: clear, range 1 (games a and c);
- [A3] -> [A1]: clear, range 2 (games b and c; [A1], [A2], [A3] are one hexgrain down column A, so a Fire Lane from [A3] through [A2] reaches [A1]).
Route of game c: [A1] -> [A2], adjacent hexes of column A, Open Ground, 1 MF.

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

SHORTAGE = ["ammo-shortage:russian"]

# (a) A19.131, ruling R72.11. Russian PFPh: the two Russian 4-4-7 r-1 and r-2 in [A3] fire PBF with their Inherent FP at the German 4-6-7 g-1 in [A2], r-1 with
# its LMG r-lmg too. In the Fire panel tick r-1 (its LMG box then shows: tick it) and r-2, target [A2]. Dice: 6, 6, an Original 12 (no effect on the 4-6-7);
# the LMG's B11 is B10 and X10 under the Shortage (A.11), so it malfunctions and is removed; the doubles make both squads cower (Cowering Random Selection dr:
# 1, 1); then the Random Selection dr for Ammunition Shortage: 2 for r-1, 5 for r-2. r-2 is Replaced by an unbroken 4-2-6 Conscript in [A3] with r-2's
# Prep Fire counter. The record line: "4-4-7 squad R2 in [A3]: the Original 12 under Ammunition Shortage (Random Selection dr 5, A.9): Replaced by 4-2-6 ...
# (A19.131)". Read back: the fire's "facts" carry "ammunitionShortage": true; its "resolution" "shortageEffects" [{"unitId": "r-2", "finalDefinitionId":
# "defender-conscript-squad", "randomSelectionDr": 5, ...}] and "weaponEffects" r-lmg "breakdownNumber": 10, "removed": true; a dice-rolled of purpose
# "fire-shortage-selection"; a public "lineage" event Replacing r-2. Both sides see the Replacement.
g = GameFile("p72-ammo-shortage-replacement", "pass 72, 5d: under Ammunition Shortage a Russian fire group's Original 12 Replaces one squad (A19.131)",
             phasing="russian", special=SHORTAGE)
g.unit("r-1", "squad", "defender-squad", "russian", "bd03:A3:0")
g.weapon("r-lmg", "defender-lmg", "russian", "r-1")
g.unit("r-2", "squad", "defender-squad", "russian", "bd03:A3:0")
g.unit("g-1", "squad", "attacker-squad", "german", "bd03:A2:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A19.131, A.11, ruling R72.12. Russian PFPh: the Russian 4-4-7 r-3 in [A3] fires its MMG r-mmg alone (tick r-3, then its MMG box, and fire the MMG
# without r-3's inherent FP; with it the result is the same, as no 12 is rolled) at the German 4-6-7 g-2 in [A1], two hexes off. The MMG's B11 is one lower, B10, and the lowered B#
# makes it an X10. Dice: 4, 6, an Original DR of 10: the MMG malfunctions and is removed. No Unit Replacement (no 12, and no Inherent FP). Read back: the fire's
# "facts" carry "ammunitionShortage": true; "weaponEffects" r-mmg "breakdownNumber": 10, "malfunctioned": true, "removed": true; an "instance-eliminated" of
# r-mmg. Without the token (delete the special rule) the same 10 does nothing to it (B11).
g = GameFile("p72-ammo-shortage-mmg", "pass 72, 5d: under Ammunition Shortage the Russian MMG breaks down at its lowered B# of 10 (A19.131, A.11)",
             phasing="russian", special=SHORTAGE)
g.unit("r-3", "squad", "defender-squad", "russian", "bd03:A3:0")
g.weapon("r-mmg", "defender-mmg", "russian", "r-3")
g.unit("g-2", "squad", "attacker-squad", "german", "bd03:A1:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (c) A19.131, A9.22, ruling R72.12. German MPh: the German 4-6-7 g-3 moves from [A1] into [A2]. The Russian side then fires r-4's LMG r-lmg2 in [A3] as
# Defensive First Fire at [A2] and declares a Fire Lane to [A1]. Refused: "the firing side's OB has Ammunition Shortages, which prevent placement of Fire
# Lanes; its MG may fire without one (A19.131, A9.22)". The same fire without the Fire Lane is allowed.
g = GameFile("p72-ammo-shortage-fire-lane", "pass 72, 5d: under Ammunition Shortage a Russian Fire Lane is refused (A19.131)",
             phasing="german", special=SHORTAGE)
g.unit("r-4", "squad", "defender-squad", "russian", "bd03:A3:0")
g.weapon("r-lmg2", "defender-lmg", "russian", "r-4")
g.unit("g-3", "squad", "attacker-squad", "german", "bd03:A1:0")
g.phases("rph", "mph", side="german")
g.write()
