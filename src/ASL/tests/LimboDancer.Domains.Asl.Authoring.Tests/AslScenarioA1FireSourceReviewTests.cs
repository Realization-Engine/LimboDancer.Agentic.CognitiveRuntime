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
    public void TheBranchSubjectsAreVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1FireSourceReview.BuildBranches(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(4, review.Records.Count);
        Assert.All(review.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", AslScenarioA1FireSourceReview.BranchesComparisonFile);
        Assert.Equal(AslScenarioA1FireSourceReview.BranchesComparisonSha256, Hashing.Sha256File(path));
    }

    [Fact]
    public void TheRallySubjectsAreVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1FireSourceReview.BuildRally(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(9, review.Records.Count);
        Assert.All(review.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", AslScenarioA1FireSourceReview.RallyComparisonFile);
        Assert.Equal(AslScenarioA1FireSourceReview.RallyComparisonSha256, Hashing.Sha256File(path));
    }

    [Fact]
    public void TheHeatOfBattleSubjectsAreVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1FireSourceReview.BuildHeatOfBattle(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(12, review.Records.Count);
        Assert.All(review.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", AslScenarioA1FireSourceReview.HeatOfBattleComparisonFile);
        Assert.Equal(AslScenarioA1FireSourceReview.HeatOfBattleComparisonSha256, Hashing.Sha256File(path));
    }

    [Fact]
    public void TheCloseCombatAndBerserkSubjectsAreVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var closeCombat = AslScenarioA1FireSourceReview.BuildCloseCombat(RepositoryPaths.Root, manifests, Attestation());
        var berserk = AslScenarioA1FireSourceReview.BuildBerserkSurrender(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal((25, 17), (closeCombat.Records.Count, berserk.Records.Count));
        Assert.All(closeCombat.Records.Concat(berserk.Records), record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));

        // A11.41 and A20.21 run across a page break: 73 to 74 and 86 to 87.
        foreach (var (file, rule, pages) in new[] { (AslScenarioA1FireSourceReview.CloseCombatComparisonFile, "A11.41", (73, 74)),
            (AslScenarioA1FireSourceReview.BerserkSurrenderComparisonFile, "A20.21", (86, 87)) })
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", file)));
            var split = json.RootElement.GetProperty("subjects").EnumerateArray().Single(item => item.GetProperty("ruleId").GetString() == rule);
            Assert.Equal(pages, (split.GetProperty("physicalPdfPage").GetInt32(), split.GetProperty("secondPhysicalPdfPage").GetInt32()));
        }
    }

    [Fact]
    public void TheExtensionSubjectsAreVerifiedWithTheirInterruptedParts()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var review = AslScenarioA1FireSourceReview.BuildExtensions(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(50, review.Records.Count);
        Assert.All(review.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));

        var path = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", AslScenarioA1FireSourceReview.ExtensionsComparisonFile);
        Assert.Equal(AslScenarioA1FireSourceReview.ExtensionsComparisonSha256, Hashing.Sha256File(path));
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var split = json.RootElement.GetProperty("subjects").EnumerateArray()
            .Where(item => item.GetProperty("comparison").GetString() == "complete-alphanumeric-match-in-two-parts").ToArray();
        Assert.Equal(["A8.26", "A8.31", "A9.2", "A12.11", "B3.4"], split.Select(item => item.GetProperty("ruleId").GetString()));

        // A12.11 runs from page 76 onto page 77.
        var a1211 = split.Single(item => item.GetProperty("ruleId").GetString() == "A12.11");
        Assert.Equal((76, 77), (a1211.GetProperty("physicalPdfPage").GetInt32(), a1211.GetProperty("secondPhysicalPdfPage").GetInt32()));
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
