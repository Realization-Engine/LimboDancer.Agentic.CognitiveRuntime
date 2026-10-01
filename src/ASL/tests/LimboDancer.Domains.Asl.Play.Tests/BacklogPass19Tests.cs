using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 19 of the Scenario Card Games Plan in live play (rulings R19.1 to R19.6): a game from a card sets up its OB, group by group in the card's
/// order, in the groups' areas, never overstacked, with its OB "?" in Concealment Terrain and up to 10% of its squads Deployed; play starts when
/// every group that sets up on board has finished. Board 01 with its real terrain.
/// </summary>
public sealed class BacklogPass19Tests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000a719");
    private static readonly GameScope Scope = new(Tenant, "p19");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-p19-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;

    public BacklogPass19Tests() => store = new FileGameStore(root);

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

    private GamePlanner Planner() => new(store, new InMemoryBoardCatalog([Board01Fixture.Handle()]), Vocabulary, [Catalog]);

    // Pass 20 (ruling R20.2): The Tractor Works rolls for the first move as it starts; each roll's first die is a 6, its second a 1.
    private static DiceRoller FirstMoveRolls()
    {
        var die = 0;
        return new(_ => die++ % 2 == 0 ? 5 : 0);
    }

    private GamePlay Play() => new(Planner(), store, new NullAudit(), roller: FirstMoveRolls());

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static ScenarioCard Card(string name) => ScenarioCards.Read(name, Catalog)!.Card!;

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        return proposed.Outcome != PlayOutcome.NeedsConfirmation ? proposed : await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private static Dictionary<string, object?> Start(string card, string? firstSide = null) => new()
    {
        ["label"] = card,
        ["catalog"] = "asl-scenario-a1@1.13.0",
        ["boards"] = Bd01,
        ["firstSide"] = firstSide,
        ["sides"] = Array.Empty<object>(),
        ["scenario"] = new
        {
            id = card,
            sha256 = ScenarioCards.Sha256(card),
            title = card
        },
    };

    /// <summary>Setup placements; the first carries the start.</summary>
    private async Task<PlayResult> Place(string card, string? firstSide, params Dictionary<string, object>[] placements)
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
            node["start"] = JsonSerializer.SerializeToNode(Start(card, firstSide));
        }

        return await Commit(Play(), GameActions.Setup, JsonSerializer.SerializeToElement(node));
    }

    private async Task<PlayResult> Advance() => await Commit(Play(), GameActions.AdvancePhase, JsonSerializer.SerializeToElement(new
    {
        gameId = Scope.Game,
        attemptId = $"advance-{Revision}",
        expectedRevision = Revision,
    }));

    private static void Committed(PlayResult result) => Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));

    private static void Refused(PlayResult result, string prefix)
    {
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static Dictionary<string, object> Unit(string id, string definition, string at, string side, string group, bool concealed = false, bool hidden = false) => new()
    {
        ["id"] = id,
        ["kind"] = Catalog.Definition(definition)!.Kind,
        ["definition"] = definition,
        ["side"] = side,
        ["group"] = group,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = concealed, ["asl:hidden"] = hidden },
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

    private static Dictionary<string, object> Dummy(string id, string at, string side, string group) => new()
    {
        ["id"] = id,
        ["kind"] = UnitKinds.Dummy,
        ["side"] = side,
        ["group"] = group,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:concealed"] = true, ["asl:hidden"] = false },
    };

    /// <summary>
    /// A group's whole OB on board 01, as a player sets it up: each line in its area (or the group's first areas), squads spread over the area's hexes
    /// three to a hex, SMC with the first squad, SW held by a squad of the area.
    /// </summary>
    private static List<Dictionary<string, object>> Ob(ScenarioCard card, string side, int groupIndex)
    {
        var sideCard = card.Sides.Single(item => item.Side == side);
        var group = sideCard.Groups[groupIndex];
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

    /// <summary>The OB's squads that hold no SW, which a test may take out or replace.</summary>
    private static List<Dictionary<string, object>> FreeSquads(List<Dictionary<string, object>> placements)
    {
        var holders = placements.Where(item => item.ContainsKey("holding")).Select(item => JsonSerializer.SerializeToElement(item["holding"]).GetProperty("holder").GetString())
            .ToHashSet(StringComparer.Ordinal);
        return [.. placements.Where(item => (string)item["kind"] == "asl:squad" && !holders.Contains((string)item["id"]))];
    }

    // R19.1 to R19.3: each group sets up its own OB in its own areas, in the card's order.
    [Fact]
    public async Task TheGuardsCounterattackSetsUpGroupByGroup()
    {
        var card = Card("guards-counterattack");
        Refused(await Place("guards-counterattack", null, Unit("r1", "defender-squad", "bd01:N4:0", "russian", "russian-1")), "play.setup-order");
        Refused(await Place("guards-counterattack", null, Unit("g1", "attacker-squad", "bd01:A1:0", "german", "german-1")), "play.setup-area");
        Refused(await Place("guards-counterattack", null, Unit("g1", "attacker-elite-squad", "bd01:F5:0", "german", "german-1")), "play.setup-pool");
        var offMap = Unit("g0", "attacker-squad", "bd01:F6:0", "german", "german-1");
        offMap.Remove("position");
        Refused(await Place("guards-counterattack", null, offMap), "play.setup-area");

        // R19.3 (referee, pass 19): the upper level of a two-level building is a Location of the building.
        Committed(await Place("guards-counterattack", null, Unit("g1", "attacker-squad", "bd01:F5:1", "german", "german-1")));
        Refused(await Advance(), "play.setup-incomplete");

        // R19.2 (referee, pass 19): no other action starts play before the setup is done.
        Refused(await Commit(Play(), GameActions.Rally, JsonSerializer.SerializeToElement(new
        {
            gameId = Scope.Game,
            attemptId = $"rally-{Revision}",
            expectedRevision = Revision,
        })), "play.setup-incomplete");

        var german = Ob(card, "german", 0);
        german.Remove(FreeSquads(german).First(item => ((string)JsonSerializer.SerializeToElement(item["position"]).GetProperty("at").GetString()!).Contains(":F", StringComparison.Ordinal)));
        Committed(await Place("guards-counterattack", null, [.. german]));
        Refused(await Place("guards-counterattack", null, Unit("g99", "attacker-squad", "bd01:F5:0", "german", "german-1")), "play.setup-pool");
        Refused(await Advance(), "play.setup-incomplete");

        Committed(await Place("guards-counterattack", null, [.. Ob(card, "russian", 0), .. Ob(card, "russian", 1)]));
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Complete);
        Committed(await Advance());
    }

    // R19.3 (A5.1): never overstacked at setup.
    [Fact]
    public async Task NoLocationIsOverstackedAtSetup()
    {
        var card = Card("guards-counterattack");
        Committed(await Place("guards-counterattack", null, [.. Ob(card, "german", 0)]));
        Refused(await Place("guards-counterattack", null, Unit("r1", "defender-guards-squad", "bd01:F3:0", "russian", "russian-2"),
            Unit("r2", "defender-guards-squad", "bd01:F3:0", "russian", "russian-2"), Unit("r3", "defender-guards-squad", "bd01:F3:0", "russian", "russian-2"),
            Unit("r4", "defender-guards-squad", "bd01:F3:0", "russian", "russian-2")), "play.setup-stacking");
    }

    // R19.6 (A2.9): up to 10% (FRU) of a side's squads set up Deployed, both HS of each: two of the Germans' thirteen (referee, pass 19).
    [Fact]
    public async Task TwoGermanSquadsOfThirteenMayDeploy()
    {
        var card = Card("guards-counterattack");
        var german = Ob(card, "german", 0);
        var squads = FreeSquads(german).Where(item => ((string)JsonSerializer.SerializeToElement(item["position"]).GetProperty("at").GetString()!)
            is "bd01:F5:0" or "bd01:F6:0" or "bd01:G6:0" or "bd01:H5:0").ToList();
        var half = ScenarioA1FireReference.HalfSquadOf("attacker-squad")!;

        var inK5 = FreeSquads(german).First(item => ((string)JsonSerializer.SerializeToElement(item["position"]).GetProperty("at").GetString()!)
            is "bd01:J4:0" or "bd01:J5:0" or "bd01:K4:0" or "bd01:K5:0");
        var three = german.Except([.. squads.Take(2), inK5]).ToList();
        three.AddRange([Unit("h1", half, "bd01:F5:0", "german", "german-1"), Unit("h2", half, "bd01:F5:0", "german", "german-1"),
            Unit("h3", half, "bd01:G6:0", "german", "german-1"), Unit("h4", half, "bd01:G6:0", "german", "german-1"),
            Unit("h5", half, "bd01:J4:0", "german", "german-1"), Unit("h6", half, "bd01:J4:0", "german", "german-1")]);
        Refused(await Place("guards-counterattack", null, [.. three]), "play.setup-deployment");

        var odd = german.Except(squads.Take(1)).ToList();
        odd.Add(Unit("h1", half, "bd01:F5:0", "german", "german-1"));
        Refused(await Place("guards-counterattack", null, [.. odd]), "play.setup-deployment");

        var two = german.Except(squads.Take(2)).ToList();
        two.AddRange([Unit("h1", half, "bd01:F5:0", "german", "german-1"), Unit("h2", half, "bd01:G6:0", "german", "german-1"),
            Unit("h3", half, "bd01:H5:0", "german", "german-1"), Unit("h4", half, "bd01:H5:0", "german", "german-1")]);
        Committed(await Place("guards-counterattack", null, [.. two]));
        Assert.True(Planner().CardSetup(Current, new HashSet<string>())!.Groups.Single(group => group.Side == "german").Complete);
    }

    // R19.5 (A12.11, A12.12): a group's OB "?" in Concealment Terrain, within its allotment; none for a group the card gives none; no HIP.
    [Fact]
    public async Task OnlyOrderOfBattleConcealmentInConcealmentTerrain()
    {
        var card = Card("guards-counterattack");
        var concealed = Ob(card, "german", 0);
        concealed[0] = Unit((string)concealed[0]["id"], "attacker-squad", "bd01:F5:0", "german", "german-1", concealed: true);
        Refused(await Place("guards-counterattack", null, [.. concealed]), "play.setup-concealment");
        var hidden = Ob(card, "german", 0);
        hidden[0] = Unit((string)hidden[0]["id"], "attacker-squad", "bd01:F5:0", "german", "german-1", hidden: true);
        Refused(await Place("guards-counterattack", null, [.. hidden]), "play.setup-hidden");
    }

    // R19.2, R19.5: The Tractor Works: the 308th sets up first in X3 with up to 18 "?", in its building.
    [Fact]
    public async Task TheTractorWorksGivesThe308thEighteenQuestionMarks()
    {
        var card = Card("tractor-works");
        var russian = Ob(card, "russian", 0);
        var dummies = Enumerable.Range(1, 18).Select(index => Dummy($"d{index}", "bd01:X3:0", "russian", "russian-1")).ToList();
        Refused(await Place("tractor-works", null, [.. russian, .. dummies, Dummy("d19", "bd01:X4:0", "russian", "russian-1")]), "play.setup-concealment");
        Refused(await Place("tractor-works", null, [.. russian, Dummy("d1", "bd01:T7:0", "russian", "russian-1")]), "play.setup-area");
        Committed(await Place("tractor-works", null, [.. russian, .. dummies]));
        var report = Planner().CardSetup(Current, new HashSet<string>())!;
        Assert.Equal(0, report.Groups.Single(group => group.Id == "russian-1").DummiesLeft);
        Assert.Equal(2, report.CurrentOrder);
        Refused(await Place("tractor-works", null, Unit("r99", "defender-line-squad", "bd01:P8:0", "russian", "russian-2")), "play.setup-order");
    }

    // R19.2 (A12.12; table player, pass 19): once a later group has begun, an earlier one adds nothing, not even a "?".
    [Fact]
    public async Task AFinishedGroupAddsNoQuestionMarkAfterALaterOneBegins()
    {
        var card = Card("tractor-works");
        Committed(await Place("tractor-works", null, [.. Ob(card, "russian", 0), Dummy("d1", "bd01:X3:0", "russian", "russian-1")]));
        Committed(await Place("tractor-works", null, [.. Ob(card, "german", 1)]));
        Refused(await Place("tractor-works", null, Dummy("d2", "bd01:X4:0", "russian", "russian-1")), "play.setup-order");
    }

    // R19.1 (referee and table player, pass 19): a SW sets up possessed by a unit of its group, not on its own.
    [Fact]
    public async Task AnUnheldSupportWeaponIsRefused()
    {
        var mmg = new Dictionary<string, object>
        {
            ["id"] = "mg1",
            ["kind"] = "asl:mg",
            ["definition"] = "attacker-mmg",
            ["side"] = "german",
            ["position"] = new
            {
                at = "bd01:A1:0"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        };
        var result = await Place("guards-counterattack", null, Unit("g1", "attacker-squad", "bd01:F5:0", "german", "german-1"), mmg);
        Assert.NotEqual(PlayOutcome.Committed, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.Contains("sets up possessed by a unit of its OB group", StringComparison.Ordinal));
    }

    private static string? Terrain(BoardLocation at) => "open-ground";

    // R19.4: Gambit's SSR 2: five British counters in hexrows 5 to 7 of board 4, at least two MMC, none under "?"; the rest enter.
    [Fact]
    public void GambitSetsUpFiveBritishCounters()
    {
        var card = Card("gambit");
        SetupCounter Counter(string id, string definition, string hex, bool concealed = false) =>
            new(id, "british", "british-1", definition, Catalog.Definition(definition)!.Kind, BoardLocation.Parse($"bd04:{hex}:0"), concealed, false, false, false, true);
        SetupReport Check(params SetupCounter[] counters) =>
            ScenarioSetup.Check(card, counters, Terrain, key => key is not null, ScenarioA1FireReference.HalfSquadOf, card.Date.Month);

        var one = Check(Counter("b1", "british-elite-squad", "E5"), Counter("b2", "british-leader-9-1", "E5"), Counter("b3", "british-leader-8-0", "E6"),
            Counter("b4", "british-leader-8-0", "E6"), Counter("b5", "british-elite-squad", "E7"));
        Assert.Empty(one.Reasons);
        Assert.True(one.Groups.Single(group => group.Side == "british").Complete);
        Assert.Equal(2, one.CurrentOrder);

        var fewMmc = Check(Counter("b1", "british-elite-squad", "E5"), Counter("b2", "british-leader-9-1", "E5"), Counter("b3", "british-leader-8-0", "E6"),
            Counter("b4", "british-leader-8-0", "E6"), Counter("b5", "british-lmg", "E7") with
            {
                Equipment = true
            });
        var british = fewMmc.Groups.Single(group => group.Side == "british");
        Assert.False(british.Complete);

        // Table player, pass 19: the group says what it still owes.
        Assert.Contains(new SetupNeed("british-1", "forward", "MMC among them", 1), british.Remaining);
        var three = Check(Counter("b1", "british-elite-squad", "E5"), Counter("b2", "british-elite-squad", "E6"), Counter("b3", "british-leader-8-0", "E6"));
        Assert.Contains(new SetupNeed("british-1", "forward", "counters", 2), three.Groups.Single(group => group.Side == "british").Remaining);

        var six = Check([.. Enumerable.Range(1, 6).Select(index => Counter($"b{index}", "british-elite-squad", $"E{5 + (index % 3)}"))]);
        Assert.Contains(six.Reasons, reason => reason.StartsWith("play.setup-limit", StringComparison.Ordinal));

        var outside = Check(Counter("b1", "british-elite-squad", "E8"));
        Assert.Contains(outside.Reasons, reason => reason.StartsWith("play.setup-area", StringComparison.Ordinal));

        // In woods the terrain allows "?", so the SSR is what refuses it (referee, pass 19).
        var covered = ScenarioSetup.Check(card, [Counter("b1", "british-elite-squad", "E5", concealed: true)], _ => "woods", key => key is not null,
            ScenarioA1FireReference.HalfSquadOf, card.Date.Month);
        Assert.Contains(covered.Reasons, reason => reason.StartsWith("play.setup-concealment", StringComparison.Ordinal) && reason.Contains("the SSRs forbid", StringComparison.Ordinal));
    }

    // R19.3 (A2.9): never where Infantry could not enter in play.
    [Fact]
    public void NoCounterSetsUpWhereItCouldNotEnter()
    {
        var card = Card("guards-counterattack");
        var report = ScenarioSetup.Check(card,
            [new SetupCounter("g1", "german", "german-1", "attacker-squad", "asl:squad", BoardLocation.Parse("bd01:F5:0"), false, false, false, false, true)],
            _ => "water", key => key != "water", ScenarioA1FireReference.HalfSquadOf, 10);
        Assert.Contains(report.Reasons, reason => reason.StartsWith("play.setup-terrain", StringComparison.Ordinal));
    }

    // R19.6, R19.7 (pass 19b): an 8-3-8 of The Tractor Works sets up Deployed as its two 3-3-8, through the planner on board 01.
    [Fact]
    public async Task AnEngineerSquadSetsUpDeployed()
    {
        var card = Card("tractor-works");
        Committed(await Place("tractor-works", null, [.. Ob(card, "russian", 0)]));
        var german = Ob(card, "german", 0);
        var squad = FreeSquads(german).First();
        Assert.Equal("attacker-elite-squad-8-3-8", squad["definition"]);
        var at = JsonSerializer.SerializeToElement(squad["position"]).GetProperty("at").GetString()!;
        german.Remove(squad);
        var half = ScenarioA1FireReference.HalfSquadOf("attacker-elite-squad-8-3-8")!;
        Assert.Equal("attacker-elite-half-squad-3-3-8", half);
        Committed(await Place("tractor-works", null, [.. german, Unit("h1", half, at, "german", "german-1"), Unit("h2", half, at, "german", "german-1")]));
        var group = Planner().CardSetup(Current, new HashSet<string>())!.Groups.Single(item => item.Id == "german-1");
        Assert.True(group.Complete);
    }

    // R19.6, R19.7 (pass 19b): one of Gambit's eight German 5-4-8 may set up Deployed as its two 2-3-8 (10% FRU is one), not two.
    [Fact]
    public void OneOfGambitsCircledESquadsMayDeploy()
    {
        var card = Card("gambit");
        SetupCounter Counter(string id, string definition, string hex) =>
            new(id, "german", "german-1", definition, Catalog.Definition(definition)!.Kind, BoardLocation.Parse($"bd04:{hex}:0"), false, false, false, false, true);
        SetupCounter British(string id, string definition, string hex) =>
            new(id, "british", "british-1", definition, Catalog.Definition(definition)!.Kind, BoardLocation.Parse($"bd04:{hex}:0"), false, false, false, false, true);
        SetupReport Check(int deployed)
        {
            var counters = new List<SetupCounter>
            {
                Counter("l1", "attacker-leader-9-2", "C9"), Counter("l2", "attacker-leader-9-1", "D9"), Counter("l3", "attacker-leader-8-1", "E9"),

                // The British five, who set up first (SSR 2).
                British("b1", "british-elite-squad", "E5"), British("b2", "british-leader-9-1", "E5"), British("b3", "british-leader-8-0", "E6"),
                British("b4", "british-leader-8-0", "E6"), British("b5", "british-elite-squad", "E7"),
            };
            var hexes = new[] { "C9", "C9", "D9", "D9", "E9", "E9", "F9", "F9" };
            for (var index = 0; index < 8; index++)
            {
                if (index < deployed)
                {
                    counters.Add(Counter($"h{index}a", "attacker-elite-half-squad-2-3-8", hexes[index]));
                    counters.Add(Counter($"h{index}b", "attacker-elite-half-squad-2-3-8", hexes[index]));
                }
                else
                {
                    counters.Add(Counter($"s{index}", "attacker-elite-squad-5-4-8", hexes[index]));
                }
            }

            return ScenarioSetup.Check(card, counters, Terrain, key => key is not null, ScenarioA1FireReference.HalfSquadOf, card.Date.Month);
        }

        var one = Check(1);
        Assert.Empty(one.Reasons);
        Assert.True(one.Groups.Single(group => group.Id == "german-1").Complete);
        Assert.Contains(Check(2).Reasons, reason => reason.StartsWith("play.setup-deployment", StringComparison.Ordinal));
    }
}
