namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rulebook's definitions and tables that the game reads as pure functions (pass 32.a, slice S1: the plan's section 19, decision 7). Each
/// was moved from Play or Units as it stood; the old member stays as a one-line forward. A definition is a rule even where it is one line.
/// </summary>
public static class ScenarioA1Definitions
{
    /// <summary>The kinds that are SMC: leader and hero.</summary>
    public static IReadOnlyList<string> SmcKinds { get; } = ["asl:leader", "asl:hero"];

    /// <summary>The kinds that may set up hidden (A12.3; ruling R23.5).</summary>
    public static IReadOnlyList<string> HiddenSetupKinds { get; } = ["asl:squad", "asl:half-squad", "asl:crew", "asl:leader", "asl:hero"];

    /// <summary>The unit kinds that are MMC for gaining Control (A26.11).</summary>
    public static IReadOnlyList<string> MmcKinds { get; } = ["asl:squad", "asl:half-squad", "asl:crew"];

    /// <summary>The kinds a view shows as a counter of a stack before play (A2.9); SW and Guns lie beneath their units and are not seen before play.</summary>
    public static IReadOnlyList<string> StackCounterKinds { get; } = ["asl:squad", "asl:half-squad", "asl:crew", "asl:leader", "asl:hero", "asl:vehicle"];

    /// <summary>The Axis Minor nations (A25.8).</summary>
    public static IReadOnlyList<string> AxisMinorNations { get; } = ["romanian", "hungarian", "slovakian", "croatian", "bulgarian"];

    /// <summary>The Axis nationalities of the catalog, for Extreme Winter (E3.741, E3.742): the Finns are Axis too, but excepted there.</summary>
    public static IReadOnlyList<string> AxisNationalities { get; } = ["german", "italian", "finnish", "japanese", "axis-minor"];

    /// <summary>The phases of a Player Turn (A3.1 to A3.8, p. 47).</summary>
    public static IReadOnlyList<string> Phases { get; } = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];

    /// <summary>The weather an SSR may name (E3; ruling R16.9).</summary>
    public static IReadOnlyList<string> WeatherKinds
    {
        get;
    } =
        ["overcast", "gusty", "mist", "rain", "heavy-rain", "mud", "falling-snow", "ground-snow", "deep-snow", "extreme-winter"];

    /// <summary>The cloud covers a night-clouds SSR may name (E1.11).</summary>
    public static IReadOnlyList<string> Clouds { get; } = ["none", "scattered", "overcast"];

    /// <summary>The moon phases a night-moon SSR may name (E1.11).</summary>
    public static IReadOnlyList<string> Moons { get; } = ["none", "half", "full"];

    /// <summary>The Inexperienced classes (A19.2, p. 86): green and conscript.</summary>
    public static IReadOnlySet<string> InexperiencedClasses { get; } = new HashSet<string>(StringComparer.Ordinal) { "green", "conscript" };

    /// <summary>
    /// The VASL terrain names the Fire package's TEM admits (Terrain Chart p. 698; B1.1, B12, B13, B14, B15, B23), as the package's keys. A road hex is
    /// Open Ground apart from its road (B1.11, p. 113). Backlog pass 10 (ruling R10.1): marsh (B16) and rubble (B24).
    /// </summary>
    public static IReadOnlyDictionary<string, string> FireTerrain
    {
        get;
    } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Open Ground"] = "open-ground",
        ["Paved Road"] = "open-ground",
        ["Dirt Road"] = "open-ground",
        ["Brush"] = "brush",
        ["Woods"] = "woods",
        ["Orchard"] = "orchard",
        ["Grain"] = "grain",
        ["Marsh"] = "marsh",
        ["Wooden Rubble"] = "wooden-rubble",
        ["Stone Rubble"] = "stone-rubble",
    };

    /// <summary>The IFT results that are at least a NMC (A10.62).</summary>
    public static IReadOnlyList<string> AtLeastNmc
    {
        get;
    } =
        ["NMC", "1MC", "2MC", "3MC", "4MC", "K/1", "K/2", "K/3", "K/4", "1KIA", "2KIA", "3KIA", "4KIA", "5KIA", "6KIA", "7KIA"];

    /// <summary>
    /// The terrain names that count as woods, building, or rubble for a firing Gun: Case A and Case E doubled, Case B at +3, and the CA fixed after
    /// its first shot (C5.11, C5.2, C5.5; p. 172). Rubble joined the list in pass 35 (task 35.10); the fact keeps its recorded name.
    /// </summary>
    public static IReadOnlyList<string> WoodsOrBuilding { get; } = ["woods", "wooden-building", "stone-building", "wooden-rubble", "stone-rubble"];

    /// <summary>
    /// The terrain of an ordinary wooden or stone building (B23; the reviewed B. Terrain Chart supplement): the reviewed case covers its ground
    /// level whatever the building's height, so the multi-level names VASL boards use count too.
    /// </summary>
    public static IReadOnlySet<string> OrdinaryBuildings
    {
        get;
    } = new HashSet<string>(
        from material in new[] { "Wooden", "Stone" }
        from suffix in new[] { string.Empty, ", 1 Level", ", 2 Level", ", 3 Level", ", 4 Level" }
        select $"{material} Building{suffix}",
        StringComparer.Ordinal);

    /// <summary>Which terrain keys are buildings: wooden-building, stone-building.</summary>
    public static bool IsBuildingTerrain(string? key) => key is "wooden-building" or "stone-building";

    /// <summary>Which terrain keys are rubble: wooden-rubble, stone-rubble.</summary>
    public static bool IsRubbleTerrain(string? key) => key is "wooden-rubble" or "stone-rubble";

    /// <summary>
    /// Whether a terrain is Concealment Terrain (A12.12): grain only June to September. The concealment gain and the setup read the same list (both
    /// copies gave the same answer for every input, so they are one function here).
    /// </summary>
    public static bool IsConcealmentTerrain(string terrain, int? month) =>
        terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
        || (terrain == "grain" && month is >= 6 and <= 9);

    /// <summary>Whether the game is at night (E1.1; ruling R16.1): exactly when a Base NVR is recorded.</summary>
    public static bool IsNight(int? nvr) => nvr is not null;

    /// <summary>The precipitation an SSR names at the start (E3.51, E3.71; ruling R16.9), or null.</summary>
    public static string? PrecipitationRule(IReadOnlyList<string> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return rules.Contains("weather:heavy-rain", StringComparer.Ordinal) ? "heavy-rain"
            : rules.Contains("weather:rain", StringComparer.Ordinal) ? "rain"
            : rules.Contains("weather:falling-snow", StringComparer.Ordinal) ? "snow"
            : null;
    }

    /// <summary>
    /// Whether a nationality's squads may Deploy (A25.2; ruling R31.4): every nationality the game has but the Russian. One list for play and for
    /// setup. A Guard of prisoners (A20.5) and a temporary crew (A21.22) are the rule's exceptions and are not built.
    /// </summary>
    public static bool MayDeploy(string? nationality) => nationality != "russian";

    /// <summary>
    /// The squad-equivalents a side may set up hidden (A12.3; ruling R23.5): the largest n of its SSR tokens <c>hip:&lt;side&gt;:&lt;n&gt;</c>; null when it has
    /// none.
    /// </summary>
    public static decimal? HipAllowance(IEnumerable<string> tokens, string side)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        var prefix = $"hip:{side}:";
        return tokens.Where(token => token.StartsWith(prefix, StringComparison.Ordinal))
            .Select(token => decimal.TryParse(token[prefix.Length..], System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture,
                out var count) ? count : (decimal?)null)
            .Where(count => count > 0).Max();
    }

    /// <summary>A non-vehicular Gun's VP, even dismantled (A26.212; ruling R24.3).</summary>
    public const int GunVp = 2;

    private static readonly Lazy<ScenarioA1OrdnanceReference> OrdnanceReference = new(() => new ScenarioA1OrdnancePackage().Reference);

    /// <summary>
    /// The ammunition a Gun or MA of this definition may carry (C8.1): AP and HE unless its listing denies them, APCR and HEAT where it lists a
    /// Depletion Number. The year and depletion still decide each shot.
    /// </summary>
    public static IReadOnlyList<string> Ammunitions(string definition)
    {
        if (!OrdnanceReference.Value.Guns.TryGetValue(definition, out var gun))
        {
            return [];
        }

        bool Lists(char letter) => gun.SpecialAmmo.Any(item => item.Length > 1 && item[0] == letter && char.IsDigit(item[1]));
        return [.. new[] { ("ap", !gun.NoAp), ("apcr", Lists('A')), ("heat", Lists('H')), ("he", !gun.NoHe) }.Where(item => item.Item2).Select(item => item.Item1)];
    }

    /// <summary>
    /// Good Order (Index, p. 23): a Personnel unit neither broken, berserk, captured, nor held in Melee. Unknown while any of those is not known;
    /// inapplicable to other kinds, since vehicular crews' stun and shock are not yet modelled. The state's own definition (pass 32.a, slice S11);
    /// the planner's and the Close Combat calculator's differ and are listed for pass 35.
    /// </summary>
    public static RuleState GoodOrder(bool personnel, RuleState broken, RuleState berserk, RuleState captured, RuleState melee)
    {
        if (!personnel)
        {
            return RuleState.Inapplicable;
        }

        RuleState[] states = [broken, berserk, captured, melee];
        return states.Contains(RuleState.True) ? RuleState.False
            : states.All(state => state == RuleState.False) ? RuleState.True
            : RuleState.Unknown;
    }

    /// <summary>
    /// Good Order (A.7, p. 43; pass 35, task 35.3), for a unit whose conditions are known: an active Personnel unit or inherent crew that is not broken,
    /// berserk, captured, stunned, shocked, or held in Melee. A pinned, CX, TI, or unarmed unit is still in Good Order. Until pass 35 the planner's
    /// reading left a TI unit out and let a berserk one in, both against the rule. A Disrupted unit is broken, so it needs no clause of its own.
    /// </summary>
    public static bool GoodOrderOf(bool active, bool broken, bool berserk, bool melee, bool captured, bool stunned, bool shocked) =>
        active && !broken && !berserk && !melee && !captured && !stunned && !shocked;

    /// <summary>
    /// Whether a unit may take one of the planner's SW and Deployment actions (Deploy, Recombine, Transfer, Recover, Drop): in Good Order, and not TI.
    /// The TI bar is the planner's as it stood, kept apart from Good Order since A.7 says a TI unit is in Good Order.
    /// </summary>
    public static bool FreeToActAsPlanned(bool goodOrder, bool ti) => goodOrder && !ti;

    /// <summary>A.18 (p. 44; pass 35, task 35.3): a Morale Level is never raised beyond 10, though the unit be Fanatic, heroic, with a Commissar, or in a Human Wave.</summary>
    public static int MoraleCeiling(int level) => Math.Min(10, level);

    /// <summary>
    /// Whether a unit in a Location is Encircled (A7.7; ruling R12.11): of the Encircled side there (<paramref name="encircledForSide"/>), or in Melee
    /// there while any side is Encircled there (<paramref name="encircledForAnySide"/>); never berserk or heroic, a hero, or a vehicle.
    /// </summary>
    public static bool Encircled(bool onMap, bool encircledForSide, bool encircledForAnySide, string kind, RuleState melee, RuleState berserk, RuleState heroic) =>
        onMap && (encircledForSide || (encircledForAnySide && melee == RuleState.True))
        && berserk != RuleState.True && heroic != RuleState.True && kind != "asl:hero" && kind != "asl:vehicle";

    /// <summary>
    /// Whether an event belongs to the setup (C13.31, ruling R9.7; ruling R19.2), by its type: the start, a unit set up, a Bore Sighted Location, or
    /// the drs a start from a card draws for the first move and the Balance (rulings R20.2, R20.3).
    /// </summary>
    public static bool IsSetupEvent(string eventType, string? dicePurpose) =>
        eventType is "game-started" or "instance-created" or "bore-sighted" or "setup-concealed" || (eventType == "dice-rolled" && dicePurpose is "first-move" or "balance");
}

/// <summary>What is known of a condition, as a fact handed to Rules or a verdict handed back (pass 32.a): one value for each of Units' ConditionState.</summary>
public enum RuleState
{
    /// <summary>Nothing is recorded.</summary>
    Unknown,
    False,
    True,

    /// <summary>The viewer may not know it.</summary>
    Withheld,

    /// <summary>The condition does not apply to the kind.</summary>
    Inapplicable,
}
