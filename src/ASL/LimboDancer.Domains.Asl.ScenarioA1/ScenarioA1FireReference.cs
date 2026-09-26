using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>A reviewed catalog definition as the Fire package reads it: printed values only.</summary>
public sealed record FireDefinition(
    string Id,
    string Kind,
    string Nationality,
    string? Class,
    int? Firepower,
    int? Range,
    int? Morale,
    int? BrokenMorale,
    int? Leadership)
{
    public bool IsLeader => Kind == "asl:leader";

    public bool IsMmc => Kind is "asl:squad" or "asl:half-squad";
}

/// <summary>
/// The pinned reference data of the Fire package: the IFT and Terrain Chart transcriptions of the reviewed chart
/// supplement and the reviewed Scenario A1 catalog, each checked against the digest the case matrix records.
/// </summary>
public sealed class ScenarioA1FireReference
{
    public static readonly int[] ColumnFp = [1, 2, 4, 6, 8, 12, 16, 20, 24, 30, 36];

    // Direct Fire TEM of the admitted terrain: Terrain Chart p. 698, B1.1, B13.3, B14.3, B15.3, B23.3.
    public static readonly IReadOnlyDictionary<string, int> Tem = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["open-ground"] = 0,
        ["brush"] = 0,
        ["woods"] = 1,
        ["orchard"] = 0,
        ["grain"] = 0,
        ["wooden-building"] = 2,
        ["stone-building"] = 3,
    };

    // The Casualty Reduction a reviewed definition suffers (A7.302): a squad to its half-squad, when the catalog has it.
    private static readonly IReadOnlyDictionary<string, string> HalfSquads = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-squad"] = "attacker-half-squad",
    };

    private readonly string[][] results;

    private ScenarioA1FireReference(string[][] results, IReadOnlyDictionary<string, FireDefinition> definitions)
    {
        this.results = results;
        Definitions = definitions;
    }

    public IReadOnlyDictionary<string, FireDefinition> Definitions { get; }

    /// <summary>The IFT result for a Final DR and a column index; <c>none</c> is the printed dash.</summary>
    public string Result(int finalDr, int column) => results[Math.Clamp(finalDr, 0, 15)][column];

    /// <summary>The half-squad a squad is Reduced to, or null when the catalog has none.</summary>
    public static string? HalfSquadOf(string definitionId) => HalfSquads.GetValueOrDefault(definitionId);

    internal static ScenarioA1FireReference Load(JsonElement matrix)
    {
        using var ift = ScenarioA1FirePackage.Read("ScenarioA1.fire-ift.json", matrix.GetProperty("iftTranscriptionSha256").GetString()!);
        using var terrain = ScenarioA1FirePackage.Read("ScenarioA1.fire-terrain-chart.json",
            matrix.GetProperty("terrainChartTranscriptionSha256").GetString()!);
        using var catalog = ScenarioA1FirePackage.Read("ScenarioA1.fire-catalog.json", matrix.GetProperty("catalogSha256").GetString()!);

        var columns = ift.RootElement.GetProperty("columns").EnumerateArray().Select(item => item.GetProperty("fp").GetInt32()).ToArray();
        var rows = ift.RootElement.GetProperty("rows").EnumerateArray().ToArray();
        if (!columns.SequenceEqual(ColumnFp) || rows.Length != 16
            || rows.Select(row => row.GetProperty("dr").GetInt32()).Where((dr, index) => dr != index).Any())
        {
            throw new InvalidOperationException("The IFT transcription changed shape.");
        }

        var table = rows.Select(row => row.GetProperty("results").EnumerateArray().Select(cell => cell.GetString()!).ToArray()).ToArray();

        // The TEM the package uses must agree with the printed Terrain Chart cells.
        var printed = terrain.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(row => row.GetProperty("terrain").GetString()!, row => row.GetProperty("temIndirect").GetString()!, StringComparer.Ordinal);
        if (printed.GetValueOrDefault("1. Open Ground") != "FFMO: -1*" || printed.GetValueOrDefault("12. Brush") != "0"
            || printed.GetValueOrDefault("13. Woods") != "+1/-1" || printed.GetValueOrDefault("14. Orchard") != "0"
            || printed.GetValueOrDefault("15. Grain") != "0" || printed.GetValueOrDefault("23. Wooden Building") != "+2(+1*)"
            || printed.GetValueOrDefault("23. Stone Building") != "+3(+1*)")
        {
            throw new InvalidOperationException("The Terrain Chart TEM cells changed.");
        }

        var definitions = catalog.RootElement.GetProperty("definitions").EnumerateArray().Select(Definition)
            .ToDictionary(item => item.Id, StringComparer.Ordinal);
        return new ScenarioA1FireReference(table, definitions);
    }

    private static FireDefinition Definition(JsonElement item)
    {
        int? Value(string face, string attribute)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("attribute", out var name)
                    && name.GetString() == attribute && value.TryGetProperty("value", out var number) && number.ValueKind == JsonValueKind.Number)
                {
                    return number.GetInt32();
                }
            }

            return null;
        }

        return new FireDefinition(
            item.GetProperty("id").GetString()!,
            item.GetProperty("kind").GetString()!,
            item.GetProperty("nationality").GetString()!,
            item.TryGetProperty("class", out var cls) && cls.ValueKind == JsonValueKind.String ? cls.GetString() : null,
            Value("front", "firepower"),
            Value("front", "range"),
            Value("front", "morale"),
            Value("broken", "broken-morale"),
            Value("front", "leadership"));
    }
}
