using System.Text;
using LimboDancer.Domains.Asl.Units.Catalog;
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
            Assert.Null(importer.ReadOwnership(imported.Game, packet.Board));
            var ownership = JsonSerializer.Serialize(new { Version = "goodwood-map-ownership-1", Board = packet.Board,
                BoardVersion = packet.Version, Parents = new Dictionary<string, string[]> { [anchor] = [setup.Placements[0].Hex] } });
            packet = packet with { MapOwnership = ownership };
            Assert.True(importer.Import(packet).Existing); // Attach metadata to an older accepted game without replacing it.
            Assert.Equal(anchor, importer.ReadOwnership(imported.Game, packet.Board)![setup.Placements[0].Hex]);
            Assert.Throws<InvalidDataException>(() => importer.Import(packet with { MapOwnership = ownership.Replace(anchor, "other", StringComparison.Ordinal) }));
            Assert.Equal(anchor, importer.ReadOwnership(imported.Game, packet.Board)![setup.Placements[0].Hex]);
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
            Assert.Equal(id == "de-81mm-01" ? 6 : 0, history.Current.Equipment.Count);
            foreach (var weapon in history.Current.Equipment)
            {
                Assert.Equal(HoldingRole.Manned, weapon.Holding!.Role);
                Assert.Equal(history.Current.Units.Single(u => u.Id == weapon.Holding.Holder).Position, weapon.Position);
            }
            var next = await live.Planner.PlanAsync(GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
            { gameId = imported.Game, attemptId = "advance-imported", expectedRevision = history.Current.Revision }), LivePlay.Tenant, "studio-user");
            Assert.True(next.CanCommit, string.Join("; ", next.Reasons));
            Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Campaign setup handoff", history.Current.Revision, next.Events, live.Planner.Replay).Status);
            recorded = GameEventWriter.Write(scope, live.Store.Read(scope)!);
            Assert.Equal(recorded, importer.ExportProgress(imported.Game));
            var transfer = packet with { ProgressRecord = importer.ExportProgress(imported.Game) };
            var transferOptions = new StudioOptions { BoardsRoot = Path.Combine(root, "transferred"), CacheRoot = options.CacheRoot };
            var (receiver, receiverBoards, receiverLive) = Services(transferOptions);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            Assert.Throws<OperationCanceledException>(() => receiver.Import(transfer, cancelled.Token));
            Assert.Throws<InvalidDataException>(() => receiver.Import(transfer with { ProgressRecord = "{}" }));
            Assert.Empty(receiverLive.Games());
            Assert.Empty(receiverBoards.List());
            Assert.False(receiver.Import(transfer).Existing);
            Assert.Equal(recorded, GameEventWriter.Write(scope, receiverLive.Store.Read(scope)!));
            Assert.Equal(history.Current.Revision + next.Events.Count, receiverLive.History(imported.Game)!.Current!.Revision);
            Assert.True(receiver.Import(packet).Existing); // Older initial setup cannot roll back transferred play.
            Assert.Equal(recorded, receiver.ExportProgress(imported.Game));
            Assert.True(receiver.Import(transfer).Existing); // Repeated transfer is idempotent.
            var conflicting = live.Store.Read(scope)!;
            var tail = conflicting.Events[^1];
            var changedTail = new GameEvent(scope, tail.EventId + "-diverged", tail.Revision, tail.Time, tail.Source,
                tail.Type, tail.Payload, tail.RulePackage, tail.Causes, tail.Visibility);
            var divergent = GameEventWriter.Write(scope, conflicting with { Events = [.. conflicting.Events.SkipLast(1), changedTail] });
            Assert.Throws<InvalidDataException>(() => receiver.Import(transfer with { ProgressRecord = divergent }));
            Assert.Equal(recorded, receiver.ExportProgress(imported.Game));
            var advanceOptions = new StudioOptions { BoardsRoot = Path.Combine(root, "existing-initial"), CacheRoot = options.CacheRoot };
            var (advancer, _, advanceLive) = Services(advanceOptions);
            advancer.Import(packet);
            Assert.True(advancer.Import(transfer).Existing); // Append the transferred continuation to an existing matching setup.
            Assert.Equal(recorded, advancer.ExportProgress(imported.Game));
            Assert.False(advanceLive.History(imported.Game)!.HasErrors);
            var (restarted, reloadedBoards, reloadedLive) = Services(options);
            Assert.True(restarted.Import(packet with { MapOwnership = null }).Existing); // Older callers cannot erase metadata.
            Assert.Equal(anchor, restarted.ReadOwnership(imported.Game, packet.Board)![setup.Placements[0].Hex]);
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

    internal static async Task AssertFullImport(SituationHandoff handoff, string root)
    {
        var context = SituationHandoffs.ContextJson(handoff);
        var acceptedBoard = new BoardPackage(BoardRef.Parse(handoff.Board), handoff.Name,
            FeatureModelJson.Deserialize(Encoding.UTF8.GetBytes(handoff.Features)));
        var ownership = SituationMapOwnership.Create(Inputs, acceptedBoard);
        var packet = new CampaignImportRequest(handoff.Board, handoff.Name, handoff.BoardVersion,
            handoff.Features, handoff.GameRecord, context, MapOwnership: JsonSerializer.Serialize(ownership));
        var options = new StudioOptions { BoardsRoot = Path.Combine(root, "boards"), CacheRoot = Path.Combine(root, "cache") };
        var (importer, boards, live) = Services(options);
        var imported = importer.Import(packet);
        Assert.False(imported.Existing);
        Assert.Equal(context, importer.ReadContext(imported.Game));
        var importedOwnership = importer.ReadOwnership(imported.Game, handoff.Board)!;
        Assert.Equal(37788, importedOwnership.Count);
        Assert.All(ownership.Parents, parent => Assert.All(parent.Value, hex => Assert.Equal(parent.Key, importedOwnership[hex])));
        var history = live.History(imported.Game)!;
        Assert.False(history.HasErrors, string.Join("; ", history.Diagnostics.Select(d => d.Message)));
        var state = history.Current!;
        Assert.Equal(530, state.Units.Count);
        Assert.Equal(94, state.Equipment.Count);
        var sourceUnits = handoff.Roster.Ground.SelectMany(f => f.Units).ToDictionary(u => PilotHandoffs.AslId(u.Id));
        var sourcePositions = handoff.Setup.Positions.ToDictionary(p => p.UnitId);
        var geometry = boards.LoadPackage(BoardRef.Parse(handoff.Board)).Package!.Model.Geometry;
        void Position(string id, Position position, DefinitionReference? definition)
        {
            var source = sourceUnits[id];
            Assert.Equal(source.Definition, definition);
            var at = Assert.IsType<MapPosition>(position).Location;
            var index = geometry.IndexOf(at.Hex);
            var expected = sourcePositions[source.Id];
            Assert.Equal((expected.Column, expected.Row),
                (index.Column + SituationAslBoard.OriginColumn, index.Row + SituationAslBoard.OriginRow));
        }
        foreach (var unit in state.Units) Position(unit.Id, unit.Position, unit.Definition);
        foreach (var weapon in state.Equipment)
        {
            Position(weapon.Id, weapon.Position, weapon.Definition);
            Assert.Equal(PilotHandoffs.AslId(sourceUnits[weapon.Id].Holding!.Holder), weapon.Holding!.Holder);
            Assert.Equal(Assert.IsType<MapPosition>(state.Units.Single(u => u.Id == weapon.Holding.Holder).Position).Location,
                Assert.IsType<MapPosition>(weapon.Position).Location);
        }
        var scope = Assert.Single(live.Games());
        var next = await live.Planner.PlanAsync(GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
        { gameId = imported.Game, attemptId = "advance-full-import", expectedRevision = state.Revision }), LivePlay.Tenant, "studio-user");
        Assert.True(next.CanCommit, string.Join("; ", next.Reasons));
        Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Operation Goodwood campaign setup", state.Revision, next.Events, live.Planner.Replay).Status);
        var recorded = GameEventWriter.Write(scope, live.Store.Read(scope)!);
        var (restarted, reloadedBoards, reloadedLive) = Services(options);
        Assert.True(restarted.Import(packet).Existing);
        Assert.Equal(recorded, GameEventWriter.Write(scope, reloadedLive.Store.Read(scope)!));
        Assert.Equal(context, restarted.ReadContext(imported.Game));
        Assert.Equal(handoff.BoardVersion, reloadedBoards.LoadPackage(BoardRef.Parse(handoff.Board)).Package!.Version);
        Assert.Throws<InvalidDataException>(() => restarted.Import(packet with { CampaignContext = "{}" }));
        Assert.Equal(context, restarted.ReadContext(imported.Game));
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

