"""The Studio games of the reviews' fixes, Round B (rulings R72.18, R72.20; the reviews R3.3, R3.10, R4.13).

Both games are on board 3. LOS, ranges, and terrain are 5c's and 5b's (code-builders/boards/los-survey, "pair" and "terrain" modes, bd03, 2026-10-27):
- [A3] -> [A1]: clear, range 2, both Open Ground at level 0 (game a, as games_5d.py's game b);
- [A2]: Open Ground at level 0, the Location of game b (as games_5b.py's game b; no LOS or route is read, the CC is in the one Location).

The other Round B changes are seen in the games of 5d and 5e, rewritten by their scripts:
- games_5d.py, game a: the shortage line lists both candidates' dr, "(Random Selection: 4-4-7 squad R1 dr 2, 4-4-7 squad R2 dr 5, A.9)";
- games_5d.py, game c: under the Shortage the Fire panel offers no "Fire Lane of" field (#fire-lane-weapon);
- games_5e.py, the PSK game: the proposal's DRM reads "hero's SW, hero ... -1" and "hero alone with a two-man SW, ... +1", the record cites A15.23 for the +1.
  A p72-hero-psk game played before this round does not replay: its recorded resolution cites C13.45 (session games only; rewrite it with games_5e.py).

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

SHORTAGE = ["ammo-shortage:russian"]

# (a) A19.131, A.11, ruling R72.18. Russian PFPh: the Russian 4-4-7 r-3 in [A3] fires its MMG r-mmg alone (tick r-3, then its MMG box, and fire the MMG
# without r-3's inherent FP) at the German 4-6-7 g-2 in [A1], two hexes off. Under the Shortage the MMG's B11 is B10 and its X# is its printed B11. Dice: 5, 6,
# an Original DR of 11: the MMG malfunctions and is removed. The record line: "the MMG ... is removed: the DR reached its X11 (B11, lowered to B10: less 1 for
# Ammunition Shortage, A19.131) (A.11, A9.7)". With 4, 6 (10) instead: "the MMG ... malfunctioned: the DR reached its B10 (B11, less 1 for Ammunition
# Shortage, A19.131) (A9.7)", not removed. Read back: "weaponEffects" r-mmg "breakdownNumber": 10, "malfunctioned": true, "removed": true on the 11 only.
g = GameFile("p72-fixb-mmg-x11", "pass 72, fixes B: under Ammunition Shortage the Russian MMG is B10 and X11 (A19.131, A.11; ruling R72.18)",
             phasing="russian", special=SHORTAGE)
g.unit("r-3", "squad", "defender-squad", "russian", "bd03:A3:0")
g.weapon("r-mmg", "defender-mmg", "russian", "r-3")
g.unit("g-2", "squad", "attacker-squad", "german", "bd03:A1:0")
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A11.5, A15.46, ruling R72.20: games_5b.py's game b with a Russian HS r-hs riding the tank ru-tk (D6.2: Riders from 1943; the game is of 1944). The
# berserk German 4-6-7 g-1 and the tank share [A2] as the German CCPh begins. Clicks: German side, Close Combat panel, the vehicle attack in [A2]: attackers
# g-1, the vehicle ru-tk; propose, confirm. Dice: 1 + 2 = 3 against its Kill Number 5: the tank is a wreck, and its Rider is no Known enemy unit left (A11.5:
# "the PRC of a vehicle ... are eliminated if the vehicle is destroyed"), so g-1 returns to normal: "4-6-7 squad ... in [A2] eliminated every Known enemy unit in
# its Location by destroying the enemy vehicle in CC, so it is no longer berserk and returns to Good Order (A15.46)". Before the round the A15.46 count read
# the Rider as a Known enemy unit left (the reviews R1.8). Read back: after "vehicle-close-combat-resolved" and "vehicle-wrecked", a "conditions-changed" of g-1
# with "asl:berserk": false; check also what became of r-hs. Same setup in Play: BacklogPass72Tests.ABerserkSquadThatDestroysATankCarryingARiderInCcReturns
# ToNormal (advanced into the tank's hex there).
g = GameFile("p72-fixb-rider-cc-return", "pass 72, fixes B: a berserk squad destroys the Russian tank carrying a Rider in CC and returns to normal (A11.5, A15.46)",
             phasing="german", year=1944)
g.unit("g-1", "squad", "attacker-squad", "german", "bd03:A2:0", berserk=True)
g.vehicle("ru-tk", "defender-tank", "russian", "bd03:A2:0", "east")
g.passenger("r-hs", "half-squad", "defender-half-squad", "russian", "ru-tk", role="rider")
g.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph", side="german")
g.write()
