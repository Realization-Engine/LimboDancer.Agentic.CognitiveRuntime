using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 20 of the Scenario Card Games Plan in live play (rulings R20.1 to R20.6): a game from a card ends after its last Game Turn, rolls for the first
/// move and the Balance as it starts, applies the Balance counters, sets up its reinforcements off board and enters them along their edge in their MPh,
/// and keeps play within the card's playable area. Board 01 with its real terrain; Gambit on open stand-in boards 4 and 2.
/// </summary>
public sealed class BacklogPass20Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a720");
    private static readonly GameScope Scope = new(Tenant, "p20");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bs9 = ["bs9"];
    private static readonly string[] Bs9AndB1 = ["bs9", "b1"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p20-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly Queue<int> dice = new();

    public BacklogPass20Tests() => store = new FileGameStore(root);

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }

    /// <summary>An open stand-in board, verified, for Gambit's boards 4 and 2.</summary>
    private static BoardHandle OpenBoard(string board)
    {
        var open = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var center = new LocationFacts(0, open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, 0, false, center, [center], sides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse(board), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle(), OpenBoard("bd04"), OpenBoard("bd02")]), Vocabulary, [Catalog]);

    /// <summary>The dice the test queues, then 6s: each die is its value.</summary>
    private DiceRoller Roller() => new(_ => (dice.Count > 0 ? dice.Dequeue() : 6) - 1);

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: Roller());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private static Dictionary<string, object?> Start(string card, object? balance = null, string? firstSide = null)
    {
        var start = new Dictionary<string, object?>
        {
            ["label"] = card,
            ["catalog"] = "asl-scenario-a1@1.13.0",
            ["boards"] = new[] { "bd01" },
            ["firstSide"] = firstSide,
            ["sides"] = Array.Empty<object>(),
            ["scenario"] = new
            {
                id = card,
                sha256 = ScenarioCards.Sha256(card),
                title = card
            },
        };
        if (balance is not null)
        {
            start["balance"] = balance;
        }

        return start;
    }

    /// <summary>Setup placements; the first carries the start.</summary>
    private async Task<PlayResult> Place(string card, object? balance, params Dictionary<string, object>[] placements) => await PlaceWith(Start(card, balance), placements);

    private async Task<PlayResult> PlaceWith(Dictionary<string, object?> start, params Dictionary<string, object>[] placements)
    {
        var node = JsonSerializer.SerializeToNode(new
        {
            gameId = Scope.Game,
            attemptId = $"setup-{Revision}",
            expectedRevision = Revision,
            placements,
        })!.AsObject();
        if (Revision == 0)
        {
            node["start"] = JsonSerializer.SerializeToNode(start);
        }

        return await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node));
    }

    private async Task<PlayResult> Act(Abstractions.Actions.ActionDescriptor action, object? arguments = null)
    {
        var node = arguments is null ? new System.Text.Json.Nodes.JsonObject() : JsonSerializer.SerializeToNode(arguments)!.AsObject();
        node["gameId"] = Scope.Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{Revision}";
        node["expectedRevision"] = Revision;
        return await Commit(Play(), action, JsonSerializer.SerializeToElement(node));
    }

    private Task<PlayResult> Advance() => Act(GameActions.AdvancePhase);

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = at == "off" ? new Dictionary<string, object> { ["offMap"] = true } : new Dictionary<string, object> { ["at"] = at },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    private static Dictionary<string, object> Weapon(string id, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    /// <summary>A group's whole OB on board 01, squads three to a hex of its buildings, SMC with the first squad, SW held by the area's first squad.</summary>
    private static List<Dictionary<string, object>> Ob(ScenarioCard card, string side, int groupIndex)
    {
        var group = card.Sides.Single(item => item.Side == side).Groups[groupIndex];
        var id = ScenarioCards.GroupId(side, groupIndex);
        var placements = new List<Dictionary<string, object>>();
        var squadsAt = new Dictionary<string, int>(StringComparer.Ordinal);
        var holders = new Dictionary<string, string>(StringComparer.Ordinal);
        var serial = 0;
        foreach (var line in group.Units)
        {
            var areas = line.Area is { } named ? group.Areas.Where(area => area.Id == named).ToArray() : [.. group.Areas.Where(area => area.Kind == "building")];
            var hexes = areas.SelectMany(area => area.Hexes!).ToArray();
            var definition = Catalog.Definition(line.Definition)!;
            for (var count = 0; count < line.Count; count++)
            {
                var unitId = $"{id}-{++serial}";
                if (Vocabulary.IsA(definition.Kind, "asl:equipment"))
                {
                    placements.Add(Weapon(unitId, line.Definition, holders[areas[0].Id], side));
                    continue;
                }

                var hex = definition.Kind == "asl:squad" ? hexes.First(item => squadsAt.GetValueOrDefault(item) < 3) : hexes[0];
                if (definition.Kind == "asl:squad")
                {
                    squadsAt[hex] = squadsAt.GetValueOrDefault(hex) + 1;
                    holders.TryAdd(areas[0].Id, unitId);
                }

                placements.Add(Unit(unitId, line.Definition, $"bd01:{hex}:0", side, id));
            }
        }

        return placements;
    }

    private async Task SetUpGuards(object? balance = null, params Dictionary<string, object>[] extra)
    {
        var card = Card("guards-counterattack");
        Committed(await Place("guards-counterattack", balance, [.. Ob(card, "german", 0), .. extra]));
        Committed(await Place("guards-counterattack", balance, [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
    }

    // R20.1 (A3.9): The Guards Counterattack ends after the German Player Turn of Game Turn 5; nothing happens after it.
    [Fact]
    public async Task TheGuardsCounterattackEndsAfterItsLastGameTurn()
    {
        await SetUpGuards();
        PlayResult? last = null;
        for (var step = 0; step < 200 && Current.Ended is null; step++)
        {
            last = await Advance();
            Committed(last);
        }

        Assert.Equal((5, "last-game-turn"), (Current.Ended!.Turn, Current.Ended.Reason));
        Assert.Contains(last!.Reasons, reason => reason.StartsWith("play.game-ended", StringComparison.Ordinal));
        Assert.Equal(("german", "ccph"), (Current.PhasingSide, Current.Phase));
        Refused(await Advance(), "play.game-over");
        Refused(await Act(GameActions.Rally), "play.game-over");
    }

    // R20.1 (A3.9): a half turn ends the game after the first side's Player Turn of the last Game Turn.
    [Fact]
    public void AHalfTurnEndsAfterTheFirstSidesPlayerTurn()
    {
        var turns = Card("guards-counterattack").Turns;
        Assert.False(ScenarioCards.EndsAfter(turns, 4, false));
        Assert.False(ScenarioCards.EndsAfter(turns, 5, true));
        Assert.True(ScenarioCards.EndsAfter(turns, 5, false));
        Assert.True(ScenarioCards.EndsAfter(turns with
        {
            HalfTurn = true
        }, 5, true));
        Assert.False(ScenarioCards.EndsAfter(turns with
        {
            HalfTurn = true
        }, 4, true));
    }

    // R20.2 (A3.9): The Tractor Works rolls for the first move as it starts: a dr for each side in the card's order, the higher moving first.
    [Fact]
    public async Task TheTractorWorksRollsForTheFirstMove()
    {
        var card = Card("tractor-works");
        Refused(await PlaceWith(Start("tractor-works", firstSide: "german"), [.. Ob(card, "russian", 0)]), "play.scenario");
        dice.Enqueue(3);
        dice.Enqueue(3);
        dice.Enqueue(2);
        dice.Enqueue(5);
        Committed(await Place("tractor-works", null, [.. Ob(card, "russian", 0)]));
        Assert.Equal(("german", "german"), (Current.FirstSide, Current.PhasingSide));
        var rolls = store.Read(Scope)!.Events.Select(item => item.Payload).OfType<DiceRolled>().ToArray();
        Assert.Equal(2, rolls.Length);
        Assert.All(rolls, roll => Assert.Equal("first-move", roll.Purpose));
    }

    // R20.3, R20.4 (A26.4): both players wish to play the Russians; the dr gives them to Ann, and Ben plays the Germans with their Balance, a Hero.
    [Fact]
    public async Task ADrDecidesTheSideBothPlayersWishAndTheOtherTakesItsBalance()
    {
        var balance = new
        {
            players = new[] { new { name = "Ann", wants = "russian" }, new { name = "Ben", wants = "russian" } }
        };
        dice.Enqueue(4);
        dice.Enqueue(2);
        await SetUpGuards(balance, Unit("hero", "attacker-hero", "bd01:F5:0", "german", "german-1"));
        Assert.Equal("german", Current.Scenario!.Balance);
        Assert.Equal([new ScenarioPlayer("Ann", "russian"), new ScenarioPlayer("Ben", "german")], Current.Scenario.Players);
        Assert.Contains(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "balance");
        Committed(await Advance());
    }

    // R20.4: without the Balance, its Hero is not in the German OB.
    [Fact]
    public async Task WithoutTheBalanceItsCountersAreRefused()
    {
        var card = Card("guards-counterattack");
        Refused(await Place("guards-counterattack", null, [.. Ob(card, "german", 0), Unit("hero", "attacker-hero", "bd01:F5:0", "german", "german-1")]), "play.setup-pool");
        Committed(await Place("guards-counterattack", new
        {
            side = "german"
        }, [.. Ob(card, "german", 0), Unit("hero", "attacker-hero", "bd01:F5:0", "german", "german-1")]));
        Assert.Equal("german", Current.Scenario!.Balance);
        Assert.Empty(Current.Scenario.Players);
    }

    // R20.4: Gambit's German Balance, a LMG, joins the German OB when the Germans take it.
    [Fact]
    public void GambitsGermanBalanceAddsALmg()
    {
        var card = Card("gambit");
        SetupCounter Counter(string id, string definition, string hex, bool equipment = false) =>
            new(id, "german", "german-1", definition, Catalog.Definition(definition)!.Kind, BoardLocation.Parse($"bd04:{hex}:0"), false, false, false, equipment, true);
        List<SetupCounter> counters = [Counter("lmg", "attacker-lmg", "C9", equipment: true)];
        var none = ScenarioSetup.Check(card, counters, _ => "open-ground", _ => true, ScenarioA1FireReference.HalfSquadOf, card.Date.Month);
        Assert.Contains(none.Reasons, reason => reason.StartsWith("play.setup-pool", StringComparison.Ordinal));
        var taken = ScenarioSetup.Check(card, counters, _ => "open-ground", _ => true, ScenarioA1FireReference.HalfSquadOf, card.Date.Month, "german");
        Assert.DoesNotContain(taken.Reasons, reason => reason.StartsWith("play.setup-pool", StringComparison.Ordinal));
    }

    private static Dictionary<string, object?> GambitStart() => new()
    {
        ["label"] = "gambit",
        ["catalog"] = "asl-scenario-a1@1.13.0",
        ["boards"] = new[] { "bd04" },
        ["sides"] = Array.Empty<object>(),
        ["scenario"] = new
        {
            id = "gambit",
            sha256 = ScenarioCards.Sha256("gambit"),
            title = "gambit"
        },
    };

    /// <summary>Gambit's British: the five of SSR 2 in hexes 5 to 7 of board 4, and the rest off board, with <paramref name="deployed"/> squads Deployed.</summary>
    private static List<Dictionary<string, object>> BritishOffBoard(int deployed)
    {
        var placements = new List<Dictionary<string, object>>();
        for (var index = 0; index < 10; index++)
        {
            if (index < deployed)
            {
                placements.Add(Unit($"bh{index}a", "british-elite-half-squad", "off", "british", "british-1"));
                placements.Add(Unit($"bh{index}b", "british-elite-half-squad", "off", "british", "british-1"));
            }
            else
            {
                placements.Add(Unit($"bs{index}", "british-elite-squad", "off", "british", "british-1"));
            }
        }

        placements.AddRange([Weapon("blmg1", "british-lmg", "bs9", "british"), Weapon("blmg2", "british-lmg", "bs8", "british"),
            Weapon("bmtr1", "british-light-mortar", "bs7", "british"), Weapon("bmtr2", "british-light-mortar", "bs6", "british"), Weapon("batr", "british-atr", "bs5", "british")]);
        return placements;
    }

    // R20.5 (A2.5, A2.51, A2.9): Gambit's British set up five counters, the rest off board; they enter along the north edge in their MPh of Turn 1.
    [Fact]
    public async Task GambitsBritishWaitOffBoardAndEnterInTheirMovementPhase()
    {
        Committed(await PlaceWith(GambitStart(), Unit("b1", "british-elite-squad", "bd04:E5:0", "british", "british-1"),
            Unit("b2", "british-elite-squad", "bd04:E6:0", "british", "british-1"), Unit("l1", "british-leader-9-1", "bd04:E5:0", "british", "british-1"),
            Unit("l2", "british-leader-8-0", "bd04:E6:0", "british", "british-1"), Unit("l3", "british-leader-8-0", "bd04:E7:0", "british", "british-1")));

        // The Germans set up after the British five; a German counter has no entry and never waits off board.
        Refused(await PlaceWith(GambitStart(), Unit("g0", "attacker-elite-squad-5-4-8", "off", "german", "german-1")), "play.setup-pool");
        List<Dictionary<string, object>> german = [.. Enumerable.Range(0, 8).Select(index => Unit($"g{index}", "attacker-elite-squad-5-4-8", $"bd04:{(char)('C' + (index / 2))}9:0", "german", "german-1")),
            Unit("gl1", "attacker-leader-9-2", "bd04:C9:0", "german", "german-1"), Unit("gl2", "attacker-leader-9-1", "bd04:D9:0", "german", "german-1"),
            Unit("gl3", "attacker-leader-8-1", "bd04:E9:0", "german", "german-1")];
        Committed(await PlaceWith(GambitStart(), [.. german]));
        Refused(await Advance(), "play.setup-incomplete");

        // A2.9: 10% (FRU) of the ten squads entering on Turn 1 is one.
        Refused(await PlaceWith(GambitStart(), [.. BritishOffBoard(2)]), "play.setup-deployment");
        Committed(await PlaceWith(GambitStart(), [.. BritishOffBoard(1)]));
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Complete);

        // The German Player Turn, then the British RPh and PFPh.
        for (var phase = 0; phase < 10; phase++)
        {
            Committed(await Advance());
        }

        // Ruling R25.1 (pass 25): the MPh may end with them waiting, since they may enter by advance; the APh holds them instead (BacklogPass25Tests).
        Assert.Equal(("british", "mph"), (Current.PhasingSide, Current.Phase));
        var top = Planner().EntryHexes(Current, "top");
        Assert.Contains(BoardLocation.Parse("bd04:E1:0"), top);
        Assert.DoesNotContain(BoardLocation.Parse("bd04:E5:0"), top);

        Refused(await Act(GameActions.Move, new
        {
            unitIds = Bs9,
            to = "bd04:E5:0"
        }), "play.entry-edge");
        Refused(await Act(GameActions.Move, new
        {
            unitIds = Bs9AndB1,
            to = "bd04:E4:0"
        }), "play.entry-stack");
        var stacks = new[] { new[] { "bs9", "bs8", "bs7" }, new[] { "bs6", "bs5", "bs4" }, new[] { "bs3", "bs2", "bs1" }, new[] { "bh0a", "bh0b" } };
        var hexes = new[] { "bd04:E1:0", "bd04:G1:0", "bd04:I1:0", "bd04:K1:0" };
        for (var index = 0; index < stacks.Length; index++)
        {
            // The last stack, the Deployed squad's two HS, enters by Assault Movement (A4.61).
            var assault = index == stacks.Length - 1;
            var entered = await Act(GameActions.Move, new
            {
                unitIds = stacks[index],
                to = hexes[index],
                assault
            });
            Committed(entered);
            Assert.Contains(entered.Reasons, reason => reason.StartsWith("play.enter", StringComparison.Ordinal) && reason.Contains("Assault", StringComparison.Ordinal) == assault);
            Committed(await Act(GameActions.PassFire));
            Committed(await Act(GameActions.EndMove, new
            {
                unitIds = stacks[index]
            }));
        }

        Assert.Equal(BoardLocation.Parse("bd04:E1:0"), Current.Location("blmg1")!.Location);
        Assert.Equal(1, Current.Unit("bs9")!.MfSpent);
        Committed(await Advance());
    }

    // R20.3, R20.5 (referee, pass 20): nothing waits off board in a group that never enters, a Balance Hero included, and no Dummy waits off board.
    [Fact]
    public void OnlyEnteringCountersWaitOffBoard()
    {
        var card = Card("guards-counterattack");
        SetupCounter OffBoard(string id, string? definition, string kind, bool dummy = false) =>
            new(id, "german", "german-1", definition, kind, null, dummy, false, dummy, false, true)
            {
                OffBoard = true
            };
        var hero = ScenarioSetup.Check(card, [OffBoard("hero", "attacker-hero", "asl:hero")], _ => "open-ground", _ => true, ScenarioA1FireReference.HalfSquadOf, 10, "german");
        Assert.Contains(hero.Reasons, reason => reason.StartsWith("play.setup-pool", StringComparison.Ordinal));
        var dummy = ScenarioSetup.Check(card, [OffBoard("d1", null, UnitKinds.Dummy, dummy: true)], _ => "open-ground", _ => true, ScenarioA1FireReference.HalfSquadOf, 10);
        Assert.Contains(dummy.Reasons, reason => reason.StartsWith("play.setup-offboard", StringComparison.Ordinal));
    }

    // R20.3 (referee, pass 20): players who wish different sides play them, and may still agree to give one side its Balance.
    [Fact]
    public async Task PlayersWhoWishDifferentSidesMayAgreeOnTheBalance()
    {
        var balance = new
        {
            side = "german",
            players = new[] { new { name = "Ann", wants = "russian" }, new { name = "Ben", wants = "german" } }
        };
        await SetUpGuards(balance, Unit("hero", "attacker-hero", "bd01:F5:0", "german", "german-1"));
        Assert.Equal("german", Current.Scenario!.Balance);
        Assert.Equal([new ScenarioPlayer("Ann", "russian"), new ScenarioPlayer("Ben", "german")], Current.Scenario.Players);
        Assert.DoesNotContain(store.Read(Scope)!.Events.Select(item => item.Payload).OfType<DiceRolled>(), roll => roll.Purpose == "balance");
    }

    // R20.6 (A2.1).
    [Fact]
    public void ThePlayableAreaIsTheCardsHexrows()
    {
        var guards = Card("guards-counterattack");
        Assert.True(ScenarioCards.Playable(guards, BoardLocation.Parse("bd01:P5:0")));
        Assert.False(ScenarioCards.Playable(guards, BoardLocation.Parse("bd01:Q5:0")));
        var tractor = Card("tractor-works");
        Assert.False(ScenarioCards.Playable(tractor, BoardLocation.Parse("bd01:N7:0")));
        Assert.True(ScenarioCards.Playable(tractor, BoardLocation.Parse("bd01:O7:0")));
        Assert.True(ScenarioCards.Playable(tractor, BoardLocation.Parse("bd01:GG5:0")));
        Assert.True(ScenarioCards.Playable(Card("gambit"), BoardLocation.Parse("bd02:A1:0")));

        // Referee, pass 20: hexrows limit their own board; the card's other boards play whole.
        var twoBoards = Card("gambit") with
        {
            PlayableArea = new ScenarioCardArea("Only hexrows A to P of board 4.", true, new ScenarioCardHexrows("A", "P", "bd04"))
        };
        Assert.False(ScenarioCards.Playable(twoBoards, BoardLocation.Parse("bd04:Q5:0")));
        Assert.True(ScenarioCards.Playable(twoBoards, BoardLocation.Parse("bd02:Q5:0")));
        Assert.Equal(32, ScenarioCards.HexrowIndex("GG"));
        Assert.Null(ScenarioCards.HexrowIndex("AB"));
    }

    // R20.6: no step leaves the playable area; the Russians move first in The Guards Counterattack.
    [Fact]
    public async Task NoMoveLeavesThePlayableArea()
    {
        await SetUpGuards();
        Committed(await Advance());
        Committed(await Advance());
        Assert.Equal(("russian", "mph"), (Current.PhasingSide, Current.Phase));
        var squad = Current.Units.First(unit => unit.Side == "russian" && unit.Kind == "asl:squad" && Current.Location(unit.Id)!.Location.Hex.ToString() == "N3").Id;
        string[] mover = [squad];
        Committed(await Act(GameActions.Move, new
        {
            unitIds = mover,
            to = "bd01:O3:0",
            doubleTime = true
        }));
        Committed(await Act(GameActions.PassFire));
        Committed(await Act(GameActions.Move, new
        {
            unitIds = mover,
            to = "bd01:P3:0"
        }));
        Committed(await Act(GameActions.PassFire));
        Refused(await Act(GameActions.Move, new
        {
            unitIds = mover,
            to = "bd01:Q3:0"
        }), "play.playable");
    }
}
