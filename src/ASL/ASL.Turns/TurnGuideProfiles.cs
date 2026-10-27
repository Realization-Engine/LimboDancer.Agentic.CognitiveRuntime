using System.Collections.Immutable;

namespace ASL.Turns;

/// <summary>Explicit host integration scope, not a claim that omitted ASL rules are inapplicable.</summary>
public sealed record TurnGuideProfile(string Id, string Version, ImmutableArray<string> IntegratedDependencies,
    ImmutableArray<string> Evidence)
{
    public TurnSupport CommandSupport(string definition, bool hasOrigin) =>
        hasOrigin && IntegratedDependencies.Contains(definition, StringComparer.Ordinal)
            ? TurnSupport.IntegratedSubset : TurnSupport.Pending;
}

public static class TurnGuideProfiles
{
    public static TurnGuideProfile Get(string id) => id switch
    {
        "existing-engine" => new(id, "b4", ["stun", "cx"], ["B4 PlayTests.TurnGuide; VehicleStepsTests"]),
        "existing-engine-dc-v1" => new(id, "b5-dc-1", ["stun", "cx", "placedDc"],
            ["A23.3-.4; ASOP 5.22A", "BacklogPass15Tests; TurnObligationRulesTests", "dcOwnerSide event field; legacy null remains incomplete"]),
        "existing-engine-fire-v1" => new(id, "b5-fire-1", ["stun", "cx", "placedDc"],
            ["B5 fire and Spotter history; C9.3-.31; A.15", "BacklogPass9Tests; TurnFireHistoryTests",
             "Existing fire-resolved/ordnance-fired events; Spotter replacement and Opportunity Fire remain pending"]),
        "existing-engine-atmosphere-v1" => new(id, "b5-atmosphere-1", ["stun", "cx", "placedDc"],
            ["A24.1-.11; ordinary infantry grenade attempts and MPh cleanup", "BacklogPass9Tests; TurnAtmosphereTests",
             "Recorded WindChanged/Gust history; white SMOKE, drift, cave WP and dust execution pending"]),
        _ => throw new ArgumentException("Unknown turn guide profile.", nameof(id)),
    };
}
