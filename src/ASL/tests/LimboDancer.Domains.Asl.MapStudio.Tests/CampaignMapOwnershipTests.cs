using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class CampaignMapOwnershipTests
{
    private static readonly BoardPackage Board = new(BoardRef.Parse("ab-ownership-test"), "Test",
        FeatureModel.New(BoardGeometry.Standard(4, 3), new string('a', 64)));

    private static string Document(string parents) =>
        "{\"Version\":\"goodwood-map-ownership-1\",\"Board\":\"" + Board.Board.Value +
        "\",\"BoardVersion\":\"" + Board.Version + "\",\"Parents\":" + parents + "}";

    [Fact]
    public void ReadsOnlyOwnedHexesWithoutAssigningExteriorGuards()
    {
        var original = Board.FeaturesBytes();
        var result = CampaignMapOwnership.Read(Document("{\"B/I-5\":[\"A1\",\"B1\"],\"B/I-6\":[\"C2\"]}"), Board);
        Assert.Equal(3, result.Count);
        Assert.Equal("B/I-5", result["B1"]);
        Assert.False(result.ContainsKey("D1"));
        Assert.Equal(original, Board.FeaturesBytes());
    }

    [Theory]
    [InlineData("{\"B/I-5\":[\"A1\",\"A1\"]}")]
    [InlineData("{\"B/I-5\":[\"A1\"],\"B/I-6\":[\"A1\"]}")]
    [InlineData("{\"B/I-5\":[\"A1\"],\"B/I-5\":[\"B1\"]}")]
    [InlineData("{\"B/I-5\":[\"Z99\"]}")]
    [InlineData("{\"B/I-5\":[null]}")]
    [InlineData("{\"B/I-5\":[]}")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void RejectsInvalidAndAmbiguousOwnership(string parents)
    {
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read(Document(parents), Board));
    }

    [Fact]
    public void RejectsMismatchedBoardVersionMalformedAndOversizedDocuments()
    {
        var json = Document("{\"B/I-5\":[\"A1\"]}");
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read(json.Replace(Board.Version, "wrong", StringComparison.Ordinal), Board));
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read(json.Replace(Board.Board.Value, "ab-other", StringComparison.Ordinal), Board));
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read(json.Replace("ownership-1", "ownership-2", StringComparison.Ordinal), Board));
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read("broken", Board));
        Assert.Throws<InvalidDataException>(() => CampaignMapOwnership.Read(new string(' ', 2_000_001), Board));
    }
}
