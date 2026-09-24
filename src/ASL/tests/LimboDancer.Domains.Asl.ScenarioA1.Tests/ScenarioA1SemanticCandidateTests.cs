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
        Assert.Equal("bb089e5ac3e82067feb76268dae6fc3a5442ef3078a217e8b8e7524b00fe82ac",
            candidate.RootSha256);
        Assert.Equal(9, candidate.Cases.Count);
        var definitive = candidate.Cases.Where(item => item.ReviewStatus == "reviewed-bounded").ToArray();
        Assert.Equal(3, definitive.Length);
        Assert.All(definitive, item => Assert.NotEmpty(item.Predicates));
        Assert.All(candidate.Cases.Where(item => item.ReviewStatus != "reviewed-bounded"),
            item => Assert.Empty(item.Predicates));
    }

    [Theory]
    [InlineData("A1-empty-ordinary-mph", "eligible-2mf")]
    [InlineData("A1-known-enemy-mmc-mph", "prohibited")]
    [InlineData("A1-fortified-unbreached-enemy-squad", "prohibited")]
    public void ExactReviewedPredicatesProduceOnlyTheirScopedDisposition(string caseId, string expected)
    {
        var candidate = new ScenarioA1SemanticCandidate();
        var semanticCase = Assert.Single(candidate.Cases, item => item.Id == caseId);
        var facts = semanticCase.Predicates.ToDictionary(item => item.Key, item => item.Equals);
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
