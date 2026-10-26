using AslAcronyms;
using LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;
using Xunit;

namespace ASL.Acronyms;

public sealed class CatalogTests
{
    private static readonly int[] ExpectedPages = [2, 3, 4];
    private static string Scan => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "scan.md"));
    private static string Ledger => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ledger.csv"));

    [Fact]
    public void CompleteSourcesReconcileWithoutLosingMeanings()
    {
        var doc = CatalogBuilder.Build(Scan, Ledger);
        Assert.Equal(685, doc.Entries.Length);
        Assert.Equal(438, doc.Entries.Count(e => e.Category == "main"));
        Assert.Equal(247, doc.Entries.Count(e => e.Category == "equipment"));
        var catalog = new AcronymCatalog(doc);
        Assert.Contains("Contre Avions", catalog.Find("equipment:CA:2")!.Definition, StringComparison.Ordinal);
        Assert.Contains("Personal computer", catalog.Find("equipment:PC")!.Definition, StringComparison.Ordinal);
        Assert.NotEqual(catalog.Find("main:DR"), catalog.Find("main:dr"));
        Assert.Contains(doc.Guidance, g => g.Heading == "Case and context distinctions");
    }

    [Fact]
    public void GenerationIsStableAcrossLineEndingsAndBom()
    {
        string scan = CatalogBuilder.Normalize(Scan), ledger = CatalogBuilder.Normalize(Ledger);
        Assert.DoesNotContain("\r", CatalogBuilder.Generate(scan, ledger), StringComparison.Ordinal);
        Assert.Equal(CatalogBuilder.Generate(scan, ledger),
            CatalogBuilder.Generate("\uFEFF" + scan.Replace("\n", "\r\n", StringComparison.Ordinal),
                ledger.Replace("\n", "\r\n", StringComparison.Ordinal)));
    }

    [Fact]
    public void ChangedDefinitionFailsInsteadOfSilentlyOverriding()
    {
        var error = Assert.Throws<InvalidDataException>(() => CatalogBuilder.Build(
            Scan.Replace("Compagnie d’Accompagnement", "Different meaning", StringComparison.Ordinal), Ledger));
        Assert.Contains("CA", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateDisplayFormsCanBeReordered()
    {
        var lines = CatalogBuilder.Normalize(Scan).Split('\n');
        var indices = lines.Select((line, index) => (line, index))
            .Where(item => item.line.StartsWith("| " + (char)96 + "PC" + (char)96 + " |", StringComparison.Ordinal))
            .Select(item => item.index).ToArray();
        Assert.Equal(2, indices.Length);
        (lines[indices[0]], lines[indices[1]]) = (lines[indices[1]], lines[indices[0]]);
        Assert.Equal(CatalogBuilder.Build(Scan, Ledger).Entries.Select(e => e.Id + e.Definition),
            CatalogBuilder.Build(string.Join('\n', lines), Ledger).Entries.Select(e => e.Id + e.Definition));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("717")]
    [InlineData("9-2")]
    [InlineData("2-3-4")]
    public void InvalidPagesFail(string text) => Assert.Throws<InvalidDataException>(() => CatalogBuilder.Pages(text, 716));

    [Fact]
    public void PageSetsAndEscapedPipesArePreserved()
    {
        Assert.Equal(ExpectedPages, CatalogBuilder.Pages("2-4, 3", 716).ToArray());
        Assert.Empty(CatalogBuilder.Pages("", 716));
        Assert.Equal(["A|B", "text"], CatalogBuilder.SplitTableRow(@"| A\|B | text |"));
    }

    [Fact]
    public void MissingAndDuplicateRowsFail()
    {
        var lines = CatalogBuilder.Normalize(Ledger).Split('\n');
        Assert.Throws<InvalidDataException>(() => CatalogBuilder.Build(Scan, string.Join('\n', lines.SkipLast(2))));
        Assert.Throws<InvalidDataException>(() => CatalogBuilder.Build(Scan, Ledger + "\n" + lines[1]));
    }
}
