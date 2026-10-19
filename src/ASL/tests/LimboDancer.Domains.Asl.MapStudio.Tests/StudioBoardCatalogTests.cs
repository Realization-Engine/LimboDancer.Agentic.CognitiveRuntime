using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

public sealed class StudioBoardCatalogTests
{
    [Fact]
    public void PinnedGamesUseAcceptedVersionEvenWhenLatestBoardHasChanged()
    {
        var provider = new VersionedBoards();
        var catalog = new StudioBoardCatalog(provider);
        var first = catalog.TryGetBoard(FakeBoardProvider.Board.Ref, FakeBoardProvider.Version).Board;
        Assert.NotNull(first);
        for (var index = 0; index < 100; index++)
        {
            Assert.Same(first, catalog.TryGetBoard(FakeBoardProvider.Board.Ref, FakeBoardProvider.Version).Board);
        }
        Assert.Equal(0, provider.LatestReads);
        Assert.Null(catalog.TryGetBoard(FakeBoardProvider.Board.Ref, "unavailable-version").Board);
        Assert.Equal(0, provider.LatestReads);
        Assert.NotNull(catalog.TryGetBoard(FakeBoardProvider.Board.Ref).Board);
        Assert.Equal(1, provider.LatestReads);
    }

    [Fact]
    public void AHitReturnsTheKeptResultAndAMissStillSaysWhy()
    {
        // The Play Step Plan, task 7 (A1): a lookup that hits returns the handle's kept result, with no diagnostic; a version the Studio cannot
        // load still says so.
        var catalog = new StudioBoardCatalog(new VersionedBoards());
        var first = catalog.TryGetBoard(FakeBoardProvider.Board.Ref, FakeBoardProvider.Version);
        Assert.NotNull(first.Board);
        Assert.Empty(first.Diagnostics);
        Assert.Same(first, catalog.TryGetBoard(FakeBoardProvider.Board.Ref, FakeBoardProvider.Version));
        var missing = catalog.TryGetBoard(FakeBoardProvider.Board.Ref, "unavailable-version");
        Assert.Null(missing.Board);
        Assert.Equal("MAP-READ-001", Assert.Single(missing.Diagnostics).Code);
    }

    [Fact]
    public void NewlyPublishedFootprintReplacesUnrestrictedCachedHandleAndSurvivesRestart()
    {
        var root = Path.Combine(Path.GetTempPath(), "asl-catalog-footprint-" + Guid.NewGuid().ToString("N"));
        try
        {
            var provider = new VersionedBoards();
            var footprints = new CampaignPlayableFootprints(root);
            var catalog = new StudioBoardCatalog(provider, footprints);
            var board = FakeBoardProvider.Board;
            var hexes = board.Facts.Hexes.Take(2).Select(hex => hex.Hex).ToArray();
            var before = catalog.TryGetBoard(board.Ref, board.Version).Board!;
            Assert.NotNull(before.HexFacts(hexes[1]));
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                Version = "goodwood-map-ownership-1",
                Board = board.Ref.Value,
                BoardVersion = board.Version,
                Parents = new Dictionary<string, string[]> { ["A/A-1"] = [hexes[0].ToString()] },
            });
            footprints.Publish(board.Ref, board.Version, board.Facts.Geometry, json);
            var after = catalog.TryGetBoard(board.Ref, board.Version).Board!;
            Assert.NotSame(before, after);
            Assert.Same(before.Facts, after.Facts);
            Assert.Equal(before.Version, after.Version);
            Assert.NotNull(after.HexFacts(hexes[0]));
            Assert.Null(after.HexFacts(hexes[1]));
            Assert.Same(after, catalog.TryGetBoard(board.Ref, board.Version).Board);
            var restarted = new StudioBoardCatalog(provider, new CampaignPlayableFootprints(root));
            Assert.Null(restarted.TryGetBoard(board.Ref, board.Version).Board!.HexFacts(hexes[1]));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private sealed class VersionedBoards : IBoardProvider
    {
        public int LatestReads
        {
            get;
            private set;
        }
        public string? SourceDescription => "Test";
        public string? CatalogBlob => null;
        public IReadOnlyList<BoardListing> List() => [];
        public BoardLoadResult? Cached(BoardRef board) => null;
        public BoardLoadResult Load(BoardRef board)
        {
            LatestReads++;
            return new(FakeBoardProvider.Board with
            {
                Version = "changed-latest",
            }, []);
        }
        public StudioBoard? LoadVersion(BoardRef board, string version) =>
            board == FakeBoardProvider.Board.Ref && version == FakeBoardProvider.Version ? FakeBoardProvider.Board : null;
    }
}
