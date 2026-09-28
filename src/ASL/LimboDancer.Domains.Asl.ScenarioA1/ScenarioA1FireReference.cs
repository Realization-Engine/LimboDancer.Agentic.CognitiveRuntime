using System.Text.Json;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A reviewed catalog definition as the Scenario A1 packages read it: printed values only (manufactured ones for the MGs
/// of catalog 1.2.0, ruling R0.3). Values a definition does not print are null.
/// </summary>
public sealed record FireDefinition(
    string Id,
    string Kind,
    string Nationality,
    string? Class,
    int? Firepower,
    int? Range,
    int? Morale,
    int? BrokenMorale,
    int? Leadership,
    bool? UnderscoredMorale)
{
    /// <summary>Self-Rally capability: the broken Morale Level printed in a square (A10.63).</summary>
    public bool? SelfRally
    {
        get; init;
    }

    /// <summary>The Assault Fire bonus: an underlined FP (A1.21, A7.36).</summary>
    public bool? AssaultFire
    {
        get; init;
    }

    /// <summary>A SW's Breakdown Number (A9.7).</summary>
    public int? Breakdown
    {
        get; init;
    }

    /// <summary>A MG's Multiple ROF (A9.2); null when it has none.</summary>
    public int? RateOfFire
    {
        get; init;
    }

    /// <summary>A SW's Repair Number (A9.72).</summary>
    public int? Repair
    {
        get; init;
    }

    /// <summary>A hero's wounded side (A15.2): FP, range, and Morale Level; null for other units.</summary>
    public int? WoundedFirepower
    {
        get; init;
    }

    public int? WoundedRange
    {
        get; init;
    }

    public int? WoundedMorale
    {
        get; init;
    }

    public bool IsLeader => Kind == "asl:leader";

    public bool IsHero => Kind == "asl:hero";

    public bool IsMmc => Kind is "asl:squad" or "asl:half-squad";

    public bool IsMg => Kind == "asl:mg";

    /// <summary>A vehicle's movement type (D1.1): truck, half-tracked, and so on.</summary>
    public string? MovementType
    {
        get; init;
    }

    /// <summary>A vehicle's printed MP allotment (D1.1).</summary>
    public int? MovementPoints
    {
        get; init;
    }

    /// <summary>A vehicle's Towing Number (C10.1); null when it cannot tow.</summary>
    public int? Towing
    {
        get; init;
    }

    /// <summary>Whether a vehicle is unarmored (D1.21).</summary>
    public bool? Unarmored
    {
        get; init;
    }

    /// <summary>Whether an AFV is open-topped (D1.23).</summary>
    public bool? OpenTopped
    {
        get; init;
    }

    /// <summary>A vehicle's MA weapon (D1.3): <c>aamg</c> for an MA AAMG.</summary>
    public string? MainArmament
    {
        get; init;
    }

    /// <summary>A vehicle's AAMG FP (D1.8, D1.83).</summary>
    public int? AntiAircraftMg
    {
        get; init;
    }

    public bool IsVehicle => Kind == "asl:vehicle";
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

    // The Casualty Reduction a squad suffers (A7.302): its half-squad of the same class (catalogs 1.1.0 and 1.3.0).
    private static readonly IReadOnlyDictionary<string, string> HalfSquads = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-squad"] = "attacker-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-conscript-squad"] = "attacker-conscript-half-squad",
        ["attacker-elite-squad"] = "attacker-elite-half-squad",
        ["defender-squad"] = "defender-half-squad",
        ["defender-conscript-squad"] = "defender-conscript-half-squad",
        ["defender-elite-squad"] = "defender-elite-half-squad",
        ["defender-line-squad"] = "defender-line-half-squad",
        ["defender-guards-squad"] = "defender-guards-half-squad",
    };

    // The leader grades from worst to best (Chapter H leader table, p. 331; A15.3): 6+1, 7-0, 8-0, 8-1, 9-1, 9-2, 10-2, 10-3.
    private static readonly string[] GermanLeaders =
        [.. new[] { "6-plus-1", "7-0", "8-0", "8-1", "9-1", "9-2", "10-2", "10-3" }.Select(grade => "attacker-leader-" + grade)];

    private static readonly string[] RussianLeaders =
    [
        "defender-leader-6-plus-1", "defender-leader-7-0", "defender-leader", "defender-leader-8-1", "defender-leader-9-1", "defender-leader-9-2",
        "defender-leader-10-2", "defender-leader-10-3",
    ];

    // Battle Hardening (A15.3): the unit of the same size and next higher quality, no part of whose Strength Factor falls,
    // gaining the least (ruling R28.6: the smallest summed increase, then the fewest added capabilities, so a German
    // Conscript becomes a 4-4-7, not a squared 5-3-7 that adds smoke and Assault Fire). The Russians have no Green or plain
    // 2nd Line class, so a Russian Conscript becomes NKVD (A25.25: 2nd Line). Elite MMC and the 10-3 are the highest
    // quality and become Fanatic instead, as does an NKVD MMC (A25.25).
    private static readonly IReadOnlyDictionary<string, string> Hardened = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-conscript-squad"] = "attacker-2nd-line-squad",
        ["attacker-conscript-half-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-squad",
        ["attacker-2nd-line-half-squad"] = "attacker-half-squad",
        ["attacker-squad"] = "attacker-elite-squad",
        ["attacker-half-squad"] = "attacker-elite-half-squad",
        ["defender-conscript-squad"] = "defender-nkvd-squad",
        ["defender-conscript-half-squad"] = "defender-nkvd-half-squad",
        ["defender-line-squad"] = "defender-guards-squad",
        ["defender-line-half-squad"] = "defender-guards-half-squad",
        ["defender-squad"] = "defender-elite-squad",
        ["defender-half-squad"] = "defender-elite-half-squad",
    };

    private static readonly HashSet<string> HighestQuality = new(StringComparer.Ordinal)
    {
        "attacker-elite-squad", "attacker-elite-half-squad", "defender-elite-squad", "defender-elite-half-squad", "defender-guards-squad",
        "defender-guards-half-squad", "attacker-leader-10-3", "defender-leader-10-3", "defender-nkvd-squad", "defender-nkvd-half-squad",
    };

    // The NKVD MMC (A25.25): 2nd Line, ELR 5, a -1 Heat of Battle DRM, Fanatic when Battle Hardened, Commissars by Field Promotion.
    private static readonly HashSet<string> Nkvd = new(StringComparer.Ordinal) { "defender-nkvd-squad", "defender-nkvd-half-squad" };

    // ELR Replacement (A19.13): a unit of lesser quality and the same size, whose Class drops and no part of whose
    // Strength Factor rises; a leader of the next lower quality. A unit missing here cannot be Replaced (A19.12).
    private static readonly IReadOnlyDictionary<string, string> Replacements = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["attacker-squad"] = "attacker-2nd-line-squad",
        ["attacker-half-squad"] = "attacker-2nd-line-half-squad",
        ["attacker-2nd-line-squad"] = "attacker-conscript-squad",
        ["attacker-2nd-line-half-squad"] = "attacker-conscript-half-squad",
        ["attacker-elite-squad"] = "attacker-squad",
        ["attacker-elite-half-squad"] = "attacker-half-squad",
        ["defender-squad"] = "defender-conscript-squad",
        ["defender-half-squad"] = "defender-conscript-half-squad",
        ["defender-elite-squad"] = "defender-squad",
        ["defender-elite-half-squad"] = "defender-half-squad",
        ["defender-guards-squad"] = "defender-line-squad",
        ["defender-guards-half-squad"] = "defender-line-half-squad",
        ["defender-line-squad"] = "defender-conscript-squad",
        ["defender-line-half-squad"] = "defender-conscript-half-squad",
        ["defender-nkvd-squad"] = "defender-conscript-squad",
        ["defender-nkvd-half-squad"] = "defender-conscript-half-squad",
    };

    private readonly string[][] results;

    private readonly int[] killNumbers;

    private ScenarioA1FireReference(string[][] results, int[] killNumbers, IReadOnlyDictionary<string, FireDefinition> definitions)
    {
        this.results = results;
        this.killNumbers = killNumbers;
        Definitions = definitions;
    }

    /// <summary>The Vehicle line's Kill Number for a column index (A7.308).</summary>
    public int KillNumber(int column) => killNumbers[column];

    /// <summary>
    /// The Morale Level of an AFV's Inherent crew (D5.1): that of its nationality's best unbroken elite Infantry MMC, read from the
    /// reviewed catalog's elite squads and half-squads; null when the catalog has none.
    /// </summary>
    public int? AfvCrewMorale(string nationality) =>
        Definitions.Values.Where(item => item.IsMmc && item.Nationality == nationality && item.Class == "elite").Max(item => item.Morale);

    public IReadOnlyDictionary<string, FireDefinition> Definitions
    {
        get;
    }

    /// <summary>The IFT result for a Final DR and a column index; <c>none</c> is the printed dash.</summary>
    public string Result(int finalDr, int column) => results[Math.Clamp(finalDr, 0, 15)][column];

    /// <summary>The half-squad a squad is Reduced to, or null when the catalog has none.</summary>
    public static string? HalfSquadOf(string definitionId) => HalfSquads.GetValueOrDefault(definitionId);

    /// <summary>The unit that Replaces a definition under A19.13, or null when none can (A19.12): a leader of the next lower grade.</summary>
    public static string? ReplacementOf(string definitionId) =>
        Replacements.GetValueOrDefault(definitionId) ?? Step(definitionId, -1);

    /// <summary>The unit a definition is Battle Hardened into (A15.3), or null for the highest quality or an unreviewed one.</summary>
    public static string? HardenedOf(string definitionId) => Hardened.GetValueOrDefault(definitionId) ?? Step(definitionId, 1);

    /// <summary>Whether Battle Hardening makes a definition Fanatic rather than exchanging it (A15.3, A25.25).</summary>
    public static bool IsHighestQuality(string definitionId) => HighestQuality.Contains(definitionId);

    /// <summary>Whether a definition is an NKVD MMC (A25.25).</summary>
    public static bool IsNkvd(string definitionId) => Nkvd.Contains(definitionId);

    private static string? Step(string definitionId, int by)
    {
        foreach (var chain in new[] { GermanLeaders, RussianLeaders })
        {
            var index = Array.IndexOf(chain, definitionId);
            if (index >= 0)
            {
                var next = index + by;
                return next >= 0 && next < chain.Length ? chain[next] : null;
            }
        }

        return null;
    }

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

        // A7.308: the Vehicle line's Kill Number beneath each FP column (IFT, p. 692).
        using var vehicleLine = ScenarioA1FirePackage.Read("ScenarioA1.fire-ift-vehicle-line.json", matrix.GetProperty("vehicleLineTranscriptionSha256").GetString()!);
        var kills = vehicleLine.RootElement.GetProperty("killNumbers").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        if (!vehicleLine.RootElement.GetProperty("columns").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(ColumnFp)
            || !kills.SequenceEqual([3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13]))
        {
            throw new InvalidOperationException("The IFT Vehicle line transcription changed.");
        }

        return new ScenarioA1FireReference(table, kills, ReadDefinitions(catalog.RootElement));
    }

    /// <summary>Every definition of a reviewed catalog, by id.</summary>
    internal static Dictionary<string, FireDefinition> ReadDefinitions(JsonElement catalog) =>
        catalog.GetProperty("definitions").EnumerateArray().Select(Definition).ToDictionary(item => item.Id, StringComparer.Ordinal);

    private static FireDefinition Definition(JsonElement item)
    {
        string? Text(string face, string attribute)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("attribute", out var name)
                    && name.GetString() == attribute && value.TryGetProperty("value", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }

            return null;
        }

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

        bool? Trait(string face, string trait)
        {
            foreach (var value in item.GetProperty("values").EnumerateArray())
            {
                if (value.GetProperty("face").GetString() == face && value.TryGetProperty("trait", out var name) && name.GetString() == trait)
                {
                    return value.TryGetProperty("present", out var present) ? present.GetBoolean() : null;
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
            Value("front", "leadership"),
            Trait("front", "asl:elr-5"))
        {
            SelfRally = Trait("broken", "asl:self-rally"),
            AssaultFire = Trait("front", "asl:assault-fire"),
            Breakdown = Value("front", "breakdown"),
            RateOfFire = Value("front", "rate-of-fire"),
            Repair = Value("malfunctioned", "repair"),
            WoundedFirepower = Value("wounded", "firepower"),
            WoundedRange = Value("wounded", "range"),
            WoundedMorale = Value("wounded", "morale"),
            MovementType = Text("front", "movement-type"),
            MovementPoints = Value("front", "movement-points"),
            Towing = Value("front", "towing"),
            Unarmored = Trait("front", "asl:unarmored"),
            OpenTopped = Trait("front", "asl:open-topped"),
            MainArmament = Text("front", "ma-weapon"),
            AntiAircraftMg = Value("front", "aamg"),
        };
    }
}
