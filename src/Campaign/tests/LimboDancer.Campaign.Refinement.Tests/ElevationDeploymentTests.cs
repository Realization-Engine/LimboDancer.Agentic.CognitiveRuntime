using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class ElevationDeploymentTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Theory]
    [InlineData("uk-rifle-02", "B/N-8", "Allied", false)]
    [InlineData("uk-rifle-12", "B/H-8", "Allied", false)]
    [InlineData("de-rifle-03", "C/X-4", "German", false)]
    [InlineData("de-rifle-03", "C/Y-5", "German", false)]
    [InlineData("uk-rifle-02", "B/M-6", "Allied", true)]
    [InlineData("uk-rifle-12", "B/F-7", "Allied", true)]
    [InlineData("de-rifle-03", "C/V-3", "German", true)]
    [InlineData("de-rifle-03", "C/Z-7", "German", true)]
    public void HillsCompileRenderDeployAndReload(string unit, string anchor, string side, bool hillCentered)
    {
        var selection = new FormationSelection(unit, anchor);
        var crop = FormationDeployment.CropFor(Inputs, selection);
        var result = new PilotGenerator(Inputs).Generate(Seed, crop);
        Assert.Contains(result.Package.Model.Features, f => f is ElevationRegion);
        if (hillCentered) Assert.Contains(result.Facts.Hexes, f => f.BaseLevel > 0);
        Assert.All(result.Facts.Hexes, f =>
        {
            Assert.InRange(f.BaseLevel, -1, 2);
            if (f.BaseLevel < 0) Assert.NotNull(f.Center.DepressionTerrain);
            Assert.All(f.Hexsides, h => { Assert.False(h.Cliff); Assert.False(h.Slope); });
        });
        var deployment = new PilotDeployment(Inputs, result, Seed, selection);
        var context = new DeploymentContext("hill-test", "snapshot", side == "Allied" ? [anchor] : [],
            side == "German" ? [anchor] : [], selection);
        var setup = deployment.SetComplete(context, deployment.AutoDeploy(context, deployment.Create(context)), true);
        Assert.Equal(4, setup.Placements.Length);
        if (hillCentered) Assert.All(setup.Placements, p => Assert.True(deployment.Cells.Single(c => c.Hex == p.Hex).BaseLevel > 0));
        var reload = new PilotDeployment(Inputs, deployment.Bundle, selection: selection);
        reload.Validate(context, setup);
        Assert.Equal(JsonSerializer.Serialize(deployment.Cells), JsonSerializer.Serialize(reload.Cells));
        Assert.Equal(PilotGenerator.GridHash(result.Grid), PilotGenerator.GridHash(reload.Grid));
        var render = BoardRenderInput.Create(result.Package.Board, result.Package.Name, result.Grid, Inputs.Catalog, result.Facts,
            new StyledSource(result.Package.Model, result.Grid, result.Facts));
        if (hillCentered) Assert.Contains("id=\"elev1\"", BoardRenderer.Document(render, BoardView.Exact));
        Assert.All(deployment.Cells, c => Assert.Equal(result.Facts.Hexes.Single(f => f.Hex.ToString() == c.Hex).BaseLevel, c.BaseLevel));
    }

    [Fact]
    public void LegacyAcceptedTerrainUsesItsOriginalIdentity()
    {
        var legacy = Inputs.ForVersion(PilotInputs.LegacyGeneratorVersion);
        var selection = new FormationSelection("de-rifle-03", "C/Y-4");
        var crop = FormationDeployment.CropFor(legacy, selection);
        var old = new PilotDeployment(legacy, new PilotGenerator(legacy).Generate(Seed, crop), Seed, selection);
        var context = new DeploymentContext("legacy", "snapshot", [], [selection.Anchor], selection);
        var setup = old.AutoDeploy(context, old.Create(context));
        var loaded = new PilotDeployment(Inputs, old.Bundle, selection: selection);
        loaded.Validate(context, setup);
        Assert.Equal(old.Bundle, loaded.Bundle);
        Assert.Equal(JsonSerializer.Serialize(old.Cells), JsonSerializer.Serialize(loaded.Cells));
        Assert.NotEqual(PilotIdentity.World(legacy, Seed), PilotIdentity.World(Inputs, Seed));
        Assert.Throws<InvalidDataException>(() => Inputs.ForVersion("unknown"));
    }
}
