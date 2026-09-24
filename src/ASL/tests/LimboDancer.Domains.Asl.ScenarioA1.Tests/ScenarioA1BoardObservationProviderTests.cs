using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1BoardObservationProviderTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task VersionedBoardSnapshotDerivesOnlyItsActualReviewedCase()
    {
        var empty = Snapshot() with { Occupancy = ScenarioA1BoardOccupancy.KnownEmpty };
        var result = await new ScenarioA1BoardObservationProvider(new StubSource(empty))
            .ObserveAsync(Query());
        var observation = Assert.Single(result.Observations);
        Assert.Equal("asl.a1.board.exact-case:A1-empty-ordinary-mph", Assert.Single(result.ReasonCodes));
        Assert.Equal("board-engine", observation.Provenance);
        Assert.Equal("state-1", observation.Version);
        var facts = Facts(observation);
        Assert.Equal("eligible-2mf", new ScenarioA1SemanticCandidate()
            .Evaluate("A1-empty-ordinary-mph", facts).Disposition);
        Assert.Equal("indeterminate", new ScenarioA1SemanticCandidate()
            .Evaluate("A1-known-enemy-mmc-mph", facts).Disposition);

        var enemy = empty with
        {
            Occupancy = ScenarioA1BoardOccupancy.KnownUnconcealedEnemyMmc,
            HasNoA414Exception = true,
        };
        var enemyResult = await new ScenarioA1BoardObservationProvider(new StubSource(enemy))
            .ObserveAsync(Query());
        Assert.Equal("asl.a1.board.exact-case:A1-known-enemy-mmc-mph",
            Assert.Single(enemyResult.ReasonCodes));
        Assert.Equal("prohibited", new ScenarioA1SemanticCandidate().Evaluate(
            "A1-known-enemy-mmc-mph", Facts(Assert.Single(enemyResult.Observations))).Disposition);
    }

    [Fact]
    public async Task UnknownModifierVersionTenantAndCallerSelectedLabelProduceNoObservation()
    {
        var state = Snapshot();
        foreach (var changed in new[]
        {
            state with { HasNoSpecialModifier = null },
            state with { Occupancy = ScenarioA1BoardOccupancy.Unknown },
            state with { Version = "state-2" },
            state with { TenantId = Guid.NewGuid() },
            state with { SourceId = "" },
        })
        {
            var result = await new ScenarioA1BoardObservationProvider(new StubSource(changed))
                .ObserveAsync(Query());
            Assert.Empty(result.Observations);
            Assert.Single(result.ReasonCodes);
        }
        var selected = new ObservationQuery("query", Tenant, ScenarioA1OccupiedPackage.Identity,
            new SemanticIdentifier(new DomainId("asl"), ScenarioA1BoardObservationProvider.QueryKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "building", observationVersion = "state-1",
                caseId = "A1-empty-ordinary-mph",
            }), 1);
        Assert.Empty((await new ScenarioA1BoardObservationProvider(new StubSource(state))
            .ObserveAsync(selected)).Observations);
    }

    private static Dictionary<string, string> Facts(Observation observation) =>
        observation.Data.EnumerateObject().ToDictionary(item => item.Name,
            item => item.Value.GetString()!, StringComparer.Ordinal);

    private static ObservationQuery Query() => new("query", Tenant, ScenarioA1OccupiedPackage.Identity,
        new SemanticIdentifier(new DomainId("asl"), ScenarioA1BoardObservationProvider.QueryKind),
        JsonSerializer.SerializeToElement(new
        {
            unitId = "squad", locationId = "building", observationVersion = "state-1",
        }), 1);

    private static ScenarioA1BoardSnapshot Snapshot() => new(Tenant, ScenarioA1OccupiedPackage.Identity,
        "squad", "building", "state-1", Now, "board-engine",
        ScenarioA1BoardOccupancy.KnownEmpty,
        true, true, true, true, true, true, true, true, true, true);

    private sealed class StubSource(ScenarioA1BoardSnapshot snapshot) : IScenarioA1BoardSnapshotSource
    {
        public ValueTask<ScenarioA1BoardSnapshot?> ReadAsync(Guid tenantId, DomainPackageRef package,
            string unitId, string locationId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ScenarioA1BoardSnapshot?>(snapshot);
    }
}
