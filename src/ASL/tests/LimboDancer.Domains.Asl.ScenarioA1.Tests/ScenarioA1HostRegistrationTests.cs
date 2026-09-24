using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.Execution;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Host;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1HostRegistrationTests
{
    [Fact]
    public void HostRequiresExplicitAuthorityAndKeepsDefaultConfirmationGate()
    {
        var services = new ServiceCollection();
        services.AddLimboDancerHost(new ConfigurationBuilder().Build());
        using (var normal = services.BuildServiceProvider())
        {
            Assert.False(normal.GetRequiredService<IActionRegistry>().TryGet(
                ScenarioA1ReturnAction.Id, ScenarioA1ReturnAction.Version, out _));
        }

        var authority = new NoCaseAuthority();
        var store = new ScenarioA1InMemoryReturnStore([]);
        services.AddScenarioA1Return(authority, authority, store);
        using var optedIn = services.BuildServiceProvider();
        Assert.True(optedIn.GetRequiredService<IActionRegistry>().TryGet(
            ScenarioA1ReturnAction.Id, ScenarioA1ReturnAction.Version, out var descriptor));
        Assert.Same(ScenarioA1ReturnAction.Descriptor, descriptor);
        Assert.True(optedIn.GetRequiredService<IActionBindingRegistry>().TryResolve(
            "asl", "scenario-a1-second-defender-return", out var binding));
        Assert.Equal(ScenarioA1ReturnAction.Id, binding.ActionId);
        Assert.Empty(optedIn.GetRequiredService<IActionBindingRegistry>().List("mcp")
            .Where(item => item.ActionId == ScenarioA1ReturnAction.Id));
        Assert.IsType<DefaultExecutionRiskPolicy>(optedIn.GetRequiredService<IExecutionRiskPolicy>());
        Assert.True(optedIn.GetRequiredService<RuntimeStructureValidator>().Validate().IsValid);
    }

    [Fact]
    public async Task UntrustedOrUnavailableTicketsNeverYieldAConclusion()
    {
        var authority = new NoCaseAuthority();
        var source = new ScenarioA1VerifiedReturnConclusionSource(authority, authority,
            TimeProvider.System);
        Assert.Null(await source.ReadAsync(Guid.NewGuid(), "opaque"));
        authority.Ticket = new ScenarioA1ReturnCase(Guid.NewGuid(), "question", "unit",
            "bd01:E4:0", "bd01:D4:0", "snapshot-1",
            "A1-second-defender-consequence-smc-revealed", []);
        Assert.Null(await source.ReadAsync(Guid.NewGuid(),
            "asl-a1-second-defender-consequence:question"));
    }

    [Fact]
    public async Task AuthoritativeCaseAndEventsProduceOnlyTheReviewedConclusion()
    {
        var tenant = Guid.NewGuid();
        var package = ScenarioA1SecondDefenderConsequencePackage.Identity;
        var descriptor = (await new ScenarioA1SecondDefenderConsequencePackage()
            .ResolveAsync(package)).Package!;
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, tenant, package,
                new SemanticIdentifier(new DomainId("asl"), "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(new DomainId("asl"), id),
                descriptor.CanonicalSources[0], new EvidenceReference("entity:" + id,
                    EvidenceKind.CanonicalSource, tenant, package, id, package.Version, "game"))],
            ["game.authority"]);
        var authority = new NoCaseAuthority
        {
            Ticket = new ScenarioA1ReturnCase(tenant, "question", "squad",
                "bd01:E4:0", "bd01:D4:0", "snapshot-1",
                "A1-second-defender-consequence-smc-revealed",
                [Entity("squad"), Entity("bd01:E4:0"), Entity("bd01:D4:0")]),
            Snapshot = new ScenarioA1SecondDefenderConsequenceSnapshot(package,
                new ScenarioA1SecondDefenderSnapshot(tenant,
                    ScenarioA1SecondDefenderPackage.Identity, "squad", "bd01:E4:0",
                    "snapshot-1", DateTimeOffset.UtcNow.AddMinutes(-1), "game-events",
                    new ScenarioA1TerrainBinding("01", Board01TerrainCatalog.BoardVersion,
                        Board01TerrainCatalog.MetadataGitBlobSha, "E4", 0, null))
                {
                    InitialOccupancy = ScenarioA1InitialConcealedOccupancy.Concealed,
                    A1215FirstRevealOccurred = true, FirstRevealedUnitId = "first-smc",
                    FirstRevealOrdinal = 3, FirstRevealedType = ScenarioA1RevealedOccupant.EnemySmc,
                    OverrunElection = ScenarioA1OverrunElection.Elected, ElectionOrdinal = 4,
                    SecondDefenderUnitId = "second-unit", SecondDefenderLocationId = "bd01:E4:0",
                    SecondDefenderState = ScenarioA1SecondDefenderState.RevealedSmc,
                    SecondRevealOrdinal = 5, Ntc = ScenarioA1OverrunNtc.Passed, NtcOrdinal = 1,
                    MfAtSecondReveal = ScenarioA1OverrunMf.AtLeastFour,
                    IsMovementPhase = true, IsGoodOrderUnconcealedNonDummyInfantryMmc = true,
                    IsAdjacentGroundLevelOrdinaryBuilding = true,
                    IsOrdinaryObstacleEntryNotBypass = true, HasNoA414Exception = true,
                    IsFirstSmcOutsideAfv = true, HasNoSpecialModifier = true,
                    HasNoLeaderExemption = true,
                }, "bd01:D4:0", true, 2, 2, false),
        };
        var source = new ScenarioA1VerifiedReturnConclusionSource(authority, authority,
            TimeProvider.System);
        var conclusion = await source.ReadAsync(tenant,
            "asl-a1-second-defender-consequence:question");
        Assert.NotNull(conclusion);
        Assert.Equal(ConclusionDisposition.Definitive, conclusion.Disposition);
        Assert.Equal("bd01:D4:0", conclusion.Value!.Value
            .GetProperty("returnToLocationId").GetString());
        authority.Snapshot = authority.Snapshot with
        {
            IsPreviousLocationLastOccupied = null,
        };
        Assert.Null(await source.ReadAsync(tenant,
            "asl-a1-second-defender-consequence:question"));
    }

    private sealed class NoCaseAuthority : IScenarioA1ReturnCaseSource,
        IScenarioA1SecondDefenderConsequenceSnapshotSource
    {
        public ScenarioA1ReturnCase? Ticket { get; set; }
        public ScenarioA1SecondDefenderConsequenceSnapshot? Snapshot { get; set; }

        public ValueTask<ScenarioA1ReturnCase?> ReadAsync(Guid tenantId,
            string conclusionId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Ticket);

        public ValueTask<ScenarioA1SecondDefenderConsequenceSnapshot?> ReadAsync(
            Guid tenantId, DomainPackageRef package, string unitId, string locationId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Snapshot);
    }
}
