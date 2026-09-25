using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Scenario A1 snapshot sources over a live game (Occupied and Concealed Entry Design, section 4). The planner
/// builds one snapshot from the whole-game state it plans against; the source returns it only for the exact tenant,
/// package, unit, and location it describes, so a provider can never read another case from it.
/// </summary>
public sealed class LiveBoardSnapshotSource(ScenarioA1BoardSnapshot snapshot) : IScenarioA1BoardSnapshotSource
{
    public ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return ValueTask.FromResult(snapshot.TenantId == tenantId && snapshot.Package == package && snapshot.UnitId == unitId && snapshot.LocationId == locationId
            ? snapshot
            : null);
    }
}

/// <summary>The post-reveal snapshot source over a live game's candidate state after the planned reveal.</summary>
public sealed class LivePostRevealSnapshotSource(ScenarioA1PostRevealSnapshot snapshot) : IScenarioA1PostRevealSnapshotSource
{
    public ValueTask<ScenarioA1PostRevealSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package, string unitId, string locationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return ValueTask.FromResult(snapshot.TenantId == tenantId && snapshot.Package == package && snapshot.UnitId == unitId && snapshot.LocationId == locationId
            ? snapshot
            : null);
    }
}
