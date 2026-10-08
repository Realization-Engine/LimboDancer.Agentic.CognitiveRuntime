namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Rout Phase and Desperation Morale (backlog pass 13, rulings R13.1 and R13.3; pass 32.f): who must or may rout, a rout's legal route to the
/// nearest woods or building Location, Low Crawl, Interdiction, Failure to Rout and surrender, and the DM a broken unit gains from an ADJACENT
/// Known armed enemy unit. Play reads the state and the map, hands the facts over through <see cref="IRoutFactReader"/>, and writes the events.
/// </summary>
public static class ScenarioA1RoutCalculator
{
    /// <summary>A10.5, A10.62: an armed enemy unit is Personnel (a leader without a SW counts), or a vehicle not Abandoned.</summary>
    public static bool Armed(bool vehicle, bool abandoned) => !vehicle || !abandoned;

    /// <summary>A10.532: a unit's own printed range counts toward its Normal Range; a leader's does not, and a unit the catalog does not know has none.</summary>
    public static int OwnRange(bool fromCatalog, bool leader, int? printedRange) => fromCatalog && !leader ? printedRange ?? 0 : 0;

    /// <summary>
    /// The Normal Range in hexes of a unit's fire (A10.532): the longest of its own range and the Normal Ranges of the functioning SW it possesses,
    /// at most 16.
    /// </summary>
    public static int NormalRange(int own, IEnumerable<int> weaponRanges)
    {
        ArgumentNullException.ThrowIfNull(weaponRanges);
        return Math.Min(16, weaponRanges.Append(own).Max());
    }

    /// <summary>D6.1 (ruling R26.2): a broken Passenger may stay aboard, free of rout requirements; a unit in Melee or captured need not rout.</summary>
    public static bool MustRoutCandidate(bool active, bool broken, bool melee, bool captured, bool aboard) => active && broken && !melee && !captured && !aboard;

    /// <summary>Why a broken unit must rout (A10.5; ruling R13.3), or null when it need not.</summary>
    public static string? MustRout(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth) =>
        NearUnbrokenArmedEnemy(reader, enemies, at) is { } enemy ? $"{enemy} is a Known unbroken armed enemy unit ADJACENT to it or in its Location"
            : ExposedInOpenGround(reader, enemies, at, scenarioMonth) is { } seen ? $"it is in Open Ground in the LOS and Normal Range of {seen}"
            : null;

    /// <summary>Whether a broken unit may rout (A10.5): it must, or it is under DM.</summary>
    public static bool MayRout(string? mustRout, bool active, bool broken, bool desperationMorale, bool melee, bool captured) =>
        mustRout is not null || (active && broken && desperationMorale && !melee && !captured);

    /// <summary>Two Locations are the same or ADJACENT.</summary>
    public static bool AdjacentOrSame(IRoutFactReader reader, int one, int two)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return one == two || reader.Adjacent(one, two);
    }

    /// <summary>A Known unbroken armed enemy unit ADJACENT to a Location or in it (A10.5), or null.</summary>
    public static string? NearUnbrokenArmedEnemy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && !item.Broken && AdjacentOrSame(reader, item.Location, at))
            .Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();
    }

    /// <summary>The Known armed enemy units ADJACENT to a Location or in it (A10.51, A10.62).</summary>
    public static IEnumerable<RoutEnemyFacts> ArmedEnemiesNear(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && AdjacentOrSame(reader, item.Location, at));
    }

    /// <summary>
    /// Open Ground (A10.531; ruling R13.3): a hex where the enemy could apply the FFMO DRM, read as Open Ground or road terrain, or grain outside June to
    /// September (B15.6; ruling R5.19), with no SMOKE there.
    /// </summary>
    public static bool OpenGround(RoutLocationFacts? read, int? scenarioMonth) =>
        read is { } location && !location.Smoke
            && (location.TerrainKey == "open-ground" || (location.TerrainKey == "grain" && scenarioMonth is < 6 or > 9));

    /// <summary>
    /// A Known unbroken enemy unit not in Melee in whose LOS and Normal Range a Location in Open Ground lies, with no Hindrance between (A10.5, A10.531), or null.
    /// </summary>
    public static string? ExposedInOpenGround(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        if (!OpenGround(reader.Location(at), scenarioMonth))
        {
            return null;
        }

        // A10.531 (table player, pass 13): only an enemy unit that could fire applies FFMO, so broken ones do not count.
        foreach (var enemy in enemies.Where(item => !item.Melee && !item.Broken).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (enemy.NormalRange is > 0 and var range && reader.Los(enemy.Location, at) is { Clear: true, Hindrance: 0 } los && los.Range <= range)
            {
                return enemy.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// The enemy unit able to Interdict a routing unit entering an Open Ground Location (A10.53, A10.532, A10.533; ruling R13.3), or null: Known unbroken
    /// Infantry not CX, pinned, Encircled, in Melee, or a prisoner, with the Location in its LOS and Normal Range and no Hindrance between. Vehicles, and
    /// units whose FP is halved for other reasons, are not read (backlog).
    /// </summary>
    public static string? Interdictor(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        if (!OpenGround(reader.Location(at), scenarioMonth))
        {
            return null;
        }

        foreach (var enemy in enemies.Where(item => !item.Vehicle && !item.Broken && !item.Cx && !item.Pinned && !item.Melee && !item.Encircled)
            .OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (enemy.NormalRange is > 0 and var range && reader.Los(enemy.Location, at) is { Clear: true, Hindrance: 0 } los && los.Range <= range)
            {
                return enemy.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Why a rout step from one Location to an ADJACENT one is not allowed (A10.5, A10.51; ruling R13.3), or null: it enters a Location holding a Known
    /// enemy unit, or a Location ADJACENT to one unless it leaves that unit's Location, or it decreases the range to a Known armed enemy unit that has had
    /// the routing unit in its LOS this rout.
    /// </summary>
    public static string? RoutStepBar(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int fromLocation, int toLocation, IReadOnlyCollection<string> seenBy)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(seenBy);
        foreach (var enemy in enemies)
        {
            if (enemy.Location == toLocation)
            {
                return $"play.rout-step: a routing unit never enters {reader.Name(toLocation)}, which holds the Known enemy unit {enemy.Id} (A10.51)";
            }

            if (enemy.Location != fromLocation && reader.Adjacent(enemy.Location, toLocation))
            {
                return $"play.rout-step: a routing unit never moves ADJACENT to the Known enemy unit {enemy.Id} unless it is leaving its Location (A10.51)";
            }

            if (enemy.Armed && seenBy.Contains(enemy.Id) && reader.Distance(enemy.Location, toLocation) is { } near && reader.Distance(enemy.Location, fromLocation) is { } far && near < far)
            {
                return $"play.rout-step: a routing unit never moves closer to the Known armed enemy unit {enemy.Id}, which has had it in its LOS (A10.51)";
            }
        }

        return null;
    }

    /// <summary>The Known armed enemy units with a clear LOS to a Location (A10.51), read as they are enumerated.</summary>
    public static IEnumerable<string> SeenBy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && reader.Los(item.Location, at) is { Clear: true }).Select(item => item.Id);
    }

    /// <summary>
    /// The half MF a routing unit pays to step between ADJACENT Locations (A10.5): the Infantry entry cost, without Bypass or Road Bonus, doubled for the
    /// first Location an Encircled unit enters (A7.7; ruling R12.11); or why it may not.
    /// </summary>
    public static RoutStepCost RoutEntry(IRoutFactReader reader, int fromLocation, int toLocation, bool encircledFirst)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var (entry, reason) = reader.Entry(fromLocation, toLocation);
        return entry switch
        {
            null => new RoutStepCost(null, false, reason ?? $"play.rout-step: the step from {reader.Name(fromLocation)} to {reader.Name(toLocation)} is not one the game allows"),
            { MinimumMoveOnly: true } => new RoutStepCost(null, false, $"play.rout-step: {reader.Name(toLocation)} is entered only by Minimum Move, which a rout does not use (A10.5)"),
            { AllMf: true } => new RoutStepCost(null, true, null),
            _ => new RoutStepCost(entry.HalfMf * (encircledFirst ? 2 : 1), false, null),
        };
    }

    /// <summary>A10.51: a woods or building Location is a rout destination.</summary>
    public static bool RoutCover(RoutLocationFacts? read) => read is { } location && location.TerrainKey is "woods" or "wooden-building" or "stone-building";

    /// <summary>A10.5, as the planner has it: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three.</summary>
    public static int RoutHalfMfAsPlanned(bool smc, bool wounded) => smc && wounded ? 6 : 12;

    /// <summary>The broken Morale Level (A10.4), one lower for a wounded SMC (A17.3), from the catalog; null for a unit the catalog does not know.</summary>
    public static int? BrokenMorale(bool fromCatalog, int? brokenMorale, int? morale, bool wounded) =>
        fromCatalog ? (brokenMorale ?? morale) - (wounded ? 1 : 0) : null;

    /// <summary>Casualty Reduction (A7.302): a squad with a HS becomes it, a SMC is wounded, or eliminated if already wounded, and anything else is eliminated.</summary>
    public static CasualtyOutcome CasualtyReduction(bool squadWithHalfSquad, bool leaderOrHero, bool wounded) =>
        squadWithHalfSquad ? CasualtyOutcome.Reduced : leaderOrHero && !wounded ? CasualtyOutcome.Wounded : CasualtyOutcome.Eliminated;

    /// <summary>A10.62 (ruling R13.1): a broken unit not under DM and not a prisoner can come under DM.</summary>
    public static bool DmCandidate(bool active, bool broken, bool desperationMorale, bool captured) => active && broken && !desperationMorale && !captured;

    /// <summary>A10.62 (ruling R13.1): whether a Known armed enemy unit is ADJACENT to a Location or in it.</summary>
    public static bool ArmedEnemyNear(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at) => ArmedEnemiesNear(reader, enemies, at).Any();

    /// <summary>A10.62 (ruling R13.1): why the broken units a plan's events put ADJACENT to a Known armed enemy unit come under DM.</summary>
    public static string AdjacentDmReason(IEnumerable<string> gaining)
    {
        ArgumentNullException.ThrowIfNull(gaining);
        return $"play.dm: {string.Join(", ", gaining)} come under DM from an ADJACENT Known armed enemy unit (A10.62)";
    }

    /// <summary>A10.62 (ruling R13.1): why a broken unit in Open Ground comes under DM as the RtPh starts.</summary>
    public static string RoutPhaseDmReason(string seen) => $"in Open Ground in the LOS and Normal Range of {seen}";

    /// <summary>
    /// Why a unit may not keep its DM as the RPh ends (A10.62; ruling R13.1), or null: it is a broken unit under DM, not in woods or a building; the
    /// Location is read only when the unit qualifies.
    /// </summary>
    public static string? RetainDmBar(string id, bool active, bool broken, bool desperationMorale, Func<bool> unplacedOrInCover)
    {
        ArgumentNullException.ThrowIfNull(unplacedOrInCover);
        return !active || !broken || !desperationMorale
            ? $"play.retain-dm: '{id}' is not a broken unit under DM (A10.62)"
            : unplacedOrInCover()
                ? $"play.retain-dm: {id} is in a woods or building Location, where DM is not retained (A10.62)"
                : null;
    }
}
