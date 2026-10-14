"""Pass 40 task 40.9: two faults of the new tests' own (an area's argument order; a constant array)."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs", [
    ('[new("a", "hex-numbers", "bd04", 8, 10, null, null, null, null, null)]', '[new("a", "hex-numbers", null, "bd04", 8, 10, null, null, null, null, null)]'),
    ('[new("b", "hex-numbers", "bd02", 1, 3, null, null, null, null, null)]', '[new("b", "hex-numbers", null, "bd02", 1, 3, null, null, null, null, null)]'),
])
apply("LimboDancer.Domains.Asl.Play.Tests/BacklogPass13Tests.cs", [
    ('        Assert.Contains(Current.Unit("g9")!.Custodian, new[] { "dep-r1-1", "dep-r1-2" });',
     '        Assert.StartsWith("dep-r1-", Current.Unit("g9")!.Custodian, StringComparison.Ordinal);'),
])
print("ok")
