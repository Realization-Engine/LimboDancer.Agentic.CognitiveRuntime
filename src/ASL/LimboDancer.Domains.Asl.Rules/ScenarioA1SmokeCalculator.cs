namespace LimboDancer.Domains.Asl.Rules;

/// <summary>One unit of the moving stack as a SMOKE attempt reads it: its MF allowance with and without the Double Time asked, and the half MF it has spent.</summary>
public sealed record SmokeMoverFacts(string UnitId, int? Allowance, int? PlainAllowance, int HalfMfSpent);

/// <summary>
/// The facts of a SMOKE grenade attempt (A24.1, A24.11, A4.51, A8.2, E3.53, E3.734; rulings R9.5, R9.6), as the planner reads them from the state
/// and the map. <paramref name="TargetTerrain"/> is the target Location's terrain as the map reads the Location on its own; <paramref name="StepTerrain"/>
/// is the terrain the step from the squad's Location reads at its far end, read only for a target other than the squad's own Location.
/// </summary>
public sealed record SmokeAttemptFacts(
    string PlacerId,
    bool PlacerInStack,
    bool PlacerIsSquad,
    bool PlacerBerserk,
    int? Exponent,
    bool AlreadyAttempted,
    bool ResidualFpHere,
    string? Precipitation,
    bool Mud,
    bool DeepSnow,
    string? TargetTerrain,
    string? StepTerrain,
    string TargetText,
    bool TargetIsOwnLocation,
    bool StepReadable,
    bool SameElevation,
    bool SameLevel,
    bool Cliff,
    bool Assault,
    IReadOnlyList<SmokeMoverFacts> Movers);

/// <summary>The verdict on a SMOKE attempt: the first refusal, or the half MF the stack spends.</summary>
public sealed record SmokeAttemptVerdict(string? Refusal, int HalfMf);

/// <summary>
/// The facts of a recorded SMOKE attempt as the projector checks a movement step (A24.1; ruling R9.5; table player, pass 9): the record's fields
/// against the state, the catalog, and the recorded dr.
/// </summary>
public sealed record SmokeRecordFacts(
    bool PlacerInMovers,
    bool AlreadyAttempted,
    bool StepEntersPlacerLocation,
    bool RollFound,
    int RollCount,
    int? RollValue,
    int RecordedDr,
    int RecordedExponent,
    bool PlacerActive,
    int? CatalogExponent,
    bool RecordedCx,
    bool DoubleTime,
    bool PlacerCx,
    int HalfMf,
    bool TargetIsStepDestination);

/// <summary>
/// SMOKE grenades (A24.1, A24.11; rulings R9.5, R9.6; pass 32.a, the worked action): in its MPh a Good Order squad with a Smoke Placement Exponent, in
/// the moving stack, names its own Location (1 MF) or an ADJACENT one at its level (2 MF); a dr at most the exponent places a SMOKE counter there. The
/// planner's checks are here in their order; the projector's one check is <see cref="Verify"/>, a second function because it differs (design D6).
/// </summary>
public static class ScenarioA1SmokeCalculator
{
    public static SmokeAttemptVerdict Plan(SmokeAttemptFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        // A24.1: a squad of the moving stack with a Smoke Placement Exponent, once per MPh, not berserk (A15.43).
        if (!facts.PlacerInStack || !facts.PlacerIsSquad || facts.PlacerBerserk || facts.Exponent is null)
        {
            return new SmokeAttemptVerdict($"play.smoke-placer: {facts.PlacerId} is not a squad of the moving stack with a Smoke Placement Exponent (A24.1)", 0);
        }

        if (facts.AlreadyAttempted)
        {
            return new SmokeAttemptVerdict($"play.smoke-once: {facts.PlacerId} has already attempted to place SMOKE this MPh (A24.1)", 0);
        }

        if (facts.ResidualFpHere)
        {
            return new SmokeAttemptVerdict("play.smoke-residual: spending MF in a Residual FP Location to place SMOKE is not reviewed (A8.2)", 0);
        }

        // E3.53, E3.734 (referee, pass 16): in rain, Mud, or Deep Snow the only SMOKE is a Blaze's or SMOKE placed inside a building.
        if ((facts.Precipitation is "rain" or "heavy-rain" || facts.Mud || facts.DeepSnow) && facts.TargetTerrain is not ("wooden-building" or "stone-building"))
        {
            return new SmokeAttemptVerdict("play.smoke-weather: in rain, Mud, or Deep Snow no SMOKE is placed but inside a building (E3.53, E3.734)", 0);
        }

        // A24.1 (ruling R9.5): the own Location for 1 MF, or an ADJACENT Location at its level for 2 MF; no other level, water, or marsh.
        int halfMf;
        if (facts.TargetIsOwnLocation)
        {
            halfMf = 2;
        }
        else
        {
            if (!facts.StepReadable || !facts.SameElevation || !facts.SameLevel || facts.Cliff || facts.StepTerrain is not { } terrain
                || !ScenarioA1ResultTables.EntryHalfMf.ContainsKey(terrain))
            {
                return new SmokeAttemptVerdict($"play.smoke-target: SMOKE grenades go in the squad's Location or an ADJACENT reviewed Location at its level, not {facts.TargetText} (A24.1; ruling R9.5)", 0);
            }

            halfMf = 4;
        }

        foreach (var mover in facts.Movers)
        {
            if (mover.Allowance is not { } allowance || mover.PlainAllowance is not { } plain)
            {
                return new SmokeAttemptVerdict($"play.move-mf: {mover.UnitId} has no MF allowance the catalog decides", 0);
            }

            var left = (allowance * 2) - mover.HalfMfSpent;
            if (left < halfMf || (facts.Assault && (plain * 2) - mover.HalfMfSpent <= halfMf))
            {
                return new SmokeAttemptVerdict($"play.move-mf: {mover.UnitId} has {left / 2m} MF left, and the SMOKE attempt costs {halfMf / 2m} (A24.1, A4.61)", 0);
            }
        }

        return new SmokeAttemptVerdict(null, halfMf);
    }

    /// <summary>
    /// Whether a recorded SMOKE attempt is one attempt per MPh by a squad of the moving stack, in its Location, with its dr: the exponent is the
    /// catalog's, CX (or Double Time with this step) adds one, and the cost is 1 MF in the own Location and 2 in another.
    /// </summary>
    public static bool Verify(SmokeRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return !(!facts.PlacerInMovers || facts.AlreadyAttempted
            || !facts.StepEntersPlacerLocation || !facts.RollFound || facts.RollCount != 1
            || facts.RollValue != facts.RecordedDr || facts.RecordedExponent < 1 || !facts.PlacerActive
            || facts.CatalogExponent != facts.RecordedExponent
            || facts.RecordedCx != (facts.DoubleTime || facts.PlacerCx)
            || facts.HalfMf != (facts.TargetIsStepDestination ? 2 : 4));
    }
}
