"""The squads whose Spraying Fire trait is present, with their FP and range, and a few without."""
import json

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/units/catalog/scenario-a1.catalog.json"
defs = json.load(open(PATH, encoding="utf-8"))["definitions"]
for d in defs:
    if d["kind"] != "asl:squad":
        continue
    values = {v.get("attribute") or v.get("trait"): v.get("value", v.get("present")) for v in d["values"] if v.get("face") == "front"}
    print(d["id"], "spray=%s fp=%s range=%s" % (values.get("asl:spraying-fire"), values.get("firepower"), values.get("range")))
