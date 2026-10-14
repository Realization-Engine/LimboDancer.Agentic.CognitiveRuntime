"""Make the Canister page test read its aim from the spot the board gives (review fixes of pass 55)."""
import io

PATH = r"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageOrdnanceTests.cs"
text = io.open(PATH, encoding="utf-8", newline="").read()
swaps = [
    ('option => option.GetAttribute("value") == "4");',
     'option => option.GetAttribute("value") == aimId);'),
    ('Assert.StartsWith("aim 4: ", aim.TextContent, StringComparison.Ordinal);',
     'Assert.StartsWith($"aim {aimId}: ", aim.TextContent, StringComparison.Ordinal);'),
    ('page.Find("#ordnance-canister-aim").Change("4");',
     'page.Find("#ordnance-canister-aim").Change(aimId);'),
    ('Assert.Contains("fires Canister at aim 4 of the CA facing " + spot.Facing, proposal, StringComparison.Ordinal);',
     'Assert.Contains($"fires Canister at aim {aimId} of the CA facing {spot.Facing}", proposal, StringComparison.Ordinal);'),
    ('Assert.Equal(("canister", 4, spot.Facing),', 'Assert.Equal(("canister", spot.Aim, spot.Facing),'),
    ('$"fires Canister at aim 4 of the CA facing {spot.Facing}: no To Hit DR', '$"fires Canister at aim {aimId} of the CA facing {spot.Facing}: no To Hit DR'),
    ('        var spot = CanisterSpot();\n        var page = context.Render<PlayPage>();\n        MinimalCards.Choose(page, new Dictionary<string, string>\n        {\n            ["board"] = Board, ["first"] = "american"',
     '        var spot = CanisterSpot();\n        var aimId = spot.Aim.ToString(System.Globalization.CultureInfo.InvariantCulture);\n        var page = context.Render<PlayPage>();\n        MinimalCards.Choose(page, new Dictionary<string, string>\n        {\n            ["board"] = Board, ["first"] = "american"'),
]
for old, new in swaps:
    count = text.count(old)
    print(count, old[:60].replace("\n", " "))
    if count != 1:
        raise SystemExit("not exactly one: " + old[:60])
    text = text.replace(old, new)
io.open(PATH, "w", encoding="utf-8", newline="").write(text)
