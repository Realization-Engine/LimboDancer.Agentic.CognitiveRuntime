using System.Collections.Concurrent;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// A synthetic game fixture replayed for the Studio: its record, its history, and whether its positions were checked
/// against the boards the Studio can load. <see cref="Diagnostics"/> holds the reader's and the projector's findings.
/// </summary>
public sealed record GameEntry(string Name, GameRecord? Record, GameHistory? History, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool PositionsChecked => History is { HasErrors: false } && History.Diagnostics.All(diagnostic => diagnostic.Code != "UNIT-STATE-020");
}

/// <summary>
/// The Studio's view of the unit state model (State Model Design, section 10): the synthetic game fixtures, replayed
/// against the location chains of the boards in play when the Studio can load them, and read by perspective. Nothing
/// here writes game state; the fixtures are display input (ASL-UNIT-050, D2).
/// </summary>
public sealed class GameLibrary(UnitLibrary units, IBoardProvider boards)
{
    private readonly ConcurrentDictionary<string, GameEntry> entries = new(StringComparer.Ordinal);
    private readonly Lazy<IReadOnlyList<UnitCatalog>> catalogs = new(() =>
        [.. UnitCatalogs.Names.Select(name => UnitCatalogs.Read(name, units.Vocabulary)?.Catalog).OfType<UnitCatalog>()]);

    public IReadOnlyList<string> Names => UnitGames.Names;

    public IReadOnlyList<UnitCatalog> Catalogs => catalogs.Value;

    public GameEntry Load(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return entries.GetOrAdd(name, Replay);
    }

    /// <summary>Registers the view as a placement set for the board viewer and returns the set id.</summary>
    public string ShowOnBoard(GameEntry entry, Perspective perspective, long revision)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(perspective);
        var history = entry.History ?? throw new InvalidOperationException($"{entry.Name} did not replay.");
        var view = GameView.Of(history, revision, perspective);
        var setId = $"game-{entry.Name.Replace('.', '-')}-{perspective.Name}-r{revision}";
        units.Register(GameDocuments.PlacementSet(view, setId, $"{entry.Record!.Label}: {perspective.Name}, revision {revision}", units.Vocabulary, Catalogs));
        return setId;
    }

    private GameEntry Replay(string name)
    {
        var read = UnitGames.Read(name);
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
