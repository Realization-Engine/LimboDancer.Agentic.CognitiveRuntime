"""Increment 3j: list the catalog's leaders and the units the 3j tests use, with their nationality and morale (read only)."""
import json
import sys

path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/units/catalog/scenario-a1.catalog.json"
with open(path, encoding="utf-8") as handle:
    catalog = json.load(handle)
wanted = {"defender-squad", "attacker-squad", "attacker-truck", "attacker-tank", "defender-half-squad", "attacker-half-squad"}
for item in catalog["definitions"]:
    ident = item.get("id", "")
    if "leader" in ident or ident in wanted:
        print(ident, item.get("kind"), json.dumps(item)[:300])
sys.stdout.flush()
