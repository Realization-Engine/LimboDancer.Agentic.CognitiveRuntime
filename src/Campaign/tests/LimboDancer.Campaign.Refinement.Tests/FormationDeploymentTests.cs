using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class FormationDeploymentTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Theory]
    [InlineData("uk-rifle-02", "A/X-6", "Allied")]
    [InlineData("de-rifle-03", "A/V-7", "German")]
    public void SelectedFormationDeploysFourSquadsInsideItsAnchorAndResumes(string id, string anchor, string side)
    {
        var selection = new FormationSelection(id, anchor);
        var crop = FormationDeployment.CropFor(Inputs, selection);
        Assert.Equal(0, crop.Column % 4); Assert.Equal(0, crop.Row % 2);
        var result = new PilotGenerator(Inputs).Generate(Seed, crop);
        var p = new PilotDeployment(Inputs, result, Seed, selection);
        var context = new DeploymentContext("test", "selected-placement", side == "Allied" ? [anchor] : [],
            side == "German" ? [anchor] : [], selection);
        Assert.Equal(4, p.Units.Count);
        Assert.All(p.Units, u => { Assert.Equal(id, u.ParentId); Assert.Equal(side, u.Side); });
        var setup = p.SetComplete(context, p.AutoDeploy(context, p.Create(context)), true);
        Assert.Equal(4, setup.Placements.Length);
        Assert.All(setup.Placements, placement => Assert.Equal(anchor, p.Cells.Single(c => c.Hex == placement.Hex).Parent));
        var outside = p.Cells.First(c => c.Admitted && c.Parent != anchor);
        Assert.Contains("selected formation", p.Rejection(context, setup, p.Units[0].Id, outside.Hex));
        var reload = new PilotDeployment(Inputs, p.Bundle, selection: selection);
        reload.Validate(context, setup);
        Assert.Equal(JsonSerializer.Serialize(p.Cells), JsonSerializer.Serialize(reload.Cells));
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, p.Bundle));
        Assert.Throws<InvalidDataException>(() => reload.Validate(context with { FormationSignature = "moved" }, setup));
        Assert.Throws<InvalidDataException>(() => reload.Validate(context with { Formation = selection with { UnitId = "uk-rifle-01" } }, setup));
        Assert.Throws<InvalidDataException>(() => reload.Create(context with { AlliedParents = [], GermanParents = [] }));
    }

    [Fact]
    public void UnsupportedSelectionFailsBeforeGeneration()
    {
        Assert.Throws<InvalidDataException>(() => FormationDeployment.CropFor(Inputs, new("uk-sherman-01", "A/X-6")));
        Assert.Throws<InvalidDataException>(() => FormationDeployment.CropFor(Inputs, new("uk-rifle-01", "A/unknown")));
        Assert.NotEqual(FormationDeployment.JobKey(Seed, new("uk-rifle-01", "A/X-6")),
            FormationDeployment.JobKey(Seed, new("uk-rifle-01", "A/W-6")));
        Assert.Equal(Seed, FormationDeployment.JobKey(Seed, null));
    }

    [Fact]
    public void NeighboringFormationCropsShareExactInteriorFacts()
    {
        var aCrop = FormationDeployment.CropFor(Inputs, new("uk-rifle-01", "A/X-6"));
        var bCrop = FormationDeployment.CropFor(Inputs, new("uk-rifle-02", "A/W-6"));
        var a = new PilotGenerator(Inputs).Generate(Seed, aCrop);
        var b = new PilotGenerator(Inputs).Generate(Seed, bCrop);
        var count = 0;
        foreach (var f in a.Facts.Hexes)
        {
            var x = aCrop.Column + f.Index.Column - bCrop.Column;
            var y = aCrop.Row + f.Index.Row - bCrop.Row;
            if (f.Index.Column < 2 || f.Index.Column >= aCrop.Width - 2 || f.Index.Row < 2 || f.Index.Row >= aCrop.Height - 2 ||
                x < 2 || x >= bCrop.Width - 2 || y < 2 || y >= bCrop.Height - 2) continue;
            var other = b.Facts[new LimboDancer.Domains.Asl.Maps.Geometry.HexIndex(x, y)];
            Assert.Equal(JsonSerializer.Serialize(new { f.BaseLevel, f.Stairway, f.Center, f.Locations, f.Hexsides, f.Bridge }),
                JsonSerializer.Serialize(new { other.BaseLevel, other.Stairway, other.Center, other.Locations, other.Hexsides, other.Bridge }));
            count++;
        }
        Assert.True(count > 25);
    }
}