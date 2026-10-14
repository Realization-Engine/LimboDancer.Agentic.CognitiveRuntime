using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public sealed record FormationSelection(string UnitId, string Anchor);

/// <summary>One source counter with a bounded, versioned setup composition. Not a playable ASL order of battle.</summary>
public static class FormationDeployment
{
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
        var (count, side, label, kind, symbol) = type switch
        {
            "uk-rifle" => (4, "Allied", "British rifle squad ", "rifle", "R"),
            "de-rifle" => (4, "German", "German rifle squad ", "rifle", "R"),
            "uk-engineer" => (4, "Allied", "British engineer squad ", "engineer", "E"),
            "de-81mm" => (6, "German", "German 81 mm mortar and crew ", "mortar", "M"),
            _ => throw new InvalidDataException("Select a deployed rifle, British engineer or German 81 mm mortar formation.")
        };
        var suffix = kind == "mortar" ? "/detachment-" : "/squad-";
        return Enumerable.Range(1, count).Select(i => new PilotUnit(selection.UnitId + suffix + i,
            selection.UnitId, side, label + i, kind, symbol)).ToArray();
    }

    public static string PolicyFor(IReadOnlyList<PilotUnit> units) => units[0].Kind switch
    {
        "engineer" => "goodwood-selected-engineer-1",
        "mortar" => "goodwood-selected-mortar-1",
        _ => Policy
    };

    public static string JobKey(string seed, FormationSelection? selection) => selection is null ? seed :
        BoardPackage.Hash(CanonicalJson.Serialize(new[] { seed, Policy, selection.UnitId, selection.Anchor }))[..32];
}