using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement;

public sealed record SituationAddress(int Column, int Row, string Parent, string Tile, string Board, string Hex);
public sealed record SituationUnitPosition(string UnitId, int Column, int Row);

/// <summary>Unique playable addresses. Compilation margins never acquire a second unit identity.</summary>
public sealed class SituationAddresses
{
    private readonly Dictionary<(int Column, int Row), SituationAddress> world = new();
    private readonly Dictionary<(string Board, string Hex), SituationAddress> local = new();
    public IReadOnlyList<SituationTile> Tiles { get; }
    public string Seed { get; }
    public IReadOnlyCollection<SituationAddress> Cells => world.Values;

    public SituationAddresses(PilotInputs source, string seed)
    {
        SeedRandom.ValidateSeed(seed);
        Seed = seed;
        var inputs = source.ForVersion(PilotInputs.SituationGeneratorVersion);
        Tiles = SituationCoverage.Plan(inputs);
        foreach (var tile in Tiles)
        {
            var board = PilotIdentity.Reference(inputs, seed, tile.Crop);
            var geometry = tile.Crop.Geometry();
            foreach (var cell in tile.Cells)
            {
                var hex = geometry.NameOf(new HexIndex(cell.Column - tile.Crop.Column, cell.Row - tile.Crop.Row)).ToString();
                var address = new SituationAddress(cell.Column, cell.Row, cell.Parent, tile.Key, board, hex);
                world.Add((cell.Column, cell.Row), address);
                local.Add((board, hex), address);
            }
        }
    }

    public SituationAddress At(int column, int row) => world.TryGetValue((column, row), out var address)
        ? address : throw new InvalidDataException("Position is outside the Situation's owned map cells.");

    public SituationAddress At(string board, string hex) => local.TryGetValue((board, hex), out var address)
        ? address : throw new InvalidDataException("Position is not an owned hex of this Situation board.");

    public SituationAddress FromCrop(Crop crop, string hex)
    {
        var index = crop.Geometry().IndexOf(HexName.Parse(hex));
        return At(checked(crop.Column + index.Column), checked(crop.Row + index.Row));
    }

    // Position transfer only. The destination terrain and setup legality must still be checked before admission.
    // Do not regenerate the saved pilot or auto-deploy its units while extracting accepted coordinates.
    public SituationUnitPosition[] Preserve(PilotDeployment deployment, DeploymentContext context, PilotSetup setup)
    {
        if (deployment.Bundle.Seed != Seed)
            throw new InvalidDataException("Saved deployment belongs to another terrain seed.");
        deployment.Validate(context, setup);
        return setup.Placements.OrderBy(p => p.UnitId, StringComparer.Ordinal).Select(p =>
        {
            var at = FromCrop(deployment.Crop, p.Hex);
            return new SituationUnitPosition(p.UnitId, at.Column, at.Row);
        }).ToArray();
    }
}
