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
        Refused(await FireAtSquad("ap", 3, 3), "asl.a1.ordnance.he-not-named");
    }

    [Fact]
    public async Task TheSixPounderHasNoHeIn1942()
    {
        // British Ordnance Note 6 (p. 426): HE from February 1943. Before it the Gun fires nothing at Infantry: AP there is C8.31's.
        await SetupBritish(1942, 6, [], [.. SixPounder(), GermanSquad()]);
        Refused(await FireAtSquad("he", 3, 3), "asl.a1.ordnance.he-unavailable", "Special Ammunition (C8.8) and it has none now", "C8.31, HE Equivalency");
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
        Refused(await Do(GameActions.FireOrdnance, NoRoll(), new { gunId = "us-gun", ammunition = "canister" }), "play.canister-aim");
        Refused(await FireCanister(4, null, 1, 1), "asl.a1.ordnance.ammunition-unavailable");
    }
}
