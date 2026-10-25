"""One more Studio game of increment 3e, written by the session: p70-hidden-gun's [L8] has no LOS to [G7] (the board viewer's LOS tab, read 2026-10-25:
"Blocked"), so the hidden Gun was never found there. Here the Russian 4-4-7 stands in [H8], whose LOS to [G7] the same tab reads "Clear". Ending the
Russian RPh places the Gun and its crew beneath "?" ("hidden-placed", visibility ["german"]); the Russian view shows a "?" in [G7]. Written apart from
games_3e.py so the games already played there are not rewritten.
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import CREWS, GameFile  # noqa: E402

g = GameFile("p70-hidden-gun-seen", "pass 70, 3e: a hidden Gun out of Concealment Terrain in a Russian LOS is found (A12.34)", phasing="russian")
g.unit("gg-crew", "crew", CREWS["german"], "german", "bd03:G7:0", hidden=True)
g.add("instance-created", {"instance": {"id": "gg", "kind": "asl:gun", "definition": "attacker-inf-gun", "side": "german",
                                        "position": {"at": "bd03:G7:0", "facing": "east"}, "holding": {"holder": "gg-crew", "role": "manned"},
                                        "conditions": {"asl:malfunctioned": False, "asl:hidden": True}}})
g.unit("r1", "squad", "defender-squad", "russian", "bd03:H8:0")
g.phases("rph")
g.write()
