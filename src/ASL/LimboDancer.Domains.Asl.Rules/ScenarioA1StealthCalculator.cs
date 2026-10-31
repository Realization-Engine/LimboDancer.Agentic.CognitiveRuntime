namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Stealth and Lax (A11.17, A11.18, p. 73; A25.64, p. 97; the A11.17 claim's design, section 4, ruling RA11S.1): whether a unit is Stealthy, Lax, or
/// neither for the Ambush status dr (A11.4), the Concealment Growth dr (A12.122), the Searching dr (A12.152), and the Search Casualties dr (A12.154), and
/// the drm it gives. "Stealthy unless Lax": Lax wins. The one place both are decided: the Ambush's lines forward here, and the Concealment, Search, and
/// Casualty drs take this status through their <c>Stealth</c> fact. Pure and static; an unknown fact never makes a unit Stealthy, and a status that
/// depends on one is null.
/// </summary>
public static class ScenarioA1StealthCalculator
{
    private const string Squad = "asl:squad";
    private const string HalfSquad = "asl:half-squad";
    private const string InfantryCrew = "asl:crew";
    private const string VehicleCrew = "asl:vehicle-crew";
    private const string Leader = "asl:leader";
    private const string Hero = "asl:hero";

    /// <summary>A11.17 and A11.18 (p. 73): a current-reading modifier cannot be resolved from an unknown contributing status.</summary>
    public static string? FactsRefusal(IEnumerable<StealthStatus?> statuses)
    {
        ArgumentNullException.ThrowIfNull(statuses);
        return statuses.Any(status => status is null)
            ? "play.stealth-facts: the facts needed to resolve Stealth or Lax are unknown (A11.17, A11.18)"
            : null;
    }

    /// <summary>
    /// A11.17 and A11.18 (p. 73): an explicit side-wide SSR token, <c>stealthy:&lt;side&gt;</c> or <c>lax:&lt;side&gt;</c>; no inference from a card's prose.
    /// A token that names an OB group is not side-wide and answers null here; <see cref="SsrScope"/> reads both forms, and <see cref="SsrFlags"/> folds them.
    /// </summary>
    public static (string Side, bool Lax)? SsrToken(string token) => SsrScope(token) is { Group: null } scope ? (scope.Side, scope.Lax) : null;

    /// <summary>
    /// A11.17 and A11.18 (p. 73: "Good Order troops", "certain units"; ruling RA11S.11): a Stealth or Lax SSR token naming a side, <c>stealthy:&lt;side&gt;</c>
    /// or <c>lax:&lt;side&gt;</c>, or one of its OB groups, <c>stealthy:&lt;side&gt;:&lt;group&gt;</c> or <c>lax:&lt;side&gt;:&lt;group&gt;</c>, as the Fanatic
    /// token names them (ruling R70.15). Null when the token is neither.
    /// </summary>
    public static (string Side, string? Group, bool Lax)? SsrScope(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Split(':') switch
        {
            ["stealthy", { Length: > 0 } side] => (side, null, false),
            ["lax", { Length: > 0 } side] => (side, null, true),
            ["stealthy", { Length: > 0 } side, { Length: > 0 } group] => (side, group, false),
            ["lax", { Length: > 0 } side, { Length: > 0 } group] => (side, group, true),
            _ => null,
        };
    }

    /// <summary>
    /// The SSR facts of one unit (A11.17, A11.18, p. 73; ruling RA11S.11): whether a <c>stealthy:</c> and whether a <c>lax:</c> token names its side, either
    /// side-wide or by its OB group. These are <see cref="StealthFacts.SsrStealthy"/> and <see cref="StealthFacts.SsrLax"/>; Lax still wins in
    /// <see cref="Status"/>. Tokens that are not Stealth or Lax SSRs are ignored; <see cref="SsrRulesBar"/> refuses malformed ones at setup.
    /// </summary>
    public static (bool Stealthy, bool Lax) SsrFlags(IReadOnlyList<string> rules, string side, string? group)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(side);
        var stealthy = false;
        var lax = false;
        foreach (var rule in rules)
        {
            if (SsrScope(rule) is { } scope && scope.Side == side && (scope.Group is null || scope.Group == group))
            {
                lax |= scope.Lax;
                stealthy |= !scope.Lax;
            }
        }

        return (stealthy, lax);
    }

    /// <summary>A11.17 and A11.18 (p. 73): Stealth/Lax SSRs name a side or one of its OB groups; when supplied, the scenario's sides bound that name.</summary>
    public static string? SsrRulesBar(IReadOnlyList<string> rules, IReadOnlyCollection<string>? sides = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var rule in rules)
        {
            if (rule is not ("stealthy" or "lax") && !rule.StartsWith("stealthy:", StringComparison.Ordinal) && !rule.StartsWith("lax:", StringComparison.Ordinal))
            {
                continue;
            }

            if (SsrScope(rule) is not { } token)
            {
                return $"play.stealth-rule: '{rule}' is not stealthy:<side>, lax:<side>, stealthy:<side>:<group>, or lax:<side>:<group> (A11.17, A11.18)";
            }

            if (sides is not null && !sides.Contains(token.Side, StringComparer.Ordinal))
            {
                return $"play.stealth-rule: '{token.Side}' is not a side of this scenario (A11.17, A11.18)";
            }
        }

        return null;
    }

    /// <summary>
    /// The designation tokens, in <see cref="StealthDesignation"/> order (index i is the enum value i): the one list that <see cref="Designation"/> reads and
    /// the contract's <c>designation</c> choices are.
    /// </summary>
    public static readonly IReadOnlyList<string> DesignationTokens = ["none", "anzac", "gurkha", "commando", "partisan", "raider"];

    /// <summary>A11.17 (p. 73): the explicit force designation on an OB group; absent means none, an unknown token is not a known designation.</summary>
    public static StealthDesignation? Designation(string? token)
    {
        if (token is null)
        {
            return StealthDesignation.None;
        }

        for (var i = 0; i < DesignationTokens.Count; i++)
        {
            if (string.Equals(DesignationTokens[i], token, StringComparison.Ordinal))
            {
                return (StealthDesignation)i;
            }
        }

        return null;
    }

    /// <summary>The Ambush drm of a side with a Lax unit, once (A11.4, A11.18), as the Ambush has always written it.</summary>
    public static readonly FireModifier AmbushLax = new("lax", 1m, "A11.18");

    /// <summary>The Ambush drm of a side with a Stealthy unit, once (A11.4, A11.17), as the Ambush has always written it.</summary>
    public static readonly FireModifier AmbushStealthy = new("stealthy", -1m, "A11.17");

    /// <summary>A19.2 (p. 86): Green and Conscript troops never use Stealth, "even a Green squad accompanied by a leader".</summary>
    public static bool StealthBarredByClass(string? @class) => @class is "green" or "conscript";

    /// <summary>Whether the kind is Personnel: a squad, HS, crew, leader, or hero. Only Personnel are Lax (A11.18: "Inexperienced Personnel").</summary>
    public static bool Personnel(string? kind) => kind is Squad or HalfSquad or InfantryCrew or VehicleCrew or Leader or Hero;

    /// <summary>Whether the kind is a MMC: a squad, a HS, or a crew (A1.12).</summary>
    public static bool Mmc(string? kind) => kind is Squad or HalfSquad or InfantryCrew or VehicleCrew;

    /// <summary>
    /// Whether an Infantry MMC is Elite or 1st Line (A11.17, A25.7 Finns; G1.6 Japanese "including crews"): an Infantry crew is always Elite (A1.123,
    /// p. 44), a vehicle crew never (A1.123, D5.1, ruling RA11S.6); a squad or HS by its class, null when the class is unknown. False for an SMC, which has
    /// no class (A1.25; ruling RA11S.2).
    /// </summary>
    public static bool? EliteOrFirstLine(string? kind, string? @class) => kind switch
    {
        InfantryCrew => true,
        Squad or HalfSquad => @class is null ? null : @class is "elite" or "1st-line",
        _ => false,
    };

    /// <summary>
    /// Whether a unit is Stealthy by what it is, before Good Order and Lax: never a Green or Conscript unit (A19.2); a hero (A15.24, p. 83) or a heroic
    /// leader (A15.21); a unit of an SSR (A11.17); a unit of an ANZAC, Gurkha, or Partisan force (A11.17, A25.44, A25.43, A25.24); a squad or HS of a
    /// Commando or Raider force (H1.24, p. 329: "A squad designated as a Commando"; G17.111, p. 319: "Raider MMC are Stealthy" of the Raider squads/HS;
    /// ruling RA11S.2); an Elite or 1st Line Finnish (A11.17, A25.7, p. 97) or Japanese (G1.6, p. 280; ruling RA11S.3) MMC. Null when a squad's or HS's
    /// class is unknown and decides it.
    /// </summary>
    public static bool? StealthyByNature(StealthFacts unit)
    {
        // Only Infantry Personnel are Stealthy: never a vehicle, a Dummy, or a vehicle crew (ruling RA11S.6), whatever the SSR or designation.
        if (!Personnel(unit.Kind) || unit.Kind == VehicleCrew || StealthBarredByClass(unit.Class))
        {
            return false;
        }

        if (unit.Kind == Hero || (unit.Kind == Leader && unit.Heroic))
        {
            return true;
        }

        var squadOrHalf = unit.Kind is Squad or HalfSquad;
        var classUnknown = unit.Class is null && squadOrHalf;
        if (unit.SsrStealthy || DesignationStealthy(unit.Designation, squadOrHalf))
        {
            return classUnknown ? null : true;
        }

        return unit.Nationality is "finnish" or "japanese" ? EliteOrFirstLine(unit.Kind, unit.Class) : false;
    }

    /// <summary>
    /// Whether a unit is Lax: Personnel only; by SSR (A11.18); for this Player Turn by a Banzai Charge (G1.6) or a Human Wave (A25.23); berserk in CC
    /// (A15.432, p. 84); Inexperienced (A19.36, p. 86); a non-elite Italian MMC (A25.64, p. 97; ruling RA11S.9: an Italian Infantry crew is Elite, a
    /// leader or hero has no class); a Japanese Conscript (G1.6). Null when Inexperience or an Italian MMC's class is unknown and decides it; a
    /// Conscript MMC is always Inexperienced (A19.3).
    /// </summary>
    public static bool? Lax(StealthFacts unit, StealthPurpose purpose)
    {
        // A15.432: a berserk unit is Lax in CC whatever its kind, as the Ambush's old Lax line reads it (the build review, finding 1).
        if (unit.Berserk && purpose == StealthPurpose.Ambush)
        {
            return true;
        }

        if (!Personnel(unit.Kind))
        {
            return false;
        }

        if (unit.SsrLax || unit.BanzaiOrHumanWave || unit.Inexperienced == true)
        {
            return true;
        }

        var squadOrHalf = unit.Kind is Squad or HalfSquad;
        if (unit.Nationality == "italian" && (unit.Kind == VehicleCrew || (squadOrHalf && unit.Class is not null and not "elite")))
        {
            return true;
        }

        if (squadOrHalf && unit.Class == "conscript")
        {
            return true;
        }

        if (squadOrHalf && unit.Class is null && unit.Nationality == "italian")
        {
            return null;
        }

        return unit.Inexperienced is null && squadOrHalf && unit.Class is null or "green" ? null : false;
    }

    /// <summary>
    /// The unit's status for a dr (A11.17, A11.18): Lax when Lax ("Stealthy unless Lax"); else Stealthy when in Good Order ("Such Good Order troops";
    /// ruling RA11S.5) and Stealthy by what it is; else Normal. Null when an unknown fact decides it.
    /// </summary>
    public static StealthStatus? Status(StealthFacts unit, StealthPurpose purpose)
    {
        switch (Lax(unit, purpose))
        {
            case null:
                return null;
            case true:
                return StealthStatus.Lax;
        }

        if (!unit.GoodOrder)
        {
            return StealthStatus.Normal;
        }

        return StealthyByNature(unit) switch
        {
            true => StealthStatus.Stealthy,
            false => StealthStatus.Normal,
            null => null,
        };
    }

    /// <summary>A status's drm (A11.17, A11.18): -1 Stealthy, +1 Lax, 0 otherwise.</summary>
    public static int Drm(StealthStatus status) => status switch
    {
        StealthStatus.Stealthy => -1,
        StealthStatus.Lax => 1,
        _ => 0,
    };

    /// <summary>
    /// Inexperience as the Lax test reads it (ruling RA11S.10): what <see cref="ScenarioA1Experience.Inexperienced"/> answers (A19.2, A19.3), and always
    /// when Unarmed (A19.3: the restrictions "always apply to Conscripts/Unarmed-units"). Null when unknown; false when inapplicable (not a MMC).
    /// </summary>
    public static bool? Inexperienced(RuleState experience, bool unarmed) => unarmed ? true : experience switch
    {
        RuleState.True => true,
        RuleState.Unknown => null,
        _ => false,
    };

    /// <summary>The Concealment Growth dr's drm of a unit (A12.122, p. 77; A11.17, A11.18), or null when its status is unknown.</summary>
    public static int? ConcealmentDrm(StealthFacts unit) => Status(unit, StealthPurpose.Concealment) is { } status ? Drm(status) : null;

    /// <summary>
    /// The number of Stealthy units among those whose dr counts each one, -1 apiece: the searchers (A12.152) or the concealed defenders (A12.154). Null
    /// when any status is unknown.
    /// </summary>
    public static int? PerUnitStealthy(IReadOnlyList<StealthFacts> units, StealthPurpose purpose) => Count(units, purpose, StealthStatus.Stealthy);

    /// <summary>The number of Lax units among those whose dr counts each one, +1 apiece (A12.152, A12.154). Null when any status is unknown.</summary>
    public static int? PerUnitLax(IReadOnlyList<StealthFacts> units, StealthPurpose purpose) => Count(units, purpose, StealthStatus.Lax);

    /// <summary>
    /// A side's Stealth and Lax Ambush drm (A11.4, p. 73: each cause once, "even if only a portion of a player's CC force is qualified"; rulings R14.13,
    /// R29.9): +1 Lax when any unit is Lax, then -1 Stealthy when any is Stealthy, the names, values, and citations the Ambush writes. Null when any
    /// unit's status is unknown.
    /// </summary>
    public static IReadOnlyList<FireModifier>? AmbushDrm(IReadOnlyList<StealthFacts> force)
    {
        ArgumentNullException.ThrowIfNull(force);
        var lax = false;
        var stealthy = false;
        foreach (var unit in force)
        {
            switch (Status(unit, StealthPurpose.Ambush))
            {
                case null:
                    return null;
                case StealthStatus.Lax:
                    lax = true;
                    break;
                case StealthStatus.Stealthy:
                    stealthy = true;
                    break;
            }
        }

        return (lax, stealthy) switch
        {
            (true, true) => [AmbushLax, AmbushStealthy],
            (true, false) => [AmbushLax],
            (false, true) => [AmbushStealthy],
            _ => [],
        };
    }

    // RA11S.2: ANZAC, Gurkha, and Partisan name a force, its leaders included; Commando (H1.24) and Raider (G17.111) name squads and HS only.
    private static bool DesignationStealthy(StealthDesignation designation, bool squadOrHalf) => designation switch
    {
        StealthDesignation.None => false,
        StealthDesignation.Commando or StealthDesignation.Raider => squadOrHalf,
        _ => true,
    };

    private static int? Count(IReadOnlyList<StealthFacts> units, StealthPurpose purpose, StealthStatus wanted)
    {
        ArgumentNullException.ThrowIfNull(units);
        var count = 0;
        foreach (var unit in units)
        {
            if (Status(unit, purpose) is not { } status)
            {
                return null;
            }

            count += status == wanted ? 1 : 0;
        }

        return count;
    }
}
