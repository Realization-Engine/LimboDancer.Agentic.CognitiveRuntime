using System.Text.Json;
using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1SemanticCandidateTests
{
    [Fact]
    public void CandidateManifestAndReviewedCasePredicatesArePinned()
    {
        var candidate = new ScenarioA1SemanticCandidate();
        Assert.Equal("41de9d7f1e54d54abc044df2541a483abd22fa10ce658653198b4b487cfce234",
            candidate.RootSha256);
        Assert.Equal(9, candidate.Cases.Count);
        Assert.Equal("33823b6b4e42cf3cb925569e3b407eec754399021b1107010775f96f959e0963",
            ScenarioA1BoundedAdmission.Sha256);
        var definitive = candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded").ToArray();
        Assert.Equal(4, definitive.Length);
        Assert.All(definitive, item => Assert.NotEmpty(item.Predicates));
        Assert.All(candidate.Cases.Where(item => item.ReviewStatus != "reviewed-bounded"),
            item => Assert.Empty(item.Predicates));
    }

    [Theory]
    [InlineData("A1-empty-ordinary-mph", "eligible-2mf")]
    [InlineData("A1-known-enemy-mmc-mph", "prohibited")]
    [InlineData("A1-fortified-unbreached-enemy-squad", "prohibited")]
    [InlineData("A1-single-known-enemy-smc-overrun", "qualified-overrun-entry-attempt-4mf")]
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
