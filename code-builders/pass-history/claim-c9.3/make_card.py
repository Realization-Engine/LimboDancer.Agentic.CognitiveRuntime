"""The C9.3 claim's minigame card (ruling RC93.10), made from the rally minigame's card: the same minimal form on board bd02, whose hill gives a
mortar at ground level a Spotter on the slope.

    python make_card.py
"""
import json
import os

UNITS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "src", "ASL", "units", "minigames")

card = json.load(open(os.path.join(UNITS, "a10-rally", "a10-rally.scenario-card.json"), encoding="utf-8"))
card["id"] = "c9-spotter"
card["title"] = "Minigame: C9.3 Spotters"
card["source"]["basis"] = ("Manufactured under R0.3 as the minigame of the C9.3 claim (the Rules-first plan, section 7): the fewest counters a Spotter "
                           "needs, on an imported board.")
card["source"]["adaptation"] = ["A minimal card (ruling R22.3): the units are set up by hand, a light mortar and its crew at ground level beside a "
                                "hill, and a squad that may spot for it from the slope, in the PFPh."]
card["introduction"] = "A light mortar below a hill is given a Spotter on the slope above it."
card["boards"][0]["board"] = "bd02"
card["minigame"] = "c9-spotter"
os.makedirs(os.path.join(UNITS, "c9-spotter"), exist_ok=True)
with open(os.path.join(UNITS, "c9-spotter", "c9-spotter.scenario-card.json"), "w", encoding="utf-8", newline="\n") as out:
    out.write(json.dumps(card, indent=2, ensure_ascii=False) + "\n")
print("written")
