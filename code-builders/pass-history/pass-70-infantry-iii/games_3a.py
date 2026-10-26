"""The Studio games of increment 3a (who sees and the Right of Inspection, A12.14, A12.141, A12.16; rulings R70.16 to R70.18), on board 3.

The LOS pairs were read from board 3 (Verified) with LosCalculator on 2026-10-25: [P8] (woods) sees [O10] and [P10] at range 2; [Q8] (road)
sees [P10] at 3 and not [O10]; [M8] (woods) sees neither; [Q9] sees [O10] at 2.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


def stacks(game):
    """Two German 4-6-7s in the wooden building [O10], one with a LMG, and two in the open of [P10]; none concealed."""
    game.unit("g1", "squad", "attacker-squad", "german", "bd03:O10:0")
    game.unit("g2", "squad", "attacker-squad", "german", "bd03:O10:0")
    game.weapon("gl", "attacker-lmg", "german", "g2")
    game.unit("g3", "squad", "attacker-squad", "german", "bd03:P10:0")
    game.unit("g4", "squad", "attacker-squad", "german", "bd03:P10:0")


# After setup: the Russian in [Q8] sees [P10] only, the one in [M8] neither. The Russian view shows the [P10] stack in full and of the [O10] stack
# its top, g1, and one counter it may not inspect (A12.16); g2's LMG is not shown. The German view shows all four squads.
g = GameFile("p70-inspect-out", "pass 70, 3a: the German stack in [O10] is out of every Russian LOS (A12.16)", phasing="german")
stacks(g)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:Q8:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:M8:0")
g.phases("rph", "pfph")
g.write()

# The same with the first Russian in the woods of [P8], which sees both stacks: the Russian view shows all four German squads and the LMG.
g = GameFile("p70-inspect-seen", "pass 70, 3a: a Russian in [P8] sees both German stacks, inspected in full (A12.16)", phasing="german")
stacks(g)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:P8:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:M8:0")
g.phases("rph", "pfph")
g.write()

# The first game before play (no phase change, so setup has not closed): both German stacks show their top only, as before the pass (A2.9).
g = GameFile("p70-inspect-before", "pass 70, 3a: before play every stack shows its top only (A2.9)", phasing="german")
stacks(g)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:Q8:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:M8:0")
g.write()

# A12.141 (ruling R70.17): in the German RPh the concealed 4-6-7 in [O10] passes its LMG to the concealed HS there (Transfer panel). The Good Order
# Russian in [Q9] sees [O10] at range 2, so both lose their "?" (two concealment-lost records after the equipment-transferred one).
g = GameFile("p70-transfer-seen", "pass 70, 3a: a transfer in view of a Good Order enemy costs both units their \"?\" (A12.141)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:O10:0", concealed=True)
g.unit("g2", "half-squad", "attacker-half-squad", "german", "bd03:O10:0", concealed=True)
g.weapon("gl", "attacker-lmg", "german", "g1")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:Q9:0")
g.phases("rph")
g.write()

# The control: the Russian in [M8] sees nothing of [O10], so the transfer keeps both "?".
g = GameFile("p70-transfer-unseen", "pass 70, 3a: the same transfer out of every Russian LOS keeps both \"?\" (A12.141)", phasing="german")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:O10:0", concealed=True)
g.unit("g2", "half-squad", "attacker-half-squad", "german", "bd03:O10:0", concealed=True)
g.weapon("gl", "attacker-lmg", "german", "g1")
g.unit("r1", "squad", "defender-squad", "russian", "bd03:M8:0")
g.phases("rph")
g.write()
