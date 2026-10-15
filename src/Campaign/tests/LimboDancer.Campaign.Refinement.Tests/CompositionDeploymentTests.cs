using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class CompositionDeploymentTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
    private const string Seed = "00112233445566778899aabbccddeeff";

    [Theory]
    [InlineData("uk-engineer-01", "B/M-6", "Allied", "engineer", 4)]
    [InlineData("de-81mm-01", "C/Y-4", "German", "mortar", 12)]
    public void TypedRosterDeploysCompletesAndReloads(string id, string anchor, string side, string kind, int count)
    {
        var selection = new FormationSelection(id, anchor);
        var crop = FormationDeployment.CropFor(Inputs, selection);
        var result = new PilotGenerator(Inputs).Generate(Seed, crop);
        var deployment = new PilotDeployment(Inputs, result, Seed, selection);
        var context = new DeploymentContext("composition-test", "snapshot", side == "Allied" ? [anchor] : [],
            side == "German" ? [anchor] : [], selection);
        Assert.Equal(count, deployment.Units.Count);
        Assert.All(deployment.Units, unit => { Assert.True(unit.Kind == kind || kind == "mortar" && unit.Kind == "crew"); Assert.Equal(id, unit.ParentId); });
        Assert.NotEqual(FormationDeployment.Policy, deployment.ActivePolicy);
        var initial = deployment.Create(context);
        Assert.Throws<InvalidDataException>(() => deployment.SetComplete(context, initial, true));
        var complete = deployment.SetComplete(context, deployment.AutoDeploy(context, initial), true);
        Assert.Equal(count, complete.Placements.Length);
        Assert.All(complete.Placements, p => Assert.Equal(anchor, deployment.Cells.Single(c => c.Hex == p.Hex).Parent));
        var reload = new PilotDeployment(Inputs, deployment.Bundle, selection: selection);
        reload.Validate(context, complete);
        Assert.Equal(deployment.Units.ToArray(), reload.Units.ToArray());
        Assert.Throws<InvalidDataException>(() => reload.Validate(context, complete with { Policy = FormationDeployment.Policy }));
        Assert.Throws<InvalidDataException>(() => reload.Validate(context, complete with
        {
            Placements = [.. complete.Placements.Skip(1), new("unknown", complete.Placements[0].Hex)]
        }));
        if (kind == "mortar")
        {
            Assert.All(complete.Placements, p => Assert.True(deployment.Cells.Single(c => c.Hex == p.Hex).Terrain is "Open Ground" or "Paved Road"));
            var blocked = deployment.Cells.First(c => c.Admitted && c.Terrain is not ("Open Ground" or "Paved Road"));
            Assert.Contains("Mortar detachments", deployment.Rejection(context, initial, deployment.Units[0].Id, blocked.Hex));
        }
    }

    [Fact]
    public void RiflePolicyAndIdentifiersRemainStable()
    {
        var units = FormationDeployment.Units(Inputs, new("uk-rifle-02", "B/N-8"));
        Assert.Equal(FormationDeployment.Policy, FormationDeployment.PolicyFor(units));
        Assert.Equal("uk-rifle-02/squad-1", units[0].Id);
        Assert.All(units, u => Assert.Equal("rifle", u.Kind));
        Assert.Throws<InvalidDataException>(() => FormationDeployment.Units(Inputs, new("not-a-source-counter", "C/Y-4")));
    }
    [Theory]
    [InlineData("uk-rifle-01", "B/M-6", "Allied", "british-squad")]
    [InlineData("de-rifle-01", "C/Y-4", "German", "attacker-squad")]
    [InlineData("uk-engineer-01", "B/M-6", "Allied", "british-elite-squad")]
    public void InfantryCatalogMetadataPreservesExistingSetup(string id, string anchor, string side, string definition)
    {
        var selection = new FormationSelection(id, anchor);
        var deployment = new PilotDeployment(Inputs,
            new PilotGenerator(Inputs).Generate(Seed, FormationDeployment.CropFor(Inputs, selection)), Seed, selection);
        var context = new DeploymentContext("infantry-catalog", "snapshot", side == "Allied" ? [anchor] : [],
            side == "German" ? [anchor] : [], selection);
        Assert.All(deployment.Units, u =>
        {
            Assert.Equal(definition, u.Definition!.Definition);
            Assert.Contains("provisional", u.CatalogStatus);
            Assert.Contains("0 separate equipment", u.AllotmentStatus);
            Assert.Contains("unresolved", u.AllotmentStatus);
            Assert.Null(u.Holding);
        });
        Assert.Empty(deployment.Equipment);
        var complete = deployment.SetComplete(context, deployment.AutoDeploy(context, deployment.Create(context)), true);
        var legacy = complete with { UnitCatalog = null, Equipment = null };
        var reloaded = new PilotDeployment(Inputs, deployment.Bundle, selection: selection);
        var upgraded = reloaded.Rebind(context, context, legacy);
        Assert.Equal(legacy.Policy, upgraded.Policy);
        Assert.Equal(legacy.Revision, upgraded.Revision);
        Assert.Equal(legacy.ContextHash, upgraded.ContextHash);
        Assert.Equal(legacy.Placements, upgraded.Placements);
        Assert.True(upgraded.Complete);
        Assert.Equal(PilotCatalog.Identity, upgraded.UnitCatalog);
        Assert.Equal(deployment.Bundle, reloaded.Bundle);
        Assert.Equal(upgraded, reloaded.Rebind(context, context, upgraded));
        Assert.Throws<InvalidDataException>(() => reloaded.Validate(context, upgraded with { UnitCatalog = "unknown@1" }));
    }

    [Fact]
    public void LegacyPilotUsesSameReviewedRifleMappings()
    {
        Assert.All(PilotDeployment.Roster, u => Assert.Equal(u.Side == "Allied" ? "british-squad" : "attacker-squad", u.Definition!.Definition));
        Assert.Throws<InvalidDataException>(() => PilotCatalog.Infantry("x", "x", "German", "x", "engineer"));
    }
}
