using System.Text.Json;
using System.Text;
using ASL.Turns;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 9 in live play: a light mortar on the Area Target Type with a Spotter (R9.2 to R9.4), SMOKE grenades (R9.5, R9.6), and
/// the Panzerfaust (R9.7, R9.8). Board 01's hex facts, fixed dice, and a stub LOS reader.
/// </summary>
public sealed class BacklogPass9Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f509");
    private static readonly GameScope Scope = new(Tenant, "pass9");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G3 = ["g3"];
    private static readonly string[] GH = ["gh"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] RussianView = ["russian"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] R4R5 = ["r4", "r5"];
    private static readonly string[] R1R2 = ["r1", "r2"];
    private static readonly string[] R3 = ["r3"];
    private static readonly string[] HalftrackIds = ["de-ht"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass9-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly StubLos los = new();
    private IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public BacklogPass9Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Clear LOS at the board's true range, except to the Locations named blocked.
    private sealed class StubLos : IFireLosReader
    {
        private static readonly BoardHandle Board = Board01Fixture.Handle();

        public HashSet<BoardLocation> Blocked { get; } = [];

        public Dictionary<(string From, string To), LosCrossedHex[]> Crossed { get; } = [];

        public Dictionary<(string From, string To), LosHindrance[]> Hindrances { get; } = [];

        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) => Blocked.Contains(target) || Blocked.Contains(from)
            ? new(LosStatus.Blocked, true, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty)
            : new(LosStatus.Clear, false, Board.Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty)
            {
                Crossed = Crossed.GetValueOrDefault((from.Hex.ToString(), target.Hex.ToString())) ?? [],
                Hindrances = Hindrances.GetValueOrDefault((from.Hex.ToString(), target.Hex.ToString())) ?? [],
            };
    }

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog], fireLos: los);

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

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at
            },
            ["conditions"] = conditions
        };
    }

    private static Dictionary<string, object> Squad(string id, string at, string side, params string[] states) =>
        Unit(id, "asl:squad", side == "german" ? "attacker-squad" : "defender-squad", at, side, states);

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, string facing = "east", params string[] states)
    {
        var conditions = new Dictionary<string, bool>();
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = "asl:vehicle",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = facing },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Gun(string id, string definition, string at, string facing, string crew, string side) => new()
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
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task Setup(string firstSide, int? month, int? year, params Dictionary<string, object>[] placements)
    {
        var start = new Dictionary<string, object>
        {
            ["label"] = "Pass 9",
            ["catalog"] = "asl-scenario-a1@1.13.0",
            ["boards"] = Bd01,
            ["firstSide"] = firstSide,
            ["sides"] = new object[]
            {
                new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
            },
        };
        if (month is { } value)
        {
            start["scenarioMonth"] = value;
        }

        if (year is { } scenarioYear)
        {
            start["scenarioYear"] = scenarioYear;
        }

        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start,
            placements,
        })));
    }

    private async Task Advance(int times = 1)
    {
        for (var index = 0; index < times; index++)
        {
            Committed(await Do(GameActions.AdvancePhase, NoRoll(), new
            {
            }));
        }
    }

    private async Task<PlayResult> Do(Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] ??= $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(roller), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Move(string[] units, string to, bool doubleTime = false, DiceRoller? roller = null) =>
        Do(GameActions.Move, roller ?? NoRoll(), new
        {
            unitIds = units,
            to,
            doubleTime
        });

    private async Task Pass() => Committed(await Do(GameActions.PassFire, NoRoll(), new
    {
    }));

    private async Task EndMove() => Committed(await Do(GameActions.EndMove, NoRoll(), new
    {
    }));

    private Task<PlayResult> Step(string vehicle, string kind, string? to = null, string? facing = null)
    {
        var arguments = new Dictionary<string, string> { ["vehicleId"] = vehicle, ["kind"] = kind };
        if (to is not null)
        {
            arguments["to"] = to;
        }

        if (facing is not null)
        {
            arguments["facing"] = facing;
        }

        return Do(GameActions.MoveVehicle, NoRoll(), arguments);
    }

    private Task<PlayResult> Choose(string key, string option, DiceRoller? roller = null) => Do(GameActions.Choose, roller ?? NoRoll(), new
    {
        key,
        option
    });

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string code)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains(code, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static BoardLocation At(string location) => BoardLocation.Parse(location);

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private static Dictionary<string, object> Dummy(string id, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    private async Task<PlayResult> TrySetup(int month, params Dictionary<string, object>[] placements) => await Commit(Play(), GameActions.Setup,
        JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Guid.NewGuid():N}",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 9",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = "german",
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3, friendlyEdge = "left" },
                    new { id = "russian", nationality = "russian", elr = 2, friendlyEdge = "right" },
                },
                ["scenarioMonth"] = month,
            },
            placements,
        }));

    private Task<PlayResult> Fire(string[] firers, string target, params int[] dice) => Do(GameActions.Fire, Once(dice), new
    {
        firers,
        target
    });

    // Ends the moving vehicle's move: its MP left are spent in its hex, and the DEFENDER passes (R5.15).
    private async Task FinishVehicle()
    {
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        if (Current.Movement is { WindowOpen: true })
        {
            await Pass();
        }
    }

    private static JsonElement Drm(FireResolved record) => record.Resolution.GetProperty("arithmetic").GetProperty("drm");

    private static bool HasDrm(FireResolved record, string name, decimal value) =>
        Drm(record).EnumerateArray().Any(item => item.GetProperty("name").GetString() == name && item.GetProperty("value").GetDecimal() == value);

    private static LosCrossedHex Crossing(string hex, int range) => new(At($"bd01:{hex}:0").Board, At($"bd01:{hex}:0").Hex, range);

    private FireResolved LastFire(long before) => Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();

    private Task<PlayResult> FireAt(string gun, string target, string? vehicle, string? ammunition, params int[] dice) =>
        FireAt(gun, target, vehicle, ammunition, false, dice);

    private Task<PlayResult> FireAt(string gun, string target, string? vehicle, string? ammunition, bool intensive, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = target };
        if (intensive)
        {
            arguments["intensive"] = true;
        }

        if (vehicle is not null)
        {
            arguments["targetVehicle"] = vehicle;
        }

        if (ammunition is not null)
        {
            arguments["ammunition"] = ammunition;
        }

        return Do(GameActions.FireOrdnance, dice.Length == 0 ? NoRoll() : Once(dice), arguments);
    }

    private OrdnanceResolution LastShot(long before) =>
        Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Resolution.Deserialize<OrdnanceResolution>(LiveFire.Json)!;

    private static Dictionary<string, object> Crew(string id, string at, string side) => Unit(id, "asl:crew", side == "german" ? "attacker-crew" : "defender-crew", at, side);

    private OrdnanceShot LastFacts(long before) =>
        Since(before).Select(item => item.Payload).OfType<OrdnanceFired>().Single().Facts.Deserialize<OrdnanceShot>(LiveFire.Json)!;

    private static Dictionary<string, object> Mortar(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:light-mortar",
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private Task<PlayResult> Shoot(string gun, string target, string? spotter, string? director, params int[] dice)
    {
        var arguments = new Dictionary<string, object> { ["gunId"] = gun, ["target"] = target };
        if (spotter is not null)
        {
            arguments["spotter"] = spotter;
        }

        if (director is not null)
        {
            arguments["director"] = director;
        }

        return Do(GameActions.FireOrdnance, dice.Length == 0 ? NoRoll() : Once(dice), arguments);
    }

    private Task<PlayResult> Smoke(string[] units, string at, string by, string target, params int[] dice) => Do(GameActions.Move, Once(dice), new
    {
        unitIds = units,
        to = at,
        smoke = target,
        smokeBy = by
    });

    [Fact]
    public async Task ALightMortarHitsTheAreaAndItsSquadStillFiresItsInherentFp()
    {
        // C3.33, C3.331, B13.3 (R9.2, R9.3): the German squad's 5cm mortar in B10 fires at the Russian squad in woods B6, 4 hexes: TH# 7 (red
        // Area row), no TEM on the To Hit DR; the hit attacks on the 2 column with -1 for Air Bursts. The colored 2 keeps ROF 3; the squad is
        // marked Prep Fire but, its mortar being its one SW, still fires its inherent FP (A7.351).
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        var before = Revision;
        Committed(await Shoot("de-mtr", "bd01:B6:0", null, null, 2, 3, 5, 6));
        var facts = LastFacts(before);
        var shot = LastShot(before);
        Assert.Equal((OrdnanceTargetTypes.Area, 4, 7), (facts.TargetType, facts.Range, shot.ToHit!.ModifiedToHit));
        Assert.True(shot.AreaTargets!.Single().Hit);
        Assert.Contains(shot.Hit!.Arithmetic!.Drm, item => item.Name == "air-burst" && item.Value == -1);
        Assert.Equal(2, shot.Hit.Arithmetic.ColumnFp);
        Assert.True(shot.Gun!.RateOfFireKept);
        Assert.True(Is(Current.Unit("g1")!, Conditions.PrepFire));
        Assert.Contains(Current.SupportWeaponUses, item => item.Unit == "g1" && item.Weapon == "de-mtr");
        Committed(await Fire(G1, "bd01:B6:0", 6, 6));
        Assert.Empty(Current.SupportWeaponUses);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.FireProfiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task ASpottedMortarFiresBeyondItsOwnLosAndKeepsItsSpotter(string profile)
    {
        // C9.3, C9.31 (R9.4): from B10, with no LOS of its own, the mortar fires at B6 along the LOS of the HS in adjacent B9: +2 and ROF 2. The HS
        // loses its fire for the phase; another unit may not spot for the mortar while the HS is Good Order.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"),
            Unit("g2", "asl:half-squad", "attacker-half-squad", "bd01:B9:0", "german"), Squad("g3", "bd01:B9:0", "german"), Squad("r1", "bd01:B6:0", "russian"));
        los.Blocked.Add(At("bd01:B10:0"));
        await Advance();
        Refused(await Shoot("de-mtr", "bd01:B6:0", null, null, 5, 6), "play.");
        var before = Revision;
        Committed(await Shoot("de-mtr", "bd01:B6:0", "g2", null, 2, 6));
        var shot = LastShot(before);
        Assert.Contains(shot.ToHit!.Drm, item => item.Name == "spotted" && item.Value == 2);
        Assert.Equal(2, shot.Gun!.RateOfFire);
        Assert.Contains(Current.MortarSpotters, item => item.Gun == "de-mtr" && item.Spotter == "g2");
        Assert.True(Is(Current.Unit("g2")!, Conditions.PrepFire));
        Refused(await Shoot("de-mtr", "bd01:B6:0", "g3", null, 2, 6), "play.ordnance-spotter-kept");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
        var shotRevision = Revision;
        var guide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile);
        var history = guide.FireHistory!;
        var record = Assert.Single(history.Shots);
        Assert.Equal(TurnFireKind.Ordnance, record.Kind);
        Assert.Equal("g1", Assert.Single(record.Firers));
        Assert.Equal("de-mtr", record.Weapon);
        Assert.Equal("g2", record.Spotter);
        Assert.True(record.RateOfFireKept);
        Assert.True(record.InCurrentPhase);
        Assert.Equal(TurnSpotterState.Retained, Assert.Single(history.Spotters).State);
        Assert.True(Assert.Single(history.Spotters).SpottedThisPhase);
        Assert.NotEmpty(history.EvaluationGaps);
        var enemy = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "pfph", profile);
        Assert.Empty(enemy.FireHistory!.Shots);
        Assert.Empty(enemy.FireHistory.Spotters);
        Assert.Null((await Planner().ReadTurnGuideAsync(Scope, Perspective.Side("german"), "pfph")).FireHistory);
        Assert.Equal(JsonSerializer.Serialize(guide), JsonSerializer.Serialize(await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)));
        Assert.Equal(shotRevision, Revision);
        var written = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(written)).Record!;
        Assert.Equal(written, GameEventWriter.Write(Scope, loaded));
        var copyStore = new FileGameStore(Path.Combine(root, "fire-copy"));
        var copyPlanner = new GamePlanner(copyStore, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, copyStore.Append(Scope, loaded.Label, 0, loaded.Events, copyPlanner.Replay).Status);
        Assert.Equal(AppendStatus.Replay, copyStore.Append(Scope, loaded.Label, 0, loaded.Events, copyPlanner.Replay).Status);
        Assert.Equal(JsonSerializer.Serialize(history), JsonSerializer.Serialize((await copyPlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory));
        var withheld = loaded.Events.Select(item => item.Payload is OrdnanceFired ? item with { Visibility = RussianView } : item).ToArray();
        var privateStore = new FileGameStore(Path.Combine(root, "private-fire-copy"));
        var privatePlanner = new GamePlanner(privateStore, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, privateStore.Append(Scope, loaded.Label, 0, withheld, privatePlanner.Replay).Status);
        var undisclosed = (await privatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!;
        Assert.Empty(undisclosed.Shots);
        Assert.Empty(undisclosed.Spotters);
        Assert.Single((await privatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "pfph", profile)).FireHistory!.Shots);
        await Advance();
        var next = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", profile)).FireHistory!;
        Assert.False(Assert.Single(next.Shots).InCurrentPhase);
        Assert.False(Assert.Single(next.Spotters).SpottedThisPhase);
        Assert.Equal(TurnSpotterState.Retained, Assert.Single(next.Spotters).State);
        var old = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile, shotRevision);
        Assert.Equal(JsonSerializer.Serialize(history), JsonSerializer.Serialize(old.FireHistory));
        Assert.Equal(TurnCloseDisposition.ReadOnly, old.PhaseClose.Disposition);
        // CCPh may require concealment dice on this board; only the governed command draws them.
        for (var phase = 0; phase < 15; phase++)
        {
            Committed(await Do(GameActions.AdvancePhase, new DiceRoller(_ => 5), new
            {
            }));
        }
        Assert.Equal("pfph", Current.Phase);
        var later = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!;
        Assert.False(Assert.Single(later.Shots).InCurrentPhase);
        var beforeHiddenShot = Revision;
        Committed(await Shoot("de-mtr", "bd01:B6:0", "g2", null, 5, 6));
        var laterPrivateStore = new FileGameStore(Path.Combine(root, "later-private-shot"));
        var laterPrivatePlanner = new GamePlanner(laterPrivateStore, boards, Vocabulary, [Catalog], fireLos: los);
        var laterPrivateEvents = store.Read(Scope)!.Events.Select(item => item.Revision > beforeHiddenShot && item.Payload is OrdnanceFired
            ? item with
            {
                Visibility = RussianView
            } : item).ToArray();
        Assert.Equal(AppendStatus.Committed, laterPrivateStore.Append(Scope, "private later shot", 0, laterPrivateEvents, laterPrivatePlanner.Replay).Status);
        var privateUse = (await laterPrivatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!;
        Assert.Single(privateUse.Shots);
        Assert.False(Assert.Single(privateUse.Spotters).SpottedThisPhase);
        Assert.True(Assert.Single((await laterPrivatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "pfph", profile)).FireHistory!.Spotters).SpottedThisPhase);
        var last = store.Read(Scope)!.Events[^1];
        var loss = last with
        {
            EventId = "spotter-broken",
            Revision = last.Revision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g2", new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.True }),
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "spotter-loss", Revision, [loss], Planner().Replay).Status);
        var lost = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", profile)).FireHistory!;
        Assert.Equal(TurnSpotterState.Lost, Assert.Single(lost.Spotters).State);
        Assert.NotEmpty(lost.EvaluationGaps);
        var retainedLoss = Assert.Single(Current.MortarSpotterLosses);
        Assert.Equal(TurnSpotterChange.Broken, retainedLoss.Change);
        Assert.Equal("spotter-broken", retainedLoss.EventId);
        Assert.Equal("pfph", retainedLoss.Occurrence!.Phase);
        var rallied = loss with
        {
            EventId = "spotter-rallied",
            Revision = loss.Revision + 1,
            Payload = new ConditionsChanged("g2", new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.False }),
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "spotter-rally", Revision, [rallied], Planner().Replay).Status);
        Assert.False(Is(Current.Unit("g2")!, Conditions.Broken));
        Assert.Equal(retainedLoss, Assert.Single(Current.MortarSpotterLosses));
        var afterRallyRevision = Revision;
        Refused(await Shoot("de-mtr", "bd01:B6:0", "g2", null), "play.ordnance-spotter-lost");
        Refused(await Shoot("de-mtr", "bd01:B6:0", "g3", null), "play.ordnance-spotter-lost");
        Assert.Equal(afterRallyRevision, Revision);
        Assert.Equal(TurnSpotterState.Lost, Assert.Single((await Planner().ReadTurnGuideProfileAsync(Scope,
            Perspective.Side("german"), "pfph", profile)).FireHistory!.Spotters).State);
        var afterRally = store.Read(Scope)!;
        var roundTrip = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, afterRally))).Record!;
        Assert.Equal(retainedLoss, Assert.Single(Planner().Replay(roundTrip.Events).Current!.MortarSpotterLosses));
        var hiddenLossStore = new FileGameStore(Path.Combine(root, "hidden-spotter-loss"));
        var hiddenLossPlanner = new GamePlanner(hiddenLossStore, boards, Vocabulary, [Catalog], fireLos: los);
        var hiddenLossEvents = roundTrip.Events.Select(item => item.EventId == loss.EventId ? item with { Visibility = RussianView } : item).ToArray();
        Assert.Equal(AppendStatus.Committed, hiddenLossStore.Append(Scope, "hidden loss", 0, hiddenLossEvents, hiddenLossPlanner.Replay).Status);
        Assert.Equal(TurnSpotterState.NotEvaluated, Assert.Single((await hiddenLossPlanner.ReadTurnGuideProfileAsync(Scope,
            Perspective.Side("german"), "pfph", profile)).FireHistory!.Spotters).State);
        Assert.Equal(TurnSpotterState.Lost, Assert.Single((await hiddenLossPlanner.ReadTurnGuideProfileAsync(Scope,
            Perspective.Adjudicator, "pfph", profile)).FireHistory!.Spotters).State);
        Assert.Equal(TurnSpotterState.Retained, Assert.Single((await Planner().ReadTurnGuideProfileAsync(Scope,
            Perspective.Side("german"), "pfph", profile, shotRevision)).FireHistory!.Spotters).State);
    }

    [Theory]
    [InlineData(false, ConditionState.False, true)]
    [InlineData(true, ConditionState.False, false)]
    [InlineData(false, ConditionState.True, false)]
    [InlineData(false, ConditionState.Unknown, false)]
    public async Task ReducedSpotterRetainsOnlyItsProvenGoodOrderHalfSquad(bool priorLoss, ConditionState broken, bool retained)
    {
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"),
            Squad("g2", "bd01:B9:0", "german"), Squad("r1", "bd01:B6:0", "russian"));
        los.Blocked.Add(At("bd01:B10:0"));
        await Advance();
        Committed(await Shoot("de-mtr", "bd01:B6:0", "g2", null, 2, 6));
        void Append(string id, string type, EventPayload payload)
        {
            var item = store.Read(Scope)!.Events[^1] with
            {
                EventId = id,
                Revision = Revision + 1,
                Type = type,
                Payload = payload
            };
            Assert.Equal(AppendStatus.Committed, store.Append(Scope, id, Revision, [item], Planner().Replay).Status);
        }
        if (priorLoss)
        {
            Append("spotter-break", "conditions-changed", new ConditionsChanged("g2",
                new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.True }));
        }
        var conditions = new Dictionary<string, ConditionState>(Current.Unit("g2")!.Conditions)
        {
            [Conditions.Broken] = broken,
            [Conditions.Berserk] = ConditionState.False,
            [Conditions.Captured] = ConditionState.False,
            [Conditions.Melee] = ConditionState.False,
        };
        Append("spotter-reduced", "lineage", new LineageRecorded(LineageAction.Reduced, ["g2"],
            [new NewInstance("gh", "asl:half-squad", "attacker-half-squad", "german", null, null, conditions)]));
        const string profile = "existing-engine-fire-v1";
        var history = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!;
        var assignment = Assert.Single(history.Spotters);
        Assert.Equal(retained ? "gh" : "g2", Assert.Single(Current.MortarSpotters).Spotter);
        Assert.Equal(retained ? TurnSpotterState.Retained : broken == ConditionState.Unknown
            ? TurnSpotterState.NotEvaluated : TurnSpotterState.Lost, assignment.State);
        Assert.Equal(retained ? "gh" : null, assignment.Successor);
        if (!retained)
        {
            Assert.Empty(Current.MortarSpotterSuccessors);
            return;
        }
        Assert.Equal("spotter-reduced", Assert.Single(Current.MortarSpotterSuccessors).EventId);
        Assert.Contains(Current.SpottedThisPhase, item => item.Spotter == "gh");
        var events = store.Read(Scope)!.Events;
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, store.Read(Scope)!))).Record!;
        Assert.Equal(Assert.Single(Current.MortarSpotterSuccessors), Assert.Single(Planner().Replay(loaded.Events).Current!.MortarSpotterSuccessors));
        var privateStore = new FileGameStore(Path.Combine(root, "private-succession"));
        var privatePlanner = new GamePlanner(privateStore, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, privateStore.Append(Scope, "private succession", 0,
            events.Select(item => item.EventId == "spotter-reduced" ? item with { Visibility = RussianView } : item).ToArray(), privatePlanner.Replay).Status);
        var hidden = Assert.Single((await privatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!.Spotters);
        Assert.Equal(TurnSpotterState.NotEvaluated, hidden.State);
        Assert.Null(hidden.Successor);
        Committed(await Shoot("de-mtr", "bd01:B6:0", "gh", null, 2, 6));
        Assert.Equal("gh", Assert.Single((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", profile)).FireHistory!.Spotters).Successor);
    }

    [Fact]
    public async Task GuardDeploymentCarriesSpottingUseAndMovementRestrictionToItsSuccessors()
    {
        var spotter = Squad("g2", "bd01:B9:0", "german");
        var conditions = (Dictionary<string, bool>)spotter["conditions"];
        conditions[Conditions.Berserk] = false;
        conditions[Conditions.Captured] = false;
        conditions[Conditions.Melee] = false;
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"),
            Mortar("m1", "attacker-light-mortar", "g1", "german"), spotter,
            Unit("r1", "asl:half-squad", "defender-half-squad", "bd01:B9:0", "russian"));
        var captured = store.Read(Scope)!.Events[^1] with
        {
            EventId = "fixture-prisoner",
            Revision = Revision + 1,
            Type = "instance-captured",
            Payload = new InstanceCaptured("r1", "g2")
        };
        Assert.Equal(AppendStatus.Committed, store.Append(Scope, "guard fixture", Revision, [captured], Planner().Replay).Status);
        await Advance();
        Committed(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }));
        Assert.Contains("g2", Current.NoMoveThisPlayerTurn);
        var request = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "guard-spotter-deploy",
            expectedRevision = Revision,
            squadId = "g2",
            spotterHalf = "second"
        });
        Committed(await Commit(Play(), GameActions.Deploy, request));
        var chosen = Assert.Single(Current.MortarSpotters).Spotter;
        Assert.Equal("guard-spotter-deploy-g2-2", chosen);
        Assert.Contains(Current.SpottedThisPhase, item => item.Spotter == chosen);
        Assert.Contains(Current.SupportWeaponUses, item => item.Unit == chosen);
        Assert.Contains("guard-spotter-deploy-g2-1", Current.NoMoveThisPlayerTurn);
        Assert.Contains(chosen, Current.NoMoveThisPlayerTurn);
        Assert.Equal(GamePlanStatus.Replay, (await Planner().PlanAsync(GameActions.Deploy, request, Tenant)).Status);
        var changed = JsonSerializer.SerializeToNode(request)!.AsObject();
        changed["spotterHalf"] = "first";
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.Deploy, JsonSerializer.SerializeToElement(changed), Tenant)).Status);
        await Advance();
        Assert.Contains(chosen, Current.NoMoveThisPlayerTurn);
    }

    [Theory]
    [InlineData("first", true)]
    [InlineData("second", true)]
    [InlineData("second", false)]
    public async Task DeployingSpotterChoosesOneHalfSquadAndBindsTheChoiceBeforeTheRoll(string selected, bool passed)
    {
        var spotter = Squad("g2", "bd01:B9:0", "german");
        var conditions = (Dictionary<string, bool>)spotter["conditions"];
        conditions[Conditions.Berserk] = false;
        conditions[Conditions.Captured] = false;
        conditions[Conditions.Melee] = false;
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"),
            Mortar("m1", "attacker-light-mortar", "g1", "german"), spotter,
            Unit("leader", "asl:leader", "attacker-leader-8-1", "bd01:B9:0", "german"),
            Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        await Advance(15);
        Assert.Equal("rph", Current.Phase);
        Assert.Equal("german", Current.PhasingSide);
        var before = Revision;
        Refused(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "g2",
            leader = "leader"
        }), "play.deploy-spotter");
        Refused(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "g2",
            leader = "leader",
            spotterHalf = "third"
        }), "play.deploy-spotter");
        Assert.Equal(before, Revision);
        var request = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "chosen-deployment",
            expectedRevision = before,
            squadId = "g2",
            leader = "leader",
            spotterHalf = selected,
            proposedBy = "german"
        });
        Committed(await Commit(Play(passed ? Once(1, 1) : Once(6, 6)), GameActions.Deploy, request));
        Assert.Equal(selected, Assert.Single(Since(before).Select(item => item.Payload).OfType<DeploymentAttempted>()).SpotterHalf);
        Assert.All(Since(before).Where(item => item.Payload is DeploymentAttempted or LineageRecorded), item =>
        {
            Assert.False(item.IsVisibleTo(Perspective.Side("russian")));
            Assert.True(item.IsVisibleTo(Perspective.Side("german")));
            Assert.True(item.IsVisibleTo(Perspective.Adjudicator));
        });
        Assert.Equal(GamePlanStatus.Replay, (await Planner().PlanAsync(GameActions.Deploy, request, Tenant)).Status);
        var changed = JsonSerializer.SerializeToNode(request)!.AsObject();
        changed["spotterHalf"] = selected == "first" ? "second" : "first";
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.Deploy, JsonSerializer.SerializeToElement(changed), Tenant)).Status);
        var written = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(written)).Record!;
        Assert.Equal(written, GameEventWriter.Write(Scope, loaded));
        Assert.False(Planner().Replay(loaded.Events).HasErrors);
        if (!passed)
        {
            Assert.Empty(Current.MortarSpotterSuccessors);
            Assert.Equal("g2", Assert.Single(Current.MortarSpotters).Spotter);
            return;
        }
        var lineageEvent = Assert.Single(Since(before), item => item.Payload is LineageRecorded);
        var lineage = (LineageRecorded)lineageEvent.Payload;
        var successor = lineage.Produced[selected == "first" ? 0 : 1].Id;
        Assert.Equal(successor, lineage.SpotterSuccessor);
        Assert.Equal(successor, Assert.Single(Current.MortarSpotters).Spotter);
        Assert.Equal(successor, Assert.Single(Current.MortarSpotterSuccessors).Successor);
        var snapshot = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "rph", "existing-engine-fire-v1")).FireHistory!;
        Assert.Equal(successor, Assert.Single(snapshot.Spotters).Successor);
        Assert.Equal(TurnSpotterState.Retained, Assert.Single(snapshot.Spotters).State);
        // Each altered journal is built from one read, and the lineage event is found by its id. FileGameStore's parse cache is process-wide and
        // cleared when full, so a second read made while other tests run may parse new objects; record equality (by reference for the payload's
        // lists) then matched nothing, and the "altered" journal was the original one (the flaky failures at the switched replay and the hidden read).
        var journal = store.Read(Scope)!.Events;
        GameEvent[] Altered(Func<GameEvent, GameEvent> change)
        {
            var altered = journal.Select(item => item.EventId == lineageEvent.EventId ? change(item) : item).ToArray();
            Assert.Single(altered.Where((item, index) => !ReferenceEquals(item, journal[index])));
            return altered;
        }
        GameEvent[] WithSuccessor(string? spotterSuccessor) => Altered(item => item with
        {
            Payload = lineage with
            {
                SpotterSuccessor = spotterSuccessor
            }
        });
        void RefusedSuccession(GameEvent[] events) => Assert.Contains(Planner().Replay(events).Diagnostics,
            item => item.Severity == UnitDiagnosticSeverity.Error && item.Code == "UNIT-SPOTTER-SUCCESSION");
        RefusedSuccession(WithSuccessor("unrelated"));
        RefusedSuccession(WithSuccessor(lineage.Produced.Single(half => half.Id != successor).Id));
        Assert.Empty(Current.SpotterDeploymentChoices);
        var legacyStore = new FileGameStore(Path.Combine(root, "legacy-deployment"));
        var legacyPlanner = new GamePlanner(legacyStore, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, legacyStore.Append(Scope, "legacy", 0, WithSuccessor(null), legacyPlanner.Replay).Status);
        Assert.Equal(TurnSpotterState.NotEvaluated, Assert.Single((await legacyPlanner.ReadTurnGuideProfileAsync(Scope,
            Perspective.Side("german"), "rph", "existing-engine-fire-v1")).FireHistory!.Spotters).State);
        var hiddenStore = new FileGameStore(Path.Combine(root, "private-deployment"));
        var hiddenPlanner = new GamePlanner(hiddenStore, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, hiddenStore.Append(Scope, "private", 0,
            Altered(item => item with { Visibility = RussianView }), hiddenPlanner.Replay).Status);
        var hidden = Assert.Single((await hiddenPlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "rph", "existing-engine-fire-v1")).FireHistory!.Spotters);
        Assert.Null(hidden.Successor);
        Assert.Equal(TurnSpotterState.NotEvaluated, hidden.State);
        Assert.Equal(successor, Assert.Single((await hiddenPlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "rph", "existing-engine-fire-v1")).FireHistory!.Spotters).Successor);
        await Advance();
        Committed(await Shoot("m1", "bd01:B6:0", successor, null, 2, 6));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OriginalSpotterDeclarationHasGovernedProvenanceBeforeFiring(bool concealedMortar)
    {
        var spotter = Unit("g2", "asl:half-squad", "attacker-half-squad", "bd01:B9:0", "german");
        var known = (Dictionary<string, bool>)spotter["conditions"];
        known[Conditions.Berserk] = false;
        known[Conditions.Captured] = false;
        known[Conditions.Melee] = false;
        await Setup("german", 7, 1942,
            Squad("g1", "bd01:B10:0", "german", concealedMortar ? [Conditions.Concealed] : []),
            Mortar("m1", "attacker-light-mortar", "g1", "german"),
            spotter,
            Squad("g3", "bd01:B3:0", "german"), Squad("r1", "bd01:B6:0", "russian"));
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }), "play.spotter");
        await Advance();
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g3"
        }), "play.spotter");
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2",
            proposedBy = "russian"
        }), "play.");
        var before = Revision;
        var request = JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "original-spotter",
            expectedRevision = before,
            gunId = "m1",
            spotter = "g2",
            proposedBy = "german"
        });
        var offered = await Planner().PlanAsync(GameActions.DesignateSpotter, request, Tenant);
        Assert.True(offered.Status == GamePlanStatus.Ready, string.Join("; ", offered.Reasons));
        Assert.Equal(before, Revision);
        Committed(await Commit(Play(), GameActions.DesignateSpotter, request));
        var declaration = Assert.Single(Current.MortarSpotterDesignations);
        Assert.Equal("g2", declaration.Spotter);
        Assert.Equal("german", declaration.Owner);
        Assert.Equal("pfph", declaration.Occurrence.Phase);
        Assert.True(Is(Current.Unit("g2")!, Conditions.PrepFire));
        Assert.Empty(Current.MortarSpotterUses);
        var disclosed = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", "existing-engine-fire-v1")).FireHistory!;
        Assert.Equal("g2", Assert.Single(disclosed.Declarations).Spotter);
        Assert.Empty(disclosed.Shots);
        Assert.Empty(disclosed.Spotters);
        Assert.Empty((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "pfph", "existing-engine-fire-v1")).FireHistory!.Declarations);
        Assert.Single((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "pfph", "existing-engine-fire-v1")).FireHistory!.Declarations);
        Assert.Empty((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", "existing-engine-fire-v1", before)).FireHistory!.Declarations);
        var recorded = store.Read(Scope)!.Events[^1];
        Assert.IsType<MortarSpotterDesignated>(recorded.Payload);
        Assert.Equal(!concealedMortar, recorded.IsVisibleTo(Perspective.Side("russian")));
        Assert.True(recorded.IsVisibleTo(Perspective.Adjudicator));
        foreach (var withheldId in new[] { recorded.EventId, declaration.Occurrence.EventId })
        {
            var privateStore = new FileGameStore(Path.Combine(root, "private-declaration-" + withheldId.Replace(':', '-')));
            var privatePlanner = new GamePlanner(privateStore, boards, Vocabulary, [Catalog], fireLos: los);
            var withheldEvents = store.Read(Scope)!.Events.Select(item => item.EventId == withheldId ? item with { Visibility = RussianView } : item).ToArray();
            Assert.Equal(AppendStatus.Committed, privateStore.Append(Scope, "private declaration", 0, withheldEvents, privatePlanner.Replay).Status);
            Assert.Empty((await privatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "pfph", "existing-engine-fire-v1")).FireHistory!.Declarations);
            Assert.Single((await privatePlanner.ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "pfph", "existing-engine-fire-v1")).FireHistory!.Declarations);
        }
        Assert.Equal(GamePlanStatus.Replay, (await Planner().PlanAsync(GameActions.DesignateSpotter, request, Tenant)).Status);
        var changed = JsonSerializer.SerializeToNode(request)!.AsObject();
        changed["spotter"] = "g3";
        Assert.Equal(GamePlanStatus.Refused, (await Planner().PlanAsync(GameActions.DesignateSpotter, JsonSerializer.SerializeToElement(changed), Tenant)).Status);
        changed["attemptId"] = "stale-spotter";
        Assert.Equal(GamePlanStatus.Stale, (await Planner().PlanAsync(GameActions.DesignateSpotter, JsonSerializer.SerializeToElement(changed), Tenant)).Status);
        Refused(await Do(GameActions.DesignateSpotter, NoRoll(), new
        {
            gunId = "m1",
            spotter = "g2"
        }), "play.spotter");
        var written = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(written)).Record!;
        Assert.Equal(written, GameEventWriter.Write(Scope, loaded));
        Assert.Equal(declaration, Assert.Single(Planner().Replay(loaded.Events).Current!.MortarSpotterDesignations));
        if (!concealedMortar)
        {
            Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        }
        await Advance();
        Assert.Contains("g2", Current.NoMoveThisPlayerTurn);
        Assert.NotEmpty((await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", "existing-engine-fire-v1")).FireHistory!.EvaluationGaps);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OneSpotterSharesMortarsOnlyAtTheSameLocationAndTargetForThePlayerTurn(bool squadSpotter)
    {
        await Setup("german", 7, 1942,
            Squad("g1", "bd01:B10:0", "german"), Mortar("m1", "attacker-light-mortar", "g1", "german"),
            Squad("g3", "bd01:B10:0", "german"), Mortar("m2", "attacker-light-mortar", "g3", "german"),
            Squad("g4", "bd01:C10:0", "german"), Mortar("m3", "attacker-light-mortar", "g4", "german"),
            squadSpotter ? Squad("g2", "bd01:B9:0", "german") : Unit("g2", "asl:half-squad", "attacker-half-squad", "bd01:B9:0", "german"),
            Squad("r1", "bd01:B6:0", "russian"), Squad("r2", "bd01:B5:0", "russian"));
        await Advance();
        Committed(await Shoot("m1", "bd01:B6:0", "g2", null, 2, 6));
        var before = Revision;
        Refused(await Shoot("m2", "bd01:B5:0", "g2", null), "play.ordnance-spotter-used");
        Refused(await Shoot("m3", "bd01:B6:0", "g2", null), "play.ordnance-spotter-used");
        Assert.Equal(before, Revision);
        var legacy = store.Read(Scope)!.Events.Select(item => item.Payload is OrdnanceFired shot ? item with
        {
            Payload = shot with
            {
                Facts = JsonSerializer.SerializeToElement(shot.Facts.Deserialize<OrdnanceShot>(LiveFire.Json)! with
                {
                    SpotterUseVersion = null
                }, LiveFire.Json)
            }
        } : item).ToArray();
        Assert.False(Planner().Replay(legacy).HasErrors);
        var unknownVersion = legacy.Select(item => item.Payload is OrdnanceFired shot ? item with
        {
            Payload = shot with
            {
                Facts = JsonSerializer.SerializeToElement(shot.Facts.Deserialize<OrdnanceShot>(LiveFire.Json)! with
                {
                    SpotterUseVersion = 2
                }, LiveFire.Json)
            }
        } : item).ToArray();
        Assert.True(Planner().Replay(unknownVersion).HasErrors);
        Committed(await Shoot("m2", "bd01:B6:0", "g2", null, 2, 6));
        Assert.Equal(2, Current.SpottedThisPhase.Count(item => item.Spotter == "g2"));
        Assert.Equal(2, Current.MortarSpotterUses.Count);
        Assert.All(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<OrdnanceFired>(),
            shot => Assert.Equal(1, shot.Facts.Deserialize<OrdnanceShot>(LiveFire.Json)!.SpotterUseVersion));
        Refused(await Shoot("m1", "bd01:B5:0", "g2", null), "play.ordnance-spotter-used");
        if (squadSpotter)
        {
            Committed(await Fire(G2, "bd01:B6:0", 6, 6));
        }
        var written = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(written)).Record!;
        Assert.Equal(JsonSerializer.Serialize(Current.MortarSpotterUses), JsonSerializer.Serialize(Planner().Replay(loaded.Events).Current!.MortarSpotterUses));
        await Advance();
        Assert.Empty(Current.SpottedThisPhase);
        Assert.Equal(2, Current.MortarSpotterUses.Count);
        for (var phase = 0; phase < 6; phase++)
        {
            Committed(await Do(GameActions.AdvancePhase, new DiceRoller(_ => 5), new
            {
            }));
        }
        Assert.Equal("russian", Current.PhasingSide);
        Assert.Equal(2, Current.PlayerTurnOrdinal);
        Assert.Empty(Current.MortarSpotterUses);
    }

    [Fact]
    public async Task AMortarKeepsItsMinimumRangeAndDoesNotFireFromABuildingOrAfterMoving()
    {
        // C9.4: not at 1 hex (minimum 2); B23.423: not from the wooden building C7; A4.41: not in the AFPh after it moved.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"),
            Squad("g2", "bd01:C7:0", "german"), Mortar("de-mtr2", "attacker-light-mortar", "g2", "german"), Squad("r1", "bd01:B9:0", "russian"), Squad("r2", "bd01:B4:0", "russian"));
        await Advance();
        Refused(await Shoot("de-mtr", "bd01:B9:0", null, null, 5, 6), "out-of-range");
        Refused(await Shoot("de-mtr2", "bd01:B4:0", null, null, 5, 6), "play.ordnance-mortar-building");
        await Advance();
        Committed(await Move(G1, "bd01:C10:0"));
        await Pass();
        await EndMove();
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        Refused(await Shoot("de-mtr", "bd01:B4:0", null, null, 5, 6), "play.ordnance-mortar-moved");
    }

    [Theory]
    [MemberData(nameof(TurnGuideAcceptanceCases.AtmosphereProfiles), MemberType = typeof(TurnGuideAcceptanceCases))]
    public async Task SmokeGrenadesHinderDefensiveFireAndLeaveAtTheEndOfTheMph(string profile)
    {
        // A24.1, A24.2, A24.8 (R9.5, R9.6): the German 4-6-7 in B8 places SMOKE there on a dr of 1 for 1 MF; the Russian squad in B10 fires at it
        // with +2 Hindrance and no FFMO. A squad entering B8 pays one more MF. The counter leaves at the end of the MPh.
        await Setup("german", 7, 1942, Squad("g1", "bd01:A8:0", "german"), Squad("g2", "bd01:A9:0", "german"), Squad("r1", "bd01:B10:0", "russian"));
        await Advance(2);
        Committed(await Move(G1, "bd01:B8:0"));
        await Pass();
        Committed(await Smoke(G1, "bd01:B8:0", "g1", "bd01:B8:0", 1));
        Assert.Single(Current.Entities, item => item.Kind == "asl:smoke" && item.Status == InstanceStatus.Active);
        Assert.Equal(G1, Current.SmokeAttempts);
        var smokeRevision = Revision;
        var smokeGuide = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", profile);
        Assert.Equal(TurnSmokeState.Active, Assert.Single(smokeGuide.Atmosphere!.Grenades).State);
        Assert.True(Assert.Single(smokeGuide.Atmosphere.Grenades).AttemptSpentThisPhase);
        Assert.Empty(smokeGuide.Atmosphere.UnclassifiedSmoke);
        Assert.Equal(7, smokeGuide.Atmosphere.PendingRules.Length);
        var enemySmoke = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "mph", profile)).Atmosphere!;
        Assert.Empty(enemySmoke.Grenades);
        Assert.NotEmpty(enemySmoke.PendingRules);
        var before = Revision;
        Committed(await Fire(R1, "bd01:B8:0", 6, 6));
        var record = LastFire(before);
        Assert.True(HasDrm(record, "los-hindrance", 2));
        Assert.False(HasDrm(record, "ffmo", -1));
        await Pass();
        Refused(await Smoke(G1, "bd01:B8:0", "g1", "bd01:B8:0", 1), "play.smoke-once");
        await EndMove();
        var entering = Revision;
        Committed(await Move(G2, "bd01:B8:0"));
        Assert.Equal(4, Since(entering).Select(item => item.Payload).OfType<MovementStepped>().Single().HalfMf);
        await Pass();
        await EndMove();
        await Advance();
        Assert.DoesNotContain(Current.Entities, item => item.Kind == "asl:smoke" && item.Status == InstanceStatus.Active);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
        var afterSmoke = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "dfph", profile)).Atmosphere!;
        Assert.Equal(TurnSmokeState.RemovedAtMovementEnd, Assert.Single(afterSmoke.Grenades).State);
        Assert.False(Assert.Single(afterSmoke.Grenades).AttemptSpentThisPhase);
        var oldSmoke = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", profile, smokeRevision);
        Assert.Equal(JsonSerializer.Serialize(smokeGuide.Atmosphere), JsonSerializer.Serialize(oldSmoke.Atmosphere));
        Assert.Equal(TurnCloseDisposition.ReadOnly, oldSmoke.PhaseClose.Disposition);
        var stored = store.Read(Scope)!;
        var disk = GameEventReader.Read(Encoding.UTF8.GetBytes(GameEventWriter.Write(Scope, stored))).Record!;
        var copied = new FileGameStore(Path.Combine(root, "smoke-copy"));
        var reader = new GamePlanner(copied, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, copied.Append(Scope, disk.Label, 0, disk.Events, reader.Replay).Status);
        Assert.Equal(AppendStatus.Replay, copied.Append(Scope, disk.Label, 0, disk.Events, reader.Replay).Status);
        var count = copied.Read(Scope)!.Events.Count;
        Assert.Equal(JsonSerializer.Serialize(afterSmoke), JsonSerializer.Serialize((await reader.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "dfph", profile)).Atmosphere));
        Assert.Equal(count, copied.Read(Scope)!.Events.Count);
        var missingLinks = disk.Events.Select(item => item.Payload is InstanceCreated { Instance.Kind: "asl:smoke" }
            ? item with
            {
                Causes = []
            } : item).ToArray();
        var legacy = new FileGameStore(Path.Combine(root, "smoke-legacy"));
        var legacyReader = new GamePlanner(legacy, boards, Vocabulary, [Catalog], fireLos: los);
        Assert.Equal(AppendStatus.Committed, legacy.Append(Scope, disk.Label, 0, missingLinks, legacyReader.Replay).Status);
        var incomplete = (await legacyReader.ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", profile, smokeRevision)).Atmosphere!;
        Assert.Equal(TurnSmokeState.NotEvaluated, Assert.Single(incomplete.Grenades).State);
        Assert.Null(Assert.Single(incomplete.Grenades).Counter);
        Assert.Single(incomplete.UnclassifiedSmoke);
        Assert.Equal(TurnSmokeState.NotEvaluated, Assert.Single((await legacyReader.ReadTurnGuideProfileAsync(Scope,
            Perspective.Side("german"), "dfph", profile)).Atmosphere!.Grenades).State);
    }

    [Fact]
    public async Task ASmokeDrOf6EndsThePlacingSquadsMph()
    {
        // A24.1 (R9.5): a 6 places nothing, costs 2 MF for the ADJACENT Location, and ends the squad's MPh once the DEFENDER passes.
        await Setup("german", 7, 1942, Squad("g1", "bd01:A8:0", "german"), Squad("r1", "bd01:B4:0", "russian"));
        await Advance(2);
        var before = Revision;
        Committed(await Smoke(G1, "bd01:A8:0", "g1", "bd01:B8:0", 6));
        Assert.DoesNotContain(Current.Entities, item => item.Kind == "asl:smoke");
        var failedSmoke = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", "existing-engine-atmosphere-v1")).Atmosphere!;
        Assert.Equal(TurnSmokeState.FailedAttempt, Assert.Single(failedSmoke.Grenades).State);
        Assert.True(Assert.Single(failedSmoke.Grenades).AttemptSpentThisPhase);
        Assert.Equal(4, Since(before).Select(item => item.Payload).OfType<MovementStepped>().Single().HalfMf);
        await Pass();
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task APanzerfaustCheckGivesAShotThatBurnsAT34AndCountsAgainstTheUsageLimit()
    {
        // C13.31 to C13.34 (R9.7, R9.8): July 1944, the German squad in B9 checks 2, then hits the T-34 in B7 at TH# 6 (10 less 2 per hex) with a
        // hull hit (colored 3, white 2); TK# 31 less AF 11 or 6 is at least 20, and a 7 burns it. One squad set up allows one shot in 1944.
        await Setup("german", 7, 1944, Squad("g1", "bd01:B9:0", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B7:0", "russian", "east"));
        await Advance();
        var before = Revision;
        Committed(await FireAt("g1:pf", "bd01:B7:0", "ru-tank", null, 2, 3, 2, 3, 4));
        var shot = LastShot(before);
        Assert.Equal((OrdnancePanzerfaustCheck.Shot, 10, 6), (shot.PanzerfaustCheck!.Outcome, shot.ToHit!.BasicToHit, shot.ToHit.ModifiedToHit));
        Assert.Equal(OrdnanceKill.Burn, shot.Kill!.Result);
        Assert.Equal(InstanceStatus.Wrecked, Current.Unit("ru-tank")!.Status);
        Assert.Equal((1, 2), (Current.PanzerfaustShots["german"], Current.SetupHalfSquads["german"]));
        Assert.Equal(1, LiveOrdnance.PanzerfaustAllowance(Current, "german"));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task APanzerfaustCheckOf6PinsTheUnit()
    {
        // C13.31 (R9.7): an Original 6 pins the squad and gives no shot.
        await Setup("german", 7, 1944, Squad("g1", "bd01:B9:0", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B7:0", "russian", "east"));
        await Advance();
        var before = Revision;
        Committed(await FireAt("g1:pf", "bd01:B7:0", "ru-tank", null, 6));
        var shot = LastShot(before);
        Assert.Equal(OrdnancePanzerfaustCheck.Pinned, shot.PanzerfaustCheck!.Outcome);
        Assert.Null(shot.ToHit);
        Assert.True(Is(Current.Unit("g1")!, Conditions.Pinned));
        Assert.False(Current.PanzerfaustShots.ContainsKey("german"));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task NoPanzerfaustBeforeOctober1943()
    {
        // C13.3 (R9.7): PF are not available before October 1943.
        await Setup("german", 9, 1943, Squad("g1", "bd01:B9:0", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B8:0", "russian", "east"));
        await Advance();
        Refused(await FireAt("g1:pf", "bd01:B8:0", "ru-tank", null, 2, 3, 2, 3, 4), "panzerfaust-date-outside");
    }

    [Fact]
    public async Task AMortarKeepsItsAreaAcquisitionForItsNextShot()
    {
        // C6.521, C9.2 (table player, pass 9): the first shot acquires B6; the ROF shot takes Case N -1 for the unit there.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Shoot("de-mtr", "bd01:B6:0", null, null, 2, 6));
        Assert.Contains(Current.Acquisitions, item => item.Gun == "de-mtr" && item.Location == At("bd01:B6:0") && item.Level == -1);
        var before = Revision;
        Committed(await Shoot("de-mtr", "bd01:B6:0", null, null, 2, 6, 6, 6));
        Assert.Contains(LastShot(before).AreaTargets!.Single().Drm, item => item.Name == "case-n" && item.Value == -1);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ASquadThatFiredEarlierOrWithAMgUsesNoSecondSw()
    {
        // A7.351 (table player, pass 9): a squad that fired its inherent FP with its LMG fires no mortar; one that fired its mortar fires its inherent
        // FP without its LMG; a squad that Prep Fired does not fire its mortar in the AFPh.
        await Setup("german", 7, 1942, Squad("g1", "bd01:B10:0", "german"), Mortar("de-mtr", "attacker-light-mortar", "g1", "german"), Weapon("de-lmg", "attacker-lmg", "g1", "german"),
            Squad("g2", "bd01:B9:0", "german"), Mortar("de-mtr2", "attacker-light-mortar", "g2", "german"), Weapon("de-lmg2", "attacker-lmg", "g2", "german"), Squad("r1", "bd01:B6:0", "russian"));
        await Advance();
        Committed(await Do(GameActions.Fire, Once(6, 6), new
        {
            firers = G1,
            target = "bd01:B6:0",
            weapons = new Dictionary<string, string[]> { ["g1"] = ["de-lmg"] }
        }));
        Refused(await Shoot("de-mtr", "bd01:B6:0", null, null, 5, 6), "play.sw-limit");
        Committed(await Shoot("de-mtr2", "bd01:B6:0", null, null, 5, 6));
        Refused(await Do(GameActions.Fire, Once(6, 6), new
        {
            firers = G2,
            target = "bd01:B6:0",
            weapons = new Dictionary<string, string[]> { ["g2"] = ["de-lmg2"] }
        }), "play.fire-sw-limit");
        await Advance();
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        Refused(await Shoot("de-mtr", "bd01:B6:0", null, null, 5, 6), "play.sw-fired");
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task APanzerfaustOriginal12ReducesTheSquadToAHsThatHasFired()
    {
        // C13.36 (table player, pass 9): the HS the squad becomes carries its Prep Fire counter.
        await Setup("german", 7, 1944, Squad("g1", "bd01:B9:0", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B7:0", "russian", "east"));
        await Advance();
        Committed(await FireAt("g1:pf", "bd01:B7:0", "ru-tank", null, 1, 6, 6));
        var half = Current.Units.Single(unit => unit.Status == InstanceStatus.Active && unit.Kind == "asl:half-squad");
        Assert.True(Is(half, Conditions.PrepFire));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

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

    [Fact]
    public void TheAtrAndPanzerschreckAreInTheCatalog()
    {
        // Rulings R9.10, R9.11 (pass 9b): manufactured values, sheet MFG.
        Assert.Equal(("atr", "psk"), (LiveOrdnance.LatwType("defender-atr"), LiveOrdnance.LatwType("attacker-psk")));
    }

    [Fact]
    public async Task AnAtrHitsATankOnTheBlackVehicleRowWithAp()
    {
        // C13.2 (R9.10): in the DFPh the Russian squad's ATR in B9 hits the PzKpfw IIIH in B7 (black 10) with AP at the Russian ATR TK# 6.
        await Setup("german", 7, 1942, Squad("r1", "bd01:B9:0", "russian"), Latw("ru-atr", "defender-atr", "r1", "russian"),
            Vehicle("de-tank", "attacker-tank", "bd01:B7:0", "german", "east"), Squad("g1", "bd01:B6:0", "german"));
        await Advance(3);
        Assert.Equal("dfph", Current.Phase);
        var before = Revision;
        Committed(await FireAt("ru-atr", "bd01:B7:0", "de-tank", null, 3, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal(("black", 10, 6), (shot.ToHit!.Color, shot.ToHit.BasicToHit, shot.Kill!.BasicTk));
        Assert.Equal("ap", LastFacts(before).Ammunition);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task AnAtrAddsOneFpToItsSquadsFireGroup()
    {
        // C13.24 (R9.10): the Russian squad fires its inherent FP with its ATR at the German squad 3 hexes away: 4 + 1 FP.
        await Setup("russian", 7, 1942, Squad("r1", "bd01:B9:0", "russian"), Latw("ru-atr", "defender-atr", "r1", "russian"), Squad("g1", "bd01:B6:0", "german"));
        await Advance();
        var before = Revision;
        Committed(await Do(GameActions.Fire, Once(6, 6), new
        {
            firers = R1,
            target = "bd01:B6:0",
            weapons = new Dictionary<string, string[]> { ["r1"] = ["ru-atr"] }
        }));
        var firers = LastFire(before).Resolution.GetProperty("arithmetic").GetProperty("firers").EnumerateArray().ToArray();
        Assert.Contains(firers, item => item.GetProperty("unitId").GetString() == "ru-atr" && item.GetProperty("firepower").GetDecimal() == 1);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task APanzerschreckHitsOnItsOwnTableAndIsRemovedOnItsXNumber()
    {
        // C13.42, C13.47, C13.48 (R9.11): the German HS's PSK in B9 fires at the T-34 in B7 on its table (9 at 2 hexes), HEAT TK# 26; a second PSK's
        // Original 11 removes it. Before September 1943 there is none.
        await Setup("german", 7, 1944, Unit("gh", "asl:half-squad", "attacker-half-squad", "bd01:B9:0", "german"), Latw("de-psk", "attacker-psk", "gh", "german"),
            Squad("g2", "bd01:B9:0", "german"), Latw("de-psk2", "attacker-psk", "g2", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B7:0", "russian", "east"));
        await Advance();
        var before = Revision;
        Committed(await FireAt("de-psk", "bd01:B7:0", "ru-tank", null, 3, 2, 6, 6));
        var shot = LastShot(before);
        Assert.Equal((9, 26), (shot.ToHit!.BasicToHit, shot.Kill!.BasicTk));
        Committed(await FireAt("de-psk2", "bd01:B7:0", "ru-tank", null, 5, 6));
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("de-psk2")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task NoPanzerschreckBeforeSeptember1943()
    {
        await Setup("german", 8, 1943, Squad("g1", "bd01:B9:0", "german"), Latw("de-psk", "attacker-psk", "g1", "german"), Vehicle("ru-tank", "defender-tank", "bd01:B7:0", "russian", "east"));
        await Advance();
        Refused(await FireAt("de-psk", "bd01:B7:0", "ru-tank", null, 3, 2, 3, 4), "latw-date-outside");
    }

    [Fact]
    public async Task AnAtrFiresOnceAPhaseAndItsSquadStillFiresItsInherentFp()
    {
        // A7.351, C13.2 (table player, pass 9b): after its ATR's shot the squad's fire is not spent, and the ATR, with no ROF, fires no more.
        await Setup("russian", 7, 1942, Squad("r1", "bd01:B9:0", "russian"), Latw("ru-atr", "defender-atr", "r1", "russian"),
            Vehicle("de-tank", "attacker-tank", "bd01:B7:0", "german", "east"), Squad("g1", "bd01:B6:0", "german"));
        await Advance();

        // C6.3 (pass 35, task 35.10): at two hexes the ATR takes Case L, so the 11 is a Final 10 and a hit; the To Kill DR of 12 does nothing.
        Committed(await FireAt("ru-atr", "bd01:B7:0", "de-tank", null, 6, 5, 6, 6));
        Assert.False(LiveFire.FireSpent(Current, Current.Unit("r1")!));
        Refused(await FireAt("ru-atr", "bd01:B7:0", "de-tank", null, 6, 5), "play.latw-fired");
        Committed(await Fire(R1, "bd01:B6:0", 6, 6));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }
}
