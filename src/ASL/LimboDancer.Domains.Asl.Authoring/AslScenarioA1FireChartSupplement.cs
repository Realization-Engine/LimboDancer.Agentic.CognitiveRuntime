using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>One FP column of the IFT: its FP (bold in print) and the printed header with its HE equivalent.</summary>
public sealed record AslIftColumn(int Fp, string PrintedHeader);

/// <summary>One DR row of the IFT. <c>none</c> is the printed dash (no effect, A7.306).</summary>
public sealed record AslIftRow(string Label, int Dr, bool OrLess, bool OrMore, IReadOnlyList<string> Results);

/// <summary>The Personnel result cells of the A7 Infantry Fire Table (physical page 692).</summary>
public sealed record AslInfantryFireTable(IReadOnlyList<AslIftColumn> Columns, IReadOnlyList<AslIftRow> Rows);

/// <summary>One admitted row of the B. Terrain Chart (physical page 698), as printed.</summary>
public sealed record AslTerrainChartRow(
    string Terrain, string Example, string LosObstacleHindrance, string TemIndirect, string Notes);

/// <summary>The admitted rows and legend entries of the B. Terrain Chart.</summary>
public sealed record AslTerrainChartTem(IReadOnlyList<AslTerrainChartRow> Rows, IReadOnlyList<string> Legend);

/// <summary>The bounded Fire chart supplement once its transcriptions reproduce the pinned extraction.</summary>
public sealed record AslScenarioA1FireCharts(
    string RegistrySha256, AslInfantryFireTable InfantryFireTable, AslTerrainChartTem TerrainChart);

/// <summary>One cell the user compared with the rendered page.</summary>
public sealed record AslFireChartSpotCheck(int PhysicalPdfPage, string Row, string Column, string Transcribed);

/// <summary>The user-delegated review of the Fire chart supplement: the xUnit item checks and the user's spot-check.</summary>
public sealed record AslScenarioA1FireChartReviewDecision(
    string Subject,
    string Status,
    string ReviewerAuthority,
    string RegistrySha256,
    string SourcePdfSha256,
    int[] PhysicalPdfPages,
    string Method,
    AslFireChartSpotCheck[] SpotCheck,
    string Scope);

/// <summary>
/// The bounded chart supplement of unit step 17: the IFT (page 692) and the TEM column of the Terrain Chart (page 698),
/// outside the registered pages 6 to 253. Each transcription is checked against the pinned <c>pdftotext</c> extraction:
/// every item, rebuilt from the transcription and normalized as the registry records, must hash to the value pinned
/// from the extraction. The original registry and its verified subjects are unchanged.
/// </summary>
public static class AslScenarioA1FireChartSupplement
{
    public const string SupplementId = "asl-supplement:fire-charts";
    public const string RegistryFile = "asl-scenario-a1.fire-chart-supplement.json";
    public const string RegistrySha256 = "8499a4ec75da057920fbc05dde60b975f39560803d26ce1433f9c6567a052985";
    public const string Extractor = "pdftotext 4.00 (xpdf) -f N -l N -table -enc UTF-8 -eol unix";
    public const string NoEffect = "none";

    private const string PrintedDash = "—";
    private static readonly int[] IftFp = [1, 2, 4, 6, 8, 12, 16, 20, 24, 30, 36];

    /// <summary>Loads the committed registry and transcriptions, pinned by digest, and checks them.</summary>
    public static AslScenarioA1FireCharts Load(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var registryPath = Path.Combine(repositoryRoot, "docs", "ASL", "SourceRegistry", RegistryFile);
        var registryText = File.ReadAllText(registryPath);
        Require(Hashing.Sha256Text(registryText) == RegistrySha256, "The Fire chart supplement registry changed.");
        using var registry = JsonDocument.Parse(registryText);
        var pages = registry.RootElement.GetProperty("pages").EnumerateArray().ToArray();
        Require(pages.Length == 2, "The Fire chart supplement pages changed.");
        string Read(JsonElement page) => File.ReadAllText(Path.Combine(repositoryRoot,
            page.GetProperty("transcriptionPath").GetString()!.Replace('/', Path.DirectorySeparatorChar)));
        return Evaluate(registryText, Read(pages[0]), Read(pages[1]));
    }

    /// <summary>
    /// Checks a registry and the two transcriptions: identity, transcription digests, table shape, and every pinned
    /// item. A changed cell fails even when the transcription digest is re-pinned, because its item no longer
    /// reproduces the extraction.
    /// </summary>
    public static AslScenarioA1FireCharts Evaluate(string registryText, string iftTranscription, string temTranscription)
    {
        ArgumentNullException.ThrowIfNull(registryText);
        ArgumentNullException.ThrowIfNull(iftTranscription);
        ArgumentNullException.ThrowIfNull(temTranscription);
        using var registry = JsonDocument.Parse(registryText);
        var root = registry.RootElement;
        Require(root.GetProperty("status").GetString() == "registered-unverified-supplement"
            && root.GetProperty("supplementId").GetString() == SupplementId
            && root.GetProperty("sourcePdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest
            && root.GetProperty("extractor").GetString() == Extractor,
            "The Fire chart supplement identity changed.");
        var pages = root.GetProperty("pages").EnumerateArray().ToArray();
        Require(pages.Length == 2
            && pages[0].GetProperty("physicalPdfPage").GetInt32() == 692
            && pages[1].GetProperty("physicalPdfPage").GetInt32() == 698,
            "The Fire chart supplement pages changed.");
        Require(Hashing.Sha256Text(iftTranscription) == pages[0].GetProperty("transcriptionSha256").GetString()
            && Hashing.Sha256Text(temTranscription) == pages[1].GetProperty("transcriptionSha256").GetString(),
            "A Fire chart transcription differs from its registered digest.");

        var ift = ParseIft(iftTranscription);
        var tem = ParseTem(temTranscription);
        CheckItems(pages[0], IftItems(ift));
        CheckItems(pages[1], TemItems(tem));
        return new AslScenarioA1FireCharts(Hashing.Sha256Text(registryText), ift, tem);
    }

    public const string AcceptedStatus = "accepted-by-user-delegated-xunit-review";
    public const string Authority = "user-directed-xunit-review-2026-09-26";
    public const string DecisionFile = "asl-scenario-a1.fire-chart-review-decision.json";

    /// <summary>
    /// The review decision: every pinned item reproduced (checked by <see cref="Load"/>) and the user's spot-check of
    /// nine cells against the rendered pages on 2026-09-26. It admits the transcribed cells as printed; what they mean
    /// for a fire attack is decided by the Fire review, not here.
    /// </summary>
    public static AslScenarioA1FireChartReviewDecision Review(string repositoryRoot)
    {
        var charts = Load(repositoryRoot);
        var ift = charts.InfantryFireTable;
        string Cell(int dr, int fp) => ift.Rows.Single(row => row.Dr == dr)
            .Results[ift.Columns.Select((column, index) => (column, index)).Single(pair => pair.column.Fp == fp).index];
        var terrain = charts.TerrainChart.Rows.ToDictionary(row => row.Terrain, StringComparer.Ordinal);
        AslFireChartSpotCheck[] spotCheck =
        [
            new(692, "4", "16", Cell(4, 16)),
            new(692, "7", "2", Cell(7, 2)),
            new(692, "2", "36", Cell(2, 36)),
            new(692, "11", "20", Cell(11, 20)),
            new(692, "6", "24", Cell(6, 24)),
            new(698, "23. Stone Building", "TEM/Indirect", terrain["23. Stone Building"].TemIndirect),
            new(698, "13. Woods", "TEM/Indirect", terrain["13. Woods"].TemIndirect),
            new(698, "15. Grain", "LOS Obstacle/Hindrance and Notes",
                terrain["15. Grain"].LosObstacleHindrance + "; " + terrain["15. Grain"].Notes),
            new(698, "14. Orchard", "LOS Obstacle/Hindrance", terrain["14. Orchard"].LosObstacleHindrance),
        ];
        return new AslScenarioA1FireChartReviewDecision(
            SupplementId, AcceptedStatus, Authority, charts.RegistrySha256, AslScenarioA1SourceInventory.PdfDigest,
            [692, 698],
            $"Each transcribed item, normalized as the registry records, hashes to the value pinned from {Extractor}; "
                + "the user compared the listed cells with the rendered pages.",
            spotCheck,
            "The IFT Personnel result cells and DR/FP header, and the LOS, TEM, and Notes cells of seven Terrain Chart "
                + "rows with five legend entries, as printed. Their use in a fire attack is decided by the step 17 Fire review.");
    }

    /// <summary>The normalization the registry records, applied alike to the extraction and the transcription.</summary>
    public static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var text = value.Replace("•", string.Empty, StringComparison.Ordinal)
            .Replace("‡", string.Empty, StringComparison.Ordinal)
            .Replace("½", "1/2", StringComparison.Ordinal);
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return Regex.Replace(text, "([≤≥]) ", "$1");
    }

    private static AslInfantryFireTable ParseIft(string transcription)
    {
        using var json = JsonDocument.Parse(transcription);
        var root = json.RootElement;
        Require(root.GetProperty("physicalPdfPage").GetInt32() == 692
            && root.GetProperty("sourcePdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest,
            "The IFT transcription names another page.");
        var columns = root.GetProperty("columns").EnumerateArray()
            .Select(item => new AslIftColumn(item.GetProperty("fp").GetInt32(), item.GetProperty("printedHeader").GetString()!))
            .ToArray();
        Require(columns.Select(column => column.Fp).SequenceEqual(IftFp), "The IFT FP columns changed.");
        var rows = root.GetProperty("rows").EnumerateArray().Select(item => new AslIftRow(
                item.GetProperty("label").GetString()!,
                item.GetProperty("dr").GetInt32(),
                item.TryGetProperty("orLess", out var less) && less.GetBoolean(),
                item.TryGetProperty("orMore", out var more) && more.GetBoolean(),
                item.GetProperty("results").EnumerateArray().Select(cell => cell.GetString()!).ToArray()))
            .ToArray();
        Require(rows.Length == 16
            && rows.Select(row => row.Dr).SequenceEqual(Enumerable.Range(0, 16))
            && rows[0].OrLess && rows[^1].OrMore
            && rows.Skip(1).Take(14).All(row => !row.OrLess && !row.OrMore)
            && rows.All(row => row.Results.Count == columns.Length && row.Results.All(IsResult)),
            "The IFT rows changed shape.");
        return new AslInfantryFireTable(columns, rows);
    }

    private static AslTerrainChartTem ParseTem(string transcription)
    {
        using var json = JsonDocument.Parse(transcription);
        var root = json.RootElement;
        Require(root.GetProperty("physicalPdfPage").GetInt32() == 698
            && root.GetProperty("sourcePdfSha256").GetString() == AslScenarioA1SourceInventory.PdfDigest,
            "The Terrain Chart transcription names another page.");
        var rows = root.GetProperty("rows").EnumerateArray().Select(item => new AslTerrainChartRow(
                item.GetProperty("terrain").GetString()!,
                item.GetProperty("example").GetString()!,
                item.GetProperty("losObstacleHindrance").GetString()!,
                item.GetProperty("temIndirect").GetString()!,
                item.GetProperty("notes").GetString()!))
            .ToArray();
        var legend = root.GetProperty("legend").EnumerateArray().Select(item => item.GetString()!).ToArray();
        return new AslTerrainChartTem(rows, legend);
    }

    private static IEnumerable<(string Key, string Text)> IftItems(AslInfantryFireTable ift)
    {
        yield return ("header", "DR/FP " + string.Join(' ', ift.Columns.Select(column => column.PrintedHeader)));
        foreach (var row in ift.Rows)
        {
            var label = Normalize(row.Label);
            yield return (label, label + " " + string.Join(' ', row.Results.Select(Printed)));
        }
    }

    private static IEnumerable<(string Key, string Text)> TemItems(AslTerrainChartTem tem)
    {
        foreach (var row in tem.Rows)
        {
            yield return (row.Terrain, string.Join(" | ",
                row.Terrain, row.Example, Printed(row.LosObstacleHindrance), row.TemIndirect));
            yield return (row.Terrain + " notes", row.Notes);
        }

        // Legend items are keyed by the start of their printed line, as the registry records them.
        string[] starts = ["Terrain listed in RED", "†:", "*, **, ***:", "■:", "FFMO: -1 DRM"];
        Require(tem.Legend.Count == starts.Length, "The Terrain Chart legend entries changed.");
        for (var index = 0; index < starts.Length; index++)
        {
            Require(tem.Legend[index].StartsWith(starts[index], StringComparison.Ordinal),
                "The Terrain Chart legend entries changed.");
            yield return ("legend " + starts[index], tem.Legend[index]);
        }
    }

    private static void CheckItems(JsonElement page, IEnumerable<(string Key, string Text)> items)
    {
        var pinned = page.GetProperty("items").EnumerateArray()
            .Select(item => (Key: item.GetProperty("key").GetString()!, Sha: item.GetProperty("normalizedSha256").GetString()!))
            .ToArray();
        var rebuilt = items.Select(item => (item.Key, Sha: Hashing.Sha256Text(Normalize(item.Text)))).ToArray();
        Require(pinned.Length == rebuilt.Length, $"Page {page.GetProperty("physicalPdfPage").GetInt32()} items changed.");
        for (var index = 0; index < pinned.Length; index++)
        {
            Require(pinned[index].Key == rebuilt[index].Key,
                $"Page {page.GetProperty("physicalPdfPage").GetInt32()} item order changed at {pinned[index].Key}.");
            Require(pinned[index].Sha == rebuilt[index].Sha,
                $"The transcription of '{pinned[index].Key}' on page {page.GetProperty("physicalPdfPage").GetInt32()} does not reproduce the extraction.");
        }
    }

    private static string Printed(string cell) => cell == NoEffect ? PrintedDash : cell;

    private static bool IsResult(string cell) =>
        cell == NoEffect || cell is "NMC" or "PTC" || Regex.IsMatch(cell, "^([1-7]KIA|K/[1-4]|[1-4]MC)$");

    private static void Require(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }
    }
}
