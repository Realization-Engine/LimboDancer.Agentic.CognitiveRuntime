"""The proving slice's Studio games (first task 4): positions for the Rule Workbench on board 3, written into the live folder.

ps-rally:   the RPh; a broken German squad and an 8-1 leader in the woods of [F5]; a Russian squad at [D10] (the A10.6 rally, every fact gathered).
ps-advance: the APh; a concealed German squad at [E6] beside the Open Ground of [D10] seen from [F10]... and a Good Order Russian squad in the woods of
            [F5] within 16 hexes (A12.14: the advance into Open Ground in view; the enemy's LOS is the adjudicator's fact).
ps-supply:  a German halftrack and a German tank, for the E10 facts entered by hand.

Each is marked a minigame's game in its header, as the store marks a game started from a minigame card, so the workbench offers every view
of it (a test position, ruling RPS.11).

    python games_workbench.py
"""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile, LIVE  # noqa: E402


def write(game, minigame):
    game.write()
    path = LIVE + game.game + ".game.json"
    with open(path, encoding="utf-8") as source:
        record = json.load(source)
    ordered = {key: record[key] for key in ("schemaVersion", "tenant", "game", "label", "synthetic")}
    ordered["minigame"] = minigame
    ordered["events"] = record["events"]
    with open(path, "w", encoding="utf-8", newline="") as out:
        json.dump(ordered, out, indent=2)
        out.write("\n")


rally = GameFile("ps-rally", "Proving slice: a broken squad and its leader in woods, in the RPh (A10.6)")
rally.unit("g-squad", "squad", "attacker-squad", "german", "bd03:F5:0", broken=True)
rally.unit("g-leader", "leader", "attacker-leader-8-1", "german", "bd03:F5:0")
rally.unit("r-squad", "squad", "defender-squad", "russian", "bd03:D10:0")
rally.phases("rph")
write(rally, "a10-rally")

advance = GameFile("ps-advance", "Proving slice: a concealed squad about to advance into Open Ground in view (A12.14)")
advance.unit("g-squad", "squad", "attacker-squad", "german", "bd03:E10:0", concealed=True)
advance.unit("r-squad", "squad", "defender-squad", "russian", "bd03:F10:0")
advance.phases("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph")
write(advance, "a12-advance-concealment")

supply = GameFile("ps-supply", "Proving slice: a supply vehicle beside a tank (E10, facts by hand)")
supply.vehicle("g-supply", "attacker-halftrack", "german", "bd03:D10:0", "east")
supply.vehicle("g-tank", "attacker-tank", "german", "bd03:D10:0", "east")
supply.phases("rph")
write(supply, "e10-eligibility")
