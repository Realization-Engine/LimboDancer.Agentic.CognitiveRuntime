using System.Collections.Immutable;

namespace ASL.Turns;

public enum TurnSmokeState
{
    NotEvaluated, FailedAttempt, Active, RemovedAtMovementEnd
}
public enum TurnAtmosphereFamily
{
    WhiteSmoke, Drift, Gusts, CaveWp, VehicleDust, AmbientDust, FfeDust
}

/// <summary>Disclosed, event-backed ordinary infantry grenade evidence. Never use for unclassified or cave smoke.</summary>
public sealed record TurnGrenadeFacts(string Unit, string OwnerSide, int GameTurn, string Phase, bool Placed,
    string? Counter, bool? Active, bool MovementCleanupRecorded, bool AttemptInCurrentPhase);
public sealed record TurnGrenadeHistory(string Unit, string OwnerSide, int GameTurn, string Phase,
    string? Counter, TurnSmokeState State, bool AttemptSpentThisPhase);
public sealed record TurnWindHistory(int GameTurn, string PhasingSide, string Phase, bool Gust);
public sealed record TurnAtmosphereGap(TurnAtmosphereFamily Family, string DependencyId, string Reference,
    string Timing, string MissingFacts, TurnSupport CommandSupport);
public sealed record TurnAtmosphereSnapshot(ImmutableArray<TurnGrenadeHistory> Grenades,
    ImmutableArray<TurnWindHistory> Wind, ImmutableArray<string> UnclassifiedSmoke,
    ImmutableArray<TurnAtmosphereGap> PendingRules);

/// <summary>Pure read composition. Stored cleanup evidence is required; counter absence is not proof of expiration.</summary>
public static class TurnAtmosphere
{
    public static TurnAtmosphereSnapshot Compose(ImmutableArray<TurnGrenadeFacts> grenades,
        ImmutableArray<TurnWindHistory> wind, ImmutableArray<string> unclassifiedSmoke)
    {
        if (grenades.IsDefault || wind.IsDefault || unclassifiedSmoke.IsDefault)
        {
            throw new ArgumentException("Supply disclosed atmospheric evidence, even when empty.");
        }
        var history = grenades.Select(item => new TurnGrenadeHistory(item.Unit, item.OwnerSide, item.GameTurn, item.Phase,
            item.Counter, State(item), item.AttemptInCurrentPhase)).ToImmutableArray();
        return new(history, wind, unclassifiedSmoke,
        [
            new(TurnAtmosphereFamily.WhiteSmoke, "smoke", "A24.4-.62; ASOP 2.11A",
                "At owner's PFPh opening remove previously dispersed white SMOKE before dispersing the remainder.",
                "Owner, source, size/type and dispersed-state history; no timing inferred from an unclassified counter.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.Drift, "driftGusts", "A24.61-.62; ASOP 1.12A, 5.11B",
                "Invalidate source-dependent drift immediately; create eligible drift at first game RPh and AFPh 5.11 before WP and Gusts.",
                "Source links, wind direction/force, source dispersal and furthest-counter identity.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.Gusts, "driftGusts", "B25.651; ASOP 1.12A, 5.13A",
                "Keep recorded RPh Gusts for the later AFPh effect; do not reroll. AFPh drift precedes WP, which precedes Gusts.",
                "Applied-effect receipts and eligible non-cave sources; wind history alone does not certify cleanup.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.CaveWp, "caveWp", "G11.85-.851",
                "Half-inch cave smoke survives MPh; expire after concealment at Player Turn end, then resolve WP connection disclosure.",
                "Cave, WP/type/size, origin and connected higher caves/tunnel openings; exclude ordinary drift/Gusts rules.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.VehicleDust, "vehicleDust", "F11.74-.741; ASOP 1.12A, 2.11A",
                "Source departure/wind event, next friendly PFPh status check, or continuing-Motion vehicle's next-MPh initial MP.",
                "Source vehicle, creation/shift/impulse, Motion and disabling-status histories.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.AmbientDust, "vehicleDust", "F11; ASOP 1.12A",
                "Use ambient-dust intensity rules, not SMOKE expiration or Vehicle Dust movement.",
                "Desert profile and intensity-change facts.", TurnSupport.Pending),
            new(TurnAtmosphereFamily.FfeDust, "vehicleDust", "F11; ASOP 1.12A",
                "Keep FFE Dust separate from Vehicle Dust and ordinary wind/Gusts removal.",
                "FFE source and its specific duration/effect evidence.", TurnSupport.Pending),
        ]);
    }

    private static TurnSmokeState State(TurnGrenadeFacts item)
    {
        if (!item.Placed)
        {
            return TurnSmokeState.FailedAttempt;
        }
        if (item.Counter is null)
        {
            return TurnSmokeState.NotEvaluated;
        }
        if (item.MovementCleanupRecorded)
        {
            return TurnSmokeState.RemovedAtMovementEnd;
        }
        return item.Active == true ? TurnSmokeState.Active : TurnSmokeState.NotEvaluated;
    }
}
