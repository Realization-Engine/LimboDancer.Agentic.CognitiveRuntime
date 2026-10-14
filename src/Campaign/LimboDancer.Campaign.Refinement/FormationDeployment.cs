using LimboDancer.Domains.Asl.Maps.Features;

namespace LimboDancer.Campaign.Refinement;

public sealed record FormationSelection(string UnitId, string Anchor);

/// <summary>One source rifle counter, using the provisional four-squad composition. Not historical personnel inference.</summary>
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
            !inputs.RifleSides.TryGetValue(selection.UnitId, out var side))
            throw new InvalidDataException("Select a deployed British or German rifle formation. Other types are not admitted.");
        var label = side == "Allied" ? "British rifle squad " : "German rifle squad ";
        return Enumerable.Range(1, 4).Select(i => new PilotUnit(selection.UnitId + "/squad-" + i, selection.UnitId, side, label + i)).ToArray();
    }

    public static string JobKey(string seed, FormationSelection? selection) => selection is null ? seed :
        BoardPackage.Hash(CanonicalJson.Serialize(new[] { seed, Policy, selection.UnitId, selection.Anchor }))[..32];
}