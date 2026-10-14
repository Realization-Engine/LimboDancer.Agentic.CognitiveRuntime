using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationAddressesTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Fact]
    public void AllOwnedCellsRoundTripAndOverlappingMarginsAreNotPlayableAddresses()
    {
        var addresses = new SituationAddresses(Inputs, Seed);
        Assert.Equal(37788, addresses.Cells.Count);
        foreach (var at in addresses.Cells)
        {
            Assert.Equal(at, addresses.At(at.Board, at.Hex));
            Assert.Equal(at, addresses.At(at.Column, at.Row));
            var tile = addresses.Tiles.Single(t => t.Key == at.Tile);
            Assert.Equal(at, addresses.FromCrop(tile.Crop, at.Hex));
        }
        foreach (var tile in addresses.Tiles)
        {
            var sample = addresses.Cells.First(a => a.Tile == tile.Key);
            var margin = tile.Crop.Geometry().NameOf(new HexIndex(0, 0)).ToString();
            Assert.Throws<InvalidDataException>(() => addresses.At(sample.Board, margin));
        }
        Assert.Throws<InvalidDataException>(() => addresses.At(int.MinValue, int.MinValue));
    }

    [Fact]
    public void AcceptedCrewAndWeaponCoordinatesSurviveTileAddressingAndJsonReload()
    {
        var selection = new FormationSelection("de-81mm-01", "A/V-7");
        var crop = FormationDeployment.CropFor(Inputs, selection);
        var deployment = new PilotDeployment(Inputs, new PilotGenerator(Inputs).Generate(Seed, crop), Seed, selection);
        var context = new DeploymentContext("test", "preserve", [], [selection.Anchor], selection);
        var setup = deployment.SetComplete(context, deployment.AutoDeploy(context, deployment.Create(context)), true);
        var before = JsonSerializer.Serialize(setup);
        var addresses = new SituationAddresses(Inputs, Seed);
        var positions = addresses.Preserve(deployment, context, setup);
        var restored = JsonSerializer.Deserialize<SituationUnitPosition[]>(JsonSerializer.Serialize(positions))!;
        Assert.Equal(12, restored.Length);
        foreach (var position in restored)
        {
            var old = setup.Placements.Single(p => p.UnitId == position.UnitId);
            Assert.Equal(addresses.FromCrop(crop, old.Hex), addresses.At(position.Column, position.Row));
        }
        foreach (var link in deployment.Equipment)
        {
            var weapon = restored.Single(p => p.UnitId == link.EquipmentId);
            var crew = restored.Single(p => p.UnitId == link.Holding.Holder);
            Assert.Equal((crew.Column, crew.Row), (weapon.Column, weapon.Row));
        }
        Assert.Equal(before, JsonSerializer.Serialize(setup));
        Assert.Throws<InvalidDataException>(() => new SituationAddresses(Inputs, "ffeeddccbbaa99887766554433221100")
            .Preserve(deployment, context, setup));
    }
}
