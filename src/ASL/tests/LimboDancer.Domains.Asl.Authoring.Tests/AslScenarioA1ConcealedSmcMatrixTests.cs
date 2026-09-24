using System.Security.Cryptography;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

/// <summary>Affirmative review of exact A12.15-origin OVR case boundaries, before package publication.</summary>
public sealed class AslScenarioA1ConcealedSmcMatrixTests
{
    private const string MatrixDigest = "f64f4749fed87ee9517130ba747ebec4aafee618bd86ab15c4984c6854c09afa";
    private static readonly string[] RuleIds = ["A12.15", "A4.14", "A4.15", "A4.151", "A4.152", "B23.4"];
    private static readonly string[] CommonKeys =
    [
        "board", "phase", "attacker", "entryMode", "a414Exception", "initialDefenderState",
        "revealProvenance", "revealedOccupant", "otherModifier", "leaderExemption", "entryMfCost",
    ];

    [Fact]
    public void AffirmativeReviewPinsExactSourcesPredecessorsAndTenBoundedCases()
    {
        Assert.Equal(MatrixDigest, Digest("asl-scenario-a1.concealed-smc-overrun-case-matrix.json"));
        using var matrix = Read("asl-scenario-a1.concealed-smc-overrun-case-matrix.json");
        using var transition = Read("asl-scenario-a1.concealed-smc-overrun-transition.json");
        var root = matrix.RootElement;
        Assert.Equal("affirmative-xunit-reviewed-case-contract-no-package-publication",
            root.GetProperty("status").GetString());
        Assert.Equal(Digest("asl-scenario-a1.concealed-smc-overrun-transition.json"),
            root.GetProperty("transitionReviewSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.occupied-package.json"),
            root.GetProperty("priorOccupiedPackageManifestSha256").GetString());
        Assert.Equal(Digest("asl-scenario-a1.post-reveal-package.json"),
            root.GetProperty("priorPostRevealPackageManifestSha256").GetString());
        Assert.Equal(RuleIds, Values(root.GetProperty("sourceRules")));
        Assert.Equal(transition.RootElement.GetProperty("sourceFragments").GetRawText(),
            root.GetProperty("sourceFragments").GetRawText());
        Assert.Equal(CommonKeys, root.GetProperty("commonFacts").EnumerateObject()
            .Select(fact => fact.Name).ToArray());
        Assert.Equal("A12.15-immediate-defender-reveal",
            root.GetProperty("commonFacts").GetProperty("revealProvenance").GetString());
        Assert.Equal("concealed", root.GetProperty("commonFacts").GetProperty("initialDefenderState").GetString());
        Assert.Equal("none", root.GetProperty("executionAuthority").GetString());

        var cases = root.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(new (string Id, string Disposition)[]
        {
            ("election-unknown", "indeterminate"),
            ("declined", "delegated"),
            ("ntc-unresolved", "indeterminate"),
            ("ntc-failed", "indeterminate"),
            ("mf-unknown", "indeterminate"),
            ("mf-insufficient", "abstained"),
            ("other-reveal-unresolved", "indeterminate"),
            ("another-defender-revealed", "indeterminate"),
            ("qualified-response-unresolved", "qualified"),
            ("response-resolved", "abstained"),
        }, cases.Select(item => (item.GetProperty("caseId").GetString()!
            .Substring("A1-concealed-smc-".Length), item.GetProperty("expectedDisposition").GetString()!)));
        Assert.All(cases, item =>
        {
            var expectedReview = item.GetProperty("expectedDisposition").GetString() is "qualified" or "delegated"
                ? "reviewed-bounded" : "reviewed-nondefinitive";
            Assert.Equal(expectedReview, item.GetProperty("reviewStatus").GetString());
            Assert.NotEmpty(item.GetProperty("reason").GetString()!);
            Assert.Contains("A12.15", Values(item.GetProperty("sourceRules")));
            Assert.All(Values(item.GetProperty("sourceRules")), rule => Assert.Contains(rule, RuleIds));
            Assert.All(item.GetProperty("facts").EnumerateObject(), fact =>
            {
                Assert.Equal(JsonValueKind.String, fact.Value.ValueKind);
                Assert.False(root.GetProperty("commonFacts").TryGetProperty(fact.Name, out _));
            });
        });
        Assert.Equal("A1-post-reveal-nondummy-forced-back",
            cases[1].GetProperty("predecessorCaseId").GetString());
        Assert.Single(cases, item => item.GetProperty("expectedDisposition").GetString() == "qualified");
    }

    [Fact]
    public void OnlyQualifiedAttemptHasPassedNtcFourMfAndVerifiedSoleOccupancy()
    {
        using var matrix = Read("asl-scenario-a1.concealed-smc-overrun-case-matrix.json");
        var cases = matrix.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var qualified = Assert.Single(cases, item => item.GetProperty("expectedDisposition").GetString() == "qualified");
        var facts = qualified.GetProperty("facts");
        Assert.Equal("elected", facts.GetProperty("overrunElection").GetString());
        Assert.Equal("passed", facts.GetProperty("ntc").GetString());
        Assert.Equal("atLeastFour", facts.GetProperty("remainingMf").GetString());
        Assert.Equal("none", facts.GetProperty("additionalDefenderReveal").GetString());
        Assert.Equal("verifiedBySuppliedState", facts.GetProperty("soleEnemySmcOccupancy").GetString());
        Assert.Equal("unresolved", facts.GetProperty("defenderResponseOrImmediateCc").GetString());
        Assert.Equal(RuleIds, Values(qualified.GetProperty("sourceRules")));
        Assert.All(cases.Where(item => item.GetProperty("caseId").GetString() !=
            "A1-concealed-smc-qualified-response-unresolved"), item =>
            Assert.NotEqual("qualified", item.GetProperty("expectedDisposition").GetString()));
        Assert.Equal("none", matrix.RootElement.GetProperty("executionAuthority").GetString());
    }

    private static string[] Values(JsonElement array) => array.EnumerateArray()
        .Select(value => value.GetString()!).ToArray();
    private static JsonDocument Read(string name) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name)));
    private static string Digest(string name) => Convert.ToHexStringLower(SHA256.HashData(
        File.ReadAllBytes(Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry", name))));
}
