using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 60 in live play, Armored combat III. Increment 2a: HE Equivalency (C8.31; rulings R60.4 to R60.6): a 6-Pounder's AP at a squad, a PSK and
/// a PF at a squad in a building, and an ATR at a Gun's crew. A board 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader.
/// Every read of the state replays the game, so each record written here is also checked by the projector.
/// </summary>
public sealed class BacklogPass60Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f560");
    private static readonly GameScope Scope = new(Tenant, "pass60");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private const string Firer = "bd01:B10:0";
    private const string Target = "bd01:B8:0";
    private const string Building = "Stone Building, 2 Level";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass60-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass60Board board = new();

    public BacklogPass60Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the buildings a test draws on it (as pass 55's test board).</summary>
    private sealed class Pass60Board
    {
        public Dictionary<string, string> Terrain { get; } = new(StringComparer.Ordinal);

        private static TerrainType Type(string name) => new()
        {
            Code = (byte)(Math.Abs(name.GetHashCode(StringComparison.Ordinal)) % 250 + 1),
            Name = name,
            Category = name == "Woods" ? LosCategory.Woods : name.Contains("Building", StringComparison.Ordinal) ? LosCategory.Building : LosCategory.Open,
        };

        public BoardHandle Handle()
        {
            var geometry = BoardGeometry.StandardGeomorphic;
            var hexes = new List<HexFacts>();
            foreach (var text in Board01Fixture.Hexes())
            {
                var name = HexName.Parse(text);
                Assert.True(geometry.TryGetIndex(name, out var index));
                var type = Type(Terrain.GetValueOrDefault(text) ?? "Open Ground");
                var center = new LocationFacts(0, type, null);
                HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
                hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
            }

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 60 test board", new HexFactSet(geometry, "pass60", hexes));
        }
    }

    // Clear LOS at the board's true range.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([board.Handle()]), Vocabulary, [Catalog], fireLos: los);

    private static DiceRoller Once(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Dequeue() - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

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

    /// <summary>A Gun manned by its crew, its barrel to the given facing.</summary>
    private static Dictionary<string, object>[] Gun(string id, string definition, string crew, string crewDefinition, string at, string side, string facing) =>
    [
        Unit(crew, "asl:crew", crewDefinition, at, side),
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

    /// <summary>A LATW possessed by its holder.</summary>
    private static Dictionary<string, object> Latw(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:latw",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game of the first side against the other at a date, gone on to the first side's PFPh.</summary>
    private async Task Setup(string firstSide, string otherSide, int year, int month, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = nameof(BacklogPass60Tests),
                ["catalog"] = "asl-scenario-a1@1.17.0",
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

    /// <summary>A shot at the target Location, naming an ammunition or none; the dice are every roll it asks for, in order.</summary>
    private Task<PlayResult> Shoot(string gun, string? ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = Target };
        if (ammunition is not null)
        {
            arguments["ammunition"] = ammunition;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private static int Equivalency(OrdnanceFired shot) => shot.Resolution.GetProperty("heEquivalency").GetInt32();

    /// <summary>The IFT column the shot's hit attacked on, its Critical Hit's halved.</summary>
    private static int HitColumn(OrdnanceFired shot) => shot.Resolution.TryGetProperty("hit", out var hit)
        ? hit.GetProperty("arithmetic").GetProperty("columnFp").GetInt32()
        : shot.Resolution.GetProperty("criticalHit").GetProperty("arithmetic").GetProperty("columnFp").GetInt32() / 2;

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task ASixPoundersApAtASquadAttacksByItsHeEquivalency()
    {
        // British Ordnance Note 6: no HE before February 1943; C8.31: its AP fires at the squad on the 2 column instead (57mm, at least 37mm).
        await Setup("british", "german", 1942, 6, [.. Gun("uk-gun", "british-at-gun", "uk-crew", "british-crew", Firer, "british", "north-east"),
            Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        var result = await Shoot("uk-gun", "ap", 4, 3, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal(("ap", 2, 2), (shot.Facts.GetProperty("ammunition").GetString(), Equivalency(shot), HitColumn(shot)));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains($"AP at {Target} (2 FP by HE Equivalency, C8.31)", StringComparison.Ordinal));
        Assert.False(shot.Resolution.TryGetProperty("ammunitionUse", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task APskAtASquadInABuildingAttacksEveryUnitAtTwelveFp()
    {
        // C13.48, C8.31: the PSK's IFT effect is 12, fired at Infantry in a building (its +3 is Case Q on the To Hit DR, 9 at two hexes).
        board.Terrain["B8"] = Building;
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Latw("de-psk", "attacker-psk", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"), Unit("r2", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        var result = await Shoot("de-psk", null, 2, 2, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal((12, 12), (Equivalency(shot), HitColumn(shot)));
        Assert.Equal(["r1", "r2"], shot.Resolution.GetProperty("hit").GetProperty("effects").EnumerateArray().Select(item => item.GetProperty("unitId").GetString()));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains($"HEAT at {Target} (12 FP by HE Equivalency, C8.31)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task APskAtASquadInTheOpenIsRefused()
    {
        // C8.31: HEAT goes only at a vehicle or Gun, or at Infantry with a wall, building, rubble, or pillbox TEM.
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Latw("de-psk", "attacker-psk", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", Target, "russian"));
        var result = await Shoot("de-psk", null, 2, 2);
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains("asl.a1.ordnance.heat-target-outside", StringComparison.Ordinal) && reason.Contains("C8.31", StringComparison.Ordinal));
    }

    [Fact]
    public async Task APfAtASquadInABuildingTakesThePlusOneAndSixteenFp()
    {
        // C13.31: +1 to the PF Check at other than an AFV (a dr of 2 is a final 3: a shot); C13.33: 10 less two a hex; C8.31: 16 FP.
        board.Terrain["B8"] = Building;
        await Setup("german", "russian", 1944, 7, Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Unit("r1", "asl:squad", "defender-squad", Target, "russian"));
        var before = Revision;
        Committed(await Shoot("g1:pf", null, 2, 1, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal((16, 16), (Equivalency(shot), HitColumn(shot)));
        Assert.Contains(shot.Resolution.GetProperty("panzerfaustCheck").GetProperty("drm").EnumerateArray(), item => item.GetProperty("name").GetString() == "not-afv");
        Assert.Equal(1, Planner().Replay(store.Read(Scope)!.Events).Current!.PanzerfaustShots["german"]);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAtrAtAGunAttacksOnlyItsCrewAtOneFp()
    {
        // C13.23, C11.52 (ruling R60.6): the Russian squad's 14mm ATR at the leIG 18 in B8: 1 FP on its crew alone, the German squad beside it
        // untouched. The black 8 at two hexes, Case L -1, the small Gun's +1 and its Emplacement's +2: a DR of 4 hits; an IFT DR of 12 does nothing.
        await Setup("russian", "german", 1943, 7, [Unit("r1", "asl:squad", "defender-squad", Firer, "russian"), Latw("ru-atr", "defender-atr", "r1", "russian"),
            .. Gun("de-gun", "attacker-inf-gun", "de-crew", "attacker-crew", Target, "german", "south-west"), Unit("de-s", "asl:squad", "attacker-squad", Target, "german")]);
        var before = Revision;
        Committed(await Shoot("ru-atr", null, 2, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal((1, 1), (Equivalency(shot), HitColumn(shot)));
        Assert.Equal(["de-crew"], shot.Resolution.GetProperty("hit").GetProperty("effects").EnumerateArray().Select(item => item.GetProperty("unitId").GetString()));
        Assert.Equal("de-gun", shot.Facts.GetProperty("hit").GetProperty("gunTarget").GetProperty("gunId").GetString());
        NoReplayErrors();
    }
}
