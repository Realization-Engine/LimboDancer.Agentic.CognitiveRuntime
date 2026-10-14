using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class GunDeploymentTests
{
    [Fact]
    public void EveryGunHasOneDistinctSameSideCrewAndNoFreeExtraWeapons()
    {
        var inputs = new PilotInputs(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));
        var types = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["uk-25pdr"] = 6, ["de-75mm"] = 4, ["de-88mm"] = 4,
            ["de-20mm"] = 6, ["de-quad20"] = 3, ["de-nebel"] = 6
        };
        var total = 0;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parent in inputs.FormationTypes.Where(p => types.ContainsKey(p.Value)))
        {
            var units = FormationDeployment.Units(inputs, new(parent.Key, "C/Y-4"));
            Assert.Equal("goodwood-selected-gun-1", FormationDeployment.PolicyFor(units));
            Assert.Equal(types[parent.Value] * 2, units.Count);
            foreach (var gun in units.Where(u => u.Kind == "gun"))
            {
                Assert.True(ids.Add(gun.Id));
                var crew = Assert.Single(units, u => u.Id == gun.Holding!.Holder);
                Assert.True(ids.Add(crew.Id));
                Assert.Equal(HoldingRole.Manned, gun.Holding!.Role);
                Assert.Equal("crew", crew.Kind);
                Assert.Equal(gun.Side, crew.Side);
                Assert.Equal(parent.Key, gun.ParentId);
                Assert.Equal(parent.Key, crew.ParentId);
                Assert.Contains("Manufactured MFG", gun.CatalogStatus);
                total++;
            }
        }
        Assert.Equal(82, total);
        Assert.Equal(164, ids.Count);
    }
}
