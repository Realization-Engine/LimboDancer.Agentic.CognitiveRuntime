"""The Studio games of increment 4b (the SAN as state and Sniper Check, A14.4, E1.76; rulings R71.9, R71.10), on board 3.

The layout is 4a's, so its one LOS pair holds: the Russian 4-4-7 in [P8] (woods) fires in the Russian PFPh at the German 4-6-7 in [P10] (Open Ground);
pass 70's read of board 3 (LosCalculator, 2026-10-25) gave [P8] a LOS to [P10] at range 2. No other LOS pair is relied on. The German SAN is 2, so the
Russian IFT DR of 1 + 1 calls the German Sniper in [Q9] (A14.1). Its Random Location DR of colored 1, white 1 moves it to [Q8] (road), where the Russian
8-1 r-l, 4-4-7 r-s, and 2-3-7 r-h stand (in [Q8], not the brief's [F10], so 4a's checked layout serves; [Q8] is the closest hex with a Russian target).

The dice, in order, entered in the Studio's dice box: the IFT DR 1, 1 (the doubles cower it to the 2 FP column: K/1 on g1); g1's MC 1, 2 (if the IFT
calls none, the next die is the Sniper's); the Sniper dr 2; the Random Location DR 1, 1; Random Selection 6, 1, 1, which picks r-h (the first by id), and
the dr 2 pins it. The game then waits for the Russian side's Sniper Check (#play-choice, two buttons "Propose: take the Sniper Check" and
"Propose: decline: no Sniper Check"; the choice reads "... units in [Q8] ... make a Sniper Check against the German Sniper ... (A14.4)").
The 8-1 directing the 4-4-7 gives -1, the squad -2: DRM -3. r-h, pinned, takes no part.

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


def start(game, label):
    """The common part: g1 and r1, the German SAN of 2, the German Sniper in [Q9], and the Russian stack in [Q8]; the Russian PFPh."""
    g = GameFile(game, label, phasing="russian")
    g.san("german", 2)
    g.unit("g1", "squad", "attacker-squad", "german", "bd03:P10:0")
    g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
    g.sniper("gs", "german", "bd03:Q9:0")
    g.unit("r-l", "leader", "defender-leader-8-1", "russian", "bd03:Q8:0")
    g.unit("r-s", "squad", "defender-squad", "russian", "bd03:Q8:0")
    g.unit("r-h", "half-squad", "defender-half-squad", "russian", "bd03:Q8:0")
    g.phases("rph", "pfph", side="russian")
    return g


# (a) The check eliminates the Sniper: answer "take" and enter 3, 2 (3 + 2 - 3 = 2). Read back: "sniper-checked" with "units": ["r-l", "r-s"], "drm": -3,
# "result": "sniper-eliminated"; "san-reduced" with "side": "german", "cause": "sniper-check"; "instance-eliminated" of gs (the SAN 2 is 1, A14.1);
# "conditions-changed" of r-l and r-s with "asl:ti": true. #play-snipers reads "... make a Sniper Check against the German Sniper (gs): DR 3, 2 = 5 - 3 =
# 2, the Sniper is eliminated; they are TI (A14.4)" and "The German Sniper is eliminated by a Sniper Check: the German SAN is now 1, below 2, so its Sniper
# counter is removed (A14.1, A14.4)". A later Russian DR of 2 calls nothing.
start("p71-sniper-check-eliminates", "pass 71, 4b: a Sniper Check eliminates the Sniper, and the SAN 2 to 1 removes its counter (A14.4)").write()

# (b) The check pins the Sniper: answer "take" and enter 3, 3 (6 - 3 = 3). Read back: "sniper-checked" with "result": "sniper-pinned", then
# "conditions-changed" of gs with "asl:pinned": true; the counter shows its pinned face, the German SAN stays 2 (no "san-reduced"). Answer "decline"
# instead: only "choice-made" is written, and nobody is TI.
start("p71-sniper-check-pins", "pass 71, 4b: a Sniper Check of 3 pins the Sniper (A14.4)").write()
