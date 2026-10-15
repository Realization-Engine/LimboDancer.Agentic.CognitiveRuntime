using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class SituationTerrainTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Fact]
    public void MarshIsPreservedAndExteriorMarginsStayUnowned()
    {
        var inputs = Inputs.ForVersion(PilotInputs.SituationGeneratorVersion);
        var marsh = inputs.Regions.Where(r => r.Kind == "marsh").Select(r => r.Key).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(5, marsh.Count);
        var tiles = SituationCoverage.Plan(inputs);
        var selected = tiles.Where(t => t.Cells.Any(c => marsh.Contains(c.Parent))).Append(tiles[0]).DistinctBy(t => t.Key).ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var exterior = false;
        foreach (var tile in selected)
        {
            var result = new PilotGenerator(inputs).Generate(Seed, tile.Crop);
            Assert.False(result.Validation.HasErrors);
            foreach (var f in result.MetricFeatures.Where(f => marsh.Contains(f.SourceKey)))
            {
                Assert.Equal((byte)14, f.Code);
                Assert.Equal("source-preserved", f.Provenance);
                seen.Add(f.SourceKey);
            }
            using var manifest = JsonDocument.Parse(result.Manifest);
            var hexes = manifest.RootElement.GetProperty("Hexes").EnumerateArray().ToArray();
            exterior |= hexes.Any(h => h.GetProperty("Parent").ValueKind == JsonValueKind.Null);
            foreach (var cell in tile.Cells)
                Assert.Contains(hexes, h => h.GetProperty("Column").GetInt32() == cell.Column &&
                    h.GetProperty("Row").GetInt32() == cell.Row && h.GetProperty("Parent").GetString() == cell.Parent);
        }
        Assert.Equal(marsh.Order(), seen.Order());
        Assert.True(exterior);
    }

    [Fact]
    public void SituationVersionDoesNotChangeEitherPilotProfile()
    {
        var situation = Inputs.ForVersion(PilotInputs.SituationGeneratorVersion);
        Assert.NotEqual(Inputs.ProfileDigest, situation.ProfileDigest);
        Assert.NotEqual(PilotIdentity.World(Inputs, Seed), PilotIdentity.World(situation, Seed));
        var restored = situation.ForVersion(PilotInputs.GeneratorVersion);
        Assert.Equal(Inputs.ProfileDigest, restored.ProfileDigest);
        Assert.Equal(PilotIdentity.World(Inputs, Seed), PilotIdentity.World(restored, Seed));
        var legacy = situation.ForVersion(PilotInputs.LegacyGeneratorVersion);
        Assert.Equal(Inputs.ProfileDigest, legacy.ProfileDigest);
        Assert.False(legacy.SupportsElevation);
        Assert.False(restored.SupportsSituation);
    }
}
