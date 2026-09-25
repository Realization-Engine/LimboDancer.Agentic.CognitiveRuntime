using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>ReadCase(tenant, game, package, attacker, location, expectedRevision, perspective)</c> (ASL-UNIT-060): a unit of
/// a game attempts something at a location, and a consumer asks for everything the case reads, as one consistent
/// snapshot at the revision it expects. <see cref="Package"/> is the domain package the question is asked under; the
/// read records it and does not interpret it.
/// </summary>
public sealed record CaseRequest(GameScope Scope, string Package, string Attacker, BoardLocation Location, long ExpectedRevision, Perspective Perspective);

/// <summary>How a read ended (ASL-UNIT-061). Only <see cref="Definitive"/> may support a definitive conclusion.</summary>
public enum CaseReadStatus
{
    Definitive,

    /// <summary>The game, the attacker, or a board is not available to this reader.</summary>
    Unavailable,

    /// <summary>The game, the map in play, or the catalog disagree with each other.</summary>
    Inconsistent,

    /// <summary>The reader is not entitled to this game or perspective.</summary>
    Unauthorized,

    /// <summary>The game has moved on from the expected revision.</summary>
    Stale,

    /// <summary>A snapshot is returned, but the terrain comes from a board that is not verified (ASL-MAP-044).</summary>
    Unverified,
}

/// <summary>The attacker as the case reads it: the instance, its definition, derived Good Order, and MF spent this phase.</summary>
public sealed record CaseAttacker(UnitInstance Unit, UnitDefinition Definition, ConditionState GoodOrder, int MfSpent);

/// <summary>
/// Who is at the location, as the perspective may know it. <see cref="Complete"/> is true only when nothing can be at the
/// location unseen: for the adjudicator. A side can never rule out hidden units (A12.3, p. 80), so its occupancy is
/// incomplete, and a sealed presence makes that plain.
/// </summary>
public sealed record CaseOccupancy(
    IReadOnlyList<UnitInstance> Units,
    IReadOnlyList<SealedPresence> Sealed,
    IReadOnlyList<EntityInstance> Entities,
    bool Complete,
    string? IncompleteReason)
{
    /// <summary>
    /// The one enemy unit at the location, or a reason there is no definitive answer (ASL-UNIT-061): it is never given
    /// from incomplete information.
    /// </summary>
    public (UnitInstance? Unit, string? Nondefinitive) SoleEnemy(string side)
    {
        ArgumentNullException.ThrowIfNull(side);
        if (!Complete)
        {
            return (null, IncompleteReason);
        }

        var enemies = Units.Where(unit => unit.Side != side).ToArray();
        return enemies.Length == 1 ? (enemies[0], null) : (null, $"{enemies.Length} enemy units are at the location.");
    }
}

/// <summary>
/// The one consistent snapshot a case reads (ASL-UNIT-060): the game stamp, source, and time; the phase; the attacker;
/// the target location and the attacker's previous location, typed through the map read API with their board version
/// and terrain facts; the occupants; and the ordered events of the current phase that concern them, reveals among them.
/// </summary>
public sealed record CaseSnapshot(
    CaseRequest Request,
    StateStamp Stamp,
    string Source,
    DateTimeOffset Time,
    int Turn,
    string Phase,
    string PhasingSide,
    CaseAttacker Attacker,
    LocationRead Target,
    LocationRead Previous,
    int? Distance,
    CaseOccupancy Occupancy,
    IReadOnlyList<GameEvent> Events,
    IReadOnlyList<GameEvent> Reveals);

public sealed record CaseReadResult(CaseReadStatus Status, string Code, string Reason, CaseSnapshot? Snapshot)
{
    public bool IsDefinitive => Status == CaseReadStatus.Definitive;
}

public enum GameLookup
{
    Found,
    NotFound,

    /// <summary>The game exists, but under another tenant.</summary>
    OtherTenant,
}

public sealed record GameSourceRead(GameLookup Lookup, GameHistory? History);

/// <summary>Where a reader finds a game's history. Until D2 is decided, the only source is synthetic fixtures.</summary>
public interface IGameSource
{
    public GameSourceRead Read(GameScope scope);
}

/// <summary>A game source over replayed histories, such as the synthetic fixtures.</summary>
public sealed class HistoryGameSource(IEnumerable<GameHistory> histories) : IGameSource
{
    private readonly List<GameHistory> histories = [.. histories];

    public GameSourceRead Read(GameScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var scoped = histories.Where(history => history.Events.Count > 0).ToArray();
        return scoped.FirstOrDefault(history => history.Events[0].Scope == scope) is { } found ? new GameSourceRead(GameLookup.Found, found)
            : scoped.Any(history => history.Events[0].Scope.Game == scope.Game) ? new GameSourceRead(GameLookup.OtherTenant, null)
            : new GameSourceRead(GameLookup.NotFound, null);
    }
}

/// <summary>
/// Answers <see cref="CaseRequest"/>s (ASL-UNIT-060, 061) from a game source, the map read API, and the catalogs. It
/// is read-only. Every failure is an explicit nondefinitive result with a code, never an exception or a partial answer.
/// </summary>
public sealed class CaseReader(IGameSource games, IBoardCatalog boards, Vocabulary.UnitVocabulary vocabulary, IReadOnlyList<UnitCatalog> catalogs)
{
    public CaseReadResult Read(CaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var source = games.Read(request.Scope);
        if (source.Lookup == GameLookup.OtherTenant)
        {
            return Fail(CaseReadStatus.Unauthorized, "CASE-001", $"{request.Scope.Game} does not belong to tenant {request.Scope.Tenant:D}.");
        }

        if (source.History is not { } history)
        {
            return Fail(CaseReadStatus.Unavailable, "CASE-002", $"There is no game {request.Scope}.");
        }

        if (history.Current is not { } state)
        {
            return Fail(CaseReadStatus.Inconsistent, "CASE-003", "The game's events do not replay into a state.");
        }

        if (!state.Perspectives.Contains(request.Perspective))
        {
            return Fail(CaseReadStatus.Unauthorized, "CASE-004", $"'{request.Perspective}' is not a perspective of this game.");
        }

        if (request.ExpectedRevision != state.Revision)
        {
            return Fail(CaseReadStatus.Stale, "CASE-005", $"The read expected revision {request.ExpectedRevision}; the game is at revision {state.Revision}.");
        }

        var view = GameView.Of(history, state.Revision, request.Perspective);
        if (view.Units.FirstOrDefault(unit => unit.Id == request.Attacker) is not { } attacker)
        {
            // An attacker the perspective cannot see is reported exactly like one that does not exist.
            return Fail(CaseReadStatus.Unavailable, "CASE-006", $"No unit '{request.Attacker}' is visible to {request.Perspective}.");
        }

        if (!view.Locations.TryGetValue(attacker.Id, out var from))
        {
            return Fail(CaseReadStatus.Inconsistent, "CASE-007", $"'{attacker.Id}' is not on the map.");
        }

        if (attacker.Definition is null || catalogs.FirstOrDefault(catalog => catalog.Identity == attacker.Definition.Catalog)?.Definition(attacker.Definition.Definition)
            is not { } definition)
        {
            return Fail(CaseReadStatus.Inconsistent, "CASE-008", $"The definition of '{attacker.Id}' is not in a loaded catalog.");
        }

        var target = Resolve(state, request.Location, out var targetFailure);
        var previous = Resolve(state, from.Location, out var previousFailure);
        if ((targetFailure ?? previousFailure) is { } failure)
        {
            return failure;
        }

        var occupancy = Occupancy(view, request.Location, attacker.Id);
        var concerned = new HashSet<string>(occupancy.Units.Select(unit => unit.Id).Concat(occupancy.Entities.Select(entity => entity.Id)), StringComparer.Ordinal)
        {
            attacker.Id,
        };
        var phaseStart = history.Events.Where(item => item.Revision <= state.Revision && item.Payload is GameStarted or PhaseChanged).Max(item => item.Revision);
        GameEvent[] events = [.. view.Events.Where(item => item.Revision >= phaseStart && Concerns(item, concerned, [request.Location, from.Location]))];
        GameEvent[] reveals = [.. events.Where(IsReveal)];
        var last = history.Events.Last(item => item.Revision == state.Revision);
        var distance = target!.Location.Board == previous!.Location.Board
            ? boards.TryGetBoard(target.Location.Board, target.BoardVersion).Board?.Distance(previous.Location.Hex, target.Location.Hex)
            : null;
        var snapshot = new CaseSnapshot(request, state.Stamp, last.Source, last.Time, state.Turn, state.Phase, state.PhasingSide,
            new CaseAttacker(attacker, definition, GameState.GoodOrder(attacker, vocabulary), attacker.MfSpent), target, previous, distance, occupancy, events, reveals);
        return target.IsDefinitive && previous.IsDefinitive
            ? new CaseReadResult(CaseReadStatus.Definitive, "CASE-000", "The case reads one consistent snapshot.", snapshot)
            : new CaseReadResult(CaseReadStatus.Unverified, "CASE-010",
                $"The terrain comes from a board that is not verified ({target.Status}, {previous.Status}); it cannot support a definitive conclusion.", snapshot);
    }

    /// <summary>
    /// Whether a later event makes a snapshot stale (ASL-UNIT-041): it moves, changes, or removes the attacker or an
    /// occupant, puts something at either location, or changes the phase. Only events the snapshot's perspective is
    /// entitled to count, so staleness itself reveals nothing.
    /// </summary>
    public static bool Affects(CaseSnapshot snapshot, GameEvent later)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(later);
        if (later.Revision <= snapshot.Stamp.Revision || !later.IsVisibleTo(snapshot.Request.Perspective))
        {
            return false;
        }

        var concerned = new HashSet<string>(snapshot.Occupancy.Units.Select(unit => unit.Id).Concat(snapshot.Occupancy.Entities.Select(entity => entity.Id)),
            StringComparer.Ordinal) { snapshot.Attacker.Unit.Id };
        return later.Payload is PhaseChanged or GameStarted || Concerns(later, concerned, [snapshot.Target.Location, snapshot.Previous.Location]);
    }

    /// <summary>Whether any event after the snapshot, in the history, makes it stale.</summary>
    public static bool IsStale(CaseSnapshot snapshot, GameHistory history)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(history);
        return history.Events.Any(item => Affects(snapshot, item));
    }

    private LocationRead? Resolve(GameState state, BoardLocation location, out CaseReadResult? failure)
    {
        failure = null;
        if (state.Map.Board(location.Board) is not { } placed)
        {
            failure = Fail(CaseReadStatus.Inconsistent, "CASE-009", $"{location.Board} is not a board in the map in play.");
            return null;
        }

        var board = boards.TryGetBoard(location.Board, placed.Version);
        if (board.Board is not { } handle)
        {
            failure = Fail(CaseReadStatus.Unavailable, "CASE-009",
                $"The game plays {location.Board} version {placed.Version}: {string.Join(" ", board.Diagnostics.Select(diagnostic => diagnostic.Message))}");
            return null;
        }

        var read = handle.Resolve(location);
        if (read.Read is null)
        {
            failure = Fail(CaseReadStatus.Inconsistent, "CASE-009", string.Join(" ", read.Diagnostics.Select(diagnostic => diagnostic.Message)));
        }

        return read.Read;
    }

    private static CaseOccupancy Occupancy(GameView view, BoardLocation location, string attacker)
    {
        bool At(string id) => view.Locations.TryGetValue(id, out var position) && position.Location == location;
        UnitInstance[] units = [.. view.Units.Where(unit => unit.Id != attacker && At(unit.Id))];
        SealedPresence[] sealedPresences = [.. view.Sealed.Where(presence => presence.Location == location)];
        EntityInstance[] entities = [.. view.Entities.Where(entity => At(entity.Id))];
        var reason = view.Perspective.IsAdjudicator ? null
            : sealedPresences.Length > 0 ? $"{sealedPresences.Length} concealed presence(s) at the location are unknown to {view.Perspective}."
            : $"{view.Perspective} cannot rule out hidden units (A12.3, p. 80).";
        return new CaseOccupancy(units, sealedPresences, entities, reason is null, reason);
    }

    private static bool Concerns(GameEvent item, HashSet<string> ids, BoardLocation[] locations)
    {
        bool AtLocation(Position? position) => position is MapPosition map && locations.Contains(map.Location);
        return item.Payload switch
        {
            InstanceCreated created => ids.Contains(created.Instance.Id) || AtLocation(created.Instance.Position),
            InstanceMoved moved => ids.Contains(moved.Id) || AtLocation(moved.Position),
            EquipmentTransferred transferred => ids.Contains(transferred.Id) || (transferred.Holding is { } holding && ids.Contains(holding.Holder))
                || AtLocation(transferred.Position),
            ConditionsChanged changed => ids.Contains(changed.Id),
            LineageRecorded lineage => lineage.Consumed.Any(ids.Contains) || lineage.Produced.Any(produced => ids.Contains(produced.Id) || AtLocation(produced.Position)),
            InstanceEliminated eliminated => ids.Contains(eliminated.Id),
            InstanceCaptured captured => ids.Contains(captured.Id) || ids.Contains(captured.Custodian),
            EntryAttempted attempted => ids.Contains(attempted.Id) || locations.Contains(attempted.Target),
            EntryForcedBack forced => ids.Contains(forced.Id) || locations.Contains(forced.ReturnedTo),
            _ => false,
        };
    }

    /// <summary>
    /// A reveal: concealment or hidden placement lost (A12.15, p. 78; A12.3, p. 80). A hidden unit placed beneath a "?"
    /// becomes concealed, which is not a reveal.
    /// </summary>
    private static bool IsReveal(GameEvent item) =>
        item.Payload is ConditionsChanged changed
        && changed.Conditions.Any(pair => pair.Key is Conditions.Concealed or Conditions.Hidden && pair.Value == ConditionState.False)
        && !changed.Conditions.Any(pair => pair.Key == Conditions.Concealed && pair.Value == ConditionState.True);

    private static CaseReadResult Fail(CaseReadStatus status, string code, string reason) => new(status, code, reason, null);
}
