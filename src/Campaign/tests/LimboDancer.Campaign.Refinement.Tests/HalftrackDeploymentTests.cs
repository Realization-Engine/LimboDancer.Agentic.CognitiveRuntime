using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class HalftrackDeploymentTests
{
    [Fact]
    public void AllSourceHalftracksRetainSeparateIdentitiesAndIntegratedEquipment()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        var parents = inputs.FormationTypes.Where(p => p.Value == "de-halftrack").Select(p => p.Key).ToArray();
        Assert.Equal(4, parents.Length);
        var units = parents.SelectMany(id => FormationDeployment.Units(inputs, new(id, "C/Y-4"))).ToArray();
        Assert.Equal(20, units.Length);
        Assert.Equal(20, units.Select(u => u.Id).Distinct().Count());
        Assert.All(units, u =>
        {
            Assert.Equal("attacker-halftrack", u.Definition!.Definition);
            Assert.Equal("vehicle", u.Kind);
            Assert.Null(u.Holding);
            Assert.Contains("No extra crew", u.AllotmentStatus);
            Assert.StartsWith(u.ParentId + "/vehicle-", u.Id);
        });
    }
}
