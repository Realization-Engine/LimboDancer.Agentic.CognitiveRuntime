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
    public void TheOrdnanceSubjectsAreVerified()
    {
        // Unit step 24: 47 Chapter C subjects, A1.123, and A21.13, each whole on its physical page.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var ordnance = AslScenarioA1FireSourceReview.BuildOrdnance(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(49, ordnance.Records.Count);
        Assert.All(ordnance.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void TheVehicleSubjectsAreVerified()
    {
        // Unit step 25: nine Chapter A subjects and 36 Chapter D subjects, D.8 and D5.341 with their continuations.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var vehicles = AslScenarioA1FireSourceReview.BuildVehicles(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(45, vehicles.Records.Count);
        Assert.All(vehicles.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass5SubjectsAreVerified()
    {
        // Backlog pass 5: 25 Chapter A subjects, two Chapter C, four Chapter D, and one Chapter B, with the continuations of A2.6, A4.43,
        // A15.1, A15.21, and D5.341; A15.1 to A15.3 are on physical page 83 though the conversion marks 81.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass5 = AslScenarioA1FireSourceReview.BuildPass5(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(31, pass5.Records.Count);
        Assert.All(pass5.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass6SubjectsAreVerified()
    {
        // Backlog pass 6: seven Chapter A subjects, three Chapter B, and four Chapter D; A12.2 runs across the page break from 79 to 80.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass6 = AslScenarioA1FireSourceReview.BuildPass6(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(14, pass6.Records.Count);
        Assert.All(pass6.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass7SubjectsAreVerified()
    {
        // Backlog pass 7: 32 Chapter C subjects and 18 Chapter D, each whole on its page.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass7 = AslScenarioA1FireSourceReview.BuildPass7(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(50, pass7.Records.Count);
        Assert.All(pass7.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass8SubjectsAreVerified()
    {
        // Backlog pass 8: 12 Chapter A subjects and 42 Chapter C; C6.17 runs across the page break from 173 to 174.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass8 = AslScenarioA1FireSourceReview.BuildPass8(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(54, pass8.Records.Count);
        Assert.All(pass8.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass9SubjectsAreVerified()
    {
        // Backlog pass 9: 4 Chapter A subjects and 19 Chapter C; C13.32 runs across the page break from 183 to 184.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass9 = AslScenarioA1FireSourceReview.BuildPass9(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(23, pass9.Records.Count);
        Assert.All(pass9.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass9bSubjectsAreVerified()
    {
        // Backlog pass 9b: the ATR and the Panzerschreck, 1 Chapter A subject, one Chapter B, and 14 Chapter C.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass9b = AslScenarioA1FireSourceReview.BuildPass9b(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(16, pass9b.Records.Count);
        Assert.All(pass9b.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass10SubjectsAreVerified()
    {
        // Backlog pass 10: movement and terrain, 12 Chapter A subjects and 28 Chapter B.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass10 = AslScenarioA1FireSourceReview.BuildPass10(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(40, pass10.Records.Count);
        Assert.All(pass10.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass15SubjectsAreVerified()
    {
        // Backlog pass 15: special units and nationalities, 46 Chapter A fragments.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass15 = AslScenarioA1FireSourceReview.BuildPass15(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(46, pass15.Records.Count);
        Assert.All(pass15.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass14SubjectsAreVerified()
    {
        // Backlog pass 14: Close Combat and capture, part 2, nine Chapter A fragments.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass14 = AslScenarioA1FireSourceReview.BuildPass14(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(9, pass14.Records.Count);
        Assert.All(pass14.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass13SubjectsAreVerified()
    {
        // Backlog pass 13: Rally, Rout, and support weapons, 21 Chapter A fragments.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass13 = AslScenarioA1FireSourceReview.BuildPass13(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(21, pass13.Records.Count);
        Assert.All(pass13.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass12SubjectsAreVerified()
    {
        // Backlog pass 12: the fire extensions, 16 Chapter A fragments (A9.222 is cited from the PDF).
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass12 = AslScenarioA1FireSourceReview.BuildPass12(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(16, pass12.Records.Count);
        Assert.All(pass12.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void ThePass11SubjectsAreVerified()
    {
        // Backlog pass 11: vehicle movement and OVR, 40 Chapter D subjects, 16 Chapter A, and 3 Chapter B.
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var pass11 = AslScenarioA1FireSourceReview.BuildPass11(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal(59, pass11.Records.Count);
        Assert.All(pass11.Records, record => Assert.Equal(TirSourceVerificationDisposition.Verified, record.Disposition));
    }

    [Fact]
    public void TheCloseCombatAndBerserkSubjectsAreVerified()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var closeCombat = AslScenarioA1FireSourceReview.BuildCloseCombat(RepositoryPaths.Root, manifests, Attestation());
        var berserk = AslScenarioA1FireSourceReview.BuildBerserkSurrender(RepositoryPaths.Root, manifests, Attestation());
        Assert.Equal((28, 18), (closeCombat.Records.Count, berserk.Records.Count));
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
