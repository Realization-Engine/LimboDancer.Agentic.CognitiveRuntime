namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class MarkdownFragmentLocatorTests
{
    [Fact]
    public void LocatesExactStructuralFragmentsAndPreservesPublishedIdentity()
    {
        const string content = """
            <!-- page 43 -->

            # A. INFANTRY

            **A.1 DICE:** Exact first rule.

            ## 1. PERSONNEL COUNTERS

            **1.1** Local rule with a footnote.<sup>2</sup>

            Continuation of the local rule.

            ![Figure](images/example.png)

            ```text
            TABLE CELL
            ```
            """;
        var sourceHash = Hashing.Sha256Text(content);

        var fragments = MarkdownFragmentLocator.Locate(
            "asl-easlrb-3.10:chapter-a",
            "chapter-a.md",
            sourceHash,
            "A",
            content);

        var localRule = Assert.Single(fragments, static fragment =>
            fragment.Locator.NormalizedElementId == "A1.1"
            && fragment.Kind == SourceFragmentKind.RuleText);
        Assert.Equal("1.1", localRule.Locator.PublishedElementId);
        Assert.Equal("A1.1", localRule.Locator.NormalizedElementId);
        Assert.Equal(43, localRule.Locator.StartPage);
        Assert.True(localRule.HasFootnoteMarkers);
        Assert.Equal(Hashing.Sha256Text(localRule.Content), localRule.ContentSha256);

        var continuation = Assert.Single(
            fragments,
            static fragment => fragment.Kind == SourceFragmentKind.RuleContinuation);
        Assert.Equal("A1.1", continuation.Locator.NormalizedElementId);
        var figure = Assert.Single(
            fragments,
            static fragment => fragment.Kind == SourceFragmentKind.FigureReference);
        Assert.Collection(
            figure.Dependencies,
            static dependency => Assert.Equal("images/example.png", dependency));
        Assert.Contains(fragments, static fragment => fragment.Kind == SourceFragmentKind.StructuredText);
    }

    [Fact]
    public void FragmentIdentityChangesWithSourceContent()
    {
        const string firstContent = "**1.1** First.\n";
        const string changedContent = "**1.1** Changed.\n";
        var firstHash = Hashing.Sha256Text(firstContent);
        var changedHash = Hashing.Sha256Text(changedContent);
        var first = Assert.Single(MarkdownFragmentLocator.Locate("source", "source.md", firstHash, "B", firstContent));
        var repeated = Assert.Single(MarkdownFragmentLocator.Locate("source", "source.md", firstHash, "B", firstContent));
        var changed = Assert.Single(MarkdownFragmentLocator.Locate("source", "source.md", changedHash, "B", changedContent));

        Assert.Equal(first.FragmentId, repeated.FragmentId);
        Assert.NotEqual(first.FragmentId, changed.FragmentId);
        Assert.NotEqual(first.ContentSha256, changed.ContentSha256);
    }

    [Fact]
    public void RealChapterLocalIdentifierIsNormalizedWithChapterContext()
    {
        var relative = Path.Combine(
            "docs",
            "ASL",
            "Rulebook_Markdown",
            "03 - Chapter B - Terrain.md");
        var path = Path.Combine(RepositoryPaths.Root, relative);
        var content = File.ReadAllText(path);
        var sourcePath = relative.Replace(Path.DirectorySeparatorChar, '/');

        var fragments = MarkdownFragmentLocator.Locate(
            "asl-easlrb-3.10:chapter-b",
            sourcePath,
            Hashing.Sha256File(path),
            "B",
            content);

        var fragment = fragments.First(item => item.Locator.NormalizedElementId == "B1.13");
        Assert.Equal("1.13", fragment.Locator.PublishedElementId);
        Assert.Equal("B1.13", MarkdownFragmentLocator.NormalizeRuleId("1.13", "B"));
    }

    [Fact]
    public void BareNumberedNotesAndListItemsAreNotRuleBoundaries()
    {
        const string content = """
            **1.** *A.2 ERRORS:* Designer note.

            **1)** First procedural choice.
            """;

        var fragments = MarkdownFragmentLocator.Locate(
            "source",
            "source.md",
            Hashing.Sha256Text(content),
            "A",
            content);

        Assert.All(fragments, static fragment => Assert.Equal(SourceFragmentKind.Paragraph, fragment.Kind));
        Assert.All(fragments, static fragment => Assert.Null(fragment.Locator.PublishedElementId));
    }

    [Fact]
    public void UnregisteredFigureDependencyFailsClosed()
    {
        const string content = "![Figure](images/missing.png)\n";
        var fragments = MarkdownFragmentLocator.Locate(
            "source",
            "docs/source.md",
            Hashing.Sha256Text(content),
            null,
            content);

        var exception = Assert.Throws<InvalidOperationException>(
            () => AslAuthoringManifestGenerator.ValidateFragmentDependencies(
                fragments,
                ["docs/source.md"]));

        Assert.Contains("Unregistered fragment dependencies", exception.Message, StringComparison.Ordinal);
    }
}
