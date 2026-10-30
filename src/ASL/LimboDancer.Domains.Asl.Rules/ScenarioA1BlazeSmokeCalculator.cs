namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What burns in a Location, for its SMOKE (B25.2): a Burning Wreck's Blaze, a terrain Blaze, or a Flame.</summary>
public enum BlazeKind
{
    WreckBlaze, TerrainBlaze, Flame
}

/// <summary>The Wind Force a Blaze's SMOKE feels (B25.63): No Wind, a Mild Breeze, Heavy Winds, or not known.</summary>
public enum WindForce
{
    None, MildBreeze, HeavyWinds, Unknown
}

/// <summary>What a burning wreck adds to a LOS through its Location (ruling RB25.4): whether its SMOKE applies, and whether D9.4 applies to it as if it were not burning.</summary>
public sealed record WreckBlazeLosVerdict(bool Smoke, bool WreckHindrance);

/// <summary>
/// B25.2 Smoke (p. 143), the one calculator of the rule (the B25.2 claim's design, ruling RB25.1): a Blaze's SMOKE Hindrance, its height, what a burning
/// wreck adds to a LOS, and its drift. Every member reads the wind as None in a non-rooftop Location of an Interior Building hex (B25.63); Gusts never
/// matter (B25.651). A Location's total and A24.8's outgoing +1 stay with A24.2's by-value total
/// (<see cref="ScenarioA1VehicleSightRules.SmokeDrm(IReadOnlyList{int}, bool, bool, Func{bool})"/>). Facts in, verdicts out.
/// </summary>
public static class ScenarioA1BlazeSmokeCalculator
{
    /// <summary>Ruling RB25.2: the wind a Blaze's SMOKE feels: No Wind in a non-rooftop Location of an Interior Building hex (B25.63), else the scenario's.</summary>
    public static WindForce WindFelt(WindForce wind, bool interiorNonRooftop)
    {
        if (!Enum.IsDefined(wind))
        {
            throw new ArgumentException("Unknown Wind Force.", nameof(wind));
        }

        return interiorNonRooftop ? WindForce.None : wind;
    }

    /// <summary>
    /// Ruling RB25.3 (B25.2): a Blaze's SMOKE Hindrance. A Wreck Blaze +2, "rather than the +3 Hindrance DRM of regular smoke", so a terrain Blaze +3;
    /// a Flame none (it makes no smoke); none in Heavy Winds (B25.63); a Mild Breeze, Rain, Mud, or Deep Snow lowers nothing (A24.5, E3.53, E3.734).
    /// Null when the wind is not known.
    /// </summary>
    public static int? Hindrance(BlazeKind kind, WindForce wind)
    {
        Validate(kind, wind);
        return wind == WindForce.Unknown ? null
            : kind == BlazeKind.Flame || wind == WindForce.HeavyWinds ? 0
            : kind == BlazeKind.WreckBlaze ? 2 : 3;
    }

    /// <summary>
    /// Ruling RB25.4 (B25.2, A9.222, D9.4): what a burning wreck adds to a LOS through its Location. Its SMOKE, and no D9.4 Hindrance, as a rule; on a
    /// Fire Lane's Residual FP attack, or in Heavy Winds, D9.4 as if it were not burning, and no SMOKE. D9.4's own conditions stay with its calculator,
    /// which the caller asks next. Null when the wind is not known.
    /// </summary>
    public static WreckBlazeLosVerdict? WreckBlazeLos(bool fireLaneResidualAttack, WindForce wind)
    {
        Validate(BlazeKind.WreckBlaze, wind);
        return wind == WindForce.Unknown ? null
            : fireLaneResidualAttack || wind == WindForce.HeavyWinds ? new(false, true) : new(true, false);
    }

    /// <summary>
    /// Ruling RB25.4, its second case (a reading the user approved): a non-burning wreck or an AFV in a terrain Blaze's hex keeps its D9.4 Hindrance
    /// beside the Blaze's SMOKE, since D9.4 exempts only a burning wreck and B25.2 replaces only that wreck's own Hindrance.
    /// </summary>
    public static bool KeepsWreckHindranceInTerrainBlaze => true;

    /// <summary>Ruling RB25.5 (B25.2, A24.4): the levels above the Fire its SMOKE reaches: four, two in a Mild Breeze, none in Heavy Winds. Null when not known.</summary>
    public static int? Height(BlazeKind kind, WindForce wind)
    {
        Validate(kind, wind);
        return wind == WindForce.Unknown ? null
            : kind == BlazeKind.Flame || wind == WindForce.HeavyWinds ? 0
            : wind == WindForce.MildBreeze ? 2 : 4;
    }

    /// <summary>
    /// Ruling RB25.6 (B25.2, A24.61, E3.53): the drifting Dispersed SMOKE counters a Blaze can create: in a Mild Breeze and not in Rain, as many as its
    /// Hindrance (2 for a Wreck Blaze, 3 for a terrain Blaze); none otherwise. Null when the wind is not known. The drift itself is A24.61's.
    /// </summary>
    public static int? DriftCounters(BlazeKind kind, WindForce wind, bool rain)
    {
        Validate(kind, wind);
        return wind == WindForce.Unknown ? null
            : wind == WindForce.MildBreeze && !rain ? Hindrance(kind, wind) : 0;
    }

    private static void Validate(BlazeKind kind, WindForce wind)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(wind))
        {
            throw new ArgumentException("Unknown Blaze or Wind Force.");
        }
    }
}
