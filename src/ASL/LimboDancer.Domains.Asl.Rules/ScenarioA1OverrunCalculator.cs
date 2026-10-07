namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a vehicle's OVR and the PAATC (S8; backlog pass 11, rulings R11.11 to R11.13, R11.17): the OVR's cost and bars, its SMOKE and
/// target terrain, and the PAATC's subjects, exemptions, Morale Level, DRM, and result (A11.6, A12.41, D7.1 to D7.21). The caller reads the state and the
/// map and writes the events.
/// </summary>
public static class ScenarioA1OverrunCalculator
{
    /// <summary>D7.1 (ruling R11.11): an OVR costs a quarter of the vehicle's printed MP allotment (FRU), in half MP.</summary>
    public static int OverrunHalfMp(int? movementPoints) => (((movementPoints ?? 0) + 3) / 4) * 2;

    /// <summary>
    /// D7.1, D7.12 to D7.14 (ruling R11.11): why a vehicle may not OVR a Location, or null: not in Reverse or VBM, not after its own Bounding First Fire
    /// (an earlier OVR aside), with an enemy unit there to attack, none of them in Melee and no enemy vehicle there in Motion.
    /// </summary>
    public static string? OverrunBar(string id, string at, bool reverse, bool bypass, bool boundingFire, Func<bool> overranThisPhase,
        Func<IReadOnlyList<OverrunEnemy>> enemies)
    {
        ArgumentNullException.ThrowIfNull(overranThisPhase);
        ArgumentNullException.ThrowIfNull(enemies);
        if (reverse)
        {
            return "play.move-vehicle-ovr: no OVR is made in Reverse (D7.13)";
        }

        if (bypass)
        {
            return "play.move-vehicle-ovr: no OVR is made from VBM (D7.13; ruling R11.11)";
        }

        var overran = overranThisPhase();
        if (boundingFire && !overran)
        {
            return $"play.move-vehicle-ovr: {id} is marked Bounding Fire from its own fire, so it makes no OVR (D7.1, D7.13)";
        }

        var there = enemies();
        if (there.Count == 0)
        {
            return $"play.move-vehicle-ovr: {at} holds no enemy unit to OVR (D7.1)";
        }

        if (there.All(unit => unit.Afv && !unit.CrewExposed))
        {
            return $"play.move-vehicle-ovr: an AFV is not OVR, and {at} holds nothing else it could attack (D7.12)";
        }

        if (there.Any(unit => unit.Melee))
        {
            return "play.move-vehicle-ovr: an OVR of units held in Melee is not reviewed (ruling R11.11)";
        }

        if (there.Any(unit => unit.VehicleInMotion))
        {
            return "play.move-vehicle-ovr: an OVR of a Location holding an enemy vehicle in Motion, with its +2 against the vehicle's PRC, is not built (D7.12)";
        }

        return null;
    }

    /// <summary>D7.1: the MP left must pay an OVR declared in the vehicle's own Location.</summary>
    public static string? OverrunMpBar(string id, int spent, int allotment, int cost) =>
        spent + cost > allotment
            ? $"play.move-vehicle-mp: {id} has {ScenarioA1VehicleMovementCalculator.Mp(Math.Max(0, allotment - spent))} MP left, and an OVR costs {ScenarioA1VehicleMovementCalculator.Mp(cost)} (D7.1)"
            : null;

    /// <summary>D7.1: why a declared OVR may not be resolved yet, or null: one is declared, by the vehicle named, and the DEFENDER's window on it is closed.</summary>
    public static string? ResolveBar(string? declaredBy, string? named, bool windowOpen)
    {
        if (declaredBy is null)
        {
            return "play.overrun: no OVR is declared (D7.1)";
        }

        if (named is not null && named != declaredBy)
        {
            return $"play.overrun: the declared OVR is {declaredBy}'s";
        }

        return windowOpen ? "play.overrun: the DEFENDER may still fire at the OVR's MP expenditure; the OVR is resolved when he passes (D7.1)" : null;
    }

    /// <summary>A24.2 (ruling R9.6): SMOKE in the OVR's Location hinders the attack within it, +2 per source and at most +3, and +1 more than traced into it.</summary>
    public static int OverrunSmoke(int sources) => sources == 0 ? 0 : Math.Min(3, 2 * sources) + 1;

    /// <summary>B15.6: grain is Open Ground for the OVR's target outside June to September.</summary>
    public static string OverrunTargetTerrain(string terrain, int? month) => terrain == "grain" && month is not (>= 6 and <= 9) ? "open-ground" : terrain;

    /// <summary>The key of the A12.41 choice of a vehicle's entry (ruling R11.12).</summary>
    public static string PaatcKey(string vehicle, string at) => $"paatc:{vehicle}:{at}";

    /// <summary>A11.6: a SMC, a Fanatic unit, or a berserk one takes no PAATC.</summary>
    public static bool PaatcExempt(bool leader, bool hero, bool fanatic, bool berserk) => leader || hero || fanatic || berserk;

    /// <summary>
    /// A11.6, A12.41 (rulings R11.12, R11.17): a PAATC's Morale Level, the lowest current Morale Level among the units: a Dummy's 7, a printed Morale +1
    /// Fanatic and -1 for a wounded leader, or 7 when none is printed.
    /// </summary>
    public static int PaatcMorale(IEnumerable<PaatcUnit> units) =>
        units.Select(unit => unit.Dummy ? 7 : unit.PrintedMorale is { } printed ? printed + (unit.Fanatic ? 1 : 0) - (unit.WoundedLeader ? 1 : 0) : 7)
            .DefaultIfEmpty(7).Min();

    /// <summary>
    /// A11.6 (ruling R11.17): a PAATC's DRM and its causes: +1 for a 1PAATC when any unit is Inexperienced, and the best leadership of an unpinned Good
    /// Order leader of their side in the Location.
    /// </summary>
    public static (int Drm, IReadOnlyList<string> Causes) PaatcDrm(bool inexperienced, string? leader, int leadership)
    {
        var drm = 0;
        var causes = new List<string>();
        if (inexperienced)
        {
            drm += 1;
            causes.Add("inexperienced-1paatc");
        }

        if (leader is not null && leadership != 0)
        {
            drm += leadership;
            causes.Add("leadership:" + leader);
        }

        return (drm, causes);
    }

    /// <summary>A11.6: a PAATC passes when the DR plus its DRM is no more than the Morale Level.</summary>
    public static bool PaatcPassed(int first, int second, int drm, int morale) => first + second + drm <= morale;

    /// <summary>A11.6, D7.21: a MMC must pass a PAATC to advance on or make CC Reaction Fire at a manned, unconcealed AFV, unless exempt or passed this phase.</summary>
    public static bool NeedsPaatc(bool afv, bool abandoned, bool concealed, bool hidden, Func<bool> exempt, Func<bool> passed)
    {
        ArgumentNullException.ThrowIfNull(exempt);
        ArgumentNullException.ThrowIfNull(passed);
        return afv && !abandoned && !concealed && !hidden && !exempt() && !passed();
    }
}

/// <summary>An enemy unit in a Location a vehicle would OVR, as its bars read it (D7.12; ruling R11.11).</summary>
public sealed record OverrunEnemy(bool Afv, bool CrewExposed, bool Melee, bool VehicleInMotion);

/// <summary>A unit taking a PAATC, as its Morale Level reads it (A11.6; ruling R11.17).</summary>
public sealed record PaatcUnit(bool Dummy, int? PrintedMorale, bool Fanatic, bool WoundedLeader);
