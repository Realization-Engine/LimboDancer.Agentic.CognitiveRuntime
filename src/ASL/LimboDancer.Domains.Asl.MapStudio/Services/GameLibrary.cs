using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A synthetic game fixture replayed for the Studio: its record, its history, and whether its positions were checked
/// against the boards the Studio can load. <see cref="Diagnostics"/> holds the reader's and the projector's findings.
/// </summary>
public sealed record GameEntry(string Name, GameRecord? Record, GameHistory? History, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool PositionsChecked => History is { HasErrors: false } && History.Diagnostics.All(diagnostic => diagnostic.Code != "UNIT-STATE-020");

    public string Label => Record?.Label ?? Name;
}

/// <summary>
/// What the display receives for one perspective at one revision (ASL-UNIT-070, 031): the view, and the unit documents
/// made from it alone, as a synthetic placement set the overlay draws.
/// </summary>
public sealed record GameProjection(GameEntry Game, GameView View, UnitPlacementSet Set);

/// <summary>
/// The Studio's view of the unit state model (State Model Design, section 10; Unit Display Design, section 20): the
/// synthetic game records, embedded and saved under <c>{boards folder}/units/games</c>, replayed against the location
/// chains of the boards in play when the Studio can load them, and projected by perspective for the display. Nothing
/// here writes game state; the records are display input (ASL-UNIT-050, D2).
/// </summary>
public sealed class GameLibrary(UnitLibrary units, IBoardProvider boards, LivePlay? live = null)
{
    /// <summary>The prefix of a live game's name, such as <c>live:village</c>.</summary>
    public const string LivePrefix = "live:";

    private const string Suffix = ".game.json";
    private readonly ConcurrentDictionary<string, GameEntry> embedded = new(StringComparer.Ordinal);
    private readonly Lazy<IReadOnlyList<UnitCatalog>> catalogs = new(() =>
        [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()]);

    public IReadOnlyList<UnitCatalog> Catalogs => catalogs.Value;

    /// <summary>The embedded records, saved ones whose names are not embedded, then the live games.</summary>
    public IReadOnlyList<string> Names =>
        [.. UnitGames.Names, .. Saved().Where(name => !UnitGames.Names.Contains(name)), .. (live?.Games() ?? []).Select(scope => LivePrefix + scope.Game)];

    private string GamesRoot => Path.Combine(units.UnitsRoot, "games");

    public GameEntry Load(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.StartsWith(LivePrefix, StringComparison.Ordinal) && live is not null)
        {
            var game = name[LivePrefix.Length..];
            var record = live.Store.Read(new GameScope(LivePlay.Tenant, game));
            var history = live.History(game);
            return record is null || history is null
                ? Replay(name, null)
                : new GameEntry(name, record with
                {
                    Label = record.Label + " (live)"
                }, history, history.Diagnostics);
        }

        if (UnitGames.Names.Contains(name))
        {
            return embedded.GetOrAdd(name, _ => Replay(name, UnitGames.Read(name)));
        }

        return Saved().Contains(name)
            ? Replay(name, GameEventReader.Read(File.ReadAllBytes(Path.Combine(GamesRoot, name + Suffix))))
            : Replay(name, null);
    }

    /// <summary>The games with anything to draw on a board or map: through its geometry, or its placed boards on a composed map.</summary>
    public IReadOnlyList<GameEntry> GamesFor(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        var target = UnitLibrary.TargetFor(board);
        return [.. Names.Select(Load).Where(entry => entry.History is { HasErrors: false, States.Count: > 0 } history
            && Projection(entry, Perspective.Adjudicator, history.States.Count).Set.Units
                .Any(unit => BoardLocation.TryParse(unit.Location, out var location) && target.Locate(location) is not null))];
    }

    /// <summary>The display input for a perspective at a revision: the view and the documents made from it.</summary>
    public GameProjection Projection(GameEntry entry, Perspective perspective, long revision)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(perspective);
        var history = entry.History ?? throw new InvalidOperationException($"{entry.Name} did not replay.");
        var view = GameView.Of(history, revision, perspective);
        var set = GameDocuments.PlacementSet(view, $"game-{entry.Name.Replace('.', '-')}-{perspective.Name}-r{revision}",
            $"{entry.Label}: {perspective.Name}, revision {revision}", units.Vocabulary, Catalogs);
        return new GameProjection(entry, view, set);
    }

    /// <summary>
    /// Reads a case (ASL-UNIT-060) in the game as it stood at <paramref name="asOf"/>: the events through that revision
    /// replayed, read through the map read API over the Studio's boards.
    /// </summary>
    public CaseReadResult ReadCase(GameEntry entry, long asOf, string attacker, BoardLocation location, long expectedRevision, Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Record is null)
        {
            return new CaseReadResult(CaseReadStatus.Unavailable, "CASE-002", $"{entry.Name} did not read.", null);
        }

        var history = GameProjector.Project([.. entry.Record.Events.Take((int)asOf)], units.Vocabulary, Catalogs, Chains(entry.Record));
        var reader = new CaseReader(new HistoryGameSource([history]), new StudioBoardCatalog(boards), units.Vocabulary, Catalogs);
        return reader.Read(new CaseRequest(entry.Record.Events[0].Scope, "map-studio-game-states", attacker, location, expectedRevision, perspective));
    }

    private string[] Saved() => Directory.Exists(GamesRoot)
        ? [.. Directory.EnumerateFiles(GamesRoot, "*" + Suffix).Select(path => Path.GetFileName(path)[..^Suffix.Length]).Order(StringComparer.Ordinal)]
        : [];

    private GameEntry Replay(string name, GameRecordResult? read)
    {
        if (read?.Record is not { } record)
        {
            return new GameEntry(name, null, null, read?.Diagnostics ?? [UnitDiagnostic.Error("UNIT-STATE-001", $"There is no game '{name}'.")]);
        }

        var history = GameProjector.Project(record.Events, units.Vocabulary, Catalogs, Chains(record));
        return new GameEntry(name, record, history, [.. read.Diagnostics, .. history.Diagnostics]);
    }

    /// <summary>The location chains of every board the game places, or null when any of them cannot be loaded.</summary>
    private HexFactLocationChains? Chains(GameRecord record)
    {
        if (record.Events.Count == 0 || record.Events[0].Payload is not GameStarted started)
        {
            return null;
        }

        var loaded = new List<(BoardRef, string, Maps.Derivation.HexFactSet)>();
        foreach (var placed in started.Map.Boards)
        {
            if (boards.Load(placed.Board).Board is not { } board)
            {
                return null;
            }

            loaded.Add((board.Ref, board.Version, board.Facts));
        }

        return new HexFactLocationChains(loaded);
    }
}
