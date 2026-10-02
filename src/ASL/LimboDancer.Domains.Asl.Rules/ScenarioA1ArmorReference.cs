using System.Globalization;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The charts of the Vehicle Target Type and the To Kill resolution (backlog pass 7, rulings R7.2 to R7.7): the C3 To Hit Table's Vehicle
/// row and the C4 APCR row (p. 700), the C7.31 to C7.34 To Kill Tables with their Case D rows and unarmored values (p. 701), and the armor
/// of the catalog's vehicles (D1.6, D1.7, D5.6).
/// </summary>
public sealed class ScenarioA1ArmorReference
{
    // The AF scale of D1.6; a superior or inferior turret is the next AF up or down it (D1.63, D1.64).
    private static readonly int[] ArmorScale = [0, 1, 2, 3, 4, 6, 8, 11, 14, 18, 26];

    // The nationalities each colored To Kill entry applies to (the C7.31 and C7.32 notes).
    private static readonly Dictionary<string, string[]> Notes = new(StringComparer.Ordinal)
    {
        ["russian-japanese"] = ["russian", "japanese"],
        ["russian"] = ["russian"],
        ["us"] = ["american"],
        ["us-grant"] = ["american"],
        ["british"] = ["british"],
        ["italian"] = ["italian"],
        ["japanese-year-38"] = ["japanese"],
        ["russian-finnish-japanese-minor"] = ["russian", "finnish", "japanese"],
    };

    private readonly IReadOnlyDictionary<string, int[]> vehicle;
    private readonly int[] apcrToHit;
    private readonly (int Tk, string Gun, string? Note)[] ap;
    private readonly (int Tk, string Gun, string? Note)[] apcr;
    private readonly (int Tk, string Gun)[] heat;
    private readonly (int Minimum, int Armored, int Unarmored)[] he;
    private readonly IReadOnlyDictionary<string, object?[]> apCaseD;
    private readonly IReadOnlyDictionary<string, object?[]> apcrCaseD;
    private readonly (int Minimum, int? Maximum)[] apRanges;
    private readonly (int Minimum, int? Maximum)[] apcrRanges;

    private ScenarioA1ArmorReference(IReadOnlyDictionary<string, int[]> vehicle, int[] apcrToHit, (int, string, string?)[] ap, (int, string, string?)[] apcr,
        (int, string)[] heat, (int, int, int)[] he, IReadOnlyDictionary<string, object?[]> apCaseD, IReadOnlyDictionary<string, object?[]> apcrCaseD,
        (int, int?)[] apRanges, (int, int?)[] apcrRanges, IReadOnlyDictionary<string, ArmorDefinition> vehicles)
    {
        this.vehicle = vehicle;
        this.apcrToHit = apcrToHit;
        this.ap = ap;
        this.apcr = apcr;
        this.heat = heat;
        this.he = he;
        this.apCaseD = apCaseD;
        this.apcrCaseD = apcrCaseD;
        this.apRanges = apRanges;
        this.apcrRanges = apcrRanges;
        Vehicles = vehicles;
    }

    /// <summary>The catalog's vehicles by definition id.</summary>
    public IReadOnlyDictionary<string, ArmorDefinition> Vehicles
    {
        get;
    }

    /// <summary>The Vehicle row's Basic TH# (C3.31) at a To Hit Table range column.</summary>
    public int BasicToHit(string color, int column) => vehicle[color][column];

    /// <summary>The C4 APDS/APCR modification of the Basic TH# at a To Hit Table range column (C4.5).</summary>
    public int ApcrToHit(int column) => apcrToHit[column];

    /// <summary>The printed name of a Gun's size on the To Kill Tables: its caliber and barrel length (C4.1).</summary>
    public static string GunSize(GunDefinition gun)
    {
        ArgumentNullException.ThrowIfNull(gun);
        // C13.2 (pass 9b): an ATR has its own entries on the AP To Kill Table.
        if (gun.LatwType == "atr")
        {
            return "ATR";
        }

        return gun.Caliber.ToString(CultureInfo.InvariantCulture) + gun.Suffix switch
        {
            "star" => "*",
            "l" => "L",
            "ll" => "LL",
            _ => string.Empty,
        };
    }

    private static int? Lookup((int Tk, string Gun, string? Note)[] table, GunDefinition gun)
    {
        var size = GunSize(gun);
        return table.Where(item => item.Gun == size && item.Note is { } note && Notes.TryGetValue(note, out var nations) && nations.Contains(gun.Nationality))
                .Select(item => (int?)item.Tk).FirstOrDefault()
            ?? table.Where(item => item.Gun == size && item.Note is null).Select(item => (int?)item.Tk).FirstOrDefault();
    }

    /// <summary>The Basic TK# of an ammunition against an armored target (C7.31 to C7.34); null when the table lists no such Gun.</summary>
    public int? BasicTk(string ammunition, GunDefinition gun)
    {
        ArgumentNullException.ThrowIfNull(gun);
        return ammunition switch
        {
            "ap" => Lookup(ap, gun),
            "apcr" => Lookup(apcr, gun),
            "heat" => heat.Where(item => item.Gun == (gun.HeatRow ?? gun.Caliber.ToString(CultureInfo.InvariantCulture))).Select(item => (int?)item.Tk).FirstOrDefault(),
            "he" => he.Where(item => gun.Caliber >= item.Minimum).Select(item => (int?)item.Armored).LastOrDefault(),
            _ => null,
        };
    }

    /// <summary>
    /// The Case D change of AP and APCR against an armored Target Facing at a range (C7.24); zero for HEAT and HE, and null where the table
    /// gives none (NA) or has no row for the caliber.
    /// </summary>
    public int? CaseD(string ammunition, GunDefinition gun, int range)
    {
        ArgumentNullException.ThrowIfNull(gun);
        (IReadOnlyDictionary<string, object?[]> Rows, (int Minimum, int? Maximum)[] Ranges, string? Row) pick = ammunition switch
        {
            "ap" => (apCaseD, apRanges, gun.Caliber <= 25 ? "atMost25mm" : gun.Caliber is >= 37 and <= 57 ? "37to57mm" : gun.Caliber >= 65 ? "atLeast65mm" : null),
            "apcr" => (apcrCaseD, apcrRanges, gun.Caliber <= 57 ? "apcrAtMost57mm" : gun.Caliber >= 75 ? "apcrAtLeast75mm" : null),
            _ => (apCaseD, apRanges, null),
        };
        if (ammunition is "heat" or "he")
        {
            return 0;
        }

        if (pick.Row is null)
        {
            return null;
        }

        var column = Array.FindIndex(pick.Ranges, item => range >= item.Minimum && (item.Maximum is null || range <= item.Maximum));
        return column >= 0 && pick.Rows[pick.Row][column] is int value ? value : null;
    }

    /// <summary>
    /// The Final TK# of an ammunition against an unarmored vehicle (C7.311, C7.321, C7.331, C7.342), before a Critical Hit doubles it; null
    /// when the table gives none for the caliber.
    /// </summary>
    public int? UnarmoredTk(string ammunition, GunDefinition gun)
    {
        ArgumentNullException.ThrowIfNull(gun);
        return ammunition switch
        {
            "ap" or "apcr" => gun.Caliber switch
            {
                <= 28 => 7,
                >= 37 and <= 57 => 8,
                >= 65 and <= 84 => 9,
                >= 85 and <= 95 => 10,
                >= 100 => 11,
                _ => null,
            },
            "heat" => 11,
            "he" => he.Where(item => gun.Caliber >= item.Minimum).Select(item => (int?)item.Unarmored).LastOrDefault(),
            _ => null,
        };
    }

    /// <summary>The AF a hit meets (D1.6, D1.63, D1.64): the hull's front or side/rear AF, or the turret's, one step up or down where it differs.</summary>
    public static int? ArmorFactor(ArmorDefinition target, string location, string facing)
    {
        ArgumentNullException.ThrowIfNull(target);
        var hull = facing == "front" ? target.FrontAf : target.SideAf;
        if (hull is not { } af || location != "turret")
        {
            return hull;
        }

        var turret = facing == "front" ? target.TurretFront : target.TurretSide;
        var index = Array.IndexOf(ArmorScale, af);
        return turret switch
        {
            "superior" when index >= 0 && index + 1 < ArmorScale.Length => ArmorScale[index + 1],
            "inferior" when index > 0 => ArmorScale[index - 1],
            _ => af,
        };
    }

    internal static ScenarioA1ArmorReference Load(JsonElement matrix, JsonDocument catalog)
    {
        using var toHit = ScenarioA1FirePackage.Read("ScenarioA1.ordnance-to-hit-vehicle.json", matrix.GetProperty("toHitVehicleTranscriptionSha256").GetString()!);
        using var toKill = ScenarioA1FirePackage.Read("ScenarioA1.ordnance-to-kill.json", matrix.GetProperty("toKillTranscriptionSha256").GetString()!);
        static int[] Row(JsonElement element) => [.. element.EnumerateArray().Select(item => item.GetInt32())];
        var hitRoot = toHit.RootElement;
        var vehicle = hitRoot.GetProperty("vehicle").EnumerateObject().ToDictionary(item => item.Name, item => Row(item.Value), StringComparer.Ordinal);
        var apcrToHit = Row(hitRoot.GetProperty("apdsApcr"));
        if (vehicle.Count != 2 || vehicle.Values.Any(row => row.Length != 10) || apcrToHit.Length != 10)
        {
            throw new InvalidOperationException("The To Hit Table's Vehicle row transcription changed shape.");
        }

        var root = toKill.RootElement;
        static (int, string, string?)[] Columns(JsonElement table) => [.. table.GetProperty("basic").EnumerateArray()
            .SelectMany(item => item.GetProperty("guns").EnumerateArray().Select(gun =>
            {
                var parts = gun.GetString()!.Split(':');
                return (item.GetProperty("tk").GetInt32(), parts[0], parts.Length > 1 ? parts[1] : (string?)null);
            }))];
        static (int, int?)[] Ranges(JsonElement table) => [.. table.GetProperty("caseDRanges").EnumerateArray().Select(item =>
        {
            var text = item.GetString()!;
            if (text.EndsWith('+'))
            {
                return (int.Parse(text.TrimEnd('+'), CultureInfo.InvariantCulture), (int?)null);
            }

            var bounds = text.Split('-');
            return (int.Parse(bounds[0], CultureInfo.InvariantCulture), (int?)int.Parse(bounds[^1], CultureInfo.InvariantCulture));
        })];
        static IReadOnlyDictionary<string, object?[]> CaseD(JsonElement table) => table.GetProperty("caseD").EnumerateObject()
            .ToDictionary(item => item.Name, item => item.Value.EnumerateArray().Select(value => value.ValueKind == JsonValueKind.Number ? (object?)value.GetInt32() : null).ToArray(),
                StringComparer.Ordinal);
        var heRoot = root.GetProperty("he");
        var bands = heRoot.GetProperty("bands").EnumerateArray().Select(item => int.Parse(item.GetString()!.TrimEnd('+'), CultureInfo.InvariantCulture)).ToArray();
        var armored = Row(heRoot.GetProperty("armoredBasic"));
        var unarmored = Row(heRoot.GetProperty("unarmoredFinal"));
        (int, int, int)[] he = [.. bands.Select((minimum, index) => (minimum, armored[index], unarmored[index]))];
        (int, string)[] heat = [.. root.GetProperty("heat").GetProperty("basic").EnumerateArray()
            .SelectMany(item => item.GetProperty("guns").EnumerateArray().Select(gun => (item.GetProperty("tk").GetInt32(), gun.GetString()!)))];

        var vehicles = catalog.RootElement.GetProperty("definitions").EnumerateArray().Where(item => item.GetProperty("kind").GetString() == "asl:vehicle")
            .Select(Vehicle).ToDictionary(item => item.Id, StringComparer.Ordinal);
        return new ScenarioA1ArmorReference(vehicle, apcrToHit, Columns(root.GetProperty("ap")), Columns(root.GetProperty("apcr")), heat, he,
            CaseD(root.GetProperty("ap")), CaseD(root.GetProperty("apcr")), Ranges(root.GetProperty("ap")), Ranges(root.GetProperty("apcr")), vehicles);
    }

    private static ArmorDefinition Vehicle(JsonElement item)
    {
        JsonElement? Find(string key, string name) => item.GetProperty("values").EnumerateArray()
            .Where(value => value.TryGetProperty(key, out var found) && found.GetString() == name).Cast<JsonElement?>().FirstOrDefault();
        int? Number(string name) => Find("attribute", name) is { } value && value.TryGetProperty("value", out var number) && number.ValueKind == JsonValueKind.Number
            ? number.GetInt32() : null;
        string? Text(string name) => Find("attribute", name) is { } value && value.TryGetProperty("value", out var text) && text.ValueKind == JsonValueKind.String
            ? text.GetString() : null;
        bool Trait(string name) => Find("trait", name) is { } value && value.TryGetProperty("present", out var present) && present.GetBoolean();
        return new ArmorDefinition(item.GetProperty("id").GetString()!, item.GetProperty("nationality").GetString()!, Trait("asl:unarmored"), Number("af-front"),
            Number("af-side"), Text("turret-af-front"), Text("turret-af-side"), Text("ma-type"), Text("target-size"), Number("crew-survival"),
            Trait("asl:cs-passengers-only"), Trait("asl:open-topped"));
    }
}
