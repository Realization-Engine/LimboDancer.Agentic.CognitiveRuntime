using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class CampaignContextSummaryTests
{
    [Fact]
    public void ReadsProvenanceWithoutExposingPositionsOrFollowingSavedUrls()
    {
        var summary = CampaignContextSummary.Read("""
            {"Version":"campaign-goodwood-handoff-1","Roster":{"Ground":[{"Units":[{},{}]},{"Units":[{}]}],
            "AirSupport":{"Aircraft":[{}]}},"CampaignContext":"https://example.com/untrusted"}
            """);
        Assert.Equal(new CampaignContextSummary(2, 3, 1), summary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"Version\":\"future\"}")]
    [InlineData("{\"Version\":\"campaign-goodwood-handoff-1\",\"Roster\":null}")]
    public void UnknownOrDamagedContextDoesNotBreakPlay(string? json) => Assert.Null(CampaignContextSummary.Read(json));

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://user@localhost:8093")]
    [InlineData("http://localhost:8093/wrong")]
    [InlineData("http://localhost:8093/?redirect=other")]
    public void RejectsNonLocalOrAmbiguousOrigins(string origin) => Assert.Null(CampaignContextSummary.ReturnUrl(origin));

    [Fact]
    public void ReturnsToConfiguredLocalCampaignHost() => Assert.Equal(
        "http://127.0.0.1:8080/index.html?exercise=source&source=5",
        CampaignContextSummary.ReturnUrl("http://127.0.0.1:8080"));
}
