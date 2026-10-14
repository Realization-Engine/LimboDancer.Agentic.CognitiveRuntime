"""Reproduce source accounting. Run with --check to detect stale audit data."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / "prototypes/europe-hex-map/sources/situations/panzer-leader-05.json"
COMPOSITION = ROOT / "prototypes/europe-hex-map/tactical/source-composition.js"
OUTPUT = ROOT / "src/Campaign/docs/Goodwood Coverage Inventory.json"
# Verified mappings in FormationDeployment.Units. This is a baseline audit, not runtime admission.
READY = {"de-halftrack": "attacker-halftrack", "uk-rifle": "british-squad", "de-rifle": "attacker-squad",
         "uk-engineer": "british-elite-squad", "de-81mm": "campaign-german-81mm-mortar + attacker-crew"}

def inventory():
    source = json.loads(SOURCE.read_text(encoding="utf-8"))
    composition = COMPOSITION.read_text(encoding="utf-8")
    rows = json.JSONDecoder().raw_decode(composition.split("const rows=", 1)[1])[0]
    counts = Counter(i["counterTypeId"] for i in source["instances"])
    roster = []
    for counter in source["counters"]:
        key = counter["id"]
        assert counts[key] == counter["quantity"], key
        evidence = rows.get(key, {})
        roster.append({"type": key, "side": counter["side"], "sourceCount": counts[key],
                       "sourceIds": sorted(i["id"] for i in source["instances"] if i["counterTypeId"] == key),
                       "principalObjectsPerFormation": evidence.get("count"),
                       "compositionEvidence": evidence.get("composition"),
                       "pilotDefinition": READY.get(key),
                       "status": "selected-formation implemented" if key in READY else
                       "retain off-map air support" if key == "uk-typhoon-rockets" else "conversion required"})
    terrain = Counter(h["terrain"]["base"] for b in source["boards"] for h in b["hexes"])
    return {"sourceSha256Lf": hashlib.sha256(SOURCE.read_bytes().replace(b"\r\n", b"\n")).hexdigest(),
            "compositionSha256Lf": hashlib.sha256(COMPOSITION.read_bytes().replace(b"\r\n", b"\n")).hexdigest(),
            "sourceCounters": sum(counts.values()), "counterTypes": len(counts),
            "selectedPilotSourceCounters": sum(counts[k] for k in READY),
            "unconvertedGroundSourceCounters": sum(c for k,c in counts.items() if k not in READY and k != "uk-typhoon-rockets"),
            "offMapAircraft": counts["uk-typhoon-rockets"],
            "sourceHexFragments": sum(terrain.values()), "terrain": dict(sorted(terrain.items())), "roster": roster}

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    text = json.dumps(inventory(), indent=2, ensure_ascii=False) + "\n"
    if args.check:
        if OUTPUT.read_text(encoding="utf-8") != text:
            raise SystemExit("Goodwood coverage inventory is stale; regenerate and review it.")
    else:
        OUTPUT.write_text(text, encoding="utf-8", newline="\n")
    print("Goodwood coverage inventory verified.")
