using System.Text.Json;
using System.Text;
using ASL.Turns;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The backlog pass 15 in live play (rulings R15.1 to R15.14): FT, MOL, Thrown and Placed DC, Snipers, Commissars, NKVD Field Promotion, Allied Troops,
/// underscored Morale Factors, Green MMC, a hero's MG, a hero created concealed, and a berserk unit's kept SW. A board 01 grid of Open Ground, fixed dice,
/// and a clear LOS everywhere.
/// </summary>
public sealed partial class BacklogPass15Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000f515");
    private static readonly GameScope Scope = new(Tenant, "pass15");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] G1G2 = ["g1", "g2"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] Gi = ["gi"];
    private static readonly string[] G1Gi = ["g1", "gi"];
    private static readonly string[] Gh = ["gh"];
    private static readonly string[] KeepThree = ["m2", "m3", "m4"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-pass15-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass15Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Board 01's grid, all Open Ground at level 0.</summary>
    private static BoardHandle Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var type = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var hexes = new List<HexFacts>();
        foreach (var text in Board01Fixture.Hexes())
        {
            var name = HexName.Parse(text);
            Assert.True(geometry.TryGetIndex(name, out var index));
            var center = new LocationFacts(0, type, null);
            HexsideFacts[] hexsides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, type, null, false, false, false, false, null))];
            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));
        }

        return new BoardHandle(BoardCatalogTerrainEvidence.Board, "authored", BoardReadStatus.Verified, "pass 15 test board", new HexFactSet(geometry, "pass15", hexes));
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    private sealed class ClearLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target) =>
            new(LosStatus.Clear, false, Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1, 0, null, string.Empty);
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());

    /// <summary>These values first, then the fallback face for every other die.</summary>
    private static DiceRoller Then(int fallback, params int[] values)
    {
        var queue = new Queue<int>(values);
        return new(_ => (queue.Count > 0 ? queue.Dequeue() : fallback) - 1);
    }

    private static DiceRoller NoRoll() => new(_ => throw new InvalidOperationException("No roll."));

    private GamePlay Play(DiceRoller? roller = null) => new(Planner(), store, new NullAudit(), roller: roller ?? NoRoll());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private IReadOnlyList<GameEvent> Since(long revision) => [.. store.Read(Scope)!.Events.Skip((int)revision)];

    private static string L(string hex) => $"bd01:{hex}:0";

    private static Dictionary<string, object> Unit(string id, string definition, string hex, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        var kind = definition.Contains("half-squad", StringComparison.Ordinal) ? "asl:half-squad"
            : definition.Contains("leader", StringComparison.Ordinal) || definition.Contains("commissar", StringComparison.Ordinal) ? "asl:leader"
            : definition.Contains("hero", StringComparison.Ordinal) ? "asl:hero" : "asl:squad";
        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at = L(hex)
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Weapon(string id, string kind, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private static Dictionary<string, object> Sniper(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:sniper",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private async Task SetupAt(int advances, string firstSide, string[] specialRules, object[] sides, params Dictionary<string, object>[] placements)
    {
        Committed(await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Pass 15",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = Bd01,
                ["firstSide"] = firstSide,
                ["specialRules"] = specialRules,
                ["sides"] = sides,
                ["scenarioMonth"] = 7,
                ["scenarioYear"] = 1942,
            },
            placements,
        })));
        await Advance(advances);
    }

    private static object[] Sides(int? germanSan = null, int? russianSan = null, string enemy = "russian") =>
    [
        new { id = "german", nationality = "german", elr = 3, san = germanSan },
        new { id = enemy, nationality = enemy, elr = 2, san = russianSan },
    ];

    private Task SetupAt(int advances, string firstSide, params Dictionary<string, object>[] placements) => SetupAt(advances, firstSide, [], Sides(), placements);

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

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static FireResolution Resolution(FireResolved fire) => fire.Resolution.Deserialize<FireResolution>(LiveFire.Json)!;

    private FireResolved[] Fires(long since) => [.. Since(since).Select(item => item.Payload).OfType<FireResolved>()];

    [Fact]
    public async Task AFlamethrowerFiresAloneWithNoTemAndItsSquadFiresItsInherentFpApart()
    {
        // A22.1, A22.2, A22.3 (ruling R15.1): 24 FP at an adjacent Location, never doubled for PBF; the squad fires its inherent FP in another attack.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"));
        var before = Revision;
        // Pass 45 (A7.9, A22.1): a FT's doubles cower, and the squad that cowers is marked with all its SW; a DR of 7 here, not doubles.
        Committed(await Do(GameActions.Fire, Then(3, 3, 4), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["ft"] },
            withoutInherent = G1,
            target = L("E4"),
        }));
        var arithmetic = Resolution(Fires(before).Single()).Arithmetic!;
        Assert.Equal(24m, arithmetic.TotalFirepower);
        Assert.DoesNotContain(arithmetic.Drm, item => item.Name.StartsWith("tem:", StringComparison.Ordinal) || item.Name.StartsWith("leadership:", StringComparison.Ordinal));
        Assert.Equal(InstanceStatus.Active, Current.Find("ft")!.Status);

        // The FT alone may not take a leader or another firer (A22.31).
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E2"),
        }));
    }

    [Fact]
    public async Task AFlamethrowerRunsOutOfFuelOnItsRemovalNumberAndItsOwnerTakesTheDrOneLower()
    {
        // A22.5, A22.3 (ruling R15.1): a 1st Line user's removal number is 8, so an Original 8 removes the FT; A22.4: the Russian FT holder takes -1.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Weapon("rft", "asl:ft", "defender-ft", "r1", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 4, 4), new
        {
            firers = G1,
            weapons = new Dictionary<string, string[]> { ["g1"] = ["ft"] },
            withoutInherent = G1,
            target = L("E4"),
        }));
        var resolution = Resolution(Fires(before).Single());
        Assert.Equal("ft", resolution.FlamethrowerRemoved);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("ft")!.Status);
        Assert.Contains("flamethrower-possessed:-1", resolution.Effects.Single(item => item.UnitId == "r1").Events);
    }

    [Fact]
    public async Task AMolNeedsItsSsrAndAPassedCheckAddsFourFp()
    {
        // A22.6 (ruling R15.4): no MOL without the SSR.
        await SetupAt(1, "russian", ["mol:russian"], Sides(), Unit("r1", "defender-squad", "E3", "russian"), Unit("g1", "attacker-squad", "E4", "german"),
            Unit("g2", "attacker-squad", "E2", "german"));
        Refused(await Do(GameActions.Fire, NoRoll(), new
        {
            firers = R1,
            target = L("E4"),
            mol = "g1",
        }), "play.fire-mol");

        // A22.611: a dr of 2, +1 against a non-AFV, is 3: passed, 4 FP after PBF: 4 x 2 + 4 = 12.
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 2), new
        {
            firers = R1,
            target = L("E4"),
            mol = "r1",
        }));
        var resolution = Resolution(Fires(before).Single());
        Assert.True(resolution.MolCheck!.Passed);
        Assert.Equal(12m, resolution.Arithmetic!.TotalFirepower);
    }

    [Fact]
    public async Task AMolIsRefusedWithoutItsSsr()
    {
        await SetupAt(1, "russian", Unit("r1", "defender-squad", "E3", "russian"), Unit("g1", "attacker-squad", "E4", "german"));
        Refused(await Do(GameActions.Fire, NoRoll(), new
        {
            firers = R1,
            target = L("E4"),
            mol = "r1",
        }), "play.fire-mol");
    }

    [Fact]
    public async Task AThrownDcAttacksItsTargetAndThenItsThrowerAndIsRemoved()
    {
        // A23.6 (ruling R15.3): +2 at the target, +3 at the thrower's Location, each its own DR; the DC is removed and the thrower is marked.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.ThrowDc, Then(3), new
        {
            unitId = "g1",
            equipmentId = "dc",
            target = L("E4"),
        }));
        var fires = Fires(before);
        Assert.Equal(2, fires.Length);
        Assert.Contains(Resolution(fires[0]).Arithmetic!.Drm, item => item.Name == "thrown-dc" && item.Value == 2m);
        Assert.Contains(Resolution(fires[1]).Arithmetic!.Drm, item => item.Name == "thrower-location" && item.Value == 3m);
        Assert.Equal(30m, Resolution(fires[0]).Arithmetic!.TotalFirepower);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("dc")!.Status);
        Assert.True(Is(Current.Unit("g1")!, Conditions.PrepFire));
    }

    private async Task<TurnHostGuide> ReadCopiedDcHistory(IReadOnlyList<GameEvent> events, Perspective perspective)
    {
        var copied = new FileGameStore(Path.Combine(root, Guid.NewGuid().ToString("N")));
        var planner = new GamePlanner(copied, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: new ClearLos());
        Assert.Equal(AppendStatus.Committed, copied.Append(Scope, "B5 replay", 0, events, planner.Replay).Status);
        Assert.Equal(AppendStatus.Replay, copied.Append(Scope, "B5 replay", 0, events, planner.Replay).Status);
        return await planner.ReadTurnGuideProfileAsync(Scope, perspective, "afph", "existing-engine-dc-v1");
    }

    [Fact]
    public async Task APlacedDcIsOperableOnceItsPlacerMovesOnAndDetonatesInTheAfph()
    {
        // A23.3, A8.1: ending the placer's own move leaves its Placement exposed until the MPh ends.
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4"),
        }));
        Assert.False(Current.PlacedCharges.Single().Operable);
        var placementRevision = Revision;
        var initial = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", "existing-engine-dc-v1");
        Assert.Equal(TurnObligationState.Scheduled, Assert.Single(initial.Obligations).State);
        Assert.Equal(TurnSupport.IntegratedSubset, Assert.Single(initial.Obligations).CommandSupport);
        Assert.Equal(114, initial.Guide.Options.Length);
        var placementEvent = store.Read(Scope)!.Events.Single(item => item.Payload is MovementStepped { DcPlacement: not null });
        var placementStep = (MovementStepped)placementEvent.Payload;
        Assert.Equal("german", placementStep.DcPlacement!.OwnerSide);
        Assert.Equal(1, placementStep.DcPlacement.SettlementVersion);
        var serialized = GameEventWriter.Write(Scope, store.Read(Scope)!);
        var loaded = GameEventReader.Read(Encoding.UTF8.GetBytes(serialized)).Record!;
        Assert.False(Planner().Replay(loaded.Events).HasErrors);
        Assert.Equal(serialized, GameEventWriter.Write(Scope, loaded));
        var legacy = loaded.Events.Select(item => item.EventId == placementEvent.EventId ? item with
        {
            Payload = placementStep with
            {
                DcPlacement = placementStep.DcPlacement with
                {
                    OwnerSide = null
                }
            }
        } : item).ToArray();
        Assert.False(Planner().Replay(legacy).HasErrors);
        var legacyGuide = await ReadCopiedDcHistory(legacy, Perspective.Side("german"));
        Assert.Equal(TurnObligationState.NotEvaluated, Assert.Single(legacyGuide.Obligations).State);
        Assert.Empty(legacyGuide.Actions);
        Assert.Equal(TurnSupport.Pending, Assert.Single(legacyGuide.Obligations).CommandSupport);
        var interrupted = placementEvent with
        {
            EventId = "pin-before-operable",
            Revision = placementRevision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
        };
        var interruptedGuide = await ReadCopiedDcHistory([.. loaded.Events, interrupted], Perspective.Side("german"));
        Assert.Equal(TurnObligationState.Interrupted, Assert.Single(interruptedGuide.Obligations).State);
        Assert.Empty(interruptedGuide.Actions);
        var eliminated = interrupted with
        {
            EventId = "eliminated-before-settlement",
            Type = "instance-eliminated",
            Payload = new InstanceEliminated("g1"),
        };
        var failedPlacement = Planner().Replay([.. loaded.Events, eliminated]).Current!;
        Assert.Empty(failedPlacement.PlacedCharges);
        var retained = Assert.IsType<EquipmentInstance>(failedPlacement.Find("dc"));
        Assert.Null(retained.Holding);
        Assert.Equal(L("E3"), Assert.IsType<MapPosition>(retained.Position).Location.ToString());
        var invalidOwner = legacy.Select(item => item.EventId == placementEvent.EventId ? item with
        {
            Payload = placementStep with
            {
                DcPlacement = placementStep.DcPlacement with
                {
                    OwnerSide = "russian"
                }
            }
        } : item).ToArray();
        Assert.True(Planner().Replay(invalidOwner).HasErrors);
        var baseline = await Planner().ReadTurnGuideAsync(Scope, Perspective.Side("german"), "mph");
        Assert.Equal(TurnSupport.Pending, Assert.Single(baseline.Obligations).CommandSupport);
        var enemy = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("russian"), "mph", "existing-engine-dc-v1");
        Assert.Empty(enemy.Obligations);
        await Assert.ThrowsAsync<ArgumentException>(() => Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "mph", "all-asl"));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.False(Current.PlacedCharges.Single().Operable);
        Assert.Equal("g1", (Current.Find("dc") as EquipmentInstance)!.Holding!.Holder);

        var oldReading = store.Read(Scope)!.Events.Select(item => item.Payload is MovementStepped { DcPlacement: { } dc } move ? item with
        {
            Payload = move with
            {
                DcPlacement = dc with
                {
                    SettlementVersion = null
                }
            }
        } : item).ToArray();
        var oldState = Planner().Replay(oldReading).Current!;
        Assert.True(oldState.PlacedCharges.Single().Operable);

        var pinnedAfterEnd = interrupted with
        {
            Revision = Revision + 1
        };
        Assert.Empty(Planner().Replay([.. store.Read(Scope)!.Events, pinnedAfterEnd]).Current!.PlacedCharges);

        // The AFPh does not end before the DC detonates (A23.4).
        await Advance(2);
        Assert.Equal("afph", Current.Phase);
        Assert.True(Current.PlacedCharges.Single().Operable);
        Assert.Equal(L("E4"), (Current.Find("dc") as EquipmentInstance)!.Position is MapPosition at ? at.Location.ToString() : null);
        var due = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-dc-v1");
        Assert.Equal(TurnObligationState.Due, Assert.Single(due.Obligations).State);
        Assert.Equal(TurnCloseDisposition.Blocked, due.PhaseClose.Disposition);
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.dc-detonate-pending");
        var before = Revision;
        Committed(await Do(GameActions.DetonateDc, Then(3), new
        {
            equipmentId = "dc",
        }));
        var arithmetic = Resolution(Fires(before).Single()).Arithmetic!;
        Assert.Equal(30m, arithmetic.TotalFirepower);
        Assert.DoesNotContain(arithmetic.Drm, item => item.Name == "advancing-fire");
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("dc")!.Status);
        Assert.Empty(Current.PlacedCharges);
        var completed = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-dc-v1");
        Assert.Equal(TurnObligationState.Resolved, Assert.Single(completed.Obligations).State);
        var recordedFire = (await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-fire-v1")).FireHistory!;
        Assert.Equal(TurnFireKind.Fire, Assert.Single(recordedFire.Shots).Kind);
        Assert.Null(Assert.Single(recordedFire.Shots).RateOfFireKept);
        Assert.Empty(recordedFire.Spotters);
        var ownCompleted = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "afph", "existing-engine-dc-v1");
        Assert.Equal(TurnObligationState.Resolved, Assert.Single(ownCompleted.Obligations).State);
        var diskReload = await ReadCopiedDcHistory(store.Read(Scope)!.Events, Perspective.Adjudicator);
        Assert.Equal(TurnObligationState.Resolved, Assert.Single(diskReload.Obligations).State);
        var revision = Revision;
        Assert.Equal(JsonSerializer.Serialize(completed), JsonSerializer.Serialize(await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Adjudicator, "afph", "existing-engine-dc-v1")));
        Assert.Equal(revision, Revision);
        var past = await Planner().ReadTurnGuideProfileAsync(Scope, Perspective.Side("german"), "mph", "existing-engine-dc-v1", placementRevision);
        Assert.Equal(TurnObligationState.Scheduled, Assert.Single(past.Obligations).State);
        Assert.Equal(TurnCloseDisposition.ReadOnly, past.PhaseClose.Disposition);
        await Advance();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task APlacedDcSettlesOnDepartureOrAnotherUnitsMovement(bool departure)
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("g2", "attacker-squad", "D3", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4"),
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        if (!departure)
        {
            Committed(await Do(GameActions.EndMove, NoRoll(), new
            {
            }));
            Assert.False(Current.PlacedCharges.Single().Operable);
            var history = store.Read(Scope)!.Events;
            foreach (var (payload, settles) in new (EventPayload Payload, bool Settles)[]
            {
                (new InstanceMoved("g2", new MapPosition(BoardLocation.Parse(L("D2")))), false),
                (new InstanceMoved("g2", new MapPosition(BoardLocation.Parse(L("D2"))), 1), true),
                (new EntryAttempted("g2", BoardLocation.Parse(L("D2")), 1), true),
            })
            {
                var entry = history[^1] with
                {
                    EventId = "other-entry",
                    Revision = Revision + 1,
                    Type = payload is EntryAttempted ? "entry-attempted" : "instance-moved",
                    Payload = payload,
                };
                var afterEntry = Planner().Replay([.. history, entry]).Current!;
                Assert.Equal(settles, afterEntry.PlacedCharges.Single().Operable);
                if (settles)
                {
                    var pin = entry with
                    {
                        EventId = "pin-after-entry",
                        Revision = Revision + 2,
                        Type = "conditions-changed",
                        Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
                    };
                    Assert.True(Planner().Replay([.. history, entry, pin]).Current!.PlacedCharges.Single().Operable);
                }
            }
        }
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = new[] { departure ? "g1" : "g2" },
            to = L(departure ? "E2" : "D2"),
        }));
        Assert.True(Current.PlacedCharges.Single().Operable);
        var last = store.Read(Scope)!.Events[^1];
        var pinned = last with
        {
            EventId = "pin-after-settlement",
            Revision = Revision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
        };
        Assert.True(Planner().Replay([.. store.Read(Scope)!.Events, pinned]).Current!.PlacedCharges.Single().Operable);
    }

    [Fact]
    public async Task AStackedPlacersDcStaysPendingThroughItsOwnStacksMove()
    {
        // A8.1 (p. 59): Defensive First Fire affects "only the moving unit/stack"; A23.3 (p. 90): the placer must survive all enemy Defensive First
        // Fire in the Placement Hex. g2 moves with the placer g1 in one stack, so its MF is the stack's own move, not another unit beginning movement:
        // the Placement stays pending, and a pin on g1 before the stack's move ends leaves the DC with g1.
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("g2", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1G2,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4"),
        }));
        Assert.Contains("g2", ((MovementStepped)store.Read(Scope)!.Events.Single(item => item.Payload is MovementStepped { DcPlacement: not null }).Payload).Movers);
        Assert.False(Current.PlacedCharges.Single().Operable);
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Assert.False(Current.PlacedCharges.Single().Operable);

        // g2 moves on apart from the placer: still the stack's own move (A4.2), not another unit beginning movement.
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = L("D3"),
        }));
        Assert.False(Current.PlacedCharges.Single().Operable);
        var history = store.Read(Scope)!.Events;
        var pin = history[^1] with
        {
            EventId = "pin-in-placement-hex",
            Revision = Revision + 1,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
        };
        var pinned = Planner().Replay([.. history, pin]).Current!;
        Assert.Empty(pinned.PlacedCharges);
        Assert.Equal("g1", ((EquipmentInstance)pinned.Find("dc")!).Holding!.Holder);
    }

    [Fact]
    public async Task APendingDcFollowsItsUnbrokenSuccessorUntilPhaseEnd()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        var history = store.Read(Scope)!.Events;
        var lineage = history[^1] with
        {
            EventId = "placer-reduced",
            Revision = Revision + 1,
            Type = "lineage",
            Payload = new LineageRecorded(LineageAction.Reduced, ["g1"],
                [new NewInstance("gh", "asl:half-squad", "attacker-half-squad", "german", null, null, Current.Unit("g1")!.Conditions)]),
        };
        var reduced = Planner().Replay([.. history, lineage]).Current!;
        var pending = Assert.Single(reduced.PlacedCharges);
        Assert.Equal("gh", pending.Unit);
        Assert.False(pending.Operable);
        Assert.Equal("gh", ((EquipmentInstance)reduced.Find("dc")!).Holding!.Holder);
        Assert.Equal(Current.Unit("g1")!.MfSpent, reduced.Unit("gh")!.MfSpent);
        var phase = lineage with
        {
            EventId = "settle-at-phase-end",
            Revision = Revision + 2,
            Type = "phase-changed",
            Payload = new PhaseChanged(Current.Turn, "dfph", "german"),
        };
        Assert.True(Assert.Single(Planner().Replay([.. history, lineage, phase]).Current!.PlacedCharges).Operable);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task APlacedDcSettlesOnAnotherUnitsRecoveryOrFailedPush(bool recovery)
    {
        var loose = Weapon("loose", "asl:mg", "attacker-lmg", "g2", "german");
        loose.Remove("holding");
        loose["position"] = new
        {
            at = L("D3")
        };
        var gun = Weapon("gun", "asl:gun", "attacker-inf-gun", "g2", "german");
        gun["holding"] = new
        {
            holder = "g2",
            role = "manned"
        };
        gun["position"] = new
        {
            at = L("D3"),
            facing = "east"
        };
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("g2", "attacker-squad", "D3", "german"), loose, gun, Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new
        {
        }));
        Committed(await Do(GameActions.EndMove, NoRoll(), new
        {
        }));
        Assert.False(Current.PlacedCharges.Single().Operable);
        var history = store.Read(Scope)!.Events;
        var roll = history[^1] with
        {
            EventId = "other-work-roll",
            Revision = Revision + 1,
            Type = "dice-rolled",
            Payload = new DiceRolled("other-work-roll", recovery ? "recovery" : "manhandling", recovery ? 1 : 2, 6,
                recovery ? [6] : [6, 6], DiceRolled.SystemSource, "german"),
        };
        var work = roll with
        {
            EventId = "other-work",
            Revision = Revision + 2,
            Type = recovery ? "recovery-attempted" : "manhandling-rolled",
            Payload = recovery ? new RecoveryAttempted("g2", "loose", "other-work-roll", 0, false)
                : new ManhandlingRolled("gun", "other-work-roll", 0, 8, ManhandlingRolled.Stay),
        };
        var pin = work with
        {
            EventId = "pin-after-work",
            Revision = Revision + 3,
            Type = "conditions-changed",
            Payload = new ConditionsChanged("g1", new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }),
        };
        Assert.True(Planner().Replay([.. history, roll, work, pin]).Current!.PlacedCharges.Single().Operable);
    }

    [Fact]
    public async Task ADrEqualToTheEnemySanCallsForASniperAttack()
    {
        // A14.1 (ruling R15.5): the German IFT DR of 6 equals the Russian SAN; the Sniper's dr 1 and Random Location DR (north, one hex) from E7 find E6,
        // whose closest German unit, g1 in E3, is broken. Since pass 71 (A14.2; ruling R71.12) the Russian side first chooses to attack rather than forfeit.
        await SetupAt(1, "german", [], Sides(russianSan: 6), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"),
            Sniper("rs", "E7", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3, 3, 3, 3, 3, 1), new
        {
            firers = G1,
            target = L("E4"),
        }));
        Assert.Equal(ChoicePending.Sniper, Current.Choice!.Kind);
        Committed(await Do(GameActions.Choose, Then(3, 1, 1), new
        {
            key = Current.Choice!.Key,
            option = ScenarioA1Sniper.Attack,
        }));
        var sniped = Since(before).Select(item => item.Payload).OfType<SniperAttacked>().Single();
        Assert.Equal("broken", sniped.Result);
        Assert.Equal("g1", sniped.Unit);
        Assert.True(Is(Current.Unit("g1")!, Conditions.Broken));
        Assert.True(Is(Current.Unit("g1")!, Conditions.DesperationMorale));
        Assert.Equal(L("E3"), Current.Location("rs")!.Location.ToString());
    }

    [Fact]
    public async Task ACommissarMustRallyHisLocationAndReplacesAUnitThatFails()
    {
        // A25.222 (ruling R15.6): the RPh does not end while the Commissar has not tried to rally r1.
        await SetupAt(0, "russian", Unit("r1", "defender-squad", "E3", "russian", "asl:broken", "asl:dm"), Unit("rc", "defender-commissar-9-0", "E3", "russian"),
            Unit("g1", "attacker-squad", "H8", "german"));
        Refused(await Do(GameActions.AdvancePhase, NoRoll(), new
        {
        }), "play.commissar-rally");

        // Another leader may not rally in his Location; with him, r1 is immune to DM and its broken morale 7 is 8: 6 + 5 = 11 fails, and r1 is Replaced.
        var before = Revision;
        Committed(await Do(GameActions.Rally, Then(3, 6, 5), new
        {
            unitId = "r1",
            leader = "rc",
        }));
        var rally = Since(before).Select(item => item.Payload).OfType<RallyAttempted>().Single();
        var resolution = rally.Resolution.Deserialize<RallyResolution>(LiveFire.Json)!;
        Assert.DoesNotContain(resolution.Arithmetic!.Drm, item => item.Name == "desperation-morale");
        Assert.Equal(8, resolution.Arithmetic.MoraleLevel);
        Assert.True(resolution.Effect!.ReplacedByCommissar);
        Assert.Equal("defender-conscript-squad", resolution.Effect.FinalDefinitionId);
        await Advance();
    }

    [Fact]
    public async Task AnNkvdFieldPromotionCreatesACommissar()
    {
        // A25.25 (ruling R15.7): the first MMC Self-Rally's Original 2; a Leader Creation dr of 3, +1 broken, is 4: an 8+1 Commissar.
        await SetupAt(0, "russian", Unit("r1", "defender-nkvd-squad", "E3", "russian", "asl:broken"), Unit("g1", "attacker-squad", "H8", "german"));
        var before = Revision;
        Committed(await Do(GameActions.Rally, Then(3, 1, 1, 3), new
        {
            unitId = "r1",
        }));
        if (Current.Choice is { } pending)
        {
            Committed(await Do(GameActions.Choose, Then(3, 3), new
            {
                key = pending.Key,
                option = "take",
            }));
        }

        Assert.Contains(Since(before).Select(item => item.Payload).OfType<InstanceCreated>(),
            item => item.Instance.Definition == "defender-commissar-8-plus-1");
    }

    [Fact]
    public async Task AnAlliedTroopsLeaderDirectsOneWorse()
    {
        // A10.7 (ruling R15.8): the German 8-1 directs an Italian squad at 0, not -1; the two nationalities fire together.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Unit("gi", "italian-squad", "E3", "german"),
            Unit("gl", "attacker-leader-8-1", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1Gi,
            director = "gl",
            target = L("E4"),
        }));
        Assert.Contains(Resolution(Fires(before).Single()).Arithmetic!.Drm, item => item.Name == "leadership:gl" && item.Value == 0m);
    }

    [Fact]
    public async Task AnUnderscoredSquadFailingBeyondItsElrBecomesTwoBrokenHalfSquads()
    {
        // A1.23, A19.13 (ruling R15.9; referee, pass 15): two 4 FP squads PBF are 16, DR 5: 3MC; the NKVD squad's 11 + 3 = 14 fails its 8 by 6, beyond the
        // ELR of 5 its underscored Morale Factor gives it.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("r1", "defender-nkvd-squad", "E4", "russian"));
        Committed(await Do(GameActions.Fire, Then(3, 2, 3, 6, 5), new
        {
            firers = G1G2,
            target = L("E4"),
        }));
        UnitInstance[] halves = [.. Current.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Definition?.Definition == "defender-nkvd-half-squad")];
        Assert.Equal(2, halves.Length);
        Assert.All(halves, half => Assert.True(Is(half, Conditions.Broken)));
    }

    [Fact]
    public async Task AGreenSquadIsInexperiencedAloneAndNotWithALeader()
    {
        // A19.3 (ruling R15.10): the British 4-3-6 is attacked; its Inexperience is the stack's fact.
        await SetupAt(1, "german", [], Sides(enemy: "british"), Unit("g1", "attacker-squad", "E3", "german"), Unit("b1", "british-green-squad", "E4", "british"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = G1,
            target = L("E4"),
        }));
        var facts = Fires(before).Single().Facts.Deserialize<FireAttack>(LiveFire.Json)!;
        Assert.True(facts.Targets!.Single().Inexperienced);
    }

    [Fact]
    public async Task AHeroFiresAMgAtFullFpWithTheTwoManDrmNegated()
    {
        // A15.23 (ruling R15.11): the LMG's 2 FP doubled for PBF, +1 for a SW needing two men, -1 heroic.
        await SetupAt(1, "german", Unit("gh", "attacker-hero", "E3", "german"), Weapon("lmg", "asl:mg", "attacker-lmg", "gh", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.Fire, Then(3), new
        {
            firers = Gh,
            weapons = new Dictionary<string, string[]> { ["gh"] = ["lmg"] },
            target = L("E4"),
        }));
        var arithmetic = Resolution(Fires(before).Single()).Arithmetic!;
        Assert.Contains(arithmetic.Drm, item => item.Name == "hero-mg:gh" && item.Value == 1m);
        Assert.Contains(arithmetic.Drm, item => item.Name == "heroic:gh" && item.Value == -1m);
        Assert.DoesNotContain(arithmetic.Firers, item => item.UnitId == "gh");
    }

    [Fact]
    public async Task AHeroCreatedByAConcealedUnitIsConcealed()
    {
        // A15.21, A12.1 (ruling R15.12): a leader rallies the concealed broken squad on an Original 2; Heat of Battle 1 + 1, +2 Russian, +1 broken, is 5: a
        // hero, and Battle Hardening; no Good Order enemy sees them, so the "?" stays.
        await SetupAt(0, "russian", Unit("r1", "defender-squad", "E3", "russian", "asl:broken", "asl:concealed"), Unit("rl", "defender-leader-8-1", "E3", "russian"),
            Unit("g1", "attacker-squad", "H8", "german", "asl:broken"));
        var before = Revision;
        Committed(await Do(GameActions.Rally, Then(3, 1, 1, 1, 1), new
        {
            unitId = "r1",
            leader = "rl",
        }));

        // Ruling R5.8: the owner takes the Battle Hardening.
        if (Current.Choice is { } pending)
        {
            Committed(await Do(GameActions.Choose, NoRoll(), new
            {
                key = pending.Key,
                option = "take",
            }));
        }

        var hero = Since(before).Select(item => item.Payload).OfType<InstanceCreated>().Single(item => item.Instance.Kind == "asl:hero");
        Assert.True(Is(Current.Unit(hero.Instance.Id)!, Conditions.Concealed));
    }

    [Fact]
    public async Task ABerserkUnitKeepsTheOnePortagePointSwItsOwnerNames()
    {
        // A15.431 (ruling R15.14): four LMG exceed a squad's IPC of three; the owner keeps the second, third, and fourth, so the first is left.
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german", "asl:berserk"), Weapon("m1", "asl:mg", "attacker-lmg", "g1", "german"),
            Weapon("m2", "asl:mg", "attacker-lmg", "g1", "german"), Weapon("m3", "asl:mg", "attacker-lmg", "g1", "german"),
            Weapon("m4", "asl:mg", "attacker-lmg", "g1", "german"), Unit("r1", "defender-squad", "E6", "russian"));
        Committed(await Do(GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4"),
            keep = KeepThree,
        }));
        Assert.NotEqual("g1", (Current.Find("m1") as EquipmentInstance)?.Holding?.Holder);
        Assert.Equal("g1", (Current.Find("m2") as EquipmentInstance)?.Holding?.Holder);
    }

    [Fact]
    public async Task AThrownDcAsksItsOwnersOptionsAndThenAttacksItsThrowerAndIsRemoved()
    {
        // Ruling R27.4 (pass 27): the DC's attack reaches the Russian squad's Original 2 and its Battle Hardening option; the attack waits for the
        // answer, and only then attacks the thrower's Location and removes the DC.
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        var before = Revision;
        Committed(await Do(GameActions.ThrowDc, Then(6, 3, 3, 1, 1, 3, 3), new
        {
            unitId = "g1",
            equipmentId = "dc",
            target = L("E4"),
        }));
        Assert.Equal(ChoicePending.BattleHardening, Current.Choice!.Kind);
        Assert.Empty(Fires(before));
        Assert.Equal(InstanceStatus.Active, Current.Find("dc")!.Status);
        Committed(await Do(GameActions.Choose, Then(6), new
        {
            key = Current.Choice!.Key,
            option = "decline",
        }));
        var fires = Fires(before);
        Assert.Equal(2, fires.Length);
        Assert.Contains(Resolution(fires[1]).Arithmetic!.Drm, item => item.Name == "thrower-location" && item.Value == 3m);
        Assert.Equal(InstanceStatus.Eliminated, Current.Find("dc")!.Status);
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }
}
