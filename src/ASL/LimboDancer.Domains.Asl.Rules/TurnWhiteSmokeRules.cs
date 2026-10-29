namespace LimboDancer.Domains.Asl.Rules;

public enum TurnSmokeSource
{
    Unknown, Ordnance, OffboardArtillery, SmokeDischarger, SmokeMortar, SmokePots, CloseDefenseWeapon,
    InfantryGrenade, VehicleGrenade, Drift, Blaze, Cave, Napalm
}
public enum TurnSmokeChemical
{
    Unknown, Conventional, WhitePhosphorus
}
public enum TurnSmokeSize
{
    Unknown, HalfInch, FiveEighthsInch
}
public enum TurnSmokeColor
{
    Unknown, White, Gray
}
public enum TurnSmokeStrength
{
    Unknown, Full, Dispersed
}
public enum TurnWhiteSmokeEffect
{
    NotEvaluated, Unchanged, Remove, Disperse
}

/// <summary>Authoritative source facts. Source identity and causal evidence must be supplied by the host, not inferred from an entity name.</summary>
public sealed record TurnWhiteSmokeFacts(string Counter, string? OwnerSide, TurnSmokeSource Source,
    TurnSmokeChemical Chemical, TurnSmokeSize Size, TurnSmokeColor Color, TurnSmokeStrength Strength,
    TurnRulePosition? Origin, bool Active, TurnRulePosition? LastCleanup = null);
public sealed record TurnWhiteSmokeVerdict(string Counter, TurnWhiteSmokeEffect Effect, string Reason)
{
    public string Reference { get; } = "A24.4-.5; A24.61; ASOP 2.11A";
}
public sealed record TurnWhiteSmokePlacement(TurnSmokeStrength Strength, int? Hindrance, string Reason);

/// <summary>Original white-counter strength and owner-relative opening cleanup only. This does not authorize placement or resolve WP/weather/drift consequences.</summary>
public static class TurnWhiteSmokeRules
{
    private static readonly string[] Phases = ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];

    /// <summary>A24.5's placement side, decided by <see cref="ScenarioA1SmokeStrengthCalculator.PlacementStrength"/> (the A24.5 claim, RA24.3): the same answer.</summary>
    public static TurnWhiteSmokePlacement InitialStrength(TurnSmokeSource source, TurnSmokeChemical chemical,
        bool ownerPrepFire, bool? beforeNonSmokeFire) =>
        ScenarioA1SmokeStrengthCalculator.PlacementStrength(source, chemical, ownerPrepFire, beforeNonSmokeFire);

    public static TurnWhiteSmokeVerdict Evaluate(TurnWhiteSmokeFacts facts, TurnRulePosition now)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentException.ThrowIfNullOrWhiteSpace(facts.Counter);
        TurnObligationRules.ValidatePosition(now);
        if (!Enum.IsDefined(facts.Source) || !Enum.IsDefined(facts.Chemical) || !Enum.IsDefined(facts.Size)
            || !Enum.IsDefined(facts.Color) || !Enum.IsDefined(facts.Strength))
        {
            throw new ArgumentException("Unknown SMOKE classification.", nameof(facts));
        }
        if (facts.Origin is { } origin)
        {
            ValidatePast(origin, now);
        }
        if (facts.LastCleanup is { } applied)
        {
            ValidatePast(applied, now);
            if (applied.Phase != "pfph" || applied.Checkpoint != TurnCheckpoint.PrepFireOpening
                || applied.PhasingSide != facts.OwnerSide || facts.Origin is null
                || applied.PlayerTurn < facts.Origin.PlayerTurn
                || applied.PlayerTurn == facts.Origin.PlayerTurn && facts.Origin.Phase != "rph")
            {
                throw new ArgumentException("A cleanup receipt must identify a later owner PFPh opening.", nameof(facts));
            }
        }
        TurnWhiteSmokeVerdict Result(TurnWhiteSmokeEffect effect, string reason) => new(facts.Counter, effect, reason);
        if (!facts.Active)
        {
            return Result(TurnWhiteSmokeEffect.Unchanged, "The counter is already inactive; this assessment does not infer why it was removed.");
        }
        if (!Supported(facts.Source) || facts.Chemical == TurnSmokeChemical.Unknown || string.IsNullOrWhiteSpace(facts.OwnerSide)
            || facts.Size != TurnSmokeSize.FiveEighthsInch || facts.Color != TurnSmokeColor.White
            || facts.Strength == TurnSmokeStrength.Unknown || facts.Origin is null
            || Dispenser(facts.Source) && facts.Chemical != TurnSmokeChemical.Conventional)
        {
            return Result(TurnWhiteSmokeEffect.NotEvaluated, "Prove owner, original white source, 5/8-inch size, chemical, strength and origin. Other source families remain pending.");
        }
        if (now.Phase != "pfph" || now.Checkpoint != TurnCheckpoint.PrepFireOpening || now.PhasingSide != facts.OwnerSide)
        {
            return Result(TurnWhiteSmokeEffect.Unchanged, "Owner-relative PFPh opening cleanup is not due at this position; other weather/source rules remain separate.");
        }
        if (facts.Origin.PlayerTurn == now.PlayerTurn && facts.Origin.Phase == "pfph" || facts.LastCleanup?.PlayerTurn == now.PlayerTurn)
        {
            return Result(TurnWhiteSmokeEffect.Unchanged, "This counter was created or already processed in this opening's Player Turn; do not process it twice.");
        }
        return facts.Strength == TurnSmokeStrength.Dispersed
            ? Result(TurnWhiteSmokeEffect.Remove, "Remove previously dispersed white SMOKE before dispersing the owner's remaining full-strength counters.")
            : Result(TurnWhiteSmokeEffect.Disperse, "After removals, disperse this full-strength white counter. Resolve linked drift effects separately.");
    }

    /// <summary>Assess one immutable opening snapshot, with all removals before any dispersal. Unknowns are retained in the result.</summary>
    public static IReadOnlyList<TurnWhiteSmokeVerdict> Opening(IReadOnlyList<TurnWhiteSmokeFacts> counters, TurnRulePosition now)
    {
        ArgumentNullException.ThrowIfNull(counters);
        TurnObligationRules.ValidatePosition(now);
        if (counters.Select(item => item.Counter).Distinct(StringComparer.Ordinal).Count() != counters.Count)
        {
            throw new ArgumentException("An opening snapshot cannot repeat a counter.", nameof(counters));
        }
        return [.. counters.Select(item => Evaluate(item, now)).OrderBy(item => item.Effect switch
        {
            TurnWhiteSmokeEffect.Remove => 0,
            TurnWhiteSmokeEffect.Disperse => 1,
            _ => 2
        })];
    }

    private static bool Supported(TurnSmokeSource source) => ScenarioA1SmokeStrengthCalculator.Supported(source);
    private static bool Dispenser(TurnSmokeSource source) => ScenarioA1SmokeStrengthCalculator.Dispenser(source);

    private static void ValidatePast(TurnRulePosition past, TurnRulePosition now)
    {
        TurnObligationRules.ValidatePosition(past);
        if (past.GameTurn > now.GameTurn || past.PlayerTurn > now.PlayerTurn
            || past.PlayerTurn == now.PlayerTurn && (past.GameTurn != now.GameTurn || past.PhasingSide != now.PhasingSide
                || Array.IndexOf(Phases, past.Phase) > Array.IndexOf(Phases, now.Phase)))
        {
            throw new ArgumentException("SMOKE evidence must belong to a coherent preceding turn position.", nameof(past));
        }
    }
}
