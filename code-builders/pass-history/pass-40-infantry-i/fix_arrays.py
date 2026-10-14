"""CA1861 in LosTargetPointTests: the firers' arrays as static readonly fields (exact blocks, each asserted)."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/LosTargetPointTests.cs"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ('new[] { "G2", "J3", "K3", "L2" }', "AlongTheRoad", 1),
    ('new[] { "I5", "J4" }', "BehindTheWoods", 2),
    ('new[] { "H4", "I3" }', "BesideTheHexside", 2),
]
for old, name, count in edits:
    assert text.count(old) == count, old
    text = text.replace(old, name)
anchor = "    private static LosMap Board05()"
assert text.count(anchor) == 1
fields = ('    private static readonly string[] AlongTheRoad = ["G2", "J3", "K3", "L2"];\n'
          '    private static readonly string[] BehindTheWoods = ["I5", "J4"];\n'
          '    private static readonly string[] BesideTheHexside = ["H4", "I3"];\n\n')
text = text.replace(anchor, fields + anchor)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
