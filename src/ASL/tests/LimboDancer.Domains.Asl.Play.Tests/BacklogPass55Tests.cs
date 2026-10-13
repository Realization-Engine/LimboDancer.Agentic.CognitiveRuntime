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
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 55 in live play, Armored combat II. Task 55.4: a Gun and a tank's MGs fire at a target at another level when the range is at least the
/// elevation difference (C2.6), and the target above them claims Height Advantage (B10.31, D4.22): Case Q on the To Hit DR, a TEM on the IFT DR.
/// A board 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader. Every read of the state replays the game, so each record
/// written here is also checked by the projector.
/// </summary>
public sealed class BacklogPass55Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f555");
    private static readonly GameScope Scope = new(Tenant, "pass55");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] TankFirer = ["de-tank"];
    private static readonly string[] Cmg = ["cmg"];
    private const string Firer = "bd01:B10:0";
    private const string Target = "bd01:B8:0";
    private const string Building = "Stone Building, 2 Level";

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass55-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private readonly Pass55Board board = new();

    public BacklogPass55Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0, with the hills, woods, and building levels a test draws on it (as pass 10's test board).</summary>
    private sealed class Pass55Board
    {
        public Dictionary<string, string> Terrain { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Base { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Stairs { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string[]> Upper { get; } = new(StringComparer.Ordinal);

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
                LocationFacts[] locations = [center, .. (Upper.GetValueOrDefault(text) ?? []).Select((level, at) => new LocationFacts(at + 1, Type(level), null))];
                HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
                hexes.Add(new HexFacts(name, index, Base.GetValueOrDefault(text), Stairs.Contains(text), center, locations, hexsides, null, CenterTerrainSource.CenterSample));
            }

            return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 55 test board", new HexFactSet(geometry, "pass55", hexes));
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

    private static Dictionary<string, object> Squad(string at) => Unit("r1", "asl:squad", "defender-squad", at, "russian");

    /// <summary>The German 7.5cm leIG 18 and its crew, its barrel to the north-east: B8 lies in its CA from B10.</summary>
    private static Dictionary<string, object>[] Gun(string at = Firer) =>
    [
        Unit("de-crew", "asl:crew", "attacker-crew", at, "german"),
        new()
        {
            ["id"] = "de-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "attacker-inf-gun",
            ["side"] = "german",
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = "north-east" },
            ["holding"] = new
            {
                holder = "de-crew",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        },
    ];

    /// <summary>The PzKpfw IIIH in B10, its VCA and TCA to the north-east.</summary>
    private static Dictionary<string, object> Tank() => new()
    {
        ["id"] = "de-tank",
        ["kind"] = "asl:vehicle",
        ["definition"] = "attacker-tank",
        ["side"] = "german",
        ["position"] = new Dictionary<string, string> { ["at"] = Firer, ["facing"] = "north-east" },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>Sets the game up and goes on to the German PFPh.</summary>
    private async Task Setup(params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 55",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
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

    private Task<PlayResult> FireGun(string target = Target) => Do(GameActions.FireOrdnance, Once(5, 6), new
    {
        gunId = "de-gun",
        target,
    });

    private Task<PlayResult> FireCmg(string target = Target) => Do(GameActions.Fire, Once(5, 6), new
    {
        firers = TankFirer,
        target,
        weapons = new Dictionary<string, string[]> { ["de-tank"] = Cmg },
    });

    private OrdnanceFired LastShot(long before) => Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single();

    private FireResolved LastFire(long before) => Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, params string[] words)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        foreach (var word in words)
        {
            Assert.Contains(result.Reasons, reason => reason.Contains(word, StringComparison.Ordinal));
        }
    }

    private void NoReplayErrors() => Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static decimal DrmOf(JsonElement drm, string prefix) => drm.EnumerateArray()
        .Where(item => item.GetProperty("name").GetString()!.StartsWith(prefix, StringComparison.Ordinal)).Sum(item => item.GetProperty("value").GetDecimal());

    private static JsonElement ToHitDrm(OrdnanceFired shot) => shot.Resolution.GetProperty("toHit").GetProperty("drm");

    private static JsonElement IftDrm(FireResolved fire) => fire.Resolution.GetProperty("arithmetic").GetProperty("drm");

    [Fact]
    public async Task AGunFiringUphillGivesItsTargetHeightAdvantageAsCaseQ()
    {
        // C2.6: B8 is a level 1 hill two hexes from the Gun, so the range is at least the elevation difference. B10.31, C6.8: +1, Case Q.
        board.Base["B8"] = 1;
        await Setup([.. Gun(), Squad(Target)]);
        var before = Revision;
        Committed(await FireGun());
        var shot = LastShot(before);
        Assert.Equal(1m, DrmOf(ToHitDrm(shot), "case-q:height-advantage"));
        Assert.True(shot.Facts.GetProperty("heightAdvantage").GetBoolean());
        Assert.True(shot.Facts.GetProperty("elevationAllowed").GetBoolean());
        Assert.False(shot.Facts.GetProperty("hit").GetProperty("sameLevel").GetBoolean());
        NoReplayErrors();
    }

    [Fact]
    public async Task ATargetWithAnotherTemTakesNoHeightAdvantage()
    {
        // B10.31: "provided that unit is not eligible to receive any other positive TEM": the woods on the hill give their +1 and no more.
        board.Base["B8"] = 1;
        board.Terrain["B8"] = "Woods";
        await Setup([.. Gun(), Squad(Target)]);
        var before = Revision;
        Committed(await FireGun());
        var shot = LastShot(before);
        Assert.Equal(1m, DrmOf(ToHitDrm(shot), "case-q:woods"));
        Assert.Equal(0m, DrmOf(ToHitDrm(shot), "case-q:height-advantage"));
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunFiresDownhillWithNoHeightAdvantageAndAtTheSameLevelAsBefore()
    {
        // The Gun on a level 2 hill fires down at range 2; its target below it has no Height Advantage.
        board.Base["B10"] = 2;
        await Setup([.. Gun(), Squad(Target)]);
        var before = Revision;
        Committed(await FireGun());
        var shot = LastShot(before);
        Assert.Equal(0m, DrmOf(ToHitDrm(shot), "case-q:"));
        Assert.False(shot.Facts.TryGetProperty("heightAdvantage", out _));
        NoReplayErrors();
    }

    [Fact]
    public async Task AShotAtARangeLessThanTheElevationDifferenceIsRefused()
    {
        // C2.6: "only if the range is >= the elevation difference between them": three levels up at two hexes, and three levels down.
        board.Base["B8"] = 3;
        await Setup([.. Gun(), Squad(Target)]);
        Refused(await FireGun(), "asl.a1.ordnance.out-of-range", "C2.6");
        Assert.Contains("C2.6", Planner().GunTargetStatus(Planner().Replay(store.Read(Scope)!.Events).Current!, "de-gun", BoardLocation.Parse(Target)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AGunFiresAtNoOtherLevelOfItsOwnHex()
    {
        // C2.6: "A Gun in a building hex may not fire at a lower-level target in its own hex, and may fire at an higher-level target in its hex
        // only if it is an AA Gun".
        board.Terrain["B10"] = Building;
        board.Upper["B10"] = [Building];
        board.Stairs.Add("B10");
        await Setup([.. Gun(), Squad("bd01:B10:1")]);
        Refused(await FireGun("bd01:B10:1"), "play.ordnance-own-hex", "another level of its own hex");
    }

    [Fact]
    public async Task ATanksCmgFiresUpAHillAndItsTargetTakesHeightAdvantage()
    {
        // C2.6, B10.31: the CMG at the squad on the level 1 hill two hexes away: +1 TEM on the IFT DR.
        board.Base["B8"] = 1;
        await Setup(Tank(), Squad(Target));
        var before = Revision;
        Committed(await FireCmg());
        var fire = LastFire(before);
        Assert.Equal(1m, DrmOf(IftDrm(fire), "height-advantage"));
        Assert.Equal(1, fire.Facts.GetProperty("targetLevelAbove").GetInt32());
        Assert.True(fire.Facts.GetProperty("heightAdvantage").GetBoolean());
        NoReplayErrors();
    }

    [Fact]
    public async Task ATanksCmgFiresAtAnUpperBuildingLevel()
    {
        // The squad on level 1 of the stone building in B8 has the building's +3 and so no Height Advantage.
        board.Terrain["B8"] = Building;
        board.Upper["B8"] = [Building];
        board.Stairs.Add("B8");
        await Setup(Tank(), Squad("bd01:B8:1"));
        var before = Revision;
        Committed(await FireCmg("bd01:B8:1"));
        var fire = LastFire(before);
        Assert.Equal(3m, DrmOf(IftDrm(fire), "tem:"));
        Assert.Equal(0m, DrmOf(IftDrm(fire), "height-advantage"));
        Assert.Equal(1, fire.Facts.GetProperty("targetLevelAbove").GetInt32());
        NoReplayErrors();
    }

    [Fact]
    public async Task ATanksMgIsHeldToTheRangeOfTheElevationDifference()
    {
        // C2.6: "These restrictions also apply to non-AA vehicular MG".
        board.Base["B8"] = 3;
        await Setup(Tank(), Squad(Target));
        Refused(await FireCmg(), "asl.a1.fire.elevation-outside", "C2.6");
    }
}
