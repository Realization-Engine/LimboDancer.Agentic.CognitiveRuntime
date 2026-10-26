"""Writes code-builders/test-games/game_started_template.json: the tenant and the game-started payload of p35-rst-cc, the game game_file.py
took its template from until the proof games were trimmed (PR 38, 2026-10-25) and the game was retired outside the repository. The payload alone
is kept, never a played event (the standing rules: a game is never built from one that has been played).

    python template_from_retired.py <retired p35-rst-cc.game.json>
"""
import json
import os
import sys

source = sys.argv[1]
game = json.load(open(source, encoding="utf-8"))
first = game["events"][0]
assert first["type"] == "game-started", first["type"]
target = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "test-games", "game_started_template.json")
template = {"note": "The game-started payload of p35-rst-cc (board 3), kept for game_file.py since PR 38 retired that game (pass 70, 2026-10-25).",
            "tenant": game["tenant"], "payload": first["payload"]}
with open(target, "w", encoding="utf-8", newline="\n") as out:
    out.write(json.dumps(template, indent=2, ensure_ascii=False) + "\n")
print(os.path.normpath(target), len(json.dumps(first["payload"])), "characters of payload")
