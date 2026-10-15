"""After the reviews' fixes: p60-random-sw carries the heldWeapons fact no longer recorded, so it is labelled SUPERSEDED and its game written
again as p60-random-sw2; and the games of the narrower Studio check (hidden information, pass 60's leak fixes)."""
import json
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games"))
from game_file import GameFile, LIVE  # noqa: E402

path = LIVE + "p60-random-sw.game.json"
with open(path, encoding="utf-8") as source:
    game = json.load(source)
if not game["label"].startswith("SUPERSEDED"):
    game["label"] = "SUPERSEDED by p60-random-sw2 (it records heldWeapons, no longer recorded): " + game["label"]
    with open(path, "w", encoding="utf-8", newline="") as out:
        json.dump(game, out, indent=2)
        out.write("\n")

g = GameFile("p60-random-sw2", "pass 60, 60.5: a KIA on a squad holding a LMG, the LMG's dr (A9.74), after the fixes", phasing="russian",
             year=1943, month=7)
g.unit("r1", "squad", "defender-squad", "russian", "bd03:D9:0")
g.unit("r2", "squad", "defender-squad", "russian", "bd03:D9:0")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
g.weapon("lmg", "attacker-lmg", "german", "g1", kind="mg")
g.phases("rph", "pfph")
g.write()

# Hidden information: a Russian 45L at [D10], where a Known German squad stands beside a hidden one and a concealed one. The Russian view must
# not learn of the hidden squad, nor of the concealed squad's weapon, from the proposal or the record.
g = GameFile("p60-hidden", "pass 60, the fixes: a shot at a Known squad beside a hidden squad and a concealed squad with a LMG", phasing="russian",
             year=1943, month=7)
g.gun("rg", "defender-at-gun", "russian", "bd03:F10:0", "west")
g.unit("g1", "squad", "attacker-squad", "german", "bd03:D10:0")
g.unit("g2", "squad", "attacker-squad", "german", "bd03:D10:0", hidden=True)
g.unit("g3", "squad", "attacker-squad", "german", "bd03:D10:0", concealed=True)
g.weapon("lmg", "attacker-lmg", "german", "g3", kind="mg")
g.phases("rph", "pfph")
g.write()

# The unseen enemy vehicle: a German squad with a LMG in [F10]; a hidden Russian T-34 far off in [P10]. The German Gun panel must not offer the
# LMG for a To Kill attack, which would tell the German side a vehicle exists.
g = GameFile("p60-unseen-afv", "pass 60, the fixes: a hidden T-34 does not make the LMG's To Kill attack appear", phasing="german", year=1943,
             month=7)
g.unit("g1", "squad", "attacker-squad", "german", "bd03:F10:0")
g.weapon("lmg", "attacker-lmg", "german", "g1", kind="mg")
g.vehicle("rt", "defender-tank", "russian", "bd03:P10:0", "east", hidden=True)
g.phases("rph", "pfph")
g.write()
