using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 65 in live play, Passengers, Riders, and crews. Increment 2a: a crew that leaves its vehicle is its nationality's vehicle crew (D5.1;
/// ruling R65.19), a vehicle crew mans a Gun as a crew does (A1.123, D5.1; ruling R65.2), and a game recorded at catalog 1.17.0 fires (ruling R65.4).
/// Board 01's hex facts, fixed dice, and a stub LOS reader; every read of the state replays the game, so each record is checked by the projector.
/// </summary>
public sealed class BacklogPass65Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f565");
    private static readonly GameScope Scope = new(Tenant, "pass65");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private const string Firer = "bd01:B10:0";
    private const string Target = "bd01:B8:0";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass65-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass65Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog], fireLos: new StubLos());

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    /// <summary>A Gun manned by a unit of the given kind, its barrel to the given facing.</summary>
    private static Dictionary<string, object>[] Gun(string id, string definition, string crew, string crewKind, string crewDefinition, string at, string side, string facing) =>
    [
        Unit(crew, crewKind, crewDefinition, at, side),
        new()
        {
            ["id"] = id,
            ["kind"] = "asl:gun",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["holding"] = new
            {
                holder = crew,
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        },
    ];

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other at a date, under the named catalog, gone on to the first side's PFPh.</summary>
    private async Task Setup(string firstSide, string otherSide, int year, int month, string catalog, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass65Tests),
                ["catalog"] = catalog,
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["scenarioMonth"] = month,
                ["scenarioYear"] = year,
                ["sides"] = new object[]
                {
                    new { id = firstSide, nationality = firstSide, elr = 3, friendlyEdge = "left" },
                    new { id = otherSide, nationality = otherSide, elr = 3, friendlyEdge = "right" },
                },
            },
            placements,
        })));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Shoot(string gun, string? vehicle, string ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = Target, ["ammunition"] = ammunition };
        if (vehicle is not null)
        {
            arguments["targetVehicle"] = vehicle;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private static int HitColumn(OrdnanceFired shot) => shot.Resolution.GetProperty("hit").GetProperty("arithmetic").GetProperty("columnFp").GetInt32();

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AKilledGermanTanksSurvivingCrewIsTheGermanVehicleCrew()
    {
        // D5.6, D5.1 (ruling R65.19): the T-34's AP eliminates the PzKpfw IIIH (CS# 6); a Crew Survival DR of 4 places the German vehicle crew,
        // 1-2-7, beneath the wreck, with the id {attempt}-{vehicle}-crew.
        await Setup("russian", "german", 1942, 7, "asl-scenario-a1@1.18.0", Vehicle("ru-tank", "defender-tank", Firer, "russian", "north-east"),
            Vehicle("de-tank", "attacker-tank", Target, "german", "east"));
        var before = Revision;
        Committed(await Shoot("ru-tank", "de-tank", "ap", 4, 2, 3, 3, 2, 2));
        var kill = LastShot(before).Resolution.GetProperty("kill");
        Assert.True(Current.Unit("de-tank")!.Status == InstanceStatus.Wrecked, kill.GetRawText());
        var crew = Assert.Single(Current.Units, unit => unit.Side == "german" && unit.Kind == "asl:vehicle-crew");
        Assert.Equal(("german-vehicle-crew", Target, true), (crew.Definition!.Definition, Current.Location(crew.Id)!.Location.ToString(), crew.Id.EndsWith("-de-tank-crew", StringComparison.Ordinal)));
        var created = Since(before).Select(item => item.Payload).OfType<InstanceCreated>().Single(item => item.Instance.Id == crew.Id);
        Assert.Equal("asl:vehicle-crew", created.Instance.Kind);
        Assert.Equal(1, Catalog.Definition("german-vehicle-crew")!.Printed("front", "asl:firepower")!.Value!.Number);
        NoReplayErrors();

        // A game recorded before the pass placed the infantry crew; the definition is in its instance-created, so it replays as it did.
        GameEvent[] older = [.. store.Read(Scope)!.Events.Select(item => item.Payload is InstanceCreated { Instance.Kind: "asl:vehicle-crew" } placed
            ? item with { Payload = new InstanceCreated(placed.Instance with { Kind = "asl:crew", Definition = "attacker-crew" }) }
            : item)];
        var replayed = Planner().Replay(older);
        Assert.False(replayed.HasErrors);
        Assert.Equal(("asl:crew", "attacker-crew"), (replayed.Current!.Unit(crew.Id)!.Kind, replayed.Current.Unit(crew.Id)!.Definition!.Definition));
    }

    [Fact]
    public async Task AVehicleCrewMansAGunAsACrewDoes()
    {
        // A1.123, D5.1 (ruling R65.2): a vehicle crew has a crew's capabilities, so it mans the 6-Pounder as qualified Infantry: the shot is the one
        // pass 60's British crew fires, its AP at the squad on the 2 column by HE Equivalency (C8.31).
        await Setup("british", "german", 1942, 6, "asl-scenario-a1@1.18.0",
            [.. Gun("uk-gun", "british-at-gun", "uk-crew", "asl:vehicle-crew", "british-vehicle-crew", Firer, "british", "north-east"),
                Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        Committed(await Shoot("uk-gun", null, "ap", 4, 3, 6, 6));
        var shot = LastShot(before);
        Assert.Equal(2, HitColumn(shot));
        Assert.False(shot.Facts.TryGetProperty("nonQualified", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task AGameRecordedAtCatalog117Fires()
    {
        // Ruling R65.4: a game set up at 1.17.0 reads the loaded 1.18.0 catalog, and the packages serve an older minor version of it.
        await Setup("british", "german", 1942, 6, "asl-scenario-a1@1.17.0",
            [.. Gun("uk-gun", "british-at-gun", "uk-crew", "asl:crew", "british-crew", Firer, "british", "north-east"),
                Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        Assert.Equal("asl-scenario-a1@1.17.0", Assert.IsType<GameStarted>(store.Read(Scope)!.Events[0].Payload).Catalog);
        Assert.True(LiveFire.ServesCatalog(Current));
        Committed(await Shoot("uk-gun", null, "ap", 4, 3, 6, 6));
        NoReplayErrors();
    }
}
