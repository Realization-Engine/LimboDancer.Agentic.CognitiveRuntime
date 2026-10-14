"""The Dash Play test's control gets its own firer (A8.3 bars the first firer's second shot). Exact blocks, asserted once each."""
path = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass10Tests.cs"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    ('        await Setup(Squad("g1", L("D5"), "german"), Squad("g2", L("E5"), "german"), Squad("r1", L("B3"), "russian"));\n        var before = Revision;\n        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("D4"), ["dash"] = L("D3") }));',
     '        await Setup(Squad("g1", L("D5"), "german"), Squad("g2", L("E5"), "german"), Squad("r1", L("B3"), "russian"), Squad("r2", L("G3"), "russian"));\n        var before = Revision;\n        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("D4"), ["dash"] = L("D3") }));'),
    ('        Committed(await Fire(R1, L("E4"), false, 6, 6, 6, 6, 6, 6));\n        Assert.DoesNotContain("dash", Multipliers(LastFire(before)));',
     '        Committed(await Fire(R2, L("E4"), false, 6, 6, 6, 6, 6, 6));\n        Assert.DoesNotContain("dash", Multipliers(LastFire(before)));'),
]
for old, new in edits:
    assert text.count(old) == 1, old[:60]
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
