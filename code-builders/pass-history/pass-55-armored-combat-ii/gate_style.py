# The gate's strict build, pass 55: the ten analyzer errors that are not whitespace. Every anchor is checked before any file is written.
T = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"
RULES = T + "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass55RulesTests.cs"
PLAY = T + "LimboDancer.Domains.Asl.Play.Tests/BacklogPass55Tests.cs"
PAGE = T + "LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageVehicleTests.cs"

EDITS = {
    RULES: [
        ('    private static readonly string[] Types = ["ap", "apcr", "apds", "heat", "he", "canister"];\n',
         '    private static readonly string[] Types = ["ap", "apcr", "apds", "heat", "he", "canister"];\n'
         '    private static readonly string[] FireCounterNames = ["First Fire", "Final Fire", "Intensive Fire"];\n'
         '    private static readonly int[] HalfPointCases = [0, 1, 2, 3, 4, 12, 40];\n'
         '    private static readonly string[] DrmCalculatorFiles = ["ScenarioA1OrdnanceCalculator.cs", "ScenarioA1ArmorCalculator.cs", "ScenarioA1AreaCalculator.cs"];\n'
         '    private static readonly string[] ArmorPiercingKinds = ["ap", "apds"];\n'
         '    private static readonly string[] BackblastWeapons = ["pf", "psk"];\n'),
        ('Assert.All(new[] { "First Fire", "Final Fire", "Intensive Fire" }, counter =>', 'Assert.All(FireCounterNames, counter =>'),
        ('new[] { 0, 1, 2, 3, 4, 12, 40 }.Select(', 'HalfPointCases.Select('),
        ('new[] { "ScenarioA1OrdnanceCalculator.cs", "ScenarioA1ArmorCalculator.cs", "ScenarioA1AreaCalculator.cs" }.SelectMany(', 'DrmCalculatorFiles.SelectMany('),
        ('Assert.Empty(given.Where(name => !listed.Contains(name) && !NotToHitDrm.Contains(name)));',
         'Assert.DoesNotContain(given, name => !listed.Contains(name) && !NotToHitDrm.Contains(name));'),
        ('Assert.All(new[] { "ap", "apds" }, named =>', 'Assert.All(ArmorPiercingKinds, named =>'),
        ('Assert.All(new[] { "pf", "psk" }, weapon =>', 'Assert.All(BackblastWeapons, weapon =>'),
    ],
    PLAY: [
        ('    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f555");\n',
         '    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f555");\n'
         '    private static readonly string[] VoidCauses = ["shocked", "eliminated", "other"];\n'),
        ('Assert.All(new[] { "shocked", "eliminated", "other" }, cause =>', 'Assert.All(VoidCauses, cause =>'),
        ('    private async Task<JsonElement> Probe(string tank)', '    private static async Task<JsonElement> Probe(string tank)'),
    ],
    PAGE: [
        ('Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(made.TextContent, "R1").Count);',
         'Assert.Single(System.Text.RegularExpressions.Regex.Matches(made.TextContent, "R1"));'),
    ],
}
NEW_NAMES = {RULES: ["FireCounterNames", "HalfPointCases", "DrmCalculatorFiles", "ArmorPiercingKinds", "BackblastWeapons"], PLAY: ["VoidCauses"], PAGE: []}

out = {}
for path, edits in EDITS.items():
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r\n" not in text, path
    for name in NEW_NAMES[path]:
        assert name not in text, (path, name)
    for old, new in edits:
        assert text.count(old) == 1, (path, old, text.count(old))
        text = text.replace(old, new)
    out[path] = text
for path, text in out.items():
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", path.rsplit("/", 1)[1])
