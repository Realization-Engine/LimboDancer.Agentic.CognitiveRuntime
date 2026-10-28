"""The Studio games of increment 5e (A15.23's hero's weapons; ruling R72.8).

All three games are on board 3 in July 1944. [A1], [A2], and [A3] are Open Ground at level 0 (code-builders/boards/los-survey "terrain" mode, bd03,
2026-10-27; [A4] is Woods and is not used). LOS and range, read with the LosCalculator through code-builders/boards/los-survey ("pair" mode, built outside
the repository, 2026-10-27):
- [A3] -> [A1]: clear, range 2 (all three games; the German 5cm mortar's minimum range is 2, the PSK's table reads 9 at 2 hexes, the ATR's Normal Range is 12).
No unit moves in any game.

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

# (a) C13.45, C13.48, A15.23, C13.25 (ruling R72.8). German PFPh: the German hero g-h in [A3] fires his PSK g-psk alone at the Russian T-34 r-t in [A1], two
# hexes off. In the Ordnance section choose the Weapon (select ordnance-gun) "[A3]: ... PSK" of g-h, the target [A1] (ordnance-target) and the vehicle
# "[A1]: T-34" (ordnance-vehicle); no leader. Dice (scripted, each with "Add"): To Hit 2, 3; To Kill 6, 6. The PSK's 9 at 2 hexes, his -1 ("hero's SW, g-h")
# and the +1 of one man firing a two-man SW ("hero alone with a two-man SW, g-h"): Final 5, a hit; the To Kill 12 is a dud, with no hero DRM. Before the
# pass a lone hero was refused the PSK. Read back: the ordnance-fired "facts" carry "heroWeapons": true; its "resolution" "toHit" "drm" holds
# {"name": "hero:g-h", "value": -1} and {"name": "hero-two-man:g-h", "value": 1}; "kill" "modifications" none named hero.
g = GameFile("p72-hero-psk", "pass 72, 5e: a lone German hero fires a PSK at a T-34 with the two-man +1 negated (A15.23, C13.45)",
             phasing="german", year=1944, month=7)
g.unit("g-h", "hero", "attacker-hero", "german", "bd03:A3:0")
g.weapon("g-psk", "attacker-psk", "german", "g-h", kind="latw")
g.vehicle("r-t", "defender-tank", "russian", "bd03:A1:0", "east")
g.phases("rph", "pfph", side="german")
g.write()

# (b) C13.24, C13.25, A15.23, A15.24 (ruling R72.8). Russian PFPh: the Russian hero r-h in [A3] fires his ATR r-atr as Small Arms at the German 4-6-7 g-1 in
# [A1], two hexes off. In the Fire panel tick r-h (its SW box then shows: tick r-atr; the hero has no inherent FP to add), target [A1] (fire-target).
# Dice (scripted, with "Add"): 5, 4. 1 FP, A15.24's heroic -1 ("heroic, r-h") within the ATR's Normal Range, no "hero's MG" +1 and no second -1: Final 8,
# no effect. Before the pass the hero was refused the ATR ("a hero fires one MG or FT"). Read back: the fire-resolved "resolution" "arithmetic" "drm" holds
# {"name": "heroic:r-h", "value": -1} and nothing named hero-mg; "totalFirepower": 1; "firers" does not list r-h.
g = GameFile("p72-hero-atr", "pass 72, 5e: a Russian hero fires an ATR as Small Arms at a German squad with his heroic DRM (A15.23, C13.24)",
             phasing="russian", year=1944, month=7)
g.unit("r-h", "hero", "defender-hero", "russian", "bd03:A3:0")
g.weapon("r-atr", "defender-atr", "russian", "r-h", kind="latw")
g.unit("g-1", "squad", "attacker-squad", "german", "bd03:A1:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (c) A15.23, C9.2 (ruling R72.8). German PFPh: the German hero g-h2 in [A3] fires his 5cm light mortar g-mtr at the Russian 4-4-7 r-1 in [A1], two hexes off.
# In the Ordnance section choose the Weapon (ordnance-gun) "[A3]: ... light mortar" of g-h2 and the target [A1] (ordnance-target); no Spotter, no leader.
# Dice (scripted, with "Add"): To Hit 2, 3; the hit's IFT DR 6, 5. The Area row's To Hit takes his -1 ("hero's SW, g-h2"): Final 4, a hit; the IFT DR of the
# hit takes neither it nor the heroic -1: 11 on the halved HE FP's column, no effect. A lone SMC loses the mortar's Multiple ROF (C9.2), and the hero has used
# a SW: his inherent FP is spent this Player Turn (A15.23; the Fire panel refuses g-h2 as a firer). Read back: the ordnance-fired "facts" carry "heroWeapons":
# true; "resolution" "toHit" "drm" holds {"name": "hero:g-h2", "value": -1}; "hit" "arithmetic" "drm" holds nothing named hero; "gun" "rateOfFire": 0.
g = GameFile("p72-hero-mortar", "pass 72, 5e: a German hero fires a light mortar with his -1 on the To Hit DR only (A15.23, C9.2)",
             phasing="german", year=1944, month=7)
g.unit("g-h2", "hero", "attacker-hero", "german", "bd03:A3:0")
g.weapon("g-mtr", "attacker-light-mortar", "german", "g-h2", kind="light-mortar")
g.unit("r-1", "squad", "defender-squad", "russian", "bd03:A1:0")
g.phases("rph", "pfph", side="german")
g.write()
