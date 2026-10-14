"""Pass 45, task 45.6: two name clashes and two one-line `with` blocks the formatter refuses."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs": [
        ("weapon.DefinitionId is { } named ? FireReference.Value.Definitions.GetValueOrDefault(named)?.Size : null,",
         "weapon.DefinitionId is { } mgNamed ? FireReference.Value.Definitions.GetValueOrDefault(mgNamed)?.Size : null,"),
        ("if (attack.Director is { UnitId: { } directing } && attack.Firers!.All(item => item.UsesInherentFp == false && item.Weapons is { Count: > 0 }))",
         "if (attack.Director is { UnitId: { } mgDirector } && attack.Firers!.All(item => item.UsesInherentFp == false && item.Weapons is { Count: > 0 }))"),
        ("OfType<FireResolved>().Any(record => record.Director == directing\n",
         "OfType<FireResolved>().Any(record => record.Director == mgDirector\n"),
        ("""                    Director = attack.Director with { DirectedTheseMgs = true },
""",
         """                    Director = attack.Director with
                    {
                        DirectedTheseMgs = true
                    },
"""),
    ],
    "LimboDancer.Domains.Asl.Play/LiveFire.cs": [
        ("""            Director = expected.Director is null || recorded.Director?.DirectedTheseMgs != true ? expected.Director : expected.Director with { DirectedTheseMgs = true },
""",
         """            Director = expected.Director is null || recorded.Director?.DirectedTheseMgs != true ? expected.Director : expected.Director with
            {
                DirectedTheseMgs = true
            },
"""),
    ],
}
texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
