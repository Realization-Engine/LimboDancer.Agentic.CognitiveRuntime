using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

public sealed record CampaignImportRequest(string Board, string Name, string Version, string Features, string GameRecord, string? CampaignContext = null, string? ProgressRecord = null);
public sealed record CampaignImportResult(string Game, string Path, bool Existing);

/// <summary>Imports campaign setup and compatible saved progress without replacing divergent history.</summary>
public sealed class CampaignImport(AuthoredBoardService boards, LivePlay live, UnitLibrary units)
{
    private readonly object gate = new();

    public CampaignImportResult Import(CampaignImportRequest request, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[]? contextBytes = null;
            if (request.CampaignContext is { } context)
            {
                contextBytes = Encoding.UTF8.GetBytes(context);
                if (contextBytes.Length > 2_000_000)
                {
                    throw new InvalidDataException("Campaign context is too large.");
                }
                using var document = JsonDocument.Parse(contextBytes);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException("Campaign context must be a JSON object.");
                }
            }
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
            if (request.ProgressRecord is { } progress)
            {
                if (Encoding.UTF8.GetByteCount(progress) > 20_000_000)
                {
                    throw new InvalidDataException("ASL progress exceeds the transfer size limit.");
                }
                var progressRead = GameEventReader.Read(Encoding.UTF8.GetBytes(progress));
                if (progressRead.HasErrors || progressRead.Record is not { } saved || saved.Synthetic
                    || saved.Events.Any(e => e.Scope.Game != scope.Game))
                {
                    throw new InvalidDataException("Invalid ASL progress record or game identity.");
                }
                var restored = saved.Events.Select(e => new GameEvent(scope, e.EventId, e.Revision, e.Time, e.Source, e.Type,
                    e.Payload, e.RulePackage, e.Causes, e.Visibility)).ToArray();
                if (restored.Length < events.Length || !SamePrefix(scope, events, restored, events.Length))
                {
                    throw new InvalidDataException("ASL progress does not belong to this accepted campaign setup.");
                }
                events = restored;
            }
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
                if (!SamePrefix(scope, events, existing.Events, Math.Min(events.Length, existing.Events.Count)))
                {
                    throw new InvalidDataException("The ASL game has divergent history. Existing progress will not be replaced.");
                }
            }
            var contextPath = ContextPath(scope.Game);
            if (File.Exists(contextPath) && (contextBytes is null || !File.ReadAllBytes(contextPath).AsSpan().SequenceEqual(contextBytes)))
            {
                throw new InvalidDataException("The ASL game already has different campaign context. It will not be replaced.");
            }
            cancellationToken.ThrowIfCancellationRequested();
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
            // Context is immutable, and may be reused after an interrupted game publication.
            if (contextBytes is not null && !File.Exists(contextPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(contextPath)!);
                var stage = contextPath + ".stage-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllBytes(stage, contextBytes);
                    try
                    {
                        File.Move(stage, contextPath);
                    }
                    catch (IOException) when (File.Exists(contextPath))
                    {
                        if (!File.ReadAllBytes(contextPath).AsSpan().SequenceEqual(contextBytes))
                        {
                            throw;
                        }
                    }
                }
                finally
                {
                    if (File.Exists(stage))
                    {
                        File.Delete(stage);
                    }
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            var revision = existing?.Events.Count ?? 0;
            if (events.Length > revision)
            {
                var result = live.Store.Append(scope, existing?.Label ?? record.Label, revision, events.Skip(revision).ToArray(), live.Planner.Replay);
                if (result.Status != AppendStatus.Committed)
                {
                    throw new InvalidDataException("ASL import was not saved: " + string.Join("; ", result.Diagnostics.Select(d => d.Message)));
                }
            }
            return new(scope.Game, "/games/play?game=" + Uri.EscapeDataString(scope.Game), existing is not null);
        }
    }

    /// <summary>A complete event snapshot. The caller must include the accepted setup and terrain when transferring it.</summary>
    public string? ExportProgress(string game)
    {
        _ = ContextPath(game); // Apply the same campaign identity bounds before reading the store.
        var scope = new GameScope(LivePlay.Tenant, game);
        return live.Store.Read(scope) is { } record ? GameEventWriter.Write(scope, record) : null;
    }

    private static bool SamePrefix(GameScope scope, IReadOnlyList<GameEvent> left, IReadOnlyList<GameEvent> right, int count) =>
        GameEventWriter.Write(scope, new GameRecord("Campaign comparison", false, left.Take(count).ToArray()))
        == GameEventWriter.Write(scope, new GameRecord("Campaign comparison", false, right.Take(count).ToArray()));

    public string? ReadContext(string game)
    {
        var path = ContextPath(game);
        return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
    }

    private string ContextPath(string game)
    {
        if (!game.StartsWith("campaign-", StringComparison.Ordinal) || game.Length > 100 ||
            game.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
        {
            throw new InvalidDataException("Invalid campaign game identity.");
        }
        return Path.Combine(boards.Boards.Root, ".campaign-contexts", game + ".json");
    }

}
