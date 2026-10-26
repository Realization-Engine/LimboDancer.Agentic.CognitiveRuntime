"""The Studio game of increment 4g (carrying a wounded man for five PP; A17.2, ruling R71.16), on board 3.

No enemy unit is on the board, so no LOS pair is relied on (the Russian side passes each DEFENDER window). Checked by the building agent with
hex_facts.py (2026-10-26): [D9], [D10], [E10], [F10], [F9], [F8], [G9], and [G10] are Open Ground at level 0 (1 MF each). Not checked: the adjacency of
the path's hexes, read from the column rule ([D9]-[D10] and [F10]-[F9]-[F8] share a column; [D10]-[E10] and [E10]-[F10] are next columns at the same
row number, which touch whichever way the columns are offset). Check it in the board viewer first.

The game: the German 4-6-7 de-s and the wounded 8-1 de-l in [D9], the German MPh.
- In the movement panel check both; "Carry the wounded" (#move-carry) offers "[D9]: ..." for de-l, and "carried by" (#move-carrier) "[D9]: ..." for de-s.
  Choose them, type [D10] in To, and propose: the proposal says "de-s carries the wounded de-l at five PP, and he spends no MF (A17.2)".
- The Russian side passes; move both on to [E10], [F10], [F9] (the carry holds with no new choice). The squad has 4 MF: 4, +2 for the wounded leader's
  bonus (A17.2 keeps it), -2 for five PP beyond its IPC of 3 (A4.42). A fifth step, to [F8], is refused: "de-s has 0 MF left".
- Or end de-s's move alone after a step: de-l, set down, is refused a step on his own ("... set down without it, so he moves no farther this MPh").

Read back in the game file: each "movement-step" of the stack has "carried": [{"man": "de-l", "carrier": "de-s"}]; de-l's "mfSpent" stays 0 in the
state (the Units tab), de-s's counts its MF. The Records tab's movement line ends ", de-s carrying the wounded de-l (A17.2)".

Not run by the building agent: the session runs it (it writes under src/ASL/boards).
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile  # noqa: E402


g = GameFile("p71-carry-wounded", "pass 71, 4g: a 4-6-7 carries a wounded 8-1 at five PP (A17.2)", phasing="german")
g.unit("de-s", "squad", "attacker-squad", "german", "bd03:D9:0")
g.unit("de-l", "leader", "attacker-leader-8-1", "german", "bd03:D9:0", wounded=True)
g.phases("rph", "pfph", "mph", side="german")
g.write()
