using LimboDancer.Campaign.Refinement;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class VehicleDeploymentTests
{
    [Fact]
    public void EntireGroundRosterHasStableUniqueIdentitiesAndExactVehicleMixes()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        var parents = inputs.FormationTypes.Where(p => p.Value != "uk-typhoon-rockets").ToArray();
        Assert.Equal(114, parents.Length);
        var units = parents.SelectMany(p => FormationDeployment.Units(inputs, new(p.Key, "A/X-6"))).ToArray();
        Assert.Equal(624, units.Length);
        Assert.Equal(624, units.Select(u => u.Id).Distinct().Count());
        Assert.Equal(320, units.Count(u => u.Kind == "vehicle"));
        Assert.Equal(94, units.Count(u => u.Kind == "crew"));
        Assert.Equal(94, units.Count(u => u.Holding is not null));
        Assert.All(units, u => Assert.NotNull(u.Definition));
        foreach (var parent in parents.Where(p => p.Value == "uk-sherman"))
        {
            var members = units.Where(u => u.ParentId == parent.Key).ToArray();
            Assert.Equal(3, members.Count(u => u.Definition!.Definition == "campaign-british-sherman75"));
            Assert.Single(members, u => u.Definition!.Definition == "campaign-british-firefly" && u.Id == parent.Key + "/vehicle-4");
        }
        foreach (var parent in parents.Where(p => p.Value is "de-pziv-w" or "de-pziv" or "de-panther-w" or "de-panther"))
            Assert.Equal(parent.Value.EndsWith("-w", StringComparison.Ordinal) ? 4 : 5, units.Count(u => u.ParentId == parent.Key));
        Assert.All(units.Where(u => u.Kind == "vehicle"), u => Assert.Null(u.Holding));
    }
}
