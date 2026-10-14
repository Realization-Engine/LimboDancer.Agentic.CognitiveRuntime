"""Ruling R55.29 in the Gun Duel page test: a squad mans the Gun (Case H), and the unrevealed Armor Leader's line is shown nowhere. LF kept."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageVehicleTests.cs"
with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
    text = handle.read()
PAIRS = [
    ("        // Pass 55 (C2.2401; rulings R55.19, R55.20): the AT Gun in the open, its barrel turned away from the ADJACENT tank, must turn two\n"
     "        // hexspines, Case A +4; the PzKpfw IIIH that has just spent its Start MP would fire with Case C +4, Case I +1, and its Armor Leader's -2: +3. The Russian shot is declared and\n"
     "        // not rolled; the German view is offered the duel, and its answer resolves both shots in order.\n",
     "        // Pass 55 (C2.2401; rulings R55.19, R55.20, R55.29): the AT Gun in the open, manned by a squad and turned away from the ADJACENT tank,\n"
     "        // must turn two hexspines: Case A +4 and Case H +2. The PzKpfw IIIH that has just spent its Start MP would fire with Case C +4 and\n"
     "        // Case I +1; its Armor Leader 9-2 is not yet revealed (D3.41), so he is not counted and his line is shown nowhere. The Russian shot is\n"
     "        // declared and not rolled; the German view is offered the duel, and its answer resolves both shots in order.\n"),
    ('        page.Find("#place-definition").Change("defender-crew");\n        page.Find("#place-id").Change("ru-crew");\n        page.Find("#place-location").Change(east);',
     '        page.Find("#place-definition").Change("defender-squad");\n        page.Find("#place-id").Change("ru-crew");\n        page.Find("#place-location").Change(east);'),
    ('        Assert.Contains("no dice are rolled until it answers", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);\n',
     '        Assert.Contains("no dice are rolled until it answers", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);\n'
     '        Assert.DoesNotContain("Armor Leader", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);\n'),
    ('        Assert.Contains("+3 (Case C +4, Case I +1, Armor Leader -2)", waiting.TextContent, StringComparison.Ordinal);\n'
     '        Assert.Contains("+4 (Case A +4)", waiting.TextContent, StringComparison.Ordinal);\n',
     '        Assert.Contains("+5 (Case C +4, Case I +1)", waiting.TextContent, StringComparison.Ordinal);\n'
     '        Assert.Contains("+6 (Case A +4, Case H +2)", waiting.TextContent, StringComparison.Ordinal);\n'
     '        Assert.DoesNotContain("Armor Leader", waiting.TextContent, StringComparison.Ordinal);\n'),
]
for old, new in PAIRS:
    assert text.count(old) == 1, old[:60]
    text = text.replace(old, new)
with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(text)
print("ok")
