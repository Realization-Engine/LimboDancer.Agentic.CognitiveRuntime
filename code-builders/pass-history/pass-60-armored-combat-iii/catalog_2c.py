"""Prints the catalog entries of the vehicles, Guns, and mortars increment 2c tests with (pass 60)."""
import json

WANTED = {"attacker-truck", "attacker-tank", "attacker-halftrack", "defender-truck", "defender-tank", "attacker-inf-gun",
          "defender-light-mortar", "attacker-light-mortar", "defender-at-gun"}

with open("src/ASL/units/catalog/scenario-a1.catalog.json", encoding="utf-8") as handle:
    catalog = json.load(handle)


def walk(node):
    if isinstance(node, dict):
        if node.get("id") in WANTED:
            print(node["id"], json.dumps(node)[:1200])
            print()
        for value in node.values():
            walk(value)
    elif isinstance(node, list):
        for value in node:
            walk(value)


walk(catalog)
