using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// A game set up under an earlier catalog version replays (the user, 2026-10-05: the version a game records is where it was set up, not a lock).
/// It reads the loaded catalog of the name it records, and every record of it is checked against the packages as in any game. The game is The
/// Guards Counterattack as it was played (the fixture <c>guards-dl-01.game.json</c>), with its catalog's version rewritten.
/// </summary>
public sealed class OlderGamesTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("5a7d1f00-0000-4000-8000-0000000057d0");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();

    private static readonly UnitCatalog[] Catalogs =
        [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, Vocabulary)?.Catalog).OfType<UnitCatalog>()];

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-older-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public OlderGamesTests() => store = new FileGameStore(Path.Combine(root, "games"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GameHistory Replayed(string catalog)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "guards-dl-01.game.json"));
        Assert.Contains("asl-scenario-a1@1.13.0", text, StringComparison.Ordinal);
        var file = Path.Combine(root, "games", Tenant.ToString("N"), "guards-dl-01.game.json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text.Replace("asl-scenario-a1@1.13.0", catalog, StringComparison.Ordinal));
        var planner = new GamePlanner(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, Catalogs, fireLos: new ClearLos());
        return planner.Replay(store.Read(new GameScope(Tenant, "guards-dl-01"))!.Events);
    }

    [Theory]
    [InlineData("asl-scenario-a1@1.1.0")]
    [InlineData("asl-scenario-a1@1.12.0")]
    public void AGameSetUpUnderAnEarlierCatalogVersionReplaysWithEveryRecordChecked(string recorded)
    {
        var history = Replayed(recorded);

        Assert.Empty(history.Diagnostics);
        Assert.Equal(history.Events.Count, history.States.Count);
        Assert.Contains(history.Events, item => item.Payload is FireResolved);
        Assert.Contains(history.Events, item => item.Payload is CloseCombatResolved);

        // The record keeps the version it was set up under; the state reads the catalog that is loaded.
        Assert.Equal(recorded, Assert.IsType<GameStarted>(history.Events[0].Payload).Catalog);
        Assert.Equal(LiveFire.CatalogVersion, history.Current!.Catalog.Version);
    }

    [Fact]
    public void AGameOfACatalogThatIsNotLoadedDoesNotReplay()
    {
        var history = Replayed("asl-no-such-catalog@1.0.0");

        Assert.True(history.HasErrors);
        Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-STATE-008");
    }
}
