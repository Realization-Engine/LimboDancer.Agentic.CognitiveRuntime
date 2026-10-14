"""Drop the blank line the 45.9 edit left before a closing brace."""
PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1FireMapRules.cs"
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
old = "(B16.32; ruling R10.1)\", false);\n            }\n\n        }\n"
assert text.count(old) == 1, text.count(old)
text = text.replace(old, "(B16.32; ruling R10.1)\", false);\n            }\n        }\n")
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("ok")
