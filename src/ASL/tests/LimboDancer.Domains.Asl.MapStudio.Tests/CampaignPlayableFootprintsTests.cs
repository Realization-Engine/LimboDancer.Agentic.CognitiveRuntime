using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class CampaignPlayableFootprintsTests
{
    [Fact]
    public void RequiresExplicitPublicationAndRetainsImmutableAreaAcrossRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), "asl-footprints-" + Guid.NewGuid().ToString("N"));
        var board = BoardRef.Parse("ab-footprint-test");
        var geometry = BoardGeometry.Standard(4, 3);
        const string version = "accepted-terrain";
        const string json = """
            {"Version":"goodwood-map-ownership-1","Board":"ab-footprint-test","BoardVersion":"accepted-terrain","Parents":{"A/A-1":["B2"]}}
            """;
        try
        {
            var store = new CampaignPlayableFootprints(root);
            Assert.Null(store.Read(board, version, geometry));
            _ = CampaignMapOwnership.Read(json, board, version, geometry);
            Assert.Null(store.Read(board, version, geometry));
            store.Publish(board, version, geometry, json);
            var accepted = Assert.IsAssignableFrom<IReadOnlySet<HexName>>(store.Read(board, version, geometry));
            Assert.Single(accepted);
            Assert.Contains(HexName.Parse("B2"), accepted);
            Assert.DoesNotContain(HexName.Parse("A1"), accepted);
            Assert.Same(accepted, store.Read(board, version, geometry));
            var restarted = new CampaignPlayableFootprints(root);
            Assert.True(accepted.SetEquals(restarted.Read(board, version, geometry)!));
            restarted.Publish(board, version, geometry, json);
            Assert.Throws<InvalidDataException>(() => restarted.Publish(board, version, geometry, json.Replace("B2", "A1", StringComparison.Ordinal)));
            Assert.True(accepted.SetEquals(new CampaignPlayableFootprints(root).Read(board, version, geometry)!));
            Assert.Null(restarted.Read(board, "another-version", geometry));
            Assert.Empty(Directory.GetFiles(root, "*.stage-*", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
