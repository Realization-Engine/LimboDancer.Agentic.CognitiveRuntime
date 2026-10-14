using LimboDancer.Domains.Asl.Maps.Features;
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
        reload.Validate(context with { FormationSignature = "unrelated formation moved" }, setup);
        var changed = context with { FormationSignature = "unrelated formation moved", AlliedParents = ["A/X-6", "A/V-7"], GermanParents = ["A/X-6", "A/V-7"] };
        var rebound = reload.Rebind(context, changed, setup);
        Assert.Equal(setup, rebound);
        Assert.Throws<InvalidDataException>(() => reload.Rebind(context, context with { CampaignId = "other" }, setup));
        Assert.Throws<InvalidDataException>(() => reload.Rebind(context, context with { AlliedParents = [], GermanParents = [] }, setup));
        var legacyBinding = new object[] { p.ActivePolicy, context.CampaignId, context.FormationSignature,
            context.AlliedParents, context.GermanParents, selection.UnitId, selection.Anchor };
        var legacy = setup with { ContextHash = LimboDancer.Domains.Asl.Maps.Features.BoardPackage.Hash(CanonicalJson.Serialize(legacyBinding)) };
        Assert.Equal(setup, reload.Rebind(context, changed, legacy));
        Assert.Throws<InvalidDataException>(() => reload.Rebind(context, changed, legacy with { ContextHash = "invalid" }));
        Assert.Throws<InvalidDataException>(() => reload.Validate(context with { Formation = selection with { UnitId = "uk-rifle-01" } }, setup));
        Assert.Throws<InvalidDataException>(() => reload.Create(context with { AlliedParents = [], GermanParents = [] }));
    }

    [Fact]
    public void UnsupportedSelectionFailsBeforeGeneration()
    {
        Assert.Throws<InvalidDataException>(() => FormationDeployment.CropFor(Inputs, new("uk-typhoon-rockets-01", "A/X-6")));
        Assert.Throws<InvalidDataException>(() => FormationDeployment.CropFor(Inputs, new("uk-rifle-01", "A/unknown")));
        Assert.NotEqual(FormationDeployment.JobKey(Seed, new("uk-rifle-01", "A/X-6")),
            FormationDeployment.JobKey(Seed, new("uk-rifle-01", "A/W-6")));
        Assert.Equal(Seed, FormationDeployment.JobKey(Seed, null));
    }

    [Theory]
    [InlineData("A/A-1")]
    [InlineData("C/GG-10")]
    public void UncoveredMapEdgesAreRejected(string anchor)
    {
        var crop = FormationDeployment.CropFor(Inputs, new("de-rifle-03", anchor));
        var error = Assert.Throws<InvalidDataException>(() => new PilotGenerator(Inputs).Generate(Seed, crop));
        Assert.Contains("outside covered source polygons", error.Message);
    }

    [Theory]
    [InlineData("C/X-4", "C/V-3")]
    [InlineData("C/Y-5", "C/Z-7")]
    public void LegacyElevatedNeighborsFailExplicitly(string anchor, string offendingParent)
    {
        var crop = FormationDeployment.CropFor(Inputs, new("de-rifle-03", anchor));
        var error = Assert.Throws<InvalidDataException>(() => new PilotGenerator(Inputs.ForVersion(PilotInputs.LegacyGeneratorVersion)).Generate(Seed, crop));
        Assert.Contains("Unsupported source terrain/elevation", error.Message);
        Assert.Contains(offendingParent, error.Message);
    }

    [Fact]
    public void NewSeedChangesBuildingDetailButPreservesSourceGeometry()
    {
        var crop = FormationDeployment.CropFor(Inputs, new("de-rifle-03", "C/Y-4"));
        var a = new PilotGenerator(Inputs).Generate(Seed, crop);
        var b = new PilotGenerator(Inputs).Generate("11111111111111111111111111111111", crop);
        // Feature IDs include world identity, so compare physical facts rather than IDs across seeds.
        static string Geometry(PilotResult result, bool generated) => JsonSerializer.Serialize(
            result.MetricFeatures.Where(f => (f.Provenance == "generated") == generated)
                .OrderBy(f => f.SourceKey, StringComparer.Ordinal)
                .Select(f => new { f.SourceKey, f.Kind, f.Code, f.WidthMm, f.Points }));
        Assert.Equal(Geometry(a, false), Geometry(b, false));
        Assert.NotEqual(Geometry(a, true), Geometry(b, true));
        Assert.Contains(a.MetricFeatures, f => f.Provenance == "generated");
        Assert.Contains(b.MetricFeatures, f => f.Provenance == "generated");
    }

    public static IEnumerable<object[]> NeighborCases()
    {
        foreach (var seed in new[] { Seed, "11111111111111111111111111111111", "ffffffffffffffffffffffffffffffff" })
            foreach (var pair in new[] { ("A/X-6", "A/W-6"), ("A/X-6", "A/X-7"), ("A/V-7", "A/W-7"), ("A/W-6", "A/W-7"), ("C/Y-4", "C/X-4"), ("C/Y-4", "C/Y-5") })
                yield return new object[] { seed, pair.Item1, pair.Item2 };
    }

    [Theory]
    [MemberData(nameof(NeighborCases))]
    public void NeighboringFormationCropsShareExactInteriorFacts(string seed, string first, string second)
    {
        var aCrop = FormationDeployment.CropFor(Inputs, new("uk-rifle-01", first));
        var bCrop = FormationDeployment.CropFor(Inputs, new("uk-rifle-02", second));
        var a = new PilotGenerator(Inputs).Generate(seed, aCrop);
        var b = new PilotGenerator(Inputs).Generate(seed, bCrop);
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
        Assert.True(count > 25, $"Too few shared interior cells: {first} / {second}: {count}");
        var shared = a.MetricFeatures.Join(b.MetricFeatures, f => f.Id, f => f.Id, (left, right) => (left, right)).ToArray();
        Assert.NotEmpty(shared);
        foreach (var (left, right) in shared)
            Assert.Equal(JsonSerializer.Serialize(left), JsonSerializer.Serialize(right));
        var replay = new PilotGenerator(Inputs).Generate(seed, aCrop);
        Assert.Equal(a.Package.Version, replay.Package.Version);
        Assert.Equal(a.Manifest, replay.Manifest);
        Assert.Equal(PilotGenerator.GridHash(a.Grid), PilotGenerator.GridHash(replay.Grid));

        var selection = new FormationSelection("de-rifle-03", first);
        var deployment = new PilotDeployment(Inputs, a, seed, selection);
        var context = new DeploymentContext("boundary-test", "snapshot", [], [first], selection);
        var setup = deployment.AutoDeploy(context, deployment.Create(context));
        foreach (var cell in deployment.Cells.Where(c => c.Reason.StartsWith("Crop boundary", StringComparison.Ordinal)))
        {
            Assert.False(cell.Admitted);
            Assert.NotNull(deployment.Rejection(context, setup, deployment.Units[0].Id, cell.Hex));
        }
        Assert.All(setup.Placements, p => Assert.Equal(first, deployment.Cells.Single(c => c.Hex == p.Hex).Parent));
    }
}