using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The delegated PDF-fidelity review of the Fire package's prose sources (unit step 17): each names the exact
/// registered fragment, matches the PDF comparison, and is Verified by the delegated reviewer.
/// </summary>
public sealed class AslScenarioA1FireSourceReviewTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static AslScenarioA1SourceAttestation Attestation()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.source-attestation.json")));
        return JsonSerializer.Deserialize<AslScenarioA1SourceAttestation>(json.RootElement.GetRawText(), JsonOptions)!;
    }

    [Fact]
    public void EverySubjectIsVerifiedExactlyAsCompared()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1FireSourceReview.Build(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(54, review.Records.Count);
        Assert.All(review.Records, record =>
        {
            Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition);
            Assert.Equal("source-provider:delegated-xunit-review", record.Actor.Identity);
        });
        Assert.Equal(review.Records.Count, review.Records.Select(record => record.SourceFragment.FragmentId).Distinct().Count());
    }

    [Fact]
    public void TheComparisonIsPinnedAndRecordsPhysicalPages()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", AslScenarioA1FireSourceReview.ComparisonFile);
        Assert.Equal(AslScenarioA1FireSourceReview.ComparisonSha256, Hashing.Sha256File(path));
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var subjects = json.RootElement.GetProperty("subjects").EnumerateArray().ToArray();

        // Conversion page markers that are off by one keep their physical page.
        var a107 = subjects.Single(item => item.GetProperty("ruleId").GetString() == "A10.7");
        Assert.Equal(67, a107.GetProperty("conversionPage").GetInt32());
        Assert.Equal(68, a107.GetProperty("physicalPdfPage").GetInt32());

        // The conversion registers the end of A7.8 under A7.72.
        var a78 = subjects.Single(item => item.GetProperty("ruleId").GetString() == "A7.8");
        Assert.Equal("A7.72", a78.GetProperty("registeredElementId").GetString());
        Assert.Equal(58, a78.GetProperty("physicalPdfPage").GetInt32());
    }
}
