using System.Text.Json;
using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class DeploymentTests
{
    private const string Seed = "00112233445566778899aabbccddeeff";
    private static readonly string Source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json"));
    private static readonly PilotInputs Inputs = new(Source);
    private static readonly Lazy<PilotDeployment> Pilot = new(() => new(Inputs, new PilotGenerator(Inputs).Generate(Seed), Seed));
    public static DeploymentContext SourceContext()
    {
        using var doc = JsonDocument.Parse(Source);
        var allowed = doc.RootElement.GetProperty("deployment").GetProperty("allowedHexes");
        string[] Refs(string id) => allowed.GetProperty(id).EnumerateArray()
            .Select(r => r.GetProperty("boardId").GetString() + "/" + r.GetProperty("hexId").GetString()).ToArray();
        return new("campaign-test", "formation-fixture", Refs("uk-rifle-01"), Refs("de-rifle-01"));
    }
    [Fact]
    public void PilotDerivedFactsRetainPreOptimizationIdentity()
    {
        var legacy = Inputs.ForVersion(PilotInputs.LegacyGeneratorVersion);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new PilotGenerator(legacy).Generate(Seed).Facts.Hexes);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
        // Captured before the bridge/tunnel bounding-box rejection optimization.
        Assert.Equal("FF79F7F19DCD2E558C64218A09EC0536E93EC9E3881DA2EE57665CE5B45EE8E7", hash);
    }
    [Fact]
    public void SourceRulesHaveCapacityAndCompletionLocksPlacement()
    {
        var p = Pilot.Value; var context = SourceContext();
        var empty = p.Create(context); Assert.Empty(empty.Placements);
        var setup = p.AutoDeploy(context, empty); Assert.Equal(8, setup.Placements.Length); p.Validate(context, setup);
        Assert.All(setup.Placements, u => Assert.True(p.Cells.Single(c => c.Hex == u.Hex).Admitted));
        setup = p.SetComplete(context, setup, true);
        Assert.Throws<InvalidDataException>(() => p.Move(context, setup, setup.Placements[0].UnitId, setup.Placements[0].Hex));
        Assert.False(p.SetComplete(context, setup, false).Complete);
        Assert.Throws<InvalidDataException>(() => p.SetComplete(context, empty, true));
    }
    [Fact]
    public void BundleRoundTripLoadsAcceptedFeaturesAndPlacements()
    {
        var p = Pilot.Value; var context = SourceContext(); var setup = p.AutoDeploy(context, p.Create(context));
        var bundle = JsonSerializer.Deserialize<PilotBundle>(JsonSerializer.Serialize(p.Bundle))!;
        var loaded = new PilotDeployment(Inputs, bundle); loaded.Validate(context, setup);
        Assert.Equal(p.Package.FeaturesBytes(), loaded.Package.FeaturesBytes());
        Assert.Equal(p.Bundle.GridSha256, loaded.Bundle.GridSha256);
        Assert.Equal(JsonSerializer.Serialize(p.Cells), JsonSerializer.Serialize(loaded.Cells));
    }
    [Fact]
    public void CorruptedOrIncompatiblePackageIsRejected()
    {
        var bundle = Pilot.Value.Bundle;
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, bundle with { PackageVersion = new string('0', 64) }));
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, bundle with { Seed = new string('0', 32) }));
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, bundle with { GridSha256 = new string('0', 64) }));
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, bundle with { CatalogSha256 = "wrong" }));
        Assert.Throws<InvalidDataException>(() => new PilotDeployment(Inputs, bundle with {
            Seed = new string('0', 32), Manifest = bundle.Manifest.Replace(Seed, new string('0', 32), StringComparison.Ordinal) }));
    }
    [Fact]
    public void BoundaryWaterAndWrongSideHaveSpecificErrors()
    {
        var p = Pilot.Value; var c = SourceContext(); var setup = p.Create(c); var id = PilotDeployment.Roster[0].Id;
        var boundary = p.Cells.First(x => x.Reason.StartsWith("Crop boundary", StringComparison.Ordinal));
        Assert.Contains("boundary", p.Rejection(c, setup, id, boundary.Hex));
        var blocked = p.Cells.First(x => x.Reason.StartsWith("This terrain", StringComparison.Ordinal));
        Assert.Contains("not admitted", p.Rejection(c, setup, id, blocked.Hex));
        var enemy = p.Cells.First(x => x.Admitted && !c.AlliedParents.Contains(x.Parent));
        Assert.Contains("Allied setup", p.Rejection(c, setup, id, enemy.Hex));
        Assert.Empty(setup.Placements);
    }
    [Fact]
    public void StackingEnemyOverlapAndDuplicateRosterAreRejected()
    {
        var p = Pilot.Value; var all = Inputs.Regions.Select(r => r.Key).ToArray();
        var c = new DeploymentContext("fixture", "shared-supply-exception", all, all); var setup = p.Create(c);
        var hex = p.Cells.First(x => x.Admitted).Hex;
        setup = p.Move(c, setup, PilotDeployment.Roster[0].Id, hex);
        Assert.Contains("Opposing", p.Rejection(c, setup, PilotDeployment.Roster[4].Id, hex));
        setup = p.Move(c, setup, PilotDeployment.Roster[1].Id, hex);
        Assert.Contains("Stacking", p.Rejection(c, setup, PilotDeployment.Roster[2].Id, hex));
        Assert.Throws<InvalidDataException>(() => p.Validate(c, setup with { Placements = [setup.Placements[0], setup.Placements[0]] }));
    }
    [Fact]
    public void ChangedCampaignContextAndInsufficientCapacityCannotOverwriteSetup()
    {
        var p = Pilot.Value; var c = SourceContext(); var state = p.AutoDeploy(c, p.Create(c));
        Assert.Throws<InvalidDataException>(() => p.Validate(c with { CampaignId = "other-campaign" }, state));
        Assert.Throws<InvalidDataException>(() => p.Validate(c with { FormationSignature = "moved-formation" }, state));
        Assert.Throws<InvalidDataException>(() => p.Validate(c with { AlliedParents = [] }, state));
        Assert.Throws<InvalidDataException>(() => p.Create(c with { AlliedParents = [] }));
        Assert.Throws<InvalidDataException>(() => p.Create(c with { AlliedParents = ["A/nonexistent"] }));
        Assert.Equal(8, state.Placements.Length);
    }
    [Fact]
    public void ParentLookupMatchesOriginalExactOrdinalSearchForEveryPilotCell()
    {
        var pilot = Pilot.Value;
        foreach (var cell in pilot.Cells)
        {
            var key = cell.GlobalKey.Split('/');
            var world = SpatialFrame.Center(int.Parse(key[0], System.Globalization.CultureInfo.InvariantCulture),
                int.Parse(key[1], System.Globalization.CultureInfo.InvariantCulture));
            var original = Inputs.Regions.FirstOrDefault(r => MetricGeometry.Contains(r.Polygon, world))?.Key;
            Assert.Equal(original, cell.Parent);
        }
        var timings = new Dictionary<string, double>(StringComparer.Ordinal);
        var loaded = new PilotDeployment(Inputs, pilot.Bundle, timings);
        Assert.Equal(pilot.Bundle, loaded.Bundle);
        Assert.Equal(JsonSerializer.Serialize(pilot.Cells), JsonSerializer.Serialize(loaded.Cells));
        Assert.Equal(6, timings.Count);
        Assert.All(timings.Values, value => Assert.True(value >= 0));
    }}
