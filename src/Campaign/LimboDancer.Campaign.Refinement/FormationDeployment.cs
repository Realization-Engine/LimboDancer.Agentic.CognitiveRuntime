using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Campaign.Refinement;

public sealed record FormationSelection(string UnitId, string Anchor);

/// <summary>One source counter with a bounded, versioned setup composition. Not a playable ASL order of battle.</summary>
public static class FormationDeployment
{
    private static readonly Dictionary<string, (int Count, string Side, string Label, string Definition)> Guns =
        new Dictionary<string, (int, string, string, string)>(StringComparer.Ordinal)
        {
            ["uk-25pdr"] = (6, "Allied", "British 25-pounder", "campaign-british-25pdr"),
            ["de-75mm"] = (4, "German", "German 75 mm AT", "campaign-german-75mm-at"),
            ["de-88mm"] = (4, "German", "German 88 mm AT", "campaign-german-88mm-at"),
            ["de-20mm"] = (6, "German", "German 20 mm AA", "campaign-german-20mm-aa"),
            ["de-quad20"] = (3, "German", "German quadruple 20 mm AA", "campaign-german-quad20-aa"),
            ["de-nebel"] = (6, "German", "German Nebelwerfer", "campaign-german-nebelwerfer")
        };

    public const string Policy = "goodwood-selected-rifle-1";

    public static Crop CropFor(PilotInputs inputs, FormationSelection selection)
    {
        _ = Units(inputs, selection);
        var region = inputs.Regions.SingleOrDefault(r => r.Key == selection.Anchor)
            ?? throw new InvalidDataException("Selected formation anchor is not on the Goodwood map.");
        var x = region.Polygon.Average(p => (double)p.X);
        var y = region.Polygon.Average(p => (double)p.Y);
        // Four-column/two-row alignment preserves parity and integer raster translation between crops.
        var column = checked((int)Math.Round(x / SpatialFrame.ColumnMm)) - 6;
        column -= ((column % 4) + 4) % 4;
        var row = checked((int)Math.Round(y / 40000)) - 6;
        row -= ((row % 2) + 2) % 2;
        return new(column, row, 17, 14);
    }

    public static IReadOnlyList<PilotUnit> Units(PilotInputs inputs, FormationSelection selection)
    {
        if (selection is null || selection.UnitId is null ||
            !inputs.FormationTypes.TryGetValue(selection.UnitId, out var type))
            throw new InvalidDataException("Unknown Goodwood formation.");
        if (Guns.TryGetValue(type, out var gun))
            return Enumerable.Range(1, gun.Count).SelectMany(i => new PilotUnit[]
            {
                new(selection.UnitId + "/crew-" + i, selection.UnitId, gun.Side, gun.Label + " crew " + i,
                    "crew", "C", PilotCatalog.Crew(gun.Side),
                    "Reviewed crew profile; one crew per weapon is an explicit campaign game-default allotment."),
                new(selection.UnitId + "/gun-" + i, selection.UnitId, gun.Side, gun.Label + " " + i,
                    "gun", "G", PilotCatalog.GameDefaultGun(gun.Definition, gun.Side),
                    "Manufactured MFG profile, not historical data. Weapon count follows the source composition table. Initial facing " +
                    (gun.Side == "Allied" ? "east" : "west") + " is a game default for review in ASL setup. Combat remains limited to existing engine support.",
                    new Holding(selection.UnitId + "/crew-" + i, HoldingRole.Manned))
            }).ToArray();
        var (count, side, label, kind, symbol) = type switch
        {
            "uk-rifle" => (4, "Allied", "British rifle squad ", "rifle", "R"),
            "de-rifle" => (4, "German", "German rifle squad ", "rifle", "R"),
            "uk-engineer" => (4, "Allied", "British engineer squad ", "engineer", "E"),
            "de-halftrack" => (5, "German", "German SPW 251/1 ", "vehicle", "HT"),
            "de-81mm" => (6, "German", "German 81 mm mortar and crew ", "mortar", "M"),
            _ => throw new InvalidDataException("Select a deployed rifle, British engineer German 81 mm mortar or German halftrack formation.")
        };
        if (kind == "vehicle")
            return Enumerable.Range(1, count).Select(i => new PilotUnit(selection.UnitId + "/vehicle-" + i,
                selection.UnitId, side, label + i, kind, symbol, PilotCatalog.GermanHalftrack(),
                "Reviewed SPW 251/1 profile; five vehicles per formation from the source composition table. Initial ASL facing is west, a game default. Review facing in ASL setup.",
                AllotmentStatus: "Integrated vehicle crew and AAMG remain vehicle capabilities. No extra crew, passenger or equipment counters are issued by this formation.")).ToArray();
        if (kind == "mortar")
            return Enumerable.Range(1, 6).SelectMany(i => new PilotUnit[]
            {
                new(selection.UnitId + "/crew-" + i, selection.UnitId, side, "German mortar crew " + i,
                    "crew", "C", PilotCatalog.GermanCrew(), PilotCatalog.CrewStatus),
                new(selection.UnitId + "/mortar-" + i, selection.UnitId, side, "German 81 mm mortar " + i,
                    "mortar", "M", PilotCatalog.GermanMortar(), PilotCatalog.MortarStatus, new Holding(selection.UnitId + "/crew-" + i, HoldingRole.Manned))
            }).ToArray();
        var suffix = "/squad-";
        return Enumerable.Range(1, count).Select(i => PilotCatalog.Infantry(selection.UnitId + suffix + i,
            selection.UnitId, side, label + i, kind, symbol)).ToArray();
    }

    public static string PolicyFor(IReadOnlyList<PilotUnit> units) => units[0].Kind switch
    {
        "vehicle" => "goodwood-selected-halftrack-1",
        "engineer" => "goodwood-selected-engineer-1",
        "crew" when units.Any(u => u.Kind == "gun") => "goodwood-selected-gun-1",
        "crew" => "goodwood-selected-mortar-2",
        _ => Policy
    };

    public static string JobKey(string seed, FormationSelection? selection) => selection is null ? seed :
        BoardPackage.Hash(CanonicalJson.Serialize(new[] { seed, Policy, selection.UnitId, selection.Anchor }))[..32];
}