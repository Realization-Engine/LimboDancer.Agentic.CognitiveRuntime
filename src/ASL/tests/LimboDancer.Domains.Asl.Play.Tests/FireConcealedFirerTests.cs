using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Pass 31d (design D4; ruling R31d.2): fire by concealed units where the target Location holds no Good Order unit. A12.14 (p. 77): a concealed unit
/// that fires loses its "?" in the LOS of a Good Order enemy ground unit within 16 hexes. The Fire package sees the target Location alone and
/// refused such fire as undecided, and a refusal marks no firer, so a side could try a "?" stack and read from the refusal that it held Dummies.
/// The planner now reads who sees each concealed firer, and the attack is made.
/// </summary>
public sealed partial class FireTests
{
    private static readonly string[] Dummies = ["d1", "d2"];

    private static bool Marked(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

    private static Dictionary<string, object> DummyAt(string id, string at, string side) => new()
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

    /// <summary>
    /// The Russian PFPh: r1, r2, and the 8-0 rl concealed in D4; two German Dummies in the stone building E4; the German g2 in A2, hidden or not.
    /// D4 and E4 see each other; A2 sees D4 only when the test says so.
    /// </summary>
    private async Task SetupDummies(bool watched, bool watcherHidden = false)
    {
        los.By = (from, to) => (from.Hex.ToString(), to.Hex.ToString()) is ("D4", "E4") or ("E4", "D4") || (watched && (from.Hex.ToString(), to.Hex.ToString()) is ("A2", "D4") or ("D4", "A2"))
            ? new LosResult(LosStatus.Clear, false, from.Hex.ToString() == "A2" || to.Hex.ToString() == "A2" ? 3 : 1, 0, null, string.Empty)
            : new LosResult(LosStatus.Blocked, true, 3, 0, null, string.Empty);
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.Setup, Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Dummies in the village",
                catalog = "asl-scenario-a1@1.13.0",
                boards = Bd01,
                firstSide = "russian",
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 2 } },
            },
            placements = new object[]
            {
                Placement("r1", "asl:squad", "defender-squad", "bd01:D4:0", "russian", concealed: true),
                Placement("r2", "asl:squad", "defender-squad", "bd01:D4:0", "russian", concealed: true),
                Placement("rl", "asl:leader", "defender-leader", "bd01:D4:0", "russian", concealed: true),
                DummyAt("d1", "bd01:E4:0", "german"),
                DummyAt("d2", "bd01:E4:0", "german"),
                Placement("g2", "asl:squad", "attacker-squad", "bd01:A2:0", "german", hidden: watcherHidden),
            },
        }))).Outcome);
        await Advance();
        Assert.Equal("pfph", Current.Phase);
    }

    [Fact]
    public async Task ConcealedUnitsFireAtDummiesAndKeepTheirConcealmentWhenNoGoodOrderEnemySeesThem()
    {
        await SetupDummies(watched: false);
        var before = Revision;

        // Before pass 31d this was refused as undecided, with no firer marked. IFT 1+1 on the halved column: the Dummies are removed (A12.14).
        var result = await Commit(Play(Once([1, 1, .. Enumerable.Repeat(1, 12)])), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        Assert.True(Revision > before);
        Assert.All(FireGroup, id => Assert.True(Marked(Current.Unit(id)!, Conditions.Concealed), $"{id} lost its \"?\" with no Good Order enemy unit in LOS."));
        Assert.All(FireGroup, id => Assert.True(Marked(Current.Unit(id)!, Conditions.PrepFire), $"{id} is not marked as having fired."));
        Assert.All(Dummies, id => Assert.Equal(InstanceStatus.Eliminated, Current.Find(id)!.Status));

        // The record carries the planner's read, so the attack is verified at every later replay as it was resolved.
        var fire = Assert.Single(store.Read(Scope)!.Events.Skip((int)before).Select(item => item.Payload).OfType<FireResolved>());
        Assert.All(fire.Facts.GetProperty("firers").EnumerateArray(), firer => Assert.False(firer.GetProperty("seenByGoodOrderEnemy").GetBoolean()));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task ConcealedUnitsThatFireAtDummiesLoseTheirConcealmentWhenAnotherGoodOrderEnemySeesThem()
    {
        await SetupDummies(watched: true);

        // 6+6: no effect on the Dummies; the German squad in A2 has the firers in its LOS within 16 hexes, so they lose "?" (A12.14).
        var result = await Commit(Play(Once(6, 6)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        Assert.All(FireGroup, id => Assert.False(Marked(Current.Unit(id)!, Conditions.Concealed), $"{id} kept its \"?\" in the LOS of a Good Order enemy unit."));
        Assert.All(Dummies, id => Assert.Equal(InstanceStatus.Active, Current.Find(id)!.Status));
    }

    [Fact]
    public async Task AHiddenUnitDoesNotForceAConcealedFirersLossAndSoDoesNotShowItself()
    {
        await SetupDummies(watched: true, watcherHidden: true);
        var result = await Commit(Play(Once(6, 6)), GameActions.Fire, Fire("fire-1", ["r1", "r2"]));
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
        Assert.All(FireGroup, id => Assert.True(Marked(Current.Unit(id)!, Conditions.Concealed)));
        Assert.True(Marked(Current.Unit("g2")!, Conditions.Hidden));
    }

    [Fact]
    public async Task TheProposalReadsTheSameWhetherTheStackIsDummiesOrUnits()
    {
        // What the firing side reads before Confirm is the same for a stack of Dummies as for concealed units: accepted, with no word of the firers' "?".
        await SetupDummies(watched: false);
        var proposed = await Play(NoRoll()).ProposeAsync(GameActions.Fire, Fire("fire-1", ["r1", "r2"]), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        var russian = Current.Perspectives.Single(item => item.Name == "russian");
        var reasons = proposed.Plan!.Fire!.ReasonsFor(proposed.Reasons, russian);
        Assert.DoesNotContain(reasons, reason => reason.Contains("Dummy", StringComparison.OrdinalIgnoreCase) || reason.Contains("d1", StringComparison.Ordinal)
            || reason.Contains("concealment", StringComparison.OrdinalIgnoreCase));
    }
}
