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
/// Pass 55 in live play, Armored combat II. Task 55.4: a Gun and a tank's MGs fire at a target at another level when the range is at least the
/// elevation difference (C2.6), and the target above them claims Height Advantage (B10.31, D4.22): Case Q on the To Hit DR, a TEM on the IFT DR.
/// A board 01 grid whose terrain each test draws, fixed dice, and a stub LOS reader. Every read of the state replays the game, so each record
/// written here is also checked by the projector.
/// </summary>
public sealed class BacklogPass55Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f555");
    private static readonly string[] VoidCauses = ["shocked", "eliminated", "other"];
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

        /// <summary>The hexes no LOS reaches or leaves, such as "E5" (the Motion attempt's tests).</summary>
        public HashSet<string> Blind { get; } = new(StringComparer.Ordinal);

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(Blind.Contains(from.Hex.ToString()) || Blind.Contains(target.Hex.ToString()) ? LosStatus.Blocked : LosStatus.Clear, false,
                Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
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
    private Task Setup(params Dictionary<string, object>[] placements) => SetupFirst("german", placements);

    /// <summary>Sets the game up with the side that moves first and goes on to its PFPh.</summary>
    private async Task SetupFirst(string firstSide, params Dictionary<string, object>[] placements)
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
                ["firstSide"] = firstSide,
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
        // The table player's review: the refusal is the elevation's own, with the levels and the range, and not the Gun's range.
        var refused = await FireGun();
        Refused(refused, "play.ordnance-elevation", "de-gun and its target are 3 levels apart at range 2", "C2.6");
        Assert.DoesNotContain(refused.Reasons, reason => reason.Contains("out-of-range", StringComparison.Ordinal));
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

    // Task 55.1, the rest (increment 2c): the British 6-Pounder's APDS (D6 from June 1944) and its HE (HE8 in 1944), each by its Depletion Number
    // (C8.9; ruling R55.7), HE named or refused (C2.21, C8.8), and an Elite force's one more (C8.2; ruling R55.6).

    private static readonly string[] EliteBritish = ["elite-ammunition:british"];

    /// <summary>A game of the British against the Germans at a date, with the game's special rules, gone on to the British PFPh.</summary>
    private async Task SetupBritish(int year, int month, string[] specialRules, params Dictionary<string, object>[] placements)
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
                ["firstSide"] = "british",
                ["scenarioMonth"] = month,
                ["scenarioYear"] = year,
                ["specialRules"] = specialRules,
                ["sides"] = new object[]
                {
                    new { id = "british", nationality = "british", elr = 3, friendlyEdge = "left" },
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "right" },
                },
            },
            placements,
        })));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    /// <summary>The OQF 6-Pounder and its crew in B10, its barrel to the north-east: B8 lies in its CA at two hexes.</summary>
    private static Dictionary<string, object>[] SixPounder() =>
    [
        Unit("uk-crew", "asl:crew", "british-crew", Firer, "british"),
        new()
        {
            ["id"] = "uk-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "british-at-gun",
            ["side"] = "british",
            ["position"] = new Dictionary<string, string> { ["at"] = Firer, ["facing"] = "north-east" },
            ["holding"] = new
            {
                holder = "uk-crew",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        },
    ];

    /// <summary>The PzKpfw IIIH in B8.</summary>
    private static Dictionary<string, object> GermanTank() => new()
    {
        ["id"] = "de-tank",
        ["kind"] = "asl:vehicle",
        ["definition"] = "attacker-tank",
        ["side"] = "german",
        ["position"] = new Dictionary<string, string> { ["at"] = Target, ["facing"] = "north-east" },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static Dictionary<string, object> GermanSquad() => Unit("de-s", "asl:squad", "attacker-squad", Target, "german");

    /// <summary>The 6-Pounder at the tank with the named ammunition; the dice are the To Hit DR and then whatever the hit asks for.</summary>
    private Task<PlayResult> FireAtTank(string ammunition, params int[] dice) => Do(GameActions.FireOrdnance, Once(dice), new
    {
        gunId = "uk-gun",
        target = Target,
        targetVehicle = "de-tank",
        ammunition,
    });

    /// <summary>The 6-Pounder at the squad's Location, naming an ammunition or none.</summary>
    private Task<PlayResult> FireAtSquad(string? ammunition, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = "uk-gun", ["target"] = Target };
        if (ammunition is not null)
        {
            arguments["ammunition"] = ammunition;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static string? Use(OrdnanceFired shot) => shot.Resolution.TryGetProperty("ammunitionUse", out var use) ? use.GetString() : null;

    private bool RanOut(string ammunition) => Current.DepletedAmmunition.Any(item => item.Gun == "uk-gun" && item.Ammunition == ammunition);

    [Fact]
    public async Task ApdsBelowItsDepletionNumberIsUsed()
    {
        // C8.9: the 6-Pounder's D6 in June 1944; an Original 3 uses the round. C7.32: D57L is 18; the To Kill DR of 12 is a dud (C7.35).
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanTank()]);
        var before = Revision;
        var result = await FireAtTank("apds", 2, 1, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal("used", Use(shot));
        Assert.Equal(("apds", 18), (shot.Resolution.GetProperty("kill").GetProperty("ammunition").GetString(), shot.Resolution.GetProperty("kill").GetProperty("basicTk").GetInt32()));
        Assert.Equal("apds", shot.Facts.GetProperty("ammunition").GetString());
        Assert.False(shot.Facts.TryGetProperty("elite", out _));
        Assert.False(RanOut("apds"));

        // The proposal says the number and what a DR above it means.
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("APDS by its Depletion Number 6: on an Original To Hit DR above it the Gun had none", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task ApdsAtItsDepletionNumberIsItsLastRound()
    {
        // C8.9: an Original 6 "uses that ammunition ... but the firer runs out of that Special Ammunition in the process".
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanTank()]);
        var before = Revision;
        Committed(await FireAtTank("apds", 4, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal("depleted", Use(shot));
        Assert.True(shot.Resolution.TryGetProperty("kill", out _));
        Assert.True(RanOut("apds"));
        Refused(await FireAtTank("apds", 2, 1), "asl.a1.ordnance.ammunition-depleted", "C8.9");
        NoReplayErrors();
    }

    [Fact]
    public async Task ApdsAboveItsDepletionNumberWasNeverThereAndTheGunHasNotFired()
    {
        // C8.9: an Original 7: "the firer had no such ammunition and is considered not to have fired yet for any purpose": no To Kill DR, no fire
        // counter, and the Gun fires its AP at once; "The Gun may not use that special ammunition again in that scenario."
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanTank()]);
        var before = Revision;
        Committed(await FireAtTank("apds", 4, 3));
        var shot = LastShot(before);
        Assert.Equal("none", Use(shot));
        Assert.False(shot.Resolution.TryGetProperty("kill", out _));
        Assert.True(shot.RateOfFireKept);
        Assert.DoesNotContain(Since(before), item => item.Payload is ConditionsChanged);
        Assert.Empty(Current.OrdnanceShots);
        Assert.True(RanOut("apds"));
        Refused(await FireAtTank("apds", 2, 1), "asl.a1.ordnance.ammunition-depleted");

        // The colored dr of 4 is above its ROF of 3, yet the Gun fires: AP, with no Depletion Number, and a To Kill DR that is a dud.
        var again = Revision;
        Committed(await FireAtTank("ap", 5, 6, 6, 6));
        Assert.Null(Use(LastShot(again)));
        Assert.Single(Current.OrdnanceShots);
        NoReplayErrors();
    }

    [Fact]
    public async Task ApdsIsNotThereBeforeJune1944()
    {
        // British Ordnance Note 6 (p. 426): APDS from June 1944 (C8.11).
        await SetupBritish(1944, 5, [], [.. SixPounder(), GermanTank()]);
        Refused(await FireAtTank("apds", 2, 1), "asl.a1.ordnance.ammunition-unavailable", "C8.11");
    }

    [Fact]
    public async Task AnEliteForcesSevenIsItsLastApdsRound()
    {
        // C8.2 (ruling R55.6): the game's special rule elite-ammunition:british makes the D6 a D7; the shot records it.
        await SetupBritish(1944, 6, EliteBritish, [.. SixPounder(), GermanTank()]);
        var before = Revision;
        var result = await FireAtTank("apds", 4, 3, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal("depleted", Use(shot));
        Assert.True(shot.Facts.GetProperty("elite").GetBoolean());
        Assert.True(RanOut("apds"));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("Depletion Number 7 (6, and 1 for Elite, C8.2)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task TheSixPoundersHeIsFiredAtInfantryWhenItIsNamed()
    {
        // C8.8, C8.9: HE8 in 1944; an Original 6 uses a round, and the hit is resolved on the IFT.
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanSquad()]);
        var before = Revision;
        var result = await FireAtSquad("he", 3, 3, 6, 6);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal("used", Use(shot));
        Assert.Equal("he", shot.Facts.GetProperty("ammunition").GetString());
        Assert.True(shot.Resolution.TryGetProperty("hit", out _) || shot.Resolution.TryGetProperty("criticalHit", out _));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("HE by its Depletion Number 8", StringComparison.Ordinal));
        Assert.False(RanOut("he"));
        NoReplayErrors();
    }

    [Fact]
    public async Task TheSixPoundersHeIsNeverFiredUndeclared()
    {
        // C2.21: HE is assumed against an unarmored target only when it "is not Special Ammunition (i.e., has no Depletion #; 8.8)".
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanSquad()]);
        Refused(await FireAtSquad(null, 3, 3), "asl.a1.ordnance.he-not-named", "name HE for the shot (C2.21, C8.8)", "C8.31");

        // The referee's review: AP named at Infantry is no HE that was not named; since pass 60 it fires by its HE Equivalency (C8.31).
    }

    [Fact]
    public async Task TheSixPounderHasNoHeIn1942()
    {
        // British Ordnance Note 6 (p. 426): HE from February 1943. Before it the Gun fires nothing at Infantry: AP there is C8.31's.
        await SetupBritish(1942, 6, [], [.. SixPounder(), GermanSquad()]);
        Refused(await FireAtSquad("he", 3, 3), "asl.a1.ordnance.he-unavailable", "Special Ammunition (C8.8) and it has none now", "HE Equivalency instead (C8.31)");
        Refused(await FireAtSquad(null, 3, 3), "asl.a1.ordnance.he-not-named");
    }

    [Fact]
    public async Task HeAboveItsDepletionNumberAsksForNoIftDrAndIsGone()
    {
        // C8.9 on the Infantry Target Type: an Original 9 is above HE8: two dice only, no fire counter, and no HE again.
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanSquad()]);
        var before = Revision;
        Committed(await FireAtSquad("he", 5, 4));
        var shot = LastShot(before);
        Assert.Equal("none", Use(shot));
        Assert.False(shot.Resolution.TryGetProperty("hit", out _));
        Assert.Single(Since(before), item => item.Payload is DiceRolled);
        Assert.DoesNotContain(Since(before), item => item.Payload is ConditionsChanged);
        Assert.True(RanOut("he"));
        Refused(await FireAtSquad("he", 3, 3), "asl.a1.ordnance.he-unavailable");
        NoReplayErrors();
    }

    // Task 55.2 (increment 2d): Canister (C8.4 to C8.42, p. 178; rulings R55.8 to R55.10). The American M3A1 37mm AT Gun (C7 from August 1942,
    // ROF 2, B12, NT) in F5: facing west, aim 4 of its CA is the vertex of D4, D5, and C5, with E5 the adjacent hex on the LOS.

    private const string GunHex = "bd01:F5:0";
    private static readonly string[] Aim4 = ["bd01:D4:0", "bd01:D5:0", "bd01:C5:0", "bd01:E5:0"];

    /// <summary>A game of the Americans against the Germans at a date, gone on to the American PFPh.</summary>
    private async Task SetupAmerican(int year, int month, params Dictionary<string, object>[] placements)
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
                ["firstSide"] = "american",
                ["scenarioMonth"] = month,
                ["scenarioYear"] = year,
                ["sides"] = new object[]
                {
                    new { id = "american", nationality = "american", elr = 3, friendlyEdge = "left" },
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "right" },
                },
            },
            placements,
        })));
        Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }));
    }

    /// <summary>The M3A1 37mm AT Gun and its crew in F5 with its barrel to the given hexspine.</summary>
    private static Dictionary<string, object>[] AmericanGun(string facing = "west") =>
    [
        Unit("us-crew", "asl:crew", "american-crew", GunHex, "american"),
        new()
        {
            ["id"] = "us-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "american-at-gun",
            ["side"] = "american",
            ["position"] = new Dictionary<string, string> { ["at"] = GunHex, ["facing"] = facing },
            ["holding"] = new
            {
                holder = "us-crew",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        },
    ];

    /// <summary>German squads in two hexes of aim 4's cluster and in the adjacent hex on its LOS; C5 holds none.</summary>
    private static Dictionary<string, object>[] Squads() =>
    [
        Unit("de-s1", "asl:squad", "attacker-squad", Aim4[0], "german"),
        Unit("de-s2", "asl:squad", "attacker-squad", Aim4[1], "german"),
        Unit("de-s3", "asl:squad", "attacker-squad", Aim4[3], "german"),
    ];

    /// <summary>The Gun's Canister at an aim, turning first when a facing is given; the dice are the IFT DR and then whatever the attacks ask for.</summary>
    private Task<PlayResult> FireCanister(int aim, string? aimFacing, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = "us-gun", ["ammunition"] = "canister", ["aim"] = aim };
        if (aimFacing is not null)
        {
            arguments["aimFacing"] = aimFacing;
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    private FireResolved[] Fires(long before) => [.. Since(before).Select(item => item.Payload).OfType<FireResolved>()];

    private static decimal Fp(FireResolved fire) => fire.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal();

    private static UnitFacing? GunFacing(GameState state) => (state.Find("us-gun") as EquipmentInstance)?.Position is MapPosition { Facing: { } facing } ? facing : null;

    [Fact]
    public async Task TheFiveAimsOfAGunAreHexesOfTheMap()
    {
        // Ruling R55.8, in the board's own geometry: from F5 facing west the CA's adjacent hexes are E6 (left) and E5 (right).
        await SetupAmerican(1943, 7, [.. AmericanGun()]);
        var planner = Planner();
        var state = Current;
        string[] Names(IEnumerable<BoardLocation> hexes) => [.. hexes.Select(at => at.Hex.ToString())];
        var west = planner.CanisterAims(state, "us-gun");
        Assert.Equal([1, 2, 3, 4, 5], west.Select(aim => aim.Aim));
        Assert.Equal([["D6", "C7", "C6"], ["D6", "D5", "C6"], ["D5", "C6", "C5"], ["D4", "D5", "C5"], ["D4", "C4", "C5"]], west.Select(aim => Names(aim.Cluster)));
        Assert.Equal([["E6"], ["E6"], ["E6", "E5"], ["E5"], ["E5"]], west.Select(aim => Names(aim.Adjacent)));
        Assert.Equal([false, false, true, false, false], west.Select(aim => aim.Quartered));

        // The aims of the facing it would turn to; a hex off the map is left out, and an aim with none on it too.
        var east = planner.CanisterAims(state, "us-gun", UnitFacing.East);
        Assert.Equal(["H6", "H5", "I6"], Names(east[3].Cluster));
        Assert.Equal(["G6"], Names(east[3].Adjacent));
        Assert.Empty(planner.CanisterAims(state, "no-gun"));
    }

    [Fact]
    public async Task TwoSmcCanisterUsesTheSameParticipantsAndRecordsBothFirers()
    {
        var pair = AmericanGun();
        pair[0] = Unit("us-crew", "asl:leader", "american-leader-8-0", GunHex, "american");
        await SetupAmerican(1943, 7, [.. pair, Unit("us-second", "asl:leader", "american-leader-8-0", GunHex, "american"), .. Squads()]);
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(1, 5, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2),
            new
            {
                gunId = "us-gun",
                ammunition = "canister",
                aim = 4,
                secondFirer = "us-second"
            }));
        var shot = LastShot(before);
        Assert.Equal("us-second", shot.Facts.GetProperty("participation").GetProperty("second").GetProperty("crew").GetProperty("unitId").GetString());
        Assert.Equal(1, shot.Facts.GetProperty("useMultiple").GetInt32());
        Assert.Contains(Current.GunCrewsFired, id => id == "us-second");
        Assert.Contains(Current.PhaseFirers, item => item.Unit == "us-second" && item.Weapon == "us-gun");
        NoReplayErrors();
    }

    [Fact]
    public async Task CanisterAttacksEveryLocationOfItsAimOnOneDr()
    {
        // C8.4: no To Hit DR; one IFT DR of 6, below the Depletion Number 7, attacks the three hexes of the cluster at 12 FP and the adjacent hex
        // on the LOS at 6 (C8.41). C5, where the Gun's side sees nothing, is attacked as a hidden unit's Location would be. Each squad passes its MC.
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads()]);
        var before = Revision;
        var result = await FireCanister(4, null, 2, 4, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal("used", Use(shot));
        Assert.Equal(JsonValueKind.Null, shot.Resolution.GetProperty("toHit").ValueKind);
        Assert.Equal(["canister"], shot.Rolls.Keys);
        Assert.Equal((Aim4[0], true, 0), (shot.Target.ToString(), shot.RateOfFireKept, shot.Acquisition));
        Assert.Null(shot.Facing);
        Assert.Null(shot.Acquired);
        var block = shot.Resolution.GetProperty("canister");
        Assert.Equal((4, "west", 12, 6, 0, 7), (block.GetProperty("aim").GetInt32(), block.GetProperty("facing").GetString(), block.GetProperty("firepower").GetInt32(),
            block.GetProperty("originalDr").GetInt32(), block.GetProperty("caseA").GetInt32(), block.GetProperty("depletionNumber").GetInt32()));
        Assert.Equal(Aim4, block.GetProperty("attacks").EnumerateArray().Select(item => item.GetProperty("locationId").GetString()));
        Assert.Equal(Aim4, shot.Facts.GetProperty("canister").GetProperty("locations").EnumerateArray().Select(item => item.GetProperty("locationId").GetString()));
        Assert.Equal("canister", shot.Facts.GetProperty("ammunition").GetString());

        // A fire record a Location, each on the shot's own DR, with its own FP.
        var fires = Fires(before);
        Assert.Equal(Aim4, fires.Select(fire => fire.TargetLocation));
        Assert.All(fires, fire => Assert.Equal((GunHex, shot.Rolls["canister"]), (fire.FirerLocation, fire.Rolls["attack"])));
        Assert.All(fires, fire => Assert.Empty(fire.Firers));
        Assert.Equal([12m, 12m, 6m, 6m], fires.Select(Fp));
        Assert.Equal(["de-s1", "de-s2", null, "de-s3"], fires.Select(fire => fire.Facts.GetProperty("targets").EnumerateArray().Select(item => item.GetProperty("unitId").GetString()).SingleOrDefault()));
        Assert.Equal(1, fires[3].Facts.GetProperty("canister").GetProperty("adjacentHexes").GetInt32());
        Assert.False(fires[0].Facts.GetProperty("canister").TryGetProperty("adjacentHexes", out _));
        Assert.Single(Since(before), item => item.Payload is DiceRolled { Purpose: "ordnance-canister" });

        // The Gun kept its ROF on the colored dr of 2, and its records bind no other attack of its Location (A7.51).
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Find("us-gun")!, Conditions.Malfunctioned));
        Assert.All(Current.FiresThisPhase, record => Assert.True(record.Ordnance));
        Assert.Equal((1, true), (Current.OrdnanceShots.Single().Shots, Current.OrdnanceShots.Single().RateOfFireKept));

        // The proposal says what is attacked and what else the DR decides.
        Assert.Contains(result.Plan!.Reasons, reason => reason.StartsWith("play.canister: us-gun fires Canister at aim 4 of the CA facing west in the PFPh; no To Hit DR", StringComparison.Ordinal)
            && reason.Contains("bd01:D4:0 with 12 FP, open-ground", StringComparison.Ordinal) && reason.Contains("bd01:E5:0 with 6 FP (half in the adjacent hex, C8.41)", StringComparison.Ordinal));
        Assert.Contains(result.Plan.Reasons, reason => reason.Contains("check of the Depletion Number 7", StringComparison.Ordinal) && reason.Contains("malfunction on 12 or more", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task CanisterDoesNotAttackTheFiringSidesOwnUnits()
    {
        // A7.4: the outcome affects "all those enemy ... units in the target Location": an American squad in C5 is no target.
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads(), Unit("us-s", "asl:squad", "american-squad", Aim4[2], "american")]);
        var before = Revision;
        Committed(await FireCanister(4, null, 2, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        Assert.Empty(Fires(before)[2].Facts.GetProperty("targets").EnumerateArray());
        Assert.DoesNotContain(Since(before), item => item.Payload is ConditionsChanged { Id: "us-s" });
        NoReplayErrors();
    }

    [Fact]
    public async Task ACanisterShotBindsNoFireGroupOfItsLocation()
    {
        // A7.51, A7.55: ordnance forms no fire group, so a squad beside the Gun still fires at a Location the Canister attacked this phase.
        string[] firers = ["us-s"];
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads(), Unit("us-s", "asl:squad", "american-squad", GunHex, "american")]);
        Committed(await FireCanister(4, null, 2, 4, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 5), new
        {
            firers,
            target = Aim4[0],
        }));
        Assert.Equal(firers, Fires(before).Single().Firers);
        Assert.Single(Current.FiresThisPhase, record => !record.Ordnance);
        NoReplayErrors();
    }

    [Fact]
    public async Task ACanisterShotThatTurnsTheGunAddsCaseAToEachAttack()
    {
        // C3.21, D3.52, C5.1: one hexspine by an NT Gun is +3 on the IFT DR of every Location; C2.5: its ROF is then 1, so the colored dr of 2
        // loses it and the Gun and its crew take the Prep Fire counter.
        await SetupAmerican(1943, 7, [.. AmericanGun("north-west"), .. Squads()]);
        var before = Revision;
        var result = await FireCanister(4, "west", 2, 3, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2);
        Committed(result);
        var shot = LastShot(before);
        Assert.Equal((UnitFacing.West, false, 1), (shot.Facing, shot.RateOfFireKept, shot.Facts.GetProperty("hexspinesToTurn").GetInt32()));
        Assert.Equal(3, shot.Resolution.GetProperty("canister").GetProperty("caseA").GetInt32());
        var fires = Fires(before);
        Assert.Equal(Aim4, fires.Select(fire => fire.TargetLocation));
        Assert.All(fires, fire => Assert.Equal(3m, DrmOf(IftDrm(fire), "case-a:us-gun")));
        Assert.All(fires, fire => Assert.Equal(3, fire.Facts.GetProperty("canister").GetProperty("caseA").GetInt32()));
        Assert.Equal(UnitFacing.West, GunFacing(Current));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Find("us-gun")!, Conditions.PrepFire));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Unit("us-crew")!, Conditions.PrepFire));
        Assert.Contains(result.Plan!.Reasons, reason => reason.Contains("turning 1 hexspine(s) to west: Case A +3 on the IFT DR (D3.52)", StringComparison.Ordinal));
        NoReplayErrors();
    }

    [Fact]
    public async Task ADrAboveTheDepletionNumberAttacksNothingAndTheGunHasNotFired()
    {
        // C8.9: an Original 8 is above C7: "the firer had no such ammunition and is considered not to have fired yet for any purpose": two dice,
        // no Location attacked, no counter, the ROF kept though the colored dr is 4; no Canister again, and the Gun fires HE at once.
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads()]);
        var before = Revision;
        Committed(await FireCanister(4, null, 4, 4));
        var shot = LastShot(before);
        Assert.Equal("none", Use(shot));
        Assert.True(shot.RateOfFireKept);
        Assert.Empty(shot.Resolution.GetProperty("canister").GetProperty("attacks").EnumerateArray());
        Assert.Empty(Fires(before));
        Assert.Single(Since(before), item => item.Payload is DiceRolled);
        Assert.DoesNotContain(Since(before), item => item.Payload is ConditionsChanged);
        Assert.Empty(Current.OrdnanceShots);
        Assert.Contains(Current.DepletedAmmunition, item => item.Gun == "us-gun" && item.Ammunition == "canister");
        Refused(await FireCanister(4, null, 1, 1), "asl.a1.ordnance.ammunition-depleted", "C8.9");

        var again = Revision;
        Committed(await Do(GameActions.FireOrdnance, Once(5, 6), new
        {
            gunId = "us-gun",
            target = Aim4[0],
        }));
        Assert.Single(Current.OrdnanceShots);
        Assert.NotEqual(JsonValueKind.Null, LastShot(again).Resolution.GetProperty("toHit").ValueKind);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnOriginalTwelveMalfunctionsTheGunThatHadNoCanister()
    {
        // C8.9, C2.28: "unless Gun Malfunction ... occurs": the Gun malfunctions, has fired, and attacked nothing.
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads()]);
        var before = Revision;
        Committed(await FireCanister(4, null, 6, 6));
        var shot = LastShot(before);
        Assert.Equal("none", Use(shot));
        Assert.False(shot.RateOfFireKept);
        Assert.Empty(Fires(before));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Find("us-gun")!, Conditions.Malfunctioned));
        Assert.Equal(ConditionState.True, GameState.Condition(Current.Find("us-gun")!, Conditions.PrepFire));
        Assert.Single(Current.OrdnanceShots);
        NoReplayErrors();
    }

    [Fact]
    public async Task CanisterCostsTheGunItsAcquisition()
    {
        // C6.5: an Acquisition is lost when the Gun "fires SMOKE, canister or IFE". An HE shot at D4 first: a 1 and a 6, the ROF kept and the
        // Location acquired, its hit's IFT DR a 12; the Canister shot then removes the counter.
        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads()]);
        Committed(await Do(GameActions.FireOrdnance, Once(1, 6, 6, 6, 1, 2, 1, 2), new
        {
            gunId = "us-gun",
            target = Aim4[0],
        }));
        Assert.Equal(-1, Assert.Single(Current.Acquisitions).Level);
        var before = Revision;
        Committed(await FireCanister(4, null, 1, 5, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1));
        Assert.Equal(0, LastShot(before).Acquisition);
        Assert.Null(LastShot(before).Acquired);
        Assert.Empty(Current.Acquisitions);
        NoReplayErrors();
    }

    [Fact]
    public async Task ACanisterShotNamesAnAimAndIsNotThereBeforeAugust1942()
    {
        // Ruling R55.8: an aim of 1 to 5; U.S. Ordnance Note C: Canister from August 1942; and no Location of an aim off the map.
        await SetupAmerican(1942, 7, [.. AmericanGun(), .. Squads()]);
        Refused(await Do(GameActions.FireOrdnance, NoRoll(), new
        {
            gunId = "us-gun",
            ammunition = "canister"
        }), "play.canister-aim");
        Refused(await FireCanister(4, null, 1, 1), "asl.a1.ordnance.ammunition-unavailable");
    }

    // Task 55.5 (increment 2e): Armor Leaders and Inexperienced Crews (D3.4 to D3.45, p. 200; rulings R55.15 to R55.18).

    private static readonly Perspective German = Perspective.Side("german");
    private static readonly Perspective Russian = Perspective.Side("russian");
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] Halftrack = ["de-ht"];
    private const string North3 = "bd01:B7:0";

    /// <summary>A vehicle with an Armor Leader of the grade ("9-1"), an Inexperienced Crew, or other conditions.</summary>
    private static Dictionary<string, object> Afv(string id, string definition, string at, string side, string facing, string? leader = null, params string[] conditions)
    {
        var vehicle = new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = "asl:vehicle",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["conditions"] = conditions.ToDictionary(name => name, _ => true, StringComparer.Ordinal),
        };
        if (leader is not null)
        {
            var dash = leader.LastIndexOf('-');
            vehicle["armorLeader"] = new
            {
                morale = int.Parse(leader[..dash], System.Globalization.CultureInfo.InvariantCulture),
                leadership = int.Parse(leader[dash..], System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        return vehicle;
    }

    private static Dictionary<string, object> LedTank(string? leader, params string[] conditions) => Afv("de-tank", "attacker-tank", Firer, "german", "north-east", leader, conditions);

    private GameView ViewAs(Perspective perspective)
    {
        var history = Planner().Replay(store.Read(Scope)!.Events);
        return GameView.Of(history, history.Current!.Revision, perspective);
    }

    private ArmorLeader? LeaderOf(string vehicle) => Current.Unit(vehicle)!.ArmorLeader;

    private async Task Advance(int times = 1)
    {
        for (var index = 0; index < times; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }
    }

    private async Task PassFire() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private Task<PlayResult> FireMa(string tank, string target, string? targetVehicle, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = tank, ["target"] = target };
        if (targetVehicle is not null)
        {
            arguments["targetVehicle"] = targetVehicle;
            arguments["ammunition"] = "ap";
        }

        return Do(GameActions.FireOrdnance, Once(dice), arguments);
    }

    /// <summary>A hex of the given column next to another: one of the two a vehicle facing that way has in its VCA.</summary>
    private static string Beside(string hex, char column)
    {
        var handle = Board01Fixture.Handle();
        return "bd01:" + Board01Fixture.Hexes().Where(text => text[0] == column && char.IsDigit(text[1]) && handle.Distance(HexName.Parse(hex), HexName.Parse(text)) == 1)
            .Order(StringComparer.Ordinal).First() + ":0";
    }

    [Fact]
    public async Task ATankWithAnArmorLeaderFiresItsMaWithHisModifierWhichRevealsHim()
    {
        // D3.41: noted on a side record until used. D3.44: his modifier on the MA's To Hit DR, BU or not.
        await Setup(LedTank("9-1"), Squad(Target));
        Assert.Equal(new ArmorLeader(9, -1), LeaderOf("de-tank"));
        Assert.Null(Assert.Single(ViewAs(Russian).Units, unit => unit.Id == "de-tank").ArmorLeader);
        Assert.All(ViewAs(Russian).Events.Select(item => item.Payload).OfType<InstanceCreated>(), created => Assert.Null(created.Instance.ArmorLeader));
        Assert.Equal(new ArmorLeader(9, -1), Assert.Single(ViewAs(German).Units, unit => unit.Id == "de-tank").ArmorLeader);
        Assert.NotNull(Assert.Single(ViewAs(Perspective.Adjudicator).Units, unit => unit.Id == "de-tank").ArmorLeader);

        // An Original 11 misses: no IFT DR. The record's DRM names him, and from then on both sides read him.
        var before = Revision;
        Committed(await FireMa("de-tank", Target, null, 5, 6));
        var shot = LastShot(before);
        Assert.Equal(-1m, DrmOf(ToHitDrm(shot), "armor-leader"));
        Assert.Equal(1m, DrmOf(ToHitDrm(shot), "case-i"));
        Assert.Equal(-1, shot.Facts.GetProperty("vehicle").GetProperty("armorLeadership").GetInt32());
        Assert.Equal(new ArmorLeader(9, -1, true), LeaderOf("de-tank"));
        Assert.Equal(new ArmorLeader(9, -1, true), Assert.Single(ViewAs(Russian).Units, unit => unit.Id == "de-tank").ArmorLeader);
        Assert.Contains(ViewAs(Russian).Events.Select(item => item.Payload).OfType<InstanceCreated>(), created => created.Instance.ArmorLeader is not null);
        NoReplayErrors();
    }

    [Fact]
    public async Task ATankWithoutAnArmorLeaderFiresAsBefore()
    {
        // The control: no fact in the record, and the same Final DR but for his modifier.
        await Setup(LedTank(null), Squad(Target));
        var before = Revision;
        Committed(await FireMa("de-tank", Target, null, 5, 6));
        var shot = LastShot(before);
        Assert.Equal(0m, DrmOf(ToHitDrm(shot), "armor-leader"));
        Assert.False(shot.Facts.GetProperty("vehicle").TryGetProperty("armorLeadership", out _));
        Assert.Null(LeaderOf("de-tank"));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnInexperiencedCrewAddsOneAndIsPublic()
    {
        // D3.45: "treated as if it contains an inherent 6+1 Armor Leader", which is no secret: the condition is the vehicle's, in both views.
        await Setup(LedTank(null, Conditions.InexperiencedCrew), Squad(Target));
        Assert.Contains(ViewAs(Russian).Units, unit => unit.Id == "de-tank" && GameState.Condition(unit, Conditions.InexperiencedCrew) == ConditionState.True);
        var before = Revision;
        Committed(await FireMa("de-tank", Target, null, 5, 6));
        Assert.Equal(1m, DrmOf(ToHitDrm(LastShot(before)), "inexperienced-crew"));
        Assert.Equal(1, LastShot(before).Facts.GetProperty("vehicle").GetProperty("armorLeadership").GetInt32());
        Assert.Null(LeaderOf("de-tank"));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnArmorLeaderOrAnInexperiencedCrewIsRefusedOffAnArmedAfvAndInABadGrade()
    {
        async Task<PlayResult> Start(params Dictionary<string, object>[] placements) => await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
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
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
            },
            placements,
        }));

        // D3.4, H1.43: a 10-1 is no grade. D3.41, D3.45: a truck is no armed AFV. D3.45: the 6+1 is the Inexperienced Crew's leader.
        Refused(await Start(LedTank("10-1"), Squad(Target)), "8-1, 9-1, 9-2, and 10-2");
        Refused(await Start(Afv("de-truck", "attacker-truck", Firer, "german", "north-east", "9-1"), Squad(Target)), "play.setup-armor-leader");
        Refused(await Start(Afv("de-truck", "attacker-truck", Firer, "german", "north-east", null, Conditions.InexperiencedCrew), Squad(Target)), "play.setup-inexperienced-crew");
        Refused(await Start(LedTank("9-1", Conditions.InexperiencedCrew), Squad(Target)), "D3.45");
        var squad = Squad(Target);
        squad["armorLeader"] = new
        {
            morale = 9,
            leadership = -1
        };
        Refused(await Start(LedTank(null), squad), "not a vehicle");
        Committed(await Start(Afv("de-ht", "attacker-halftrack", Firer, "german", "north-east", "8-1"), Squad(Target)));
    }

    [Fact]
    public async Task AHalftracksArmorLeaderModifiesItsMaAamgAndATanksDoesNotModifyItsCmg()
    {
        // D3.44: "IFT otherwise": the SPW 251/1's MA is its AAMG; "an Armor Leader cannot modify non-OVR MG/FT (unless MA) attacks".
        await Setup(Afv("de-ht", "attacker-halftrack", Firer, "german", "north-east", "8-1"), Afv("de-tank", "attacker-tank", "bd01:C10:0", "german", "north-east", "9-2"), Squad(Target));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(5, 6), new
        {
            firers = Halftrack,
            target = Target
        }));
        var fire = LastFire(before);
        Assert.Equal(-1m, DrmOf(IftDrm(fire), "armor-leader"));
        Assert.Equal(-1, fire.Facts.GetProperty("vehicleFire").GetProperty("armorLeadership").GetInt32());
        Assert.True(LeaderOf("de-ht")!.Revealed);

        before = Revision;
        Committed(await FireCmg());
        fire = LastFire(before);
        Assert.Equal(0m, DrmOf(IftDrm(fire), "armor-leader"));
        Assert.False(fire.Facts.GetProperty("vehicleFire").TryGetProperty("armorLeadership", out _));
        Assert.False(LeaderOf("de-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnArmorLeaderModifiesHisVehiclesOvr()
    {
        // D7.16: the PzKpfw IIIH with a 9-2 enters the Russian squad's hex with an OVR; an Original 11, FFMO -1 and his -2.
        var entered = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", "bd01:E5:0", "german", "east", "9-2"), Squad(entered), Unit("r2", "asl:squad", "defender-squad", "bd01:J9:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-tank",
            kind = "start"
        }));
        await PassFire();
        Committed(await Do(GameActions.MoveVehicle, NoRoll(), new
        {
            vehicleId = "de-tank",
            kind = "enter",
            to = entered,
            overrun = true
        }));
        await PassFire();
        Assert.False(LeaderOf("de-tank")!.Revealed);
        var before = Revision;
        Committed(await Do(GameActions.Overrun, Once(6, 5, 2, 2, 1, 1, 1, 1), new
        {
        }));
        var fire = LastFire(before);
        Assert.Equal(-2m, DrmOf(IftDrm(fire), "armor-leader"));
        Assert.Equal(-2, fire.Facts.GetProperty("overrun").GetProperty("armorLeadership").GetInt32());
        Assert.True(LeaderOf("de-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnArmorLeaderModifiesHisVehiclesOwnCcAttackAndNotTheAttackOnIt()
    {
        // A11.6, A11.31, A11.62: in the Russian APh the squad passes its PAATC (DR 7 against 7) and advances into the tank's hex; in the CCPh it
        // attacks first, and the tank then attacks with its CMG, its 9-1 modifying the CC DR (D3.44).
        var beside = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", "bd01:E5:0", "german", "east", "9-1"), Squad(beside), Unit("g1", "asl:squad", "attacker-squad", "bd01:J9:0", "german"));
        await Advance(13);
        Assert.Equal(("aph", "russian"), (Current.Phase, Current.PhasingSide));
        Committed(await Do(GameActions.Advance, Once(3, 4), new
        {
            unitIds = R1,
            to = "bd01:E5:0"
        }));
        await Advance();
        Assert.Equal("ccph", Current.Phase);
        var before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Once(6, 5), new
        {
            location = "bd01:E5:0",
            vehicleId = "de-tank",
            attackers = R1
        }));
        var against = Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>().Single();
        Assert.False(against.Facts.GetProperty("vehicle").TryGetProperty("armorLeadership", out _));
        Assert.False(LeaderOf("de-tank")!.Revealed);

        before = Revision;
        Committed(await Do(GameActions.VehicleCloseCombat, Once(6, 5, 1, 1), new
        {
            location = "bd01:E5:0",
            vehicleId = "de-tank",
            defenders = R1
        }));
        var own = Since(before).Select(item => item.Payload).OfType<VehicleCloseCombatResolved>().Single();
        Assert.Equal(-1, own.Facts.GetProperty("vehicle").GetProperty("armorLeadership").GetInt32());
        Assert.Equal(-1m, DrmOf(own.Resolution.GetProperty("defending")[0].GetProperty("drm"), "armor-leader"));
        Assert.True(LeaderOf("de-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnArmorLeaderModifiesTheColoredDrOfHisVehiclesBogRemoval()
    {
        // D8.3: a colored 5 would Mire the tank; "one armor leader in either AFV also modifies the colored dr": a Final 4 frees it. The Start MP is
        // the Original colored dr times the white dr: 5 x 2 = 10 MP.
        await Setup(LedTank("9-1", Conditions.Bogged), Squad("bd01:J9:0"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.MoveVehicle, Once(5, 2), new
        {
            vehicleId = "de-tank",
            kind = "start"
        }));
        var check = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().Single();
        Assert.Equal((VehicleCheckRolled.BogRemoval, -1, (int?)-1, VehicleCheckRolled.Freed), (check.Check, check.Drm, check.ArmorLeadership, check.Result));
        Assert.Equal(20, Since(before).Select(item => item.Payload).OfType<VehicleStepped>().Single().HalfMp);
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit("de-tank")!, Conditions.Bogged));
        Assert.True(LeaderOf("de-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task ACrewChecksAtItsArmorLeadersMoraleAndOnlyThenIsHeRevealed()
    {
        // Two T-34 M41 in B7 fire AP at the PzKpfw IIIH with a 10-2, three hexes off, in the DFPh. The first misses: its record names no crew
        // morale and he stays unseen. The second strikes the turret, and a TK DR one above the Final TK# asks the crew's NTC (C7.41), taken at his
        // Morale Level of 10 (D3.42): a 9 passes where the crew's own 8 would not.
        await Setup(LedTank("10-2"), Afv("ru-t1", "defender-tank", North3, "russian", "south-west"), Afv("ru-t2", "defender-tank", North3, "russian", "south-west"));
        await Advance(2);
        Assert.Equal("dfph", Current.Phase);
        var before = Revision;
        Committed(await FireMa("ru-t1", Firer, "de-tank", 6, 5));
        Assert.False(LastShot(before).Facts.GetProperty("vehicleTarget").TryGetProperty("crewMorale", out _));
        Assert.False(LeaderOf("de-tank")!.Revealed);
        Assert.Null(Assert.Single(ViewAs(Russian).Units, unit => unit.Id == "de-tank").ArmorLeader);

        before = Revision;
        var kill = (await Probe("ru-t2")).GetProperty("finalTk").GetInt32();
        Committed(await FireMa("ru-t2", Firer, "de-tank", 2, 4, kill - 5, 6, 5, 4));
        var shot = LastShot(before);
        var check = shot.Resolution.GetProperty("kill").GetProperty("shockCheck");
        Assert.Equal((10, true, "armor-leader"), (check.GetProperty("moraleLevel").GetInt32(), check.GetProperty("passed").GetBoolean(), check.GetProperty("moraleSource").GetString()));
        Assert.Equal(10, shot.Facts.GetProperty("vehicleTarget").GetProperty("crewMorale").GetInt32());
        Assert.NotEqual(ConditionState.True, GameState.Condition(Current.Unit("de-tank")!, Conditions.Shocked));
        Assert.Equal(new ArmorLeader(10, -2, true), LeaderOf("de-tank"));
        Assert.NotNull(Assert.Single(ViewAs(Russian).Units, unit => unit.Id == "de-tank").ArmorLeader);
        NoReplayErrors();
    }

    /// <summary>The Final TK# a T-34's AP turret hit on the German tank meets, read from a game played aside to a dud (an Original TK DR of 12).</summary>
    private static async Task<JsonElement> Probe(string tank)
    {
        var aside = new BacklogPass55Tests();
        try
        {
            aside.board.Base.Clear();
            await aside.Setup(LedTank(null), Afv(tank, "defender-tank", North3, "russian", "south-west"));
            await aside.Advance(2);
            var before = aside.Revision;
            Committed(await aside.FireMa(tank, Firer, "de-tank", 2, 4, 6, 6));
            return aside.LastShot(before).Resolution.GetProperty("kill").Clone();
        }
        finally
        {
            aside.Dispose();
        }
    }

    [Fact]
    public async Task AWreckLosesItsArmorLeader()
    {
        // D3.43: he "ceases to exist once the crew takes counter form": a TK DR of 2 burns the tank.
        await Setup(LedTank("9-1"), Afv("ru-t1", "defender-tank", North3, "russian", "south-west"));
        await Advance(2);
        Committed(await FireMa("ru-t1", Firer, "de-tank", 2, 4, 1, 1, 6, 6));
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("de-tank")!.Status);
        Assert.Null(LeaderOf("de-tank"));
        NoReplayErrors();
    }

    // Task 55.3 (increment 2f): Motion attempts (D2.401, p. 198; rulings R55.11 to R55.13) and the DEFENDER's Motion firer (C5.35; ruling R55.14).
    // The German mover begins in E5, which the stub LOS hides from every Location, and enters a hex of column F in the LOS of the T-34 M41 in J5
    // (a red MP allotment: the Mechanical Reliability DR of D2.51).

    private const string Start = "bd01:E5:0";
    private const string Watcher = "bd01:J5:0";
    private static readonly string[] G1 = ["g1"];

    private Task<PlayResult> Drive(string kind, string? to = null) => Do(GameActions.MoveVehicle, NoRoll(), to is null
        ? new Dictionary<string, object> { ["vehicleId"] = "de-tank", ["kind"] = kind }
        : new Dictionary<string, object> { ["vehicleId"] = "de-tank", ["kind"] = kind, ["to"] = to });

    private Task<PlayResult> AttemptMotion(DiceRoller roller, string? facing = null, string? turret = null, string vehicle = "ru-tank")
    {
        var arguments = new Dictionary<string, object> { ["vehicleId"] = vehicle };
        if (facing is not null)
        {
            arguments["facing"] = facing;
        }

        if (turret is not null)
        {
            arguments["turret"] = turret;
        }

        return Do(GameActions.MotionAttempt, roller, arguments);
    }

    /// <summary>The German tank starts in E5 and enters a hex of column F, where the DEFENDER's window is open; returns that hex.</summary>
    private async Task<string> TankEntersTheLos(bool hidden = true, string watcherFacing = "north-east")
    {
        if (hidden)
        {
            los.Blind.Add("E5");
        }

        var entered = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", Start, "german", "east"), Afv("ru-tank", "defender-tank", Watcher, "russian", watcherFacing));
        await Advance();
        Committed(await Drive("start"));
        if (hidden)
        {
            // The Start MP was spent out of the T-34's LOS: nothing in it yet, and a refusal spends no attempt.
            Refused(await AttemptMotion(Once(1)), "play.motion-attempt-los", "no attempt is spent");
        }

        await PassFire();
        Committed(await Drive("enter", entered));
        return entered;
    }

    private MotionAttempted LastAttempt(long before) => Since(before).Select(item => item.Payload).OfType<MotionAttempted>().Single();

    private static decimal? ExactDrm(JsonElement drm, string name) => drm.EnumerateArray().Where(item => item.GetProperty("name").GetString() == name)
        .Select(item => (decimal?)item.GetProperty("value").GetDecimal()).SingleOrDefault();

    private UnitFacing? FacingOf(string vehicle) => (Current.Unit(vehicle)!.Position as MapPosition)?.Facing;

    private bool Has(string vehicle, string condition) => GameState.Condition(Current.Unit(vehicle)!, condition) == ConditionState.True;

    [Fact]
    public async Task ADefendersTankGainsMotionTurnsItsVcaAndFiresAsAMotionFirer()
    {
        // D2.401: the PzKpfw IIIH has spent 1 MP in the T-34's LOS, so a dr of 1 passes; D2.51: the T-34's red MP then make it roll, and a 7
        // is no breakdown; "allowed to freely change its VCA/TCA": from north-east to west, toward the mover.
        var entered = await TankEntersTheLos();
        var option = Assert.Single(Planner().MotionAttemptOptions(Current, store.Read(Scope)!.Events, "russian"));
        Assert.Equal(("ru-tank", null, "de-tank", 2, true, true, false), (option.Vehicle, option.Bar, option.Mover, option.HalfPoints, option.VehicleMover, option.Turreted, option.Bypass));
        Assert.Empty(Planner().MotionAttemptOptions(Current, store.Read(Scope)!.Events, "german"));

        var before = Revision;
        Committed(await AttemptMotion(Once(1, 3, 4), facing: "west"));
        Assert.Equal(["dice-rolled", "dice-rolled", "vehicle-check-rolled", "motion-attempted"], Since(before).Select(item => item.Type));
        var attempt = LastAttempt(before);
        Assert.Equal(("ru-tank", 2, MotionAttempted.Passed, "de-tank", (UnitFacing?)UnitFacing.West, null),
            (attempt.Vehicle, attempt.HalfPoints, attempt.Result, attempt.Mover, attempt.Facing, attempt.Turret));
        var reliability = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().Single();
        Assert.Equal((VehicleCheckRolled.Mechanical, VehicleCheckRolled.Passed), (reliability.Check, reliability.Result));
        Assert.True(Has("ru-tank", Conditions.Motion));
        Assert.Equal(UnitFacing.West, FacingOf("ru-tank"));
        Assert.Equal(["ru-tank"], Current.MotionAttempts);

        // The attempt is no fire action: the window stays open, and the T-34 fires its MA as Defensive First Fire. C5.35 (ruling R55.14): Case C
        // alone, +4 for its turret, and the lower dr of the TH DR doubled; the target lies in the VCA it turned to, so no Case A.
        Assert.True(Current.Movement is { WindowOpen: true });
        before = Revision;
        Committed(await FireMa("ru-tank", entered, "de-tank", 5, 6));
        var shot = LastShot(before);
        Assert.Equal("first-fire", shot.Facts.GetProperty("fireKind").GetString());
        Assert.True(shot.Facts.GetProperty("vehicle").GetProperty("inMotion").GetBoolean());
        Assert.Equal(4m, ExactDrm(ToHitDrm(shot), "case-c"));
        Assert.Equal(5m, ExactDrm(ToHitDrm(shot), "case-c4:lower-dr"));
        Assert.Equal(0m, DrmOf(ToHitDrm(shot), "case-a"));
        Assert.Null(ExactDrm(ToHitDrm(shot), "case-c1"));
        Assert.Null(ExactDrm(ToHitDrm(shot), "case-c2"));

        // "only once per enemy MPh", and never under a First Fire counter.
        Assert.True(Has("ru-tank", Conditions.FirstFire));
        Refused(await AttemptMotion(Once(1)), "First Fire counter");
        await PassFire();

        // The phase's end clears the mark; the Motion counter stays until the T-34's own next MPh (D2.4).
        Committed(await Drive("stop"));
        await PassFire();
        await Advance();
        Assert.Equal("dfph", Current.Phase);
        Assert.Empty(Current.MotionAttempts);
        Assert.True(Has("ru-tank", Conditions.Motion));
        NoReplayErrors();
    }

    [Fact]
    public async Task AFailedMotionAttemptHasNoPenaltyButBarsASecond()
    {
        // D2.401: a dr of 2 against 1 MP fails: no Mechanical Reliability DR, no Motion, no change of VCA; "a vehicle may attempt Motion status
        // only once per enemy MPh".
        await TankEntersTheLos();
        var before = Revision;
        Committed(await AttemptMotion(Once(2), facing: "west"));
        Assert.Equal(["dice-rolled", "motion-attempted"], Since(before).Select(item => item.Type));
        var attempt = LastAttempt(before);
        Assert.Equal((MotionAttempted.Failed, null), (attempt.Result, attempt.Facing));
        Assert.False(Has("ru-tank", Conditions.Motion));
        Assert.Equal(UnitFacing.NorthEast, FacingOf("ru-tank"));
        Refused(await AttemptMotion(Once(1)), "only once");
        Assert.Equal("ru-tank", Assert.Single(Planner().MotionAttemptOptions(Current, store.Read(Scope)!.Events, "russian"), option => option.Bar is not null).Vehicle);

        // The DEFENDER may still fire or pass.
        Assert.True(Current.Movement is { WindowOpen: true });
        await PassFire();
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAttemptAgainstAMoverSeenSinceItsStartIsRefusedAndSpendsNoAttempt()
    {
        // Ruling R55.12: the tank began its move in the T-34's LOS; the game makes the rule's LOS Check first and refuses.
        await TankEntersTheLos(hidden: false);
        var before = Revision;
        Refused(await AttemptMotion(Once(1)), "play.motion-attempt-seen", "no attempt is spent");
        Assert.Equal(before, Revision);
        Assert.Empty(Current.MotionAttempts);

        // Nor does the side that moves attempt, nor a vehicle that is not there.
        Refused(await AttemptMotion(Once(1), vehicle: "de-tank"), "not a vehicle of the DEFENDER");
        Refused(await AttemptMotion(Once(1), vehicle: "nobody"), "not a vehicle of the DEFENDER");
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleUnderAFirstFireCounterAttemptsNoMotion()
    {
        // D2.401: "may not attempt it at all if already marked with a First Fire (or Final/Intensive Fire) counter". An Original 11 misses and
        // keeps no ROF.
        var entered = await TankEntersTheLos(watcherFacing: "west");
        Committed(await FireMa("ru-tank", entered, "de-tank", 5, 6));
        Assert.True(Has("ru-tank", Conditions.FirstFire));
        Refused(await AttemptMotion(Once(1)), "First Fire counter");
        Assert.Empty(Current.MotionAttempts);
        NoReplayErrors();
    }

    [Fact]
    public async Task TheMechanicalReliabilityDrMayUndoAPassedAttempt()
    {
        // D2.401, D2.51: "a successful Motion Attempt results in neither Motion nor VCA change if any mechanical reliability DR results in
        // Immobilization": a 12.
        await TankEntersTheLos();
        var before = Revision;
        Committed(await AttemptMotion(Once(1, 6, 6), facing: "west", turret: "south-west"));
        var attempt = LastAttempt(before);
        Assert.Equal((MotionAttempted.Immobilized, null, null), (attempt.Result, attempt.Facing, attempt.Turret));
        Assert.True(Has("ru-tank", Conditions.Immobilized));
        Assert.False(Has("ru-tank", Conditions.Motion));
        Assert.Equal(UnitFacing.NorthEast, FacingOf("ru-tank"));
        Assert.Empty(Current.TurretFacings);
        Refused(await AttemptMotion(Once(1)), "immobilized");
        NoReplayErrors();
    }

    [Fact]
    public async Task AVcaChangeInWoodsTakesABogCheckForEachHexspine()
    {
        // D2.11, C5.11, D8.2: "one Bog DR per hexspine" of a VCA change in woods; from north-east to west is two. Both pass.
        board.Terrain["J5"] = "Woods";
        await TankEntersTheLos();
        var before = Revision;
        Committed(await AttemptMotion(Once(1, 3, 4, 1, 1, 1, 2), facing: "west"));
        var checks = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().ToArray();
        Assert.Equal([VehicleCheckRolled.Mechanical, VehicleCheckRolled.Bog, VehicleCheckRolled.Bog], checks.Select(item => item.Check));
        Assert.All(checks, item => Assert.Equal(VehicleCheckRolled.Passed, item.Result));
        Assert.Equal(MotionAttempted.Passed, LastAttempt(before).Result);
        Assert.Equal(UnitFacing.West, FacingOf("ru-tank"));
        Assert.True(Has("ru-tank", Conditions.Motion));
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehicleThatBogsOnItsVcaChangeGainsNeitherMotionNorTheChange()
    {
        // Ruling R55.13: the first hexspine's Bog DR of 12 bogs the T-34: no second check, no Motion, its VCA as it was. A TCA alone takes none.
        board.Terrain["J5"] = "Woods";
        await TankEntersTheLos();
        var before = Revision;
        Committed(await AttemptMotion(Once(1, 3, 4, 6, 6), facing: "west"));
        var checks = Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>().ToArray();
        Assert.Equal([(VehicleCheckRolled.Mechanical, VehicleCheckRolled.Passed), (VehicleCheckRolled.Bog, VehicleCheckRolled.Bogged)], checks.Select(item => (item.Check, item.Result)));
        var attempt = LastAttempt(before);
        Assert.Equal((MotionAttempted.Bogged, null), (attempt.Result, attempt.Facing));
        Assert.True(Has("ru-tank", Conditions.Bogged));
        Assert.False(Has("ru-tank", Conditions.Motion));
        Assert.Equal(UnitFacing.NorthEast, FacingOf("ru-tank"));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnInfantryMoversMfInLosAreSummedAndATcaAloneIsTurned()
    {
        // D2.401: "the number of MF/MP expended in its LOS": a squad that began in E5, out of LOS, has spent 1 MF in each of two Open Ground
        // hexes in the T-34's LOS: a dr of 2 passes. The TCA named is taken with no Bog Check, and the VCA stays.
        los.Blind.Add("E5");
        var first = Beside("E5", 'F');
        var second = Beside(first.Split(':')[1], 'G');
        await Setup(Unit("g1", "asl:squad", "attacker-squad", Start, "german"), Afv("ru-tank", "defender-tank", Watcher, "russian", "north-east"));
        await Advance();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = first
        }));
        var early = Assert.Single(Planner().MotionAttemptOptions(Current, store.Read(Scope)!.Events, "russian"));
        Assert.Equal((null, 2, false), (early.Bar, early.HalfPoints, early.VehicleMover));
        await PassFire();
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = second
        }));
        var before = Revision;
        Committed(await AttemptMotion(Once(2, 3, 4), turret: "south-west"));
        var attempt = LastAttempt(before);
        Assert.Equal((4, MotionAttempted.Passed, "g1", null, (UnitFacing?)UnitFacing.SouthWest), (attempt.HalfPoints, attempt.Result, attempt.Mover, attempt.Facing, attempt.Turret));
        Assert.DoesNotContain(Since(before).Select(item => item.Payload).OfType<VehicleCheckRolled>(), check => check.Check == VehicleCheckRolled.Bog);
        Assert.True(Has("ru-tank", Conditions.Motion));
        Assert.Equal(UnitFacing.NorthEast, FacingOf("ru-tank"));
        Assert.Equal(UnitFacing.SouthWest, Assert.Single(Current.TurretFacings, item => item.Vehicle == "ru-tank").Facing);

        // A vehicle already in Motion may attempt again for the change of CA, but not in the same enemy MPh.
        Refused(await AttemptMotion(Once(1), facing: "west"), "only once");
        NoReplayErrors();
    }

    // Task 55.7 (increment 2g): Gun Duels (C2.2401, pp. 167 and 168; rulings R55.19, R55.20).

    private const string DuelHex = "bd01:J5:0";
    private const string SecondDuelHex = "bd01:J6:0";

    /// <summary>A Russian 45L AT Gun and its crew; from J5 or J6 a barrel to the south-west must turn one hexspine to cover column F.</summary>
    private static Dictionary<string, object>[] RussianGun(string gun = "ru-gun", string crew = "ru-crew", string at = DuelHex, string facing = "south-west", bool concealed = false) =>
    [
        new()
        {
            ["id"] = crew,
            ["kind"] = "asl:crew",
            ["definition"] = "defender-crew",
            ["side"] = "russian",
            ["position"] = new
            {
                at
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = concealed, ["asl:hidden"] = false },
        },
        new()
        {
            ["id"] = gun,
            ["kind"] = "asl:gun",
            ["definition"] = "defender-at-gun",
            ["side"] = "russian",
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["holding"] = new
            {
                holder = crew,
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false, ["asl:concealed"] = concealed },
        },
    ];

    /// <summary>
    /// The PzKpfw IIIH, BU, starts in E5 and enters a hex of column F, where the DEFENDER's window is open; returns that hex. Seen from its
    /// start it fires with Case C (+4) and Case I (+1); begun out of LOS, with Case C2 (+6) and Case I.
    /// </summary>
    private async Task<string> TankComesOn(bool woods = true, bool seen = true, string? leader = null, bool fireFirst = false, params Dictionary<string, object>[] guns)
    {
        if (woods)
        {
            board.Terrain["J5"] = "Woods";
            board.Terrain["J6"] = "Woods";
        }

        if (!seen)
        {
            los.Blind.Add("E5");
        }

        var entered = Beside("E5", 'F');
        await Setup([Afv("de-tank", "attacker-tank", Start, "german", "east", leader), .. guns]);
        await Advance();
        Committed(await Drive("start"));
        await PassFire();
        if (fireFirst)
        {
            // A Bounding First Fire shot at the Gun that keeps its ROF (a colored dr of 1) and misses: the record uses, and reveals, an Armor Leader.
            Committed(await FireMa("de-tank", DuelHex, null, 1, 6));
        }

        Committed(await Drive("enter", entered));
        return entered;
    }

    private Task<PlayResult> DeclareShot(string entered, DiceRoller roller, string gun = "ru-gun") => Do(GameActions.FireOrdnance, roller,
        new Dictionary<string, object> { ["gunId"] = gun, ["target"] = entered, ["targetVehicle"] = "de-tank", ["ammunition"] = "ap" });

    private Task<PlayResult> Answer(string option, DiceRoller roller) => Do(GameActions.Choose, roller, new Dictionary<string, object> { ["key"] = Current.Choice!.Key, ["option"] = option });

    private OrdnanceFired[] Shots(long before) => [.. Since(before).Select(item => item.Payload).OfType<OrdnanceFired>()];

    private static bool IsDuelShot(OrdnanceFired shot) => shot.Facts.TryGetProperty("gunDuel", out var duel) && duel.ValueKind == JsonValueKind.True;

    [Fact]
    public async Task AShotDeclaredAtAMovingTankWithTheLowerGunDuelDrmWaitsForItsAnswer()
    {
        // C2.2401: the AT Gun in woods must turn one hexspine, Case A +3 doubled: +6. The tank, seen since its MPh began, would fire with Case C
        // +4 and Case I +1: +5, the doubled lower dr of its Motion not counted. 5 < 6: the shot is declared, and no dice are rolled.
        var entered = await TankComesOn(guns: RussianGun());
        var before = Revision;
        var declared = await DeclareShot(entered, NoRoll());
        Committed(declared);
        Assert.Contains(declared.Reasons, reason => reason.Contains("answer with a Gun Duel (C2.2401)", StringComparison.Ordinal) && reason.Contains("no dice are rolled", StringComparison.Ordinal));
        Assert.Equal(["choice-pending"], Since(before).Select(item => item.Type));
        var choice = Current.Choice!;
        Assert.Equal((ChoicePending.GunDuel, "german"), (choice.Kind, choice.Side));
        Assert.Equal([ChoicePending.Duel, ChoicePending.Decline], choice.Options);
        Assert.Equal(5, choice.Resume.GetProperty("attacker").GetProperty("total").GetInt32());
        Assert.Equal(6, choice.Resume.GetProperty("defender").GetProperty("total").GetInt32());
        Assert.Equal(["case-c", "case-i"], choice.Resume.GetProperty("attacker").GetProperty("lines").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(["case-a:1"], choice.Resume.GetProperty("defender").GetProperty("lines").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        var shot = choice.Resume.GetProperty("shot");
        Assert.Equal(("ru-gun", entered, "de-tank"), (shot.GetProperty("gunId").GetString(), shot.GetProperty("target").GetString(), shot.GetProperty("targetVehicle").GetString()));
        Assert.False(shot.TryGetProperty("attemptId", out _));
        Assert.False(choice.Resume.TryGetProperty("rolls", out _));
        var asked = GamePlanner.DescribeChoice(choice);
        Assert.Contains($"ru-gun in {DuelHex} has declared a shot at de-tank", asked, StringComparison.Ordinal);
        Assert.Contains("de-tank +5 (Case C +4, Case I +1); ru-gun +6 (Case A +6)", asked, StringComparison.Ordinal);

        // Nothing else happens until the ATTACKER answers; the Gun has not fired and has not turned.
        Refused(await Do(GameActions.PassFire, NoRoll(), new
        {
        }), "play.choice-pending");
        Assert.Empty(Current.OrdnanceShots);
        Assert.Equal(UnitFacing.SouthWest, ((MapPosition)Current.Find("ru-gun")!.Position).Facing);
        Assert.Null(Current.GunDuel);
        NoReplayErrors();
    }

    [Fact]
    public async Task ADeclinedGunDuelResolvesTheDeclaredShot()
    {
        var entered = await TankComesOn(guns: RussianGun());
        Committed(await DeclareShot(entered, NoRoll()));
        var before = Revision;
        var answered = await Answer(ChoicePending.Decline, Once(5, 6));
        Committed(answered);
        Assert.Contains(answered.Reasons, reason => reason.Contains("declines the Gun Duel", StringComparison.Ordinal));
        Assert.Equal("choice-made", Since(before)[0].Type);
        var shot = Assert.Single(Shots(before));
        Assert.Equal("ru-gun", shot.Gun);
        Assert.Equal("first-fire", shot.Facts.GetProperty("fireKind").GetString());
        Assert.False(IsDuelShot(shot));
        Assert.Equal(6m, DrmOf(ToHitDrm(shot), "case-a"));
        Assert.Null(Current.Choice);
        Assert.Null(Current.GunDuel);
        Assert.DoesNotContain(Current.VehicleWeaponShots, item => item.Vehicle == "de-tank");
        NoReplayErrors();
    }

    [Fact]
    public async Task InAGunDuelTheTankFiresFirstAndTheDeclaredShotFollows()
    {
        // The tank's MA misses (an Original 11), as Bounding First Fire while the window is open; the Gun then turns and fires its declared shot.
        var entered = await TankComesOn(guns: RussianGun());
        Committed(await DeclareShot(entered, NoRoll()));
        var step = Current.Movement!.Step;
        var before = Revision;
        var answered = await Answer(ChoicePending.Duel, Once(5, 6, 5, 6));
        Committed(answered);
        Assert.Contains(answered.Reasons, reason => reason.Contains("declares a Gun Duel and fires its MA first", StringComparison.Ordinal));
        var shots = Shots(before);
        Assert.Equal(["de-tank", "ru-gun"], shots.Select(item => item.Gun));
        Assert.True(IsDuelShot(shots[0]));
        Assert.Equal("bounding-first-fire", shots[0].Facts.GetProperty("fireKind").GetString());
        Assert.Equal(DuelHex, shots[0].Target.ToString());
        Assert.Equal(4m, ExactDrm(ToHitDrm(shots[0]), "case-c"));
        Assert.Equal(5m, ExactDrm(ToHitDrm(shots[0]), "case-c4:lower-dr"));
        Assert.False(IsDuelShot(shots[1]));
        Assert.Equal(6m, DrmOf(ToHitDrm(shots[1]), "case-a"));
        Assert.DoesNotContain(Since(before), item => item.Payload is GunDuelVoided);

        // The duel's shot is a Bounding First Fire shot for every later purpose: kept with this MP expenditure, under a Bounding Fire counter.
        Assert.True(Current.Movement is { WindowOpen: true });
        Assert.Contains(Current.VehicleWeaponShots, item => item.Vehicle == "de-tank" && item.Weapon == "ma" && item.Step == step);
        Assert.True(Has("de-tank", Conditions.BoundingFire));
        Assert.Equal(UnitFacing.West, ((MapPosition)Current.Find("ru-gun")!.Position).Facing);
        Assert.Null(Current.Choice);
        Assert.Null(Current.GunDuel);
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunDuelsFirstShotThatBreaksTheCrewVoidsTheDeclaredShot()
    {
        // "voids the opponent's return shot by eliminating, breaking, stunning or shocking it": an Original 2 hits on a subsequent dr of 2
        // (C3.6), and the hit's IFT DR gives the 2-2-7 crew a MC it fails. The Gun's shot is not made, and the Gun does not turn.
        var entered = await TankComesOn(guns: RussianGun());
        Committed(await DeclareShot(entered, NoRoll()));
        var before = Revision;
        Committed(await Answer(ChoicePending.Duel, Once(1, 1, 2, 3, 3, 5, 5)));
        var shot = Assert.Single(Shots(before));
        Assert.Equal("de-tank", shot.Gun);
        Assert.True(IsDuelShot(shot));
        Assert.True(shot.Resolution.GetProperty("toHit").GetProperty("hit").GetBoolean(), shot.Resolution.GetRawText());
        Assert.True(Has("ru-crew", Conditions.Broken));
        var voided = Assert.Single(Since(before).Select(item => item.Payload).OfType<GunDuelVoided>());
        Assert.Equal(("ru-gun", "de-tank", "broken"), (voided.Firer, voided.Vehicle, voided.Cause));
        Assert.Equal("gun-duel-voided", Since(before)[^1].Type);
        Assert.Equal(UnitFacing.SouthWest, ((MapPosition)Current.Find("ru-gun")!.Position).Facing);
        Assert.DoesNotContain(Current.OrdnanceShots, item => item.Gun == "ru-gun");
        Assert.Null(Current.GunDuel);
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunStackedWithInfantryIsOfferedNoDuelWhileTheTanksShotAtThemIsNotReviewed()
    {
        // The vehicle's potential shot is planned as the shot itself would be: a hit's attack on a Gun's crew stacked with other Infantry is
        // not reviewed, so its MA cannot fire at that Location, and the declared shot resolves at once.
        var entered = await TankComesOn(guns: [.. RussianGun(), Squad(DuelHex)]);
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6)));
        Assert.Equal("ru-gun", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);
        NoReplayErrors();
    }

    [Fact]
    public async Task AGunDuelsFirstShotThatStopsAtAChoiceStillOwesTheDeclaredShot()
    {
        // A squad mans the Gun (Case H +2 beside Case A +6). The hit's MC is passed on an Original 2, and Heat of Battle offers the squad's
        // owner an option (ruling R5.8): the tank's shot waits for the Russian answer and carries the declared shot, which resolves in the
        // answer's commit.
        var gun = RussianGun();
        gun[0]["kind"] = "asl:squad";
        gun[0]["definition"] = "defender-squad";
        var entered = await TankComesOn(guns: gun);
        Committed(await DeclareShot(entered, NoRoll()));
        Assert.Equal(8, Current.Choice!.Resume.GetProperty("defender").GetProperty("total").GetInt32());
        var before = Revision;
        Committed(await Answer(ChoicePending.Duel, Once(1, 1, 2, 3, 3, 1, 1, 3, 3)));
        Assert.True(Current.Choice is not null, string.Join(" | ", Since(before).Select(item => item.Payload is DiceRolled dice ? $"{dice.Purpose} {string.Join(",", dice.Values)}" : item.Type)));
        var waiting = Current.Choice!;
        Assert.Equal((ChoicePending.BattleHardening, "russian"), (waiting.Kind, waiting.Side));
        Assert.True(waiting.Resume.TryGetProperty("gunDuelOwed", out var owed));
        Assert.Equal("ru-gun", owed.GetProperty("shot").GetProperty("gunId").GetString());
        Assert.Empty(Shots(before));
        Assert.True(Current.GunDuel is { Vehicle: "de-tank", Firer: "ru-gun", Fired: false });

        Committed(await Do(GameActions.Choose, Once(5, 6), new Dictionary<string, object> { ["key"] = waiting.Key, ["option"] = ChoicePending.Decline }));
        var shots = Shots(before);
        Assert.Equal(["de-tank", "ru-gun"], shots.Select(item => item.Gun));
        Assert.True(IsDuelShot(shots[0]));
        Assert.Null(Current.Choice);
        Assert.Null(Current.GunDuel);
        NoReplayErrors();
    }

    [Fact]
    public async Task ARecordThatNamesItselfADuelsFirstShotWithNoDuelDeclaredIsRefused()
    {
        // Once the DEFENDER has passed, the tank fires as Bounding First Fire; the same record with the Gun Duel fact answers no choice, and the
        // projector refuses it. A record without the fact, as every record made before the pass, replays.
        await TankComesOn(guns: RussianGun());
        await PassFire();
        Committed(await FireMa("de-tank", DuelHex, null, 5, 6));
        var events = store.Read(Scope)!.Events;
        Assert.False(Planner().Replay(events).HasErrors);
        Assert.False(IsDuelShot(events.Select(item => item.Payload).OfType<OrdnanceFired>().Single()));
        GameEvent[] forged = [.. events.Select(item =>
        {
            if (item.Payload is not OrdnanceFired fired)
            {
                return item;
            }

            var facts = System.Text.Json.Nodes.JsonNode.Parse(fired.Facts.GetRawText())!.AsObject();
            facts["gunDuel"] = true;
            return item with { Payload = fired with { Facts = JsonSerializer.SerializeToElement(facts) } };
        })];
        var replayed = Planner().Replay(forged);
        Assert.True(replayed.HasErrors);
        Assert.Contains(replayed.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-049");
    }

    [Fact]
    public async Task AConcealedGunsShotIsNotDueled()
    {
        // "Vs a non-concealed ... DEFENDER's declared Defensive First Fire attack": the shot resolves at once, and nothing of the Gun is told.
        var entered = await TankComesOn(guns: RussianGun(concealed: true));
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6)));
        Assert.Equal("ru-gun", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);
        Assert.DoesNotContain(Since(before), item => item.Payload is ChoicePending);
        NoReplayErrors();
    }

    [Fact]
    public async Task EqualOrHigherGunDuelDrmOfferNoDuel()
    {
        // Ruling R55.20: with equal totals the rule lets the Final DRs decide; that is not built, so the shot resolves. In the open the Gun
        // turns three hexspines for Case A +5, against the tank's +5.
        var entered = await TankComesOn(woods: false, guns: RussianGun(facing: "east"));
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6)));
        var shot = Assert.Single(Shots(before));
        Assert.Equal(5m, DrmOf(ToHitDrm(shot), "case-a"));
        Assert.Null(Current.Choice);
        NoReplayErrors();
    }

    [Fact]
    public async Task AnUnrevealedArmorLeaderIsNotCountedAtTheDeclarationAndIsToldNowhere()
    {
        // D3.41, ruling R55.29: in the open the Gun turns three hexspines, Case A +5, against the tank's Case C +4 and Case I +1. Its 9-2 would
        // make that +3, but no record has used him: the totals are equal, no duel is offered, and the shot resolves with no word of him.
        var entered = await TankComesOn(woods: false, leader: "9-2", guns: RussianGun(facing: "east"));
        Assert.False(LeaderOf("de-tank")!.Revealed);
        var before = Revision;
        var declared = await DeclareShot(entered, Once(5, 6));
        Committed(declared);
        Assert.Equal("ru-gun", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);
        Assert.DoesNotContain(Since(before), item => item.Payload is ChoicePending);
        Assert.DoesNotContain(declared.Reasons, reason => reason.Contains("Armor Leader", StringComparison.OrdinalIgnoreCase) || reason.Contains("armor-leader", StringComparison.Ordinal)
            || reason.Contains("Gun Duel", StringComparison.Ordinal));
        Assert.All(Shots(before), shot => Assert.DoesNotContain("armor", shot.Facts.GetRawText() + shot.Resolution.GetRawText(), StringComparison.OrdinalIgnoreCase));
        Assert.False(LeaderOf("de-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task ARevealedArmorLeaderIsCountedInTheGunDuelDrm()
    {
        // The same game after the tank's own MA shot has used and revealed its 9-2 (D3.41): +3 against the Gun's +5, and the duel is offered.
        var entered = await TankComesOn(woods: false, leader: "9-2", fireFirst: true, guns: RussianGun(facing: "east"));
        Assert.True(LeaderOf("de-tank")!.Revealed);
        Committed(await DeclareShot(entered, NoRoll()));
        var choice = Current.Choice!;
        Assert.Equal(ChoicePending.GunDuel, choice.Kind);
        Assert.Equal(3, choice.Resume.GetProperty("attacker").GetProperty("total").GetInt32());
        Assert.Equal(5, choice.Resume.GetProperty("defender").GetProperty("total").GetInt32());
        Assert.Contains("armor-leader", choice.Resume.GetProperty("attacker").GetProperty("lines").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        NoReplayErrors();
    }

    [Fact]
    public async Task ATankWithTheHigherGunDuelDrmIsOfferedNoDuel()
    {
        // The tank began out of the Gun's LOS: Case C2 +6 and Case I +1 against the Gun's +6.
        var entered = await TankComesOn(seen: false, guns: RussianGun());
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6)));
        Assert.Equal("ru-gun", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);
        NoReplayErrors();
    }

    [Fact]
    public async Task NoSecondGunDuelIsOfferedAtTheSameMpExpenditure()
    {
        // Ruling R55.20: the tank keeps its ROF (a colored dr of 1), and a second Gun that must turn in woods declares its shot at the same MP
        // expenditure: the tank's MA has fired at it (C5.3), so the shot resolves at once.
        var entered = await TankComesOn(guns: [.. RussianGun(), .. RussianGun("ru-gun2", "ru-crew2", SecondDuelHex)]);
        Committed(await DeclareShot(entered, NoRoll()));
        Committed(await Answer(ChoicePending.Duel, Once(1, 6, 5, 6)));
        Assert.Contains(Current.OrdnanceShots, item => item.Gun == "de-tank" && item.RateOfFireKept);
        var before = Revision;
        Committed(await DeclareShot(entered, Once(5, 6), "ru-gun2"));
        Assert.Equal("ru-gun2", Assert.Single(Shots(before)).Gun);
        Assert.Null(Current.Choice);

        // After another MP expenditure a duel may be declared again: the first Gun is under its First Fire counter, the second turned.
        await PassFire();
        NoReplayErrors();
    }

    [Fact]
    public async Task AVehiclesDeclaredShotIsDueledOnTheVehicleTargetType()
    {
        // The DEFENDER's T-34 in woods turns its slow turret two hexspines: Case A +3 doubled, +6, and Case I +1: +7. The PzKpfw IIIH has an
        // Armor Leader 9-2 no record has revealed, who is not counted (ruling R55.29): Case C +4, Case I +1: +5. It answers with AP at the T-34
        // on the Vehicle Target Type, and that shot uses and reveals him.
        board.Terrain["J5"] = "Woods";
        var entered = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", Start, "german", "east", "9-2"), Afv("ru-tank", "defender-tank", DuelHex, "russian", "south-east"));
        await Advance();
        Committed(await Drive("start"));
        await PassFire();
        Committed(await Drive("enter", entered));
        Committed(await Do(GameActions.FireOrdnance, NoRoll(), new Dictionary<string, object> { ["gunId"] = "ru-tank", ["target"] = entered, ["targetVehicle"] = "de-tank", ["ammunition"] = "ap" }));
        var choice = Current.Choice!;
        Assert.Equal(ChoicePending.GunDuel, choice.Kind);
        Assert.Equal(5, choice.Resume.GetProperty("attacker").GetProperty("total").GetInt32());
        Assert.Equal(7, choice.Resume.GetProperty("defender").GetProperty("total").GetInt32());
        Assert.DoesNotContain("armor", choice.Resume.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Armor Leader", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        var before = Revision;
        Committed(await Answer(ChoicePending.Duel, Once(5, 6, 5, 6)));
        Assert.Equal(-2m, ExactDrm(ToHitDrm(Shots(before)[0]), "armor-leader"));
        Assert.True(LeaderOf("de-tank")!.Revealed);
        var shots = Shots(before);
        Assert.Equal(["de-tank", "ru-tank"], shots.Select(item => item.Gun));
        Assert.Equal("ru-tank", shots[0].Facts.GetProperty("vehicleTarget").GetProperty("vehicleId").GetString());
        Assert.Equal("ap", shots[0].Facts.GetProperty("ammunition").GetString());
        Assert.True(IsDuelShot(shots[0]));
        NoReplayErrors();
    }

    // The fixes of the pass's five reviews.

    /// <summary>Dice in their order, and a 3 for every die after them.</summary>
    private static DiceRoller Then(params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => queue.Count > 0 ? queue.Dequeue() - 1 : 2);
    }

    /// <summary>A game's events read again with a unit set up under one more condition.</summary>
    private static GameEvent[] SetUpWith(IReadOnlyList<GameEvent> events, string unit, string condition) => [.. events.Select(item =>
        item.Payload is InstanceCreated created && created.Instance.Id == unit
            ? item with
            {
                Payload = created with
                {
                    Instance = created.Instance with
                    {
                        Conditions = new Dictionary<string, ConditionState>(created.Instance.Conditions, StringComparer.Ordinal) { [condition] = ConditionState.True }
                    }
                }
            }
            : item)];

    private string Rolled(long before) => string.Join(" | ", Since(before).Select(item => item.Payload is DiceRolled dice ? $"{dice.Purpose} {string.Join(",", dice.Values)}" : item.Type));

    [Fact]
    public async Task AHiddenVehicleAttemptsNoMotionAndARecordOfOneIsRefused()
    {
        // A12.32 (p. 80; ruling R55.32): "Hidden units may not move (even within their hex or to change a CA)". The game is played with the
        // T-34 in view, and then read again with the T-34 set up hidden: the planner bars the attempt, and the projector refuses its record.
        await TankEntersTheLos();
        var before = (int)Revision;
        Committed(await AttemptMotion(Once(2)));
        var events = store.Read(Scope)!.Events;
        var hidden = SetUpWith(events, "ru-tank", Conditions.Hidden);
        var until = Planner().Replay(hidden[..before]);
        Assert.False(until.HasErrors);
        var option = Assert.Single(Planner().MotionAttemptOptions(until.Current!, hidden[..before], "russian"));
        Assert.Contains("A12.32", option.Bar, StringComparison.Ordinal);
        Assert.Equal("hidden", option.BarShort);

        // The ATTACKER's list holds none of the DEFENDER's vehicles, so the bar tells him nothing of the hidden one.
        Assert.Empty(Planner().MotionAttemptOptions(until.Current!, hidden[..before], "german"));
        var replayed = Planner().Replay(hidden);
        Assert.True(replayed.HasErrors);
        Assert.Contains(replayed.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-048");
    }

    [Fact]
    public async Task ARecordedMotionAttemptByAStunnedVehicleIsRefused()
    {
        // The boundary review: the projector holds the record to the bars of the attempt (D2.401, D5.34). The T-34 fails its attempt; read
        // again with its crew Stunned from its setup, the record is refused.
        await TankEntersTheLos();
        Committed(await AttemptMotion(Once(2)));
        var events = store.Read(Scope)!.Events;
        Assert.False(Planner().Replay(events).HasErrors);
        var replayed = Planner().Replay(SetUpWith(events, "ru-tank", Conditions.Stunned));
        Assert.True(replayed.HasErrors);
        Assert.Contains(replayed.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-048");
    }

    /// <summary>The T-34 under a "?" in the grain of J5 (July: in season), and the PzKpfw IIIH come from E5 into its LOS in a hex of column F.</summary>
    private async Task ConcealedWatcher()
    {
        board.Terrain["J5"] = "Grain";
        los.Blind.Add("E5");
        await Setup(Afv("de-tank", "attacker-tank", Start, "german", "east"), Afv("ru-tank", "defender-tank", Watcher, "russian", "north-east", null, Conditions.Concealed));
        await Advance();
        Committed(await Drive("start"));
        await PassFire();
        Committed(await Drive("enter", Beside("E5", 'F')));
        Assert.True(Has("ru-tank", Conditions.Concealed));
    }

    [Fact]
    public async Task AConcealedVehicleLosesItsConcealmentWhenItsMotionAttemptPasses()
    {
        // A12.141 (p. 78): "Any other activity which a concealed unit engages in ... causes loss of concealment if that unit is currently in
        // the LOS and within 16 hexes of a Good Order enemy ground unit". The passed attempt gains Motion with no change of CA, and the "?" goes.
        await ConcealedWatcher();
        var before = Revision;
        Committed(await AttemptMotion(Once(1, 3, 4)));
        Assert.Equal(MotionAttempted.Passed, LastAttempt(before).Result);
        Assert.Equal("concealment-lost", Since(before)[^1].Type);
        Assert.Equal("ru-tank", Assert.Single(Since(before).Where(item => item.Type == "concealment-lost").Select(item => item.Payload).OfType<ConditionsChanged>()).Id);
        Assert.False(Has("ru-tank", Conditions.Concealed));
        Assert.True(Has("ru-tank", Conditions.Motion));
        NoReplayErrors();
    }

    [Fact]
    public async Task AConcealedVehicleKeepsItsConcealmentWhenItsMotionAttemptFails()
    {
        // D2.401: "There is no penalty (including "?" loss) for failing a Motion attempt dr".
        await ConcealedWatcher();
        var before = Revision;
        Committed(await AttemptMotion(Once(2), facing: "west"));
        Assert.Equal(MotionAttempted.Failed, LastAttempt(before).Result);
        Assert.DoesNotContain(Since(before), item => item.Type == "concealment-lost");
        Assert.True(Has("ru-tank", Conditions.Concealed));
        NoReplayErrors();
    }

    [Fact]
    public async Task ADeclaringVehiclesUnrevealedArmorLeaderIsNotCountedOrToldAtItsDeclaration()
    {
        // D3.41, ruling R55.29 extended (the referee's review): the T-34 in woods turns its turret two hexspines, +6, and Case I +1: +7. Its
        // 9-1 would make that +6, but the declaration rolls nothing and uses nothing of him: the choice both sides read says +7 and no word of
        // him. The declared shot then takes his modifier, and its record reveals him.
        board.Terrain["J5"] = "Woods";
        var entered = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", Start, "german", "east"), Afv("ru-tank", "defender-tank", DuelHex, "russian", "south-east", "9-1"));
        await Advance();
        Committed(await Drive("start"));
        await PassFire();
        Committed(await Drive("enter", entered));
        var declared = await Do(GameActions.FireOrdnance, NoRoll(), new Dictionary<string, object> { ["gunId"] = "ru-tank", ["target"] = entered, ["targetVehicle"] = "de-tank", ["ammunition"] = "ap" });
        Committed(declared);
        var choice = Current.Choice!;
        Assert.Equal(ChoicePending.GunDuel, choice.Kind);
        Assert.Equal(7, choice.Resume.GetProperty("defender").GetProperty("total").GetInt32());
        Assert.DoesNotContain("armor", choice.Resume.GetRawText(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Armor Leader", GamePlanner.DescribeChoice(choice), StringComparison.Ordinal);
        Assert.DoesNotContain("Armor Leader", Assert.Single(declared.Reasons, reason => reason.StartsWith("play.gun-duel:", StringComparison.Ordinal)), StringComparison.Ordinal);
        Assert.False(LeaderOf("ru-tank")!.Revealed);
        Assert.Null(Assert.Single(ViewAs(German).Units, unit => unit.Id == "ru-tank").ArmorLeader);

        // The ATTACKER's proposals of both answers word the declared shot before it is fired: neither names the leader (D3.41).
        static void NamesNoLeader(PlayResult result)
        {
            Assert.Contains(result.Reasons, reason => reason.StartsWith("play.ordnance: ru-tank fires", StringComparison.Ordinal));
            Assert.All(result.Reasons, reason => Assert.DoesNotContain("Armor Leader", reason, StringComparison.OrdinalIgnoreCase));
            Assert.All(result.Reasons, reason => Assert.DoesNotContain("D3.44", reason, StringComparison.Ordinal));
        }

        async Task<PlayResult> ProposeAnswer(string option) => await Play().ProposeAsync(GameActions.Choose, JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["gameId"] = Scope.Game,
            ["attemptId"] = $"answer-{option}",
            ["expectedRevision"] = Revision,
            ["key"] = Current.Choice!.Key,
            ["option"] = option,
        }), Player);
        var duel = await ProposeAnswer(ChoicePending.Duel);
        Assert.Equal(PlayOutcome.NeedsConfirmation, duel.Outcome);
        NamesNoLeader(duel);
        var decline = await ProposeAnswer(ChoicePending.Decline);
        Assert.Equal(PlayOutcome.NeedsConfirmation, decline.Outcome);
        NamesNoLeader(decline);
        Assert.False(LeaderOf("ru-tank")!.Revealed);

        var before = Revision;
        Committed(await Answer(ChoicePending.Decline, Once(5, 6)));
        Assert.Equal(-1m, ExactDrm(ToHitDrm(Assert.Single(Shots(before))), "armor-leader"));
        Assert.True(LeaderOf("ru-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task APendingChoiceHoldsNoMoraleOfAnUnrevealedArmorLeadersCrew()
    {
        // D3.41 (the referee's review): two German squads in B9 fire at B8, which holds a Russian squad and a CE T-34 with a 10-2 no record
        // has revealed. The squad passes its MC on an Original 2, and Heat of Battle gives its owner an option: the attack waits. The
        // choice's resume data, which both sides read, holds no Morale Level of the crew; after the answer the crew's check is made at 10.
        string[] firers = ["g1", "g2"];
        var tank = Afv("ru-tank", "defender-tank", Target, "russian", "south-west", "10-2");
        tank["conditions"] = new Dictionary<string, bool> { [Conditions.ButtonedUp] = false };
        await Setup(Unit("g1", "asl:squad", "attacker-squad", "bd01:B9:0", "german"), Unit("g2", "asl:squad", "attacker-squad", "bd01:B9:0", "german"), Squad(Target), tank);
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(2, 3, 1, 1), new
        {
            firers,
            target = Target
        }));
        Assert.True(Current.Choice is not null, Rolled(before));
        var waiting = Current.Choice!;
        Assert.Equal("russian", waiting.Side);
        Assert.Contains("ru-tank", waiting.Resume.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain("crewMorale", waiting.Resume.GetRawText(), StringComparison.Ordinal);
        Assert.All(Since(before), item => Assert.DoesNotContain("crewMorale", JsonSerializer.Serialize(item.Payload, item.Payload.GetType()), StringComparison.Ordinal));
        Assert.False(LeaderOf("ru-tank")!.Revealed);

        Committed(await Do(GameActions.Choose, Then(), new Dictionary<string, object> { ["key"] = waiting.Key, ["option"] = waiting.Options[^1] }));
        Assert.True(Current.Choice is null, Rolled(before));
        var fire = LastFire(before);
        var crew = fire.Resolution.GetProperty("vehicleEffects").EnumerateArray().Single().GetProperty("crewCheck");
        Assert.Equal((10, "armor-leader"), (crew.GetProperty("moraleLevel").GetInt32(), crew.GetProperty("moraleSource").GetString()));
        Assert.Equal(10, fire.Facts.GetProperty("vehicles").EnumerateArray().Single().GetProperty("crewMorale").GetInt32());
        Assert.True(LeaderOf("ru-tank")!.Revealed);
        NoReplayErrors();
    }

    [Fact]
    public async Task APassedMotionAttemptThatKeepsTheTurretInPlaceKeepsTheAcquisition()
    {
        // C6.5 (p. 174): the Acquisition is lost when "the Gun changes its CA without firing". In its PFPh the PzKpfw IIIH in J5 fires at
        // the T-34 in column F, misses, and acquires it. In the Russian MPh that follows it has fired at nothing; a squad comes out of E5,
        // out of LOS, beside the T-34, and the tank passes a Motion attempt that turns its VCA and names the TCA its turret already has. The
        // turret has not turned, and the Acquisition stays; turning the turret loses it.
        los.Blind.Add("E5");
        var entered = Beside("E5", 'F');
        await Setup(Afv("de-tank", "attacker-tank", Watcher, "german", "west"), Afv("ru-tank", "defender-tank", entered, "russian", "east"),
            Unit("r1", "asl:squad", "defender-squad", Start, "russian"));
        var fired = Revision;
        Committed(await Do(GameActions.FireOrdnance, Then(5, 6, 6, 6), new Dictionary<string, object>
        {
            ["gunId"] = "de-tank",
            ["target"] = entered,
            ["targetVehicle"] = "ru-tank",
            ["ammunition"] = "ap",
        }));
        Assert.True(Current.Acquisitions.Any(item => item.Gun == "de-tank"), Rolled(fired));
        await Advance(9);
        Assert.Equal(("mph", "russian"), (Current.Phase, Current.PhasingSide));
        Assert.Contains(Current.Acquisitions, item => item.Gun == "de-tank");
        string[] mover = ["r1"];
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = mover,
            to = entered
        }));
        var before = Revision;
        Committed(await AttemptMotion(Then(1), facing: "south-west", turret: "west", vehicle: "de-tank"));
        var attempt = LastAttempt(before);
        Assert.Equal((MotionAttempted.Passed, (UnitFacing?)UnitFacing.SouthWest, (UnitFacing?)UnitFacing.West), (attempt.Result, attempt.Facing, attempt.Turret));
        Assert.Equal(UnitFacing.SouthWest, FacingOf("de-tank"));
        Assert.Contains(Current.Acquisitions, item => item.Gun == "de-tank");
        NoReplayErrors();

        // The same record with another TCA: the MA has turned without firing, and the Acquisition is gone.
        GameEvent[] turned = [.. store.Read(Scope)!.Events.Select(item => item.Payload is MotionAttempted motion ? item with { Payload = motion with { Turret = UnitFacing.NorthWest } } : item)];
        Assert.DoesNotContain(Planner().Replay(turned).Current!.Acquisitions, item => item.Gun == "de-tank");
    }

    /// <summary>A game's events with each ordnance record's resolution changed.</summary>
    private static GameEvent[] WithResolution(IReadOnlyList<GameEvent> events, Action<System.Text.Json.Nodes.JsonObject> change) => [.. events.Select(item =>
    {
        if (item.Payload is not OrdnanceFired fired)
        {
            return item;
        }

        var resolution = System.Text.Json.Nodes.JsonNode.Parse(fired.Resolution.GetRawText())!.AsObject();
        change(resolution);
        return item with
        {
            Payload = fired with
            {
                Resolution = JsonSerializer.SerializeToElement(resolution)
            }
        };
    })];

    [Fact]
    public async Task AResolutionNamesItsDepletionNumberAndARecordWithoutItStillReplays()
    {
        // C8.9 (the UI review): the resolution names the number its ammunition's use was decided under, so its record is worded from itself.
        // A record made before that names none: it replays, compared without it; a record that names another number does not.
        await SetupBritish(1944, 6, [], [.. SixPounder(), GermanTank()]);
        var before = Revision;
        Committed(await FireAtTank("apds", 2, 1, 6, 6));
        Assert.Equal(6, LastShot(before).Resolution.GetProperty("depletionNumber").GetInt32());
        NoReplayErrors();
        var events = store.Read(Scope)!.Events;
        Assert.False(Planner().Replay(WithResolution(events, resolution => resolution.Remove("depletionNumber"))).HasErrors);
        Assert.True(Planner().Replay(WithResolution(events, resolution => resolution["depletionNumber"] = 7)).HasErrors);
    }

    [Fact]
    public async Task AVoidReturnShotIsHeldToTheCauseItsFirersStateGives()
    {
        // C2.2401 (the boundary review): Rules decides whether the declared shot is void, for the planner and again for the projector. The
        // crew the first shot broke has a void shot for that cause; the same record under another cause, or for a firer the game shows able
        // to fire, is refused.
        var entered = await TankComesOn(guns: RussianGun());
        Committed(await DeclareShot(entered, NoRoll()));
        var before = Revision;
        Committed(await Answer(ChoicePending.Duel, Once(1, 1, 2, 3, 3, 5, 5)));
        var voided = Assert.Single(Since(before).Select(item => item.Payload).OfType<GunDuelVoided>());
        Assert.Equal(("broken", null), (voided.Cause, voided.Reason));
        NoReplayErrors();
        var events = store.Read(Scope)!.Events;
        GameEvent[] Caused(string cause) => [.. events.Select(item => item.Payload is GunDuelVoided record ? item with { Payload = record with { Cause = cause } } : item)];
        Assert.All(VoidCauses, cause =>
        {
            var replayed = Planner().Replay(Caused(cause));
            Assert.True(replayed.HasErrors);
            Assert.Contains(replayed.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-049");
        });

        // The record's reason, which an "other" cause carries, is written and read back.
        GameEvent[] reasoned = [.. events.Select(item => item.Payload is GunDuelVoided record
            ? item with { Payload = record with { Cause = "other", Reason = "the Gun has no LOS to the target (A6)" } } : item)];
        var read = GameEventReader.Read(System.Text.Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, new GameRecord("Pass 55", false, reasoned))));
        Assert.False(read.HasErrors);
        Assert.Equal("the Gun has no LOS to the target (A6)", Assert.Single(read.Record!.Events.Select(item => item.Payload).OfType<GunDuelVoided>()).Reason);
        var plain = GameEventReader.Read(System.Text.Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, new GameRecord("Pass 55", false, events))));
        Assert.Null(Assert.Single(plain.Record!.Events.Select(item => item.Payload).OfType<GunDuelVoided>()).Reason);
        Assert.DoesNotContain("\"reason\"", GameEventWriter.Write(Scope, new GameRecord("Pass 55", false, events)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task APanzerfaustIsNotFiredAtATargetTwoLevelsUpInAnAdjacentHex()
    {
        // C13.8 (p. 185): "These weapons also may not fire at a target two or more levels higher in an adjacent hex". The T-34 stands on a
        // level 2 hill beside the German squad; C2.6 frees a LATW of its own limit, so C13.8's refusal is the one given. One level up it is
        // not given (the game's date, 1942, then refuses the PF for another reason).
        const string hill = "bd01:B9:0";
        Task<PlayResult> FirePf() => Do(GameActions.FireOrdnance, NoRoll(), new Dictionary<string, object> { ["gunId"] = "g1:pf", ["target"] = hill, ["targetVehicle"] = "ru-tank" });
        board.Base["B9"] = 2;
        await Setup(Unit("g1", "asl:squad", "attacker-squad", Firer, "german"), Afv("ru-tank", "defender-tank", hill, "russian", "south-west"));
        var before = Revision;
        Refused(await FirePf(), "play.latw-elevation", "2 levels higher in an adjacent hex", "C13.8");
        Assert.Equal(before, Revision);

        board.Base["B9"] = 1;
        var lower = await FirePf();
        Assert.DoesNotContain(lower.Reasons, reason => reason.Contains("play.latw-elevation", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ACanisterShotResumedAfterAnOwnersChoiceCallsTheSniperOnceOnItsDr()
    {
        // Pass 71 (ruling R71.13, which supersedes R55.30, the referee's review of pass 55): a Canister shot's IFT DR is an A14.1 trigger. The German SAN is 6
        // and the shot's IFT DR is a 6. The first squad passes its MC on an Original 2, and Heat of Battle gives its owner an option, so the shot stops; the
        // answer resolves that Location and the rest, and the German Sniper attacks once, after them, on the shot's one DR (its dr 3 has no effect).
        Dictionary<string, object>[] placements =
        [
            .. AmericanGun(), .. Squads(),
            new()
            {
                ["id"] = "de-sniper",
                ["kind"] = "asl:sniper",
                ["side"] = "german",
                ["position"] = new Dictionary<string, string> { ["at"] = "bd01:H5:0" },
                ["conditions"] = new Dictionary<string, bool>(),
            },
        ];
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
                ["firstSide"] = "american",
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1943,
                ["sides"] = new object[]
                {
                    new { id = "american", nationality = "american", elr = 3, friendlyEdge = "left" },
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "right", san = 6 },
                },
            },
            placements,
        })));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.FireOrdnance, Then(2, 4, 1, 1), new Dictionary<string, object> { ["gunId"] = "us-gun", ["ammunition"] = "canister", ["aim"] = 4 }));
        Assert.True(Current.Choice is not null, Rolled(before));
        Assert.Empty(Fires(before));
        while (Current.Choice is { } waiting)
        {
            Committed(await Do(GameActions.Choose, Then(), new Dictionary<string, object> { ["key"] = waiting.Key, ["option"] = waiting.Options[^1] }));
        }

        Assert.Equal(Aim4, Fires(before).Select(fire => fire.TargetLocation));
        var sniped = Assert.Single(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-sniper", LastShot(before).Rolls["canister"], "none"), (sniped.Sniper, sniped.Trigger, sniped.Result));
        NoReplayErrors();
    }

    [Fact]
    public async Task AnAimWithNoLocationOfItsClusterToAttackIsRefusedThoughItsAdjacentHexIsThere()
    {
        // C8.41 (the referee's review): the adjacent hex is attacked in addition to the cluster. The three hexes of aim 4's cluster are on a
        // hill, at another level than the Gun: no Location of the cluster is attacked, so the squad in the adjacent hex is not either, and
        // no round is spent.
        foreach (var hex in new[] { Aim4[0], Aim4[1], Aim4[2] })
        {
            board.Base[hex.Split(':')[1]] = 1;
        }

        await SetupAmerican(1943, 7, [.. AmericanGun(), .. Squads()]);
        var before = Revision;
        Refused(await FireCanister(4, null, 2, 4), "play.canister-aim", "the adjacent hex on its LOS included (C8.4, C8.41)");
        Assert.Equal(before, Revision);

        // The page offers the aim still: its cluster is on the map. An aim whose cluster is off the map is not offered.
        Assert.Contains(Planner().CanisterAims(Current, "us-gun"), aim => aim.Aim == 4);
        Assert.Empty(Current.DepletedAmmunition);
    }
}
