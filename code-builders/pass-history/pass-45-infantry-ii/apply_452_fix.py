"""Pass 45, task 45.2: the cower selection roll in the follow-ups' roll table, and four older tests whose dice now roll a Cowering."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PLAY = "tests/LimboDancer.Domains.Asl.Play.Tests/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs": [
        ("""        var selected = kind is "randomSelection" or "weaponSelection" or "firerSelection" ? unit.Split(',') : [];
""",
         """        var selected = kind is "randomSelection" or "weaponSelection" or "firerSelection" or "cowerSelection" ? unit.Split(',') : [];
"""),
        ("""            "firerSelection" => (selected.Length, "fire-firer-selection"),
            "checks" => (2, "fire-check"),
""",
         """            "firerSelection" => (selected.Length, "fire-firer-selection"),
            "cowerSelection" => (selected.Length, "fire-cower-selection"),
            "checks" => (2, "fire-check"),
"""),
    ],
    # Pass 45 (A7.9): doubles now mark the firer Final Fire, so these tests roll 11, not 12, where the Cowering is not their subject.
    PLAY + "BacklogPass10Tests.cs": [
        ("""        Committed(await Fire(R1, L("D4"), false, 6, 6, 6, 6, 6, 6));
        Assert.Contains(Drm(LastFire(before)), item => item.Name == "ffmo");
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "tem:woods");
        before = Revision;
        Committed(await Fire(R2, L("D4"), false, 6, 6, 6, 6, 6, 6));
""",
         """        Committed(await Fire(R1, L("D4"), false, 6, 5, 6, 6, 6, 6));
        Assert.Contains(Drm(LastFire(before)), item => item.Name == "ffmo");
        Assert.DoesNotContain(Drm(LastFire(before)), item => item.Name == "tem:woods");
        before = Revision;
        Committed(await Fire(R2, L("D4"), false, 6, 5, 6, 6, 6, 6));
"""),
    ],
    PLAY + "BacklogPass15Tests.cs": [
        ("""        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["ft"] },
            withoutInherent = G1,
            target = L("E4"),
        }));
        var arithmetic = Resolution(Fires(before).Single()).Arithmetic!;
        Assert.Equal(24m, arithmetic.TotalFirepower);
""",
         """        // Pass 45 (A7.9, A22.1): a FT's doubles cower, and the squad that cowers is marked with all its SW; a DR of 7 here, not doubles.
        Committed(await Do(GameActions.Fire, Then(3, 3, 4), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["ft"] },
            withoutInherent = G1,
            target = L("E4"),
        }));
        var arithmetic = Resolution(Fires(before).Single()).Arithmetic!;
        Assert.Equal(24m, arithmetic.TotalFirepower);
"""),
    ],
    PLAY + "BacklogPass15TablePlayerTests.cs": [
        ("""        var inherent = await Try("inherent", GameActions.Fire, Then(6), new
""",
         """        // Pass 45 (A7.9): doubles would mark the squad and its FT; a DR of 11.
        var inherent = await Try("inherent", GameActions.Fire, Then(6, 6, 5), new
"""),
        ("""        var dff = await Try("mol dff", GameActions.Fire, Then(5, 2), new
""",
         """        // Pass 45 (A7.9): the MOL Check dr of 2, then an IFT DR of 9 that is not doubles, which would mark the squad Final Fire.
        var dff = await Try("mol dff", GameActions.Fire, Then(5, 2, 4), new
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
