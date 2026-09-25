using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Scenario A1 terrain evidence read through the map read API (ASL-MAP-081; Occupied and Concealed Entry Design,
/// section 3), checked over the committed board 01 Hex Fact oracle fixture, so it runs without a VASL checkout.
/// </summary>
public sealed class BoardCatalogTerrainEvidenceTests
{
    [Fact]
    public void BothEvidenceSourcesAgreeOnEveryHexOfBoard01()
    {
        var pinned = new Board01TerrainCatalog();
        var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([Board01Fixture.Handle()]));
        var supported = 0;
        foreach (var hex in Board01Fixture.Hexes())
        {
            var binding = Binding(hex);
            Assert.Equal(pinned.IsSupportedGroundLevel(binding), read.IsSupportedGroundLevel(binding));
            supported += read.IsSupportedGroundLevel(binding) ? 1 : 0;
        }

        Assert.Equal(346, Board01Fixture.Hexes().Count);
        Assert.Equal(63, supported);
    }

    [Fact]
    public void BuildingHexesWithoutAnOverrideAreNotEvidence()
    {
        // Board 01 has building hexes whose type its metadata does not state; the packages were reviewed only for the
        // explicit overrides, so reading the board must not widen them.
        var handle = Board01Fixture.Handle();
        var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([handle]));
        var unlisted = Board01Fixture.Hexes()
            .Where(hex => handle.HexFacts(HexName.Parse(hex))!.Center.Terrain!.Name.Contains("Building", StringComparison.Ordinal))
            .Where(hex => !new Board01TerrainCatalog().TryGetBuilding(hex, out _))
            .ToArray();
        Assert.NotEmpty(unlisted);
        Assert.All(unlisted, hex => Assert.False(read.IsSupportedGroundLevel(Binding(hex))));
    }

    [Fact]
    public void BindingALiveLocationNeedsTheGroundLevelOfAnOverrideHex()
    {
        var handle = Board01Fixture.Handle();
        var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([handle]));
        Assert.Equal(Binding("E4"), read.Bind(handle, BoardLocation.Parse("bd01:E4:0")));
        Assert.Equal(Binding("F1"), read.Bind(handle, BoardLocation.Parse("bd01:F1:0")));
        Assert.Null(read.Bind(handle, BoardLocation.Parse("bd01:E4:1")));
        Assert.Null(read.Bind(handle, BoardLocation.Parse("bd01:A1:0")));
        Assert.Null(read.Bind(handle, BoardLocation.Parse("bd02:E4:0")));
    }

    [Fact]
    public void ABoardThatDisagreesWithAnOverrideIsNotEvidenceForThatHex()
    {
        var handle = Board01Fixture.Handle(changed: ("E4", "Wooden Building"));
        var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([handle]));
        Assert.False(read.IsSupportedGroundLevel(Binding("E4")));
        Assert.Null(read.Bind(handle, BoardLocation.Parse("bd01:E4:0")));
        Assert.True(read.IsSupportedGroundLevel(Binding("F1")));
    }

    [Fact]
    public void OnlyAVerifiedBoardReadFromThePinnedMetadataIsEvidence()
    {
        var source = Board01Fixture.Source();
        foreach (var handle in new[]
        {
            Board01Fixture.Handle(source: null),
            Board01Fixture.Handle(source: source with { MetadataBlob = "different" }),
            Board01Fixture.Handle(source: source with { MetadataVersion = "6.8" }),
            Board01Fixture.Handle(source: source with { BoardName = "02" }),
            Board01Fixture.Handle(status: BoardReadStatus.Ingested),
        })
        {
            var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([handle]));
            Assert.False(read.IsSupportedGroundLevel(Binding("E4")));
            Assert.Null(read.Bind(handle, BoardLocation.Parse("bd01:E4:0")));
        }

        Assert.False(new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([])).IsSupportedGroundLevel(Binding("E4")));
    }

    [Fact]
    public void TheCatalogBindingMustStillNameThePinnedMetadata()
    {
        var read = new BoardCatalogTerrainEvidence(new InMemoryBoardCatalog([Board01Fixture.Handle()]));
        var binding = Binding("E4");
        Assert.True(read.IsSupportedGroundLevel(binding));
        Assert.False(read.IsSupportedGroundLevel(binding with { BoardVersion = "6.8" }));
        Assert.False(read.IsSupportedGroundLevel(binding with { MetadataGitBlobSha = "different" }));
        Assert.False(read.IsSupportedGroundLevel(binding with { Level = 1 }));
        Assert.False(read.IsSupportedGroundLevel(binding with { Variant = "NoRoads" }));
        Assert.False(read.IsSupportedGroundLevel(null));
    }

    [Fact]
    public void OverrideTerrainNamesAreTheOnesVaslUses()
    {
        Assert.Equal("Stone Building, 2 Level", BoardCatalogTerrainEvidence.TerrainName(new Board01Building("stone", 2)));
        Assert.Equal("Wooden Building, 1 Level", BoardCatalogTerrainEvidence.TerrainName(new Board01Building("wooden", 1)));
        Assert.Throws<InvalidOperationException>(() => BoardCatalogTerrainEvidence.TerrainName(new Board01Building("brick", 1)));
    }

    internal static ScenarioA1TerrainBinding Binding(string hex) =>
        new(Board01TerrainCatalog.BoardId, Board01TerrainCatalog.BoardVersion, Board01TerrainCatalog.MetadataGitBlobSha, hex, 0, null);
}

/// <summary>Board 01 as a verified board handle, built from the committed Hex Fact oracle fixture and its source identities.</summary>
internal static class Board01Fixture
{
    private static readonly Lazy<JsonDocument> Document = new(() =>
    {
        using var file = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Oracle", "bd01.hexfacts.json.gz"));
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    });

    public static IReadOnlyList<string> Hexes() =>
        [.. Document.Value.RootElement.GetProperty("hexes").EnumerateArray().Select(hex => hex.GetProperty("hex").GetString()!)];

    public static VaslBoardSource Source()
    {
        var root = Document.Value.RootElement;
        return new VaslBoardSource(Board01TerrainCatalog.BoardId, Board01TerrainCatalog.BoardVersion, root.GetProperty("metadataBlob").GetString()!,
            root.GetProperty("losDataBlob").GetString()!, root.GetProperty("vaslCommit").GetString());
    }

    public static BoardHandle Handle(BoardReadStatus status = BoardReadStatus.Verified, (string Hex, string Terrain)? changed = null) =>
        Handle(Source(), status, changed);

    public static BoardHandle Handle(VaslBoardSource? source, BoardReadStatus status = BoardReadStatus.Verified, (string Hex, string Terrain)? changed = null)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var terrains = new Dictionary<string, TerrainType>(StringComparer.Ordinal);
        TerrainType? Terrain(JsonElement location, string hex)
        {
            if (location.GetProperty("terrain").GetString() is not { } name)
            {
                return null;
            }

            if (changed is { } change && change.Hex == hex && location.GetProperty("level").GetInt32() == 0)
            {
                name = change.Terrain;
            }

            if (!terrains.TryGetValue(name, out var terrain))
            {
                terrain = new TerrainType { Code = (byte)(terrains.Count + 1), Name = name, Category = LosCategory.Open };
                terrains[name] = terrain;
            }

            return terrain;
        }

        var hexes = new List<HexFacts>();
        foreach (var item in Document.Value.RootElement.GetProperty("hexes").EnumerateArray())
        {
            var hexText = item.GetProperty("hex").GetString()!;
            var name = HexName.Parse(hexText);
            Assert.True(geometry.TryGetIndex(name, out var index));
            LocationFacts Location(JsonElement location) => new(location.GetProperty("level").GetInt32(), Terrain(location, hexText), null);
            var center = Location(item.GetProperty("center"));
            LocationFacts[] locations = item.TryGetProperty("locations", out var list)
                ? [.. list.EnumerateArray().Select(Location)]
                : [center];
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, item.GetProperty("baseLevel").GetInt32(), false, center, locations, sides, null,
                CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, source?.LosDataBlob ?? "authored", status, "board 01 oracle fixture",
            new HexFactSet(geometry, "oracle", hexes), source);
    }
}
