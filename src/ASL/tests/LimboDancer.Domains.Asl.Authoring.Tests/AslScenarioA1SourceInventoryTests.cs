using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class AslScenarioA1SourceInventoryTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

    [Fact]
    public void CandidateInventoryPreservesPageCrossingAndPdfCorrectionSeparately()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var inventory = AslScenarioA1SourceInventory.Extract(manifests);

        Assert.Equal("3.01", inventory.Edition);
        Assert.Equal(8, inventory.Rules.Count);
        var enemy = Assert.Single(inventory.Rules, rule => rule.NormalizedRuleId == "A4.14");
        Assert.Equal(49, enemy.PdfStartPage);
        Assert.Equal(48, Assert.Single(enemy.Fragments).ConversionStartPage);
        var overrun = Assert.Single(inventory.Rules, rule => rule.NormalizedRuleId == "A4.15");
        Assert.Equal(49, overrun.PdfStartPage);
        Assert.True(Assert.Single(overrun.Fragments).HasFootnoteMarkers);
        var fortified = Assert.Single(inventory.Rules, rule => rule.NormalizedRuleId == "B23.922");
        Assert.Collection(fortified.Fragments,
            first =>
            {
                Assert.Equal(SourceFragmentKind.RuleText, first.Kind);
                Assert.Equal(140, first.ConversionStartPage);
                Assert.Equal(1556, first.StartLine);
            },
            second =>
            {
                Assert.Equal(SourceFragmentKind.RuleContinuation, second.Kind);
                Assert.Equal(141, second.ConversionStartPage);
                Assert.Equal(1560, second.StartLine);
            });
        var breach = Assert.Single(inventory.Rules, rule => rule.NormalizedRuleId == "B23.9221");
        var illustration = Assert.Single(breach.Fragments, fragment => fragment.Kind == SourceFragmentKind.FigureReference);
        Assert.Equal(1564, illustration.StartLine);
        Assert.Contains("images/eASLRB_v3_01-p141-1.png", illustration.Dependencies);
        Assert.All(inventory.Rules.SelectMany(rule => rule.Fragments), fragment =>
        {
            Assert.StartsWith("asl-fragment:sha256:", fragment.FragmentId, StringComparison.Ordinal);
            Assert.Equal(64, fragment.ContentSha256.Length);
        });

        using var json = JsonDocument.Parse(inventory.Serialize());
        Assert.Equal("candidate-unverified", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(8, json.RootElement.GetProperty("rules").GetArrayLength());
        var sample = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "SourceRegistry",
            "asl-scenario-a1.candidate-source-inventory.json");
        using var committed = JsonDocument.Parse(File.ReadAllText(sample));
        Assert.True(JsonElement.DeepEquals(committed.RootElement, json.RootElement));
    }

    [Fact]
    public void CandidateInventoryRejectsChangedSourceDigest()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var changed = manifests with
        {
            Fragments = manifests.Fragments.Select(fragment =>
                fragment.Locator.NormalizedElementId == "A4.14"
                    ? fragment with { SourceSha256 = new string('0', 64) }
                    : fragment).ToArray(),
        };
        Assert.Throws<InvalidOperationException>(() => AslScenarioA1SourceInventory.Extract(changed));
    }
}
