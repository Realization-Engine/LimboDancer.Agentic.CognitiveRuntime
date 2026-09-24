using System.Text.Json;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SemanticCandidateTests
{
    [Fact]
    public void AffirmativeSemanticReviewAcceptsExactlySevenReproducibleContracts()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        ScenarioA1SemanticAcceptance.Validate(candidate);
        Assert.Equal("7c9157b26fb5d0c23ac1b39f7181127b4d9530af9db6f9a90b05b27e6b8c1b66",
            ScenarioA1SemanticAcceptance.Sha256);
        var exactContracts = candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded")
            .Select(item => string.Join('|', item.Predicates.Select(predicate =>
                predicate.Key + "=" + predicate.ExpectedValue))).ToArray();
        Assert.Equal(7, exactContracts.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CandidateManifestAndReviewedCasePredicatesArePinned()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        Assert.Equal("6c7a415db0e08c308e20afec9a079da5e700ca9b115794cab0d39c113861fdf5",
            candidate.RootSha256);
        Assert.Equal(9, candidate.Cases.Count);
        Assert.Equal("007dbe2512c7bd5f0b17c51ad6ab2cea800b4f8e2c47267fb6cc0e5fbc57b2ee",
            ScenarioA1BoundedAdmission.Sha256);
        var definitive = candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded").ToArray();
        Assert.Equal(7, definitive.Length);
        Assert.All(definitive, item => Assert.NotEmpty(item.Predicates));
        Assert.All(candidate.Cases.Where(item => item.ReviewStatus != "reviewed-bounded"),
            item => Assert.Empty(item.Predicates));
    }

    [Theory]
    [InlineData("A1-empty-ordinary-mph", "eligible-2mf")]
    [InlineData("A1-known-enemy-mmc-mph", "prohibited")]
    [InlineData("A1-fortified-unbreached-enemy-squad", "prohibited")]
    [InlineData("A1-single-known-enemy-smc-overrun", "qualified-overrun-entry-attempt-4mf")]
    [InlineData("A1-fortified-breached-entry", "qualified-breached-advance-attempt")]
    [InlineData("A1-stacking-equivalents-needed", "eligible-3mf-with-overstack-penalty")]
    [InlineData("A1-advance-phase-entry", "qualified-advance-entry-attempt")]
    public void ExactReviewedPredicatesProduceOnlyTheirScopedDisposition(string caseId, string expected)
    {
        var candidate = new ScenarioA1SemanticCandidate();
        var semanticCase = Assert.Single(candidate.Cases, item => item.Id == caseId);
        var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
        var result = candidate.Evaluate(caseId, facts);
        Assert.Equal(expected, result.Disposition);
        Assert.Equal(semanticCase.SourceRules, result.SourceRules);
        facts.Remove(semanticCase.Predicates[0].Key);
        Assert.Equal("indeterminate", candidate.Evaluate(caseId, facts).Disposition);
        facts[semanticCase.Predicates[0].Key] = "contrary";
        Assert.Equal("abstained", candidate.Evaluate(caseId, facts).Disposition);
        facts["unreviewedModifier"] = "true";
        Assert.Equal("abstained", candidate.Evaluate(caseId, facts).Disposition);
    }

    [Fact]
    public void DeferredOrUnknownCasesNeverInheritAReviewedLabel()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        foreach (var semanticCase in candidate.Cases.Where(item => item.ReviewStatus == "deferred"))
        {
            Assert.Equal("abstained", candidate.Evaluate(semanticCase.Id,
                new Dictionary<string, string>()).Disposition);
        }
        Assert.Equal("indeterminate", candidate.Evaluate("A1-concealed-occupancy-attempt",
            new Dictionary<string, string>()).Disposition);
        Assert.Equal("abstained", candidate.Evaluate("unknown",
            new Dictionary<string, string>()).Disposition);
    }

    [Fact]
    public void AdmittedOccupiedBranchesRejectChangedCrossedHexsideStackAndPhase()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        foreach (var id in new[] { "A1-fortified-breached-entry", "A1-stacking-equivalents-needed",
            "A1-advance-phase-entry" })
        {
            var semanticCase = Assert.Single(candidate.Cases, item => item.Id == id);
            var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
            facts["phase"] = id == "A1-stacking-equivalents-needed" ? "aph" : "mph";
            Assert.Equal("abstained", candidate.Evaluate(id, facts).Disposition);
        }
        var breach = Assert.Single(candidate.Cases, item => item.Id == "A1-fortified-breached-entry")
            .Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
        breach["breach"] = "counterAtDifferentHexside";
        Assert.Equal("abstained", candidate.Evaluate("A1-fortified-breached-entry", breach).Disposition);

        var stack = Assert.Single(candidate.Cases, item => item.Id == "A1-stacking-equivalents-needed")
            .Predicates.ToDictionary(item => item.Key, item => item.ExpectedValue);
        Assert.True(ScenarioA1StackingCost.IsReviewedThreeMfEntry(stack));
        stack["friendlyUnmannedCrewsOrHalfSquads"] = "2";
        Assert.False(ScenarioA1StackingCost.IsReviewedThreeMfEntry(stack));
        Assert.Equal("abstained", candidate.Evaluate("A1-stacking-equivalents-needed", stack).Disposition);
    }

    [Fact]
    public void CandidateManifestRejectsAlteredSourceOrRootDigest()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        var assembly = typeof(ScenarioA1SemanticCandidate).Assembly;
        using var stream = assembly.GetManifestResourceStream("ScenarioA1.candidate-manifest.json")!;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        using var document = JsonDocument.Parse(content);
        var matrix = document.RootElement.GetProperty("caseMatrixSha256").GetString()!;
        var comparison = document.RootElement.GetProperty("occupiedComparisonSha256").GetString()!;
        Assert.Equal(candidate.RootSha256,
            ScenarioA1SemanticCandidate.ValidateManifest(content, matrix, comparison));
        Assert.Throws<InvalidOperationException>(() => ScenarioA1SemanticCandidate.ValidateManifest(
            content, new string('0', 64), comparison));
        Assert.Throws<InvalidOperationException>(() => ScenarioA1SemanticCandidate.ValidateManifest(
            content.Replace(candidate.RootSha256, new string('0', 64), StringComparison.Ordinal),
            matrix, comparison));
    }
}
