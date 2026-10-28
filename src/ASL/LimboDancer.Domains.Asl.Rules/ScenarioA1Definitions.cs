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
    public static IReadOnlyList<string> HiddenSetupKinds { get; } = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero"];

    /// <summary>The unit kinds that are MMC for gaining Control (A26.11).</summary>
    public static IReadOnlyList<string> MmcKinds { get; } = ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew"];

    /// <summary>The kinds a view shows as a counter of a stack before play (A2.9); SW and Guns lie beneath their units and are not seen before play.</summary>
    public static IReadOnlyList<string> StackCounterKinds
    {
        get;
    } =
        ["asl:squad", "asl:half-squad", "asl:crew", "asl:vehicle-crew", "asl:leader", "asl:hero", "asl:vehicle"];

    /// <summary>
    /// Whether a kind is a crew (A1.123, p. 44; D5.1, p. 203; pass 65, ruling R65.2): an infantry crew or a vehicle crew. "A vehicle-crew counter
    /// has all the capability of an infantry-crew counter (A1.123), other than its lower Strength Factor", and that difference lives in the
    /// definition's values, so every rule that speaks of a crew reads this member.
    /// </summary>
    public static bool IsCrew(string? kind) => kind is "asl:crew" or "asl:vehicle-crew";

    /// <summary>The kind of the counter a crew becomes when it leaves its vehicle (D5.1, p. 203).</summary>
    public const string VehicleCrewKind = "asl:vehicle-crew";

    /// <summary>
    /// The definition a crew that leaves its vehicle is placed as (D5.1, p. 203: "Whenever a crew leaves its vehicle it becomes a vehicle-crew
    /// counter"; pass 65, ruling R65.19): the vehicle-crew definition of the vehicle's nationality, the first by id; null when the catalog has none.
    /// </summary>
    public static string? LeavingCrewDefinition(IEnumerable<FireDefinition> definitions, string? nationality)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return definitions.Where(item => item.Kind == VehicleCrewKind && item.Nationality == nationality).Select(item => item.Id)
            .Order(StringComparer.Ordinal).FirstOrDefault();
    }

    /// <summary>
    /// Whether a package that reads <paramref name="packageCatalog"/> at <paramref name="packageVersion"/> serves a game's catalog (pass 65, ruling
    /// R65.4): the same catalog at the same major version and a version no later than the package's, as the vocabulary's <c>Serves</c> reads a pack.
    /// A catalog only adds and corrects definitions, never removes one, so a game of an older minor version still reads every definition it names.
    /// </summary>
    public static bool CatalogServes(string packageCatalog, string packageVersion, string gameCatalog, string gameVersion) =>
        gameCatalog == packageCatalog && Version.TryParse(packageVersion, out var package) && Version.TryParse(gameVersion, out var game)
        && game.Major == package.Major && game <= package;

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

    /// <summary>
    /// B13.3, B13.31 (p. 128; pass 40, ruling R40.4): a woods-road hex, whose "normal woods TEM is in effect" but for a mover at the road rate. The
    /// map names such a hex as VASL does (<c>VASLBoard.fillWoodsRoadHexes</c>): a road at its center dot and Woods on one of its hexside Locations.
    /// </summary>
    public static bool IsWoodsRoadHex(bool centerIsRoad, IEnumerable<string?> hexsideLocationTerrains)
    {
        ArgumentNullException.ThrowIfNull(hexsideLocationTerrains);
        return centerIsRoad && hexsideLocationTerrains.Any(name => name == "Woods");
    }

    /// <summary>
    /// A4.132, B3.3 (pp. 48, 113; pass 40): the terrain a fire attack reads for a mover that entered its Location at the road rate, from each
    /// firing Location's two target points: Open Ground (no TEM of the other terrain, and FFMO as Open Ground has it) when every firing Location
    /// reaches the hex center dot or the road's hexside without crossing the hex's other terrain; the Location's own terrain otherwise, and when
    /// the map cannot say. A7.52: the worst case of a Fire Group's Locations applies to all of it.
    /// </summary>
    public static string RoadRateTargetTerrain(string locationTerrain, IEnumerable<(bool? CenterCrossesTerrain, bool? HexsideCrossesTerrain)> firingLocations)
    {
        ArgumentNullException.ThrowIfNull(firingLocations);
        var read = firingLocations.ToArray();
        return read.Length > 0 && read.All(item => item.CenterCrossesTerrain == false || item.HexsideCrossesTerrain == false) ? "open-ground" : locationTerrain;
    }

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
    /// The ammunition a Gun or MA of this definition may carry (C8.1): AP and HE unless its listing denies them, APCR, APDS, HEAT, and Canister
    /// where it lists a Depletion Number, and HE by its Depletion Number though the caliber is underscored (C8.8, p. 179; pass 55). The year and
    /// depletion still decide each shot.
    /// </summary>
    public static IReadOnlyList<string> Ammunitions(string definition)
    {
        if (!OrdnanceReference.Value.Guns.TryGetValue(definition, out var gun))
        {
            return [];
        }

        bool Lists(string type) => gun.SpecialAmmo.Any(item => ScenarioA1ArmorCalculator.ParseAmmunition(item)?.Type == type);
        return [.. new[] { ("ap", !gun.NoAp), ("apcr", Lists("A")), ("apds", Lists("D")), ("heat", Lists("H")), ("he", !gun.NoHe || Lists("HE")), ("canister", Lists("C")) }
            .Where(item => item.Item2).Select(item => item.Item1)];
    }

    /// <summary>
    /// The Depletion Number of an ammunition (<c>apcr</c>, <c>apds</c>, <c>heat</c>, <c>he</c>, <c>ap</c>, <c>canister</c>) for a Gun at a date
    /// (C8.9, C8.91, C8.3, C8.8; pp. 178 and 179), one more for Elite forces (C8.2), one less under Ammunition Shortage (A19.131); null when the Gun has
    /// none then.
    /// </summary>
    public static int? DepletionNumber(GunDefinition gun, string ammunition, int? year, int? month, bool elite = false, bool shortage = false) =>
        ScenarioA1ArmorCalculator.Depletion(gun, ammunition, year, month, elite, shortage);

    /// <summary>The start of the game's special rule that makes a side's force Elite for its Depletion Numbers (C8.2; pass 55, ruling R55.6); the side's id follows.</summary>
    public const string EliteAmmunitionRule = "elite-ammunition:";

    /// <summary>
    /// C8.2 (p. 178; pass 55, ruling R55.6): "An armed-vehicle/weapon in a printed scenario is Elite for this purpose if so specified by an SSR":
    /// whether the game's special rules name the side.
    /// </summary>
    public static bool EliteAmmunition(IReadOnlyList<string> specialRules, string? side)
    {
        ArgumentNullException.ThrowIfNull(specialRules);
        return side is not null && specialRules.Contains(EliteAmmunitionRule + side, StringComparer.Ordinal);
    }

    /// <summary>
    /// A Depletion Number in words, with the one an Elite force adds (C8.2): "Depletion Number 7 (6, and 1 for Elite, C8.2)"; and the one an Ammunition
    /// Shortage takes away (A19.131; pass 72, ruling R72.12): "Depletion Number 6 (7, less 1 for Ammunition Shortage, A19.131)".
    /// </summary>
    private static string DepletionWords(int number, bool elite, string open = " (", string close = ")", bool shortage = false) =>
        "Depletion Number " + number.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + (elite || shortage ? open + (number - (elite ? 1 : 0) + (shortage ? 1 : 0)).ToString(System.Globalization.CultureInfo.InvariantCulture)
                + (elite ? ", and 1 for Elite, C8.2" : string.Empty) + (shortage ? ", less 1 for Ammunition Shortage, A19.131" : string.Empty) + close : string.Empty);

    /// <summary>
    /// A recorded Depletion Number in a record's words, with the one an Elite force added (C8.2) and the one an Ammunition Shortage took away (A19.131): what
    /// a record says of the number it names.
    /// </summary>
    public static string DepletionNumberWords(int number, bool elite, bool shortage = false) => DepletionWords(number, elite, shortage: shortage);

    /// <summary>A weapon's B# or X# as a recorded attack lowered it, in a record's words (<see cref="ScenarioA1FireCalculator.BreakdownWords"/>); null as printed.</summary>
    public static string? BreakdownWords(FireAttack attack, FireWeaponEffect effect) => ScenarioA1FireCalculator.BreakdownWords(attack, effect, OrdnanceReference.Value.Fire);

    /// <summary>
    /// Whether an Ammunition Shortage lowered a recorded shot's Depletion Number (A19.131): never for a captured Gun (A21.11, p. 88; pass 72, ruling R72.18).
    /// </summary>
    public static bool DepletionShortage(OrdnanceShot? shot) =>
        shot is not null && OrdnanceReference.Value.Guns.TryGetValue(shot.Gun?.DefinitionId ?? string.Empty, out var gun) && ScenarioA1ArmorCalculator.DepletionShortage(shot, gun);

    /// <summary>
    /// The Depletion Number a shot's declared ammunition is fired under, in words (C8.9; pass 55, ruling R55.7), or null when it has none: what
    /// the proposal and the record of the shot say.
    /// </summary>
    public static string? DepletionWords(OrdnanceShot shot)
    {
        ArgumentNullException.ThrowIfNull(shot);
        return OrdnanceReference.Value.Guns.TryGetValue(shot.Gun?.DefinitionId ?? string.Empty, out var gun) && ScenarioA1ArmorCalculator.ShotDepletion(shot, gun) is { } number
            ? DepletionWords(number, shot.Elite == true, shortage: ScenarioA1ArmorCalculator.DepletionShortage(shot, gun)) : null;
    }

    /// <summary>
    /// The ammunition a player may choose for a Gun or MA of this definition at a vehicle or at Infantry (C8.1, C2.21; pass 55), each with its
    /// Depletion Number at the game's date and, when the Gun has none then or has run out, the reason. At a vehicle: what the definition may
    /// carry but Canister, which "is ineffective vs armored targets" (C8.4). At Infantry: HE first, then Canister where the Gun lists it (C8.4),
    /// then AP, APCR, APDS, and HEAT by their HE Equivalency (C8.31; pass 60), each with the FP it attacks with; HEAT only for a Gun with an HE FP,
    /// and only at Infantry behind a wall or in a building or rubble, which the shot decides. Nothing to choose when the Gun has only an HE that is
    /// assumed (C2.21); its HE is then not named.
    /// </summary>
    public static IReadOnlyList<AmmunitionChoice> AmmunitionChoices(string definition, bool vehicleTarget, int? year, int? month, bool elite, IReadOnlyList<string> depleted,
        bool shortage = false)
    {
        ArgumentNullException.ThrowIfNull(depleted);
        if (!OrdnanceReference.Value.Guns.TryGetValue(definition, out var gun) || gun.GunType == "latw")
        {
            return [];
        }

        var canister = ScenarioA1ArmorCalculator.Lists(gun, ScenarioA1CanisterCalculator.Ammunition) && ScenarioA1CanisterCalculator.Firepower(gun.Caliber) is not null;
        var namedHe = ScenarioA1ArmorCalculator.Lists(gun, "he");
        var heFirepower = gun.NoHe && !namedHe ? (int?)null : OrdnanceReference.Value.HeFirepower(gun.Caliber);
        string[] equivalent = [.. Ammunitions(definition).Where(item => item is "ap" or "apcr" or "apds" || (item == "heat" && heFirepower is > 0))];
        string[] offered = vehicleTarget ? [.. Ammunitions(definition).Where(item => item != ScenarioA1CanisterCalculator.Ammunition)]
            : [.. new[] { ("he", namedHe || ((canister || equivalent.Length > 0) && !gun.NoHe)), (ScenarioA1CanisterCalculator.Ammunition, canister) }.Where(item => item.Item2)
                .Select(item => item.Item1), .. equivalent];
        return [.. offered.Select(ammunition =>
        {
            var special = ammunition is "apcr" or "apds" or "heat" || ScenarioA1ArmorCalculator.Lists(gun, ammunition);
            var number = special ? ScenarioA1ArmorCalculator.Depletion(gun, ammunition, year, month, elite, shortage) : null;
            var name = AmmunitionName(ammunition);
            var fp = vehicleTarget ? null : ScenarioA1HeEquivalency.Firepower(ammunition, ScenarioA1HeEquivalency.Gun, gun.Caliber, heFirepower);
            return new AmmunitionChoice(ammunition, number is { } held ? $"{name} ({DepletionWords(held, elite, ": ", string.Empty, shortage)})" : name, number,
                depleted.Contains(ammunition, StringComparer.Ordinal) ? "it has run out (C8.9)"
                    : special && number is null ? NoneAtTheDate(ammunition, ScenarioA1ArmorCalculator.StartMonthUnknown(gun, ammunition, year, month)) : null)
            {
                Equivalency = fp is { } equivalency
                    ? $"{equivalency.ToString(System.Globalization.CultureInfo.InvariantCulture)} FP by HE Equivalency{(ammunition == "heat" ? ", only at a Gun or at Infantry behind a wall or in a building or rubble" : string.Empty)}, C8.31"
                    : null,
            };
        })];
    }

    /// <summary>
    /// Whether a Gun or MA of this definition fires HE at Infantry with no ammunition named (C2.21; pass 60): it has HE with no Depletion Number, so
    /// the shot leaves it unnamed, as every shot before pass 55 did.
    /// </summary>
    public static bool HeAssumed(string definition) =>
        OrdnanceReference.Value.Guns.TryGetValue(definition, out var gun) && !gun.NoHe && !ScenarioA1ArmorCalculator.Lists(gun, "he");

    /// <summary>
    /// The start of the detail a refusal's code carries when the ammunition starts in a month of the game's year and the game gives no month
    /// (ruling R55.31): <c>asl.a1.ordnance.ammunition-unavailable:from-month-5</c>.
    /// </summary>
    public const string StartMonthMark = ScenarioA1ArmorCalculator.StartMonthMark;

    /// <summary>An ammunition as a choice names it: "Canister", "APDS", "HE".</summary>
    public static string AmmunitionName(string ammunition)
    {
        ArgumentNullException.ThrowIfNull(ammunition);
        return ammunition == ScenarioA1CanisterCalculator.Ammunition ? "Canister" : ammunition.ToUpperInvariant();
    }

    /// <summary>
    /// Why a Gun has no such ammunition at the game's date, with the rule that dates it: C8.3 for HEAT, C8.8 for HE or AP by number, C8.11 for
    /// the others, and all three when the ammunition is not named. <paramref name="startMonth"/> is the month it starts in when a game with a
    /// year and no month is the cause (ruling R55.31).
    /// </summary>
    public static string NoneAtTheDate(string? ammunition, int? startMonth)
    {
        var rule = ammunition switch
        {
            null => "C8.11, C8.3, C8.8",
            "heat" => "C8.3",
            "he" or "ap" => "C8.8",
            _ => "C8.11",
        };
        return startMonth is { } first and >= 1 and <= 12
            ? "none known at the scenario's date: the game gives a year and no month, and this ammunition starts in "
                + $"{System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(first)} of that year ({rule})"
            : $"none at the scenario's date ({rule})";
    }

    /// <summary>
    /// Good Order (Index, p. 23): a Personnel unit neither broken, berserk, captured, nor held in Melee. Unknown while any of those is not known;
    /// inapplicable to other kinds, since vehicular crews' stun and shock are not yet modelled. The state's own definition (pass 32.a, slice S11);
    /// <see cref="GoodOrderOf"/> is the planner's since pass 35, with a vehicle's stun and shock, and the Close Combat calculator keeps its own
    /// (the pass 35 design, section 8.2).
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
    /// <summary>
    /// A1.6 (p. 45; pass 40): the Unit Size Number. "Personnel units have a US# equal to the number of silhouettes on their counter (SMC: 1;
    /// Crew/HS: 2; Squad: 3). Horses and all other 5/8\" counters have a US# of 4 ... [EXC: Any vehicle or Gun classified as large or very large
    /// (red H, AF, or M#) has a US# of 5]". The one member for it: Concealment (A12.12), Guarding prisoners (A20.5), and CC read it here.
    /// </summary>
    public static int UnitSize(bool squad, bool halfSquadOrCrew, bool fiveEighths = false, bool large = false) =>
        fiveEighths ? (large ? 5 : 4) : squad ? 3 : halfSquadOrCrew ? 2 : 1;

    /// <summary>The US# of a Personnel unit by its kind (A1.6).</summary>
    public static int UnitSize(string? kind) => UnitSize(kind == "asl:squad", kind == "asl:half-squad" || IsCrew(kind));

    public static bool GoodOrderOf(bool active, bool broken, bool berserk, bool melee, bool captured, bool stunned, bool shocked) =>
        active && !broken && !berserk && !melee && !captured && !stunned && !shocked;

    /// <summary>
    /// Whether a unit may take one of the planner's SW and Deployment actions (Deploy, Recombine, Transfer, Recover, Drop): in Good Order, and not TI.
    /// The TI bar is the planner's as it stood, kept apart from Good Order since A.7 says a TI unit is in Good Order.
    /// </summary>
    public static bool FreeToActAsPlanned(bool goodOrder, bool ti) => goodOrder && !ti;

    /// <summary>
    /// Whether a TI unit may take a SW or Deployment action (A.7, p. 43; A4.8, p. 52; pass 45, ruling R45.6): "A unit can be pinned, CX, TI, and/or
    /// unarmed and still be considered in 'Good Order'", and "A TI unit may not move, advance, fire, or perform other labor tasks". Deployment,
    /// Recombining, a Transfer, a Drop, and Recovery in the RPh spend no MF and are no labor task, so Good Order is enough; Recovery in the MPh
    /// costs a MF, which a TI unit may not spend (<paramref name="spendsMf"/>).
    /// </summary>
    public static bool FreeToAct(bool goodOrder, bool ti, bool spendsMf) => goodOrder && !(ti && spendsMf);

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
