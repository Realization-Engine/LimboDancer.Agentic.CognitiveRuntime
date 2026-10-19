"""The placed-map test game of the A3 Map Context Plan (task 1): a3-gambit-rout.

The Gambit card's map, bd04 at (0,0) and bd02 at (0,1) reversed, at the German Advancing Fire Phase of turn 1. Reversed below bd04, bd02's
row 10 meets bd04's row 10 and its columns run mirrored (bd04's CC10 faces bd02's E10), so bd02's woods at E9 and C9 lie one hex past the
seam from bd04's open ground at CC10 to EE10, while bd04's nearest building (Y9) is four or more hexes away. The German broken squads
there rout across the seam. (bd04's BB10, an even column's row 10, is the seam's half hex: a unit placed there by its bd04 name makes
the game stop replaying at its first phase, with no diagnostic; backlog.) The British broken squad at bd02's CC10 faces bd04's E10, with bd04's woods at D9 beyond it, and must rout
from the German squad in bd02's woods at Z9; the one at bd02's P8 is in no enemy's LOS, a control that stays. Good-order squads and a leader of each side stand on both boards, the British ones in LOS
of the German broken squads in the open, so those must rout.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402

g = GameFile("a3-gambit-rout", "A3: a Rout Phase across the seam of a placed map (bd04, bd02 reversed)", phasing="german", other="british",
             year=1941, month=5, boards=[("04", 0, 0, False), ("02", 0, 1, True)])
# German: three broken squads in the open by the seam; a leader with two squads in bd04's building at Y9; a squad in bd02's building at I9.
g.unit("g-broken-1", "squad", "attacker-squad", "german", "bd04:CC10:0", broken=True)
g.unit("g-broken-2", "squad", "attacker-squad", "german", "bd04:EE10:0", broken=True)
g.unit("g-broken-3", "squad", "attacker-squad", "german", "bd04:DD9:0", broken=True)
g.unit("g-leader", "leader", "attacker-leader-8-1", "german", "bd04:Y9:0")
g.unit("g-squad-1", "squad", "attacker-squad", "german", "bd04:Y9:0")
g.unit("g-squad-2", "squad", "attacker-squad", "german", "bd04:Y9:0")
g.unit("g-squad-3", "squad", "attacker-squad", "german", "bd02:I9:0")
g.unit("g-squad-4", "squad", "attacker-squad", "german", "bd02:Z9:0")
# British: a leader and a squad on bd04's road at BB7, in LOS of the German broken squads; a squad in bd02's building at M9; two broken.
g.unit("b-leader", "leader", "british-leader-8-0", "british", "bd04:BB7:0")
g.unit("b-squad-1", "squad", "british-squad", "british", "bd04:BB7:0")
g.unit("b-squad-2", "squad", "british-squad", "british", "bd02:M9:0")
g.unit("b-broken-1", "squad", "british-squad", "british", "bd02:CC10:0", broken=True)
g.unit("b-broken-2", "squad", "british-squad", "british", "bd02:P8:0", broken=True)
g.phases("rph", "pfph", "mph", "dfph", "afph")
g.write()
