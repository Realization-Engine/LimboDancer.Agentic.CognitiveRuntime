import json, sys
p = r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\units\catalog\scenario-a1.catalog.json"
d = json.load(open(p, encoding="utf-8"))
print({k: (v if not isinstance(v, list) else len(v)) for k, v in d.items() if k != "definitions"})
want = sys.argv[1:]
for x in d["definitions"]:
    if x["id"] in want:
        print("==", x["id"], x["kind"], x.get("nationality"), x["counter"])
        for v in x["values"]:
            if "attribute" not in v:
                print("  ", v)
                continue
            val = v.get("value", "printed:false" if v.get("printed") is False else ("recorded:false" if v.get("recorded") is False else "?"))
            extra = {k: v[k] for k in v if k not in ("face", "attribute", "value", "row", "printed", "recorded")}
            print("  ", v["face"], v["attribute"], "=", val, extra if extra else "")
if "--ids" in want:
    for x in d["definitions"]:
        if x["kind"] in ("asl:gun", "asl:vehicle") or "crew" in x["id"]:
            print(x["id"], x["kind"], x.get("nationality"))
