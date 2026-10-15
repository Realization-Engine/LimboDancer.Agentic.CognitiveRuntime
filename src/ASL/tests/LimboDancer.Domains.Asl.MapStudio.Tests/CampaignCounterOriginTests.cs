using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class CampaignCounterOriginTests
{
    private const string Context = """
        {"Version":"campaign-goodwood-handoff-1","Setup":{"Formations":[{"UnitId":"de-mortar-01","Anchor":"A/D-5"}]},
        "Roster":{"Ground":[{"Id":"de-mortar-01","Units":[
          {"Id":"de-mortar-01/crew-1","ParentId":"de-mortar-01","Definition":{"Catalog":{"Catalog":"asl","Version":"1.17.0"},"Definition":"crew"},"CatalogStatus":"Reviewed crew"},
          {"Id":"de-mortar-01/mortar-1","ParentId":"de-mortar-01","Definition":{"Catalog":{"Catalog":"asl","Version":"1.17.0"},"Definition":"mortar"},"CatalogStatus":"Manufactured profile"}]}]}}
        """;

    [Fact]
    public void ReadsStableCrewAndWeaponOriginsWithoutChangingIdsOrAnchors()
    {
        var origins = CampaignCounterOrigin.Read(Context);
        Assert.Equal(2, origins.Count);
        Assert.Equal("A/D-5", origins["de-mortar-01--crew-1"].FormationHex);
        Assert.Equal("de-mortar-01", origins["de-mortar-01--mortar-1"].Formation);
        Assert.Equal("asl@1.17.0 / mortar", origins["de-mortar-01--mortar-1"].Definition);
        Assert.Equal("Manufactured profile", origins["de-mortar-01--mortar-1"].CatalogStatus);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("broken")]
    [InlineData("{}")]
    [InlineData("{\"Version\":\"future\"}")]
    public void MissingOrUnsupportedContextHasNoInventedOrigins(string? context)
    {
        Assert.Empty(CampaignCounterOrigin.Read(context));
    }

    [Fact]
    public void RejectsInconsistentParentOrDuplicateCounterIdentity()
    {
        Assert.Empty(CampaignCounterOrigin.Read(Context.Replace("\"ParentId\":\"de-mortar-01\"", "\"ParentId\":\"other\"", StringComparison.Ordinal)));
        Assert.Empty(CampaignCounterOrigin.Read(Context.Replace("de-mortar-01/mortar-1", "de-mortar-01/crew-1", StringComparison.Ordinal)));
    }
}
