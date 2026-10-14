"""Correct the labels of pass 55's test games to what each file holds (review 3, finding 3), and mark the spoiled copies."""
import io
import json

LIVE = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/"
LABELS = {
    "p55-he-numberc": "Pass 55 task 55.1: June 1944, a 6-Pounder's HE (HE8) at a squad in [G10], named by the page: used on an Original DR of 6 (C8.8, C8.9; board 3)",
    "p55-can-none": "Pass 55 task 55.2: Canister on an Original DR of 8: the Gun had none, attacks nothing, and has not fired (C8.9; board 3)",
    "p55-al-shock": "Pass 55 task 55.5: a T-34 hits a PzKpfw IIIH with a 10-2 Armor Leader and the TK DR is a dud: no check, so no morale in the record (D3.42; board 3)",
    "p55-al-shock2": "Pass 55 task 55.5: a T-34 hits a PzKpfw IIIH with a 10-2 Armor Leader for a possible Shock: the NTC against morale 10 (D3.42; board 3)",
    "p55-al-ctl": "NOT PLAYED: the control of p55-al-fire; p55-al-shock's firer has no Armor Leader and shows the same shot without his line",
}
SPOILED = ["p55-apds-used", "p55-apds-none", "p55-apds-malf", "p55-apds-may", "p55-apds-elite", "p55-he-number", "p55-he-1942",
           "p55-apds-usedb", "p55-apds-noneb", "p55-apds-malfb", "p55-apds-mayb", "p55-apds-eliteb", "p55-he-numberb", "p55-he-1942b"]
for game, label in LABELS.items():
    path = LIVE + game + ".game.json"
    data = json.load(io.open(path, encoding="utf-8"))
    data["label"] = label
    with io.open(path, "w", encoding="utf-8", newline="") as out:
        json.dump(data, out, indent=2)
        out.write("\n")
    print("relabelled", game)
for game in SPOILED:
    path = LIVE + game + ".game.json"
    data = json.load(io.open(path, encoding="utf-8"))
    if not data["label"].startswith("SUPERSEDED"):
        data["label"] = "SUPERSEDED by the copy whose id ends in c: " + data["label"]
        with io.open(path, "w", encoding="utf-8", newline="") as out:
            json.dump(data, out, indent=2)
            out.write("\n")
        print("superseded", game)
