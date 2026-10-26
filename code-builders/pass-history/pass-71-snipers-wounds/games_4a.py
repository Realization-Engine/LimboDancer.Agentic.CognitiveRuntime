"""The Studio games of increment 4a (the Sniper's targets and results, A14.21, A14.22, A14.3, A14.31, A14.33, D5.34; rulings R71.3 to R71.6), on board 3.

In each game the Russian is phasing in his PFPh: his 4-4-7 in [P8] (woods) fires at the German 4-6-7 in [P10] (Open Ground). Pass 70's read of board 3
(LosCalculator, 2026-10-25) gave [P8] a LOS to [P10] at range 2; no other LOS pair is relied on. The German SAN is 6, so an IFT DR of 6 calls the German
Sniper (A14.1). The German Sniper counter is in [Q9], unoccupied; a Random Location DR of colored 1, white 1 moves it one hex across its top hexside,
to [Q8] (road), where each game puts its target. [Q8] is the closest hex holding a Russian target, so nothing else is attacked.

The dice, in order, entered in the Studio's dice box: the IFT DR 3, 3; the German 4-6-7's MC 3, 3 when the IFT calls one (if it does not, the next die
is the Sniper's); the Sniper dr; the Random Location DR 1, 1; then any Random Selection dice the game names.

Not run by the building agent: the session runs it (it writes under src/ASL/boards). The Studio's load of a Sniper counter placed by an instance-created
event with no definition is not yet checked (brief_build.md, test games).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


def start(game, label):
    """The game's common part: the two squads, the German SAN, and the German Sniper in [Q9]; the Russian PFPh."""
    g = GameFile(game, label, phasing="russian")
    g.san("german", 6)
    g.unit("g1", "squad", "attacker-squad", "german", "bd03:P10:0")
    g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
    g.sniper("gs", "german", "bd03:Q9:0")
    return g


# (a) A14.3, D5.34: the CE Russian T-34 in [Q8] (bu=False): its Inherent crew is the one target there. Dice: 3, 3, (3, 3), Sniper dr 2, 1, 1. The
# German Sniper moves to [Q8] and the T-34's crew is Stunned: the tank shows Stun and BU. Read back: "sniper-attacked" with "unit": "r-tk" and
# "result": "stunned", then "conditions-changed" of r-tk with "asl:stunned": true. The Snipers record list (#play-snipers) reads
# "...: dr 2; [Q8]: the crew of T-34 M41 ... is Stunned (A14.3, D5.34)". A Sniper dr of 1 instead Recalls it ("recalled", D5.341).
g = start("p71-sniper-ce-tank", "pass 71, 4a: a Sniper's dr 2 Stuns the CE crew of a tank (A14.3, D5.34)")
g.vehicle("r-tk", "defender-tank", "russian", "bd03:Q8:0", "east", bu=False)
g.phases("rph", "pfph", side="russian")
g.write()

# (b) A14.22, A14.33: the Russian truck in [Q8] carries a 4-4-7 Passenger: three candidates, in id order r-p (the Passenger), r-t's Inherent crew, and
# r-t itself. Dice: 3, 3, (3, 3), Sniper dr 1, 1, 1, then Random Selection 1, 1, 6: the truck is immobilized ("result": "immobilized"; r-t
# "asl:immobilized": true). With Random Selection 1, 6, 1 instead, the crew breaks ("crew-broken"): a broken crew counter is placed inside r-t, which
# Stops. The Passenger takes no dr either way (a vehicle and its PRC never share one).
g = start("p71-sniper-truck", "pass 71, 4a: a Sniper picks among a truck's Passenger, crew, and the truck itself (A14.22, A14.33)")
g.vehicle("r-t", "defender-truck", "russian", "bd03:Q8:0", "east")
g.passenger("r-p", "squad", "defender-squad", "russian", "r-t")
g.phases("rph", "pfph", side="russian")
g.write()

# (c) A14.31: the Russian Sniper counter alone in [Q8] is the target. Dice: 3, 3, (3, 3), Sniper dr 2, 1, 1: it is pinned ("result": "sniper-pinned";
# rs "asl:pinned": true; the counter shows its pinned face). Advance to the end of the CCPh: the pin is removed with the others. A Sniper dr of 1
# eliminates it ("sniper-eliminated"; the SAN is not yet reduced, increment 4b).
g = start("p71-sniper-vs-sniper", "pass 71, 4a: a Sniper attacks the enemy Sniper counter and pins it to the CCPh's end (A14.31)")
g.san("russian", 3)
g.sniper("rs", "russian", "bd03:Q8:0")
g.phases("rph", "pfph", side="russian")
g.write()
