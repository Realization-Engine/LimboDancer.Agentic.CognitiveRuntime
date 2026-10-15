using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class EquipmentDeploymentTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private static readonly FormationSelection Selection = new("de-81mm-01", "C/Y-4");
    private static readonly DeploymentContext Context = new("equipment-test", "snapshot", [], [Selection.Anchor], Selection);
    private static readonly PilotDeployment Deployment = CreateDeployment();
    private static PilotDeployment CreateDeployment()
    {
        const string seed = "00112233445566778899aabbccddeeff";
        return new(Inputs, new PilotGenerator(Inputs).Generate(seed, FormationDeployment.CropFor(Inputs, Selection)), seed, Selection);
    }

    [Fact]
    public void ExplicitAllotmentUsesAslHoldingAndLabeledGameDefault()
    {
        Assert.Equal(6, Deployment.Equipment.Length);
        Assert.All(Deployment.Equipment, link =>
        {
            Assert.Equal(HoldingRole.Manned, link.Holding.Role);
            var crew = Deployment.Units.Single(u => u.Id == link.Holding.Holder);
            var mortar = Deployment.Units.Single(u => u.Id == link.EquipmentId);
            Assert.Equal("attacker-crew", crew.Definition!.Definition);
            Assert.Contains("provisional", crew.CatalogStatus);
            Assert.Equal("campaign-german-81mm-mortar", mortar.Definition!.Definition);
            Assert.Contains("Game default (MFG)", mortar.CatalogStatus);
        });
    }

    [Fact]
    public void OlderCatalogSetupKeepsItsPositionsAndLinks()
    {
        var current = Deployment.AutoDeploy(Context, Deployment.Create(Context)) with { UnitCatalog = "asl-scenario-a1@1.14.0" };
        var saved = JsonSerializer.Serialize(current);
        var restored = JsonSerializer.Deserialize<PilotSetup>(saved)!;
        Deployment.Validate(Context, restored);
        Assert.Equal(saved, JsonSerializer.Serialize(Deployment.Rebind(Context, Context, restored)));
    }

    [Fact]
    public void MovingEitherIdentityMovesPairAndStackingCountsCrews()
    {
        var state = Deployment.Create(Context);
        Assert.NotNull(state.UnitCatalog);
        Assert.Throws<InvalidDataException>(() => Deployment.Validate(Context, state with { UnitCatalog = "unknown@1" }));
        var hex = Deployment.Cells.First(c => c.Parent == Selection.Anchor && c.Admitted && c.Terrain == "Open Ground").Hex;
        state = Deployment.Move(Context, state, "de-81mm-01/mortar-1", hex);
        Assert.Equal(2, state.Placements.Length);
        Assert.All(state.Placements, p => Assert.Equal(hex, p.Hex));
        state = Deployment.Move(Context, state, "de-81mm-01/crew-2", hex);
        Assert.Equal(4, state.Placements.Length);
        Deployment.Validate(Context, state);
        Assert.Contains("Stacking limit", Deployment.Rejection(Context, state, "de-81mm-01/crew-3", hex));
        Assert.Throws<InvalidDataException>(() => Deployment.Validate(Context, state with { Equipment = [] }));
        Assert.Throws<InvalidDataException>(() => Deployment.Validate(Context, state with { Placements = state.Placements.Skip(1).ToArray() }));
        var badLinks = state.Equipment!.ToArray();
        badLinks[0] = badLinks[0] with { Holding = new("de-81mm-01/crew-2", HoldingRole.Manned) };
        Assert.Throws<InvalidDataException>(() => Deployment.Validate(Context, state with { Equipment = badLinks }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyMigrationPreservesHexesCompletionAndAcceptedTerrain(bool complete)
    {
        var current = Deployment.AutoDeploy(Context, Deployment.Create(Context));
        const string policy = "goodwood-selected-mortar-1";
        var hash = BoardPackage.Hash(CanonicalJson.Serialize(new object[] { policy, "selected-anchor-binding-2",
            Context.CampaignId, Selection.UnitId, Selection.Anchor, "German" }));
        var oldPlacements = current.Placements.Where(p => p.UnitId.Contains("/crew-", StringComparison.Ordinal))
            .Select(p => p with { UnitId = p.UnitId.Replace("/crew-", "/detachment-", StringComparison.Ordinal) }).ToArray();
        if (!complete) oldPlacements = oldPlacements.Take(2).ToArray();
        var legacy = new PilotSetup(policy, hash, current.PackageVersion, 12, complete, oldPlacements);
        // Exercise missing optional properties as they appear in an existing saved campaign.
        legacy = JsonSerializer.Deserialize<PilotSetup>(JsonSerializer.Serialize(legacy))!;
        var migrated = Deployment.Rebind(Context, Context, legacy);
        Assert.Equal(complete, migrated.Complete);
        Assert.Equal(13, migrated.Revision);
        Assert.Equal(oldPlacements.Length * 2, migrated.Placements.Length);
        foreach (var old in oldPlacements)
            Assert.Equal(old.Hex, migrated.Placements.Single(p => p.UnitId == old.UnitId.Replace("/detachment-", "/mortar-", StringComparison.Ordinal)).Hex);
        var reloaded = new PilotDeployment(Inputs, Deployment.Bundle, selection: Selection);
        reloaded.Validate(Context, JsonSerializer.Deserialize<PilotSetup>(JsonSerializer.Serialize(migrated))!);
        Assert.Equal(Deployment.Bundle, reloaded.Bundle);
        Assert.Equal(migrated, reloaded.Rebind(Context, Context, migrated));
        Assert.Throws<InvalidDataException>(() => Deployment.Rebind(Context, Context, legacy with { ContextHash = "wrong" }));
        Assert.Throws<InvalidDataException>(() => Deployment.Rebind(Context, Context, legacy with { Placements = [oldPlacements[0], oldPlacements[0]] }));
    }
}
