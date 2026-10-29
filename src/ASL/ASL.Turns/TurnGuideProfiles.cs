using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

/// <summary>Explicit host integration scope, not a claim that omitted ASL rules are inapplicable.</summary>
public sealed record TurnGuideProfile(string Id, string Version, ImmutableArray<string> IntegratedDependencies,
    ImmutableArray<string> Evidence)
{
    public ImmutableArray<TurnHostCapabilityBound> Bounds => TurnHostCapabilityBounds.ForProfile(Id);

    public TurnSupport AssessedCommandSupport(string definition, bool hasOrigin, TurnObligationState state) =>
        state is TurnObligationState.Scheduled or TurnObligationState.Due or TurnObligationState.Resolved
            or TurnObligationState.Interrupted or TurnObligationState.Cancelled
            ? CommandSupport(definition, hasOrigin) : TurnSupport.Pending;

    public TurnSupport CommandSupport(string definition, bool hasOrigin) =>
        hasOrigin && IntegratedDependencies.Contains(definition, StringComparer.Ordinal)
            ? TurnSupport.IntegratedSubset : TurnSupport.Pending;
}

public static class TurnGuideProfiles
{
    public static TurnGuideProfile Get(string id) => id switch
    {
        "existing-engine" => new(id, "b5-r2-base", ["stun", "cx"], ["B4 PlayTests.TurnGuide; VehicleStepsTests"]),
        "existing-engine-dc-v1" => new(id, "b5-r5-dc", ["stun", "cx", "placedDc"],
            ["A23.3-.4; ASOP 5.22A", "BacklogPass15Tests; TurnObligationRulesTests", "dcOwnerSide event field; legacy null remains incomplete"]),
        "existing-engine-fire-v1" => new(id, "wire-fire-1", ["stun", "cx", "placedDc"],
            ["B5 fire and Spotter history; C9.3-.31; A.15", "BacklogPass9Tests; TurnFireHistoryTests",
             "Versioned original/replacement Spotter declarations and spotted-fire marks; Opportunity Fire remains pending"]),
        "existing-engine-atmosphere-v1" => new(id, "b5-r5-atmosphere", ["stun", "cx", "placedDc"],
            ["A24.1-.11; ordinary infantry grenade attempts and MPh cleanup", "BacklogPass9Tests; TurnAtmosphereTests",
             "Recorded WindChanged/Gust history; white SMOKE, drift, cave WP and dust execution pending"]),
        "existing-engine-work-v1" => new(id, "b5-work-8", ["stun", "cx", "placedDc"],
            ["E10.3-.4 progress duration; TurnWorkProgressTests", "Permissioned work adjudication and opt-in packing assembly/replenishment and supply transfer/offload effects; automatic eligibility, animal transport and full Ammo Dump use remain pending"]),
        _ => throw new ArgumentException("Unknown turn guide profile.", nameof(id)),
    };
}
