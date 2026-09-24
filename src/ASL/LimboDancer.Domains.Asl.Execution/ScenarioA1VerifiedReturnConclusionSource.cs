using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Execution;

/// <summary>A server-owned case ticket, including entity resolutions from the game authority.</summary>
public sealed record ScenarioA1ReturnCase(
    Guid TenantId, string QuestionId, string UnitId, string LocationId,
    string PreviousLocationId, string ObservationVersion, string CaseId,
    IReadOnlyList<DomainEntityResolution> EntityResolutions);

public interface IScenarioA1ReturnCaseSource
{
    ValueTask<ScenarioA1ReturnCase?> ReadAsync(Guid tenantId, string conclusionId,
        CancellationToken cancellationToken = default);
}

/// <summary>Rebuilds the conclusion from a trusted ticket and current authoritative events.</summary>
public sealed class ScenarioA1VerifiedReturnConclusionSource(
    IScenarioA1ReturnCaseSource cases,
    IScenarioA1SecondDefenderConsequenceSnapshotSource snapshots,
    TimeProvider clock) : IScenarioA1ReturnConclusionSource
{
    public async ValueTask<DomainConclusion?> ReadAsync(Guid tenantId, string conclusionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ticket = await cases.ReadAsync(tenantId, conclusionId, cancellationToken);
        if (ticket is null || ticket.TenantId != tenantId
            || conclusionId != "asl-a1-second-defender-consequence:" + ticket.QuestionId
            || ticket.EntityResolutions is null || ticket.EntityResolutions.Count != 3
            || ticket.EntityResolutions.Any(item => item.Query.TenantId != tenantId
                || item.Query.Package != ScenarioA1SecondDefenderConsequencePackage.Identity))
            return null;

        var package = ScenarioA1SecondDefenderConsequencePackage.Identity;
        var parameters = JsonSerializer.SerializeToElement(new
        {
            unitId = ticket.UnitId, locationId = ticket.LocationId,
            previousLocationId = ticket.PreviousLocationId,
            observationVersion = ticket.ObservationVersion, caseId = ticket.CaseId,
        });
        var observation = await new ScenarioA1SecondDefenderConsequenceObservationProvider(
            snapshots, new Board01TerrainCatalog()).ObserveAsync(new ObservationQuery(
                ticket.QuestionId, tenantId, package,
                new SemanticIdentifier(new DomainId("asl"),
                    ScenarioA1SecondDefenderConsequenceObservationProvider.QueryKind),
                parameters, 1), cancellationToken);
        if (observation.Observations.Count != 1)
            return null;
        var descriptor = (await new ScenarioA1SecondDefenderConsequencePackage()
            .ResolveAsync(package, cancellationToken)).Package;
        if (descriptor is null)
            return null;
        var question = new DomainQuestion(ticket.QuestionId, tenantId, package,
            new SemanticIdentifier(new DomainId("asl"),
                ScenarioA1SecondDefenderConsequenceConclusionResolver.QuestionKind),
            parameters, clock.GetUtcNow());
        var result = await new ScenarioA1SecondDefenderConsequenceConclusionResolver()
            .ConcludeAsync(new DomainConclusionContext(question, descriptor,
                ticket.EntityResolutions, observation.Observations), cancellationToken);
        return result.ConclusionId == conclusionId
            && result.Disposition == ConclusionDisposition.Definitive ? result : null;
    }
}
