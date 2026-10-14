using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

public sealed record CampaignImportRequest(string Board, string Name, string Version, string Features, string GameRecord);
public sealed record CampaignImportResult(string Game, string Path, bool Existing);

/// <summary>Imports a bounded setup without replacing a board or an existing game's progress.</summary>
public sealed class CampaignImport(AuthoredBoardService boards, LivePlay live, UnitLibrary units)
{
    private readonly object gate = new();

    public CampaignImportResult Import(CampaignImportRequest request)
    {
        lock (gate)
        {
            var board = BoardRef.Parse(request.Board);
            if (board.Kind != BoardRefKind.Authored)
            {
                throw new InvalidDataException("Campaign terrain must be an authored board.");
            }

            var package = new BoardPackage(board, request.Name, FeatureModelJson.Deserialize(Encoding.UTF8.GetBytes(request.Features)));
            if (package.Version != request.Version)
            {
                throw new InvalidDataException("Campaign board version does not match its contents.");
            }

            var catalog = boards.Catalog ?? throw new InvalidDataException("Configure the ASL terrain catalog before importing a campaign.");
            if (package.Model.CatalogHash != catalog.Hash)
            {
                throw new InvalidDataException("Campaign and ASL terrain catalogs differ.");
            }

            var built = boards.Build(package, catalog.Catalog, false);
            if (built.Status != BoardStatus.AuthoredValid)
            {
                throw new InvalidDataException("Campaign board failed ASL validation.");
            }

            var read = GameEventReader.Read(Encoding.UTF8.GetBytes(request.GameRecord));
            if (read.HasErrors || read.Record is not { Events.Count: > 0 } record || record.Synthetic ||
                record.Events[0].Payload is not GameStarted || record.Events.Skip(1).Any(e => e.Payload is not InstanceCreated))
            {
                throw new InvalidDataException("Only a validated initial ASL setup can be imported.");
            }

            var scope = new GameScope(LivePlay.Tenant, record.Events[0].Scope.Game);
            if (!scope.Game.StartsWith("campaign-", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Invalid campaign game identity.");
            }

            GameEvent[] events = [.. record.Events.Select(e => new GameEvent(scope, e.EventId, e.Revision, e.Time, e.Source, e.Type,
                e.Payload, e.RulePackage, e.Causes, e.Visibility))];
            // Validate before publishing either file. The temporary catalog contains only this exact board.
            var planner = new GamePlanner(live.Store, new InMemoryBoardCatalog([new BoardHandle(board, package.Version,
                BoardReadStatus.AuthoredValid, "Campaign import", built.Facts)]), units.Vocabulary, live.Catalogs);
            var history = planner.Replay(events);
            if (history.HasErrors)
            {
                throw new InvalidDataException("ASL setup validation: " + string.Join("; ", history.Diagnostics.Select(d => d.Message)));
            }

            if (boards.Exists(board) && boards.LoadPackage(board).Package?.Version != package.Version)
            {
                throw new InvalidDataException("An existing ASL board has different contents. It will not be replaced.");
            }

            var existing = live.Store.Read(scope);
            if (existing is not null)
            {
                var expected = GameEventWriter.Write(scope, new GameRecord(record.Label, false, events));
                var prefix = GameEventWriter.Write(scope, new GameRecord(record.Label, false, existing.Events.Take(events.Length).ToArray()));
                if (prefix != expected)
                {
                    throw new InvalidDataException("The ASL game identity already belongs to a different setup.");
                }
            }
            // Publish the complete package directory in one rename. A retry after game publication fails can reuse it.
            if (!boards.Exists(board))
            {
                var staging = Path.Combine(boards.Boards.Root, ".campaign-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var store = new BoardPackageStore(staging);
                    store.Save(package);
                    Directory.Move(store.DirectoryOf(board), boards.Boards.DirectoryOf(board));
                }
                finally
                {
                    if (Directory.Exists(staging))
                    {
                        Directory.Delete(staging, true);
                    }
                }
            }
            if (existing is null)
            {
                var result = live.Store.Append(scope, record.Label, 0, events, live.Planner.Replay);
                if (result.Status != AppendStatus.Committed)
                {
                    throw new InvalidDataException("ASL import was not saved: " + string.Join("; ", result.Diagnostics.Select(d => d.Message)));
                }
            }
            return new(scope.Game, "/games/play?game=" + Uri.EscapeDataString(scope.Game), existing is not null);
        }
    }
}
