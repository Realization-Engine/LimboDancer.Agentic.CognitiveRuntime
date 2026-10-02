using System.Text.Json;
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
/// The backlog pass 15 table player's situations in live play (rulings R15.1 to R15.14): FT, MOL, Thrown and Placed DC, Snipers, Commissars, NKVD Field Promotion, Allied Troops,
/// underscored Morale Factors, Green MMC, a hero's MG, a hero created concealed, and a berserk unit's kept SW. A board 01 grid of Open Ground, fixed dice,
/// and a clear LOS everywhere.
/// </summary>
public sealed class BacklogPass15TablePlayerTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a715");
    private static readonly GameScope Scope = new(Tenant, "tp15");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] R1 = ["r1"];
    private static readonly string[] Gi = ["gi"];
    private static readonly string[] G1Gi = ["g1", "gi"];
    private static readonly string[] Gh = ["gh"];
    private static readonly string[] G1G2 = ["g1", "g2"];
    private static readonly string[] G1G2G3 = ["g1", "g2", "g3"];
    private static readonly string[] G2 = ["g2"];
    private static readonly string[] H1 = ["h1"];
    private static readonly string[] R1R2 = ["r1", "r2"];
    private static readonly string[] R2 = ["r2"];
    private static readonly string[] R2R3R4 = ["r2", "r3", "r4"];
    private static readonly string[] KeepThree = ["m2", "m3", "m4"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-tp15-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass15TablePlayerTests() => store = new FileGameStore(root);

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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board()]), Vocabulary, [Catalog], fireLos: los);

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

    private IFireLosReader los = new ClearLos();

    /// <summary>A LOS with a brush Hindrance one hex out, for shots at range 2 or more.</summary>
    private sealed class BrushLos : IFireLosReader
    {
        public LosResult? Read(GameState state, BoardLocation from, BoardLocation target)
        {
            var range = Board01Fixture.Handle().Distance(from.Hex, target.Hex) ?? 1;
            return range < 2 ? new(LosStatus.Clear, false, range, 0, null, string.Empty)
                : new(LosStatus.Clear, false, range, 1, null, string.Empty)
                {
                    Hindrances = [new LosHindrance(1, 1) { Terrains = ["Brush"] }]
                };
        }
    }

    private static Dictionary<string, object> Dummy(string id, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:dummy",
        ["side"] = side,
        ["position"] = new
        {
            at = L(hex)
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    /// <summary>One action; <paramref name="what"/> names the step for the reader.</summary>
    private Task<PlayResult> Try(string what, Abstractions.Actions.ActionDescriptor action, DiceRoller roller, object arguments) =>
        string.IsNullOrEmpty(what) ? throw new ArgumentException("Name the step.", nameof(what)) : Do(action, roller, arguments);

    private async Task<PlayResult> TryAdvance(string what) => await Try(what, GameActions.AdvancePhase, NoRoll(), new
    {
    });

    private static Dictionary<string, string[]> Uses(string unit, string weapon) => new() { [unit] = [weapon] };

    // 1. A22.1: a FT is never halved for the AFPh.
    [Fact]
    public async Task AFlamethrowerInTheAfphIsNotHalved()
    {
        await SetupAt(4, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"));
        Assert.Equal("afph", Current.Phase);
        var before = Revision;
        var result = await Try("ft afph", GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E4")
        });
        Committed(result);
        Assert.Equal(24m, Resolution(Fires(before).Single()).Arithmetic!.TotalFirepower);
    }

    // 2. A22.32: no Long Range FT through an obstructed LOS; 12 FP at Long Range otherwise.
    [Fact]
    public async Task AFlamethrowerAtLongRange()
    {
        los = new BrushLos();
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E5", "russian"));
        var hindered = await Try("ft long range through brush", GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E5")
        });
        los = new ClearLos();
        var before = Revision;
        var clear = await Try("ft long range clear", GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E5")
        });
        Assert.NotEqual(PlayOutcome.Committed, hindered.Outcome);
        Committed(clear);
        Assert.Equal(12m, Resolution(Fires(before).Single()).Arithmetic!.TotalFirepower);
    }

    // 3. A22.3: a squad that fired its inherent FP may still fire its FT in a separate attack.
    [Fact]
    public async Task ASquadFiresItsInherentFpAndThenItsFlamethrower()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"));
        var inherent = await Try("inherent", GameActions.Fire, Then(6), new
        {
            firers = G1,
            target = L("E4")
        });
        var before = Revision;
        var ft = await Try("ft after inherent", GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E2")
        });
        Committed(inherent);
        Committed(ft);
        Assert.Equal(24m, Resolution(Fires(before).Single()).Arithmetic!.TotalFirepower);
    }

    // 4. A22.3, A15.24: a HS fires a FT; a hero fires one with no heroic DRM, and need not say it fires without inherent FP.
    [Fact]
    public async Task AHalfSquadAndAHeroFireFlamethrowers()
    {
        await SetupAt(1, "german", Unit("h1", "attacker-half-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "h1", "german"),
            Unit("gh", "attacker-hero", "G3", "german"), Weapon("ft2", "asl:ft", "attacker-ft", "gh", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "G4", "russian"));
        var before = Revision;
        var half = await Try("hs ft", GameActions.Fire, Then(3), new
        {
            firers = H1,
            weapons = Uses("h1", "ft"),
            withoutInherent = H1,
            target = L("E4")
        });
        var halfFires = Fires(before);
        var middle = Revision;
        var hero = await Try("hero ft", GameActions.Fire, Then(3), new
        {
            firers = Gh,
            weapons = Uses("gh", "ft2"),
            target = L("G4")
        });
        Committed(half);
        Assert.Equal(24m, Resolution(halfFires.Single()).Arithmetic!.TotalFirepower);
        Committed(hero);
        var arithmetic = Resolution(Fires(middle).Single()).Arithmetic!;
        Assert.Equal(24m, arithmetic.TotalFirepower);
        Assert.DoesNotContain(arithmetic.Drm, item => item.Name.StartsWith("heroic", StringComparison.Ordinal) || item.Name.StartsWith("hero-mg", StringComparison.Ordinal));
    }

    // 5. A22.3: a unit uses one FT per Player Turn: after Defensive First Fire with it, no Final Fire with it.
    [Fact]
    public async Task AFlamethrowerIsUsedOncePerPlayerTurn()
    {
        await SetupAt(2, "german", Unit("r1", "defender-squad", "E3", "russian"), Weapon("rft", "asl:ft", "defender-ft", "r1", "russian"),
            Unit("g1", "attacker-squad", "E5", "german"), Unit("g2", "attacker-squad", "E2", "german"));
        Committed(await Try("g1 moves to E4", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        }));
        var dff = await Try("ft dff", GameActions.Fire, Then(6, 3, 4), new
        {
            firers = R1,
            weapons = Uses("r1", "rft"),
            withoutInherent = R1,
            target = L("E4")
        });
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        await TryAdvance("to dfph");
        var again = await Try("ft final fire at E2", GameActions.Fire, Then(6), new
        {
            firers = R1,
            weapons = Uses("r1", "rft"),
            withoutInherent = R1,
            target = L("E2")
        });
        Committed(dff);
        Assert.NotEqual(PlayOutcome.Committed, again.Outcome);
    }

    // 6. A22.3: one FT or DC per Player Turn.
    [Fact]
    public async Task TwoDemolitionChargesInOnePhase()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc1", "asl:dc", "attacker-dc", "g1", "german"),
            Weapon("dc2", "asl:dc", "attacker-dc", "g1", "german"), Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"));
        var first = await Try("throw dc1", GameActions.ThrowDc, Then(6, 2, 2), new
        {
            unitId = "g1",
            equipmentId = "dc1",
            target = L("E4")
        });
        var second = await Try("throw dc2", GameActions.ThrowDc, Then(6, 2, 2), new
        {
            unitId = "g1",
            equipmentId = "dc2",
            target = L("E2")
        });
        Committed(first);
        Assert.False(Is(Current.Unit("g1")!, Conditions.Broken));
        Assert.NotEqual(PlayOutcome.Committed, second.Outcome);
    }

    // 6b. A22.3: one FT or DC per Player Turn: a squad fires its inherent FP, Throws a DC, and then may not fire its FT too.
    [Fact]
    public async Task InherentFpThenADcThenAFlamethrower()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc1", "asl:dc", "attacker-dc", "g1", "german"),
            Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"), Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"),
            Unit("r3", "defender-squad", "D3", "russian"));
        var inherent = await Try("inherent at E4", GameActions.Fire, Then(6), new
        {
            firers = G1,
            target = L("E4")
        });
        var dc = await Try("throw dc1 at E2", GameActions.ThrowDc, Then(6, 2, 2), new
        {
            unitId = "g1",
            equipmentId = "dc1",
            target = L("E2")
        });
        var ft = await Try("ft at D3", GameActions.Fire, Then(3), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("D3")
        });
        Committed(inherent);
        Committed(dc);
        Assert.False(Is(Current.Unit("g1")!, Conditions.Broken));
        Assert.NotEqual(PlayOutcome.Committed, ft.Outcome);
    }

    // 7. A23.2: a squad Throwing a DC may use its inherent FP in the same phase (DFPh).
    [Fact]
    public async Task AThrownDcInTheDfphAndTheSquadsInherentFp()
    {
        await SetupAt(3, "german", Unit("r1", "defender-squad", "E3", "russian"), Weapon("rdc", "asl:dc", "defender-dc", "r1", "russian"),
            Unit("g1", "attacker-squad", "E4", "german"), Unit("g2", "attacker-squad", "E2", "german"));
        Assert.Equal("dfph", Current.Phase);
        var before = Revision;
        var thrown = await Try("throw dc dfph", GameActions.ThrowDc, Then(6, 4, 4, 5, 5), new
        {
            unitId = "r1",
            equipmentId = "rdc",
            target = L("E4")
        });
        Assert.False(Is(Current.Unit("r1")!, Conditions.Broken));
        var fires = Fires(before);
        var inherent = await Try("inherent after dc", GameActions.Fire, Then(6), new
        {
            firers = R1,
            target = L("E2")
        });
        Committed(thrown);
        Assert.Equal(2, fires.Length);
        Committed(inherent);
    }

    // 8. A23.6: a DC Thrown as Defensive First Fire at the moving stack; the thrower is then marked First Fire (A8.1).
    [Fact]
    public async Task AThrownDcAsDefensiveFirstFire()
    {
        await SetupAt(2, "german", Unit("r1", "defender-squad", "E3", "russian"), Weapon("rdc", "asl:dc", "defender-dc", "r1", "russian"),
            Unit("g1", "attacker-squad", "E5", "german"), Unit("g2", "attacker-squad", "E5", "german"));
        Committed(await Try("stack moves to E4", GameActions.Move, NoRoll(), new
        {
            unitIds = G1G2,
            to = L("E4")
        }));
        var before = Revision;
        var thrown = await Try("throw dc dff", GameActions.ThrowDc, Then(6, 4, 4), new
        {
            unitId = "r1",
            equipmentId = "rdc",
            target = L("E4")
        });
        var fires = Fires(before);
        var marked = Is(Current.Unit("r1")!, Conditions.FirstFire) || Is(Current.Unit("r1")!, Conditions.FinalFire);
        var inherentFirstFire = await Try("r1 inherent dff at same stack", GameActions.Fire, Then(6), new
        {
            firers = R1,
            target = L("E4")
        });
        Committed(thrown);
        Assert.Equal(2, fires.Length);
        Assert.True(marked, "the thrower should be marked First Fire");
    }

    // 9. A22.611: a MOL Check in Defensive First Fire, and none in Final Fire.
    [Fact]
    public async Task AMolInFirstFireAndThenFinalFire()
    {
        await SetupAt(2, "german", ["mol:russian"], Sides(), Unit("r1", "defender-squad", "E3", "russian"), Unit("g1", "attacker-squad", "E5", "german"));
        Committed(await Try("g1 moves to E4", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        }));
        var before = Revision;
        var dff = await Try("mol dff", GameActions.Fire, Then(5, 2), new
        {
            firers = R1,
            target = L("E4"),
            mol = "r1"
        });
        var fires = Fires(before);
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        await TryAdvance("to dfph");
        var final = await Try("mol final fire", GameActions.Fire, Then(5, 2), new
        {
            firers = R1,
            target = L("E4"),
            mol = "r1"
        });
        var plain = await Try("final fire without mol", GameActions.Fire, Then(5), new
        {
            firers = R1,
            target = L("E4")
        });
        Committed(dff);
        Assert.True(Resolution(fires.Single()).MolCheck!.Passed);
        Refused(final, "play.fire-mol");
        Committed(plain);
    }

    // 10. A22.6111: a colored 6 breaks the MOL user and voids its FP and the MOL's; the rest of the FG still attacks.
    [Fact]
    public async Task AMolColoredSixBreaksItsUser()
    {
        await SetupAt(1, "russian", ["mol:russian"], Sides(), Unit("r1", "defender-squad", "E3", "russian"), Unit("r2", "defender-squad", "E3", "russian"),
            Unit("g1", "attacker-squad", "E4", "german"));
        var before = Revision;
        var result = await Try("mol colored 6", GameActions.Fire, Then(6, 2, 6, 3), new
        {
            firers = R1R2,
            target = L("E4"),
            mol = "r1"
        });
        Committed(result);
        var resolution = Resolution(Fires(before).Single());
        Assert.True(resolution.MolCheck!.UserBroken);
        Assert.True(Is(Current.Unit("r1")!, Conditions.Broken));
        Assert.Equal(8m, resolution.Arithmetic!.TotalFirepower);
    }

    // 11. A22.611: a squad making the MOL Check fires no other SW.
    [Fact]
    public async Task AMolSquadMayNotFireItsLmg()
    {
        await SetupAt(1, "russian", ["mol:russian"], Sides(), Unit("r1", "defender-squad", "E3", "russian"), Weapon("rlmg", "asl:mg", "defender-lmg", "r1", "russian"),
            Unit("g1", "attacker-squad", "E4", "german"));
        var result = await Try("mol with lmg", GameActions.Fire, Then(5, 2), new
        {
            firers = R1,
            weapons = Uses("r1", "rlmg"),
            target = L("E4"),
            mol = "r1"
        });
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
    }

    // 12. A23.3: a placer broken by Defensive First Fire in the Placement Location keeps its DC; the AFPh then ends.
    [Fact]
    public async Task APlacerBrokenBeforeItMovesOn()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E1", "russian"));
        Committed(await Try("place", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        var dff = await Try("r2 dff at placer", GameActions.Fire, Then(6, 3, 3, 6, 5), new
        {
            firers = R2,
            target = L("E3")
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        for (var index = 0; index < 4 && Current.Phase != "rtph"; index++)
        {
            if ((await TryAdvance("advance from " + Current.Phase)).Outcome != PlayOutcome.Committed)
            {
                break;
            }
        }

        Committed(dff);
        var holder = (Current.Find("dc") as EquipmentInstance)?.Holding?.Holder;
        Assert.NotNull(holder);
        Assert.True(Is(Current.Unit(holder!)!, Conditions.Broken));
        Assert.Equal("rtph", Current.Phase);
    }

    // 13. A23.3: once operably Placed, the placer may suffer adverse results with no effect to the DC; it still detonates in the AFPh.
    [Fact]
    public async Task APlacerEliminatedAfterAnOperablePlacement()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "D2", "russian"), Unit("r3", "defender-squad", "D2", "russian"),
            Unit("r4", "defender-squad", "D2", "russian"));
        Committed(await Try("place", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        Committed(await Try("g1 moves to E2", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2")
        }));
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        await TryAdvance("to dfph");
        var kill = await Try("kill placer", GameActions.Fire, Then(1), new
        {
            firers = R2R3R4,
            target = L("E2")
        });
        await TryAdvance("to afph");
        var detonate = await Try("detonate", GameActions.DetonateDc, Then(3), new
        {
            equipmentId = "dc"
        });
        var end = await TryAdvance("end afph");
        Committed(kill);
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("g1")?.Status ?? InstanceStatus.Eliminated);
        Committed(detonate);
        Committed(end);
    }

    // 14. A23.4: a DC Placed in an empty Location still detonates; a friendly unit that moved in is attacked.
    [Fact]
    public async Task APlacedDcInALocationThatIsEmptyOrHoldsFriends()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("g2", "attacker-squad", "E5", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        Committed(await Try("place in empty E4", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        }));
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        var move = await Try("g2 into E4", GameActions.Move, NoRoll(), new
        {
            unitIds = G2,
            to = L("E4")
        });
        await Try("pass", GameActions.PassFire, NoRoll(), new
        {
        });
        await Try("end move", GameActions.EndMove, NoRoll(), new
        {
        });
        await TryAdvance("to dfph");
        await TryAdvance("to afph");
        var before = Revision;
        var detonate = await Try("detonate", GameActions.DetonateDc, Then(3), new
        {
            equipmentId = "dc"
        });
        var end = await TryAdvance("end afph");
        Committed(detonate);
        Committed(end);
        if (move.Outcome == PlayOutcome.Committed && Current.Location("g2")?.Location.ToString() == L("E4"))
        {
            var facts = Fires(before).Single().Facts.Deserialize<FireAttack>(LiveFire.Json)!;
            Assert.Contains(facts.Targets!, item => item.UnitId == "g2");
        }
    }

    // 15. A14.2, A14.23: several possible Sniper targets, one of them a concealed stack with a Dummy.
    [Fact]
    public async Task ASniperAmongSeveralTargetsAndAConcealedStack()
    {
        await SetupAt(1, "german", [], Sides(russianSan: 6), Unit("h1", "attacker-half-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("g3", "attacker-squad", "E3", "german", "asl:concealed"), Dummy("gd", "E3", "german"), Unit("r1", "defender-squad", "E6", "russian"),
            Sniper("rs", "E7", "russian"));
        var before = Revision;
        var result = await Try("fire DR 6", GameActions.Fire, Then(6, 3, 3, 1, 1, 1, 1, 1, 6), new
        {
            firers = H1,
            target = L("E6")
        });
        Committed(result);
        var sniped = Since(before).Select(item => item.Payload).OfType<SniperAttacked>().ToArray();
        Assert.Contains(sniped, item => item.Unit == "g3" && item.Result == "broken");
        Assert.True(Is(Current.Unit("g3")!, Conditions.Broken));
        Assert.False(Is(Current.Unit("g3")!, Conditions.Concealed));
    }

    // 16. A14.3: a Sniper dr of 2 eliminates a Dummy stack.
    [Fact]
    public async Task ASniperEliminatesADummyStackOnATwo()
    {
        await SetupAt(1, "german", [], Sides(russianSan: 6), Unit("h1", "attacker-half-squad", "E3", "german"), Dummy("gd", "E5", "german"),
            Unit("r1", "defender-squad", "E6", "russian"), Sniper("rs", "E7", "russian"));
        var before = Revision;
        var result = await Try("fire DR 6", GameActions.Fire, Then(6, 3, 3, 2, 1, 1), new
        {
            firers = H1,
            target = L("E6")
        });
        Committed(result);
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<SniperAttacked>(), item => item.Result == "dummy-stack-eliminated");
        Assert.NotEqual(InstanceStatus.Active, Current.Unit("gd")!.Status);
    }

    // 17. Ruling R15.5: a side with a SAN and no Sniper counter makes no Sniper attack.
    [Fact]
    public async Task ASideWithNoSniperCounter()
    {
        await SetupAt(1, "german", [], Sides(russianSan: 6), Unit("h1", "attacker-half-squad", "E3", "german"), Unit("r1", "defender-squad", "E6", "russian"));
        var before = Revision;
        var result = await Try("fire DR 6", GameActions.Fire, Then(6, 3, 3, 1, 1, 1), new
        {
            firers = H1,
            target = L("E6")
        });
        Committed(result);
        Assert.Empty(Since(before).Select(item => item.Payload).OfType<SniperAttacked>());
    }

    // 19. A25.222: a Commissar rallies a Disrupted broken squad; the RPh ends after the attempt.
    [Fact]
    public async Task ACommissarAndADisruptedSquad()
    {
        await SetupAt(0, "russian", Unit("r1", "defender-squad", "E3", "russian", "asl:broken", "asl:disrupted", "asl:dm"),
            Unit("rc", "defender-commissar-9-0", "E3", "russian"), Unit("g1", "attacker-squad", "H8", "german"));
        var early = await TryAdvance("advance before rally");
        await Try("rally", GameActions.Rally, Then(3, 6, 5), new
        {
            unitId = "r1",
            leader = "rc"
        });
        if (Current.Phase == "rph")
        {
            await TryAdvance("advance after rally");
        }

        Assert.NotEqual(PlayOutcome.Committed, early.Outcome);
        Assert.NotEqual("rph", Current.Phase);
    }

    // 20. A25.222: a Commissar with two broken squads rallies both; the RPh waits for the second one too.
    [Fact]
    public async Task ACommissarWithTwoBrokenSquads()
    {
        await SetupAt(0, "russian", Unit("r1", "defender-squad", "E3", "russian", "asl:broken"), Unit("r2", "defender-squad", "E3", "russian", "asl:broken"),
            Unit("rc", "defender-commissar-9-0", "E3", "russian"), Unit("g1", "attacker-squad", "H8", "german"));
        Committed(await Try("rally r1", GameActions.Rally, Then(3, 2, 3), new
        {
            unitId = "r1",
            leader = "rc"
        }));
        var early = await TryAdvance("advance with r2 unrallied");
        var rally = await Try("rally r2", GameActions.Rally, Then(3, 2, 3), new
        {
            unitId = "r2",
            leader = "rc"
        });
        var end = await TryAdvance("advance after both");
        Refused(early, "play.commissar-rally");
        Committed(rally);
        Committed(end);
    }

    // 21. A25.222: a Commissar going berserk takes the other units of his Location with him.
    [Fact]
    public async Task ACommissarGoesBerserk()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Unit("rc", "defender-commissar-9-0", "E4", "russian"),
            Unit("r1", "defender-squad", "E4", "russian"));
        var result = await Try("fire", GameActions.Fire, Then(3, 2, 3, 1, 1, 6, 5), new
        {
            firers = G1,
            target = L("E4")
        });
        Committed(result);
        Assert.True(Is(Current.Unit("rc")!, Conditions.Berserk));
        Assert.True(Is(Current.Unit("r1")!, Conditions.Berserk));
    }

    // 22. A10.7: a leader directs Allied Troops of another nationality one worse; his own nationality at full.
    [Fact]
    public async Task AlliedTroopsWithALeaderOfEachNationality()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Unit("il", "italian-leader-8-1", "E3", "german"),
            Unit("gi", "italian-squad", "G3", "german"), Unit("il2", "italian-leader-8-1", "G3", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "G4", "russian"));
        var before = Revision;
        var german = await Try("italian leader directs german", GameActions.Fire, Then(6), new
        {
            firers = G1,
            director = "il",
            target = L("E4")
        });
        var middle = Revision;
        var italian = await Try("italian leader directs italian", GameActions.Fire, Then(6), new
        {
            firers = Gi,
            director = "il2",
            target = L("G4")
        });
        Committed(german);
        Committed(italian);
        Assert.Contains(Resolution(Fires(before).First()).Arithmetic!.Drm, item => item.Name == "leadership:il" && item.Value == 0m);
        Assert.Contains(Resolution(Fires(middle).Single()).Arithmetic!.Drm, item => item.Name == "leadership:il2" && item.Value == -1m);
    }

    // 23. A15.1, A15.5: a non-elite Italian surrenders on a Final Heat of Battle DR of 10 or more to an ADJACENT enemy.
    [Fact]
    public async Task AnItalianSurrendersToAnAdjacentEnemy()
    {
        await SetupAt(1, "german", [], Sides(enemy: "italian"), Unit("g1", "attacker-squad", "E3", "german"), Unit("i1", "italian-squad", "E4", "italian"));
        var fire = await Try("fire", GameActions.Fire, Then(3, 2, 3, 1, 1, 4, 4), new
        {
            firers = G1,
            target = L("E4")
        });
        var take = Current.PendingSurrenders.Count > 0
            ? await Try("take prisoner", GameActions.TakePrisoner, NoRoll(), new
            {
                unitId = Current.PendingSurrenders[0].Unit,
                captorId = "g1"
            }) : null;
        var end = await TryAdvance("advance");
        Committed(fire);
        Assert.NotNull(take);
        Committed(take!);
        Assert.True(Is(Current.Units.Single(unit => unit.Side == "italian" && unit.Status == InstanceStatus.Active), Conditions.Captured));
        Committed(end);
    }

    // 24. A15.5: with no ADJACENT enemy, a surrendering unit is only Disrupted; play goes on.
    [Fact]
    public async Task AnItalianSurrendersWithNoAdjacentEnemy()
    {
        await SetupAt(1, "german", [], Sides(enemy: "italian"), Unit("g1", "attacker-squad", "E3", "german"), Unit("g2", "attacker-squad", "E3", "german"),
            Unit("g3", "attacker-squad", "E3", "german"), Unit("i1", "italian-squad", "E6", "italian"));
        var fire = await Try("fire", GameActions.Fire, Then(3, 2, 3, 1, 1, 4, 4), new
        {
            firers = G1G2G3,
            target = L("E6")
        });
        var end = await TryAdvance("advance");
        Committed(fire);
        Committed(end);
    }

    // 24b. A15.5: the only ADJACENT enemy is broken, so the surrendering Italian is only Disrupted; nothing waits for a captor.
    [Fact]
    public async Task AnItalianSurrendersNextToABrokenEnemy()
    {
        await SetupAt(1, "german", [], Sides(enemy: "italian"), Unit("g1", "attacker-squad", "E6", "german"), Unit("g2", "attacker-squad", "E3", "german", "asl:broken"),
            Unit("i1", "italian-squad", "E4", "italian"));
        var fire = await Try("fire", GameActions.Fire, Then(3, 1, 3, 1, 1, 4, 4), new
        {
            firers = G1,
            target = L("E4")
        });
        var pending = Current.PendingSurrenders.Count;
        var end = await TryAdvance("advance");
        Committed(fire);
        Assert.Equal(0, pending);
        Committed(end);
    }

    // 25. Ruling R15.13: a Finnish Original 2 in a Rally creates no leader.
    [Fact]
    public async Task AFinnishRallyOriginalTwo()
    {
        await SetupAt(0, "finnish", [], [new { id = "german", nationality = "german", elr = 3, san = (int?)null }, new { id = "finnish", nationality = "finnish", elr = 5, san = (int?)null }],
            Unit("f1", "finnish-squad", "E3", "finnish", "asl:broken"), Unit("f2", "finnish-squad", "G3", "finnish", "asl:broken"),
            Unit("f3", "finnish-squad", "J3", "finnish", "asl:broken"),
            Unit("fl", "finnish-leader-9-1", "E3", "finnish"), Unit("g1", "attacker-squad", "H8", "german"));
        var before = Revision;
        var firstSelf = await Try("first MMC self rally original 2", GameActions.Rally, Then(3, 1, 1, 1, 1), new
        {
            unitId = "f2"
        });
        for (var index = 0; index < 3 && Current.Choice is { } pending; index++)
        {
            await Try("choose take", GameActions.Choose, NoRoll(), new
            {
                key = pending.Key,
                option = "take"
            });
        }

        var led = await Try("leader rally original 2", GameActions.Rally, Then(3, 1, 1, 3, 3), new
        {
            unitId = "f1",
            leader = "fl"
        });
        for (var index = 0; index < 3 && Current.Choice is { } pending; index++)
        {
            await Try("choose take", GameActions.Choose, NoRoll(), new
            {
                key = pending.Key,
                option = "take"
            });
        }

        var secondSelf = await Try("second MMC self rally (A25.7 capability)", GameActions.Rally, Then(3, 2, 3), new
        {
            unitId = "f3"
        });
        Committed(firstSelf);
        Committed(led);
        Assert.DoesNotContain(Since(before).Select(item => item.Payload).OfType<InstanceCreated>(), item => item.Instance.Kind == "asl:leader");
        Committed(secondSelf);
    }

    // 27. A7.1, A23.3: a squad that fired its FT in the PFPh has fired: it neither moves nor Places a DC in the MPh.
    [Fact]
    public async Task AFlamethrowerUserInThePfphHasFired()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"), Unit("r1", "defender-squad", "E4", "russian"));
        Committed(await Try("ft pfph", GameActions.Fire, Then(6, 3, 4), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E4")
        }));
        var marked = Is(Current.Find("ft")!, Conditions.PrepFire);
        await TryAdvance("to mph");
        var place = await Try("place dc", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4")
        });
        var move = await Try("move", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E2")
        });
        Assert.True(marked, "the FT g1 fired alone in the PFPh carries a Prep Fire counter; g1 itself may still fire its inherent FP in that phase (A22.3)");
        Assert.NotEqual(PlayOutcome.Committed, place.Outcome);
        Assert.NotEqual(PlayOutcome.Committed, move.Outcome);
    }

    // 27b. A7.1: a squad that fired its FT in the PFPh does not fire its inherent FP in the AFPh.
    [Fact]
    public async Task AFlamethrowerUserInThePfphDoesNotFireInTheAfph()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"));
        Committed(await Try("ft pfph", GameActions.Fire, Then(6, 3, 4), new
        {
            firers = G1,
            weapons = Uses("g1", "ft"),
            withoutInherent = G1,
            target = L("E4")
        }));
        await TryAdvance("to mph");
        await TryAdvance("to dfph");
        await TryAdvance("to afph");
        var afph = await Try("inherent in afph", GameActions.Fire, Then(6), new
        {
            firers = G1,
            target = L("E2")
        });
        Assert.Equal("afph", Current.Phase);
        Assert.NotEqual(PlayOutcome.Committed, afph.Outcome);
    }

    // 27c. A23.2: a squad that Throws a DC in the PFPh may still fire its inherent FP in that phase.
    [Fact]
    public async Task ADcThenInherentFpInThePfph()
    {
        await SetupAt(1, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Unit("r1", "defender-squad", "E4", "russian"), Unit("r2", "defender-squad", "E2", "russian"));
        Committed(await Try("throw dc", GameActions.ThrowDc, Then(6, 2, 2), new
        {
            unitId = "g1",
            equipmentId = "dc",
            target = L("E4")
        }));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Broken));
        var inherent = await Try("inherent after dc", GameActions.Fire, Then(6), new
        {
            firers = G1,
            target = L("E2")
        });
        Committed(inherent);
    }

    // 28. A22.611: a HS that fails its MOL Check may not attack at all.
    [Fact]
    public async Task AHalfSquadFailsItsMolCheck()
    {
        await SetupAt(1, "russian", ["mol:russian"], Sides(), Unit("r1", "defender-half-squad", "E3", "russian"), Unit("g1", "attacker-squad", "E4", "german"));
        var before = Revision;
        var result = await Try("hs mol fails", GameActions.Fire, Then(1, 6, 1, 1), new
        {
            firers = R1,
            target = L("E4"),
            mol = "r1"
        });
        Committed(result);
        var resolution = Resolution(Fires(before).Single());
        Assert.False(resolution.MolCheck!.Passed);
        Assert.Equal(0m, resolution.Arithmetic?.TotalFirepower ?? 0m);
        Assert.False(Is(Current.Unit("g1")!, Conditions.Broken));
    }

    private static Dictionary<string, object> Vehicle(string id, string definition, string hex, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = L(hex), ["facing"] = "east" },
        ["conditions"] = new Dictionary<string, bool>(),
    };

    // 29. A23.3, A23.5 (referee and table player, pass 15): no DC is Placed where a vehicle stands, friendly or not, since its attack on a vehicle is not
    // built; so no Placement is left that its AFPh cannot detonate.
    [Fact]
    public async Task NoDcIsPlacedInALocationWithAFriendlyTank()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german"), Weapon("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Vehicle("gt", "attacker-tank", "E4", "german"), Unit("r1", "defender-squad", "H8", "russian"));
        Refused(await Try("place", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E3"),
            placeDc = "dc",
            placeDcAt = L("E4"),
        }), "play.dc-vehicle");
        Assert.Empty(Current.PlacedCharges);
    }

    // 30. A14.1: a Sniper is called by a MC DR equal to the enemy SAN, made by the unit's own side.
    [Fact]
    public async Task AMoraleCheckDrCallsTheEnemySniper()
    {
        await SetupAt(1, "german", [], Sides(germanSan: 7), Unit("g1", "attacker-squad", "E3", "german"), Unit("r1", "defender-squad", "E4", "russian"),
            Unit("r2", "defender-squad", "E6", "russian"), Sniper("gs", "E8", "german"));
        var before = Revision;
        var result = await Try("fire, MC DR 7", GameActions.Fire, Then(6, 2, 3, 3, 4, 1, 1, 1), new
        {
            firers = G1,
            target = L("E4")
        });
        Committed(result);
        Assert.Contains(Since(before).Select(item => item.Payload).OfType<SniperAttacked>(), item => item.Sniper == "gs");
    }

    // 26. A15.431 (ruling R15.14): a berserk squad with four LMG and a FT and no "keep" keeps three LMG.
    [Fact]
    public async Task ABerserkSquadWithMoreLightSwThanItsIpc()
    {
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "E3", "german", "asl:berserk"), Weapon("m1", "asl:mg", "attacker-lmg", "g1", "german"),
            Weapon("m2", "asl:mg", "attacker-lmg", "g1", "german"), Weapon("m3", "asl:mg", "attacker-lmg", "g1", "german"),
            Weapon("m4", "asl:mg", "attacker-lmg", "g1", "german"), Weapon("ft", "asl:ft", "attacker-ft", "g1", "german"), Unit("r1", "defender-squad", "E6", "russian"));
        var move = await Try("charge", GameActions.Move, NoRoll(), new
        {
            unitIds = G1,
            to = L("E4")
        });
        Committed(move);
        var held = Current.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == "g1").Select(item => item.Id).Order().ToArray();
        Assert.Equal(["m1", "m2", "m3"], held);
    }
}
