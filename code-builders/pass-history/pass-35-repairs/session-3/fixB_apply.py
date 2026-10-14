"""Pass 35 review fixes 17, 21, 22, 23, 25, and two sentences of 26 (the table player's and the Rules boundary's findings on wording).
Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
RULES = "LimboDancer.Domains.Asl.Rules/"
STUDIO = "LimboDancer.Domains.Asl.MapStudio/"
EDITS = {
    RULES + "ScenarioA1SequenceCalculator.cs": [
        # Fix 21.
        ("""    /// were absent. <paramref name="kinds"/> are the labels of the unbuilt kinds, read when the game has events or is being set up.""",
         """    /// were absent. <paramref name="kinds"/> are the unbuilt kinds as the vocabulary names them, read when the game has events or is being set up."""),
        ("""                + $"{(setup ? "it is not set up with them" : "it is not played")} (B23.9, B24, B25.15, B26 to B30; passes 115 and 120)";""",
         """                + $"{(setup ? "it is not set up with them" : "it cannot be played")} (B23.9, B24, B25.15, B26 to B30)";"""),
        ("""{string.Join(", ", kinds)} counters, whose rules are not built, so \"""",
         """{string.Join(", ", kinds)} counters, whose rules are not built yet, so \""""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Acquisition.cs": [
        ("""    /// <summary>The labels of the kinds among <paramref name="kinds"/> whose rules are not built (pass 35, task 35.15), each once, in order; Rules says which.</summary>""",
         """    /// <summary>
    /// The kinds among <paramref name="kinds"/> whose rules are not built (pass 35, task 35.15), each once, in order, by the vocabulary's own
    /// label ("Wire", "Fortified Building Location"); Rules says which.
    /// </summary>"""),
        (""".Select(kind => kind.Replace("asl:", string.Empty, StringComparison.Ordinal)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];""",
         """.Select(kind => vocabulary.TryGetKind(kind, out var known) ? known.Label : kind).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];"""),
    ],
    RULES + "ScenarioA1Wounds.cs": [
        # Fix 22.
        ("""    /// <summary>What a record says of a Wound Severity dr.</summary>
    public static string SeverityText(int severityDr) => $"Wound Severity dr {severityDr}: a 5 or more is mortal, with +1 for a man already wounded (A17.11)";""",
         """    /// <summary>
    /// What a record says of a Wound Severity dr (A17.11): the man when the record knows him, the dr with the +1 of a man already wounded, the
    /// Final dr, and what it comes to.
    /// </summary>
    public static string SeverityText(int severityDr, string? unitId = null, bool alreadyWounded = false) =>
        $"Wound Severity dr {severityDr}{(alreadyWounded ? $" + 1 (already wounded) = {severityDr + 1}" : string.Empty)}{(unitId is null ? string.Empty : $" for {unitId}")}: "
            + $"{(Mortal(severityDr, alreadyWounded) ? "a mortal wound, eliminated" : "a wound, not mortal")}; a Final dr of 5 or more is mortal (A17.11)";"""),
    ],
    RULES + "ScenarioA1RoutCalculator.cs": [
        # Fix 26: two sentences.
        ("""        $"{unitId} does not rout: it {cause}, so it surrenders to {string.Join(" or ", captors)} as the RtPh ends (A20.21).";""",
         """        $"{unitId} does not rout: it {cause}, so it surrenders to {string.Join(" or ", captors)} as the RtPh ends ({(cause == "is Disrupted" ? "A19.12, " : string.Empty)}A20.21).";"""),
        ("""$"play.rout-low-crawl: {unitId} is Disrupted and may not use Low Crawl (A19.12)" : null;""",
         """$"play.rout-low-crawl: {unitId} is Disrupted and may not use Low Crawl; it routs normally if it must rout, and otherwise stays (A19.12)" : null;"""),
        # Fix 17.
        ("""    /// <summary>A20.21: the refusal of a rout by a unit that surrenders instead.</summary>""",
         """    /// <summary>
    /// What the Rout panel says of where a broken unit's rout may end (A10.5, A10.51; pass 35, the Rules boundary review): no legal step; places
    /// within reach that all lie where it may not end, so that the route passes through them; the places it must end in; or none within reach.
    /// The places come worded, each with the way to it.
    /// </summary>
    public static string RoutEndAdvice(bool canRout, bool must, IReadOnlyList<string> places, bool anyPlaceMayEndTheRout)
    {
        ArgumentNullException.ThrowIfNull(places);
        return !canRout
            ? must ? ". It has no legal rout step, so it is eliminated for Failure to Rout, or surrenders, when the RtPh ends (A10.5)." : ". It has no legal rout step, so it stays where it is."
            : places.Count > 0 && !anyPlaceMayEndTheRout
                ? $". It may not end its rout in or ADJACENT to the Location of the enemy unit it began with or beside (A10.5, A10.51): its route passes through {string.Join(" or ", places)} and ends in woods or a building beyond."
            : places.Count > 0 ? $". Its route must end in {string.Join(" or ", places)}."
            : ". No woods or building is within its reach, so any legal route will do.";
    }

    /// <summary>A20.21: the refusal of a rout by a unit that surrenders instead.</summary>"""),
    ],
    STUDIO + "Components/Pages/Play.razor": [
        ("""        return (!canRout
            ? must ? ". It has no legal rout step, so it is eliminated for Failure to Rout, or surrenders, when the RtPh ends (A10.5)." : ". It has no legal rout step, so it stays where it is."
            // Pass 35 (A.8, A10.51): every place within reach may lie ADJACENT to the enemy unit the rout began beside, where it may not end.
            : targets.Count > 0 && !targets.Any(routes.ContainsKey)
                ? $". It may not end its rout ADJACENT to the enemy unit it began beside (A10.51): its route passes through {string.Join(" or ", targets.Select(Place))} and ends in woods or a building beyond."
            : targets.Count > 0 ? $". Its route must end in {string.Join(" or ", targets.Select(Place))}."
            : ". No woods or building is within its reach, so any legal route will do.",
""",
         """        // Pass 35 (A.8, A10.51): every place within reach may lie where the rout may not end. Rules words which case it is.
        return (ScenarioA1RoutCalculator.RoutEndAdvice(canRout, must, [.. targets.Select(Place)], targets.Any(routes.ContainsKey)),
"""),
    ],
    STUDIO + "Services/PlayRecords.cs": [
        ("""                    records.Add((item.EventId, "wound", ScenarioA1Wounds.SeverityText(severity.Values[0])));""",
         """                    // The table player, pass 35: the man and the outcome are read from what the same action did next, a wound or an elimination.
                    var later = attempts[attempt].SkipWhile(other => other.EventId != item.EventId).Skip(1).Select(other => other.Payload);
                    var man = later.Select(payload => payload switch
                    {
                        InstanceEliminated gone => gone.Id,
                        ConditionsChanged changed when changed.Conditions.ContainsKey(Conditions.Wounded) => changed.Id,
                        _ => null,
                    }).FirstOrDefault(id => id is not null);
                    records.Add((item.EventId, "wound", ScenarioA1Wounds.SeverityText(severity.Values[0], man,
                        man is not null && before?.Unit(man) is { } hurt && GameState.Condition(hurt, Conditions.Wounded) == ConditionState.True)));"""),
    ],
    STUDIO + "Services/ReplaySteps.cs": [
        # Fix 25.
        ("""        if (First<RoutStepped>(payloads) is { } routed)
        {""",
         """        // The table player, pass 35: a rout repulsed after a step it made is titled by the repulse, not by its first step.
        if ((payloads.OfType<RoutStepped>().FirstOrDefault(step => step.Attempted is not null) ?? First<RoutStepped>(payloads)) is { } routed)
        {"""),
    ],
    STUDIO + "Services/DisplayText.cs": [
        # Fix 23.
        ("""        ["crew-exposed"] = "CE",
""",
         """        ["crew-exposed"] = "CE",
        ["stun-recovery"] = "Stun +1",
"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1RoutRallyRulesTests.cs": [
        ("""        Assert.Equal("play.rout-low-crawl: u is Disrupted and may not use Low Crawl (A19.12)", ScenarioA1RoutCalculator.DisruptedLowCrawlBar(""",
         """        Assert.Equal("play.rout-low-crawl: u is Disrupted and may not use Low Crawl; it routs normally if it must rout, and otherwise stays (A19.12)", ScenarioA1RoutCalculator.DisruptedLowCrawlBar("""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
