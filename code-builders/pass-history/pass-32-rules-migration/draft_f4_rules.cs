
    /// <summary>A10.5: a leader wounded on the way routs on only with the MF a wounded SMC has; a step beyond them ends the rout.</summary>
    public static bool StepExceedsMf(int used, int cost, int halfMf) => used + cost > halfMf;

    /// <summary>A10.53: Interdiction as a unit enters an Open Ground hex without Low Crawl, once per hex; the Interdictor is read only then.</summary>
    public static string? InterdictionDue(bool lowCrawl, bool firstEntryOfHex, Func<string?> interdictor)
    {
        ArgumentNullException.ThrowIfNull(interdictor);
        return lowCrawl || !firstEntryOfHex ? null : interdictor();
    }

    /// <summary>A10.53: the Morale Level an Interdiction NMC is taken against; a unit the catalog does not know has none.</summary>
    public static int InterdictionMorale(int? brokenMorale) => brokenMorale ?? 0;

    /// <summary>A10.53, A10.31: what an Interdiction result does to the routing unit: it routs on, is pinned and routs no further, is Casualty Reduced, or is eliminated.</summary>
    public static InterdictionOutcome InterdictionEffect(string result) =>
        result == ScenarioA1ResultTables.InterdictionPassed ? InterdictionOutcome.Passed
            : result == ScenarioA1ResultTables.InterdictionPinned ? InterdictionOutcome.Pinned
            : result == ScenarioA1ResultTables.InterdictionReduced ? InterdictionOutcome.Reduced
            : InterdictionOutcome.Eliminated;

    /// <summary>A10.53: a HS left by Casualty Reduction, or a wounded SMC, routs on; an eliminated unit does not.</summary>
    public static bool RoutsOn(bool eliminated) => !eliminated;

    /// <summary>
    /// The plan's words (A10.52, A10.53, A10.4, A4.431): the steps and their costs, which steps an enemy unit could Interdict, and what is left behind.
    /// </summary>
    public static string[] RoutSummary(string unitId, bool lowCrawl, IReadOnlyList<(string Step, int HalfMf)> steps, IReadOnlyList<(string Step, string By)> threatened,
        IReadOnlyList<(string Weapon, int Pp)> left, IReadOnlyList<string> kept, string start)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(threatened);
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(kept);
        var route = string.Join(", ", steps.Select(item => $"{item.Step} ({(item.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture)} MF)"));
        string[] threats = [.. threatened.Select(item => $"{item.Step} by {item.By}")];
        var interdiction = lowCrawl ? "Low Crawl is never Interdicted (A10.52)"
            : threats.Length == 0 ? "no step enters Open Ground an enemy unit could Interdict (A10.53)"
            : $"Interdicted as it enters {string.Join(", ", threats)}, a NMC each (A10.53)";
        string[] leaves = left.Count == 0 ? []
            : [$"play.rout-leaves: {unitId} leaves {string.Join(" and ", left.Select(item => $"{item.Weapon} ({item.Pp} PP)"))} in {start}, unpossessed, and routs with "
                + $"{(kept.Count == 0 ? "no SW" : string.Join(" and ", kept))} (A10.4, A4.431)"];
        return [$"play.rout: {unitId} routs{(lowCrawl ? " by Low Crawl" : "")} to {route}; {interdiction}", .. leaves];
    }

    /// <summary>E1.54 (backlog pass 16, ruling R16.6): no unit is eliminated for Failure to Rout at night.</summary>
    public static bool NoFailureToRout(bool night) => night;

    /// <summary>A10.5, A10.53: Failure to Rout falls on a broken Personnel unit not in Melee, not a prisoner, and not aboard a vehicle.</summary>
    public static bool FailureToRoutCandidate(bool active, bool broken, bool melee, bool captured, bool vehicle, bool aboard) =>
        active && broken && !melee && !captured && !vehicle && !aboard;

    /// <summary>
    /// Why the end of the RtPh eliminates a broken unit for Failure to Rout (A10.5, A10.53; ruling R13.3), or null: ADJACENT to or in the Location of a
    /// Known unbroken armed enemy unit; or, having not routed and not pinned, in Open Ground in the LOS and Normal Range of a Known enemy unit.
    /// </summary>
    public static string? FailureToRoutWhy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, bool routedThisPhase, bool pinned, int? scenarioMonth) =>
        NearUnbrokenArmedEnemy(reader, enemies, at) is { } enemy ? $"it is ADJACENT to or in the Location of the Known unbroken armed enemy unit {enemy}"
            : !routedThisPhase && !pinned && ExposedInOpenGround(reader, enemies, at, scenarioMonth) is { } seen
                ? $"it did not rout from Open Ground in the LOS and Normal Range of {seen}" : null;

    /// <summary>A20.21: a unit with captors surrenders instead of failing to rout, unless Fanatic, under No Quarter, or already rejected this phase.</summary>
    public static bool SurrendersInstead(bool fanatic, bool noQuarter, bool rejected) => !fanatic && !noQuarter && !rejected;

    /// <summary>A20.551 (ruling R31.8): a SMC that is free (no Custodian, not captured) and still Unarmed is Armed again.</summary>
    public static bool FreedUnarmedSmc(bool active, bool noCustodian, bool smc, bool unarmed, bool captured) => active && noCustodian && smc && unarmed && !captured;

    /// <summary>A20.551 (ruling R31.8): a plan's events are followed by the arming, unless there are none or the last ends the game or awaits a choice or a surrender.</summary>
    public static bool ArmsFreedSmc(int eventCount, bool lastEndsOrWaits) => eventCount > 0 && !lastEndsOrWaits;

    /// <summary>A20.551 (ruling R31.8): why the freed SMC are Armed again.</summary>
    public static string SmcArmedReason(IReadOnlyList<string> freed)
    {
        ArgumentNullException.ThrowIfNull(freed);
        return $"play.smc-armed: {string.Join(", ", freed)} {(freed.Count == 1 ? "is" : "are")} free and so Armed again (A20.551; ruling R31.8)";
    }
}
