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
