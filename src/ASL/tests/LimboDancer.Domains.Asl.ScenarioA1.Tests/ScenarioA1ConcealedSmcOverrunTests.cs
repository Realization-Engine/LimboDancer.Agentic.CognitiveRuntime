using System.Text.Json;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Abstractions.Observations;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1ConcealedSmcOverrunTests
{
    private static readonly Guid Tenant = Guid.Parse("e753fdf9-d585-45cf-a7fa-d0f2cf625276");
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
    private const string Qualified = "A1-concealed-smc-qualified-response-unresolved";

    [Fact]
    public async Task ExactRevealProvenancePermitsOnlyReadOnlyQualifiedAttempt()
    {
        var package = new ScenarioA1ConcealedSmcOverrunPackage();
        var descriptor = (await package.ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Package!;
        Assert.Equal(6, descriptor.CanonicalSources.Count);
        Assert.Equal(DomainPackageResolutionOutcome.Unavailable,
            (await package.ResolveAsync(ScenarioA1PostRevealPackage.Identity)).Outcome);
        var facts = Facts();
        var observation = Observation(facts);
        var before = observation.Data.GetRawText();
        var conclusion = await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
            .ConcludeAsync(Context(descriptor, observation, Qualified));
        Assert.Equal(ConclusionDisposition.Qualified, conclusion.Disposition);
        Assert.Equal(Qualified, conclusion.Value!.Value.GetProperty("caseId").GetString());
        Assert.Equal("A12.15-immediate-defender-reveal",
            conclusion.Value.Value.GetProperty("revealProvenance").GetString());
        Assert.Equal(4, conclusion.Value.Value.GetProperty("entryMfRequired").GetInt32());
        Assert.True(conclusion.Value.Value.GetProperty("attemptOnly").GetBoolean());
        Assert.Equal("defenderResponseOrImmediateCloseCombat",
            conclusion.Value.Value.GetProperty("nextResolution").GetString());
        Assert.Equal(6, conclusion.ApplicableRules.Count);
        Assert.Contains(conclusion.ApplicableRules, item => item.ElementId == "A12.15");
        Assert.Contains(conclusion.ApplicableRules, item => item.ElementId == "A4.151");
        Assert.Contains(conclusion.ApplicableRules, item => item.ElementId == "A4.152");
        Assert.Contains(conclusion.Evidence, item => item.Kind == EvidenceKind.Observation
            && item.Version == "snapshot-1");
        Assert.Equal(before, observation.Data.GetRawText());
    }

    [Theory]
    [InlineData("revealProvenance", "known-from-outset", ConclusionDisposition.Abstained)]
    [InlineData("soleEnemySmcOccupancy", "unknown", ConclusionDisposition.Abstained)]
    [InlineData("remainingMf", "insufficient", ConclusionDisposition.Abstained)]
    [InlineData("additionalDefenderReveal", "anotherNonDummyRevealed", ConclusionDisposition.Abstained)]
    [InlineData("defenderResponseOrImmediateCc", "resolvedWithOutcome", ConclusionDisposition.Abstained)]
    [InlineData("extraModifier", "yes", ConclusionDisposition.Abstained)]
    public async Task ContraryOrExtraFactCannotInheritQualifiedAttempt(string key, string value,
        ConclusionDisposition expected)
    {
        var facts = Facts();
        facts[key] = value;
        var descriptor = await Descriptor();
        var conclusion = await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
            .ConcludeAsync(Context(descriptor, Observation(facts), Qualified));
        Assert.Equal(expected, conclusion.Disposition);
        Assert.Null(conclusion.Value);
        Assert.Empty(conclusion.ApplicableRules);
    }

    [Fact]
    public async Task MissingOrStaleControllingEvidenceStaysIndeterminate()
    {
        var descriptor = await Descriptor();
        var resolver = new ScenarioA1ConcealedSmcOverrunConclusionResolver();
        var facts = Facts();
        facts.Remove("soleEnemySmcOccupancy");
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(facts), Qualified))).Disposition);
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, Observation(Facts(), "snapshot-2"),
                Qualified))).Disposition);
        Assert.Equal(ConclusionDisposition.Indeterminate,
            (await resolver.ConcludeAsync(Context(descriptor, null, Qualified))).Disposition);
    }

    [Theory]
    [InlineData("A1-concealed-smc-election-unknown", ConclusionDisposition.Indeterminate)]
    [InlineData("A1-concealed-smc-ntc-unresolved", ConclusionDisposition.Indeterminate)]
    [InlineData("A1-concealed-smc-ntc-failed", ConclusionDisposition.Indeterminate)]
    [InlineData("A1-concealed-smc-mf-insufficient", ConclusionDisposition.Abstained)]
    [InlineData("A1-concealed-smc-other-reveal-unresolved", ConclusionDisposition.Indeterminate)]
    [InlineData("A1-concealed-smc-another-defender-revealed", ConclusionDisposition.Indeterminate)]
    [InlineData("A1-concealed-smc-declined", ConclusionDisposition.Abstained)]
    public async Task ExactNondefinitiveCasesDoNotGrantEntry(string caseId,
        ConclusionDisposition expected)
    {
        var descriptor = await Descriptor();
        var facts = Facts();
        foreach (var key in new[] { "overrunElection", "ntc", "remainingMf",
            "additionalDefenderReveal", "soleEnemySmcOccupancy", "defenderResponseOrImmediateCc" })
            facts.Remove(key);
        foreach (var item in CaseFacts(caseId))
            facts.Add(item.Key, item.Value);
        var conclusion = await new ScenarioA1ConcealedSmcOverrunConclusionResolver()
            .ConcludeAsync(Context(descriptor, Observation(facts), caseId));
        Assert.Equal(expected, conclusion.Disposition);
        Assert.Null(conclusion.Value);
        Assert.Empty(conclusion.ApplicableRules);
    }

    private static Dictionary<string, string> CaseFacts(string caseId) => caseId switch
    {
        "A1-concealed-smc-election-unknown" => new() { ["overrunElection"] = "unknown" },
        "A1-concealed-smc-declined" => new() { ["overrunElection"] = "declined" },
        "A1-concealed-smc-ntc-unresolved" => new() { ["overrunElection"] = "elected", ["ntc"] = "unresolved" },
        "A1-concealed-smc-ntc-failed" => new() { ["overrunElection"] = "elected", ["ntc"] = "failed" },
        "A1-concealed-smc-mf-insufficient" => new() { ["overrunElection"] = "elected", ["ntc"] = "passed", ["remainingMf"] = "insufficient" },
        "A1-concealed-smc-other-reveal-unresolved" => new() { ["overrunElection"] = "elected", ["ntc"] = "passed", ["remainingMf"] = "atLeastFour", ["additionalDefenderReveal"] = "unresolved" },
        "A1-concealed-smc-another-defender-revealed" => new() { ["overrunElection"] = "elected", ["ntc"] = "passed", ["remainingMf"] = "atLeastFour", ["additionalDefenderReveal"] = "anotherNonDummyRevealed" },
        _ => throw new InvalidOperationException("Unexpected case."),
    };

    private static Dictionary<string, string> Facts() => new(StringComparer.Ordinal)
    {
        ["board"] = "bd01-ground-level-ordinary-building",
        ["phase"] = "mph", ["attacker"] = "goodOrderUnconcealedNonDummyInfantryMmc",
        ["entryMode"] = "ordinaryObstacleEntryNotBypass", ["a414Exception"] = "none",
        ["initialDefenderState"] = "concealed",
        ["revealProvenance"] = "A12.15-immediate-defender-reveal",
        ["revealedOccupant"] = "oneEnemySmc", ["otherModifier"] = "none",
        ["leaderExemption"] = "none", ["entryMfCost"] = "2",
        ["overrunElection"] = "elected", ["ntc"] = "passed",
        ["remainingMf"] = "atLeastFour", ["additionalDefenderReveal"] = "none",
        ["soleEnemySmcOccupancy"] = "verifiedBySuppliedState",
        ["defenderResponseOrImmediateCc"] = "unresolved",
    };

    private static async Task<DomainPackageDescriptor> Descriptor() =>
        (await new ScenarioA1ConcealedSmcOverrunPackage()
            .ResolveAsync(ScenarioA1ConcealedSmcOverrunPackage.Identity)).Package!;

    private static Observation Observation(Dictionary<string, string> facts,
        string version = "snapshot-1") => new("a12-reveal", new ObservationSource("test-supplied-state"),
        Tenant, Now, JsonSerializer.SerializeToElement(facts), "bd01:E4:0", version,
        "test-supplied-state", ScenarioA1ConcealedSmcOverrunPackage.Identity);

    private static DomainConclusionContext Context(DomainPackageDescriptor descriptor,
        Observation? observation, string caseId)
    {
        var question = new DomainQuestion("concealed-smc-overrun", Tenant, descriptor.Identity,
            new SemanticIdentifier(descriptor.Identity.DomainId,
                ScenarioA1ConcealedSmcOverrunConclusionResolver.QuestionKind),
            JsonSerializer.SerializeToElement(new
            {
                unitId = "squad", locationId = "bd01:E4:0",
                observationVersion = "snapshot-1", caseId,
            }), Now);
        DomainEntityResolution Entity(string id) => new(
            new DomainEntityQuery("entity-" + id, Tenant, descriptor.Identity,
                new SemanticIdentifier(descriptor.Identity.DomainId, "unit-or-location"), id),
            DomainEntityResolutionOutcome.Resolved,
            [new DomainEntityCandidate(new SemanticIdentifier(descriptor.Identity.DomainId, id),
                descriptor.CanonicalSources[0],
                new EvidenceReference("entity:" + id, EvidenceKind.CanonicalSource, Tenant,
                    descriptor.Identity, id, descriptor.Identity.Version, "test"))],
            ["test.resolution"]);
        return new DomainConclusionContext(question, descriptor,
            [Entity("squad"), Entity("bd01:E4:0")], observation is null ? [] : [observation]);
    }
}
