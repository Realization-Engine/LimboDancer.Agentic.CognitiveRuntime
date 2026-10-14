"""Pass 40 task 40.4: the three Play tests' own faults (a hex not adjacent, a DR 2 that draws a further roll, an empty hex wrongly barred)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass27Tests.cs"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ("Unit(\"l1\", \"asl:leader\", \"attacker-leader-8-0\", L(\"C5\"), \"german\"), Squad(\"g2\", L(\"D5\"), \"german\"),",
     "Unit(\"l1\", \"asl:leader\", \"attacker-leader-8-0\", L(\"C5\"), \"german\"), Squad(\"g2\", L(\"D4\"), \"german\"),"),
    ("        Committed(await Move(G2, L(\"D4\")));\n        NoReplayErrors();", "        Committed(await Move(G2, L(\"D5\")));\n        NoReplayErrors();"),
    ("        Committed(await Do(GameActions.CloseCombat, Once(1, 1, 6, 6), new\n        {\n            location = L(\"C4\"),",
     "        Committed(await Do(GameActions.CloseCombat, Once(1, 2, 6, 6), new\n        {\n            location = L(\"C4\"),"),
    ("        Assert.DoesNotContain(L(\"C5\"), choice.Options);", "        Assert.DoesNotContain(L(\"C4\"), choice.Options);\n        Assert.Contains(L(\"C5\"), choice.Options);"),
]
for old, new in edits:
    assert text.count(old) == 1, (old[:60], text.count(old))
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
