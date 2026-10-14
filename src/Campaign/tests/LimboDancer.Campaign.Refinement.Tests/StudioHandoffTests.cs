using System.Text;
using System.Text.Json;
using LimboDancer.Campaign.PilotHost;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Campaign.Refinement.Tests;

public sealed class StudioHandoffTests
{
    private static readonly PilotInputs Inputs = new(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "goodwood.json")));

    [Theory]
    [InlineData("uk-engineer-01", "B/M-6", "Allied", 4)]
    [InlineData("de-81mm-01", "C/Y-4", "German", 6)]
    public async Task ImportedSetupReopensAfterRestartWithoutChangingCampaign(string id, string anchor, string side, int count)
    {
        var root = Path.Combine(Path.GetTempPath(), "campaign-studio-" + Guid.NewGuid().ToString("N"));
        try
        {
            const string seed = "00112233445566778899aabbccddeeff";
            var selection = new FormationSelection(id, anchor);
            var d = new PilotDeployment(Inputs, new PilotGenerator(Inputs).Generate(seed, FormationDeployment.CropFor(Inputs, selection)), seed, selection);
            var context = new DeploymentContext("studio-test", "snapshot", side == "Allied" ? [anchor] : [], side == "German" ? [anchor] : [], selection);
            var setup = d.SetComplete(context, d.AutoDeploy(context, d.Create(context)), true);
            var original = JsonSerializer.Serialize(setup);
            var handoff = await PilotHandoffs.Prepare(d, context, setup);
            var packet = new CampaignImportRequest(d.Package.Board.Value, d.Package.Name, d.Package.Version,
                Encoding.UTF8.GetString(d.Package.FeaturesBytes()), handoff.GameRecord);
            var options = new StudioOptions { BoardsRoot = Path.Combine(root, "boards"), CacheRoot = Path.Combine(root, "cache") };
            var (importer, boards, live) = Services(options);
            Assert.Throws<InvalidDataException>(() => importer.Import(packet with { Version = "wrong" }));
            Assert.Throws<InvalidDataException>(() => importer.Import(packet with { GameRecord = "{}" }));
            var wrongCatalog = d.Package with { Model = d.Package.Model with { CatalogHash = "wrong" } };
            Assert.Throws<InvalidDataException>(() => importer.Import(packet with { Features = Encoding.UTF8.GetString(wrongCatalog.FeaturesBytes()), Version = wrongCatalog.Version }));
            Assert.Empty(boards.List());
            Assert.Empty(live.Games());
            var imported = importer.Import(packet);
            Assert.False(imported.Existing);
            var scope = Assert.Single(live.Games());
            Assert.Equal(imported.Game, scope.Game);
            var recorded = GameEventWriter.Write(scope, live.Store.Read(scope)!);
            var history = live.History(imported.Game)!;
            Assert.False(history.HasErrors);
            Assert.Equal(count, history.Current!.Units.Count);
            foreach (var unit in history.Current.Units)
            {
                var source = d.Units.Single(u => PilotHandoffs.AslId(u.Id) == unit.Id);
                Assert.Equal(source.Definition, unit.Definition);
                Assert.Equal(setup.Placements.Single(p => p.UnitId == source.Id).Hex, Assert.IsType<MapPosition>(unit.Position).Location.Hex.ToString());
            }
            var next = await live.Planner.PlanAsync(GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
            { gameId = imported.Game, attemptId = "advance-imported", expectedRevision = history.Current.Revision }), LivePlay.Tenant, "studio-user");
            Assert.True(next.CanCommit, string.Join("; ", next.Reasons));
            Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Campaign setup handoff", history.Current.Revision, next.Events, live.Planner.Replay).Status);
            recorded = GameEventWriter.Write(scope, live.Store.Read(scope)!);
            var (restarted, reloadedBoards, reloadedLive) = Services(options);
            Assert.True(restarted.Import(packet).Existing);
            Assert.Single(reloadedLive.Games());
            Assert.Equal(recorded, GameEventWriter.Write(scope, reloadedLive.Store.Read(scope)!));
            Assert.Equal(d.Package.Version, reloadedBoards.LoadPackage(d.Package.Board).Package!.Version);
            Assert.Equal(d.Package.FeaturesBytes(), reloadedBoards.LoadPackage(d.Package.Board).Package!.FeaturesBytes());
            Assert.Equal(original, JsonSerializer.Serialize(setup));
            var different = d.Package with { Name = "Different board" };
            Assert.Throws<InvalidDataException>(() => restarted.Import(packet with { Name = different.Name, Version = different.Version }));
            // A valid board published before interruption is reusable when no game record exists yet.
            var recoveryOptions = new StudioOptions { BoardsRoot = Path.Combine(root, "recovery"), CacheRoot = options.CacheRoot };
            var (recover, recoveryBoards, recoveryLive) = Services(recoveryOptions);
            recoveryBoards.Save(d.Package, false);
            Assert.False(recover.Import(packet).Existing);
            Assert.Single(recoveryLive.Games());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static (CampaignImport Import, AuthoredBoardService Boards, LivePlay Live) Services(StudioOptions options)
    {
        var boards = new AuthoredBoardService(options, new FixedCatalog());
        var units = new UnitLibrary(options);
        var live = new LivePlay(units, new Provider(boards));
        return (new CampaignImport(boards, live, units), boards, live);
    }
    private sealed class FixedCatalog : ICatalogSource
    {
        public (TerrainCatalog Catalog, string Hash)? Catalog() => (Inputs.Catalog, Inputs.CatalogBlob);
    }
    private sealed class Provider(AuthoredBoardService boards) : IBoardProvider
    {
        public string? SourceDescription => "Test campaign";
        public string? CatalogBlob => Inputs.CatalogBlob;
        public IReadOnlyList<BoardListing> List() => [];
        public BoardLoadResult Load(BoardRef board) => boards.Load(board);
        public BoardLoadResult? Cached(BoardRef board) => null;
    }
}

