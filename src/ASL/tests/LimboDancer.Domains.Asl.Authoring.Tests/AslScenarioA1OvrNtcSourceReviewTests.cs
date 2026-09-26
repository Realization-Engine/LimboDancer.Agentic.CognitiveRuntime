using System.Text.Json;
using LimboDancer.Domains.Asl.Authoring;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>
/// The delegated PDF-fidelity review of the OVR NTC package's new source subjects (unit step 10): each names the exact
/// registered fragment, matches the PDF comparison, and is Verified by the delegated reviewer.
/// </summary>
public sealed class AslScenarioA1OvrNtcSourceReviewTests
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
    public void TheFourNewSubjectsAreVerifiedExactlyAsCompared()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1OvrNtcSourceReview.Build(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(4, review.Records.Count);
        Assert.All(review.Records, record =>
        {
            Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition);
            Assert.Equal("source-provider:delegated-xunit-review", record.Actor.Identity);
        });
        Assert.Equal(
            [
                "asl-fragment:sha256:79b276101baa5aa6918f969c83f86a8fb1727717cbb0dd22dbdc6e89d5a2013f",
                "asl-fragment:sha256:885bdc7ae257ae12974bf5511a06740b28001410bf211476e2d154924188af5b",
                "asl-fragment:sha256:5b8331ba4d24b32ea6cda7bd479eb72d14f7b86aea80ff783537a31f24a74afc",
                "asl-fragment:sha256:1371497a18cb0ae925c9a34964ed3de4383fbd22f180936c2b83e4a350082b8a",
            ],
            review.Records.Select(record => record.SourceFragment.FragmentId));
    }

    [Fact]
    public void TheComparisonIsPinnedByDigest()
    {
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", "asl-scenario-a1.ovr-ntc-pdf-comparison.json");
        Assert.Equal(AslScenarioA1OvrNtcSourceReview.ComparisonSha256, Hashing.Sha256File(path));
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal([43, 65, 136, 30], json.RootElement.GetProperty("subjects").EnumerateArray().Select(item => item.GetProperty("physicalPdfPage").GetInt32()));
    }
}
